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

namespace Common.XAML.Windows
{
    public partial class MinimizeButton : UserControl
    {
        // --------------------------------------------------------------------------------------------------

        public event RoutedEventHandler Click = null;
        
        // --------------------------------------------------------------------------------------------------

        public MinimizeButton()
        {
            InitializeComponent();
            
            btnMinimize.Click += Button_Click;
        }

        // --------------------------------------------------------------------------------------------------

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Click != null)
                Click(sender, e);
        }

        // --------------------------------------------------------------------------------------------------

    }

    // ######################################################################################################
    #region Bindable Properties

    public partial class MinimizeButton
    {
        // --------------------------------------------------------------------------------------------------
        // --------------------------------------------------------------------------------------------------
    }

    #endregion
}
