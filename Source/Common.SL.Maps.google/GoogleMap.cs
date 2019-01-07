using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Browser;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Xml.Linq;
using System.Xml.XPath;
using Common.XAML.Converters;

namespace Common.XAML.Controls.Maps
{
    public class GoogleMap : ContentControl
    {
        // --------------------------------------------------------------------------------------------------

        public string APIKey
        {
            get { return _APIKey; }
            set
            {
                if (_GoogleMapsAPIScriptLoaded)
                    throw new InvalidOperationException("APIKey: The Google Maps API is already loaded with the current key.");
                _APIKey = value;
                if (_IsLoaded) LoadGoogleMapsAPIScript();
            }
        }
        string _APIKey;

        public string GoogeMapsAPIUrl // HTTP S?
        {
            get { return string.Format("{0}://maps.googleapis.com/maps/api/js?sensor={1}&key={2}", (_UseHTTPS ? "https" : "http"), (_UsingSensor ? "true" : "false"), _APIKey); }
        }

        public bool UseHTTPS
        {
            get { return _UseHTTPS; }
            set
            {
                if (_GoogleMapsAPIScriptLoaded)
                    throw new InvalidOperationException("UseHTTPS: The Google Maps API is already loaded with the current protocol.");
                _UseHTTPS = value;
            }
        }
        bool _UseHTTPS;

        public bool UsingSensor
        {
            get { return _UsingSensor; }
            set
            {
                if (_GoogleMapsAPIScriptLoaded)
                    throw new InvalidOperationException("UsingSensor: The Google Maps API is already loaded with the current setting.");
                _UsingSensor = value;
            }
        }
        bool _UsingSensor;

        // --------------------------------------------------------------------------------------------------
        // Geocoding

        /// <summary>
        /// The addresses which closely match the address passed to the 'GeocodeAddress()' method.
        /// These results are only available after the 'GeocodeCompleted' event fires.
        /// </summary>
        public XDocument GeocodeResult { get; private set; }
        public bool GeocodeSuccessful { get; private set; }
        public string GeocodeErrorMessage { get; private set; }

        /// <summary>
        /// Fires when the required scripts are loaded and ready.
        /// If the scripts are already loaded, the added handler is called immediately.
        /// Performing any actions Google Maps API related actions will fail until this event occurs.
        /// </summary>
        public event Action<GoogleMap> GoogleAPIReady
        {
            add { if (_GoogleMapsAPIScriptLoaded) value(this); else  _GoogleAPIReady += value; }
            remove { _GoogleAPIReady -= value; }
        }
        Action<GoogleMap> _GoogleAPIReady;

        /// <summary>
        /// Fires after a called to 'GeocodeAddress()' completes.
        /// When this event occurs, 'GeocodeSuccessful', 'GeocodeResult', and 'GeocodeErrorMessage' properties contain the status of the call to 'GeocodeAddress()'.
        /// If 'GeocodeSuccessful' is 'false', any error message is stored in 'GeocodeErrorMessage'.
        /// </summary>
        public event Action<GoogleMap> GeocodeCompleted;

        /// <summary>
        /// Fires when the map is updated.
        /// The map is always set to the first result after calling 'GeocodeAddress()'.
        /// </summary>
        public event Action<GoogleMap> MapUpdated;

        // --------------------------------------------------------------------------------------------------

        bool _IsLoaded;
        bool _GoogleMapsAPIScriptLoading;
        bool _GoogleMapsAPIScriptLoaded;
        ScriptObject _GoogleMapUtilities;

        WebBrowser _WebBrowser;
        object _Content;

        HtmlElement _SLPlugin;
        HtmlElement _Div;

        bool _DivIsVisibile
        {
            get { return _Div != null ? _Div.GetStyleAttribute("visibility") == "visible" : false; }
            set { if (_Div != null) _Div.SetStyleAttribute("visibility", value ? "visible" : "hidden"); }
        }

        // --------------------------------------------------------------------------------------------------

        string _DivIDGUID = Guid.NewGuid().ToString("N");

        /// <summary>
        /// The ID of the DIV element created in the HTML DOM of the host browser window.
        /// </summary>
        public string DivID { get { return "_googleMap_" + _DivIDGUID; } }

        // --------------------------------------------------------------------------------------------------

