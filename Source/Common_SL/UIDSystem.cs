using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Shared.CoreUtilities;

namespace Shared.Silverlight.Attachables.UIDSystem
{
    public static class U
    {
        // --------------------------------------------------------------------------------------------------

        public class ElementIDWrapper
        {
            // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

            /// <summary>
            /// The ID text used with this UID entry.
            /// </summary>
            public string ID
            {
                get
                {
                    if (_ID != null && _ID.Contains("*"))
                    {
                        if (HostElement == null)
                            throw new InvalidOperationException("U.ID: Host cannot be null if '*' is specified (" + _ID + ").");
                        return _ID.Replace("*", Objects.GetPropertyOrFieldValue<string>(HostElement, "Name") ?? "");
                    }
                    return _ID;
                }
                private set { _ID = (value != null ? value.ToLower() : null); }
            }
            string _ID;

            /// <summary>
            /// The host is the element which has this ID attached.
            /// </summary>
            public DependencyObject HostElement { get; internal set; }

            /// <summary>
            /// The next parent element which has a UID assigned.
            /// If there is no valid parent found, then the global root instance is used (U.Root).
            /// </summary>
            public ElementIDWrapper Parent { get; internal set; }

            /// <summary>
            /// The child IDs on elements below this host element.
            /// </summary>
            List<ElementIDWrapper> _ChildIDList = new List<ElementIDWrapper>(1);

            public bool IsGlobal
            { get { return (!string.IsNullOrEmpty(ID) && ID[0] == '#'); } }

            // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

            public ElementIDWrapper(string id, DependencyObject host) { ID = id; HostElement = host; }

            // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

            public ElementIDWrapper GetChildID(string id)
            {
                foreach (var idItem in _ChildIDList)
                    if (idItem.ID == id) return idItem;
                return null;
            }

            /// <summary>
            /// Checks to see if an instance of "ID" already exists.
            /// </summary>
            public bool HasIDAsChild(ElementIDWrapper id)
            {
                return (_ChildIDList.IndexOf(id) >= 0);
            }

            internal void _RemoveChildID(ElementIDWrapper id)
            {
                var i = _ChildIDList.IndexOf(id);
                if (i >= 0)
                {
                    _ChildIDList[i].Parent = null;
                    _ChildIDList.RemoveAt(i); // (Note: The child still has its own children, and is not removed from the link list)
                }
            }

            internal void _AddChildID(ElementIDWrapper id)
            {
                if (id != null && !HasIDAsChild(id))
                {
                    if (!id.IsGlobal)
                        _ReconcileIDWithExistingChildIDs(id);
                    ElementIDWrapper existingID = GetChildID(id.ID);
                    if (existingID != null)
                        throw new InvalidOperationException("U.ID: Another ID already exists with the ID '" + id.ID + "'.");
                    _ChildIDList.Add(id);
                    id.Parent = this;
                }
            }

            bool _ReconcileIDWithExistingChildIDs(ElementIDWrapper id)
            {
                if (id.HostElement is FrameworkElement)
                {
                    bool childrenMoved = false;

                    for (int childIndex = _ChildIDList.Count - 1; childIndex >= 0; childIndex--)
                    {
                        var childID = _ChildIDList[childIndex];
                        if (childID.HostElement is FrameworkElement
                            && VisualTree.ContainsElementInHierarchy(childID.HostElement, id.HostElement))
                        {
                            _MoveChildIDToParent(childIndex, id);
                            childrenMoved = true;
                        }
                    }

                    return childrenMoved;
                }
                return false;
            }

            void _MoveChildIDToParent(int childIndex, ElementIDWrapper newParent)
            {
                ElementIDWrapper childID = _ChildIDList[childIndex];
                // ... remove child ID and add the new parent ID ...
                _ChildIDList.RemoveAt(childIndex);
                childID.Parent = newParent;
                newParent._ChildIDList.Add(childID);
            }

            // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

            internal void _RemoveFromLinkList()
            {
                var thisParent = Parent;
                if (Parent != null) // (remove from parent, if exists)
                    Parent._RemoveChildID(this); // (Note: Sets 'Parent' to null)
                foreach (var idItem in _ChildIDList) // (relink parent on child IDs to this ID parent)
                {
                    idItem.Parent = thisParent; // (set new parent on child ID)
                    thisParent._AddChildID(idItem); // (register the child ID with the new parent)
                }
                _ChildIDList.Clear();
            }

            // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

