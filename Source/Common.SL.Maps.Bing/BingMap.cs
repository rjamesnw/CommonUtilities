using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Browser;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Xml.Linq;
using System.Xml.XPath;
using Common.XAML.Controls.Maps.GeocodeService;
using Common.XAML.Controls.Maps.ImageryService;
using Common.XAML.Controls.Maps.RouteService;
using Common.XAML.Controls.Maps.SearchService;
using Common.XAML.Converters;
using Microsoft.Maps.MapControl;

namespace Common.XAML.Controls.Maps
{
    public class BingMap : ContentControl
    {
        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// The Bing Maps application key (required by various services, such as geocoding).
        /// This key is the default while the instance key is not explicitly set.
        /// </summary>
        public static string DefaultApplicationKey { get; set; }

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// The Bing Maps application key (required by various services, such as geocoding).
        /// </summary>
        public string Key
        {
            get { if ((_Key ?? DefaultApplicationKey) == null) throw new InvalidOperationException("Bing Maps API key is missing."); else return _Key ?? DefaultApplicationKey; }
            set { if (value != _Key) { _Key = value; _CredentialsUpdated = false; _UpdateCredentials(); } }
        }
        string _Key;

        bool _CredentialsUpdated;

        void _UpdateCredentials()
        {
            if (!_CredentialsUpdated)
            {
                _Map.CredentialsProvider = new ApplicationIdCredentialsProvider(Key);
                _GeocodeRequest.Credentials = new GeocodeService.Credentials { ApplicationId = Key };
                _CredentialsUpdated = true;
            }
        }

        // --------------------------------------------------------------------------------------------------

        public Map Map { get { return _Map; } }
        Map _Map;

        Microsoft.Maps.MapControl.Pushpin _CenterPin;

        GeocodeRequest _GeocodeRequest;

        // --------------------------------------------------------------------------------------------------

        public event EventHandler<LoadingErrorEventArgs> LoadingError;

        public event EventHandler Ready
        {
            add
            {
                if (IsReady)
                    value(this, new EventArgs());
                else
                    _Ready += value;
            }
            remove
            {
                _Ready -= value;
            }
        }
        event EventHandler _Ready;

        public bool IsReady { get { return _MapLoaded && _FirstTimeViewChangedEnded; } }
        bool _MapLoaded, _FirstTimeViewChangedEnded;

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns 'true' if the view is changing to a new target, and 'false' otherwise/
        /// </summary>
        public bool ViewIsChanging { get; private set; }

        /// <summary>
        /// Allows using the 'async' statement to wait for the map view to stop changing.
        /// </summary>
        public async Task WhileViewIsChanging()
        {
            if (ViewIsChanging)
                await _Map.AsTask<MapEventArgs>("ViewChangeEnd");
        }

        // --------------------------------------------------------------------------------------------------

        public BingMap()
        {
            // ... set a min size as a default to make sure the control is usable ;) ...

            MinWidth = 256d;
            MinHeight = 256d;

            // ... create the map content ...

            Content = _Map = new Map
            {
                LogoVisibility = Visibility.Collapsed,
                CopyrightVisibility = Visibility.Collapsed,
                UseInertia = true,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                VerticalAlignment = System.Windows.VerticalAlignment.Stretch
            };

            _CenterPin = new Microsoft.Maps.MapControl.Pushpin
            {
                PositionOrigin = PositionOrigin.Center,
                Location = _Map.Center
            };
            _Map.Children.Add(_CenterPin);

            if (Utilities.InDesignMode) return;

            _GeocodeRequest = new GeocodeRequest();
            // (...limit the results to at least medium or higher confidence levels...)
            FilterBase[] filters = new FilterBase[1];
            filters[0] = new ConfidenceFilter { MinimumConfidence = Common.XAML.Controls.Maps.GeocodeService.Confidence.Medium };
            var geocodeOptions = new GeocodeOptions();
            geocodeOptions.Filters = filters;
            _GeocodeRequest.Options = geocodeOptions;

            // ... initialize a service request object to use later ...

            _Map.LoadingError += _Map_LoadingError;
            _Map.ViewChangeStart += (_s, _e) => { ViewIsChanging = true; };
            _Map.ViewChangeOnFrame += (_s, _e) => { _CenterPin.Location = _Map.Center; };
            _Map.ViewChangeEnd += (_s, _e) => { _FirstTimeViewChangedEnded = true; _CheckReady(); ViewIsChanging = false; };
            _Map.Loaded += (_s, _e) => { _MapLoaded = true; _CheckReady(); };

            if (!string.IsNullOrWhiteSpace(DefaultApplicationKey))
                _UpdateCredentials(); // (if a global app key is specified, use it now! [this is more the more reliable method to prevent the 'developer account required' prompt])
            else
                Dispatching.Dispatch(() => // (note: this method doesn't seems only reliable the first time, if used)
                {
                    _UpdateCredentials(); // (force this upon immediately at the next message event [the key should have been set immediately after construction, before the element gets loaded, else an 'developer account required' prompt will show])
                });

            Loaded += BingMap_Loaded;
            SizeChanged += BingMap_SizeChanged;
        }

