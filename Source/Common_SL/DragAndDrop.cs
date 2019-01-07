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
using System.Reflection;

namespace Shared.Silverlight.Attachables
{
    public class DragAndDrop : DependencyObject
    {
        //------------------------------------------------------------------------------------------------

        private static bool _initialized = false;

        public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
            "Enabled",               //Name of the property
            typeof(bool),            //Type of the property
            typeof(DragAndDrop),  //Type of the provider of the registered attached property
            new PropertyMetadata(false, OnEnabledChanged));

        public static void SetEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(EnabledProperty, value);
        }

        public static bool GetEnabled(DependencyObject obj)
        {
            try { return (bool)obj.GetValue(EnabledProperty); }
            catch { return false; }
        }

        public static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)d;
            bool enabled = (bool)e.NewValue;
            if (enabled)
            {
                element.MouseLeftButtonDown += OnElementMouseLeftButtonDown;
                element.MouseLeftButtonUp += OnMouseLeftButtonUp;
                element.MouseEnter += OnMouseEnter;
                element.MouseLeave += OnMouseLeave;
            }
            else
            {
                element.MouseLeftButtonDown -= OnElementMouseLeftButtonDown;
                element.MouseLeftButtonUp -= OnMouseLeftButtonUp;
                element.MouseEnter -= OnMouseEnter;
                element.MouseLeave -= OnMouseLeave;
            }

            if (!_initialized)
            {
                if (Application.Current != null && Application.Current.RootVisual != null)
                {
                    Application.Current.RootVisual.MouseMove += OnMouseMove;
                    Application.Current.RootVisual.MouseLeftButtonUp += OnMouseLeftButtonUp;
                }
                _initialized = true;
            }
        }

        //------------------------------------------------------------------------------------------------

        /// <summary>
        /// Validates that the object has Drag 'n Drop enabled before returning.
        /// </summary>
        protected static void _AssertDragNDrop(string method, DependencyObject obj)
        {
            if (!GetEnabled(obj))
                throw new Exception(method + ": The specific object must have drag 'n drop enabled (via <... DragAndDrop.Enabled=\"true\" .../> or DragAndDrop.SetEnabled()).");
        }

        //------------------------------------------------------------------------------------------------

        public static readonly DependencyProperty DropTargetInterfaceProperty = DependencyProperty.RegisterAttached(
            "DropTargetInterface",              //Name of the property
            typeof(DropTargetInterface),     //Type of the property
            typeof(DragAndDrop),             //Type of the provider of the registered attached property
            new PropertyMetadata(null));

        public static void SetDropTargetInterface(DependencyObject obj, DropTargetInterface value)
        {
            // ... validate the object first ...
            _AssertDragNDrop("SetDropTargetInterface", obj);
            obj.SetValue(DropTargetInterfaceProperty, value);
        }

        public static DropTargetInterface GetDropTargetInterface(DependencyObject obj)
        {
            return (DropTargetInterface)obj.GetValue(DropTargetInterfaceProperty);
        }

        //------------------------------------------------------------------------------------------------

        public static readonly DependencyProperty DragSourceInterfaceProperty = DependencyProperty.RegisterAttached(
            "DragSourceInterface",              //Name of the property
            typeof(DragSourceInterface),     //Type of the property
            typeof(DragAndDrop),             //Type of the provider of the registered attached property
            new PropertyMetadata(null));

        public static void SetDragSourceInterface(DependencyObject obj, DragSourceInterface value)
        {
            // ... validate the object first ...
            _AssertDragNDrop("SetDragSourceInterface", obj);
            obj.SetValue(DragSourceInterfaceProperty, value);
        }

        public static DragSourceInterface GetDragSourceInterface(DependencyObject obj)
        {
            return (DragSourceInterface)obj.GetValue(DragSourceInterfaceProperty);
        }

        //------------------------------------------------------------------------------------------------

        public static FrameworkElement SelectedElement { get; private set; }
        public static object SelectedItem { get; private set; }
        public static DragSourceInterface DragSource { get; private set; }
        private static Cursor _previousDragSourceElementCursor = null;

        //------------------------------------------------------------------------------------------------
        // Mouse events for the "drag" part of the event system.

        private static void ResetSource()
        {
            if (SelectedElement != null)
                SelectedElement.Cursor = _previousDragSourceElementCursor;
            SelectedElement = null;
            SelectedItem = null;
            DragSource = null;

            ResetTarget();
        }

        private static void OnElementMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ResetSource();

            FrameworkElement element = (FrameworkElement)sender;
            DragSourceInterface sourceInterface = GetDragSourceInterface(element);

            // ... if element has no explicitly defined interface, then implicitly create one ...

            if (sender is IDragSourceHandlers)
            {
                sourceInterface = new DragSourceInterface(
                    (sender as IDragSourceHandlers).CanDrag,
                    (sender as IDragSourceHandlers).GetDragItem
                    );
                SetDragSourceInterface(element, sourceInterface);  // (save for quick future reference)
            }

            // ... check if an object is ready to be dragged, then store a reference to the source interface and item object ...

            if (sourceInterface != null && (sourceInterface.CanDrag == null || sourceInterface.CanDrag((FrameworkElement)sender)))
            {
                DragSource = sourceInterface;

                SelectedElement = (FrameworkElement)sender;

                // (note: the select item is "recorded", since this may change before the drop target is reached)

                if (sourceInterface.GetDragItem != null)
                    SelectedItem = sourceInterface.GetDragItem(SelectedElement) ?? SelectedElement;
                else
                    SelectedItem = SelectedElement;

                _previousDragSourceElementCursor = SelectedElement.Cursor;
                SelectedElement.Cursor = Cursors.Hand;
            }
        }

        private static void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (SelectedItem != null)
            {
                if (DropTarget != null && DropTarget.OnDrop != null && DragSource != null)
                    DropTarget.OnDrop(DropTargetElement, SelectedItem);
            }

            ResetSource();
        }

        private static void OnMouseMove(object sender, MouseEventArgs e)
        {
            //TODO: Drag 'n Drop: Move an icon image to show drag 'n drop?
        }

        //------------------------------------------------------------------------------------------------
        // Mouse events for the "drop" reception of the event system.
        // Only 

        public static FrameworkElement DropTargetElement { get; private set; }
        public static DropTargetInterface DropTarget { get; private set; }
        private static Cursor _previousDropTargetElementCursor = null;

        private static void ResetTarget()
        {
            if (DropTargetElement != null)
                DropTargetElement.Cursor = _previousDropTargetElementCursor;
            DropTargetElement = null;
            DropTarget = null;
        }

        private static void OnMouseEnter(object sender, MouseEventArgs e)
        {
            ResetTarget(); // (clear fields, just in case)

            // ... make sure to update the drag item reference, in case a selected item focus hasn't changed yet ...
            if (DragSource != null)
                SelectedItem = DragSource.GetDragItem(SelectedElement) ?? SelectedElement;

            if (SelectedItem != null)
            {
                FrameworkElement element = (FrameworkElement)sender;
                DropTargetInterface targetInterface = GetDropTargetInterface(element);

                // ... if element has no explicitly defined interface, then implicitly create one ...

                if (targetInterface == null && element is IDropTargetHandlers)
                {
                    targetInterface = new DropTargetInterface(
                        (element as IDropTargetHandlers).DropAccepted,
                        (element as IDropTargetHandlers).OnDrop
                        );
                    SetDropTargetInterface(element, targetInterface); // (save for quick future reference)
                }

                // ... check if drop is accepted, then store a reference to the target interface ...

                if (targetInterface != null && (targetInterface.DropAccepted == null || targetInterface.DropAccepted(element, SelectedItem)))
                {
                    DropTargetElement = element;
                    DropTarget = targetInterface;
                    _previousDropTargetElementCursor = DropTargetElement.Cursor;
                    DropTargetElement.Cursor = Cursors.Hand;
                }
            }
        }

        private static void OnMouseLeave(object sender, MouseEventArgs e)
        {
            ResetTarget();
        }

        //------------------------------------------------------------------------------------------------
    }

    // ###################################################################################################

    /// <summary>
    /// If a control has DragAndDrop.Enabled applied, and also has DropTargetInterface attached,
    /// then it is a valid drop target.
    /// </summary>
    public class DropTargetInterface
    {
        /// <summary>
        /// Returns true if the object is accepted for dropping, and false otherwise.
        /// If null, then the default is to assume "accept ee" (true).
        /// </summary>
        public delegate bool DropAcceptedHandler(FrameworkElement target, object item);
        /// <summary>
        /// Executes code to handle the drop event.
        /// </summary>
        public delegate void OnDropHandler(FrameworkElement target, object item);

        public DropAcceptedHandler DropAccepted { get; set; }
        public OnDropHandler OnDrop { get; set; }

        public DropTargetInterface(DropAcceptedHandler dropAccepted, OnDropHandler onDrop)
        {
            DropAccepted = dropAccepted;
            OnDrop = onDrop;
        }
    }

    /// <summary>
    /// Provides a quick an easy way to create the needed methods in a class (via intellisense) to handle drop targets.
    /// </summary>
    public interface IDropTargetHandlers
    {
        bool DropAccepted(FrameworkElement target, object item);
        void OnDrop(FrameworkElement target, object item);
    }

    /// <summary>
    /// If a control has DragAndDrop.Enabled applied, and also has DropSourceInterface attached,
    /// then it is a valid drop source.
    /// </summary>
    public class DragSourceInterface
    {
        /// <summary>
        /// Returns true if there is something to drag (eg: a list item selected), and false otherwise.
        /// </summary>
        public delegate bool CanDragHandler(FrameworkElement source);
        /// <summary>
        /// Returns the object to drag 'n drop. This object can be of any type.
        /// If null, the element clicked becomes the source by default.
        /// </summary>
        public delegate object GetDragItemHandler(FrameworkElement source);

        public CanDragHandler CanDrag { get; set; }
        /// <summary>
        /// If null, the element clicked becomes the source by default.
        /// </summary>
        public GetDragItemHandler GetDragItem { get; set; }

        public DragSourceInterface(CanDragHandler canDrag, GetDragItemHandler getDragItem)
        {
            CanDrag = canDrag;
            GetDragItem = getDragItem;
        }
    }

    /// <summary>
    /// Provides a quick an easy way to create the needed methods in a class (via intellisense) to handle drag sources.
    /// </summary>
    public interface IDragSourceHandlers
    {
        bool CanDrag(FrameworkElement source);
        object GetDragItem(FrameworkElement source);
    }
}
