using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Common.XAML.Windows
{
    public partial class WindowTitleBarButtons : UserControl
    {
        // --------------------------------------------------------------------------------------------------

        public event RoutedEventHandler Minimize = null;
        public event RoutedEventHandler MaximizeOrRestore = null;
        public event RoutedEventHandler Close = null;

        // TODO: Make these bindable.
        public Visibility MinimizeButtonVisibility { get { return btnMinimizeButton.Visibility; } set { btnMinimizeButton.Visibility = value; } }
        public Visibility MaximizeButtonVisibility { get { return btnMaximizeButton.Visibility; } set { btnMaximizeButton.Visibility = value; } }
        public Visibility CloseButtonVisibility { get { return btnCloseButton.Visibility; } set { btnCloseButton.Visibility = value; } }

        // --------------------------------------------------------------------------------------------------

        public WindowTitleBarButtons()
        {
            InitializeComponent();

            btnMinimizeButton.Click += MinimizeButton_Click;
            btnMaximizeButton.Click += MaximizeButton_Click;
            btnCloseButton.Click += CloseButton_Click;
        }

        // --------------------------------------------------------------------------------------------------

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (Minimize != null)
                Minimize(sender, e);
        }

        // --------------------------------------------------------------------------------------------------

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (MaximizeOrRestore != null)
                MaximizeOrRestore(sender, e);
        }

        // --------------------------------------------------------------------------------------------------

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (Close != null)
                Close(sender, e);
        }

        // --------------------------------------------------------------------------------------------------
    }

    // ######################################################################################################
    #region Bindable Properties

    public partial class WindowTitleBarButtons
    {
        // --------------------------------------------------------------------------------------------------
        // --------------------------------------------------------------------------------------------------
    }

    #endregion
}
