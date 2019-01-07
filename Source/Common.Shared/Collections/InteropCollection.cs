#if (NETSTANDARD1_5 || NETSTANDARD1_6 || NETCOREAPP1_0 || DNXCORE50 || NETCORE45  || NETCORE451 || NETCORE50)
#define DOTNETCORE
#endif

// Purpose:
//   A cross-CLR collection object created to replace the limited features of
//   ObservableCollection<> between CLRs, such as the standard CLR and Silverlight.
//
// Features:
//   * No reallocations: Designed to never need to release and recreate the item list array.
//   * Ability to "assign" items from other collections, lists, and arrays.
//   * Ability to "wrap" other IList instances without the need to transfer items, effectively
//     enabling most of the InteropCollection features on the wrapped IList!
//   * Code supported in both the Silverlight CLR, and the standard .NET CLR.
//     (Both INotifyCollectionChanged and INotifyPropertyChanged interfaces implemented)
//   * An ICollectionView wrapper is also provided for use with data views, such as 
//     'DataGrid' in Silverlight.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel; // ObservableCollection<> is added via WindowsBase (WindowsBase.dll).
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;

#if SILVERLIGHT
// (This section is to provided client side compatibility)
namespace System.ComponentModel
{
    // Summary:
    //     Provides data for the System.ComponentModel.INotifyPropertyChanging.PropertyChanging
    //     event.
    //public class PropertyChangingEventArgs : EventArgs --- this is now included in SL 5.0
    //{
    //    // Summary:
    //    //     Initializes a new instance of the System.ComponentModel.PropertyChangingEventArgs
    //    //     class.
    //    //
    //    // Parameters:
    //    //   propertyName:
    //    //     The name of the property whose value is changing.
    //    // [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
    //    public PropertyChangingEventArgs(string propertyName) { PropertyName = propertyName; }

    //    // Summary:
    //    //     Gets the name of the property whose value is changing.
    //    //
    //    // Returns:
    //    //     The name of the property whose value is changing.
    //    public virtual string PropertyName { get; private set; }
    //}

    // Summary:
    //     Represents the method that will handle the System.ComponentModel.INotifyPropertyChanging.PropertyChanging
    //     event of an System.ComponentModel.INotifyPropertyChanging interface.
    //
    // Parameters:
    //   sender:
    //     The source of the event.
    //
    //   e:
    //     A System.ComponentModel.PropertyChangingEventArgs that contains the event
    //     data.
    //public delegate void PropertyChangingEventHandler(object sender, PropertyChangingEventArgs e); --- this is now included in SL 5.0

    // Summary:
    //     Notifies clients that a property value is changing.
    //public interface INotifyPropertyChanging --- this is now included in SL 5.0
    //{
    //    // Summary:
    //    //     Occurs when a property value is changing.
    //    event PropertyChangingEventHandler PropertyChanging;
    //}
}
#elif DOTNETCORE
namespace System.ComponentModel
{
    public delegate void CurrentChangingEventHandler(object sender, CurrentChangingEventArgs e);

public class CurrentChangingEventArgs : EventArgs
    {
        public CurrentChangingEventArgs() { }
        public CurrentChangingEventArgs(bool isCancelable) { _IsCancelable = isCancelable; }

        public bool Cancel { get; set; }
        public bool IsCancelable { get { return _IsCancelable; } }
        bool _IsCancelable;
    }
}
#endif

    namespace System.Collections.Specialized
{
    /// <summary>
    /// Describes the action that caused a System.Collections.Specialized.INotifyCollectionChanging.CollectionChanged event.
    /// </summary>
    public enum NotifyCollectionChangingAction
    {
        /// <summary>
        /// One or more items were added to the collection.
        /// </summary>
        Add = 0,
        /// <summary>
        /// One or more items were removed from the collection.
        /// </summary>
        Remove = 1,
        /// <summary>
        /// One or more items were replaced in the collection.
        /// </summary>
        Replace = 2,
        /// <summary>
        /// One or more items were moved within the collection.
        /// </summary>
        Move = 3,
        /// <summary>
        /// The content of the collection changed dramatically.
        /// </summary>
        Reset = 4,
    }

    public class NotifyCollectionChangingEventArgs : EventArgs
    {
        public NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction action) { Action = action; }
        public NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction action, IList changedItems) : this(action) { NewItems = changedItems; }
        public NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction action, object changedItem) : this(action) { NewItems = new object[] { changedItem }; }
        public NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction action, IList newItems, IList oldItems) : this(action, newItems) { OldItems = oldItems; }
        public NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction action, IList changedItems, int startingIndex) : this(action, changedItems) { NewStartingIndex = startingIndex; }
        public NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction action, object changedItem, int index) : this(action, changedItem) { NewStartingIndex = index; }
        public NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction action, object newItem, object oldItem) : this(action, new object[] { newItem }, new object[] { oldItem }) { }
        public NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction action, IList newItems, IList oldItems, int startingIndex) : this(action, newItems, oldItems) { NewStartingIndex = startingIndex; }
        public NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction action, IList changedItems, int index, int oldIndex) : this(action, changedItems) { NewStartingIndex = index; OldStartingIndex = oldIndex; }
        public NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction action, object changedItem, int index, int oldIndex) : this(action, new object[] { changedItem }) { NewStartingIndex = index; OldStartingIndex = oldIndex; }
        public NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction action, object newItem, object oldItem, int index) : this(action, new object[] { newItem }, new object[] { oldItem }) { NewStartingIndex = index; }

        public NotifyCollectionChangingAction Action { get; private set; }
        public IList NewItems { get; private set; }
        public int NewStartingIndex { get; private set; }
        public IList OldItems { get; private set; }
        public int OldStartingIndex { get; private set; }
    }

    public delegate void NotifyCollectionChangingEventHandler(object sender, NotifyCollectionChangingEventArgs e);

    public interface INotifyCollectionChanging
    {
        event NotifyCollectionChangingEventHandler CollectionChanging;
    }
}


