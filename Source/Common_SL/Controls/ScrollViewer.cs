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

namespace Common.XAML.Controls
{
    [System.Windows.Markup.ContentProperty("Content")]
    public partial class ScrollViewer : ContentControl
    {
        // --------------------------------------------------------------------------------------------------

        new public UIElement Content
        {
            get { return _ScrollViewer != null ? (UIElement)_ScrollViewer.Content : null; }
            set { if (base.Content == null) base.Content = value; else _ScrollViewer.Content = value; }
        }

        // --------------------------------------------------------------------------------------------------

        System.Windows.Controls.ScrollViewer _ScrollViewer;

        // --------------------------------------------------------------------------------------------------

        public ScrollViewer()
        {
            _ScrollViewer = new System.Windows.Controls.ScrollViewer();
            _ScrollViewer.HorizontalScrollBarVisibility = HorizontalScrollBarVisibility;
            _ScrollViewer.VerticalScrollBarVisibility = VerticalScrollBarVisibility;
            Content = _ScrollViewer;
            SizeChanged += _ScrollViewer_SizeChanged;
        }

        // ----------------------------------------------------------------------------------------------------

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _ScrollViewer.BorderBrush = this.BorderBrush;
            _ScrollViewer.BorderThickness = this.BorderThickness;
            _ScrollViewer.Background = this.Background;
        }

        // ----------------------------------------------------------------------------------------------------

        void _ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _ScrollViewer.MaxWidth = ActualWidth;
            _ScrollViewer.MaxHeight = ActualHeight;
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
            get { return _ScrollViewer == null ? 0d : (double)_ScrollViewer.ViewportWidth; }
        }

        public double ViewportHeight
        {
            get { return _ScrollViewer == null ? 0d : (double)_ScrollViewer.ViewportHeight; }
        }

        // . . .

        public double ExtentWidth
        {
            get { return _ScrollViewer == null ? 0d : (double)_ScrollViewer.ExtentWidth; }
        }

        public double ExtentHeight
        {
            get { return _ScrollViewer == null ? 0d : (double)_ScrollViewer.ExtentHeight; }
        }

        // . . .

        public double ScrollableWidth
        {
            get { return _ScrollViewer == null ? 0d : (double)_ScrollViewer.ScrollableWidth; }
        }

        public double ScrollableHeight
        {
            get { return _ScrollViewer == null ? 0d : (double)_ScrollViewer.ScrollableHeight; }
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
            DependencyProperty.Register("HorizontalScrollBarVisibility", typeof(ScrollBarVisibility), typeof(ScrollViewer),
            new PropertyMetadata(ScrollBarVisibility.Auto,
                (d, e) => { var sv = ((ScrollViewer)d)._ScrollViewer; if (sv != null) sv.HorizontalScrollBarVisibility = (ScrollBarVisibility)e.NewValue; }));

        // . . .

        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get { return (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty); }
            set { SetValue(VerticalScrollBarVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VerticalContentAlignment.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
            DependencyProperty.Register("VerticalScrollBarVisibility", typeof(ScrollBarVisibility), typeof(ScrollViewer),
            new PropertyMetadata(ScrollBarVisibility.Auto,
                (d, e) => { var sv = ((ScrollViewer)d)._ScrollViewer; if (sv != null) sv.VerticalScrollBarVisibility = (ScrollBarVisibility)e.NewValue; }));

        // --------------------------------------------------------------------------------------------------
    }

    #endregion
}
