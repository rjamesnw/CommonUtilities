using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Common.CollectionsAndLists;
using System.Collections.Generic;

namespace Common.XAML.Windows
{
    // ######################################################################################################

    /// <summary>
    /// Manages windows for a XAML based application.
    /// </summary>
    public sealed class WindowsManager
    {
        // --------------------------------------------------------------------------------------------------

        public static readonly WindowsManager DefaultInstance = new WindowsManager();

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// A place holder for ALL windows opened in the system.
        /// Each newly constructed instance auto registers with this list.
        /// If the window is 'disposed' without notice, and there are no other references, then the weak
        /// reference index position will be set to 'null'.
        /// <para>Warning: 'null' index positions are reclaimed for use by new windows.</para>
        /// <para>Note: The proper way to deal with windows is via the GetWindow() and CloseWindow()
        /// instance methods, or via the window instance methods themselves.</para>
        /// </summary>
        public static readonly WeakReferenceList<Window> Windows = new WeakReferenceList<Window>(); // TODO: Lock down via assembly access?

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// Holds a list of all commonly constructed windows in the system, such as the login window, main
        /// menus, etc. Setting a window's name automatically registers it with this list.
        /// </summary>
        static readonly Dictionary<object, Dictionary<string, Window>> _NamedWindows = new Dictionary<object, Dictionary<string, Window>>();

        public static bool Exists(object owner, string name) { return _NamedWindows.ContainsKey(owner) && _NamedWindows[owner].ContainsKey(name); }

        // ---------------------------------------------------------------------------------------------------------------------

        internal WindowsManager() // (instance constructor for this controller part)
        {
        }

        // --------------------------------------------------------------------------------------------------

        void _RequireName(object owner, string name, bool checkExisting = false)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Invalid window name: '" + name + "' - must not be null or empty.", "name");
            if (checkExisting && Exists(owner, name))
                throw new ArgumentException("Invalid window name: '" + name + "' - a window by this name already exists for the application.", "name");
        }

        /// <summary>
        /// Returns an existing window reference, with the caption changed and events reset, or 'null' if not found.
        /// To reset the window and change content-type-specific details, use the type-specific "Get...()" method.
        /// <para>Note: This does not create a window, it only returns a reference to an existing one.
        /// The resulting window reference, if any, can be found in {WindowReference&lt;&gt;}.Window. </para>
        /// </summary>
        /// <param name="name">A window identifier for future access.</param>
        /// <param name="caption">Window caption (leave 'null' to use existing caption).</param>
        public WindowReference<ContentType> GetWindow<ContentType>(object owner, string name, string caption = null)
            where ContentType : class
        {
            if (owner == null)
                throw new ArgumentException("Application reference cannot be null.", "owner");

            _RequireName(owner, name);

            Window win = Exists(owner, name) ? _NamedWindows[owner][name] : null;
            if (win != null)
            {
                if (caption != null)
                    win.Title = caption;
                win.ResetWindowEvents();
                return new WindowReference<ContentType>(win);
            }

            return new WindowReference<ContentType>();
        }

        public WindowReference<object> GetWindow(object owner, string name, string caption = null)
        { return GetWindow<object>(owner, name, caption); }

        /// <summary>
        /// Just a convenient way to get the window by name and close it at the same time.
        /// </summary>
        /// <param name="name">Name of the window to close.</param>
        public bool CloseWindow(object owner, string name)
        {
            _RequireName(owner, name);
            var winRef = GetWindow(owner, name);
            if (winRef.Ok) { winRef.Window.Close(); return true; }
            return false;
        }

        /// <summary>
        /// Closes and removes the specified window from the internal window list.
        /// This allows the window object to be reclaimed by the GC (if not referenced elsewhere).
        /// Calling the Window.Close() instance method calls this method implicitly.
        /// </summary>
        /// <param name="name">Window identifier.</param>
        internal bool _UnRegisterNamedWindow(Window window)
        {
            if (window == null) throw new ArgumentNullException("window");
            _RequireName(window.Owner, window.Name);
            var winRef = GetWindow(window.Owner, window.Name);
            if (winRef.Ok)
            {
                winRef.Window.Close(); // (note: will close if still in the visual tree; if called via Close(), then window will not be in the visual tree, and this does nothing)
                _NamedWindows[window.Owner].Remove(window.Name);
                if (_NamedWindows[window.Owner].Count == 0)
                    _NamedWindows.Remove(window.Owner); // (window collections are stored with owners, so if the owner is no longer valid, this needs to be cleaned up)
                return true;
            }
            return false;
        }

        /// <summary>
        /// Registers a window in the static dictionary with the specified name, replacing any existing table.
        /// </summary>
        internal Window _RegisterNamedWindow(Window window)
        {
            if (window == null) throw new ArgumentNullException("window");
            _RequireName(window.Owner, window.Name, true);
            if (!_NamedWindows.ContainsKey(window.Owner))
                _NamedWindows.Add(window.Owner, new Dictionary<string, Window>());
            _NamedWindows[window.Owner][window.Name] = window;
            return window;
        }

