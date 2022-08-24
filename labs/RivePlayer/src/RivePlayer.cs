// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using RiveSharp;
using SkiaSharp;
using System.Collections.Concurrent;
using System.Net;

namespace CommunityToolkit.Labs.WinUI;

// Implements a simple view that plays content from a .riv file.
public partial class RivePlayer
{
    private CancellationTokenSource? _activeSourceFileLoader = null;

    public RivePlayer()
    {
        this.StateMachineInputCollection.SetRivePlayer(this);
        this.Loaded += OnLoaded;
        this.PointerPressed +=
            (object s, PointerRoutedEventArgs e) => HandlePointerEvent(_scene.PointerDown, e);
        this.PointerMoved +=
            (object s, PointerRoutedEventArgs e) => HandlePointerEvent(_scene.PointerMove, e);
        this.PointerReleased +=
            (object s, PointerRoutedEventArgs e) => HandlePointerEvent(_scene.PointerUp, e);
        this.PaintSurface += OnPaintSurface;
    }

    private async void LoadSourceFileDataAsync(string name, CancellationToken cancellationToken)
    {
        byte[]? data = null;
        if (Uri.TryCreate(name, UriKind.Absolute, out var uri))
        {
            data = await new WebClient().DownloadDataTaskAsync(uri);
        }
        else
        {
            data = await ReadFileBytesAsync(name, cancellationToken);
        }
        if (data != null && !cancellationToken.IsCancellationRequested)
        {
            sceneActionsQueue.Enqueue(() => UpdateScene(SceneUpdates.File, data));
            // Apply deferred state machine inputs once the scene is fully loaded.
            foreach (Action stateMachineInput in _deferredSMInputsDuringFileLoad!)
            {
                sceneActionsQueue.Enqueue(stateMachineInput);
            }
        }
        _deferredSMInputsDuringFileLoad = null;
        _activeSourceFileLoader = null;
    }

    // State machine inputs to set once the current async file load finishes.
    private List<Action>? _deferredSMInputsDuringFileLoad = null;

    private void EnqueueStateMachineInput(Action stateMachineInput)
    {
        if (_deferredSMInputsDuringFileLoad != null)
        {
            // A source file is currently loading async. Don't set this input until it completes.
            _deferredSMInputsDuringFileLoad.Add(stateMachineInput);
        }
        else
        {
            sceneActionsQueue.Enqueue(stateMachineInput);
        }
    }

    public void SetBool(string name, bool value)
    {
        EnqueueStateMachineInput(() => _scene.SetBool(name, value));
    }

    public void SetNumber(string name, float value)
    {
        EnqueueStateMachineInput(() => _scene.SetNumber(name, value));
    }

    public void FireTrigger(string name)
    {
        EnqueueStateMachineInput(() => _scene.FireTrigger(name));
    }

    private delegate void PointerHandler(Vec2D pos);

    private void HandlePointerEvent(PointerHandler handler, PointerRoutedEventArgs e)
    {
        if (_activeSourceFileLoader != null)
        {
            // Ignore pointer events while a new scene is loading.
            return;
        }

        // Capture the viewSize and pointerPos at the time of the event.
        var viewSize = this.ActualSize;
        var pointerPos = e.GetCurrentPoint(this).Position;

        // Forward the pointer event to the render thread.
        sceneActionsQueue.Enqueue(() =>
        {
            Mat2D mat = ComputeAlignment(viewSize.X, viewSize.Y);
            if (mat.Invert(out var inverse))
            {
                Vec2D artboardPos = inverse * new Vec2D((float)pointerPos.X, (float)pointerPos.Y);
                handler(artboardPos);
            }
        });
    }

    // _scene is used on the render thread exclusively.
    Scene _scene = new Scene();

    // Source actions originating from other threads must be funneled through this queue.
    readonly ConcurrentQueue<Action> sceneActionsQueue = new ConcurrentQueue<Action>();

    // This is the render-thread copy of the animation parameters. They are set via
    // _sceneActionsQueue. _scene is then blah blah blah
    private string? _artboardName;
    private string? _animationName;
    private string? _stateMachineName;

    private enum SceneUpdates
    {
        File = 3,
        Artboard = 2,
        AnimationOrStateMachine = 1,
    };

    private DateTime? _lastPaintTime;

    private void PaintNextAnimationFrame(SKSurface surface, int surfaceWidth, int surfaceHeight)
    {
        // Handle pending scene actions from the main thread.
        while (sceneActionsQueue.TryDequeue(out var action))
        {
            action();
        }

        if (!_scene.IsLoaded)
        {
            return;
        }

        // Run the animation.
        var now = DateTime.Now;
        if (_lastPaintTime != null)
        {
            _scene.AdvanceAndApply((now - _lastPaintTime!).Value.TotalSeconds);
        }
        _lastPaintTime = now;

        // Render.
        surface.Canvas.Clear();
        var renderer = new Renderer(surface.Canvas);
        renderer.Save();
        renderer.Transform(ComputeAlignment(surfaceWidth, surfaceHeight));
        _scene.Draw(renderer);
        renderer.Restore();
    }

    // Called from the render thread. Updates _scene according to updates.
    void UpdateScene(SceneUpdates updates, byte[]? sourceFileData = null)
    {
        if (updates >= SceneUpdates.File)
        {
            _scene.LoadFile(sourceFileData);
        }
        if (updates >= SceneUpdates.Artboard)
        {
            _scene.LoadArtboard(_artboardName);
        }
        if (updates >= SceneUpdates.AnimationOrStateMachine)
        {
            if (!String.IsNullOrEmpty(_stateMachineName))
            {
                _scene.LoadStateMachine(_stateMachineName);
            }
            else if (!String.IsNullOrEmpty(_animationName))
            {
                _scene.LoadAnimation(_animationName);
            }
            else
            {
                if (!_scene.LoadStateMachine(null))
                {
                    _scene.LoadAnimation(null);
                }
            }
        }
    }

    // Called from the render thread. Computes alignment based on the size of _scene.
    private Mat2D ComputeAlignment(double width, double height)
    {
        return ComputeAlignment(new AABB(0, 0, (float)width, (float)height));
    }

    // Called from the render thread. Computes alignment based on the size of _scene.
    private Mat2D ComputeAlignment(AABB frame)
    {
        return Renderer.ComputeAlignment(Fit.Contain, Alignment.Center, frame,
                                         new AABB(0, 0, _scene.Width, _scene.Height));
    }
}
