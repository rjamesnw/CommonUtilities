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

namespace Common.Silverlight.Controls
{
    [System.Windows.Markup.ContentProperty("Content")]
    public partial class ScrollViewer : System.Windows.Controls.ScrollViewer
    {
        // --------------------------------------------------------------------------------------------------

        public ScrollViewer()
        {
            InitializeComponent();
            SizeChanged += _ScrollViewer_SizeChanged;
        }

        // ----------------------------------------------------------------------------------------------------

        void _ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ContentScrollViewer.MaxWidth = ActualWidth;
            ContentScrollViewer.MaxHeight = ActualHeight;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            base.MeasureOverride(availableSize);
            return new Size(0, 0); // (allow taking whatever size in parent is available!)
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return base.ArrangeOverride(finalSize);
        }

        // ----------------------------------------------------------------------------------------------------

        public double ViewportWidth
        {
            get { return (double)ContentScrollViewer.ViewportWidth; }
        }

        public double ViewportHeight
        {
            get { return (double)ContentScrollViewer.ViewportHeight; }
        }

        // . . .

        public double ExtentWidth
        {
            get { return (double)ContentScrollViewer.ExtentWidth; }
        }

        public double ExtentHeight
        {
            get { return (double)ContentScrollViewer.ExtentHeight; }
        }

        // . . .

        public double ScrollableWidth
        {
            get { return (double)ContentScrollViewer.ScrollableWidth; }
        }

        public double ScrollableHeight
        {
            get { return (double)ContentScrollViewer.ScrollableHeight; }
        }

        // ----------------------------------------------------------------------------------------------------
    }

    // ######################################################################################################
    #region Bindable Properties

    public partial class ScrollViewer
    {
        // --------------------------------------------------------------------------------------------------

        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get { return (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty); }
            set { SetValue(HorizontalScrollBarVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HorizontalContentAlignment.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty =
            DependencyProperty.Register("HorizontalScrollBarVisibility", typeof(ScrollBarVisibility), typeof(ScrollViewer), new PropertyMetadata(ScrollBarVisibility.Auto));

        // . . .

        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get { return (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty); }
            set { SetValue(VerticalScrollBarVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VerticalContentAlignment.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
            DependencyProperty.Register("VerticalScrollBarVisibility", typeof(ScrollBarVisibility), typeof(ScrollViewer), new PropertyMetadata(ScrollBarVisibility.Auto));

        // --------------------------------------------------------------------------------------------------
    }

    #endregion
}
