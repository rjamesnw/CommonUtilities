using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.XAML
{
    /// <summary>
    /// Used in conjunction with the 'DependencyPropertyGenerator.tt' T4 template script to auto generate required
    /// dependency properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class , AllowMultiple = true)]
    public class DependencyPropertyAttribute : Attribute
    {
        public DependencyPropertyAttribute(string name, Type type, object defaultValue, string summary)
        {
            Name = name;
            Type = type;
            DefaultValue = defaultValue;
            Summary = summary;
        }

        public string Name;
        public Type Type;
        public object DefaultValue;
        public string Summary;
    }
}
