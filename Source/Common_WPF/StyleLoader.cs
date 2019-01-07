using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Resources;
using System.Windows.Shapes;
using System.Windows.Threading;
using Common.CollectionsAndLists;
using Common;

namespace Common.WPF.Attachables
{
    /// <summary>
    /// Provides methods for dynamically applying styles to controls.
    /// <para>&#160;</para>
    /// <para>
    /// The "StyleLoader.Events" attachable property allows attaching event methods to dynamically loaded XAML.
    /// Format: "[Local Event Name], [ancestor handler name], (and repeat both...)".
    /// If local event name is "*", then Proxy.Data is expected to be referencing another handler (can be used with special attachables, such as the entity data loader "EntityData.Name").
    /// See "<seealso cref="StyleLoader.EventsMethodSource" />" for more information.
    /// </para>
    /// </summary>
    public class StyleLoader : DependencyObject
    {
        /// <summary>
        /// Sets the default folder under which the controls are placed in the resource.
        /// This sub-path usually resembles the same layout as the folders in the project itself (such as "Controls/").
        /// Default is project root ("").
        /// </summary>
        public static string ControlSubFolder
        {
            get { return StyleLoader.fControlSubFolder; }
            set { StyleLoader.fControlSubFolder = value; }
        }
        private static string fControlSubFolder = "";

        public const string BEGIN_RESOURCE_TAG = "<!--BEGIN_RESOURCE-->";
        public const string END_RESOURCE_TAG = "<!--END_RESOURCE-->";

        /// <summary>
        /// <para>Loads and parses a XAML resource, and then returns the root object.
        /// The XAML resource is expected to be a .XAML file with valid XAML contents.
        /// An exception is thrown if the resource cannot be found.</para>
        /// <remarks>To load only a section of the resource file, instead of the root object, simply wrap the root
        /// element within "&lt;!--BEGIN_RESOURCE-->" and "&lt;!--END_RESOURCE-->" comment markers.
        /// This makes it possible to use the Visual Studio XAML previewer for custom controls that are
        /// derived from 'Control' - in which, normally, this is not supported.</remarks>
        /// </summary>
        /// <param name="filename">File name, such as "generic.xaml".</param>
        /// <param name="path">A sub-folder the resource is located in, if any.</param>
        /// <param name="assembly">The assembly name the resource is located in (just the name part only).</param>
        /// <returns>The loaded resource.</returns>
        public static object LoadResource(string filename, string path = "", string assembly = "")
        {
            try
            {
                object resource = WPFUtilities.LoadXAML<object>(filename, path, assembly, BEGIN_RESOURCE_TAG, END_RESOURCE_TAG);
                // (Note: LoadResource() may be called again during this process)

                return resource;
            }
            catch (Exception ex)
            {
                throw new Exception("StyleLoader: Error loading/parsing resource (" + filename + "," + path + "," + assembly + ").", ex);
            }
        }