        public GoogleMap()
        {
            if (Application.Current.IsRunningOutOfBrowser)
            {
                Content = _WebBrowser = new WebBrowser();
                //??_WebBrowser.LoadCompleted += (s, e) => { };
                _WebBrowser.ScriptNotify += (s, e) => // (e.Value will contain either "ready", when the scripts are loaded, or the result of the last API call)
                {
                    if (e.Value == "ready")
                        _DoScriptsLoaded();
                    else if (e.Value == "mapUpdated")
                        _DoMapUpdated();
                    else
                    {
                        string[] lines = e.Value.Split(new string[] { "\r\n" }, StringSplitOptions.None); // (line 0: success, line 1: status, rest is result xml)
                        var xml = lines.Length > 2 ? string.Join("\r\n", lines, 2, lines.Length - 2) : "";

                        if (lines.Length > 1)
                            _DoGeocodeCompleted(Utilities.ND(lines[0], false), lines[1], xml);
                        else if (lines.Length > 0)
                            _DoGeocodeCompleted(Utilities.ND(lines[0], false), "", "");
                        else
                            _DoGeocodeCompleted(false, "ScriptNotify: Invalid XML received.", "");
                    }
                };
            }
            else if (!HtmlPage.IsEnabled)
            {
                if (!Utilities.InDesignMode)
                    throw new InvalidOperationException("GoogleMap(): The DOM is not accessible.");
            }
            else
            {
                try
                {
                    _SLPlugin = HtmlPage.Plugin;

                    var parentElement = _SLPlugin.Parent; // (note: it is expected that the parent element of the Silverlight plugin object is a DIV [or at least supports adding DIV tags as children])

                    _Div = HtmlPage.Document.CreateElement("div");
                    _Div.Id = DivID;
                    _Div.SetStyleAttribute("position", "absolute");
                    _Div.SetStyleAttribute("visibility", "hidden");
                    _Div.SetStyleAttribute("z-index", "999999");
                    _Div.SetStyleAttribute("top", "0px");
                    _Div.SetStyleAttribute("left", "0px");
                    _Div.SetStyleAttribute("height", "64px");
                    _Div.SetStyleAttribute("width", "64px");//style="position: relative; zoom: 1; text-align:left"
                    _Div.SetStyleAttribute("background-color", "#000000");
                    // TODO: change style of parent of DIV to "text-align:left"

                    parentElement.AppendChild(_Div);

                    _UpdateDivVisibility();
                }
                catch (Exception ex) /* something is not compatible with the browser (usually happens in Safari for Windows) */
                {
                    _Div = null;
#if DEBUG
                    MessageBox.Show("GoogleMap DOM Access Error:\r\n" + ex.Message);
#else
                    return; /*do nothing*/
#endif
                }
            }

            SizeChanged += GoogleMap_SizeChanged;
            LayoutUpdated += GoogleMap_LayoutUpdated;
            Loaded += (s, e) => { _IsLoaded = true; LoadGoogleMapsAPIScript(); _UpdateDivVisibility(); };
            Unloaded += (s, e) => { _IsLoaded = false; _DivIsVisibile = false; };
        }