namespace Common.CollectionsAndLists
{
    public partial class InteropCollection<T>
        : IList<T>, ICollection<T>, IEnumerable<T>,
        IList, ICollection, IEnumerable,
        INotifyCollectionChanging, // (new)
        INotifyCollectionChanged, // (WindowsBase.dll).
        INotifyPropertyChanging, // (WindowsBase.dll).
        INotifyPropertyChanged // (WindowsBase.dll).
    {
        // -----------------------------------------------------------------------------------------------

        private static int _DefaultAllocationStep = 8;
        public static int DefaultAllocationStep
        {
            get { return _DefaultAllocationStep; }
            set
            {
                if (value < 1)
                    throw new InvalidOperationException("InteropCollection<T>.DefaultAllocationStep Error: Step cannot be less than 1.");
                _DefaultAllocationStep = value;
            }
        }

        // -----------------------------------------------------------------------------------------------

        List<T[]> _itemArayList = new List<T[]>(1);
        IList _wrappedList = null; // (if set, this becomes the main focus of ALL operations)

        int _allocStep = DefaultAllocationStep;
        public int AllocStep
        {
            get { return _allocStep; }
            set
            {
                if (value < 1)
                    throw new InvalidOperationException("InteropCollection<T>.AllocStep Error: Step cannot be less than 1.");
                _allocStep = value;
            }
        }

        int _itemArayListInsertIndex = -1;
        int _itemArayItemInsertIndex = -1;
        int _totalItems = 0;
        int _totalStorage = 0;

        private bool _isReadOnly = false;

        /// <summary>
        /// If "out of bounds" exceptions are not desired, then set this to 'false'.
        /// </summary>
        public bool EnableOutOfBoundsException = true;

        // -----------------------------------------------------------------------------------------------
        // Named Index Call-backs

        /// <summary>
        /// If set, then this collection will support named indexes (not required for string collections).
        /// The function is expected to accept a reference to the item to test, and should return a string from the object to use as the index name.
        /// Any attempts to used named indexing on non-string collections without setting this call-back method will result in an exception error.
        /// <para>Note: The item passed to the call-back method is guaranteed to never be 'null'.</para>
        /// </summary>
        public Func<T, string> GetIndexNameCallback;

        /// <summary>
        /// Holds the code to execute for comparing equality between a given name, and a given collection item.
        /// The default code is sufficient in most cases, but is exposed to allow developer to customize it.
        /// Set this property to the 'IsIndexNameEqual' method to restore default functionality.
        /// </summary>
        public Func<string, T, bool> NameComparisonDeligate { get { return _NameComparisonDeligate ?? IsIndexNameEqual; } set { _NameComparisonDeligate = value; } }
        Func<string, T, bool> _NameComparisonDeligate;
        /// <summary>
        /// The default named index comparison method used when no 'NameComparisonDeligate' is specified.
        /// This default method is sufficient in most cases.
        /// Set 'NameComparisonDeligate' to this method to restore default functionality.
        /// </summary>
        public bool IsIndexNameEqual(string name, T item)
        {
            var name2 = GetIndexNameCallback(item); // (Warning: 'null' may be returned!!!)
            return CaseSensitiveIndexNames ? name == name2 : (name ?? "").ToLower() == (name2 ?? "").ToLower();
        }

        /// <summary>
        /// If set, this method is responsible for handling set operations.
        /// </summary>
        public Func<T, object, T> SetItemTranslation; // TODO: Needs testing.

        /// <summary>
        /// If 'false' (default), then the letter casing of index names is ignored.
        /// Warning: Changing this to 'true' AFTER items already exist may result in undefined errors.
        /// </summary>
        public bool CaseSensitiveIndexNames = false;

        /// <summary>
        /// If the 'GetIndexNameCallback' method fails to return a name matching any existing items when a name is required, this method will be called upon to return a custom error message.
        /// If not set, a default exception is thrown.
        /// <para>The call-back function is expected to create and return a custom error message using the given named index. To abort the exception and cause a -1 index to be returned, simply return a 'null' reference instead of a string.</para>
        /// </summary>
        public Func<string, string> NamedIndexNotFoundMessageCallback;

        /// <summary>
        /// If the 'GetIndexNameCallback' method returns a name matching any existing items during an 'Add()' or 'Insert()' operation, then this method will be called upon to return a custom error message.
        /// If not set, a default exception is thrown.
        /// <para>The call-back function is expected to create and return a custom error message using the given named index. If 'null' is returned, then duplicate items will be allowed and no exception will be generated.</para>
        /// </summary>
        public Func<string, string> DuplicateNamedIndexMessageCallback;

        /// <summary>
        /// If 'true', then adding duplicate named indexes are allowed (not recommended, as ambiguity sets in when
        /// this happens, and only the first item found is returned).
        /// This is mainly useful when there's a need to managed the collection by index number instead of names, but
        /// still allow searching by name.
        /// </summary>
        public bool IgnoreDuplicateNamedIndexes = false;

        // -----------------------------------------------------------------------------------------------
        // Modification Security

#if DOTNETCORE

        object _LockKey; // (the generated locking key)

        /// <summary>
        /// When this collection is first created, the list can be "locked" to prevent modifications from external
        /// assemblies.
        /// To lock the collection, simply call this method and store the returned key, or ignore it if not needed later.
        /// Once the collection is locked, the lock can only be removed by calling 'Unlock()'.
        /// </summary>
        public object Lock()
        {
            if (_LockKey != null)
                throw new InvalidOperationException("InteropCollection<T>: Invalid call to lock and already locked collection.");
            return _LockKey = new object();
        }

        /// <summary>
        /// When this collection is first created, the list can be "locked" to prevent modifications from external
        /// assemblies.
        /// To lock the collection, simply call 'EnableAssemblyLock()' from within the assembly that will "own" the
        /// collection.
        /// Once the collection is locked, the lock can only be removed by calling 'DisableAssemblyLock()' from within
        /// the assembly that created the lock, and the collection can only be modified from within the assembly that
        /// locked the collection.
        /// </summary>
        /// <returns>The collection instance to allow chaining calls.</returns>
        public InteropCollection<T> Unlock(object key)
        {
            if (key != _LockKey)
                throw new UnauthorizedAccessException("InteropCollection<T>: Incorrect key.");
            _LockKey = null;
            return this;
        }

        void _DoReadOnlyCheck()
        {
            if (_isReadOnly)
                throw new InvalidOperationException("InteropCollection<T>: Collection is read only.");

            if (_LockKey != null)
                throw new UnauthorizedAccessException("This collection is locked.");
        }

#else
        Assembly _AssemblyLock; // (the assembly where this list is allowed to be modified from)

        /// <summary>
        /// When this collection is first created, the list can be "locked" to prevent modifications from external
        /// assemblies.
        /// To lock the collection, simply call this method from within the assembly that will "own" the collection.
        /// Once the collection is locked, the lock can only be removed by calling 'DisableAssemblyLock()' from within
        /// the assembly that created the lock, and the collection can only be modified from within the assembly that
        /// locked the collection.
        /// </summary>
        public void EnableAssemblyLock()
        {
            var callingAssembly = Assembly.GetCallingAssembly();
            if (_AssemblyLock != null && _AssemblyLock != callingAssembly)
                throw new InvalidOperationException("InteropCollection<T>: Invalid call to lock for assembly '" + callingAssembly.FullName + "'; Already locked to assembly '" + _AssemblyLock.FullName + "'.");
            _AssemblyLock = callingAssembly;
        }

        /// <summary>
        /// When this collection is first created, the list can be "locked" to prevent modifications from external
        /// assemblies.
        /// To lock the collection, simply call 'EnableAssemblyLock()' from within the assembly that will "own" the
        /// collection.
        /// Once the collection is locked, the lock can only be removed by calling 'DisableAssemblyLock()' from within
        /// the assembly that created the lock, and the collection can only be modified from within the assembly that
        /// locked the collection.
        /// </summary>
        public void DisableAssemblyLock()
        {
            var callingAssembly = Assembly.GetCallingAssembly();
            if (_AssemblyLock != null)
            {
                if (_AssemblyLock != callingAssembly)
                    throw new InvalidOperationException("InteropCollection<T>: Cannot disable from calling assembly '" + callingAssembly.FullName + "'; Collection is locked to assembly '" + _AssemblyLock.FullName + "'.");
                _AssemblyLock = null;
            }
        }

        void _DoReadOnlyCheck(Assembly callingAssembly)
        {
            if (_isReadOnly)
                throw new InvalidOperationException("InteropCollection<T>: Collection is read only.");

            if (_AssemblyLock != null && _AssemblyLock != callingAssembly && callingAssembly != Assembly.GetExecutingAssembly()
                && !callingAssembly.FullName.StartsWith("System."))
                throw new InvalidOperationException("This collection can only be modified within the '" + _AssemblyLock.FullName + "' assembly.");
        }

#endif
        // -----------------------------------------------------------------------------------------------


        public InteropCollection(T[] array, int initialCapacity, int allocStep)
        {
            if (initialCapacity < 0)
                throw new InvalidOperationException("InteropCollection<T>: Initial capacity cannot be less than 0.");

            AllocStep = allocStep;

            if (initialCapacity > 0)
                _Growing(initialCapacity);

            if (array != null)
                Assign(array);
        }
        public InteropCollection(int initialCapacity, int allocStep) : this(null, initialCapacity, allocStep) { }
        public InteropCollection(int initialCapacity) : this(initialCapacity, DefaultAllocationStep) { }
        public InteropCollection() : this(1) { }

        public InteropCollection(IList<T> items) : this(items.ToArray(), 0, DefaultAllocationStep) { }

        public InteropCollection(IEnumerable<T> items, int count)
            : this(count)
        {
            Assign(items, count);
        }
        public InteropCollection(IEnumerable<T> items) : this(items, items.Count()) { }

        public InteropCollection(InteropCollection<T> collection) : this(0) { Assign(collection); }


        // -----------------------------------------------------------------------------------------------

        public static implicit operator InteropCollection<T>(ObservableCollection<T> collection)
        {
            return new InteropCollection<T>(collection, collection.Count);
        }

        public static implicit operator ObservableCollection<T>(InteropCollection<T> collection)
        {
            return Collections.ToObservableCollection<T>(collection);
        }

        // -----------------------------------------------------------------------------------------------

        /// <summary>
        /// Clears the collection and assigns new items.
        /// </summary>
        /// <param name="collection">New items, which entirely replace existing items.</param>
        public void Assign(InteropCollection<T> collection)
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );
            if (collection != null)
                Assign((IEnumerable<T>)collection);
        }

        /// <summary>
        /// Clears the collection and assigns new items.
        /// </summary>
        /// <param name="items">New items, which entirely replace existing items.</param>
        /// <param name="count">Number of items to copy, or 0 to copy everything. </param>
        public void Assign(IEnumerable<T> items, int count)
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );
            if (items != null)
            {
                // ... get list of items to add FIRST, as the enumerator may actually 
                // be referencing 'this' collection...
                List<T> itemsToAdd = new List<T>(items);

                if (count > 0)
                    Assign(itemsToAdd, count);
                else
                    Assign(itemsToAdd); // (slightly faster in this case)
            }
        }

        /// <summary>
        /// Clears the collection and assigns new items.
        /// </summary>
        /// <param name="items">New items, which entirely replace existing items.</param>
        public void Assign(IEnumerable<T> items)
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );
            Assign(items, -1);
        }

        /// <summary>
        /// Clears the collection and assigns new items.
        /// </summary>
        /// <param name="items">New items, which entirely replace existing items.</param>
        public void Assign(IList<T> items)
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );
            if (items != null)
            {
                if (CollectionChanging != null)
                    _NotifiyCollectionChanging(NotifyCollectionChangingAction.Reset, null, null, 0);

                _Clear(0); // (delete everything and start over; _totalStorage == 0 after this.)

                if (_wrappedList != null)
                {
                    foreach (T item in items)
                        _wrappedList.Add(item);
                }
                else
                {
                    _itemArayList.Add(items.ToArray()); // (_Growing() will use the first array found if _totalStorage == 0)
                    _Growing(items.Count); // (reinitialize)
                }

                if (CollectionChanged != null)
                    _NotifiyCollectionChanged(NotifyCollectionChangedAction.Reset, null, null, 0);
            }
        }

        /// <summary>
        /// Clears the collection and assigns new items.
        /// </summary>
        /// <param name="items">New items, which entirely replace existing items.</param>
        /// <param name="count">Number of items to copy, or 0 to copy everything. </param>
        public void Assign(IList<T> items, int count)
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );
            if (items != null)
            {
                if (CollectionChanging != null)
                    _NotifiyCollectionChanging(NotifyCollectionChangingAction.Reset, null, null, 0);

                _Clear(0); // (delete everything and start over)
                _Growing(items.Count); // (reinitialize)

                foreach (T item in items)
                {
                    Add(item);
                    count--;
                    if (count == 0) break;
                }

                if (CollectionChanged != null)
                    _NotifiyCollectionChanged(NotifyCollectionChangedAction.Reset, null, null, 0);
            }
        }
        public void Assign(T[] items)
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );
            if (items != null)
            {
                if (CollectionChanging != null)
                    _NotifiyCollectionChanging(NotifyCollectionChangingAction.Reset, null, null, 0);

                _Clear(0); // (delete everything and start over)

                if (_wrappedList != null)
                {
                    foreach (T item in items)
                        _wrappedList.Add(item);
                }
                else
                {
                    // ... don't store the list reference, make a copy ...
                    T[] _items = new T[items.Length];
                    for (int i = 0; i < items.Length; i++)
                        _items[i] = items[i];
                    _itemArayList.Add(_items); // (_Growing() will use the first array found if _totalStorage == 0)
                    _Growing(items.Length); // (reinitialize)
                }

                if (CollectionChanged != null)
                    _NotifiyCollectionChanged(NotifyCollectionChangedAction.Reset, null, null, 0);
            }
        }
        /// <summary>
        /// Assigns 'count' items from the specified array, starting from 'startIndex'.
        /// The number of new items in the collection will always equal the specified count, as long as count is greater than 0.
        /// If count is greater than the number of items, then the rest of the collection will be set to 'default(T)'.
        /// If count is 0 or negative, then the count is equal to the number of items, minus the count value (i.e. 0 == copy all).
        /// </summary>
        public void Assign(T[] items, int startIndex, int count)
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );
            if (items != null)
            {
                if (CollectionChanging != null)
                    _NotifiyCollectionChanging(NotifyCollectionChangingAction.Reset, null, null, 0);

                _Clear(0); // (delete everything and start over)

                if (count <= 0) count = items.Length + count; // (subtracted from end)
                if (count <= -items.Length) count = 0;

                if (_wrappedList != null)
                {
                    for (int i = 0; i < count; i++)
                        _wrappedList.Add((startIndex + i < items.Length ? items[startIndex + i] : default(T)));
                }
                else
                {
                    T[] subItems = new T[count];
                    for (int i = 0; i < count; i++)
                        subItems[i] = startIndex + i < items.Length ? items[startIndex + i] : default(T);
                    _itemArayList.Add(subItems); // (_Growing() will use the first array found if _totalStorage == 0)
                    _Growing(items.Length); // (reinitialize)
                }

                if (CollectionChanged != null)
                    _NotifiyCollectionChanged(NotifyCollectionChangedAction.Reset, null, null, 0);
            }
        }

        // -----------------------------------------------------------------------------------------------

        static bool _AreReferenceTypeItems = typeof(T).GetTypeInfo().IsClass || typeof(T).GetTypeInfo().IsInterface;

        /// <summary>
        /// Wraps the collection around another IList supported instance.
        /// Any existing items in the collection "wrapper" are cleared first before wrapping the given list.
        /// All future operations are directed to the wrapped list as the new storage target.
        /// <para>Note: If the IList instance is modified externally, it doesn't affect the events of this collection,
        /// as it is viewed ONLY as an external storage medium and nothing else.</para>
        /// <para>Warning: The items are directly cast to the same type as this collection "wrapper", so
        /// any types that cannot be cast during a read/write operation will generate an exception error.</para>
        /// </summary>
        public InteropCollection<T> WrapObjectList(IList list)
        {
            if (_wrappedList == null)
                Clear(); // (only clear native storage before setting wrapped list)

            // ... remove events from any existing items ...
            if (_wrappedList != null)
            {
                if (_wrappedList is INotifyCollectionChanging)
                    ((INotifyCollectionChanging)_wrappedList).CollectionChanging -= _WrappedList_CollectionChanging;
                if (_wrappedList is INotifyCollectionChanged)
                    ((INotifyCollectionChanged)_wrappedList).CollectionChanged -= _WrappedList_CollectionChanged;

                if (!_AreReferenceTypeItems)
                    foreach (T item in _wrappedList)
                    {
                        _UnattachPropertyChangedBridge(item as INotifyPropertyChanged);
                        _UnattachPropertyChangingBridge(item as INotifyPropertyChanging);
                    }
                else
                    foreach (object item in _wrappedList)
                        if (item != null)
                        {
                            _UnattachPropertyChangedBridge(item as INotifyPropertyChanged);
                            _UnattachPropertyChangingBridge(item as INotifyPropertyChanging);
                        }
            }

            _wrappedList = list;

            if (_wrappedList != null)
            {
                if (_wrappedList is INotifyCollectionChanging)
                    ((INotifyCollectionChanging)_wrappedList).CollectionChanging += _WrappedList_CollectionChanging;
                if (_wrappedList is INotifyCollectionChanged)
                    ((INotifyCollectionChanged)_wrappedList).CollectionChanged += _WrappedList_CollectionChanged;

                // ... attach/reattach property changed event to new items ...
                if (!_AreReferenceTypeItems)
                    foreach (T item in _wrappedList)
                    {
                        _UnattachPropertyChangedBridge(item as INotifyPropertyChanged); // (just to be safe)
                        _UnattachPropertyChangingBridge(item as INotifyPropertyChanging);
                        _AttachPropertyChangedBridge(item as INotifyPropertyChanged); // (attaches the "changing" bridge event bridge also!)
                        _AttachPropertyChangingBridge(item as INotifyPropertyChanging);
                    }
                else
                    foreach (object item in _wrappedList)
                        if (item != null)
                        {
                            _UnattachPropertyChangedBridge(item as INotifyPropertyChanged); // (just to be safe)
                            _UnattachPropertyChangingBridge(item as INotifyPropertyChanging);
                            _AttachPropertyChangedBridge(item as INotifyPropertyChanged); // (attaches the "changing" bridge event bridge also!)
                            _AttachPropertyChangingBridge(item as INotifyPropertyChanging);
                        }
            }

            return this;
        }

        void _WrappedList_CollectionChanging(object sender, NotifyCollectionChangingEventArgs e)
        {
            if (CollectionChanging != null)
                CollectionChanging(sender, e);
        }

        void _WrappedList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
                CollectionChanged(sender, e);
        }

        /// <summary>
        /// Wraps the collection around another IList supported instance.
        /// Any existing items in the collection "wrapper" are cleared first before wrapping the given list.
        /// All future operations are directed to the wrapped list as the new storage target.
        ///<para>Note: If the IList instance is modified externally, it doesn't affect the events of this collection,
        /// as it is viewed ONLY as an eternal storage medium and nothing else.</para>
        /// </summary>
        public InteropCollection<T> WrapList(IList<T> list) { return WrapObjectList((IList)list); }

        /// <summary>
        /// If set, then all operations are redirected to the specified list.
        /// </summary>
        public IList WrappedList { get { return _wrappedList; } set { WrapObjectList(value); } }

        // -----------------------------------------------------------------------------------------------

        /// <summary>
        /// Allocates more item lists if needed based on the specified amount of anticipated additions.
        /// If adding a lot of items at one time, make sure to call this method first.
        /// </summary>
        void _Growing(int amount)
        {
            if (amount > 0 && _wrappedList == null)
            {
                _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );

                if (_totalStorage == 0)
                {
                    if (_itemArayList.Count == 0) // (this will be > 0 if the constructor or an Assign() method sets an initial list first)
                    {
                        _itemArayList.Add(new T[amount]);
                        _itemArayListInsertIndex = 0;
                        _totalItems = 0;
                    }
                    else
                    {
                        _itemArayListInsertIndex = 1;
                        _totalItems = _itemArayList[0].Length;
                    }
                    _itemArayItemInsertIndex = 0;
                    _totalStorage = _itemArayList[0].Length;
                }
                else
                {
                    if (_totalItems + amount > _totalStorage)
                    {
                        int availableSpaceInCurrentArray = (_totalItems < _totalStorage) ? (_itemArayList.Last().Length - _itemArayItemInsertIndex) : (0);
                        int numberOfListsNeeded = (amount - availableSpaceInCurrentArray + (_allocStep - 1)) / _allocStep;
                        _totalStorage += numberOfListsNeeded * _allocStep;
                        for (; numberOfListsNeeded > 0; numberOfListsNeeded--)
                            _itemArayList.Add(new T[_allocStep]);
                    }
                }
            }
        }

        // -----------------------------------------------------------------------------------------------

        void _incInsertIndexes()
        {
            if (_itemArayListInsertIndex == -1)
            {
                _itemArayListInsertIndex = 0;
                _itemArayItemInsertIndex = 0;
            }
            else
            {
                _itemArayItemInsertIndex++;
                if (_itemArayItemInsertIndex >= _itemArayList[_itemArayListInsertIndex].Length)
                {
                    _itemArayItemInsertIndex = 0;
                    _itemArayListInsertIndex++;
                }
            }
            _totalItems++;
        }
        void _decInsertIndexes()
        {
            if (_itemArayListInsertIndex >= 0)
            {
                _itemArayItemInsertIndex--;
                if (_itemArayItemInsertIndex < 0)
                {
                    _itemArayListInsertIndex--;
                    if (_itemArayListInsertIndex >= 0)
                        _itemArayItemInsertIndex = _itemArayList[_itemArayListInsertIndex].Length - 1;
                }
                _totalItems--;
            }
        }

        // -----------------------------------------------------------------------------------------------

        #region IList<T> Members

        public int IndexOf(T item)
        {
            T _item;
            for (int i = 0; i < Count; i++)
            {
                _item = _GetItem(i);
                if (_item != null && _item.Equals(item))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Get an item's numerical index by its named index.
        /// The call-back function 'GetIndexNameCallback' must be set for this to work.
        /// </summary>
        public int IndexOf(string indexName, bool throwNotFoundException)
        {
            if (typeof(T) != typeof(string) && GetIndexNameCallback == null)
                throw new InvalidOperationException("InteropCollection<T>.IndexOf(indexName): Named indexing is not supported by this collection ('GetIndexNameCallback' not set - required for named indexing of non-string type collections).");

            if (indexName != null)
            {
                if (!CaseSensitiveIndexNames) indexName = indexName.ToLower();

                T _item;

                for (int i = 0; i < Count; i++)
                {
                    _item = _GetItem(i);
                    if (_item != null) // (skip null items [they never have a name])
                        if (typeof(T) == typeof(string)) // (strings items as named indexes are supported by default)
                        {
                            if (CaseSensitiveIndexNames)
                            { if ((string)(object)_item == indexName) return i; }
                            else
                            { if (((string)(object)_item).ToLower() == indexName) return i; }
                        }
                        else if (NameComparisonDeligate(indexName, _item))
                            return i;
                }
            }

            if (throwNotFoundException)
                if (NamedIndexNotFoundMessageCallback != null)
                {
                    var msg = NamedIndexNotFoundMessageCallback(indexName);
                    if (msg != null)
                        throw new IndexOutOfRangeException(msg);
                }
                else
                    throw new IndexOutOfRangeException("InteropCollection<T>.IndexOf(indexName): Named index ['" + indexName + "'] not found.");

            return -1;
        }
        public int IndexOf(string indexName) { return IndexOf(indexName, false); }

        /// <summary>
        /// Returns the first instance of type 'T' that matches the specified name, or 'default(T)' if not found.
        /// <para>For example, if type 'T' is a class based object, and the object doesn't exist, then 'default(T)' resolves to 'null'.</para>
        /// </summary>
        /// <param name="name">For name indexed items, this is an item's index name to find.</param>
        public T GetNamedItem(string name)
        {
            var i = IndexOf(name, false);
            if (i >= 0) return this[i];
            return default(T);
        }

        public void Insert(int index, T item)
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );

            if (index != 0 && index != Count && !_AssertItemRange(index))
                return;

            _DoDuplicateIndexNameCheck(item);

            if (CollectionChanging != null)
                _NotifiyCollectionChanging(NotifyCollectionChangingAction.Add, item, null, index);

            if (_wrappedList != null)
                _wrappedList.Insert(index, item);
            else
            {
                _Growing(1);

                int i = _totalItems;

                _incInsertIndexes();

                while (i > index)
                { _SetItem(i, _GetItem(i - 1)); i--; }
                _SetItem(index, item);
            }

            if (CollectionChanged != null)
                _NotifiyCollectionChanged(NotifyCollectionChangedAction.Add, item, null, index);
        }

        public void RemoveAt(int index)
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );

            if (!_AssertItemRange(index))
                return;

            // ... move all other items down, replacing the item to be removed ...

            T itemRemoved = _GetItem(index);

            if (CollectionChanging != null)
                _NotifiyCollectionChanging(NotifyCollectionChangingAction.Remove, null, itemRemoved, index);

            if (_wrappedList != null)
                _wrappedList.RemoveAt(index);
            else
            {
                int i = index;
                while (i < _totalItems - 1)
                { _SetItem(i, _GetItem(i + 1)); i++; }
                _SetItem(i, default(T));

                _decInsertIndexes();
            }

            if (CollectionChanged != null)
                _NotifiyCollectionChanged(NotifyCollectionChangedAction.Remove, null, itemRemoved, index);
        }

        private bool _AssertItemRange(int index)
        {
            if (index < 0 || index >= Count)
            {
                if (EnableOutOfBoundsException)
                    throw new IndexOutOfRangeException("InteropCollection<T>: Index out of bounds.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Get/set item via it's index.
        /// </summary>
        public T this[int index]
        {
            get { return _GetItem(index); }
            set
            {
                _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );
                _SetItem(index, value, true);
            }
        }

        /// <summary>
        /// Set item via it's reference.
        /// </summary>
        public T this[T index]
        {
            // (no point in this); get { return _GetItem(IndexOf(index)); }
            set // (allows replacing an object via its reference)
            {
                _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );
                _SetItem(IndexOf(index), value, true);
            }
        }

        /// <summary>
        /// Get/set item via it's named index.
        /// </summary>
        public T this[string index]
        {
            get { return _GetItem(IndexOf(index, true)); }
            set
            {
                _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );
                _SetItem(IndexOf(index, true), value, true);
            }
        }

        /// <summary>
        /// Gets an item at the specified index.
        /// </summary>
        protected T _GetItem(int index)
        {
            if (!_AssertItemRange(index))
                return default(T); // (out of bounds, and exception disabled, so return defaults)

            if (_wrappedList != null)
                if (_AreReferenceTypeItems)
                    return (_wrappedList[index] is T) ? (T)_wrappedList[index] : default(T); /*return null on invalid cast*/
                else
                    return (T)_wrappedList[index];

            int firstListLength = _itemArayList[0].Length;

            if (index < firstListLength)
                return _itemArayList[0][index];

            int ofsDiff = index - firstListLength;
            int listIndex = 1 + ofsDiff / _allocStep;
            int arrayIndex = ofsDiff % _allocStep;

            return _itemArayList[listIndex][arrayIndex];
        }
        /// <summary>
        /// Sets an item at the specified index.
        /// </summary>
        protected void _SetItem(int index, T value, bool allowCollectionChangedEvent)
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );
            if (!_AssertItemRange(index))
                return; // (out of bounds, and exception disabled, so ignore)

            T itemReplaced;

            if (_wrappedList != null)
            {
                itemReplaced = (T)_wrappedList[index];
                if (SetItemTranslation != null)
                {
                    T translatedItem = SetItemTranslation(itemReplaced, value);
                    if (Objects.Equals(translatedItem, itemReplaced)) return; // (no change [perhaps a property/field on the object was only changed])
                    value = translatedItem;
                }
                if (allowCollectionChangedEvent && CollectionChanging != null)
                    _NotifiyCollectionChanging(NotifyCollectionChangingAction.Replace, value, itemReplaced, index);
                _wrappedList[index] = value;
            }
            else
            {
                int firstListLength = _itemArayList[0].Length;

                if (index < firstListLength)
                {
                    itemReplaced = _itemArayList[0][index];
                    if (SetItemTranslation != null)
                    {
                        T translatedItem = SetItemTranslation(itemReplaced, value);
                        if (Objects.Equals(translatedItem, itemReplaced)) return; // (no change [perhaps a property/field on the object was only changed])
                        value = translatedItem;
                    }
                    if (allowCollectionChangedEvent && CollectionChanging != null)
                        _NotifiyCollectionChanging(NotifyCollectionChangingAction.Replace, value, itemReplaced, index);
                    _itemArayList[0][index] = value;
                }
                else
                {
                    int ofsDiff = index - firstListLength;
                    int listIndex = 1 + ofsDiff / _allocStep;
                    int arrayIndex = ofsDiff % _allocStep;
                    itemReplaced = _itemArayList[listIndex][arrayIndex];
                    if (SetItemTranslation != null)
                    {
                        T translatedItem = SetItemTranslation(itemReplaced, value);
                        if (Objects.Equals(translatedItem, itemReplaced)) return; // (no change [perhaps a property/field on the object was only changed])
                        value = translatedItem;
                    }
                    if (allowCollectionChangedEvent && CollectionChanging != null)
                        _NotifiyCollectionChanging(NotifyCollectionChangingAction.Replace, value, itemReplaced, index);
                    _itemArayList[listIndex][arrayIndex] = value;
                }
            }

            if (value != null)
            {
                _AttachPropertyChangedBridge(value as INotifyPropertyChanged);
                _AttachPropertyChangingBridge(value as INotifyPropertyChanging);
            }

            if (itemReplaced != null)
            {
                _UnattachPropertyChangedBridge(itemReplaced as INotifyPropertyChanged);
                _UnattachPropertyChangingBridge(itemReplaced as INotifyPropertyChanging);
            }

            if (allowCollectionChangedEvent && CollectionChanged != null)
                _NotifiyCollectionChanged(NotifyCollectionChangedAction.Replace, value, itemReplaced, index);
        }
        protected void _SetItem(int index, T value) { _SetItem(index, value, false); }

        #endregion

        // -----------------------------------------------------------------------------------------------

        void _DoDuplicateIndexNameCheck(T item)
        {
            if (GetIndexNameCallback != null) // (named indexing enabled? then no duplicates!)
            {
                var indexName = GetIndexNameCallback(item);

                if (Contains(indexName))
                {
                    if (!CaseSensitiveIndexNames) indexName = indexName.ToLower(); // (convert casing for purpose of message below)

                    var message = DuplicateNamedIndexMessageCallback != null ?
                        DuplicateNamedIndexMessageCallback(indexName)
                        : "InteropCollection<T>: Another item with the index name '" + indexName + "' already exists.";

                    if (message != null)
                        throw new InvalidOperationException(message);
                }
            }
        }

        // -----------------------------------------------------------------------------------------------

        #region ICollection<T> Members

        public void Add(T item)
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );

            if (!IgnoreDuplicateNamedIndexes)
                _DoDuplicateIndexNameCheck(item);

            if (CollectionChanging != null)
                _NotifiyCollectionChanging(NotifyCollectionChangingAction.Add, item, null, Count);

            if (_wrappedList != null)
                _wrappedList.Add(item);
            else
            {
                _Growing(1);
                _itemArayList[_itemArayListInsertIndex][_itemArayItemInsertIndex] = item;

                if (item != null)
                {
                    _UnattachPropertyChangedBridge(item as INotifyPropertyChanged); // (just in case)
                    _UnattachPropertyChangingBridge(item as INotifyPropertyChanging); // (just in case)

                    _AttachPropertyChangedBridge(item as INotifyPropertyChanged);
                    _AttachPropertyChangingBridge(item as INotifyPropertyChanging);
                }

                _incInsertIndexes();
            }

            if (CollectionChanged != null)
                _NotifiyCollectionChanged(NotifyCollectionChangedAction.Add, item, null, Count - 1);
        }
        public void Add(T[] items) /*NEW*/
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );
            for (int i = 0; i < items.Length; i++)
                Add(items[i]);
        }
        public void Add(IEnumerable<T> items) /*NEW*/
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );
            foreach (T item in items)
                Add(item);
        }

        public void Clear() { _Clear(true); }
        bool _Clear(bool triggerEvents)
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );

            bool changed = false;

            if (_wrappedList != null)
            {
                if (triggerEvents && CollectionChanging != null)
                    _NotifiyCollectionChanging(NotifyCollectionChangingAction.Reset, null, null, 0);
                changed = (_wrappedList.Count > 0);
                _wrappedList.Clear();
            }
            else if (_totalStorage > 0)
            {
                changed = (_totalItems > 0);

                if (triggerEvents && changed && CollectionChanging != null)
                    _NotifiyCollectionChanging(NotifyCollectionChangingAction.Reset, null, null, 0);

                // ... release reference types ...
                if (typeof(T).GetTypeInfo().IsClass)
                    for (int i = 0; i < _totalItems; i++)
                        _SetItem(i, default(T));

                _itemArayListInsertIndex = 0;
                _itemArayItemInsertIndex = 0;
                _totalItems = 0;

                if (triggerEvents && changed && CollectionChanged != null)
                    _NotifiyCollectionChanged(NotifyCollectionChangedAction.Reset, null, null, 0);
            }

            return changed;
        }
        bool _Clear() { return _Clear(false); }

        public void Clear(int newCapacity) /*NEW*/ { _Clear(newCapacity, true); }
        bool _Clear(int newCapacity, bool triggerEvents) /*NEW*/
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );

            bool changed = false;

            if (_wrappedList != null)
            {
                changed = (_wrappedList.Count > 0);
                _wrappedList.Clear();
            }
            else
            {
                if (newCapacity < 0)
                    throw new InvalidOperationException("InteropCollection<T>: Storage capacity cannot be less than 0.");

                changed = _Clear(triggerEvents);

                if (newCapacity != _totalStorage)
                {
                    _itemArayList.Clear();

                    if (newCapacity > 0)
                    {
                        _itemArayList.Add(new T[newCapacity]);
                        _totalStorage = newCapacity;
                    }
                    else
                    {
                        _totalStorage = 0;
                        _itemArayListInsertIndex = -1;
                        _itemArayItemInsertIndex = -1;
                    }

                    changed = true;
                }
            }

            return changed;
        }
        bool _Clear(int newCapacity) { return _Clear(newCapacity, false); }

        public bool Contains(T item)
        {
            T _item;
            var count = Count;
            for (int i = 0; i < count; i++)
            {
                _item = _GetItem(i);
                if (_item != null && _item.Equals(item)) return true;
            }
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );

            var count = Count;
            for (int i = 0; i < count && arrayIndex + i < array.Length; i++)
                array[arrayIndex + i] = _GetItem(i);
        }
        public void CopyTo(IList<T> list, int listIndex) /*NEW*/
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );

            for (int i = 0; i < Count; i++)
            {
                if (listIndex + i >= list.Count)
                    list.Add(_GetItem(i));
                else
                    list[listIndex + i] = _GetItem(i);
            }
        }
        public void CopyTo(IList<T> list) /*NEW*/
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );
            CopyTo(list, 0);
        }

        public int Count
        {
            get { return (_wrappedList != null) ? _wrappedList.Count : _totalItems; }
        }

        public bool IsReadOnly
        {
            get { return _isReadOnly; }
        }

        public bool Remove(T item)
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );

            int i = IndexOf(item);
            if (i < 0) return false;
            RemoveAt(i);
            return true;
        }

        #endregion

        public bool Remove(string indexName, bool throwNotFoundException)
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );

            var i = IndexOf(indexName, throwNotFoundException);
            if (i >= 0) { RemoveAt(i); return true; }
            return false;
        }
        public bool Remove(string indexName) { return Remove(indexName, false); }

        // -----------------------------------------------------------------------------------------------

        #region IEnumerable<T> Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        #endregion

        // -----------------------------------------------------------------------------------------------

        #region INotifyCollectionChanging Members

        public event NotifyCollectionChangingEventHandler CollectionChanging;

        #endregion

        void _NotifiyCollectionChanging(NotifyCollectionChangingAction action, object newItem, object oldItem, int index)
        {
            if (CollectionChanging != null)
            {
                NotifyCollectionChangingEventArgs args;

                if (action == NotifyCollectionChangingAction.Replace)
                    args = new NotifyCollectionChangingEventArgs(
                        action,
                        newItem,
                        oldItem,
                        index
                        );
                else if (action == NotifyCollectionChangingAction.Remove)
                    args = new NotifyCollectionChangingEventArgs(
                        action,
                        oldItem,
                        index
                        );
                else if (action == NotifyCollectionChangingAction.Add)
                    args = new NotifyCollectionChangingEventArgs(
                        action,
                        newItem,
                        index
                        );
                else
                    args = new NotifyCollectionChangingEventArgs(action);

                try
                {
                    CollectionChanging(this, args);
                }
                catch (NotSupportedException) /*...a method probably not implemented (if replace, then simulate, otherwise, reset)...*/
                {
                    if (args.Action == NotifyCollectionChangingAction.Replace)
                    {
                        CollectionChanging(this, new NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction.Remove, oldItem, index));
                        CollectionChanging(this, new NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction.Add, newItem, index));
                    }
                    else
                        CollectionChanging(this, new NotifyCollectionChangingEventArgs(NotifyCollectionChangingAction.Reset));
                }
            }
        }

        // -----------------------------------------------------------------------------------------------

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion


        void _NotifiyCollectionChanged(NotifyCollectionChangedAction action, object newItem, object oldItem, int index)
        {
            if (CollectionChanged != null)
            {
                NotifyCollectionChangedEventArgs args;

                if (action == NotifyCollectionChangedAction.Replace)
                    args = new NotifyCollectionChangedEventArgs(
                        action,
                        newItem,
                        oldItem,
                        index
                        );
                else if (action == NotifyCollectionChangedAction.Remove)
                    args = new NotifyCollectionChangedEventArgs(
                        action,
                        oldItem,
                        index
                        );
                else if (action == NotifyCollectionChangedAction.Add)
                    args = new NotifyCollectionChangedEventArgs(
                        action,
                        newItem,
                        index
                        );
                else
                    args = new NotifyCollectionChangedEventArgs(action);

                try
                {
                    CollectionChanged(this, args);
                }
                catch (NotSupportedException) /*...a method probably not implemented (if replace, then simulate, otherwise, reset)...*/
                {
                    if (args.Action == NotifyCollectionChangedAction.Replace)
                    {
                        CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItem, index));
                        CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItem, index));
                    }
                    else
                        CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
            }
        }

        // -----------------------------------------------------------------------------------------------
        // Add methods to attach a property changing event bridge.

        #region INotifyPropertyChanging Members

        public event PropertyChangingEventHandler PropertyChanging;

        #endregion

        void _ItemPropertyChangingBridge(object sender, PropertyChangingEventArgs e)
        {
            if (PropertyChanging != null)
                PropertyChanging(sender, e);
        }
        void _AttachPropertyChangingBridge(INotifyPropertyChanging item)
        {
            if (item != null)
                item.PropertyChanging += _ItemPropertyChangingBridge;
        }
        void _UnattachPropertyChangingBridge(INotifyPropertyChanging item)
        {
            if (item != null)
                item.PropertyChanging -= _ItemPropertyChangingBridge;
        }

        // -----------------------------------------------------------------------------------------------
        // Add methods to attach a property changed event bridge.
        // Note: Attaching the changed events attempts to attach the changing events also.

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        void _ItemPropertyChangedBridge(object sender, PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(sender, e);
        }
        void _AttachPropertyChangedBridge(INotifyPropertyChanged item)
        {
            if (item != null)
                item.PropertyChanged += _ItemPropertyChangedBridge;
        }
        void _UnattachPropertyChangedBridge(INotifyPropertyChanged item)
        {
            if (item != null)
                item.PropertyChanged -= _ItemPropertyChangedBridge;
        }

        // -----------------------------------------------------------------------------------------------

        public class Enumerator : IEnumerator<T>, IEnumerator // (T: EnumType)
        {
            public InteropCollection<T> _collection;

            // Enumerators are positioned before the first element
            // until the first MoveNext() call.
            int position = -1;

            public Enumerator(InteropCollection<T> collection)
            {
                _collection = collection;
            }

            public bool MoveNext()
            {
                if (position < _collection.Count)
                    position++;
                return (position < _collection.Count);
            }

            public void Reset()
            {
                position = -1;
            }

            object IEnumerator.Current
            {
                get
                {
                    try
                    {
                        return _collection[position];
                    }
                    catch (IndexOutOfRangeException ex)
                    {
                        throw new InvalidOperationException("Indexing error.", ex);
                    }
                }
            }

            #region *** IEnumerator<T> Members ***
            //*
            T IEnumerator<T>.Current
            {
                get { return (T)((IEnumerator)this).Current; }
            }
            //*
            #endregion ***************************

            #region *** IDisposable Members ***
            //*
            public void Dispose()
            {
                _collection = null;
            }
            //*
            #endregion ***************************
        }

        // -----------------------------------------------------------------------------------------------

        #region IList Members

        public int Add(object value)
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );
            Add((T)value);
            return Count - 1;
        }

        public bool Contains(object value)
        {
            return Contains((T)value);
        }

        public bool Contains(string namedIndex)
        {
            return IndexOf(namedIndex) >= 0;
        }

        public int IndexOf(object value)
        {
            return IndexOf((T)value);
        }

        public void Insert(int index, object value)
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );
            Insert(index, (T)value);
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public void Remove(object value)
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );
            Remove((T)value);
        }

        object IList.this[int index]
        {
            get
            {
                return ((IList<T>)this)[index];
            }
            set
            {
                _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );
                if (value != null && value.GetType() != typeof(T))
                    throw new InvalidCastException("InteropCollection<" + typeof(T).Name + ">: Collection is strongly typed and cannot accept an item of type '" + value.GetType().Name + "'.");
                ((IList<T>)this)[index] = (T)value;
            }
        }

        #endregion

        // -----------------------------------------------------------------------------------------------

        #region ICollection Members

        public void CopyTo(Array array, int index)
        {
            CopyTo(array, index);
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { return this; }
        }

        #endregion

        // -----------------------------------------------------------------------------------------------

        public void Swap(int index1, int index2) /*NEW*/
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );
            T temp = this[index1];
            this[index1] = this[index2];
            this[index2] = temp;
        }

        // -----------------------------------------------------------------------------------------------

        public T[] ToArray()
        {
            T[] typedArray = new T[Count];
            CopyTo(typedArray, 0);
            return typedArray;
        }

        // -----------------------------------------------------------------------------------------------

        public DerivedType[] ToArray<DerivedType>()
        {
            DerivedType[] typedArray = new DerivedType[Count];
            if (typeof(DerivedType).GetTypeInfo().IsSubclassOf(typeof(T)))
                CopyTo(typedArray, 0);
            return typedArray;
        }

        // -----------------------------------------------------------------------------------------------

        /// <summary>
        /// Compacts the list of item arrays into a single array.
        /// This can greatly speed up item access when accessing items using the this[] index operator,
        /// which in turns greatly speeds up some other collection methods.
        /// </summary>
        public InteropCollection<T> Compact()
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );
            if (_wrappedList == null)
            {
                T[] compactedItems = new T[Count];
                CopyTo(compactedItems, 0);
                Assign(compactedItems);
            }
            return this;
        }

        // -----------------------------------------------------------------------------------------------

        public T Pop()
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );
            if (Count > 0)
            {
                T item = this[Count - 1];
                RemoveAt(Count - 1);
                return item;
            }
            throw new InvalidOperationException("InteropCollection<" + typeof(T).Name + ">: No more items to pop.");
        }

        public void Push(T item)
        {
            _DoReadOnlyCheck(
#if !DOTNETCORE
                Assembly.GetCallingAssembly()
#endif
            );
            Add(item);
        }

        // -----------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns the first item of the collection.
        /// An error will occur if there are no items in the array (check 'Count' first).
        /// </summary>
        public T First { get { return this[0]; } }
        /// <summary>
        /// Returns the last item of the collection.
        /// An error will occur if there are no items in the array (check 'Count' first).
        /// </summary>
        public T Last { get { return this[Count - 1]; } }

        // -----------------------------------------------------------------------------------------------

        InteropCollectionView<T> _LocalView = null;
        /// <summary>
        /// Creates and returns a new view, if not already created.
        /// This provides a quick and convenient way to get a view for the current collection.
        /// </summary>
        public InteropCollectionView<T> View
        {
            get
            {
                if (_LocalView == null)
                    _LocalView = new InteropCollectionView<T>(this);
                return _LocalView;
            }
        }

        // -----------------------------------------------------------------------------------------------
    }

    // ###################################################################################################

