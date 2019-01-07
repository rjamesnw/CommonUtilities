using System;
using System.Collections;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Common;

namespace Common.XAML.Converters
{
    // #####################################################################################################################

    /// <summary>
    /// Converts a hex color string to a color and visa versa.
    /// Also, provides a static method to programmably convert hex strings to colors.
    /// </summary>
    public class ColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts a color string to a 'Color' object.
        /// </summary>
        /// <param name="colorString">A color string, which is either a supported color name from the 'Colors' type, or a XAML color type in the format "#AARRGGBB" or "#RRGGBB".</param>
        /// <returns>A 'Color' object.</returns>
        public static Color? FromString(string colorString)
        {
            if (colorString.IsNullOrWhiteSpace()) return null;

            // ... first, see if this is a named color ...

            Type colorType = typeof(System.Windows.Media.Colors);
            if (colorType.GetProperty(colorString) != null)
            {
                object c = colorType.InvokeMember(colorString, BindingFlags.GetProperty, null, null, null);
                if (c != null) return (Color)c;
            }

            // ... ok, not named, check if the value is in a XAML type color format ...
            if (colorString[0] == '#')
                colorString = colorString.Substring(1);

            Color color = new Color() { A = 0xFF };

            try // (attempt to convert the string hex values)
            {
                byte pos = 0;
                if (colorString.Length == 8)
                {
                    color.A = System.Convert.ToByte(colorString.Substring(pos, 2), 16);
                    pos += 2;
                }
                color.R = System.Convert.ToByte(colorString.Substring(pos, 2), 16);
                pos += 2;
                color.G = System.Convert.ToByte(colorString.Substring(pos, 2), 16);
                pos += 2;
                color.B = System.Convert.ToByte(colorString.Substring(pos, 2), 16);
            }
            catch { return null; }

            return color;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Color? color = FromString(Utilities.ND(value, ""));
            return new SolidColorBrush(color.HasValue ? color.Value : Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            SolidColorBrush brush = value as SolidColorBrush;
            return "#" + brush.Color.A.ToString() + brush.Color.R.ToString() + brush.Color.G.ToString() + brush.Color.B.ToString();
        }
    }

    // #####################################################################################################################

