using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Common.CollectionsAndLists;
using Common.XAML;
using Common.Tasks;

namespace Common.XAML.Windows
{
    public partial class Window : UserControl
    {
        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// The window base control is the root container of all windowed objects. The owner/parent of this
        /// base control is the focus of a close event when determining which control needs to be removed
        /// from the visual display tree. This is detected by default, but a custom control can be set if
        /// needed (for instance, if there are panel-derived controls between the window element and the root
        /// control).
        /// Note: The base control can be set by name via the BaseControlName property.
        /// </summary>
        public FrameworkElement BaseControl
        { get { return VisualTree.ParentPanelRootChild(this); } }


        /// <summary>
        /// Makes the window "modal", in that nothing behind it can be selected.
        /// </summary>
        public bool Modal
        {
            get { return _Modal; }
            set
            {
                _Modal = value;
            }
        }
        bool _Modal;

        /// <summary>
        /// A user defined object that is responsible for the window, if any.
        /// The meaning is arbitrary, and is mainly useful in managing windows in groups (for example, closing all
        /// windows associated with the owning object when that object is being destroyed).
        /// By default, the application's instance (Application.Current) owns all windows.
        /// </summary>
        public object Owner { get { return _Owner ?? Application.Current; } private set { _Owner = value; } }
        object _Owner;

        /// <summary>
        /// Setting a window name allows finding the window via WindowsManager.Windows.GetWindow().
        /// </summary>
        new public string Name
        {
            get { return _Name ?? base.Name; }
            set
            {
                if (value != _Name)
                {
                    if (!string.IsNullOrWhiteSpace(_Name))
                        WindowsManager.DefaultInstance._UnRegisterNamedWindow(this); // (release named window from dictionary)
                    _Name = value;
                    base.Name = _Name;
                    if (!string.IsNullOrWhiteSpace(_Name))
                        WindowsManager.DefaultInstance._RegisterNamedWindow(this); // (if name value is valid, then [re-]register the window)
                }
            }
        }
        string _Name;

        // --------------------------------------------------------------------------------------------------

        //public int MinWidth { get; set; }
        //public int MinHeight { get; set; }

        /// <summary>
        /// Enable/disable resizing the window.
        /// </summary>
        public bool Resizeable { get; set; }

        /// <summary>
        /// If true, the window is simply hidden on close, and not removed.
        /// </summary>
        public bool HideOnClose { get; set; }

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// The call-back method to execute when a button is pressed.
        /// </summary>
        public WindowEventHandler ButtonPressed;

        public event RoutedEventHandler Showing;
        public event RoutedEventHandler Hiding;
        public event RoutedEventHandler Closing;

        public void ResetWindowEvents()
        {
            ButtonPressed = null;
            Showing = null;
            Hiding = null;
            Closing = null;

            Showing += _Window_Showing;
            Hiding += _Window_Hiding;
        }

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// If false, any non-title-bar button will automatically close the window.
        /// </summary>
        public bool CloseOnButtonPressed = true;

        /// <summary>
        /// Once set, automatically closes the window. Returns the last button clicked, if any.
        /// </summary>
        public WindowButtonType LastButtonPressed { get { return _LastButtonPressed; } }

        // --------------------------------------------------------------------------------------------------

        bool _initialized = false;

        bool _WindowMoving = false;
        Point _MoveMoveOffset;
        double _OpacityBeforeMove = 1.0;
        TranslateTransform _Translation = null;
        WindowButtonType _LastButtonPressed = WindowButtonType.None;
        bool _MouseOverResizeArea = false;
        bool _Resizing = false;

        Panel _lastPanelOwner = null;

        TimedTask _TimeoutTask;
        TimedTask _WindowAnimationTask;

        // --------------------------------------------------------------------------------------------------

        void _Construct()
        {
            InitializeComponent();

            MinWidth = 54;
            MinHeight = 90;
            Resizeable = true;
            HideOnClose = false;

            //??Effect = new DropShadowEffect { BlurRadius = 16, Color = Colors.Black, Direction = 0, Opacity = 1, ShadowDepth = 0 };

            this.RenderTransform = _Translation = new TranslateTransform();


            // WARNING: App.Current.RootVisual may be null here, so only hook root events in the "loaded" event.
            Loaded += _Window_Loaded;
            Unloaded += _Window_Unloaded;

            ResetWindowEvents();

            // ...

            OnButtonsChanged();

            // ...

            _Initialize();

            _UpdateWindowVisibility();
            _UpdateCloseButtonVisibility();

            if (OkButton != null && OkButton.Visibility == System.Windows.Visibility.Visible) OkButton.Focus();

            int windowIndex = WindowsManager.Windows.GetFirstNullIndex();
            if (windowIndex >= 0)
                WindowsManager.Windows[windowIndex] = this;
            else
                WindowsManager.Windows.Add(this);
            // TODO: Add this good example to the help doc.
        }