#if DOTNETCORE || !ComponentModel
    public class InteropCollectionView<T>
        : INotifyPropertyChanged, INotifyCollectionChanged, IEnumerable<T>, IEnumerable
    {
#else
    public class InteropCollectionView<T>
        : ICollectionView, INotifyPropertyChanged, INotifyCollectionChanged, IEnumerable<T>, IEnumerable
    {
        public event CurrentChangingEventHandler CurrentChanging;
#endif
        Int32 __ViewIndex = -1;
        CultureInfo _Culture = CultureInfo.CurrentCulture;

        public event EventHandler CurrentChanged;
        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;


        class _RefreshLock : IDisposable
        {
            InteropCollectionView<T> _Subject;
            public _RefreshLock(InteropCollectionView<T> subject) { _Subject = subject; }
            public void Dispose()
            { _Subject._RefreshLockCount--; if (_Subject._RefreshLockCount == 0) _Subject.Refresh(); }
        }
        int _RefreshLockCount = 0;

#if !DOTNETCORE && ComponentModel
        ObservableCollection<GroupDescription> _GroupDescriptions = null;
        //??ReadOnlyObservableCollection<object> _Groups = null;

        SortDescriptionCollection _SortDescriptions = null;
#endif

        IList<T> _SourceCollection;
        InteropCollection<T> _FilteredCollection;

#if SILVERLIGHT
        public PagedCollectionView PagedView { get { return _PagedView ?? (_PagedView = new PagedCollectionView(this, false, false)); } }
        PagedCollectionView _PagedView;
#endif

        // -----------------------------------------------------------------------------------------------

        public InteropCollectionView(IList<T> list, bool useSourceCapacity)
        {
            if (list == null)
                throw new ArgumentNullException("InteropCollectionView<T>(): 'list' cannot be null.");

            _SourceCollection = list;
            if (_SourceCollection is INotifyCollectionChanged)
                ((INotifyCollectionChanged)_SourceCollection).CollectionChanged += _SourceCollection_CollectionChanged;

            int capacity = useSourceCapacity ? _SourceCollection.Count : 0;
            _FilteredCollection = new InteropCollection<T>(capacity);
            _FilteredCollection.PropertyChanged += _FilteredCollection_PropertyChanged;
            _FilteredCollection.CollectionChanged += _FilteredCollection_CollectionChanged;

            Refresh();
        }
        public InteropCollectionView(IList<T> list) : this(list, true) { }

        // -----------------------------------------------------------------------------------------------

        void _SourceCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    //int startIndex = e.NewStartingIndex;
                    //foreach (T item in e.NewItems)
                    //{
                    //    if (_ItemAccepted(item))
                    //        _FilteredCollection.Insert(startIndex++, item);
                    //}
                    Refresh();
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (T item in e.OldItems)
                    {
                        _FilteredCollection.Remove(item);
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    //foreach (T item in e.OldItems)
                    //{
                    //    _FilteredCollection.Remove(item);
                    //}
                    //int index = e.NewStartingIndex;
                    //foreach (T item in e.NewItems)
                    //{
                    //    if (_ItemAccepted(item))
                    //        _FilteredCollection.Insert(index++, item);
                    //}
                    Refresh();
                    break;

                case NotifyCollectionChangedAction.Reset:
                    Refresh();
                    break;
            }
        }

        // -----------------------------------------------------------------------------------------------

        void _FilteredCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Count == 0)
                __ViewIndex = -1;
            else if (__ViewIndex > Count)
                __ViewIndex = Count;

            if (CollectionChanged != null)
                CollectionChanged(sender, e);
        }

        void _FilteredCollection_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(sender, e);
        }

        // -----------------------------------------------------------------------------------------------

        public T this[int index]
        {
            get { return _FilteredCollection[index]; }
            set { _FilteredCollection[index] = value; }
        }

        public int Count { get { return _FilteredCollection.Count; } }

        // -----------------------------------------------------------------------------------------------

        #region ICollectionView Members

        Int32 _ViewIndex // (will need to test after change to see if the change was successful)
        {
            get { return __ViewIndex; }
            set
            {
                if (value != __ViewIndex && DoCurrentChanging(value < 0 ? null : (object)this[value]))
                {
                    __ViewIndex = value;
                    DoCurrentChanged();
                }
            }
        }

        public bool CanFilter { get { return true; } }
        public bool CanGroup { get { return false; } }
        public bool CanSort { get { return true; } }

        public CultureInfo Culture
        {
            get { return _Culture; }
            set { _Culture = value; }
        }

        void DoCurrentChanged()
        { if (CurrentChanged != null) CurrentChanged(this, new EventArgs()); }

        bool DoCurrentChanging(object target, bool isCancelable)
        {
#if ComponentModel
            CurrentChangingEventArgs args = new CurrentChangingEventArgs(isCancelable);
            if (CurrentChanging != null) CurrentChanging(this, args);
            return !args.Cancel;
#else
            return true;
#endif
        }
        bool DoCurrentChanging(object target) { return DoCurrentChanging(target, true); }

        public object CurrentItem
        {
            get { return _ViewIndex < 0 || _ViewIndex >= Count ? null : (object)this[_ViewIndex]; }
        }

        public int CurrentPosition { get { return _ViewIndex; } }

        public bool Contains(object item)
        { return _FilteredCollection.Contains(item); }

        public IDisposable DeferRefresh() { _RefreshLockCount++; return new _RefreshLock(this); }
        public bool IsRefreshLocked { get { return _RefreshLockCount > 0; } }

        /// <summary>
        /// Additional filters (other than the main filter).
        /// </summary>
        public event Predicate<object> Filters
        {
            add { _Filters.Add(value); }
            remove { if (_Filters.Contains(value)) _Filters.Remove(value); }
        }
        readonly List<Predicate<object>> _Filters = new List<Predicate<object>>();

        /// <summary>
        /// The main "parent" filter method.
        /// This allows filtering the view on a single "parent" filter that normally never changes.
        /// "child filters" can be added or removed via the 'Filters' property without affecting this "main" filter.
        /// </summary>
        public Predicate<object> Filter { get; set; }