        /// <summary>
        /// <para>Loads a resource based on a control, type name, or file name.</para>
        /// <para>The resource root object determines how the style is obtained:</para>
        /// <para>* If DictionaryResource, then the control type, or a given type name, is expected to be the style key.</para>
        /// <para>* If FrameworkElement, then the style is taken from the element's style property.</para>
        /// <para>* If Style, then the style object is simply returned as is.</para>
        /// </summary>
        /// <param name="source">
        /// <para>A control object, object type name, or file name.</para>
        /// <para>* If a FrameworkElement is given, the file name is taken from the derived type name (with .xaml added). If the XAML contains a DictionaryResource, the element derived type name is expected to be the style key.</para>
        /// <para>* If a file name is given (ends with .xaml), the name part (without the extension) is expected to be the type name (note: case sensitive).</para>
        /// <para>* If a string is given, but it is not a file name (doesn't end with .xaml), it is treated as a control type name.</para>
        /// </param>
        /// <param name="directory">A sub-folder that the resource is located in, if any.</param>
        /// <param name="filename">Resource filename override: If 'source' is a control or type name, then this is used as the resource file name instead.</param>
        /// <param name="assembly">The assembly that contains the resource file, or "" to default to the current executing assembly.</param>
        /// <returns></returns>
        public static Style LoadStyle(Type controlType, string directory = "", string filename = "", string assembly = "")
        {
            if (controlType == null) throw new Exception("StyleLoader.LoadStyle: 'controlType' is null.");

            // ... test if source object is a control, otherwise assume the source is a string 
            // that is the name of the resource file to load ...

            string typeName = controlType.Name;
            if (typeName.ToLower().EndsWith(".xaml"))
                typeName = System.IO.Path.GetFileNameWithoutExtension(typeName);
            string styleKey = typeName;

            if (filename == "")
                filename = typeName + ".xaml";

            try
            {
                object obj = LoadResource(filename, directory, assembly);
                Style style = null;

                if (obj == null)
                    throw new Exception("StyleLoader: Resource '" + filename + "' is empty."); // TODO: Confirm if an empty file really does produce a null return instead of an exception.

                if (obj is ResourceDictionary)
                {
                    ResourceDictionary dic = (ResourceDictionary)obj;
                    if (!dic.Contains(styleKey))
                        throw new Exception("No style with key '" + styleKey + "' was found.");
                    style = (Style)dic[styleKey];
                }
                else if (obj is FrameworkElement)
                {
                    style = (obj as FrameworkElement).Style;
                    if (style == null) throw new Exception("StyleLoader: Control object in '" + filename + "' does not have a style defined.");
                }
                else if (obj is Style)
                    style = obj as Style;
                else
                    throw new Exception("StyleLoader: Root object of resource '" + filename + "' is not supported as a valid source for control styles.");

                return style;
            }
            catch (Exception ex)
            {
                throw new Exception("StyleLoader: Error loading/parsing resource '" + filename + "'.", ex);
            }
        }

        /// <summary>
        /// Loads a resource bases on a control reference.
        /// </summary>
        /// <param name="control">
        /// The control to load the resource for.
        /// It is expected that the control resource is either a ResourceDictionary with a key that is the
        /// same as the control's type name, a 'Style' object, or a 'Style' object defined between
        /// "&lt;!--BEGIN_RESOURCE--->" and "&lt;!--END_RESOURCE-->" comment markers.
        /// </param>
        /// <param name="directory">A sub-folder that the resource is located in, if any.</param>
        /// <param name="filename">The resource file name, or "" to default to the control's type name.</param>
        /// <param name="assembly">The assembly that contains the resource file, or "" to default to the current executing assembly.</param>
        public static void LoadControlStyle(Control control, Type controlType, string directory = "", string filename = "", string assembly = "")
        {
            if (control == null) throw new Exception("StyleLoader: 'control' is null.");

            try
            {
                control.Style = LoadStyle(controlType, directory, filename, assembly);
            }
            catch (Exception ex)
            {
                throw new Exception("StyleLoader: Error getting resource for control '" + control.GetType().Name + "'.", ex);
            }
        }

        public static ControlTemplate LoadTemplate(Type controlType, string directory = "", string filename = "", string assembly = "")
        {
            if (controlType == null) throw new Exception("StyleLoader.LoadTemplate: 'controlType' is null.");

            string typeName = controlType.Name;
            if (typeName.ToLower().EndsWith(".xaml"))
                typeName = System.IO.Path.GetFileNameWithoutExtension(typeName);
            string styleKey = typeName;

            if (filename == "")
                filename = typeName + ".xaml";

            try
            {
                object obj = LoadResource(filename, directory, assembly);
                ControlTemplate template = null;

                if (obj is ControlTemplate)
                    template = obj as ControlTemplate;
                else
                    throw new Exception("StyleLoader: Root object of resource '" + filename + "' is not supported as a valid source for control styles.");

                return template;
            }
            catch (Exception ex)
            {
                throw new Exception("StyleLoader: Error loading/parsing resource '" + filename + "'.", ex);
            }
        }