        /// <summary>
        /// Loads the Google Maps API and returns 'null' on success, or an exception object on error.
        /// This is automatically called when the control loads, but you can force it ahead of time by calling this method.
        /// </summary>
        public Exception LoadGoogleMapsAPIScript()
        {
            if (_WebBrowser != null && !_GoogleMapsAPIScriptLoaded)
            {
                if (string.IsNullOrWhiteSpace(_WebBrowser.Source.OriginalString))
                {
                    _WebBrowser.Source = new Uri(Utilities.GetNewDocumentPath("GoogleMap.html?mapUrl=" + GoogeMapsAPIUrl), UriKind.Absolute);
                    _GoogleMapsAPIScriptLoading = true;
                }
                return null;
            }
            else
                try
                {
                    if (!_GoogleMapsAPIScriptLoaded && !_GoogleMapsAPIScriptLoading)
                    {
                        //??var name = (string.IsNullOrWhiteSpace(Name)) ? Guid.NewGuid().ToString("N") : Name;
                        //??HtmlPage.RegisterScriptableObject("GoogleMap_" + name, this);

                        if (HtmlPage.Window.GetProperty("isMapsAPILoaded") == null) // (check if the script is already in place)
                        {
                            HtmlPage.Window.Eval(@"
var isUtilitiesLoaded = false;
var isMapsAPILoaded = false;
var slCtrls = []; // (if multiple controls are created in Silverlight, this calls all who are waiting)
var ctrlIndex = 0;

function doReadyCheck(slCtrl) {
    if (typeof slCtrl != 'undefined') {
        slCtrls[ctrlIndex++] = slCtrl;
    }
    if (isMapsAPILoaded && isUtilitiesLoaded)
        if (ctrlIndex > 0) {
            for (var i = 0; i < ctrlIndex; i++)
                slCtrls[i]._ScriptsLoaded();
            ctrlIndex = 0;
        }
}

// ... the completed loading of the utilities will trigger detection of the Google Maps API load status check ...
function onGoogleMapUtilitiesLoaded() {
    isUtilitiesLoaded = true;
    doReadyCheck();
}
function onGoogleMapAPILoaded() { // (this is called via the Google Maps API loader when it is finished [see 'mapUrl' above])
    isMapsAPILoaded = true;
    doReadyCheck();
}
");
                            var script = HtmlPage.Document.CreateElement("script") as HtmlElement;
                            script.SetProperty("type", "text/javascript");
                            script.SetProperty("language", "javascript");
                            script.SetProperty("src", GoogeMapsAPIUrl + "&callback=onGoogleMapAPILoaded");
                            HtmlPage.Document.Body.AppendChild(script);

                            script = HtmlPage.Document.CreateElement("script") as HtmlElement;
                            script.SetProperty("type", "text/javascript");
                            script.SetProperty("language", "javascript");
                            script.SetProperty("src", Utilities.GetNewDocumentPath("GoogleMapUtilities.js"));
                            HtmlPage.Document.Body.AppendChild(script);
                        }

                        _GoogleMapsAPIScriptLoading = true;

                        HtmlPage.Window.Invoke("doReadyCheck", this); // (if scripts are already loaded then there's no wait and '_ScriptsLoaded()' is called immediately)
                    }

                    return null;
                }
                catch (Exception ex) { return ex; }

            //??return new InvalidOperationException("GoogleMaps(): APIKey property is not set.");
        }

        [ScriptableMember]
        public void _ScriptsLoaded()
        {
            _UpdateMapPositionAndSize(); // (need to let the Google API know the expected size for this DIV!)
            _GoogleMapUtilities = HtmlPage.Window.CreateInstance("GoogleMapUtilities", this, _Div.Id);
            _DoScriptsLoaded();
        }

        void _DoScriptsLoaded()
        {
            _GoogleMapsAPIScriptLoading = false;
            _GoogleMapsAPIScriptLoaded = true;
            if (_GoogleAPIReady != null)
                _GoogleAPIReady(this);
        }

        [ScriptableMember]
        public void _DoMapUpdated()
        {
            if (MapUpdated != null)
                MapUpdated(this);
        }

        // ----------------------------------------------------------------------------------------------------

        void GoogleMap_LayoutUpdated(object sender, EventArgs e)
        {
            if (VisualTree.IsInVisualTree(this))
            {
                _UpdateDivVisibility();
                _UpdateMapPositionAndSize();
            }
        }

        void GoogleMap_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _UpdateMapPositionAndSize();
        }

        void _UpdateMapPositionAndSize()
        {
            if (_WebBrowser != null)
            {
                if (_WebBrowser.Width != this.ActualWidth)
                    _WebBrowser.Width = this.ActualWidth;
                if (_WebBrowser.Height != this.ActualHeight)
                    _WebBrowser.Height = this.ActualHeight;
            }
            else if (_Div != null && _IsLoaded && this.IsVisible())
            {
                // ... locate position of this control in the visual tree and add to the DOM position ...

                try
                {
                    var thisPos = this.GetRootVisualPosition();
                    var x = thisPos.X;
                    //var y = -Application.Current.Host.Content.ActualHeight + thisPos.Y; only for relative positioning.
                    var y = thisPos.Y;
                    //??_Div.SetStyleAttribute("margin-top", y.ToString() + "px"); only for relative positioning.
                    _Div.SetStyleAttribute("top", y.ToString() + "px");
                    _Div.SetStyleAttribute("left", x.ToString() + "px");
                    _Div.SetStyleAttribute("height", this.ActualHeight.ToString() + "px");
                    _Div.SetStyleAttribute("width", this.ActualWidth.ToString() + "px");
                }
                catch { /*no in visual tree anymore!*/ _IsLoaded = false; }
            }
        }

        void _UpdateDivVisibility()
        {
            if (_Div != null)
            {
                var shouldBeVisible = this.IsVisible() && _IsLoaded;
                if (shouldBeVisible != _DivIsVisibile)
                    _DivIsVisibile = shouldBeVisible;
            }
        }

        public void UpdatePositionAndVisibility()
        {
            _UpdateDivVisibility();
            _UpdateMapPositionAndSize();
        }

        // ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Calls the Google Maps API to geocode an address.
        /// When the results are available, 'GeocodeSuccessful', 'GeocodeResult', and 'GeocodeErrorMessage' properties are updated, and the 'GeocodeCompleted' event will fire.
        /// </summary>
        public void GeocodeAddress(string address)
        {
            if (!_GoogleMapsAPIScriptLoaded && _WebBrowser == null)
                throw new InvalidOperationException("GeocodeAddress(): The Google Maps API is not loaded yet.");

            if (_GoogleMapUtilities != null)
                _GoogleMapUtilities.Invoke("GeocodeAddress", address);
            else if (_WebBrowser != null)
                _WebBrowser.InvokeScript("geocodeAddress", address);
        }


