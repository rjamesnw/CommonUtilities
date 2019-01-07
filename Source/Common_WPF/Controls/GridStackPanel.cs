using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Specialized;
using System.Windows.Markup;

namespace Common.XAML.Controls
{
    //[ContentProperty("Children")]
    public class GridStackPanel : Grid
    {
        // ------------------------------------------------------------------------------------------------------------

        public Orientation Orientation { get { return _Orientation; } set { _Orientation = value; _UpdateLayout(); } }
        Orientation _Orientation = Orientation.Vertical;

        public GridLength DefaultItemLength { get { return _DefaultItemLength; } set { _DefaultItemLength = value; _UpdateLayout(); } }
        GridLength _DefaultItemLength = new GridLength(1, GridUnitType.Star);

        // ------------------------------------------------------------------------------------------------------------

        new public ObservableCollection<UIElement> Children { get { return _Children; } }
        readonly ObservableCollection<UIElement> _Children = new ObservableCollection<UIElement>();

        // ------------------------------------------------------------------------------------------------------------

        public GridStackPanel()
        {
            Loaded += GridStackPanel_Loaded;
            SizeChanged += GridStackPanel_SizeChanged;
            Children.CollectionChanged += Children_CollectionChanged;
        }

        void Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                for (var i = e.OldItems.Count - 1; i >= 0; i--)
                    base.Children.RemoveAt(e.OldStartingIndex + i);

            if (e.NewItems != null)
                for (var i = 0; i < e.NewItems.Count; i++)
                    base.Children.Insert(e.NewStartingIndex + i, (UIElement)e.NewItems[i]);

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                base.Children.Clear();
                foreach (var item in Children)
                    base.Children.Add(item);
            }

            _UpdateLayout();
        }

        // ------------------------------------------------------------------------------------------------------------

        void GridStackPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _UpdateLayout();
        }

        // ------------------------------------------------------------------------------------------------------------

        void GridStackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            _UpdateLayout();
        }

        // ------------------------------------------------------------------------------------------------------------

        void _UpdateLayout()
        {
            RowDefinitions.Clear();
            ColumnDefinitions.Clear();

            FrameworkElement element;
            GridLength itemLength;
            int index = 0;

            foreach (var child in Children)
            {
                element = child as FrameworkElement;
                if (element != null)
                {
                    itemLength = GetItemLength(element);
                    if (itemLength.IsStar && itemLength.Value == 0d)
                        itemLength = DefaultItemLength;

                    if (Orientation == System.Windows.Controls.Orientation.Horizontal)
                    {
                        ColumnDefinitions.Add(new ColumnDefinition() { Width = itemLength });
                        element.ClearValue(Grid.RowSpanProperty);
                        element.ClearValue(Grid.ColumnSpanProperty);
                        Grid.SetRow(element, 0);
                        Grid.SetColumn(element, index++);
                    }
                    else
                    {
                        RowDefinitions.Add(new RowDefinition() { Height = itemLength });
                        element.ClearValue(Grid.ColumnSpanProperty);
                        element.ClearValue(Grid.RowSpanProperty);
                        Grid.SetColumn(element, 0);
                        Grid.SetRow(element, index++);
                    }
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        // Attachable Properties

        public static GridLength GetItemLength(DependencyObject obj)
        {
            return (GridLength)obj.GetValue(ItemLengthProperty);
        }
        public static void SetItemLength(DependencyObject obj, GridLength value)
        {
            obj.SetValue(ItemLengthProperty, value);
        }
        // Allows specifying a specific length of individual elements.
        public static readonly DependencyProperty ItemLengthProperty =
            DependencyProperty.RegisterAttached("ItemLength", typeof(GridLength), typeof(GridStackPanel), new PropertyMetadata(new GridLength(0, GridUnitType.Star), _OnItemLengthChanged));

        static void _OnItemLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement && ((FrameworkElement)d).Parent is GridStackPanel)
            {
                var gridStackPanel = (GridStackPanel)((FrameworkElement)d).Parent;
                gridStackPanel._UpdateLayout();
            }
        }

        // ------------------------------------------------------------------------------------------------------------
    }
}
