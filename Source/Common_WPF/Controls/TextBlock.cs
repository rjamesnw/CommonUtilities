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
using Common.XAML.Converters;

namespace Common.XAML.Controls
{
    [System.Windows.Markup.ContentProperty("Content")]
    public class TextBlock : ContentControl
    {
        // --------------------------------------------------------------------------------------------------

        new public UIElement Content { get { return base.Content as UIElement; } }

        // --------------------------------------------------------------------------------------------------

        public System.Windows.Controls.TextBlock InternalTextBlock { get { return _TextBlock; } }
        System.Windows.Controls.TextBlock _TextBlock;

        // --------------------------------------------------------------------------------------------------

        public TextBlock()
        {
            SizeChanged += _TextBox_SizeChanged;
        }

        public override void OnApplyTemplate()
        {
            _TextBlock = new System.Windows.Controls.TextBlock();
            _TextBlock.Text = Text;
            _TextBlock.TextWrapping = TextWrapping;
            _TextBlock.FontFamily = base.FontFamily;
            _TextBlock.FontSize = base.FontSize;
            _TextBlock.FontStretch = base.FontStretch;
            _TextBlock.FontStyle = base.FontStyle;
            _TextBlock.FontWeight = base.FontWeight;

            if (!(Foreground is SolidColorBrush) || ((SolidColorBrush)Foreground).Color != Colors.Black)
                _TextBlock.Foreground = Foreground;
            
            if (!(base.Foreground is SolidColorBrush) || ((SolidColorBrush)base.Foreground).Color != Colors.Black)
                _TextBlock.Foreground = base.Foreground;

            base.Content = _TextBlock;
        }

        // ----------------------------------------------------------------------------------------------------

        void _TextBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _TextBlock.MaxWidth = ActualWidth;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var size = base.MeasureOverride(availableSize);
            if (TextWrapping == TextWrapping.NoWrap)
                return size; // (allow taking whatever size in parent is available!)
            else
                return new Size(0, size.Height); // (allow taking whatever size in parent is available!)
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return base.ArrangeOverride(finalSize);
        }

        // ----------------------------------------------------------------------------------------------------

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(TextBlock), new PropertyMetadata("",
                (d, e) => { var tb = ((TextBlock)d)._TextBlock; if (tb != null) tb.Text = (string)e.NewValue; }));


        // ----------------------------------------------------------------------------------------------------

        public TextWrapping TextWrapping
        {
            get { return (TextWrapping)GetValue(TextWrappingProperty); }
            set { SetValue(TextWrappingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TextWrapping.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextWrappingProperty =
            DependencyProperty.Register("TextWrapping", typeof(TextWrapping), typeof(TextBlock), new PropertyMetadata(TextWrapping.NoWrap,
                (d, e) => { var tb = ((TextBlock)d)._TextBlock; if (tb != null) tb.TextWrapping = (TextWrapping)e.NewValue; }));

        // ----------------------------------------------------------------------------------------------------

        new public Brush Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TextWrapping.  This enables animation, styling, binding, etc...
        new public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register("Foreground", typeof(Brush), typeof(TextBlock), new PropertyMetadata(new SolidColorBrush(Colors.Black),
                (d, e) => { var tb = ((TextBlock)d)._TextBlock; if (tb != null) tb.Foreground = (Brush)e.NewValue ?? new SolidColorBrush(Colors.Black); }));

        // ----------------------------------------------------------------------------------------------------
    }
}