        public static void LoadControlTemplate(Control control, Type controlType, string directory = "", string filename = "", string assembly = "")
        {
            if (control == null) throw new Exception("StyleLoader: 'control' is null.");

            try
            {
                control.Template = LoadTemplate(controlType, directory, filename, assembly);
            }
            catch (Exception ex)
            {
                throw new Exception("StyleLoader: Error getting resource for control '" + control.GetType().Name + "'.", ex);
            }
        }

        //------------------------------------------------------------------------------------------------

        /// <summary>
        /// Set this to the object instance that implements the methods that will be hooked into when XAML is dynamically parsed and instantiated.
        /// This needs to be set before any calls to methods that dynamically loads XAML.
        /// WARNING: The "StyleLoader.Events" attachable property should ONLY be used on XAML elements loaded dynamically (i.e. by calling XamlReader.Load(), StyleLoader.LoadResource(), etc.).
        /// </summary>

        public static readonly DependencyProperty EventsProperty = DependencyProperty.RegisterAttached(
            "Events",               //Name of the property
            typeof(string),            //Type of the property
            typeof(StyleLoader),  //Type of the provider of the registered attached property
            new PropertyMetadata("", OnEventsChanged));

        public static void SetEvents(DependencyObject obj, string value)
        {
            obj.SetValue(EventsProperty, value);
        }

        public static string GetEvents(DependencyObject obj)
        {
            return (string)obj.GetValue(EventsProperty);
        }

        //public static void HookupTemplateEvents(DependencyObject owner)
        //{
        //    _HookupTemplateEvents(owner, owner);
        //}
        //private static void _HookupTemplateEvents(DependencyObject owner, DependencyObject subject)
        //{
        //    if (owner == null) return;

        //    // ... cycle through child controls for any that specify StyleLoader.Events ...

        //    DependencyObject child = null;
        //    for (int i = 0; i < VisualTreeHelper.GetChildrenCount(subject); i++)
        //    {
        //        child = VisualTreeHelper.GetChild(subject, i);
        //        // if child isn't the start of another template, then hook up the events
        //        if (!GetIsEventHandlerSource(child))
        //            _HookupTemplateEvents(owner, child);
        //    }

        //    _HookupTemplateEvent(owner, subject);
        //}

        public static void OnEventsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // ... use reflection to find the owning object that implements the first handler ...

            if (!WPFUtilities.InDesignMode && e.NewValue.ToString() != "" && Application.Current.GetType() != typeof(Application))
            {
                if (d is FrameworkElement)
                {
                    LayoutUpdatedInstanceWrapper wrapper = new LayoutUpdatedInstanceWrapper(d);
                }
            }
        }

        class LayoutUpdatedInstanceWrapper
        {
            internal DependencyObject Subject = null;
            internal string EventType;
            internal EventArgs Args;
            DispatcherTimer _timeoutTimer = new DispatcherTimer();

            public LayoutUpdatedInstanceWrapper(DependencyObject subject)
            {
                Subject = subject;
                ((FrameworkElement)Subject).Loaded += Subject_Loaded;
                ((FrameworkElement)Subject).LayoutUpdated += Subject_LayoutUpdated; // (backup plan)
                _timeoutTimer.Interval = new TimeSpan(0, 0, 0, 0, 30000);
                _timeoutTimer.Tick += _Timeout;
                _timeoutTimer.Start();
            }

            void _Timeout(object sender, EventArgs e)
            {
                // (Note: Element must be in the visual tree, otherwise there is an obvious reason why parent cannot be located yet.)
                if (VisualTree.IsInVisualTree(Subject))
                {
                    _timeoutTimer.Stop();

                    Messaging.ShowMessage("Timeout: Unable to hookup events on element '" + Subject.GetType().Name + "' (" + GetEvents(Subject) + ").\r\n");
                }
            }