        /// <summary>
        /// Note: To create via code, use the 'CreateWindow()' method.
        /// </summary>
        public Window() : base() { _Construct(); }

        /// <summary>
        /// Creates a new window associated with an application.
        /// </summary>
        /// <param name="app">The application that will own this window.</param>
        /// <param name="title">The window title.</param>
        /// <param name="buttons">Buttons to show on the bottom of the window.</param>
        /// <param name="timeout">If >0, this will create a task to 'auto close' the window, or execute a timeout call-back instead.</param>
        /// <param name="timeoutCallback">If specified, this call back method will handle the timeout. The window will not 'auto close'. You can manually call 'DoButtonPressed()' to close the window yourself.</param>
        internal Window(string title, WindowButtons buttons, TitleBarButtons titleBarButtons = TitleBarButtons.Close, int timeout = 0, Action<Window> timeoutCallback = null)
        {
            //??MaxWidth = WindowsManager.App.Host.Content.ActualWidth;
            //??MaxHeight = WindowsManager.App.Host.Content.ActualHeight;

            Title = title;
            Buttons = buttons;
            TitleBarButtons = titleBarButtons;

            _Construct();

            if (timeout > 0)
            {
                _TimeoutTask = TimedTasks.AddTask(timeout, () =>
                {
                    if (VisualTree.IsVisible(this))
                        if (timeoutCallback != null)
                            timeoutCallback(this);
                        else
                            if (Buttons == WindowButtons.OkCancel || Buttons == WindowButtons.YesNoCancel)
                                DoButtonPressed(CancelButton, WindowButtonType.Cancel);
                            else if (Buttons == WindowButtons.YesNo)
                                DoButtonPressed(NoButton, WindowButtonType.No);
                            else if (Buttons == WindowButtons.Ok)
                                DoButtonPressed(NoButton, WindowButtonType.Ok);
                            else
                                DoButtonPressed(null, WindowButtonType.None); // (just close, no default cancel/no/ok button)
                });

                _TimeoutTask.Paused = !this.IsVisible;
            }
        }

        ~Window()
        {
            // WARNING: Runs in the GC thread.
        }

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// Creates a simple empty window, and optionally shows an "Ok" button.
        /// </summary>
        /// <param name="title">The window's title.</param>
        /// <param name="showOkButton">If 'false' (default), no buttons are shown.</param>
        public static Window CreateEmptyWindow(string title, bool showOkButton = false)
        {
            var msgWin = new Window(title, (showOkButton ? WindowButtons.Ok : WindowButtons.None));
            return msgWin;
        }

        // --------------------------------------------------------------------------------------------------

        ClickHandler _InputBlockHandler;

