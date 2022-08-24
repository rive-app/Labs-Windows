// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if WINDOWS_UWP

using RiveSharp;
using SkiaSharp.Views.UWP;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;

namespace CommunityToolkit.Labs.WinUI;

public partial class RivePlayer : SKSwapChainPanel
{
    // Incremented when the "InvalLoop" (responsible for scheduling PaintSurface events) should
    // terminate.
    int _invalLoopContinuationToken = 0;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Window.Current.Visible)
        {
            InvalLoopAsync(_invalLoopContinuationToken);
        }
        Window.Current.VisibilityChanged += (object s, VisibilityChangedEventArgs vce) =>
        {
            ++_invalLoopContinuationToken;  // Terminate the existing inval loop (if any).
            if (vce.Visible)
            {
                InvalLoopAsync(_invalLoopContinuationToken);
            }
        };
    }

    // Schedules continual PaintSurface events at 120fps until the window is no longer visible.
    // (Multiple calls to Invalidate() between PaintSurface events are coalesced.)
    private async void InvalLoopAsync(int continuationToken)
    {
        while (continuationToken == _invalLoopContinuationToken)
        {
            Invalidate();
            await Task.Delay(TimeSpan.FromMilliseconds(8));  // 120 fps
        }
    }

    private async Task<byte[]?> ReadFileBytesAsync(string name, CancellationToken cancellationToken)
    {
        byte[]? data = null;
        var getFileTask = Package.Current.InstalledLocation.TryGetItemAsync(name);
        var storageFile = await getFileTask as StorageFile;
        if (storageFile != null && !cancellationToken.IsCancellationRequested)
        {
            var inputStream = await storageFile.OpenSequentialReadAsync();
            var fileStream = inputStream.AsStreamForRead();
            data = new byte[fileStream.Length];
            fileStream.Read(data, 0, data.Length);
            fileStream.Dispose();  // Don't keep the file open.
        }
        return data;
    }

    private void OnPaintSurface(object sender, SKPaintGLSurfaceEventArgs e)
    {
        this.PaintNextAnimationFrame(e.Surface, e.BackendRenderTarget.Width, e.BackendRenderTarget.Height);
    }
}

#endif  // WINDOWS_UWP