        [ScriptableMember]
        public void _DoGeocodeCompleted(bool successful, string errorMessage, string resultXML)
        {
            try { GeocodeResult = XDocument.Parse(resultXML); }
            catch (Exception ex) { GeocodeResult = null; successful = false; errorMessage = ex.Message; }

            GeocodeSuccessful = successful;
            GeocodeErrorMessage = errorMessage;

            if (GeocodeCompleted != null)
                GeocodeCompleted(this);
        }

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns an array of addresses found from the last call to 'GeocodeAddress()'.
        /// Results are available when the 'GeocodeSuccessful' even fires.
        /// </summary>
        public string[] GetGeocodeAddressMatches()
        {
            return GeocodeResult == null ? new string[0]
                : (from xn in GeocodeResult.Elements("results").Elements("items")
                   select xn.Attribute("formatted_address").Value).ToArray();
        }

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns the longitude and latitude of a given address in the geocoded results.
        /// If no results exist, or the address is not found, this method will fail.
        /// </summary>
        /// <param name="address">The EXACT address returned from 'GetGeocodeAddressMatches()'.</param>
        public bool GetAddressCoordinates(string address, out double latitude, out double longitude)
        {
            latitude = 0;
            longitude = 0;
            if (GeocodeResult == null) return false;
            try
            {
                XElement location = (from xn in GeocodeResult.Elements("results").Elements("items")
                                     where xn.Attribute("formatted_address").Value == address
                                     select xn.Element("geometry").Element("location")).FirstOrDefault();
                if (location == null) return false;
                // (note: the Google Maps API has the latitude and longitude backwards from the norm)
                latitude = double.Parse(location.Attribute("latitude").Value);
                longitude = double.Parse(location.Attribute("longitude").Value);
            }
            catch { return false; }
            return true;
        }

        // ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Sets the map to the specified address.
        /// If the address is the exact value returned from calling "GetGeocodeAddressMatches()" (the 'formatted_address' API value) then no API call is needed, and 'MapUpdated' is fired when done.
        /// If there is no match, or no previous results, then a Geocode lookup is made instead, and both 'GeocodeCompleted' and 'MapUpdated' are called.
        /// </summary>
        public void SetMapAddress(string address)
        {
            if (!_GoogleMapsAPIScriptLoaded && _WebBrowser == null)
                throw new InvalidOperationException("SetMapAddress(): The Google Maps API is not loaded yet.");

            if (_GoogleMapUtilities != null)
                _GoogleMapUtilities.Invoke("UpdateMapWithAddress", address);
            else if (_WebBrowser != null)
                _WebBrowser.InvokeScript("updateMapWithAddress", address);
        }

        // ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Attempts to set the map to the specified location and zoom level.
        /// </summary>
        public void SetMapCenter(double latitude, double longitude, int zoomLevel = 16)
        {
            if (!_GoogleMapsAPIScriptLoaded && _WebBrowser == null)
                throw new InvalidOperationException("SetMapCenter(): The Google Maps API is not loaded yet.");

            if (_GoogleMapUtilities != null)
                _GoogleMapUtilities.Invoke("CenterMap", latitude, longitude, zoomLevel); // (note: the Google Maps API has the latitude and longitude backwards from the norm)
            else if (_WebBrowser != null)
                _WebBrowser.InvokeScript("centerMap", latitude.ToString(), longitude.ToString(), zoomLevel.ToString());
        }

        // ----------------------------------------------------------------------------------------------------
    }
}

// Testing page: https://google-developers.appspot.com/maps/documentation/javascript/examples/geocoding-simple (use script console to play with it)
/*
   Examples: 
         
    map 
    {
        gm_accessors_ : [object Object],
        zoom : 8,
        center : (-34.397, 150.644),
        mapTypeId : "roadmap",
        e : [object Object],
        ac : [object Object],
        mapTypes : [object Object],
        features : [object Object],
        bc : [object Object],
        re : [object Object]
        ...
    }
            
    result.geometry 
    {
        bounds : ((41.6813277, -95.156227), (56.8565279, -74.3438476)),
        location : (51.253775, -85.32321389999998), <-- NOTE: Latitude is FIRST (See toString() below)
        location_type : "APPROXIMATE",
        viewport : ((43.5391202, -101.7148158), (57.86121319999999, -68.93161199999997))
    }
            
    result.geometry.location 
    (51.253775, -85.32321389999998) {
        $a : 51.253775,
        ab : -85.32321389999999,
        toString : function(){return"("+this.lat()+", "+this.lng()+")"},
        equals : function(a){return!a?k:Dd(this.lat(),a.lat())&&Dd(this.lng(),a.lng())},
        lat : function(){return this[a]},
        lng : function(){return this[a]},
        toUrlValue : function(a){a=Id(a)?a:6;return ae(this.lat(),a)+","+ae(this.lng(),a)}
    }
 */