#if !DOTNETCORE && ComponentModel
        public ObservableCollection<GroupDescription> GroupDescriptions
        {
            get
            {
                if (_GroupDescriptions == null)
                    _GroupDescriptions = new ObservableCollection<GroupDescription>();
                return _GroupDescriptions;
            }
        }
#endif

        public ReadOnlyObservableCollection<object> Groups
        {
            get
            {
                // (not supported as of yet)
                //if (_Groups == null)
                //    _Groups = new ReadOnlyObservableCollection<object>(Collections.ToObservableCollection<object>((IEnumerable<object>)this));
                return null;
            }
        }

        public bool IsCurrentAfterLast { get { return _ViewIndex >= Count; } }

        public bool IsCurrentBeforeFirst { get { return _ViewIndex < 0; } }

        public bool IsEmpty { get { return Count == 0; } }

        public bool MoveCurrentTo(object item)
        {
            int i = -1;
            if (item != null) // (allow 'null' to reset the selected)
            {
                i = _FilteredCollection.IndexOf(item);
                if (i < 0) return false;
            }
            _ViewIndex = i;
            return (_ViewIndex == i);
        }

        public bool MoveCurrentToFirst()
        {
            if (Count == 0) return false;
            _ViewIndex = 0;
            return (_ViewIndex == 0);
        }

        public bool MoveCurrentToLast()
        {
            int i = Count - 1;
            if (i < 0) return false;
            _ViewIndex = i;
            return (_ViewIndex == i);
        }

        public bool MoveCurrentToNext()
        {
            int i = _ViewIndex + 1;
            if (i >= Count) return false;
            _ViewIndex = i;
            return (_ViewIndex == i);
        }

        public bool MoveCurrentToPosition(int position)
        {
            if (position < -1 || position > Count) return false;
            _ViewIndex = position;
            return (_ViewIndex == position);
        }

        public bool MoveCurrentToPrevious()
        {
            int i = _ViewIndex - 1;
            if (i < 0) return false;
            _ViewIndex = i;
            return (_ViewIndex == i);
        }

        public void SelectNone() { MoveCurrentTo(null); }

        /// <summary>
        /// Rebuilds the collection view (this is done automatically if the source list changes).
        /// </summary>
        public void Refresh()
        {
            _BuildLocalCollection();
        }

