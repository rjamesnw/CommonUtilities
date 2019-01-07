using System;
using System.Net;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Reflection;
using System.Windows.Data;

namespace Common.XAML.Attachables
{
    public class BindableTooltip : DependencyObject
    {
        public static DependencyProperty ToolTipProperty = DependencyProperty.RegisterAttached(
                "ToolTip",
                typeof(object),
                typeof(BindableTooltip),
                new PropertyMetadata(_OnTooltipChanged));

        public static void SetToolTip(DependencyObject d, object value)
        {
            d.SetValue(ToolTipProperty, value);
        }

        public static object GetToolTip(DependencyObject d)
        {
            return d.GetValue(ToolTipProperty);
        }

        static void _OnTooltipChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is FrameworkElement)
            {
                FrameworkElement element = sender as FrameworkElement;
                element.Loaded += Element_Loaded;
            }

        }

        static void Element_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement)
            {
                FrameworkElement element = sender as FrameworkElement;
                element.Loaded -= Element_Loaded;
                try
                {
                    FrameworkElement tooltip = GetToolTip(element) as FrameworkElement;
                    tooltip.DataContext = element.DataContext;
                    ToolTipService.SetToolTip(element, tooltip);

                    new _LayoutUpdateProxy(element, tooltip);
                }
                catch { }
            }
        }

        class _LayoutUpdateProxy
        {
            FrameworkElement _Element;
            FrameworkElement _Tooltip;

            public _LayoutUpdateProxy(FrameworkElement element, FrameworkElement tooltip)
            { _Element = element; _Tooltip = tooltip; _Element.LayoutUpdated += _Element_LayoutUpdated; }

            void _Element_LayoutUpdated(object sender, EventArgs e)
            {
                _Tooltip.DataContext = _Element.DataContext;
            }
        }
    }
}
