using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Specialized;
using System.Collections;

namespace Common.XAML.Controls
{
    public delegate void AfterGenerateElementHandler(DataGridColumn column, FrameworkElement element, object dataItem, Binding binding);

    public class DataGridDateColumn : DataGridBoundColumn
    {
        // --------------------------------------------------------------------------------------------------

        public object Tag { get; set; }

        public event KeyEventHandler EditingElementKeyDown;
        public event KeyEventHandler EditingElementKeyUp;
        public event AfterGenerateElementHandler AfterGenerateElement;
        public event AfterGenerateElementHandler AfterGenerateEditingElement;

        // --------------------------------------------------------------------------------------------------

        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            TextBlock tb = new TextBlock();
            tb.Margin = new Thickness(4);
            tb.VerticalAlignment = VerticalAlignment.Center;

            Binding binding = WPFUtilities.CloneBinding((Binding)Binding);

            if (AfterGenerateElement != null)
                AfterGenerateElement(this, tb, dataItem, binding);

            if (binding != null)
                tb.SetBinding(TextBlock.TextProperty, binding);
            return tb;
        }

        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            DatePicker element = new DatePicker();
            element.DataContext = dataItem;
            element.KeyDown += EditingElement_KeyDown;
            element.KeyUp += EditingElement_KeyUp;

            Binding binding = WPFUtilities.CloneBinding((Binding)Binding);

            if (AfterGenerateEditingElement != null)
                AfterGenerateEditingElement(this, element, dataItem, binding);