#if !DOTNETCORE && ComponentModel
        public SortDescriptionCollection SortDescriptions
        {
            get
            {
                if (_SortDescriptions == null)
                    _SortDescriptions = new SortDescriptionCollection();
                return _SortDescriptions;
            }
        }
#endif

        public IEnumerable SourceCollection
        {
            get { return _SourceCollection; }
        }

        public InteropCollection<T> View { get { return _FilteredCollection; } }

        /// <summary>
        /// Builds the view-based collection using the specified filter.
        /// ///
        /// </summary>
        void _BuildLocalCollection()
        {
            DoCurrentChanging(null, false); // TODO: Should only do this if its actually changing!

            object selectedItem = CurrentItem;

            _FilteredCollection.Clear();
            __ViewIndex = -1;

            for (int i = 0; i < _SourceCollection.Count; i++)
            {
                T item = _SourceCollection[i];
                if (_ItemAccepted(item))
                {
                    _FilteredCollection.Add(item);
                }
            }

            // ... reselect the last item (make sure to trigger 'DoCurrentChanged()' only if the selection becomes unavailable!!!) ...

            if (CurrentItem != selectedItem)
            {
                bool changed = true;

                for (int i = 0; i < _FilteredCollection.Count; i++)
                    if (object.Equals(_FilteredCollection[i], selectedItem)) { __ViewIndex = i; changed = false; break; }

                if (changed)
                    DoCurrentChanged();
            }

#if SILVERLIGHT
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs("PagedView"));
#endif
        }

        /// <summary>
        /// Checks if the item passes the registered filter.
        /// </summary>
        bool _ItemAccepted(T item)
        {
            if (Filter != null && !Filter(item)) return false; // (note: returns 'true' if accepted)
            foreach (var filter in _Filters)
                if (!filter(item)) return false;
            return true; // (no filter = accept all)
        }

#endregion

        // -----------------------------------------------------------------------------------------------

#region IEnumerable<T> and IEnumerable Members

        public IEnumerator<T> GetEnumerator()
        {
            return new InteropCollection<T>.Enumerator(_FilteredCollection);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new InteropCollection<T>.Enumerator(_FilteredCollection);
        }

#endregion
    }

    // ###################################################################################################
}
