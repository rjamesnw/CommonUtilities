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
using System.Collections.Specialized;
using System.Collections;

namespace Common.XAML.Controls
{
    public partial class XListBox : ListBox, IEnumerable
    {
        // --------------------------------------------------------------------------------------------------

        public event NotifyCollectionChangedEventHandler ItemsChanged = null;

        List<DependencyObject> _items = new List<DependencyObject>();

        // --------------------------------------------------------------------------------------------------

        public XListBox()
        {
        }

        // --------------------------------------------------------------------------------------------------

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        // --------------------------------------------------------------------------------------------------

        protected override DependencyObject GetContainerForItemOverride()
        {
            DependencyObject dObj = base.GetContainerForItemOverride();
            _items.Add(dObj);
            return dObj;
        }

        // --------------------------------------------------------------------------------------------------

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            if (ItemsChanged != null)
                ItemsChanged(this, e);
        }

        // --------------------------------------------------------------------------------------------------

        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            if (_items.Contains((element as FrameworkElement).Tag as DependencyObject))
                _items.Remove((element as FrameworkElement).Tag as DependencyObject);
            base.ClearContainerForItemOverride(element, item);
        }

        // --------------------------------------------------------------------------------------------------

        public DependencyObject GetItemContentByIndex(int itemIndex)
        {
            return (_items[itemIndex]);
        }

        public int ItemContentCount { get { return _items.Count; } }

        // --------------------------------------------------------------------------------------------------

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            (element as FrameworkElement).Tag = item;
            base.PrepareContainerForItemOverride(element, item);
        }

        // --------------------------------------------------------------------------------------------------

        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            return _items.GetEnumerator(); 
        }

        #endregion
    }
    // ######################################################################################################

    #region Bindable Properties

    public partial class XListBox
    {
        // --------------------------------------------------------------------------------------------------
        // --------------------------------------------------------------------------------------------------
    }

    #endregion
}