            /// <summary>
            /// Traverses "self" and the children to find a matching ID instance at the end of the specified path.
            /// If the path cannot be resolved from this ID instance, then 'null' is returned.
            /// The first path part must match this ID instance.
            /// </summary>
            /// <param name="pathParts">Each ID name of the path in the sequence (i.e. mygrid.mydatagrid.mydgcolumn..., split into an array).</param>
            public ElementIDWrapper LocateIDByPath(string[] pathParts, int firstPartIndex, Type expectedHostType)
            {
                if (pathParts == null) return null;
                if (firstPartIndex < 0 || firstPartIndex >= pathParts.Length)
                    throw new IndexOutOfRangeException("U.ID.LocateIDByPath(): 'firstPartIndex' (" + firstPartIndex + ") is out of bounds (0..." + (pathParts.Length - 1) + ").");
                if (pathParts[firstPartIndex] != ID) return null;

                if (firstPartIndex == pathParts.Length - 1) return this; // (if this ID is a match, and there are no more parts, return this instance)

                ElementIDWrapper foundID = null;
                foreach (var id in _ChildIDList)
                {
                    foundID = id.LocateIDByPath(pathParts, firstPartIndex + 1, expectedHostType);
                    if (foundID != null && (expectedHostType == null || foundID.HostElement != null && expectedHostType.IsAssignableFrom(foundID.HostElement.GetType())))
                        break;
                }

                return foundID;
            }
            public ElementIDWrapper LocateIDByPath(string[] pathParts, Type expectedHostType) { return LocateIDByPath(pathParts, 0, expectedHostType); }
            public ElementIDWrapper LocateIDByPath(string[] pathParts) { return LocateIDByPath(pathParts, 0, null); }

            // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

            public override string ToString() { return ID ?? ""; }

            public static implicit operator string(ElementIDWrapper id)
            { return id != null ? id.ToString() : null; }
            public static implicit operator ElementIDWrapper(string id)
            { return id != null ? new ElementIDWrapper(id, null) : null; }

            // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        }

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// A scope begins for each object marked with an id - all child elements are then associated with it.
        /// All child objects with UIDs are related to the root object's scope.
        /// An ID can be global if '#' is pretended to the name.
        /// To use UIDs for non-framework elements, or to be able to find objects within
        /// a constructor (which depends on creation order), use global IDs - but be careful, since global IDs
        /// are only good for application-wide presence (i.e. not with templates).
        /// Scope names, specified after the root using "@" (i.e. "root@name"), help identify a specific
        /// root scope when locating elements.
        /// <para>
        /// Format: "#AGlobalID", "root@UserControlRoot" // Here, '#AGlobalID' is a global ID, and '@UserControlRoot' is a scope name.
        /// </para>
        /// </summary>

        public static DependencyProperty IDProperty = DependencyProperty.RegisterAttached(
            "id", typeof(string), typeof(U), new PropertyMetadata(null, _OnIDChanged));

        public static void Setid(DependencyObject d, string value)
        { d.SetValue(IDProperty, value); }
        public static string Getid(DependencyObject d)
        { return (string)d.GetValue(IDProperty); }

        static DependencyProperty ElementIDWrapperProperty = DependencyProperty.RegisterAttached(
           "ElementIDWrapper", typeof(ElementIDWrapper), typeof(U), new PropertyMetadata(null));

        static void SetElementIDWrapper(DependencyObject d, ElementIDWrapper value)
        { d.SetValue(ElementIDWrapperProperty, value); }
        static ElementIDWrapper GetElementIDWrapper(DependencyObject d)
        { return (ElementIDWrapper)d.GetValue(ElementIDWrapperProperty); }

        // ----------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Contains root elements that do not have a root parent, and those with global UIDs.
        /// </summary>
        public static readonly ElementIDWrapper Root = new ElementIDWrapper("root", null);

        static U() { }

        // ----------------------------------------------------------------------------------------------------------------------

        public static ElementIDWrapper UID(this DependencyObject subject) { return GetElementIDWrapper(subject); }

        static void _OnIDChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == e.OldValue) return; // (same object instance, so ignore)

            ElementIDWrapper oldID = GetElementIDWrapper(sender);
            ElementIDWrapper newID = (string)e.NewValue;

            if (oldID != null)
            {
                if (newID == null || newID.ID != oldID.ID)
                {
                    // ... unhook old ID from existing chain ...

                    oldID._RemoveFromLinkList();

                    // (the new linkage info on the new ID will be determined once the element parent is available...)
                }
            }