        void BingMap_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _Map.Width = ActualWidth;
            _Map.Height = ActualHeight;
        }

        void _CheckReady()
        {
            if (IsReady)
                if (_Ready != null)
                {
                    _Ready(this, new EventArgs());
                    _Ready = null;
                }
        }

        void BingMap_Loaded(object sender, RoutedEventArgs e)
        {
        }

        void _Map_LoadingError(object sender, LoadingErrorEventArgs e)
        {
            if (LoadingError != null)
            {
                LoadingError(this, e);
                e.Handled = true;
            }
        }

        // ----------------------------------------------------------------------------------------------------

        public async Task<Common.XAML.Controls.Maps.GeocodeService.GeocodeResult[]> GeocodeAddress(string address)
        {
            _GeocodeRequest.Query = address;

            var geocodeService = new GeocodeServiceClient("BasicHttpBinding_IGeocodeService");
            geocodeService.GeocodeAsync(_GeocodeRequest); // (this starts the request and returns immediately; the next line will wait for the response)
            var args = await geocodeService.AsTask<GeocodeCompletedEventArgs>("GeocodeCompleted");

            if (args.Error != null)
                throw args.Error;

            return args.Result.Results;
        }

        // ----------------------------------------------------------------------------------------------------

        public void CenterMap(Common.XAML.Controls.Maps.GeocodeService.GeocodeResult result, double zoomLevel = 15d)
        {
            var mapCenter = new Microsoft.Maps.MapControl.Location(result.Locations[0].Latitude, result.Locations[0].Longitude);
            _Map.SetView(mapCenter, zoomLevel);
        }

        public void CenterMap(double longitude, double latitude, double? zoomLevel = null)
        {
            var mapCenter = new Microsoft.Maps.MapControl.Location(latitude, longitude);
            _Map.SetView(mapCenter, zoomLevel ?? _Map.ZoomLevel);
        }

        // ----------------------------------------------------------------------------------------------------

        public Microsoft.Maps.MapControl.Pushpin AddPin(double longitude, double latitude, string pinText, Color? color = null, PositionOrigin? positionOrigin = null)
        {
            var pin = new Microsoft.Maps.MapControl.Pushpin
            {
                Location = new Microsoft.Maps.MapControl.Location(latitude, longitude),
                PositionOrigin = positionOrigin ?? PositionOrigin.Center
            };

            if (!string.IsNullOrWhiteSpace(pinText))
            {
                ToolTipService.SetToolTip(pin, pinText);
                ToolTipService.SetPlacement(pin, PlacementMode.Top);
            }

            if (color != null)
                pin.Background = new SolidColorBrush(color.Value);

            _Map.Children.Add(pin);

            return pin;
        }

        // ----------------------------------------------------------------------------------------------------

        public Microsoft.Maps.MapControl.Pushpin UpdatePushPin(int index, string pinText, Color? color = null)
        {
            var pin = (Microsoft.Maps.MapControl.Pushpin)_Map.Children[index];

            if (!string.IsNullOrWhiteSpace(pinText))
            {
                ToolTipService.SetToolTip(pin, pinText);
                ToolTipService.SetPlacement(pin, PlacementMode.Top);
            }
            else
                pin.SetValue(ToolTipService.ToolTipProperty, null);

            if (color != null)
                pin.Background = new SolidColorBrush(color.Value);

            return pin;
        }

        // ----------------------------------------------------------------------------------------------------
    }
}

// Required Library Setup Steps: http://www.dotnetfunda.com/blogs/anil_ku2001/1511/using-bing-map-in-silverlight-to-show-the-location
// ... also: http://msdn.microsoft.com/en-us/library/cc879136.aspx
// Keys: https://www.bingmapsportal.com/application/index/1149896?status=NoStatus