            void Subject_Loaded(object sender, RoutedEventArgs e)
            {
                EventType = "Loaded";
                Subject_LayoutUpdated(sender, e);
            }
            public void Subject_LayoutUpdated(object sender, EventArgs e)
            {
                Args = e;
                if (sender == null)
                    EventType = "LayoutUpdated";

                DependencyObject subject = Subject;
                string eventHookList = GetEvents(subject);

                if (eventHookList != "")
                {
                    // ... locate template parent as owner ...
                    DependencyObject owner = subject;
                    FrameworkElement element = subject as FrameworkElement;
                    for (; ; )
                    {
                        owner = element.Parent != null ? element.Parent : VisualTreeHelper.GetParent(element);
                        element = element.Parent as FrameworkElement;
                        if (owner == null)
                        { if (element != null) element.InvalidateArrange(); return; } // (nothing to do yet)
                        if (owner is IStyleLoaderEventSource && _ObjectContainsHandlers(owner, eventHookList))
                            break;
                        element = owner as FrameworkElement;
                    }

                    _timeoutTimer.Stop();

                    /// (the event hook is no longer needed)
                    ((FrameworkElement)subject).LayoutUpdated -= Subject_LayoutUpdated;
                    ((FrameworkElement)subject).Loaded -= Subject_Loaded;

                    _HookupTemplateEvent(owner, subject, this);
                }
            }
        }

        static bool _ObjectContainsHandlers(DependencyObject obj, string eventHookList)
        {
            string[] nameParts = eventHookList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (nameParts.Length == 0 || nameParts.Length % 2 == 1)
            {
                SetEvents(obj, "");
                throw new InvalidOperationException("StyleLoader.Events must be in the format \"Event-Name1, Method-Name1, EN2, MN2, ...\".");
            }

            string handlerName;
            for (int i = 0; i < nameParts.Length; i += 2)
            {
                handlerName = nameParts[i + 1];
                MethodInfo methodInfo = obj.GetType().GetMethod(handlerName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (methodInfo == null) return false;
            }

            return true;
        }

        static void _HookupTemplateEvent(DependencyObject owner, DependencyObject subject, LayoutUpdatedInstanceWrapper wrapper)
        {
            if (owner == null || subject == null) return;

            string eventInfoTokens = GetEvents(subject);

            if (eventInfoTokens != "")
            {
                SetEvents(subject, ""); // (make sure to only do this once)

                string[] nameParts = eventInfoTokens.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                string eventName, handlerName;

                for (int i = 0; i < nameParts.Length; i += 2)
                {
                    eventName = nameParts[i];
                    handlerName = nameParts[i + 1];

                    // ... assert that the event exists ...

                    EventInfo eventInfo = null;
                    if (eventName != "*")
                    {
                        eventInfo = subject.GetType().GetEvent(eventName);
                        if (eventInfo == null)
                            throw new NotImplementedException("The event '" + eventName + "' is not implemented in object type '" + subject.GetType().Name + "'.");
                    }

                    // ... assert that the handler exists ...

                    MethodInfo methodInfo = owner.GetType().GetMethod(handlerName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (methodInfo == null)
                        throw new NotImplementedException("The event handler method '" + handlerName + "' is not implemented in object type '" + owner.GetType().Name + "'.");

                    // ... attempt to add the method to the event, or to the proxy ...

                    try
                    {
                        if (eventName != "*")
                            Proxy.AttachHandler(owner, methodInfo, subject, eventInfo);
                        else
                            Proxy.SetData(subject, Delegate.CreateDelegate(typeof(Proxy.SimpleDataEventHandler), owner, methodInfo)); // (store method for another attachable property to use)

                        // ... if this is a "Loaded"/"LayoutUpdated" event handler then there will be a 
                        // conflict with the wrapper, so execute the event method now using the same
                        // arguments ... (note: if event is "Loaded", always execute the method)
                        if (eventName == wrapper.EventType)
                            methodInfo.Invoke(owner, new object[] { wrapper.Subject, wrapper.Args });
                        else if (eventName == "Loaded")
                            methodInfo.Invoke(owner, new object[] { wrapper.Subject, new RoutedEventArgs() });
                    }
                    catch (Exception ex)
                    { throw new MethodAccessException("The event handler method '" + handlerName + "' is not accessible in object type '" + owner.GetType().Name + "'. Try changing the accessor to either 'internal' or 'public'.", ex); }
                }
            }
        }

        //------------------------------------------------------------------------------------------------
        // Attachable property for storing user-defined values.
        public static readonly DependencyProperty DataProperty = DependencyProperty.RegisterAttached(
            "Data",               //Name of the property
            typeof(object),            //Type of the property
            typeof(StyleLoader),  //Type of the provider of the registered attached property
            new PropertyMetadata(OnDataChanged));

        public static void SetData(DependencyObject obj, object value)
        {
            obj.SetValue(DataProperty, value);
        }

        public static object GetData(DependencyObject obj)
        {
            return (object)obj.GetValue(DataProperty);
        }

        public static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //// ... this data source can only be replaced by another source (null not accepted) ...
            //// (prevents a parent DataContext from interferring with ContentPresenter)
            //if (e.NewValue == null && e.OldValue != null)
            //    SetData(d, e.OldValue);
            if (DataChanged != null)
                DataChanged(d, new RoutedEventArgs());
        }

        public static event RoutedEventHandler DataChanged;

        //------------------------------------------------------------------------------------------------

        //public static readonly DependencyProperty IsEventHandlerSourceProperty = DependencyProperty.RegisterAttached(
        //    "IsEventHandlerSource",               //Name of the property
        //    typeof(bool),            //Type of the property
        //    typeof(StyleLoader),  //Type of the provider of the registered attached property
        //    new PropertyMetadata(false));

        //public static void SetIsEventHandlerSource(DependencyObject obj, bool value)
        //{
        //    obj.SetValue(DataProperty, value);
        //}

        //public static bool GetIsEventHandlerSource(DependencyObject obj)
        //{
        //    try
        //    {
        //        return (bool)obj.GetValue(DataProperty);
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}

