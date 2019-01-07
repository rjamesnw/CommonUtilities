using System;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Common.XAML.Controls
{
    /// <summary>
    /// Used by the extension methods in the 'CustomValidationExtensionMethods' class to force custom validation error messages.
    /// </summary>
    public class CustomValidation
    {
        public bool ShowErrorMessage { get; set; }

        internal string _Message;

        public CustomValidation(string message) { this._Message = message; }

        public object ValidationError
        {
            get { return null; }
            set
            {
                if (ShowErrorMessage)
                    throw new ValidationException(_Message);
            }
        }

        internal CustomValidation() { }  
    }

    public static class CustomValidationExtensionMethods
    {
        /// <summary>
        /// Register custom manually-triggered validation with an element.
        /// </summary>
        public static void RegisterValidation(this FrameworkElement frameworkElement, string initialMessage)
        {
            if (initialMessage == null) throw new ArgumentNullException("initialMessage");

            CustomValidation customValidation = new CustomValidation(initialMessage);

            Binding binding = new Binding("ValidationError")
                {
                    Mode = BindingMode.TwoWay,
                    NotifyOnValidationError = true,
                    ValidatesOnExceptions = true,
                    Source = customValidation
                };

            frameworkElement.SetBinding(Control.TagProperty, binding);
        }

        /// <summary>
        /// After first using 'RegisterValidation()' to attach validation, this method shows either message.
        /// </summary>
        public static void RaiseValidationError(this FrameworkElement frameworkElement, string message)
        {
            BindingExpression b = frameworkElement.GetBindingExpression(Control.TagProperty);
            if (b != null)
            {
                if (message != null)
                    ((CustomValidation)b.DataItem)._Message = message;
                ((CustomValidation)b.DataItem).ShowErrorMessage = true;
                b.UpdateSource();
            }
        }

        /// <summary>
        /// After calling 'RegisterValidation()' to attach a validation, this method removes it.
        /// </summary>
        public static void ClearValidationError(this FrameworkElement frameworkElement)
        {
            BindingExpression b = frameworkElement.GetBindingExpression(Control.TagProperty);
            if (b != null)
            {
                ((CustomValidation)b.DataItem).ShowErrorMessage = false;
                b.UpdateSource();
            }
        }
    }
}
