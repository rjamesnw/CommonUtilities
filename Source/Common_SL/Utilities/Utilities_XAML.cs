// (C) 2013 Surreal Logic Systems
// This file expects the directive "SILVERLIGHT" for use with Silverlight applications.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;

#if SILVERLIGHT
using System.Windows.Browser;
#endif

namespace Common
{
    // =========================================================================================================================
    /// <summary>
    /// This class contains global (shared) utility methods used by most XAML-base applications.
    /// </summary>
    public partial class WPFUtilities
    {
        // ---------------------------------------------------------------------------------------------------------------------

        public static bool InDesignMode
        { get { return Application.Current == null || Application.Current.GetType() == typeof(Application); } }

        public static bool InDebugMode
        {
#if DEBUG
            get { return true; }
#else
            get { return false; }
#endif
        }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Clones a binding instance.
        /// </summary>
        public static Binding CloneBinding(Binding binding)
        {
            if (binding == null)
                return null; // (a clone of 'null' is the same - just 'null')
            Binding b = new Binding();
            b.BindsDirectlyToSource = binding.BindsDirectlyToSource;
            b.Converter = binding.Converter;
            b.ConverterCulture = binding.ConverterCulture;
            b.ConverterParameter = binding.ConverterParameter;
            if (b.ElementName != binding.ElementName)
                b.ElementName = binding.ElementName;
            b.Mode = binding.Mode;
            b.NotifyOnValidationError = binding.NotifyOnValidationError;
            b.Path = new PropertyPath(binding.Path.Path);
            if (b.RelativeSource != binding.RelativeSource)
                b.RelativeSource = binding.RelativeSource;
            b.Source = binding.Source;
            b.UpdateSourceTrigger = binding.UpdateSourceTrigger;
            b.ValidatesOnDataErrors = binding.ValidatesOnDataErrors;
            b.ValidatesOnExceptions = binding.ValidatesOnExceptions;
#if !(V2 || V3 || V3_5)
            b.ValidatesOnNotifyDataErrors = binding.ValidatesOnNotifyDataErrors;
#endif
            b.StringFormat = binding.StringFormat;
            b.FallbackValue = binding.FallbackValue;
            b.TargetNullValue = binding.TargetNullValue;
            return b;
        }

        // ---------------------------------------------------------------------------------------------------------------------

        public class BindingEvaluator<T> : FrameworkElement
        {
            static readonly DependencyProperty ValueProperty =
                DependencyProperty.Register("Value", typeof(T), typeof(BindingEvaluator<T>), new PropertyMetadata(DependencyProperty.UnsetValue));
            public T Value
            {
                get { SetBinding(BindingEvaluator<T>.ValueProperty, _binding); return (T)GetValue(ValueProperty); }
                set { SetBinding(BindingEvaluator<T>.ValueProperty, _binding); SetValue(ValueProperty, value); }
            }
            Binding _binding;
            public BindingEvaluator(Binding binding, object subject)
            {
                _binding = CloneBinding(binding);
                if (subject != null)
                    _binding.Source = subject;
            }
            public BindingEvaluator(Binding binding) : this(binding, null) { }
        }

        /// <summary>
        /// Returns a value according to the specified binding.
        /// Note: This does not work with element name binding, as the binding evaluator would need to be
        /// within the same container (i.e. with the same parent panel) as the subject element object.
        /// </summary>
        /// <param name="binding">The binding information used to retrieve the value.</param>
        /// <param name="subject">Object that contains the value of the binding path.</param>
        public static T GetBindedValue<T>(Binding binding, object subject)
        {
            BindingEvaluator<T> be = new BindingEvaluator<T>(binding, subject);
            return be.Value;
        }
        public static T GetBindedValue<T>(Binding binding) { return GetBindedValue<T>(binding, null); }

        /// <summary>
        /// Sets a value according to the specified binding.
        /// Note: This does not work with element name binding, as the binding evaluator would need to be
        /// within the same container (i.e. with the same parent panel) as the subject element object.
        /// </summary>
        /// <param name="binding">The binding information used to retrieve the value.</param>
        /// <param name="subject">Object that contains the data member of the binding path to be set.</param>
        /// <param name="value">Value to set.</param>
        public static void SetBindedValue<T>(Binding binding, object subject, T value)
        {
            BindingEvaluator<T> be = new BindingEvaluator<T>(binding, subject);
            be.Value = value;
        }
        public static void SetBindedValue<T>(Binding binding, T value) { SetBindedValue<T>(binding, null, value); }

        // ---------------------------------------------------------------------------------------------------------------------

        public static partial class Controls
        {
            // . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . .

            public static bool SelectListItem(DependencyObject listElement, string itemFieldName, string value)
            {
                object existingValue = "", ItemsSource = null, _value = null;
                if (Objects.GetPropertyOrFieldValue(listElement, "SelectedItem", out existingValue))
                {
                    Objects.GetPropertyOrFieldValue(listElement, "ItemsSource", out ItemsSource);
                    if (ItemsSource == null)
                        Objects.GetPropertyOrFieldValue(listElement, "Items", out ItemsSource);
                    if (ItemsSource is IEnumerable)
                    {
                        foreach (object item in (IEnumerable)ItemsSource)
                        {
                            // ... get value from item as either a field/property, or the "ToString()" result ...
                            if (string.IsNullOrEmpty(itemFieldName))
                                _value = item; // (select based on string representation)
                            else
                                if (!Objects.GetPropertyOrFieldValue(item, itemFieldName, out _value)) // (if data member found, select via that)
                                    continue;

                            // ... if the item value matches the requested value, flag selection as found
                            // (warning: don't update properties is value hasn't changed) ...
                            if (value == Utilities.ND(_value, ""))
                            {
                                if (item != existingValue)
                                    Objects.SetPropertyOrFieldValue(listElement, "SelectedItem", item);
                                return true;
                            }
                        }
                        Objects.SetPropertyOrFieldValue<object>(listElement, "SelectedItem", null);
                    }
                }
                return false;
            }

            // . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . .
        }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Loads XAML from a specified URI.
        /// </summary>
        /// <typeparam name="T">The type of object expected: Just specify 'object' for any type.</typeparam>
        /// <param name="fileUri">The URI to load the XAML from.</param>
        /// <param name="parseBeginText">If specified, the XAML parsing begins AFTER this text in the file.</param>
        /// <param name="parseEndText">If specified, the XAML parsing begins BEFORE this text in the file.</param>
        /// <returns></returns>
        public static T LoadXAML<T>(Uri fileUri, string parseBeginText, string parseEndText) where T : class
        {
            List<byte[]> buffers = new List<byte[]>(1);
            buffers.Add(new byte[0xFFFF]); // Add a 64k buffer to begin (enough for most needs).

            // ... read xaml as string ...

            StreamResourceInfo info = Application.GetResourceStream(fileUri);

            if (info == null)
                throw new Exception("Resource '" + fileUri.OriginalString + "' not found.");

            int sizeRead, i = 0;
            while ((sizeRead = info.Stream.Read(buffers[i], 0, buffers[i].Length)) == buffers[i].Length)
            {
                buffers.Add(new byte[0xFFFF]); // Add another 64k buffer (in most cases this will never happen - added just in case more is needed).
                i++;
            }

            int totalRead = (buffers.Count - 1) * 0xFFFF + sizeRead;
            byte[] resourceData = buffers.Count == 1 ? buffers[0] : Arrays.Join<byte>(buffers.ToArray());

            string xaml = Encoding.UTF8.GetString(resourceData, 0, totalRead);

            // ... check if there is a defined section to load ...

            int i1 = parseBeginText != null ? xaml.IndexOf(parseBeginText) : -1;
            int i2 = parseEndText != null ? xaml.IndexOf(parseEndText) : -1;

            if (i1 == -1) i1 = 0; else i1 += parseBeginText.Length;
            if (i2 == -1) i2 = xaml.Length;

            if (i1 > i2) { i = i1; i1 = i2; i2 = i; } // (swap indexes if they end up reversed)

            xaml = xaml.Substring(i1, i2 - i1);

            // ... create xaml object from string ...

#if SILVERLIGHT
            T resource = (T)XamlReader.Load(xaml); // (Note: LoadXAML() may be called again during this process)
#else
            T resource = (T)XamlReader.Parse(xaml); // (Note: LoadXAML() may be called again during this process)
#endif

            return resource;
        }
        public static T LoadXAML<T>(Uri fileUri) where T : class
        { return LoadXAML<T>(fileUri, null, null); }

        /// <summary>
        /// Loads the XAML file from the specified assembly and path.
        /// </summary>
        /// <typeparam name="T">The type of object expected: Just specify 'object' for any type.</typeparam>
        /// <param name="filename">The XAML file name.</param>
        /// <param name="path"></param>
        /// <param name="assemblyName"></param>
        /// <param name="parseBeginText">If specified, the XAML parsing begins AFTER this text in the file.</param>
        /// <param name="parseEndText">If specified, the XAML parsing begins BEFORE this text in the file.</param>
        public static T LoadXAML<T>(string filename, string path, string assemblyName,
            string parseBeginText, string parseEndText) where T : class
        {
            // ... extract the path from the file name if exists ...
            if (filename.Contains("\\") || filename.Contains("/"))
            {
                path = System.IO.Path.GetDirectoryName(filename);
                filename = System.IO.Path.GetFileName(filename);
            }

            if (path != "")
            {
                // (fix path string to work properly)
                if (!path.EndsWith("/")) path += "/";
                if (path.StartsWith("/")) path = path.Substring(1);
            }

            if (assemblyName == "") // (use currently assembly by default)
            {
                var assemName = new AssemblyName(Assembly.GetCallingAssembly().FullName);
                assemblyName = assemName.Name;
            }
            else
                assemblyName = assemblyName.Split(',')[0]; // (just in case)

            path = string.Format("/{0};component/{1}{2}", assemblyName, path, filename);
            var uri = new Uri(path, UriKind.Relative);

            return LoadXAML<T>(uri, parseBeginText, parseEndText);
        }
        public static T LoadXAML<T>(string filename) where T : class
        { return LoadXAML<T>(filename, "", "", null, null); }

        // ---------------------------------------------------------------------------------------------------------------------
#if SILVERLIGHT

        /// <summary>
        /// Returns the current 'http://host:port' of the underlying page hosting the current application.
        /// </summary>
        public static string CurrentAbsoluteHostAndPortPath
        {
            get { return CurrentDocumentUri.Scheme + "://" + CurrentHostAndPort; }
        }

        public static int GetPortFromScheme(string scheme) { return new UriBuilder(CurrentDocumentUri.Scheme, "localhost").Uri.Port; }

        public static string CurrentHostAndPort
        {
            get { return CurrentDocumentUri.DnsSafeHost + (CurrentDocumentUri.Port != GetPortFromScheme(CurrentDocumentUri.Scheme) ? ":" + CurrentDocumentUri.Port.ToString() : null); }
        }

        /// <summary>
        /// Returns the current 'http://host:port/path' of the underlying page hosting the current application (minus the query details).
        /// </summary>
        public static string CurrentDocumentPath
        {
            get { return CurrentAbsoluteHostAndPortPath + CurrentDocumentUri.AbsolutePath; }
        }

        public static Uri CurrentDocumentUri
        {
            get
            {
                try
                {
                    if (HtmlPage.IsEnabled && !HtmlPage.BrowserInformation.ProductName.ToUpper().Contains("SAFARI"))
                        return HtmlPage.Document.DocumentUri;
                }
                catch
                {
                }
                var pathToXAP = Application.Current.Host.Source.AbsoluteUri;
                var i = pathToXAP.LastIndexOf("ClientBin");
                var documentPath = i >= 0 ? pathToXAP.Substring(0, i) + "default.html" : pathToXAP + (pathToXAP.EndsWith("/") ? "" : "/") + "default.html";
                return new Uri(documentPath, UriKind.Absolute);
            }
        }

