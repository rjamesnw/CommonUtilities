using System;
using System.Net;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Reflection;
using System.Windows.Data;

namespace Common.Silverlight.Attachables
{
    /// <summary>
    /// Sets a two-way data binding between a child control and its template parent.
    /// </summary>
    public class TemplateBind : DependencyObject
    {
        //------------------------------------------------------------------------------------------------

        public static readonly DependencyProperty PropertiesProperty = DependencyProperty.RegisterAttached(
            "Properties",               //Name of the property
            typeof(string),            //Type of the property
            typeof(TemplateBind),  //Type of the provider of the registered attached property
            new PropertyMetadata("", _OnPropertyChanged));

        public static void SetProperties(DependencyObject obj, string value)
        {
            obj.SetValue(PropertiesProperty, value);
        }

        public static string GetProperties(DependencyObject obj)
        {
            try
            {
                return (string)obj.GetValue(PropertiesProperty);
            }
            catch
            {
                return "";
            }
        }
        //------------------------------------------------------------------------------------------------

        static void _OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue.ToString() != "" && Application.Current.GetType() != typeof(Application))
            {
                if (d is FrameworkElement)
                {
                    ((FrameworkElement)d).Loaded += new RoutedEventHandler(TemplateBind_Loaded);
                }
            }
        }

        static void TemplateBind_Loaded(object sender, RoutedEventArgs e)
        {
            // ... traverse up the parent hierarchy to find the template parent ...

            DependencyObject sourceElement = sender as DependencyObject;
            string properties = GetProperties(sourceElement);
            if (properties != "")
            {
                string[] propertyNames = properties.Split(',');

                if (propertyNames.Length == 0 || propertyNames.Length % 2 == 1)
                    throw new InvalidOperationException("TemplateBind: Two property name pairs required for binding (eg: \"LocalProp,TemplateProp,etc...\").");

                // ... locate template parent as owner ...

                DependencyObject targetElement = sourceElement;
                FrameworkElement element = sourceElement as FrameworkElement;
                for (; ; )
                {
                    targetElement = element.Parent != null ? element.Parent : VisualTreeHelper.GetParent(element);
                    element = element.Parent as FrameworkElement;
                    if (targetElement == null)
                        return; // (nothing to do, no template parent found)
                    if (element == null)
                        break;
                    element = targetElement as FrameworkElement;
                }

                for (int i = 0; i < propertyNames.Length; i += 2)
                {
                    // ... locate property on attached object ...

                    PropertyInfo sourceProp = sourceElement.GetType().GetProperty(propertyNames[i]);
                    if (sourceProp == null)
                        throw new MissingMemberException("TemplateBind: '" + sourceElement.GetType().Name + "' does not have a public property named '" + propertyNames[i] + "'.");

                    // ... locate bindable property on parent ...

                    PropertyInfo targetProp = targetElement.GetType().GetProperty(propertyNames[i + 1]);
                    if (targetProp == null)
                        throw new MissingMemberException("TemplateBind: Template target type '" + sourceElement.GetType().Name + "' does not have a public property named '" + propertyNames[i + 1] + "'.");

                    // ... attempt to bind the properties ...

                    CreateRelayBinding(sourceElement, sourceProp, targetElement, targetProp);
                }
            }
        }

        //------------------------------------------------------------------------------------------------

        public class _ValueProxy : INotifyPropertyChanged
        {
            private object _value;

            public object Value
            {
                get { return _value; }
                set
                {
                    _value = value;
                    OnPropertyChanged("Value");
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }

        /// <summary>
        /// Creates a relay binding between the two given elements using the properties and converters
        /// detailed in the supplied bindingProperties.
        /// </summary>
        public static void CreateRelayBinding(DependencyObject sourceElement, PropertyInfo sourceProp, DependencyObject targetElement, PropertyInfo targetProp)
        {
            string sourcePropertyName = sourceProp.Name + "Property";
            string targetPropertyName = targetProp.Name + "Property";

            // find the source dependency property
            FieldInfo sourceDependencyPropertyField = sourceElement.GetType().GetField(sourcePropertyName, BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            DependencyProperty sourceDependencyProperty = sourceDependencyPropertyField.GetValue(null) as DependencyProperty;

            // find the target dependency property
            FieldInfo targetDependencyPropertyField = targetElement.GetType().GetField(targetPropertyName, BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            DependencyProperty targetDependencyProperty = targetDependencyPropertyField.GetValue(null) as DependencyProperty;

            _ValueProxy valueProxy = new _ValueProxy();

            object tarVal = targetProp.GetValue(targetElement, null);
            object srcVal = sourceProp.GetValue(sourceElement, null);

            if (tarVal != null && (!(tarVal is string) || (string)tarVal != ""))
                valueProxy.Value = tarVal;
            else
                valueProxy.Value = srcVal;

            // ... create the binding for our source element to the proxy object ...

            Binding sourceToProxy = new Binding();
            sourceToProxy.Source = valueProxy;
            sourceToProxy.Path = new PropertyPath("Value");
            sourceToProxy.Mode = BindingMode.TwoWay;

            // ... create the binding for our target element to the proxy object ...

            Binding targetToProxy = new Binding();
            targetToProxy.Source = valueProxy;
            targetToProxy.Path = new PropertyPath("Value");
            targetToProxy.Mode = BindingMode.TwoWay;

            // ... set bindings ...

            ((FrameworkElement)sourceElement).SetBinding(sourceDependencyProperty, sourceToProxy);
            ((FrameworkElement)targetElement).SetBinding(targetDependencyProperty, targetToProxy);
        }

        //------------------------------------------------------------------------------------------------
    }
}