        // --------------------------------------------------------------------------------------------------

        ///// <summary>
        ///// Returns the specified window, creating it only once, and returning the same instance for each subsequent call.
        ///// </summary>
        ///// <param name="name">Window identifier (leave 'null' to use the default caption value).</param>
        ///// <param name="caption">Window caption (set to 'null' to use the name identifier as the caption).</param>
        //public WindowReference<Login> GetLoginWindow(object owner, string name = null, string caption = "Login")
        //{
        //    if (name == null) name = caption;
        //    if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Invalid window identifier", "name");
        //    if (caption == null) caption = name;

        //    var winRef = GetWindow<Login>(owner, name, caption);

        //    var loginCtrl = winRef.Content ?? new Login();
        //    var win = winRef.Window ?? owner.CreateWindow(caption, WindowButtons.Ok);
        //    win.Name = name;
        //    win.Content = loginCtrl;
        //    win.Resizeable = false;
        //    win.CloseButtonEnabled = false;
        //    win.HideOnClose = true;

        //    return new WindowReference<Login>(win);
        //}

        //// --------------------------------------------------------------------------------------------------

        ///// <summary>
        ///// Returns the specified window, creating it only once, and returning the same instance for each
        ///// subsequent call.
        ///// </summary>
        ///// <param name="name">Window identifier (leave 'null' to use the default caption value).</param>
        ///// <param name="itemSource">Menu selection list.</param>
        ///// <param name="caption">Window caption (set to 'null' to use the name identifier as the caption).</param>
        ///// <param name="message">A message at the menu top, and under the window title bar (pass 'null' to use any existing message).</param>
        //public WindowReference<Menu> GetMenuWindow(object owner, IEnumerable<MenuItem> itemSource, string name = null, string caption = "Menu", string message = null)
        //{
        //    if (name == null) name = caption;
        //    if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Invalid window identifier", "name");
        //    if (caption == null) caption = name;

        //    var winRef = GetWindow<Menu>(owner, name, caption);

        //    var content = winRef.Content ?? new Menu();
        //    if (!string.IsNullOrEmpty(message)) content.Title = message;
        //    content.MenuItems.Add(itemSource);

        //    var win = winRef.Window ?? owner.CreateWindow(caption, WindowButtons.Ok);
        //    win.Name = name;
        //    win.Content = content;
        //    win.Resizeable = false;
        //    win.HideOnClose = true;
        //    win.Modal = true;
        //    win.ButtonPressed = (w, e) => { if (!Controller.Authentication.IsLoggedIn) e.Cancelled = true; };

        //    return new WindowReference<Menu>(win);
        //}

        //// --------------------------------------------------------------------------------------------------

        ///// <summary>
        ///// Returns the specified window, creating it only once, and returning the same instance for each subsequent call.
        ///// </summary>
        ///// <param name="name">Window identifier (leave 'null' to use the default caption value).</param>
        ///// <param name="caption">Window caption (set to 'null' to use the name identifier as the caption).</param>
        //public WindowReference<PropertyEditor> GetPropertyEditorWindow(object owner, string name = null, string caption = "Properties")
        //{
        //    if (name == null) name = caption;
        //    if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Invalid window identifier", "name");
        //    if (caption == null) caption = name;

        //    var winRef = GetWindow<PropertyEditor>(owner, name, caption);

        //    var contentCtrl = winRef.Content ?? new PropertyEditor();
        //    var win = winRef.Window ?? owner.CreateWindow(caption, WindowButtons.OkCancel);
        //    win.Name = name;
        //    win.Content = contentCtrl;
        //    win.Resizeable = true;
        //    win.CloseButtonEnabled = true;
        //    win.HideOnClose = true;

        //    return new WindowReference<PropertyEditor>(win);
        //}

        // --------------------------------------------------------------------------------------------------

    }

    // ######################################################################################################

    /// <summary>
    /// A wrapper used with returning window references, which simply provides a means to type cast the 
    /// window's content object as needed.
    /// </summary>
    /// <typeparam name="ContentType"></typeparam>
    public struct WindowReference<ContentType> where ContentType : class
    {
        /// <summary>
        /// Registered window instance.
        /// </summary>
        public readonly Window Window;
        /// <summary>
        /// Strongly-typed content reference.
        /// </summary>
        public ContentType Content { get { return Window != null ? Window.Content as ContentType : null; } }

        public WindowReference(Window window) { Window = window; }

        public WindowReference<TContent> As<TContent>() where TContent : class
        { return new WindowReference<TContent>(Window); }

        /// <summary>
        /// Returns 'true' if a window reference exists, and 'false' otherwise.
        /// </summary>
        public bool Ok { get { return Window != null; } }
    }

    // ######################################################################################################
}