    /// <summary>
    /// Attempts to apply the specified color, and defaults to RED upon failure.
    /// (Used mainly for reading colors strings from a database)
    /// </summary>
    public class RequiredColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color? color = ColorConverter.FromString(Utilities.ND(value, ""));
            if (color.HasValue)
                return new SolidColorBrush(color.Value);
            return new SolidColorBrush(Colors.Red); // Error: no color defined, or invalid value. (defaults to solid red)
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush brush = value as SolidColorBrush;
            return "#" + brush.Color.A.ToString() + brush.Color.R.ToString() + brush.Color.G.ToString() + brush.Color.B.ToString();
        }
    }

    // #####################################################################################################################

    /// <summary>
    /// Shows only the date part of a DateTime object (default format is 'd-MMM-yyyy').
    /// The default format can be changed via the DefaultFormatString static property.
    /// </summary>
    public class DatePartConverter : IValueConverter
    {
        public static string DefaultFormatString = "d-MMM-yyyy"; // (for GUI display)
        public static string DefaultUnformatString = "yyyy-MM-dd"; // Note: "yyyyMMdd" is the HL7 timestamp format (for the database, if date is stored as a string)

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if (targetType == typeof(object))
                {
                    return value;
                }
                else if (targetType == typeof(string))
                {
                    try
                    {
                        // ... convert the value to a datetime object first ...
                        value = Utilities.ToDateTime(value, DateTime.MinValue);
                        DateTime d = DateTime.MinValue;
                        if (value is DateTime) d = (DateTime)value;
                        else if (value is DateTime?) d = (DateTime)(DateTime?)value;
                        if (d == DateTime.MinValue)
                            return "";
                        // ... return with expected formatting ...
                        return d.ToString(DefaultFormatString);
                    }
                    catch { }
                    return value.ToString();
                }
                else if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
                {
                    try
                    {
                        if (value is string)
                        {
                            DateTime datePart;
                            if (!DateTime.TryParse((string)value, out datePart))
                            {
                                // ... attempt to convert a time stamp format ...
                                string timestamp = (string)value;
                                datePart = DateTime.MinValue;
                                TimeSpan timePart = TimeSpan.Zero;
                                if (timestamp.Length >= 8) // yyyymmdd
                                    datePart = DateTime.Parse(timestamp.Substring(0, 4) + "-" + timestamp.Substring(4, 2) + "-" + timestamp.Substring(6, 2));
                                if (timestamp.Length >= 14) // yyyymmddhhmmss
                                    timePart = TimeSpan.Parse(timestamp.Substring(8, 2) + ":" + timestamp.Substring(10, 2) + ":" + timestamp.Substring(12, 2));
                                if (datePart == DateTime.MinValue && timePart == TimeSpan.Zero)
                                    return null;
                                return datePart + timePart;
                            }
                            else value = datePart;
                        }
                        if (value is DateTime) return (DateTime)value;
                        if (value is DateTime?) return ((DateTime?)value).Value;
                    }
                    catch { }
                    return new DateTime();
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(DateTime)) return DateTime.Parse(value.ToString());
            if (targetType == typeof(string))
                if (value is DateTime) return ((DateTime)value).ToString(DefaultUnformatString);
                else if (value is DateTime) return ((DateTime)value).ToString(DefaultUnformatString);
                else return Utilities.ND(value, "");
            return null;
        }
    }

    // #####################################################################################################################

    /// <summary>
    /// Shows only the time part of a DateTime object (default format is 'HH:mm').
    /// The default format can be changed via the DefaultFormatString static property.
    /// </summary>
    public class TimePartConverter : IValueConverter // TODO: Fix this up like the date converter above.
    {
        public static string DefaultFormatString = "HH:mm";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string retValue = "";
            if (value != null)
            {
                DateTime? date = null;

                if (value is DateTime?)
                    date = (DateTime?)value;
                else if (value is DateTime)
                    date = (DateTime)value;
                else if (value is string)
                    date = DateTime.Parse((string)value);

                if (date != null)
                {
                    retValue = date.Value.ToString(DefaultFormatString);
                }
            }
            return retValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DateTime.Parse(value.ToString());
        }
    }


    // #####################################################################################################################

    /// <summary>
    /// Attempts to convert a primitive value representation, to an actual primitive value.
    /// Example: 'Yes'/'No'/'True'/'False'/'1'/'0' strings to true/false boolean values, and vice versa.
    /// Note: Only numerical values and strings supported.
    /// </summary>
    public class PrimitiveValueOrDefaultConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value.GetType().Equals(targetType))
                return value;

            if (targetType == typeof(bool?)) targetType = typeof(bool);
            if (targetType == typeof(Int32?)) targetType = typeof(Int32);
            if (targetType == typeof(int?)) targetType = typeof(int);
            if (targetType == typeof(double?)) targetType = typeof(double);
            if (targetType == typeof(float?)) targetType = typeof(float);
            if (targetType.IsClass && parameter is string)
                targetType = Type.GetType((String)parameter);

            string defaultStr = "";
            string[] parameters = (parameter != null) ? parameter.ToString().Split(',') : null;
            if (parameters != null) defaultStr = parameters[0];

            if (targetType == typeof(bool))
            {
                // (if converting back to boolean from a string, make sure to check against the parameters, if specified)
                string falseStr = (parameters != null || defaultStr != "") ? defaultStr : "No";
                string trueStr = (parameters != null && parameters.Length > 1) ? parameters[1] : "Yes";
                string val = Utilities.ND(value, "");
                if (val == falseStr) return false;
                if (val == trueStr) return true;
                return Utilities.ND(value, Utilities.ToBoolean(defaultStr, false));
            }
            else if (targetType == typeof(string))
            {
                string falseStr = (parameters != null || defaultStr != "") ? defaultStr : "No";
                string trueStr = (parameters != null && parameters.Length > 1) ? parameters[1] : "Yes";
                if (value is string) return value;
                if (value is bool) return (bool)value ? trueStr : falseStr;
                return Utilities.ND(value, defaultStr);
            }
            else if (targetType == typeof(Int16))
            {
                if (value is Int16) return value;
                return Utilities.ND(value, Utilities.ToInt16(defaultStr, 0));
            }
            else if (targetType == typeof(Int32) || targetType == typeof(int))
            {
                if (value is Int32 || value is int) return value;
                return Utilities.ND(value, Utilities.ToInt32(defaultStr, 0));
            }
            else if (targetType == typeof(Int64))
            {
                if (value is Int64) return value;
                return Utilities.ND(value, Utilities.ToInt64(defaultStr, 0));
            }
            else if (targetType == typeof(double) || targetType == typeof(float))
            {
                if (value is double || value is float) return value;
                return Utilities.ND(value, Utilities.ToDouble(defaultStr, 0.0));
            }
            else if (targetType == typeof(object))
            {
                return value;
            }
            return null; // (unsupported type conversion)
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }
    }

    // #####################################################################################################################

    /// <summary>
    /// Converts a value into a combobox item, if a matching item object exists.
    /// The ConverterParameter is expected to be IEnumerable, or contain a reference to an object with an enumerable "ItemsSource" property.
    /// Note: Comparison doesn't work on items with duplicate "displayable names".
    /// Example: See usage in SWDataGrid.cs.
    /// </summary>
    public class ItemsSourceSelectConverter : IValueConverter
    {
        static PrimitiveValueOrDefaultConverter _PrimitiveConverter = new PrimitiveValueOrDefaultConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!targetType.IsClass || targetType == typeof(string)) // (is this actually a primitive type?)
                return _PrimitiveConverter.Convert(value, targetType, parameter, culture);
            else
            {
                IEnumerable list = (parameter is IEnumerable) ? (parameter as IEnumerable)
                    : Objects.GetPropertyOrFieldValue<IEnumerable>(parameter, "ItemsSource");
                if (list != null)
                    foreach (object item in list)
                        if (Utilities.ND(item, "") == Utilities.ND(value, ""))
                            return item;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    // #####################################################################################################################

    /// <summary>
    /// Converts "double" types to grid lengths and visa versa.
    /// </summary>
    public class DoubleToGridLengthConverter : IValueConverter // TODO: Fix this up like the date converter above.
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            if (value is GridLength)
            {
                if (targetType == typeof(GridLength)) return targetType;
                if (((GridLength)value).GridUnitType == GridUnitType.Pixel)
                    value = ((GridLength)value).Value;
                else
                    value = 0d; // (can't convert non-pixel length to a length)
            }
            else
            {
                if (!Utilities.IsNumeric(value.GetType())) return null;
                if (!Utilities.IsFloat(value.GetType())) value = Utilities.ND(value, 0d);
            }
            if (targetType == typeof(GridLength))
                return new GridLength((double)value, GridUnitType.Pixel);
            else
                if (Utilities.IsNumeric(targetType))
                    return System.Convert.ChangeType(value, targetType, System.Threading.Thread.CurrentThread.CurrentCulture);
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }
    }

    // #####################################################################################################################

    /// <summary>
    /// Provides a converter to convert from a file name, to a bitmap image. If a full path is not given, then The
    /// filename is expected to be the name of an image file in a local assembly folder (which is /'Images' by default - as determined by the 'DefaultImagesPath' static property). 
    /// The given parameter is expected to be the assembly name where the image can be found. If this is null, then
    /// the calling assembly is assumed.
    /// <para>Any path that contains ";" in it will be treated as an assembly resource.</para>
    /// <para>Any path that contains "://" in it will be treated as a URI.</para>
    /// <para>Putting "://*/" in the path causes it to be replaced with the current server and port.</para>
    /// </summary>
    public class ImageConverter : IValueConverter
    {
        public static string DefaultAssembly = ""; // ("" == auto detect)
        public static string DefaultImagesPath = "Images";
#if SILVERLIGHT
        public const string DefaultNoImagePath = "/Common_SL;component/Images/default.png";
#elif WPF
        public const string DefaultNoImagePath = "pack://application:,,,/Common_WPF;component/Images/default.png";
#endif

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            BitmapImage image = new BitmapImage();
            bool detectingImageFilename = false;

            if (value != null)
            {
                var imageSource = Utilities.ND(value, "");
                var imgSrcLC = imageSource.ToLower();

                if (!imgSrcLC.StartsWith("pack://") && imgSrcLC.Contains("://")) // (if true, this looks like an absolute URI)
                {
#if SILVERLIGHT
                    var currentRootPath = Utilities.GetNewDocumentPath("");
                    imageSource = imageSource.Replace("://*/", currentRootPath.Substring(4));
#else
                    // ... use AS IS for WPF (not web based, so there's no "document root") ...
#endif
                }
                else if (!imageSource.Contains(";component/")) // (i.e. If ONLY a filename [with or without a path] is given, and not a URI or Assembly path)
                {
                    // ... get the local assembly path ('parameter' may contain the assembly name) ...

                    var assemblyName = "";

                    if (parameter != null)
                        assemblyName = parameter.ToString();
                    else if (DefaultAssembly != "")
                        assemblyName = DefaultAssembly;
                    else
                        assemblyName = Assembly.GetCallingAssembly().FullName.Split(',')[0];

                    if (DefaultImagesPath.StartsWith("/") || DefaultImagesPath.EndsWith("/"))
                        DefaultImagesPath = DefaultImagesPath.Trim('/');

                    if (!imageSource.StartsWith("/") && DefaultImagesPath != "")
                        imageSource = "/" + imageSource;

                    // ... build path to image ...

                    var extension = "";
                    if (!imageSource.Contains(".")) // (if there's no extension, this can be detected...)
                    {
                        detectingImageFilename = true;
                        extension = ".png"; // (assume PNG if no extension is given)
                        // ... if .png fails, try .jpg ...

#if SILVERLIGHT
                        var jpgImageSource = "/" + assemblyName + ";component/" + DefaultImagesPath + imageSource + ".jpg";
                        image.ImageFailed += (s, e) =>
#else
                        var jpgImageSource = "pack://application:,,,/" + assemblyName + ";component/" + DefaultImagesPath + imageSource + ".jpg";
                        image.DownloadFailed += (s, e) =>
#endif
                        {
                            if (jpgImageSource != "")
                                image.UriSource = new Uri(jpgImageSource, UriKind.RelativeOrAbsolute); // ('jpgImageSource' will be cleared next, so if this attempt fails, the default "no image" image will load on next pass)
                            else
                                image.UriSource = new Uri(DefaultNoImagePath, UriKind.RelativeOrAbsolute);
                            jpgImageSource = "";
                        };
                    }

#if SILVERLIGHT
                    imageSource = "/" + assemblyName + ";component/" + DefaultImagesPath + imageSource + extension;
#else
                    imageSource = "pack://application:,,,/" + assemblyName + ";component/" + DefaultImagesPath + imageSource + extension;
#endif
                }

                if (!detectingImageFilename)
#if SILVERLIGHT 
                image.ImageFailed +=  (s, e) =>
#else
                    image.DownloadFailed += (s, e) =>
#endif
                    { image.UriSource = new Uri(DefaultNoImagePath, UriKind.RelativeOrAbsolute); };

#if !SILVERLIGHT
                image.BeginInit();
#endif
                image.UriSource = new Uri(imageSource, UriKind.RelativeOrAbsolute);
#if !SILVERLIGHT
                image.EndInit();
#endif
            }

            return image;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion

        public static BitmapImage LoadBitmapImage(string imageFilename, string assemblyName)
        {
            var imageLoader = new ImageConverter();
            return imageLoader.Convert(imageFilename, typeof(BitmapImage), assemblyName, Thread.CurrentThread.CurrentCulture) as BitmapImage;
        }
        public static BitmapImage LoadBitmapImage(string imageFilename) { return LoadBitmapImage(imageFilename, null); }

        static BitmapImage _DefaultNoImage;
        public static BitmapImage DefaultNoImage
        {
            get
            {
#if SILVERLIGHT 
                return _DefaultNoImage ?? (_DefaultNoImage = LoadBitmapImage("default.png", "Common_SL"));
#elif WPF
                return _DefaultNoImage ?? (_DefaultNoImage = LoadBitmapImage("default.png", "Common_WPF"));
#endif
            }
        }
    }

    // #####################################################################################################################

    /// <summary>
    /// Converts Boolean to a visibility value.
    /// </summary>
    public class VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Utilities.ND(value, false) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && value.Equals(Visibility.Visible);
        }
    }

    // #####################################################################################################################

    /// <summary>
    /// Formats byte sizes.
    /// </summary>
    public class ByteSizeConverter : IValueConverter
    {
        static ByteSizeConverter _Converter = new ByteSizeConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var size = Utilities.ND(value, (Int64)0); // (convert to Int64 first to make sure there's no fractions)
            return Utilities.GetShortByteSizeDescription(size);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static string GetByteSizeSuffix(Int64 size)
        {
            return (string)_Converter.Convert(size, null, null, null);
        }
    }

    // #####################################################################################################################
}