        public static string GetNewDocumentPath(string newPageFilename)
        {
            var currentDocumentPath = CurrentDocumentPath ?? "";
            int i = currentDocumentPath.LastIndexOf('/');
            if (i < 0) return "";
            return currentDocumentPath.Substring(0, i + 1) + newPageFilename;
        }

#endif
        // ---------------------------------------------------------------------------------------------------------------------
    }

    // =========================================================================================================================

    public static partial class ExtentionMethods
    {
        /// <summary>
        /// Binds a dependency property on the specified target element to a the dependency property of a source object,
        /// then returns the binding instance.
        /// The binding process always copies the source value to the target one time by default.
        /// Future updates are dictated by the 'bindingMode' parameter.
        /// </summary>
        /// <param name="source">The source object which holds the value for binding.</param>
        /// <param name="targetElement">The target element instance which will get (and possibly set) the bounded value. The binding is applied to this element instance.</param>
        /// <param name="dpOnTargetElement">A dependency property on the target to bind with.</param>
        /// <param name="sourcePropertyName">the dependency property name on the source object to bind to.</param>
        /// <param name="bindingMode">The binding mode (one time update only, one way continuous updates only, and two way continuous updates).</param>
        public static Binding SetSimpleBinding(this DependencyObject source, FrameworkElement targetElement, DependencyProperty dpOnTargetElement, string sourcePropertyName,
            BindingMode bindingMode, Action<Binding> setOtherBindingPropertiesCallback)
        {
            var binding = new Binding(sourcePropertyName) { Source = source, Mode = bindingMode };
            if (setOtherBindingPropertiesCallback != null)
                setOtherBindingPropertiesCallback(binding);
            targetElement.SetBinding(dpOnTargetElement, binding);
            return binding;
        }
        public static Binding SetSimpleBinding(this DependencyObject source, FrameworkElement targetElement, DependencyProperty dpOnTargetElement, string sourcePropertyName)
        { return SetSimpleBinding(source, targetElement, dpOnTargetElement, sourcePropertyName, BindingMode.OneWay, null); }

        //public static LoadOperation<TEntity> Load<TEntity>(this EntityQuery<TEntity> query) where TEntity : Entity
        //{ return SWController.Service.Load(query, LoadBehavior.MergeIntoCurrent, true); }
        //public static LoadOperation<TEntity> Load<TEntity>(this EntityQuery<TEntity> query, bool throwOnError) where TEntity : Entity
        //{ return SWController.Service.Load(query, LoadBehavior.MergeIntoCurrent, throwOnError); }
        //public static LoadOperation<TEntity> Load<TEntity>(this EntityQuery<TEntity> query, Action<LoadOperation<TEntity>> callback, object userState) where TEntity : Entity
        //{ return SWController.Service.Load(query, LoadBehavior.MergeIntoCurrent, callback, userState); }
        //public static LoadOperation<TEntity> Load<TEntity>(this EntityQuery<TEntity> query, LoadBehavior loadBehavior, bool throwOnError) where TEntity : Entity
        //{ return SWController.Service.Load(query, loadBehavior, throwOnError); }
        //public static LoadOperation<TEntity> Load<TEntity>(this EntityQuery<TEntity> query, LoadBehavior loadBehavior, Action<LoadOperation<TEntity>> callback, object userState) where TEntity : Entity
        //{ return SWController.Service.Load(query, loadBehavior, callback, userState); }

        /// <summary>
        /// Transforms a point from child element space, to a parent's element space.
        /// </summary>
        /// <param name="element">The child element.</param>
        /// <param name="pointToTransform">The point to transform.</param>
        /// <param name="relativeToThisParent">An element in the parent hierarchy to translate the point to.</param>
        /// <returns>The translated point.</returns>
        public static Point TransformPoint(this FrameworkElement element, Point pointToTransform, FrameworkElement relativeToThisParent)
        {
            if (element == null) throw new ArgumentNullException("element");
            if (!VisualTree.IsVisible(element))
                throw new InvalidOperationException("TransformPoint(): The element is not in the visual tree.");
            if (relativeToThisParent == null) relativeToThisParent = VisualTree.RootVisual;
            return element.TransformToVisual(relativeToThisParent).Transform(pointToTransform);
        }
        public static Point TransformPoint(this FrameworkElement element, Point pointToTransform) { return TransformPoint(element, pointToTransform, null); }

        /// <summary>
        /// Based on a given element, this returns its position relative to the root visual element.
        /// </summary>
        /// <param name="element">The element who's position will be translate to root visual space.</param>
        public static Point GetRootVisualPosition(this FrameworkElement element)
        {
            if (element == null) throw new ArgumentNullException("element");
            if (VisualTree.RootVisual == null) throw new InvalidOperationException("GetRootVisualPosition(): 'VisualTree.RootVisual' cannot be 'null'.");
            return element.TransformPoint(new Point(0, 0), VisualTree.RootVisual);
        }

        /// <summary>
        /// Returns 'true' if the object contains a 'Content' property.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool HasContentProperty(this DependencyObject obj) /* Extension Method */
        {
            return obj != null && obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.Name == "Content" || p.Name == "Child").FirstOrDefault() != null;
        }

        /// <summary>
        /// If the control has a 'Content' property (see 'HasContentProperty()'), then this returns its value.
        /// </summary>
        public static DependencyObject GetContent(this DependencyObject obj) /* Extension Method */
        {
            if (obj == null) return null;

            // ... get the lower most 'Content' property in the hierarchy ([0] is lowest, [1] is next, and so on) ...
            PropertyInfo pInfo = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.Name == "Content" || p.Name == "Child").FirstOrDefault();

            if (pInfo != null)
                return pInfo.GetValue(obj, null) as DependencyObject;

            if (VisualTreeHelper.GetChildrenCount(obj) == 1)
                return VisualTreeHelper.GetChild(obj, 0);

