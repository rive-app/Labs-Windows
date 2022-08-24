// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !WINDOWS_UWP

namespace CommunityToolkit.Labs.WinUI;

public partial class RivePlayer : ContentControl
{
    public bool DrawInBackground { get; set; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
    }

    private async Task<byte[]?> ReadFileBytesAsync(string name, CancellationToken cancellationToken)
    {
        return null;
    }


    private void OnPaintSurface(object? sender, EventArgs e)
    {
    }

    public event EventHandler PaintSurface;
}

#endif