        //------------------------------------------------------------------------------------------------
    }

    /// <summary>
    /// If implemented, allows an element which uses the "StyleLoader.Events" attachable property to
    /// specify private and protected methods. Alternately, this interface can be skipped by simply
    /// setting the method to internal or private (depending on implementation).
    /// </summary>
    public interface ICreateDelegateProxy
    {
        Delegate CreateDelegate(Type type, MethodInfo method, bool throwOnBindFailure);

        //Example Implementation (for private/protected handlers, this needs to go into the *owning* class):
        //public Delegate CreateDelegate(Type type, MethodInfo method, bool throwOnBindFailure)
        //{
        //    return Delegate.CreateDelegate(type, this, method, throwOnBindFailure);
        //}
    }

    public interface IStyleLoaderEventSource { }


    // ===================================================================================================

    public class Proxy : DependencyObject
    {
        public static readonly DependencyProperty DataProperty = DependencyProperty.RegisterAttached(
               "Data",               //Name of the property
               typeof(object),            //Type of the property
               typeof(StyleLoader),  //Type of the provider of the registered attached property
               new PropertyMetadata(null));

        public static void SetData(DependencyObject obj, object value)
        {
            obj.SetValue(DataProperty, value);
        }

        public static object GetData(DependencyObject obj)
        {
            return (object)obj.GetValue(DataProperty);
        }

        public static Delegate GetDelegate(object handlerSource, Type handlerType, MethodInfo handlerMethodInfo)
        {
            Delegate del = null;
            if (handlerSource is ICreateDelegateProxy && !handlerMethodInfo.IsPublic)
            {   // (use an interface to create the delegate - supports private and protected methods)
                try { del = (handlerSource as ICreateDelegateProxy).CreateDelegate(handlerType, handlerMethodInfo, false); }
                catch { }
            }
            if (del == null)
            {   // (no interface, but try in case the handler's access type is internal or public)
                del = Delegate.CreateDelegate(handlerType, handlerSource, handlerMethodInfo);
            }
            return del;
        }

        public static void AttachHandler(object handlerSource, MethodInfo handlerMethodInfo, object eventSource, EventInfo eventInfo)
        {
            Delegate del = GetDelegate(handlerSource, eventInfo.EventHandlerType, handlerMethodInfo);
            eventInfo.AddEventHandler(eventSource, del);
        }

        public delegate void SimpleDataEventHandler(object sender, object data);
    }
}