            if (binding != null)
                element.SetBinding(DatePicker.TextProperty, binding);
            return element;
        }

        // --------------------------------------------------------------------------------------------------

        protected override object PrepareCellForEdit(FrameworkElement editingElement, RoutedEventArgs editingEventArgs)
        {
            DatePicker element = (DatePicker)editingElement;
            return element.SelectedDate;
        }

        protected override void CancelCellEdit(FrameworkElement editingElement, object uneditedValue)
        {
            DatePicker element = (DatePicker)editingElement;
            element.SelectedDate = (DateTime?)uneditedValue;
        }

        // --------------------------------------------------------------------------------------------------

        void EditingElement_KeyDown(object sender, KeyEventArgs e)
        {
            if (EditingElementKeyDown != null)
                EditingElementKeyDown(sender, e);
        }

        void EditingElement_KeyUp(object sender, KeyEventArgs e)
        {
            if (EditingElementKeyUp != null)
                EditingElementKeyUp(sender, e);
        }

        // --------------------------------------------------------------------------------------------------
    }

    // ######################################################################################################

    public class DataGridNumericColumn : DataGridBoundColumn
    {
        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// If false, the decimal character is not accepted.
        /// The default is 'true'.
        /// </summary>
        public bool IsFloat { get; set; }
        public bool CanBeNegative { get; set; }

        public int MaxLength { get { return _MaxLength; } set { if (value < 0) _MaxLength = 0; else _MaxLength = value; } }
        int _MaxLength = 0;

        public object Tag { get; set; }

        public event KeyEventHandler EditingElementKeyDown;
        public event KeyEventHandler EditingElementKeyUp;
        public event AfterGenerateElementHandler AfterGenerateElement;
        public event AfterGenerateElementHandler AfterGenerateEditingElement;

        // --------------------------------------------------------------------------------------------------

        public DataGridNumericColumn() { IsFloat = true; CanBeNegative = true; }

        // --------------------------------------------------------------------------------------------------

        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            TextBlock tb = new TextBlock();
            tb.Margin = new Thickness(4);
            tb.VerticalAlignment = VerticalAlignment.Center;

            //Binding binding = WPFUtilities.CloneBinding((Binding)Binding);
            Binding binding = new Binding();
            binding.Path = ((Binding)Binding).Path;
            binding.Source = ((Binding)Binding).Source;

            if (AfterGenerateElement != null)
                AfterGenerateElement(this, tb, dataItem, binding);

            if (binding != null)
                tb.SetBinding(TextBlock.TextProperty, binding);
            return tb;
        }

        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            TextBox textBox = new TextBox();
            textBox.DataContext = dataItem;
            textBox.MaxLength = MaxLength;
            textBox.KeyDown += EditingElement_KeyDown;
            textBox.KeyUp += EditingElement_KeyUp;

            Binding binding = WPFUtilities.CloneBinding((Binding)Binding);

            if (AfterGenerateEditingElement != null)
                AfterGenerateEditingElement(this, textBox, dataItem, binding);

            if (binding != null)
                textBox.SetBinding(TextBox.TextProperty, binding);
            return textBox;
        }

        // --------------------------------------------------------------------------------------------------

        protected override object PrepareCellForEdit(FrameworkElement editingElement, RoutedEventArgs editingEventArgs)
        {
            TextBox element = (TextBox)editingElement;
            return element.Text;
        }

        protected override void CancelCellEdit(FrameworkElement editingElement, object uneditedValue)
        {
            TextBox element = (TextBox)editingElement;
            element.Text = (string)uneditedValue;
        }

        // --------------------------------------------------------------------------------------------------

        void EditingElement_KeyDown(object sender, KeyEventArgs e)
        {
            // (filter for numeric entry keys only)
            if (e.Key == Key.Enter || e.Key == Key.Escape || e.Key == Key.Tab || CanBeNegative && e.Key == Key.Subtract || e.Key >= Key.D0 && e.Key <= Key.D9 || IsFloat && e.Key == Key.Decimal)
            {
                if (EditingElementKeyDown != null)
                    EditingElementKeyDown(sender, e);
            }
            else e.Handled = true;
        }

        void EditingElement_KeyUp(object sender, KeyEventArgs e)
        {
            // (filter for numeric entry keys only)
            if (e.Key == Key.Enter || e.Key == Key.Escape || e.Key == Key.Tab || CanBeNegative && e.Key == Key.Subtract || e.Key >= Key.D0 && e.Key <= Key.D9 || IsFloat && e.Key == Key.Decimal)
            {
                if (EditingElementKeyUp != null)
                    EditingElementKeyUp(sender, e);
            }
            else e.Handled = true;
        }

        // --------------------------------------------------------------------------------------------------
    }

    // ######################################################################################################

    public class DataGridComboBoxColumn : DataGridBoundColumn
    {
        // --------------------------------------------------------------------------------------------------

        public object Tag { get; set; }

        public event KeyEventHandler EditingElementKeyDown;
        public event KeyEventHandler EditingElementKeyUp;
        public event AfterGenerateElementHandler AfterGenerateElement;
        public event AfterGenerateElementHandler AfterGenerateEditingElement;

        // --------------------------------------------------------------------------------------------------

        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            TextBlock tb = new TextBlock();
            tb.Margin = new Thickness(4);
            tb.VerticalAlignment = VerticalAlignment.Center;

            Binding binding = WPFUtilities.CloneBinding((Binding)Binding);

            if (AfterGenerateElement != null)
                AfterGenerateElement(this, tb, dataItem, binding);

            if (binding != null)
                tb.SetBinding(TextBlock.TextProperty, binding);
            return tb;
        }

        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            ComboBox comboBox = new ComboBox();
            comboBox.DataContext = dataItem;
            comboBox.KeyDown += EditingElement_KeyDown;
            comboBox.KeyUp += EditingElement_KeyUp;

            Binding binding = WPFUtilities.CloneBinding((Binding)Binding);

            if (AfterGenerateEditingElement != null)
                AfterGenerateEditingElement(this, comboBox, dataItem, binding);

            if (binding != null)
                comboBox.SetBinding(ComboBox.SelectedItemProperty, binding);
            return comboBox;
        }

        // --------------------------------------------------------------------------------------------------

        protected override object PrepareCellForEdit(FrameworkElement editingElement, RoutedEventArgs editingEventArgs)
        {
            ComboBox element = (ComboBox)editingElement;
            return element.SelectedItem;
        }

        protected override void CancelCellEdit(FrameworkElement editingElement, object uneditedValue)
        {
            ComboBox element = (ComboBox)editingElement;
            element.SelectedItem = uneditedValue;
        }

        // --------------------------------------------------------------------------------------------------

        void EditingElement_KeyDown(object sender, KeyEventArgs e)
        {
            if (EditingElementKeyDown != null)
                EditingElementKeyDown(sender, e);
        }

        void EditingElement_KeyUp(object sender, KeyEventArgs e)
        {
            if (EditingElementKeyUp != null)
                EditingElementKeyUp(sender, e);
        }

        // --------------------------------------------------------------------------------------------------
    }

    // ######################################################################################################

    public class DataGridTextBoxColumn : DataGridBoundColumn
    {
        // --------------------------------------------------------------------------------------------------

        public object Tag { get; set; }

        public int MaxLength { get { return _MaxLength; } set { if (value < 0) _MaxLength = 0; else _MaxLength = value; } }
        int _MaxLength = 0;

        public event KeyEventHandler EditingElementKeyDown;
        public event KeyEventHandler EditingElementKeyUp;
        public event AfterGenerateElementHandler AfterGenerateElement;
        public event AfterGenerateElementHandler AfterGenerateEditingElement;

        // --------------------------------------------------------------------------------------------------

        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            TextBlock tb = new TextBlock();
            tb.Margin = new Thickness(4);
            tb.VerticalAlignment = VerticalAlignment.Center;

            Binding binding = WPFUtilities.CloneBinding((Binding)Binding);

            if (AfterGenerateElement != null)
                AfterGenerateElement(this, tb, dataItem, binding);

            if (binding != null)
                tb.SetBinding(TextBlock.TextProperty, binding);
            return tb;
        }

        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            TextBox textBox = new TextBox();
            textBox.DataContext = dataItem;
            textBox.MaxLength = MaxLength;
            textBox.KeyDown += EditingElement_KeyDown;
            textBox.KeyUp += EditingElement_KeyUp;

            Binding binding = WPFUtilities.CloneBinding((Binding)Binding);

            if (AfterGenerateEditingElement != null)
                AfterGenerateEditingElement(this, textBox, dataItem, binding);

            if (binding != null)
                textBox.SetBinding(TextBox.TextProperty, binding);
            return textBox;
        }

        // --------------------------------------------------------------------------------------------------

        protected override object PrepareCellForEdit(FrameworkElement editingElement, RoutedEventArgs editingEventArgs)
        {
            TextBox element = (TextBox)editingElement;
            return element.Text;
        }

        protected override void CancelCellEdit(FrameworkElement editingElement, object uneditedValue)
        {
            TextBox element = (TextBox)editingElement;
            element.Text = (string)uneditedValue;
        }

        // --------------------------------------------------------------------------------------------------

        void EditingElement_KeyDown(object sender, KeyEventArgs e)
        {
            if (EditingElementKeyDown != null)
                EditingElementKeyDown(sender, e);
        }

        void EditingElement_KeyUp(object sender, KeyEventArgs e)
        {
            if (EditingElementKeyUp != null)
                EditingElementKeyUp(sender, e);
        }

        // --------------------------------------------------------------------------------------------------
    }

    // ######################################################################################################
}