            if (newID != null)
            {
                newID.HostElement = sender;

                SetElementIDWrapper(sender, newID);

                if (sender is FrameworkElement) // (Note: only framework elements can be traversed for their root parents)
                    Dispatching.ElementPreLoadDispatch(
                        (FrameworkElement)sender,
                        Dispatching.Priority.High, MethodBase.GetCurrentMethod().Name, sender,
                        new Action<DependencyObject, FrameworkElement>(AddUIDObject), 
                        sender, null);
            }
        }

        // ----------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Used for delay-load UID assignments (for framework elements)
        /// </summary>
        static void _U_Element_Loaded(object sender, RoutedEventArgs e) // (occurs once the element is added to the visual tree)
        {
            var element = (FrameworkElement)sender;
            element.Loaded -= _U_Element_Loaded;
            AddUIDObject(element, null);
        }

        /// <summary>
        /// Add an object with a UID to the UID system.
        /// (Note: Normally, this is done automatically when setting the U.id property.)
        /// </summary>
        /// <param name="uidObject">The object to add (must already have a UID assigned).</param>
        /// <param name="owner">Required for objects that are NOT framework elements themselves. This aids in resolving the root object of the scope (Example: For DataGridColumn items, pass in the owning DataGrid instance).</param>
        public static void AddUIDObject(DependencyObject uidObject, FrameworkElement parent)
        {
            if (uidObject == null)
                throw new ArgumentNullException("uidObject");

            FrameworkElement uidObjectFEOwner; // (holds the framework element owner for non-FrameworkElement objects)
            if (parent == null && uidObject is FrameworkElement)
                uidObjectFEOwner = (FrameworkElement)uidObject; // (framework element focus is on 'self')
            else
                uidObjectFEOwner = parent;

            var id = uidObject.UID();

            if (id == null)
                throw new InvalidOperationException("No UID is specified for object " + uidObject.GetType().FullName + ".");
            if (string.IsNullOrEmpty(id.ID))
                throw new InvalidOperationException("U.id cannot be empty - are you missing an 'x:Name' attribute? (control: " + uidObject.GetType().Name + ")");

            if (id.Parent != null) return; // (already handled)

            if (!(uidObjectFEOwner is FrameworkElement)) return;
            // (Note: non-visual element, with no events ... nothing more can be done - a parent framework element is
            // responsible for reconciling this. Example: DataGrid with DataGridColumn items)

            // ... if there is no visual parent, then there's no way to determine the root object until object is loaded ...

            if (VisualTreeHelper.GetParent(uidObjectFEOwner) == null)
            {
                ((FrameworkElement)uidObjectFEOwner).Loaded -= _U_Element_Loaded; // (just in case)
                ((FrameworkElement)uidObjectFEOwner).Loaded += _U_Element_Loaded;
                return; // (must wait for object to load for some reason [should never happen, but just in case]...)
            }

            // ... get the parent ID from the parent if specified, otherwise search for it ...
            ElementIDWrapper parentID = parent != null ? parent.UID() : GetParentID(uidObjectFEOwner); // (use frame work owner, unless the object itself is a framework element)

            // ... the default parent is always the root ID instance ...
            if (parentID == null) parentID = Root;

            parentID._AddChildID(id);

            // ... scan object for list properties that contain other DependencyObject items that are not FrameworkElement
            // objects (such as data grid columns) - these stand alone until added via their parents ...
            // (Note: This requires a UID on the parent as well!)

            _ScanForChildUIDs(uidObject);
        }

