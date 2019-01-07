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
using Common.WPF.Scripting;
using Common.WPF.Scripting.Extensions;

namespace Common.WPF.Attachables
{
    /// <summary>
    /// Use the EntityData.Name attachable property to populate list controls with database values from server side entities.
    /// <para>
    /// The expected format is: table_name, order by, where_statement, call-back function
    /// </para>
    /// <para>Example: EntityData.Name="countries, name, name IS NOT null, OnCountryPopCompleted"</para>
    /// <para>(not all comma separated values are required, only the table name.</para>
    /// </summary>
    public class EntityData : DependencyObject //?? Used?
    {
        public static readonly DependencyProperty NameProperty = DependencyProperty.RegisterAttached(
            "Name",                  //Name of the property
            typeof(string),            //Type of the property
            typeof(EntityData),   //Type of the provider of the registered attached property
            new PropertyMetadata("", _OnNameChanged));

        static void _OnNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((string)e.NewValue != "")
            {
                string[] parts = ((string)e.NewValue).Split(',');
                if (parts.Length > 0)
                {
                    string tableName = parts[0].Trim();
                    if (tableName == "") throw new InvalidOperationException("EntityData: Table name missing for control '" + d.GetType().Name + "'.");
                    string orderByFields = parts.Length > 1 ? parts[1].Trim() : "EntityData.Name: ";
                    string where = parts.Length > 2 ? parts[2].Trim() : "";

                    // ... call server to get options ...

                    CodeBlockQueueScript cbqScript = new CodeBlockQueueScript();
                    cbqScript.AddAsyncCall_GetEntityData(true, tableName, orderByFields, where);
                    cbqScript += (Action<object, object>)((callResult, userStateData) =>
                    {
                        var result = cbqScript.GetEntityData_Result();
                        // ... update item source with returned records ...
                        PropertyInfo pInfo = d.GetType().GetProperty("ItemsSource");
                        if (pInfo != null)
                        {
                            pInfo.SetValue(d, result.Data, null);
                            ((FrameworkElement)d).InvalidateMeasure();
                            ((FrameworkElement)d).InvalidateArrange();

                            // ... call a proxy handler if specified ...
                            Proxy.SimpleDataEventHandler handlerDelegate = Proxy.GetData(d) as Proxy.SimpleDataEventHandler;
                            if (handlerDelegate != null)
                                handlerDelegate(d, result.Data);
                        }
                    });
                    cbqScript.Start();
                }
            }
        }

        public static void SetName(DependencyObject obj, string name)
        {
            obj.SetValue(NameProperty, name);
        }

        public static string GetName(DependencyObject obj)
        {
            try
            {
                return (string)obj.GetValue(NameProperty);
            }
            catch
            {
                return "";
            }
        }

        // -------------------------------------------------------------------------------------------
    }
}