        void _Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_MovedToFrontElementOrder != sender)
                _InputBlockHandler = WindowsManager.CreateScreenOverlay();
            _MovedToFrontElementOrder = null;
        }

        void _Window_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_MovedToFrontElementOrder != sender)
                _InputBlockHandler.RemoveElement();
        }

        void _Window_Showing(object sender, RoutedEventArgs e)
        {
            if (_InputBlockHandler != null)
                _InputBlockHandler.ShowElement();
        }

        void _Window_Hiding(object sender, RoutedEventArgs e)
        {
            if (_InputBlockHandler != null)
                _InputBlockHandler.HideElement();
        }

        // --------------------------------------------------------------------------------------------------

        public bool DoButtonPressed(object eventSource, WindowButtonType button)
        {
            var eventArgs = new WindowEventArgs(eventSource, button);
            if (ButtonPressed != null)
                ButtonPressed(this, eventArgs);
            if (!eventArgs.Cancelled)
            {
                if (CloseOnButtonPressed && button != WindowButtonType.Minimize && button != WindowButtonType.MaximizeOrRestore
                    && button != WindowButtonType.Close) Close();
                _LastButtonPressed = button;
                return true;
            }
            return false;
        }

        // --------------------------------------------------------------------------------------------------

        void _Initialize()
        {
            if (!_initialized)
            {
                if (VisualTree.RootVisual != null)
                {
                    VisualTree.RootVisual.MouseMove += RootVisual_MouseMove;
                    VisualTree.RootVisual.MouseLeftButtonUp += RootVisual_MouseLeftButtonUp;
                }

                WindowTitleBar.TitleBar.MouseLeftButtonDown += TitleBar_MouseLeftButtonDown;
                WindowTitleBar.WinTitleBarButtons.Minimize += TitleBarButtons_Minimize;
                WindowTitleBar.WinTitleBarButtons.MaximizeOrRestore += TitleBarButtons_MaximizeOrRestore;
                WindowTitleBar.WinTitleBarButtons.Close += TitleBarButtons_Close;
                MouseLeftButtonDown += SWWindow_MouseLeftButtonDown;

                YesButton.Click += YesButton_Click;
                NoButton.Click += NoButton_Click;
                OkButton.Click += OkButton_Click;
                CancelButton.Click += CancelButton_Click;

                LayoutUpdated += Window_LayoutUpdated;

                _initialized = true;
            }
        }

        void Window_LayoutUpdated(object sender, EventArgs e)
        {
            if (IsInVisualTree)
            {
                var topLeftWindowCornerOffset = this.GetRootVisualPosition();
                if (topLeftWindowCornerOffset.Y < 0)
                    _Translation.Y += -topLeftWindowCornerOffset.Y; // (keep window title bar under top of screen)
            }
        }

        // --------------------------------------------------------------------------------------------------

        void _Uninitialize()
        {
            if (_initialized)
            {
                LayoutUpdated -= Window_LayoutUpdated;

                if (VisualTree.RootVisual != null)
                {
                    VisualTree.RootVisual.MouseMove -= RootVisual_MouseMove;
                    VisualTree.RootVisual.MouseLeftButtonUp -= RootVisual_MouseLeftButtonUp;
                }

                Content = null; // (release the content into the GC separately)

                WindowTitleBar.TitleBar.MouseLeftButtonDown -= TitleBar_MouseLeftButtonDown;
                WindowTitleBar.TitleBarButtons.Minimize -= TitleBarButtons_Minimize;
                WindowTitleBar.TitleBarButtons.MaximizeOrRestore -= TitleBarButtons_MaximizeOrRestore;
                WindowTitleBar.TitleBarButtons.Close -= TitleBarButtons_Close;
                MouseLeftButtonDown -= SWWindow_MouseLeftButtonDown;

                YesButton.Click -= YesButton_Click;
                NoButton.Click -= NoButton_Click;
                OkButton.Click -= OkButton_Click;
                CancelButton.Click -= CancelButton_Click;

                _initialized = false;
            }
        }

        // --------------------------------------------------------------------------------------------------

        public void OnButtonsChanged()
        {
            if (ButtonPanel != null)
                switch (Buttons)
                {
                    case WindowButtons.None:
                        ButtonPanel.Visibility = Visibility.Collapsed;
                        YesButton.Visibility = Visibility.Collapsed;
                        NoButton.Visibility = Visibility.Collapsed;
                        OkButton.Visibility = Visibility.Collapsed;
                        CancelButton.Visibility = Visibility.Collapsed;
                        break;
                    case WindowButtons.Ok:
                        ButtonPanel.Visibility = Visibility.Visible;
                        YesButton.Visibility = Visibility.Collapsed;
                        NoButton.Visibility = Visibility.Collapsed;
                        OkButton.Visibility = Visibility.Visible;
                        CancelButton.Visibility = Visibility.Collapsed;
                        break;
                    case WindowButtons.OkCancel:
                        ButtonPanel.Visibility = Visibility.Visible;
                        YesButton.Visibility = Visibility.Collapsed;
                        NoButton.Visibility = Visibility.Collapsed;
                        OkButton.Visibility = Visibility.Visible;
                        CancelButton.Visibility = Visibility.Visible;
                        break;
                    case WindowButtons.YesNo:
                        ButtonPanel.Visibility = Visibility.Visible;
                        YesButton.Visibility = Visibility.Visible;
                        NoButton.Visibility = Visibility.Visible;
                        OkButton.Visibility = Visibility.Collapsed;
                        CancelButton.Visibility = Visibility.Collapsed;
                        break;
                    case WindowButtons.YesNoCancel:
                        ButtonPanel.Visibility = Visibility.Visible;
                        YesButton.Visibility = Visibility.Visible;
                        NoButton.Visibility = Visibility.Visible;
                        OkButton.Visibility = Visibility.Collapsed;
                        CancelButton.Visibility = Visibility.Visible;
                        break;
                }

            if (WindowTitleBar != null)
                WindowTitleBar.TitleBarButtonMode = TitleBarButtons;
        }

        // --------------------------------------------------------------------------------------------------

        private void RootVisual_MouseMove(object sender, MouseEventArgs e)
        {
            if (!VisualTree.IsInVisualTree(this))
                return;

            Point mouseXY = e.GetPosition(null);
            if (_WindowMoving)
            {
                mouseXY = e.GetPosition(WindowsManager.App.RootVisual);
                _Translation.X = mouseXY.X - _MoveMoveOffset.X;
                _Translation.Y = mouseXY.Y - _MoveMoveOffset.Y;
                IsMaximized = false;
            }

            mouseXY = e.GetPosition(this);

            double w;
            double h;

            if (LayoutRoot != null && Resizeable)
            {
                if (_Resizing)
                {
                    if (mouseXY.X + 4 < MinWidth) w = MinWidth;
                    else if (mouseXY.X + 4 > MaxWidth) w = MaxWidth;
                    else w = mouseXY.X + 4;

                    if (mouseXY.Y + 4 < MinHeight) h = MinHeight;
                    else if (mouseXY.Y + 4 > MaxHeight) h = MaxHeight;
                    else h = mouseXY.Y + 4;

                    this.Width = w;
                    this.Height = h;

                    IsMaximized = false;
                }

                w = ActualWidth;
                h = ActualHeight;

                _MouseOverResizeArea = (mouseXY.X <= w && w - mouseXY.X <= 5) && (mouseXY.Y <= h && h - mouseXY.Y <= 5);

                if (_MouseOverResizeArea)
                { if (Cursor != Cursors.Hand) Cursor = Cursors.Hand; }
                else
                { if (Cursor == Cursors.Hand) Cursor = null; }
            }
            else
            {
                _MouseOverResizeArea = false;
                _Resizing = false;
                if (Cursor == Cursors.Hand) Cursor = null;
            }
        }

        // --------------------------------------------------------------------------------------------------

        private void RootVisual_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Opacity = _OpacityBeforeMove;
            _WindowMoving = false;
            _Resizing = false;
        }

        // --------------------------------------------------------------------------------------------------


        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_WindowMoving) // (this helps make sure cursor doesn't go off-screen and mess everything up)
            {
                _MoveMoveOffset = e.GetPosition(WindowsManager.App.RootVisual);
                _MoveMoveOffset.X -= _Translation.X;
                _MoveMoveOffset.Y -= _Translation.Y;

                _OpacityBeforeMove = Opacity;
                Opacity = 0.75;

                _WindowMoving = true;
            }
        }

        /// <summary>
        /// If user clicks any part of the window, this event will bring it to the front.
        /// </summary>
        void SWWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MoveToFront();
            if (_MouseOverResizeArea) _Resizing = true;
        }

        // --------------------------------------------------------------------------------------------------

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            DoButtonPressed(sender, WindowButtonType.Yes);
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            DoButtonPressed(sender, WindowButtonType.No);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DoButtonPressed(sender, WindowButtonType.Ok);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DoButtonPressed(sender, WindowButtonType.Cancel);
        }

        private void TitleBarButtons_Minimize(object sender, RoutedEventArgs e)
        {
            if (_WindowAnimationTask == null)
                if (DoButtonPressed(sender, WindowButtonType.Minimize))
                    Hide();
        }

        public bool IsMaximized { get; private set; }
        Point _MaximizeRestorePoint, _MaximizeDeltaOffset, _MaximizeDeltaSizeOffset, _MaximizeStartPoint;
        double _MaximizeRestoreW, _MaximizeRestoreH, _MaximizeStartW, _MaximizeStartH;
        bool _MaximizeOrRestoreInProgress;

        private void TitleBarButtons_MaximizeOrRestore(object sender, RoutedEventArgs e)
        {
            if (_WindowAnimationTask == null)
                if (DoButtonPressed(sender, WindowButtonType.MaximizeOrRestore))
                    MaximizeOrRestore();
        }

        private void TitleBarButtons_Close(object sender, RoutedEventArgs e)
        {
            if (_WindowAnimationTask == null)
                if (DoButtonPressed(sender, WindowButtonType.Close))
                    Close();
        }

        // --------------------------------------------------------------------------------------------------

        public void MaximizeOrRestore()
        {
            if (_WindowAnimationTask == null)
            {
                var baseControl = BaseControl;
                _SetNewWindowAnimationTask(AddTask(1, (TimedTask task, object data) =>
                {
                    if (!_MaximizeOrRestoreInProgress)
                    {
                        if (IsMaximized)
                        {
                            _MaximizeDeltaOffset.X = _MaximizeRestorePoint.X;
                            _MaximizeDeltaOffset.Y = _MaximizeRestorePoint.Y;
                            _MaximizeStartPoint.X = 0;
                            _MaximizeStartPoint.Y = 0;
                            _MaximizeDeltaSizeOffset.X = -_MaximizeDeltaSizeOffset.X;
                            _MaximizeDeltaSizeOffset.Y = -_MaximizeDeltaSizeOffset.Y;
                            _MaximizeStartW = ActualWidth;
                            _MaximizeStartH = ActualHeight;
                        }
                        else
                        {
                            //??var currentPoint = TransformToVisual(WindowsManager.RootVisual).Transform(new Point(0, 0));
                            _MaximizeRestorePoint.X = _Translation.X;
                            _MaximizeRestorePoint.Y = _Translation.Y;
                            _MaximizeRestoreW = ActualWidth;
                            _MaximizeRestoreH = ActualHeight;

                            _MaximizeDeltaOffset.X = -_MaximizeRestorePoint.X; //-currentPoint.X;
                            _MaximizeDeltaOffset.Y = -_MaximizeRestorePoint.Y - WindowsManager.Desktop.TaskBar.ActualHeight / 2; //-currentPoint.Y;
                            _MaximizeStartPoint.X = _MaximizeRestorePoint.X;
                            _MaximizeStartPoint.Y = _MaximizeRestorePoint.Y;
                            _MaximizeDeltaSizeOffset.X = WindowsManager.Desktop.Explorer.ActualWidth - _MaximizeRestoreW;
                            _MaximizeDeltaSizeOffset.Y = WindowsManager.Desktop.Explorer.ActualHeight - _MaximizeRestoreH;
                            _MaximizeStartW = _MaximizeRestoreW;
                            _MaximizeStartH = _MaximizeRestoreH;

                        }

                        _MaximizeOrRestoreInProgress = true;
                        task.RepeatCount = 10;
                    }
                    else
                    {
                        _Translation.X = _MaximizeStartPoint.X + _MaximizeDeltaOffset.X * (10d - (double)task.RepeatCount) / 10d;
                        _Translation.Y = _MaximizeStartPoint.Y + _MaximizeDeltaOffset.Y * (10d - (double)task.RepeatCount) / 10d;

                        Width = _MaximizeStartW + _MaximizeDeltaSizeOffset.X * (10d - (double)task.RepeatCount) / 10d;
                        Height = _MaximizeStartH + _MaximizeDeltaSizeOffset.Y * (10d - (double)task.RepeatCount) / 10d;

                        if (task.RepeatCount == 0)
                        {
                            _MaximizeOrRestoreInProgress = false;
                            IsMaximized = !IsMaximized;
                            _WindowAnimationTask = null;
                        }
                    }
                }));
            }
            else // (... call back when the animation completes ...)
                _WindowAnimationTask.Removing = (task) => { MaximizeOrRestore(); };
        }

        // --------------------------------------------------------------------------------------------------

        public void Close()
        {
            var baseControl = BaseControl;

            if (VisualTree.IsInVisualTree(baseControl))
            {
                if (HideOnClose)
                    Hide();
                else
                {
                    if (Closing != null)
                        Closing(baseControl, new RoutedEventArgs());

                    _Uninitialize();

                    VisualTree.RemoveElement(baseControl);

                    Name = ""; // (unregisters the window)
                }
            }
        }

        // --------------------------------------------------------------------------------------------------

        public void Show(Panel desiredParentPanel)
        {
            if (desiredParentPanel == null) desiredParentPanel = WindowsManager.RootPanel;

            var baseControl = BaseControl;
            var baseControlPanel = VisualTreeHelper.GetParent(BaseControl) as Panel;

            _Initialize(); // (just in case the window was previously closed)

            // ... make sure the base object is visible ...

            if (baseControl.Visibility == Visibility.Collapsed)
            {
                baseControl.Visibility = Visibility.Visible;
                if (Showing != null)
                    Showing(baseControl, new RoutedEventArgs());
            }

            if (desiredParentPanel != null && desiredParentPanel != baseControlPanel)
            {
                // ... remove from existing parent first, if any ...

                if (baseControlPanel != null)
                    VisualTree.RemoveElement(baseControl);

                // ... add to new parent ...

                desiredParentPanel.Children.Add(baseControl);

                _lastPanelOwner = desiredParentPanel;

                // ... window is being shown now, so enable the timeout timer for the first time ...

                if (_TimeoutTask != null)
                    _TimeoutTask.Paused = false;
            }
        }
        public void Show() { Show(_lastPanelOwner); }

        // --------------------------------------------------------------------------------------------------

        public void Hide()
        {
            if (_WindowAnimationTask == null)
            {
                var baseControl = BaseControl;
                if (baseControl.Visibility == Visibility.Visible)
                {
                    _SetNewWindowAnimationTask(AddTask(1, (TimedTask task, object data) =>
                    {
                        if (Opacity > 0d)
                        {
                            task.RepeatCount = 1;
                            if (baseControl.Opacity < 0.1d) baseControl.Opacity = 0d; else baseControl.Opacity -= 0.1d;
                        }
                        else
                        {
                            baseControl.Visibility = Visibility.Collapsed;
                            baseControl.Opacity = 1d;
                            if (Hiding != null)
                                Hiding(baseControl, new RoutedEventArgs());
                            _WindowAnimationTask = null;
                        }
                    }));
                }
            }
            else
                _WindowAnimationTask.Removing = (task) => { Hide(); };
        }

        // --------------------------------------------------------------------------------------------------

        void _SetNewWindowAnimationTask(TimedTask task)
        {
            if (_WindowAnimationTask != null)
                RemoveTask(_WindowAnimationTask);
            _WindowAnimationTask = task;
        }

        // --------------------------------------------------------------------------------------------------

        public void ToggleVisibility() { if (IsVisible) Hide(); else Show(); }

        // --------------------------------------------------------------------------------------------------

        public bool IsVisible { get { return VisualTree.IsVisible(this); } }

        // --------------------------------------------------------------------------------------------------

        FrameworkElement _MovedToFrontElementOrder; // The element that was moved to the front for unload/load events.

        public void MoveToFront()
        {
            var baseControl = BaseControl;
            var baseControlPanel = VisualTreeHelper.GetParent(BaseControl) as Panel;

            if (baseControlPanel != null)
            {
                if (baseControlPanel is Canvas)
                {
                    // ... get the highest z position ...
                    int z, highZ = 0;
                    foreach (FrameworkElement element in baseControlPanel.Children)
                        if ((z = Canvas.GetZIndex(element)) > highZ) highZ = z;
                    // ... move everything down by one ...
                    foreach (FrameworkElement element in baseControlPanel.Children)
                        Canvas.SetZIndex(element, Canvas.GetZIndex(element) - 1);
                    // ... move this window to the top ...
                    Canvas.SetZIndex(this, highZ);
                }
                else // (move element to the end of the children list [for non-canvas controls])
                {
                    _MovedToFrontElementOrder = baseControl; // (prevent some events from working)
                    VisualTree.RemoveElement(baseControl);
                    baseControlPanel.Children.Add(baseControl);
                }
            }
        }

        // --------------------------------------------------------------------------------------------------
    }

    // ######################################################################################################
    #region Bindable Properties

    public partial class Window
    {
        // --------------------------------------------------------------------------------------------------

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Content.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(Window), new PropertyMetadata(""));

        // --------------------------------------------------------------------------------------------------

        public WindowButtons Buttons
        {
            get { return (WindowButtons)GetValue(ButtonsProperty); }
            set { SetValue(ButtonsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Buttons.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ButtonsProperty =
            DependencyProperty.Register("Buttons", typeof(WindowButtons), typeof(Window), new PropertyMetadata(WindowButtons.Ok, OnButtonsChanged));

        static void OnButtonsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Window)d).OnButtonsChanged();
        }

        // --------------------------------------------------------------------------------------------------

        public TitleBarButtons TitleBarButtons
        {
            get { return (TitleBarButtons)GetValue(TitleBarButtonsProperty); }
            set { SetValue(TitleBarButtonsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Buttons.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TitleBarButtonsProperty =
            DependencyProperty.Register("TitleBarButtons", typeof(TitleBarButtons), typeof(Window), new PropertyMetadata(TitleBarButtons.Close, OnTitleBarButtonsChanged));

        static void OnTitleBarButtonsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Window)d).OnButtonsChanged();
        }

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// Specified the base control for the window. Upon closing, the base control is removed instead of the window itself.
        /// </summary>
        public string BaseControlName
        {
            get { return (string)GetValue(BaseControlNameProperty); }
            set { SetValue(BaseControlNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Content.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BaseControlNameProperty =
            DependencyProperty.Register("BaseControlName", typeof(string), typeof(Window), new PropertyMetadata(""));

        // --------------------------------------------------------------------------------------------------

        public Visibility WindowVisibility
        {
            get { return (Visibility)GetValue(WindowVisibilityProperty); }
            set { SetValue(WindowVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Visible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WindowVisibilityProperty =
            DependencyProperty.Register("WindowVisibility", typeof(Visibility), typeof(Window), new PropertyMetadata(Visibility.Visible, OnWindowVisibilityChanged));

        static void OnWindowVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Window win = (Window)d;
            win._UpdateWindowVisibility();
        }

        void _UpdateWindowVisibility()
        {
            if (_initialized)
                if (WindowVisibility == Visibility.Visible) // (Note: Use queuing in case this is called in the constructor, or if visibility toggles in fast succession)
                    Dispatching.QueueDispatch(Dispatching.Priority.Low, new Action(Show), "Show", this);
                else
                    Dispatching.QueueDispatch(Dispatching.Priority.Low, new Action(Hide), "Hide", this);
        }

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// Whether or not the close button at the top right of the window is visible.
        /// </summary>
        public bool CloseButtonEnabled
        {
            get { return (bool)GetValue(CloseButtonEnabledProperty); }
            set { SetValue(CloseButtonEnabledProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CanClose.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CloseButtonEnabledProperty =
            DependencyProperty.Register("CloseButtonEnabled", typeof(bool), typeof(Window), new PropertyMetadata(true, OnCloseButtonEnabledChanged));

        static void OnCloseButtonEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Window win = (Window)d;
            win._UpdateCloseButtonVisibility();
        }

        void _UpdateCloseButtonVisibility()
        {
            if (_initialized)
                if (CloseButtonEnabled)
                    WindowTitleBar.TitleBarButtons.CloseButtonVisibility = Visibility.Visible;
                else
                    WindowTitleBar.TitleBarButtons.CloseButtonVisibility = Visibility.Collapsed;
        }

        // --------------------------------------------------------------------------------------------------
    }

    #endregion

    // ######################################################################################################

    public enum WindowButtonType { None, Yes, No, Ok, Cancel, Minimize, MaximizeOrRestore, Close }
    public enum WindowButtons { None, Ok, OkCancel, YesNo, YesNoCancel }
    public enum TitleBarButtons { None = 0, Minimize = 1, MaximizeOrRestore = 2, Close = 4, All = 7 }

    // ######################################################################################################

    public class WindowEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// The button that was pressed (including title bar buttons).
        /// </summary>
        public WindowButtonType ButtonPressed { get; internal set; }

        /// <summary>
        /// The origin of the event.
        /// </summary>
        new public object OriginalSource { get; protected set; }

        /// <summary>
        /// Set to 'true' to cancel the button press.
        /// </summary>
        public bool Cancelled = false;

        public WindowEventArgs() { }
        public WindowEventArgs(object source, WindowButtonType button)
        {
            OriginalSource = source;
            ButtonPressed = button;
        }
    }

    // ######################################################################################################

    /// <summary>
    /// Represents a handler signature for button press events.
    /// </summary>
    /// <param name="sender">The window the generated the button event.</param>
    /// <param name="e">Contains a reference to the button object that triggered the event, and the button type.</param>
    public delegate void WindowEventHandler(Window window, WindowEventArgs e);

    // ######################################################################################################
}
