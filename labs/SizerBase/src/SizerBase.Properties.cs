// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !WINAPPSDK
using CursorEnum = Windows.UI.Core.CoreCursorType;
#else
using Microsoft.UI.Input;
using CursorEnum = Microsoft.UI.Input.InputSystemCursorShape;
#endif

namespace CommunityToolkit.Labs.WinUI;

/// <summary>
/// Properties for <see cref="SizerBase"/>
/// </summary>
public partial class SizerBase : Control
{
    /// <summary>
    /// Gets or sets the cursor to use when hovering over the gripper bar. If left as <c>null</c>, the control will manage the cursor automatically based on the <see cref="Orientation"/> property value.
    /// </summary>
    public CursorEnum Cursor
    {
        get { return (CursorEnum)GetValue(CursorProperty); }
        set { SetValue(CursorProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="Cursor"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty CursorProperty =
        DependencyProperty.Register(nameof(Cursor), typeof(CursorEnum), typeof(SizerBase), new PropertyMetadata(null, OnOrientationPropertyChanged));

    /// <summary>
    /// Gets or sets the incremental amount of change for draging with the mouse or touch of a sizer control. Effectively a snapping increment for changes. The default is 1.
    /// </summary>
    /// <example>
    /// For instance, if the DragIncrement is set to 16. Then when a component is resized with the sizer, it will only increase or decrease in size in that increment. I.e. -16, 0, 16, 32, 48, etc...
    /// </example>
    /// <remarks>
    /// This value is indepedent of the <see cref="KeyboardIncrement"/> property. If you need to provide consistent snapping when moving regardless of input device, set these properties to the same value.
    /// </remarks>
    public double DragIncrement
    {
        get { return (double)GetValue(DragIncrementProperty); }
        set { SetValue(DragIncrementProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="DragIncrement"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty DragIncrementProperty =
        DependencyProperty.Register(nameof(DragIncrement), typeof(double), typeof(SizerBase), new PropertyMetadata(1d));

    /// <summary>
    /// Gets or sets the distance each press of an arrow key moves a sizer control. The default is 8.
    /// </summary>
    /// <remarks>
    /// This value is independent of the <see cref="DragIncrement"/> setting when using mouse/touch. If you want a consistent behavior regardless of input device, set them to the same value if snapping is required.
    /// </remarks>
    public double KeyboardIncrement
    {
        get { return (double)GetValue(KeyboardIncrementProperty); }
        set { SetValue(KeyboardIncrementProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="KeyboardIncrement"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty KeyboardIncrementProperty =
        DependencyProperty.Register(nameof(KeyboardIncrement), typeof(double), typeof(SizerBase), new PropertyMetadata(8d));

    /// <summary>
    /// Gets or sets the orientation the sizer will be and how it will interact with other elements. Defaults to <see cref="Orientation.Vertical"/>.
    /// </summary>
    /// <remarks>
    /// Note if using <see cref="GridSplitter"/>, use the <see cref="GridSplitter.ResizeDirection"/> property instead.
    /// </remarks>
    public Orientation Orientation
    {
        get { return (Orientation)GetValue(OrientationProperty); }
        set { SetValue(OrientationProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="Orientation"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(SizerBase), new PropertyMetadata(Orientation.Vertical, OnOrientationPropertyChanged));

    private static void OnOrientationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SizerBase gripper)
        {
            CursorEnum cursorToUse = gripper.Orientation == Orientation.Vertical ? CursorEnum.SizeWestEast : CursorEnum.SizeNorthSouth;

            // See if there's been a cursor override, otherwise we'll pick
            var cursor = gripper.ReadLocalValue(CursorProperty);
            if (cursor == DependencyProperty.UnsetValue || cursor == null)
            {
                cursor = cursorToUse;

                // On UWP, we use the extension in XAML to control this behavior,
                // so we'll update it here (and maintain binding).
                // We'll keep it in-sync to maintain behavior for WinUI 3 as well.
                gripper.SetValue(CursorProperty, cursor);

                // We return here, as the Cursor will trigger this function again anyway to set for WinUI 3
                return;
            }

            // TODO: [UNO] Only supported on certain platforms
            // See ProtectedCursor here: https://github.com/unoplatform/uno/blob/3fe3862b270b99dbec4d830b547942af61b1a1d9/src/Uno.UI/UI/Xaml/UIElement.cs#L1015-L1023
#if WINAPPSDK && !HAS_UNO
            // Need to wait until we're at least applying template step of loading before setting Cursor
            // See https://github.com/microsoft/microsoft-ui-xaml/issues/7062
            if (gripper._applyingTemplate &&
                cursor is CursorEnum cursorToSet &&
                (gripper.ProtectedCursor == null ||
                    (gripper.ProtectedCursor is InputSystemCursor current &&
                     current.CursorShape != cursorToSet)))
            {
                gripper.ProtectedCursor = InputSystemCursor.Create(cursorToSet);
            }
#endif
        }
    }
}