            return null;
        }

        /// <summary>
        /// If the control has a 'Content' property (see 'HasContentProperty()'), then this can be used to set its value.
        /// The value returned is the same as the one given as the 'value' argument.
        /// </summary>
        public static DependencyObject SetContent(this DependencyObject obj, DependencyObject value) /* Extension Method */
        {
            if (obj == null) return value;

            PropertyInfo pInfo = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.Name == "Content" || p.Name == "Child").FirstOrDefault();

            if (pInfo != null)
                pInfo.SetValue(obj, value, null);
            else if (obj is Panel && value is UIElement)
                if (((Panel)obj).Children.Count == 0)
                    ((Panel)obj).Children.Add((UIElement)value);
                else if (((Panel)obj).Children.Count == 1)
                    ((Panel)obj).Children[0] = (UIElement)value;

            return value;
        }

        /// <summary>
        /// Returns a convenient true/false condition on whether or not an element has its visibility flag set to 'Visible', has an opacity setting greater than '0.0', and the actual width and height is more than 0.0.
        /// This DOES NOT determine if the element is ACTUALLY visible in the visual tree (i.e. not added, or near zero opacity setting [such as 0.01]).
        /// </summary>
        public static bool IsVisible(this FrameworkElement e)
        { return (e != null && (e.Visibility == Visibility.Visible && e.Opacity > 0.0d)); }

        /// <summary>
        /// Simply calls 'IsVisible()', and also checks if the element has actual non-zero dimensions.
        /// Note: Some elements won't have any dimensions until they are loaded.
        /// </summary>
        public static bool IsActualVisible(this FrameworkElement e)
        { return (IsVisible(e) && e.ActualWidth > 0d && e.ActualHeight > 0d); }

        /// <summary>
        /// Simply calls 'IsVisible()', and also checks if the element is in the visual tree as well.
        /// </summary>
        public static bool IsVisibleInTree(this FrameworkElement e) { return IsVisible(e) && VisualTree.IsInVisualTree(e); }

        /// <summary>
        /// Fits the given image to the bitmap source size, if one exists.
        /// </summary>
        public static void FitToBitmap(this Image image)
        {
            if (image != null)
            {
                var bitmap = image.Source as BitmapImage;
                if (bitmap != null)
                {
                    image.Width = bitmap.PixelWidth;
                    image.Height = bitmap.PixelHeight;
                }
            }
        }
    }

    // =========================================================================================================================

    public static partial class VisualTree
    {
#if SILVERLIGHT
     /// <summary>
        /// Returns the top most level visual root. For Silverlight, this means the application visual root.
        /// <para>Warning: It is advised to use controller implementations to find this instead. The host application may nest elements from the root before the proper root begins.</para>
        /// </summary>
        public static FrameworkElement RootVisual
        { get { return Application.Current != null ? VisualTree.RootVisual as FrameworkElement : null; } }
#else
        /// <summary>
        /// Returns the top most level visual root. For WPF, this means the main window 'Content' reference.
        /// <para>Warning: It is advised to use controller implementations to find this instead. The host application may nest elements from the root before the proper root begins.</para>
        /// </summary>
        public static FrameworkElement RootVisual
        { get { return Application.Current != null ? Application.Current.MainWindow.Content as FrameworkElement : null; } }
#endif

        /// <summary>
        /// Returns the first *Panel* type control under the visual root.
        /// </summary>
        public static Panel RootPanel
        {
            get
            {
                if (RootVisual == null) return null;
                if (RootVisual is Panel)
                    return RootVisual as Panel;
                var rootPanel = VisualTreeHelper.GetChild(RootVisual, 0);
                while (rootPanel != null && !(rootPanel is Panel))
                    rootPanel = VisualTreeHelper.GetChild(rootPanel, 0);
                return rootPanel as Panel;
            }
        }

        /// <summary>
        /// In order to remove a control (such as a custom window) from it's container, the parent chain
        /// has to be traversed in order to find the root child to be removed (as some controls are usually
        /// embedded into others). This works well as long as there are no controls in this chain that
        /// derived from the Panel (container) control.
        /// </summary>
        /// <param name="searchStart">The element to begin searching at.</param>
        /// <param name="rootVisual">The object acting as the root visual, or 'null' for 'VisualTree.RootVisual'.</param>
        public static FrameworkElement ParentPanelRootChild(FrameworkElement searchStart, DependencyObject rootVisual)
        {
            if (rootVisual == null) rootVisual = VisualTree.RootVisual;
            DependencyObject p = searchStart, p2;
            while (p != null && p != rootVisual)
            {
                p2 = VisualTreeHelper.GetParent(p);
                if (p2 == null || p2 == rootVisual || p2 is Panel) break;
                p = p2;
            }
            return p as FrameworkElement;
        }
        public static FrameworkElement ParentPanelRootChild(FrameworkElement searchStart) { return ParentPanelRootChild(searchStart, null); }

        /// <summary>
        /// Returns the first parent panel found, which is the same as getting the parent of the 'ParentPanelRootChild' property.
        /// </summary>
        public static Panel ParentPanel(FrameworkElement element, bool includeElement)
        {
            DependencyObject p = (includeElement || element == null) ? element : VisualTreeHelper.GetParent(element);
            while (p != null && !(p is Panel))
                p = VisualTreeHelper.GetParent(p);
            return p as Panel;
        }
        public static Panel ParentPanel(FrameworkElement element)
        {
            return ParentPanel(element, false);
        }

        public static bool RemoveElement(FrameworkElement element)
        {
            var parent = VisualTreeHelper.GetParent(element);
            if (parent != null)
                if (parent is Panel)
                {
                    ((Panel)parent).Children.Remove(element);
                    return true;
                }
                else if (parent is ContentPresenter)
                {
                    ((ContentPresenter)parent).Content = null;
                    return true;
                }
                else if (parent is ContentControl)
                {
                    ((ContentControl)parent).Content = null;
                    return true;
                }
                else if (Objects.SetPropertyOrFieldValue<object>(parent, "Content", null)) return true;
                else if (Objects.SetPropertyOrFieldValue<object>(element, "Child", null)) return true;
            return false;
        }

        /// <summary>
        /// Returns true if the specified element is attached to the visual tree.
        /// </summary>
        public static bool IsInVisualTree(DependencyObject element)
        {
            while (element != null)
            {
                if (element == VisualTree.RootVisual)
                    return true;
                element = VisualTreeHelper.GetParent(element);
            }
            return false;
        }

        /// <summary>
        /// Returns true if the specified element is attached to the visual tree, and all elements in the 
        /// parent hierarchy are visible with an opacity greater than 0.
        /// </summary>
        public static bool IsVisible(DependencyObject element)
        {
            while (element != null)
            {
                if (element is UIElement && (((UIElement)element).Visibility == Visibility.Collapsed || ((UIElement)element).Opacity == 0.0d))
                    break;
                if (element == VisualTree.RootVisual)
                    return true;
                element = VisualTreeHelper.GetParent(element);
            }
            return false;
        }

        /// <summary>
        /// Returns true if the expected parent element is within the parent hierarchy of the starting element.
        /// </summary>
        /// <param name="startingElement">Where to begin the parent hierarchy search.</param>
        /// <param name="expectedParentElement">The element that is validated as within the parent chain of the starting element.</param>
        public static bool ContainsElementInHierarchy(DependencyObject startingElement, DependencyObject expectedParentElement)
        {
            DependencyObject p = startingElement;
            while (p != null && p != expectedParentElement)
                p = VisualTreeHelper.GetParent(p);
            return (p != null);
        }

        /// <summary>
        /// Locate an object starting from the root visual element.
        /// </summary>
        /// <param name="name">The element identifier to search for.</param>
        /// <param name="bridgeHandler">Allows a call back method to help extend or bridge the search for each element found for cases where controls have user content in unknown places.
        /// Each element in the name scope is given to the handler if there is no match, along with the identifier name to look for, and if a non-null value is returned, the search ends.</param>
        /// <param name="ignoreNameScopes">If name scopes are not ignored, the template children are also considered.</param>
        public static FrameworkElement GetElement(string name, Func<FrameworkElement, IEnumerable<FrameworkElement>> bridgeHandler, bool ignoreNameScopes)
        {
            if (name == null) return null;
            FrameworkElement root = VisualTree.RootVisual as FrameworkElement;
            return _GetElementByName(name, root, bridgeHandler, ignoreNameScopes);
        }
        public static FrameworkElement GetElement(string name) { return GetElement(name, null, false); }

        /// <summary>
        /// Locate an object by its type starting from the root visual element.
        /// </summary>
        /// <param name="rootElement">The element which is the root of the search.</param>
        /// <param name="bridgeHandler">Allows a call back method to help extend or bridge the search for each element found for cases where controls have user content in unknown places.
        /// Each element in the name scope is given to the handler if there is no match, along with the identifier name to look for, and if a non-null value is returned, the search ends.</param>
        /// <param name="ignoreNameScopes">If name scopes are not ignored, the template children are also considered.</param>
        public static EType GetElement<EType>(this FrameworkElement rootElement, Func<FrameworkElement, IEnumerable<FrameworkElement>> bridgeHandler, bool ignoreNameScopes)
            where EType : FrameworkElement
        {
            if (rootElement == null) return null;
            return EnumerateChildElements(rootElement, bridgeHandler, ignoreNameScopes).Where(e => e is EType).Select(e => (EType)e).FirstOrDefault();
        }
        public static EType GetElement<EType>(this FrameworkElement rootElement)
            where EType : FrameworkElement
        { return GetElement<EType>(rootElement, null, false); }

        /// <summary>
        /// Locate an object starting from the given element as the root of the search.
        /// </summary>
        /// <param name="rootElement">The element which is the root of the search.</param>
        /// <param name="name">The element identifier to search for.</param>
        /// <param name="bridgeHandler">Allows a call back method to help extend or bridge the search for each element found for cases where controls have user content in unknown places.
        /// Each element in the name scope is given to the handler if there is no match, along with the identifier name to look for, and if a non-null value is returned, the search ends.</param>
        /// <param name="ignoreNameScopes">If name scopes are not ignored, the template children are also considered.</param>
        public static FrameworkElement GetElement(this FrameworkElement rootElement, string name, Func<FrameworkElement, IEnumerable<FrameworkElement>> bridgeHandler, bool ignoreNameScopes)
        {
            if (rootElement == null) return null;
            return _GetElementByName(name, rootElement, bridgeHandler, ignoreNameScopes);
        }
        public static FrameworkElement GetElement(this FrameworkElement rootElement, string name) { return GetElement(rootElement, name, null, false); }

        static FrameworkElement _GetElementByName(string name, FrameworkElement element, Func<FrameworkElement, IEnumerable<FrameworkElement>> bridgeHandler, bool ignoreNameScopes)
        {
            if (name == null) return null;
            return EnumerateChildElements(element, bridgeHandler, ignoreNameScopes).Where(e => e.Name == name).Select(e => e).FirstOrDefault();
        }

        public static IEnumerable<FrameworkElement> EnumerateChildElements(FrameworkElement element, Func<FrameworkElement, IEnumerable<FrameworkElement>> bridgeHandler, bool ignoreNameScopes)
        {
            bool hasContentProperty = false;

            int childCount = VisualTreeHelper.GetChildrenCount(element); // (always do this first! [more reliable])

            if (childCount == 0) // (no visual children, test for known logical content-type properties...)
            {
                hasContentProperty = element.HasContentProperty();
                if (hasContentProperty) childCount = 1;
            }

            FrameworkElement child;

            for (int i = 0; i < childCount; i++)
            {
                // ... check child element ...

                child = hasContentProperty ? element.GetContent() as FrameworkElement : VisualTreeHelper.GetChild(element, i) as FrameworkElement;
                if (child == null) continue; // (not supported, whatever this might be)

                yield return child;

                // ... bridge handler supplied? ...

                if (bridgeHandler != null)
                {
                    foreach (var childElement in bridgeHandler(child))
                        yield return childElement;
                }

                // ... check children of the child element, if any, and if in the name scope! ...

                if (ignoreNameScopes || child.Parent != null)
                {
                    foreach (var childElement in EnumerateChildElements(child, bridgeHandler, ignoreNameScopes))
                        yield return childElement;
                }
            }
        }

        public static FrameworkElement GetParentElementByName(FrameworkElement rootElement, string name)
        {
            FrameworkElement element = (FrameworkElement)VisualTreeHelper.GetParent(rootElement);
            while (element != null)
            {
                if (element.Name == name) return element;
                element = (FrameworkElement)VisualTreeHelper.GetParent(element);
            }
            return null;
        }

        /// <summary>
        /// Locates a parent object of the exact specified type.
        /// </summary>
        /// <typeparam name="T">The type to look for.</typeparam>
        /// <param name="searchStart">The element to begin searching at.</param>
        /// <param name="includeDerivedTypes">If 'true', the first valid derived type is returned.</param>
        /// <param name="returnLastObject">If 'true', instead of returning 'null' on exit during filtered searches, the last object checked of the requested type is returned. 'null' is still returned if there are no parent objects of the requested type.</param>
        /// <param name="whileFilter">Search continues only while this filter method returns 'true'.</param>
        /// <param name="whereFilter">If specified, the search ends and returns the element of the request type only if the "where" filter returns 'true'. If not, the search continues.</param>
        public static T GetParentObject<T>(DependencyObject searchStart, bool includeDerivedTypes, bool returnLastObject,
            Func<FrameworkElement, bool> whileFilter,
            Func<T, bool> whereFilter) where T : DependencyObject
        {
            DependencyObject dObj = searchStart;
            T lastObjOfTypeT = null;
            while (dObj != null)
            {
                if (dObj is T) lastObjOfTypeT = (T)dObj;
                dObj = VisualTreeHelper.GetParent(dObj) ?? (dObj is FrameworkElement ? ((FrameworkElement)dObj).Parent : null);
                if (dObj != null)
                    if (includeDerivedTypes)
                    { if (typeof(T).IsAssignableFrom(dObj.GetType()) && (whereFilter == null || whereFilter((T)dObj))) return dObj as T; }
                    else
                    { if (dObj.GetType() == typeof(T) && (whereFilter == null || whereFilter((T)dObj))) return dObj as T; }
                if (whileFilter != null && dObj is FrameworkElement && !whileFilter((FrameworkElement)dObj)) break;
            }
            return returnLastObject ? lastObjOfTypeT : (T)null;
        }
        public static T GetParentObject<T>(DependencyObject searchStart) where T : DependencyObject
        { return GetParentObject<T>(searchStart, false, false, null, null); }

        /// <summary>
        /// Returns true if the specified element is under the specified point.
        /// </summary>
        /// <param name="element">The element to look for.</param>
        /// <param name="point">The point at which to look under, relative to the visual root (use {MouseButtonEventArgs}.GetPosition(null)).</param>
        /// <param name="subTree">The parent node which should contain the specified element (defaults to the visual tree root - but much slower if the element is buried deep in the tree).</param>
        /// <returns>'True' if the element is in the subTree, and under the specified point.</returns>
        public static bool IsMouseOver(UIElement element, Point point, UIElement subTree)
        {
            if (subTree == null) subTree = VisualTree.RootVisual;
#if SILVERLIGHT
            var elements = VisualTreeHelper.FindElementsInHostCoordinates(point, subTree);
            return elements.Contains(element);
#else
            var result = VisualTreeHelper.HitTest(subTree, point);
            return result != null;
#endif
        }

        /// <summary>
        /// Searches the parent hierarchy for a window instance (or any object that inherits from it).  Returns null if not found.
        /// </summary>
        public static Window FindWindow(this DependencyObject _this)
        {
           return GetParentObject<Window>(_this, true, true, null, null);
        }

        /// <summary>
        /// Searches the parent hierarchy for a window instance to close.  Returns true if successful, and false otherwise.
        /// </summary>
        public static bool CloseWindow(this DependencyObject _this)
        {
            var win = FindWindow(_this);
            if (win != null)
            {
                win.Close();
                return true;
            }
            else return false;
        }
    }

    // =========================================================================================================================

    public partial class Messaging
    {
        public static readonly Popup GlobalMessagePopup = new Popup();

        /// <summary>
        /// Shows a message on the screen using a simple semi-transparent text box.
        /// If a message already exists, the new message is appended.
        /// </summary>
        public static void ShowMessage(string msg)
        {
            StackPanel rootPanel;
            UIElementCollection uieCol;
            TextBox tb;

            if (GlobalMessagePopup.Child == null)
            {
                rootPanel = new StackPanel();
                uieCol = rootPanel.Children;

                tb = new TextBox();
                tb.VerticalAlignment = VerticalAlignment.Stretch;
                tb.HorizontalAlignment = HorizontalAlignment.Stretch;
                Canvas.SetZIndex(tb, Int16.MaxValue - 1);
                //??tb.LostFocus += (object s, RoutedEventArgs a) => { ((Panel)((FrameworkElement)s).Parent).Children.Remove((UIElement)s); };
                uieCol.Add(tb);

                Button btn = new Button();
                btn.HorizontalAlignment = HorizontalAlignment.Left;
                btn.Content = "Close";
                btn.Click += new RoutedEventHandler(popup_close_btn_Click);

                uieCol.Add(btn);

                GlobalMessagePopup.HorizontalAlignment = HorizontalAlignment.Center;
                GlobalMessagePopup.VerticalAlignment = VerticalAlignment.Center;
                GlobalMessagePopup.Child = rootPanel;
                GlobalMessagePopup.Opacity = 0.80;
            }
            else
            {
                rootPanel = (StackPanel)GlobalMessagePopup.Child;
                uieCol = rootPanel.Children;
                tb = (TextBox)uieCol[0];
            }

            if (tb.Text != "") tb.Text += "\r\n";
            tb.Text += msg;

            if (!GlobalMessagePopup.IsOpen)
                GlobalMessagePopup.IsOpen = true;
        }

        /// <summary>
        /// Simple dispatches a call to 'ShowMessage()' with the specified message.
        /// This is useful in cases where a message needs to be displayed before the visual root exists.
        /// </summary>
        public static void ShowMessageDelayed(string msg)
        {
            Dispatching.Dispatch((Action<string>)ShowMessage, msg);
        }

        static void popup_close_btn_Click(object sender, RoutedEventArgs e)
        {
            GlobalMessagePopup.IsOpen = false;
        }
    }

    // =========================================================================================================================

    public partial class Dispatching
    {
        // .....................................................................................................................

        struct _DelegateCall { public int Priority; public string Scope; public object Key; public Delegate Delegate; public object[] Args; }
        public enum Priority { Low = int.MinValue, Normal = 0, High = int.MaxValue };

        static List<_DelegateCall> _DispatchQueue = new List<_DelegateCall>(10);

        static bool _QueueDispatchCallInProgress = false;

        /// <summary>
        /// Insert a method dispatch into a queue that will be called based on the specified priority.
        /// If a key is specified, the any existing entry found matching the key is moved to the back of the queue.
        /// This is useful to help prevent executing code more than is necessary - especially order-sensitive tasks.
        /// </summary>
        /// <param name="priority">Highest priority numbered calls are dispatched first.</param>
        /// <param name="keyScope">If using keys (see next parameter), this separates them into scopes (example: method names, like 'MethodBase.GetCurrentMethod().Name'). Specify 'null' to ignore.</param>
        /// <param name="key">Any matching dispatch with the same key (example: element instance), and in the same scope, is moved to the back of the queue. Specify 'null' to ignore.</param>
        /// <param name="d">The method to call.</param>
        /// <param name="args">Arguments for the queued method.</param>
        static void _AddDispatch(int priority, string keyScope, object key, Delegate d, object[] args) // TODO: More than one key needed (see CPPOptionList usage).
        {
            int i;

            if (key != null)
            {
                // ... remove all items with the same key (key is by name, if string, or instance otherwise) ...
                bool keyIsString = key is string;
                for (i = _DispatchQueue.Count - 1; i >= 0; i--)
                    if (
                        (keyScope == null || keyScope == _DispatchQueue[i].Scope)
                        &&
                        (_DispatchQueue[i].Key == key || keyIsString && (_DispatchQueue[i].Key is string) && (string)_DispatchQueue[i].Key == (string)key)
                        )
                        _DispatchQueue.RemoveAt(i);
            }

            if (d != null)
            {
                for (i = 0; i < _DispatchQueue.Count; i++)
                    if (_DispatchQueue[i].Priority >= priority) break;

                _DispatchQueue.Insert(i, new _DelegateCall { Priority = priority, Scope = keyScope, Key = key, Delegate = d, Args = args });
            }
        }

        // .....................................................................................................................

        /// <summary>
        /// Uses "DependencyObject.Dispatcher" to queue a method call on the main thread's message queue.
        /// This is a simple shortcut to conveniently access this existing functionality within the Silverlight system.
        /// </summary>
        public static void Dispatch(Action a)
        {
            if (VisualTree.RootVisual != null)
                VisualTree.RootVisual.Dispatcher.BeginInvoke(a);
            else
                new _TempDepObj().Dispatcher.BeginInvoke(a);
        }

        /// <summary>
        /// Uses "DependencyObject.Dispatcher" to queue a method call on the main thread's message queue.
        /// This is a simple shortcut to conveniently access this existing functionality within the Silverlight system.
        /// </summary>
        public static void Dispatch(Delegate d, params object[] args)
        {
            if (VisualTree.RootVisual != null)
                VisualTree.RootVisual.Dispatcher.BeginInvoke(d, args);
            else
                new _TempDepObj().Dispatcher.BeginInvoke(d, args);
        }

        class _TempDepObj : DependencyObject { };

        // .....................................................................................................................

        /// <summary>
        /// Queues an action under optional scope and key categories to be executed in consecutive sequence (synchronized).
        /// </summary>
        public static void QueueDispatch(int priority, Action a, string keyScope, object key)
        {
            _AddDispatch(priority, keyScope, key, a, null);
            _ProcessDispatchQueue();
        }
        public static void QueueDispatch(int priority, Action a) { QueueDispatch(priority, a, null, null); }

        /// <summary>
        /// Queues an action under optional scope and key categories to be executed in consecutive sequence (synchronized).
        /// </summary>
        public static void QueueDispatch(Priority priority, Action a, string keyScope, object key)
        { QueueDispatch((int)priority, a, keyScope, key); }
        public static void QueueDispatch(Priority priority, Action a) { QueueDispatch(priority, a, null, null); }

        /// <summary>
        /// Queues an action under optional scope and key categories to be executed in consecutive sequence (synchronized).
        /// </summary>
        public static void QueueDispatch(int priority, string keyScope, object key, Delegate d, params object[] args)
        {
            _AddDispatch(priority, keyScope, key, d, args);
            _ProcessDispatchQueue();
        }

        /// <summary>
        /// Queues an action under optional scope and key categories to be executed in consecutive sequence (synchronized).
        /// </summary>
        /// <param name="priority">All queued items are executed in the order added within each priority. In that ordering, all high priority actions go first, regardless of the order of other lower priorities added.</param>
        /// <param name="a">The action to execute.</param>
        /// <param name="keyScope">A "user defined" scope (grouping), such as a XAML page, or class name.</param>
        /// <param name="key">A "user defined" key identifier, which can be a string, or object instance. The key is unique within the given scope. An example usage might be sub-actions within a XAML page, or class. For globally unique queued actions, you can use the scope as a unique key in itself.</param>
        /// <param name="args">Optional arguments that can be passed to the queued delegate.</param>
        public static void QueueDispatch(Priority priority, string keyScope, object key, Delegate d, params object[] args)
        { QueueDispatch((int)priority, keyScope, key, d, args); }

        /// <summary>
        /// Cancels a previous queued dispatch request based on the given scope and optional key.
        /// If 'null' is given as the scope, then ALL queued dispatches are cancelled, whether they have scopes or not.
        /// If the scope and key are not found, then call simply returns without error.
        /// </summary>
        public static void CancelQueuedDispatch(string keyScope, object key)
        {
            _AddDispatch(-1, keyScope, key, null, null);
        }
        public static void CancelQueuedDispatch(string keyScope) { CancelQueuedDispatch(keyScope, null); }

        static void _ProcessDispatchQueue()
        {
            if (!_QueueDispatchCallInProgress && _DispatchQueue.Count > 0)
            {
                _QueueDispatchCallInProgress = true;
                Dispatch(_DoDispatch);
            }
        }

        static void _DoDispatch()
        {
            int lastIndex = _DispatchQueue.Count - 1;
            if (lastIndex >= 0)
            {
                var call = _DispatchQueue[lastIndex];
                _DispatchQueue.RemoveAt(lastIndex); // (make sure to remove BEFORE invoking the method, as more dispatch items might become added)
                call.Delegate.DynamicInvoke(call.Args);
            }
            _QueueDispatchCallInProgress = false;
            _ProcessDispatchQueue(); // (keep dispatching until the queue is emptied)
        }

        // .....................................................................................................................

        /// <summary>
        /// If the framework element is not yet loaded (has no parent), then the dispatch request is queued, otherwise the
        /// delegate is called immediately. For XAML that is loaded by the system directly into the visual tree, the dispatched
        /// method will always call BEFORE the 'Loaded' event triggers, but is not dependent on it.
        /// To force dispatch queuing, use one of the other 'Dispatch()' methods instead.
        /// </summary>
        public static void ElementPreLoadDispatch(FrameworkElement element, int priority, string keyScope, object key, Delegate d, params object[] args)
        {
            if (VisualTreeHelper.GetParent(element) != null)
                d.DynamicInvoke(args); // (object already loaded, so execute immediately)
            else
                QueueDispatch(priority, keyScope, key, d, args); // (object not yet loaded, so wait ...)

            // Note: The object may be created dynamically, and thus, may never have a parent for some time. In this case,
            // the dispatch will still occur - that is, regardless if the 'Loaded' event is immediately following, or not.
            // Note: All objects within dynamically loaded XAML will not have the 'Loaded' event triggered until the whole
            // logical tree becomes added to the visual tree.
        }
        public static void ElementPreLoadDispatch(FrameworkElement element, Priority priority, string keyScope, object key, Delegate d, params object[] args)
        { ElementPreLoadDispatch(element, (int)priority, keyScope, key, d, args); }

        public static void ElementPreLoadDispatch(FrameworkElement element, int priority, Action a, string keyScope, object key)
        { ElementPreLoadDispatch(element, priority, keyScope, key, a, null); }

        public static void ElementPreLoadDispatch(FrameworkElement element, Priority priority, Action a, string keyScope, object key)
        { ElementPreLoadDispatch(element, (int)priority, keyScope, key, a, null); }
    }

    // =========================================================================================================================

    public class ActualSizePropertyProxy : FrameworkElement, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public FrameworkElement Element
        {
            get { return (FrameworkElement)GetValue(ElementProperty); }
            set { SetValue(ElementProperty, value); }
        }

        public double ActualWidthValue
        {
            get { return Element == null ? 0 : Element.ActualWidth; }
        }

        public double ActualHeightValue
        {
            get { return Element == null ? 0 : Element.ActualHeight; }
        }

        public class GridIndexedLength
        {
            ActualSizePropertyProxy _Proxy;
            Func<int, double> _GetValueCallback;
            public GridIndexedLength(ActualSizePropertyProxy proxy, Func<int, double> getValueCallback)
            { _Proxy = proxy; _GetValueCallback = getValueCallback; }
            public double this[int index]
            {
                get
                {
                    if (_Proxy.Element is Grid && index >= 0 && index < ((Grid)_Proxy.Element).ColumnDefinitions.Count)
                        return _GetValueCallback(index);
                    else
                        return 0;
                }
            }
        }

        public GridIndexedLength ColumnActualWidth = null;
        public GridIndexedLength RowActualHeight = null;

        public ActualSizePropertyProxy()
        {
            ColumnActualWidth = new GridIndexedLength(this, i => ((Grid)Element).ColumnDefinitions[i].ActualWidth);
            RowActualHeight = new GridIndexedLength(this, i => ((Grid)Element).RowDefinitions[i].ActualHeight);
        }

        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register("Element", typeof(FrameworkElement), typeof(ActualSizePropertyProxy),
                                        new PropertyMetadata(null, OnElementPropertyChanged));

        private static void OnElementPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ActualSizePropertyProxy)d).OnElementChanged(e);
        }

        private void OnElementChanged(DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement oldElement = (FrameworkElement)e.OldValue;
            FrameworkElement newElement = (FrameworkElement)e.NewValue;

            if (oldElement != null)
                oldElement.SizeChanged -= new SizeChangedEventHandler(Element_SizeChanged);
            newElement.SizeChanged += new SizeChangedEventHandler(Element_SizeChanged);
            NotifyPropChange();
        }

        private void Element_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            NotifyPropChange();
        }

        private void NotifyPropChange()
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("ActualWidthValue"));
                PropertyChanged(this, new PropertyChangedEventArgs("ActualHeightValue"));
                PropertyChanged(this, new PropertyChangedEventArgs("ColumnActualWidth"));
                PropertyChanged(this, new PropertyChangedEventArgs("RowActualHeight"));
            }
        }
    }

    // =========================================================================================================================

    public static class DependencyProperties
    {

    }

    // =========================================================================================================================
}