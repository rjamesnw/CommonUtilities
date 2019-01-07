using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Windows.Data;

namespace Common.XAML.Windows
{
    public partial class WindowTitleBar : UserControl
    {
        // --------------------------------------------------------------------------------------------------

        public event MouseButtonEventHandler Click = null;

        public TitleBarButtons TitleBarButtonMode
        {
            get { return _TitleBarButtonMode; }
            set
            {
                _TitleBarButtonMode = value;

                if ((_TitleBarButtonMode & TitleBarButtons.Minimize) != 0)
                    WinTitleBarButtons.MinimizeButtonVisibility = Visibility.Visible;
                else
                    WinTitleBarButtons.MinimizeButtonVisibility = Visibility.Collapsed;

                if ((_TitleBarButtonMode & TitleBarButtons.MaximizeOrRestore) != 0)
                    WinTitleBarButtons.MaximizeButtonVisibility = Visibility.Visible;
                else
                    WinTitleBarButtons.MaximizeButtonVisibility = Visibility.Collapsed;

                if ((_TitleBarButtonMode & TitleBarButtons.Close) != 0)
                    WinTitleBarButtons.CloseButtonVisibility = Visibility.Visible;
                else
                    WinTitleBarButtons.CloseButtonVisibility = Visibility.Collapsed;
            }
        }
        TitleBarButtons _TitleBarButtonMode;

        // --------------------------------------------------------------------------------------------------

        private bool fMouseWasDown = false;

        // --------------------------------------------------------------------------------------------------

        public WindowTitleBar()
        {
            InitializeComponent();

            this.SetSimpleBinding(WinTitleBarButtons, UIElement.VisibilityProperty, "ButtonVisibility");
            this.SetSimpleBinding(TitleText, TextBlock.TextProperty, "Title");

            TitleBar.MouseLeftButtonDown += TitleBar_MouseLeftButtonDown;
            TitleBar.MouseLeftButtonUp += TitleBar_MouseLeftButtonUp;
        }

        // --------------------------------------------------------------------------------------------------

        void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ClickHighlight.Visibility = Visibility.Visible;
            fMouseWasDown = true;
        }

        // --------------------------------------------------------------------------------------------------

        void TitleBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ClickHighlight.Visibility = Visibility.Collapsed;
            if (fMouseWasDown && Click != null)
                Click(TitleBar, e);
            fMouseWasDown = false;
        }

        // --------------------------------------------------------------------------------------------------
    }

    // ######################################################################################################
    #region Bindable Properties

    public partial class WindowTitleBar
    {
        // --------------------------------------------------------------------------------------------------

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Caption.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(WindowTitleBar), new PropertyMetadata("", DoDependencyPropertyChanged));

        // --------------------------------------------------------------------------------------------------

        public Visibility ButtonVisibility
        {
            get { return (Visibility)GetValue(ButtonVisibilityProperty); }
            set { SetValue(ButtonVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ButtonVisibility.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ButtonVisibilityProperty =
            DependencyProperty.Register("ButtonVisibility", typeof(Visibility), typeof(WindowTitleBar), new PropertyMetadata(Visibility.Visible, DoDependencyPropertyChanged));

        // --------------------------------------------------------------------------------------------------
    }

    #endregion
}