        static void _ScanForChildUIDs(DependencyObject uidObject)
        {
            if (uidObject is FrameworkElement)
            {
                var properties = uidObject.GetType().GetProperties();
                foreach (var prop in properties)
                    if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType))
                    {
                        var enumeration = (IEnumerable)prop.GetValue(uidObject, null);
                        if (enumeration != null)
                            foreach (var item in enumeration)
                                if (item is DependencyObject && !(item is FrameworkElement) && ((DependencyObject)item).UID() != null)
                                    AddUIDObject((DependencyObject)item, (FrameworkElement)uidObject);
                    }
            }
        }

        public static ElementIDWrapper GetParentID(DependencyObject elementStart)
        {
            if (elementStart == null)
                throw new ArgumentNullException("elementStart");

            ElementIDWrapper id = GetElementIDWrapper(elementStart);

            if (id != null && id.Parent != null)
                return id.Parent;

            if (!(elementStart is FrameworkElement))
                return null; // (framework element required for the following)

            // ... no parent found, so scan visual tree for a parent ID ...

            DependencyObject element = elementStart;
            ElementIDWrapper idFound = null;

            while (element != null)
            {
                element = VisualTreeHelper.GetParent(element);
                // ... end if element has no parent ...
                if (element == null) break; //?? || element is UserControl / should user control boundaries be respected?
                idFound = GetElementIDWrapper(element);
                if (idFound != null) break;
            }

            return idFound; // (returned element is either the closest parent element marked as "root", or the tree root element)
        }

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the object, of type 'T', using a UID path string, which is found in relation to the specified reference element.
        /// A "UID Path" string is simply a period delimited list of ID names that exist somewhere in the parent hierarchy.
        /// The parent hierarchy is simply searched for the first occurance of the string combination and desired element type.
        /// <para>
        /// Warning: Although an ID is associated with it's host immediately upon being attached, it is not registered with
        /// any parent element until after all elements are created, and before the "Loaded" event fires (for non-dynamically
        /// loaded XAML).
        /// Locating objects within constructors will only work with globally unique IDs, else 'null' will be returned.
        /// Warning: Globally unique IDs (which start with '#') should never be used in templates, or reuseable controls, but in the application scope only.
        /// </para>
        /// </summary>
        /// <typeparam name="T">Expected object type.</typeparam>
        /// <param name="referenceElement">An element to begin the ID search with, or simply 'null' for globally unique UIDs, or UIDs with scope IDs. (This parameter is ignored if 'uidPath' is a global UID.)</param>
        /// <param name="uidPath">
        /// A UID path string of the element to search for (example: "@#AppPage", "@mygrid.mydatagrid.mydgcolumn").
        /// Note: The "@" symbol prefix only helps to designate a string as a UID Path string, but is not required.
        /// </param>
        /// <returns>Requested object, of type 'T'.</returns>
        public static T GetObject<T>(this DependencyObject referenceElement, string uidPath) where T : DependencyObject
        {
            if (referenceElement == null)
                throw new ArgumentNullException("referenceElement");
            if (uidPath == null)
                throw new ArgumentNullException("uidPath");

            if (uidPath[0] == '@') uidPath = uidPath.Substring(1); // (the "@" symbol prefix only helps to flag a string as a UID Path string, but is not required)

            if (string.IsNullOrEmpty(uidPath))
                throw new InvalidOperationException("The UID Path '" + uidPath + "' is not valid: it cannot be empty.");

            if (uidPath.StartsWith("#"))
            {
                ElementIDWrapper id = Root.GetChildID(uidPath);
                return id != null ? (T)id.HostElement : null;
            }

            string[] uidPathParts = uidPath.Split('.');
            // ... make all lower case ...
            for (int i = 0; i < uidPathParts.Length; i++)
                uidPathParts[i] = uidPathParts[i].ToLower();

            bool startAtRoot = (uidPathParts[0] == "root");
            bool searchFromSelf = (uidPathParts[0].Trim() == "");

            ElementIDWrapper searchFromID = null;

            if (!startAtRoot)
            {
                searchFromID = GetElementIDWrapper(referenceElement);

                if (searchFromID == null)
                {
                    // ... element has no ID, search visual parents for an ID - throw error if not a FrameworkElement object ...

                    if (!(referenceElement is FrameworkElement))
                        throw new InvalidOperationException("U.GetObject<" + typeof(T).FullName + ">(...,'" + uidPath + "'): Reference element (" + referenceElement.GetType().FullName + ") has no ID, and thus must be a FrameworkElement object in order for it to be used as a reference.");
                    if (VisualTreeHelper.GetParent(referenceElement) == null)
                        throw new InvalidOperationException("U.GetObject<" + typeof(T).FullName + ">(...,'" + uidPath + "'): Reference element (" + referenceElement.GetType().FullName + ") has no visual parent (not loaded yet?).");

                    searchFromID = GetParentID(referenceElement);
                }
                else searchFromID = searchFromID.Parent; // (search from parent)
            }

            // ... if no ID can be found, default to the root ...

            if (searchFromID == null) searchFromID = Root;

            // ... process the specified UID Path string ...

            T objectFound = default(T);
            ElementIDWrapper idFound;

            if (searchFromSelf)
            {
                idFound = searchFromID.LocateIDByPath(uidPathParts, 1, typeof(T)); // (Search from index '1' to skip the first empty part)
                if (idFound != null)
                    objectFound = (T)idFound.HostElement;
            }
            else // (go up parent hierarchy to find the match)
            {
                while (searchFromID != null)
                {
                    idFound = searchFromID.LocateIDByPath(uidPathParts, typeof(T));
                    if (idFound != null)
                    {
                        objectFound = (T)idFound.HostElement;
                        break;
                    }
                    searchFromID = searchFromID.Parent;
                }
            }

            return objectFound; // (not found)
        }

        // --------------------------------------------------------------------------------------------------
    }
}
