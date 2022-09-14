// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !WINDOWS_WINAPPSDK
using SkiaSharp.Views.UWP;
using SkiaSharp;
#endif

namespace SkiaExperiment.Samples;

[ToolkitSample(id: nameof(SkiaCustomSample), "Custom control", description: $"A sample for showing how to create and use a {nameof(Skia)} custom control.")]
public sealed partial class SkiaCustomSample : Page
{
    public SkiaCustomSample()
    {
        this.InitializeComponent();
#if WINDOWS_WINAPPSDK
        throw new Exception("not supported");
#else
        var panel = new SKSwapChainPanel()
        {
            Width = 500,
            Height = 500,
            EnableRenderLoop = true
        };
        panel.PaintSurface += (object sender, SKPaintGLSurfaceEventArgs args) =>
        {
            double t = (DateTime.Now - DateTime.MinValue).TotalSeconds;
            var pow2 = (double x) => (float)(x * x);
            var sinPow2 = (double t) => pow2(Math.Sin(t));
            args.Surface.Canvas.Clear(new SKColorF(sinPow2(t * .75), sinPow2(t * 1.1), sinPow2(t * .4)));
        };
        this.Content = panel;
#endif
    }
}
