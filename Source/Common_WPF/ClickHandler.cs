using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Common.XAML
{
    /// <summary>
    /// Provides 
    /// </summary>
    public class ClickHandler
    {
        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// The buttons types supported for clicking (Note: Silverlight doesn't yet support middle mouse clicking).
        /// </summary>
        public enum MouseButtons { None, Left, Right };

        // ---------------------------------------------------------------------------------------------------------------------

        public UIElement WrappedElement { get; private set; }

        /// <summary>
        /// Number of milliseconds between each click required to qualify a sequence as either a single, double, or triple click.
        /// <para>Note: Changing this value has no affect on click sequences already in progress.</para>
        /// </summary>
        public static int DefaultClickTimeout = 150;

        DispatcherTimer _ClickTimer = new DispatcherTimer();
        int _Clicks;
        MouseButtonEventArgs _LastMouseButtonUpEventArgs;
        MouseButtons _ButtonClicked = MouseButtons.None;

        public Point LastMouseClickPosition;
        public MouseButtons LastMouseButtonClicked;
        public UIElement LastElementClicked;

        /// <summary>
        /// If false (default), then the mouse has to be within a certain radius for each click sequence in order to quality (see 'ClickRadiusThreshold' for more details).
        /// On touch devices, it is recommended to set this to true, as the user will most likely not press twice on the same exact spot. 
        /// </summary>
        public bool IgnoreMousePosition { get; set; }

        /// <summary>
        /// This is the maximum pixel distance allowed between each click in order to qualify the number of clicks (defaults to 3 pixels).
        /// If the mouse is outside this range, the click sequence is reset.
        /// <para>Note: This property has no effect if 'IgnoreMousePosition' is true.</para>
        /// </summary>
        public double ClickRadiusThreshold { get; set; }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Triggered for any mouse down event.
        /// </summary>
        public event MouseButtonEventHandler AnyMouseButtonDown;
        /// <summary>
        /// Triggered for any mouse up event.
        /// </summary>
        public event MouseButtonEventHandler AnyMouseButtonUp;
        /// <summary>
        /// Triggered for any mouse button single click.
        /// </summary>
        public event MouseButtonEventHandler AnyMouseButtonClick;
        /// <summary>
        /// Triggered for any mouse button double click.
        /// </summary>
        public event MouseButtonEventHandler AnyMouseButtonDoubleClick;
        /// <summary>
        /// Triggered for any mouse button triple click.
        /// </summary>
        public event MouseButtonEventHandler AnyMouseButtonTripleClick;

        /// <summary>
        /// Called for each left mouse button click sequence.
        /// </summary>
        public event MouseButtonEventHandler MouseLeftClick;

        /// <summary>
        /// Called when the left mouse button is clicked. This is qualified by a timeout before another "click" is detected.
        /// </summary>
        public event MouseButtonEventHandler MouseLeftSingleClick;

        /// <summary>
        /// Called when the left mouse is double clicked. This is qualified by a timeout before another "click" is detected.
        /// </summary>
        public event MouseButtonEventHandler MouseLeftDoubleClick;

        /// <summary>
        /// Called when the left mouse is triple clicked. This is qualified by a timeout before another "click" is detected.
        /// </summary>
        public event MouseButtonEventHandler MouseLeftTripleClick;

        /// <summary>
        /// Called for each right mouse button click sequence.
        /// </summary>
        public event MouseButtonEventHandler MouseRightClick;

        /// <summary>
        /// Called when the right mouse is clicked. This is qualified by a timeout before another "click" is detected.
        /// </summary>
        public event MouseButtonEventHandler MouseRightSingleClick;

        /// <summary>
        /// Called when the right mouse is double clicked. This is qualified by a timeout before another "click" is detected.
        /// </summary>
        public event MouseButtonEventHandler MouseRightDoubleClick;

        /// <summary>
        /// Called when the right mouse is triple clicked. This is qualified by a timeout before another "click" is detected.
        /// </summary>
        public event MouseButtonEventHandler MouseRightTripleClick;

        // ---------------------------------------------------------------------------------------------------------------------

        public double ElementOpacity { get { return WrappedElement.Opacity; } set { WrappedElement.Opacity = value; } }

        // ---------------------------------------------------------------------------------------------------------------------

        public ClickHandler(UIElement element)
        {
            ClickRadiusThreshold = 3d;

            WrappedElement = element;

            _ClickTimer.Tick += _ClickTimer_Tick;

            element.MouseLeftButtonDown += _ClickHandling_MouseLeftButtonDown;
            element.MouseLeftButtonUp += _ClickHandling_MouseLeftButtonUp;
            element.MouseRightButtonDown += _ClickHandling_MouseRightButtonDown;
            element.MouseRightButtonUp += _ClickHandling_MouseRightButtonUp;
        }

        // ---------------------------------------------------------------------------------------------------------------------

        void _ClickHandling_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ((UIElement)sender).CaptureMouse();

            if (_ButtonClicked == MouseButtons.None)
                _ButtonClicked = MouseButtons.Left;

            // ... if beginning a click process, get mouse position (if it moves, the clicks are ignored) ...
            if (_Clicks == 0)
            {
                _ClickTimer.Interval = new TimeSpan(0, 0, 0, 0, DefaultClickTimeout); // (after a "UserControl_MouseLeftButtonUp" event, the user must click again within this time to add another click count)
                LastMouseClickPosition = e.GetPosition(VisualTree.RootVisual);
                LastMouseButtonClicked = _ButtonClicked;
                LastElementClicked = sender as UIElement;
            }

            // ... "pause" timer until the mouse comes back up ...
            _ClickTimer.Stop();

            if (AnyMouseButtonDown != null)
                AnyMouseButtonDown(sender, e);

            e.Handled = true; // (note: this will prevent chaining the click handlers - which is bad practice anyhow)
        }

        void _ClickHandling_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        { _ButtonClicked = MouseButtons.Right; _ClickHandling_MouseLeftButtonDown(sender, e); }

        void _ClickHandling_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_ButtonClicked == MouseButtons.None)
                _ButtonClicked = MouseButtons.Left;

            var mousePosOk = true;

            if (!IgnoreMousePosition)
            {
                var mousePos = e.GetPosition(VisualTree.RootVisual);
                var distance = MathExt.GetDistance(LastMouseClickPosition.X, LastMouseClickPosition.Y, mousePos.X, mousePos.Y);
                if (distance > ClickRadiusThreshold)
                    mousePosOk = false;
            }

            if (mousePosOk
                && _ButtonClicked == LastMouseButtonClicked
                && sender as UIElement == LastElementClicked)
            {
                _Clicks++;
                _LastMouseButtonUpEventArgs = e;

                // (provide instant click feedback event)
                if (_ButtonClicked == MouseButtons.Left)
                {
                    if (MouseLeftClick != null)
                        MouseLeftClick(LastElementClicked, _LastMouseButtonUpEventArgs);
                }
                else
                {
                    if (MouseRightClick != null)
                        MouseRightClick(LastElementClicked, _LastMouseButtonUpEventArgs);
                }

                if (AnyMouseButtonUp != null)
                    AnyMouseButtonUp(sender, e);

                _ClickTimer.Start();
            }
            else _Clicks = 0; // (mouse moved, or wrong button, so reset the click event process)

            ((UIElement)sender).ReleaseMouseCapture();
            _ButtonClicked = MouseButtons.None;

            e.Handled = true; // (note: this will prevent chaining the click handlers - which is bad practice anyhow)
        }

        void _ClickHandling_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        { _ButtonClicked = MouseButtons.Right; _ClickHandling_MouseLeftButtonUp(sender, e); }

        void _ClickTimer_Tick(object sender, EventArgs e)
        {
            _ClickTimer.Stop();
            if (_Clicks == 1)
            {
                if (AnyMouseButtonClick != null)
                    AnyMouseButtonClick(LastElementClicked, _LastMouseButtonUpEventArgs);

                if (LastMouseButtonClicked == MouseButtons.Left)
                {
                    if (MouseLeftSingleClick != null)
                        MouseLeftSingleClick(LastElementClicked, _LastMouseButtonUpEventArgs);
                }
                else
                {
                    if (MouseRightSingleClick != null)
                        MouseRightSingleClick(LastElementClicked, _LastMouseButtonUpEventArgs);
                }
            }
            else if (_Clicks == 2)
            {
                if (AnyMouseButtonDoubleClick != null)
                    AnyMouseButtonDoubleClick(LastElementClicked, _LastMouseButtonUpEventArgs);

                if (LastMouseButtonClicked == MouseButtons.Left)
                {
                    if (MouseLeftDoubleClick != null)
                        MouseLeftDoubleClick(LastElementClicked, _LastMouseButtonUpEventArgs);
                }
                else
                {
                    if (MouseRightDoubleClick != null)
                        MouseRightDoubleClick(LastElementClicked, _LastMouseButtonUpEventArgs);
                }
            }
            else if (_Clicks == 3)
            {
                if (AnyMouseButtonTripleClick != null)
                    AnyMouseButtonTripleClick(LastElementClicked, _LastMouseButtonUpEventArgs);

                if (LastMouseButtonClicked == MouseButtons.Left)
                {
                    if (MouseLeftTripleClick != null)
                        MouseLeftTripleClick(LastElementClicked, _LastMouseButtonUpEventArgs);
                }
                else
                {
                    if (MouseRightTripleClick != null)
                        MouseRightTripleClick(LastElementClicked, _LastMouseButtonUpEventArgs);
                }
            }
            _Clicks = 0; // (reset clicks [possible to support 4+ clicks?])
        }

        // ---------------------------------------------------------------------------------------------------------------------

        public void RemoveElement()
        {
            if (WrappedElement is FrameworkElement && ((FrameworkElement)WrappedElement).Parent != null)
                VisualTree.RemoveElement((FrameworkElement)WrappedElement);
        }

        public void HideElement()
        {
            WrappedElement.Visibility = Visibility.Collapsed;
        }

        public void ShowElement()
        {
            WrappedElement.Visibility = Visibility.Visible;
        }

        // ---------------------------------------------------------------------------------------------------------------------
    }

    public static class ClickHandlerExtensionMethods
    {
        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Get a click handlers that supports clicking on elements one or more times.
        /// </summary>
        /// <param name="element">The element to apply click handling to.</param>
        public static ClickHandler GetClickHandler(this UIElement element)
        {
            return new ClickHandler(element);
        }

        // ---------------------------------------------------------------------------------------------------------------------
    }
}
