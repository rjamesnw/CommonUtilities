using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Common
{
    // ########################################################################################################################

    public static class MVCUtilities
    {
        // --------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns the value stored in the web.config 'AppPublicURL' app setting,
        /// which represents the root URL of the application, such as 'https://mydomain.com".
        /// This is useful in cases where a proxy server is redirecting requests and changing
        /// the request paths to local network/server paths. Any dynamic detection of the domain
        /// may not work in such cases, and the rendered responses may contain invalid URIs.
        /// </summary>
        public static string AppPublicURL = ConfigurationManager.AppSettings["AppPublicURL"];

        public static string AppURLBase { get; private set; }

        public static string LocalHostName { get; set; }

        public static string APP_NAME = ConfigurationManager.AppSettings["AppName"];
        public static string APP_LOGO = ConfigurationManager.AppSettings["AppLogo"];
        public static string SMTP_SENDER_EMAIL = ConfigurationManager.AppSettings["SMTPSender"];
        public static string SMTP_SENDER_EMAIL_SERVER = ConfigurationManager.AppSettings["SMTPServer"];
        public static int SMTP_SENDER_EMAIL_SERVER_PORT = Common.Utilities.ND(ConfigurationManager.AppSettings["SMTPPort"], 25);
        public static string SMTP_SENDER_EMAIL_USERNAME = ConfigurationManager.AppSettings["SMTPUsername"];
        public static string SMTP_SENDER_EMAIL_PASSWORD = ConfigurationManager.AppSettings["SMTPPassword"];
        public static int REGISTRATION_DAYS_EXPIRATION = Common.Utilities.ND(ConfigurationManager.AppSettings["RegistrationExpiration"], 30);
        public static int DELETED_USER_EXPIRATION = Common.Utilities.ND(ConfigurationManager.AppSettings["DeletedUserExpiration"], 90);
        public static int AUDIT_LOG_EXPIRATION = Common.Utilities.ND(ConfigurationManager.AppSettings["AuditLogExpiration"], 30);

        // --------------------------------------------------------------------------------------------------------------------

        static MVCUtilities()
        {
            var request = HttpContext.Current?.Request;
            if (request != null)
            {
                var appUrl = HttpRuntime.AppDomainAppVirtualPath;
                if (appUrl != "/") appUrl += "/";

                if (System.Diagnostics.Debugger.IsAttached || string.IsNullOrWhiteSpace(AppPublicURL))
                {
                    AppURLBase = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl);

                    if (!string.IsNullOrWhiteSpace(LocalHostName))
                        AppURLBase = AppURLBase.Replace("//localhost", "//" + LocalHostName); // (using dev PC name allows links to work on internal networks during development/testing)
                }
                else
                    AppURLBase = Strings.Append(AppPublicURL, appUrl, "/");
            }
            else
                AppURLBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
        }

        // --------------------------------------------------------------------------------------------------------------------

        public static string GetRelaiveURLFromAsoluteFilePath(string absoluteFilePath)
        {
            if (HttpContext.Current == null)
                throw new InvalidOperationException("'HttpContext.Current' is null.");
            var relativePath = absoluteFilePath?.Replace(HttpContext.Current.Request.ServerVariables["APPL_PHYSICAL_PATH"], String.Empty) ?? "";
            return relativePath.Replace('\\', '/');
        }

        public static string GetAbsoluteURLFromAsoluteFilePath(string absoluteFilePath)
        {
            return AppURLBase + GetRelaiveURLFromAsoluteFilePath(absoluteFilePath);
        }

        // --------------------------------------------------------------------------------------------------------------------

        [DbFunction("TimeSheetEntityModels", "IsLike")]
        public static bool IsLike(this string str, string pattern)
        {
            throw new NotSupportedException("Supported for SQL queries only.");
        }

        // --------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns the client (if available in cookie) or server timezone, in minutes.
        /// </summary>
        public static int GetClientTimeZoneOffset()
        {
            var request = HttpContext.Current.Request;
            // Default to the server time zone.
            TimeZone tz = TimeZone.CurrentTimeZone;
            TimeSpan ts = tz.GetUtcOffset(DateTime.Now);
            int result = (int)ts.TotalMinutes;
            // Then check for client time zone (minutes) in a cookie.
            HttpCookie cookie = request.Cookies["ClientTimeZone"];
            if (cookie != null)
            {
                int clientTimeZone;
                if (Int32.TryParse(cookie.Value, out clientTimeZone))
                    result = clientTimeZone;
            }
            return result;
        }

        /// <summary>
        /// Returns the week start and end dates that surround a given date.
        /// </summary>
        public static void GetWeekDateRange(DateTime date, out DateTime sunday, out DateTime saturday)
        {
            sunday = date.AddDays(-(int)date.DayOfWeek); // (will reset the date to the first day of the week that contains this date)
            saturday = sunday.AddDays(6);
            // ... need to adjust the times as well ...
            sunday = sunday.Date;
            saturday = saturday.Date.AddDays(1).AddTicks(-1);
        }
        /// <summary>
        /// Returns the week start and end dates that surround a given date, adjusted for the client time zone.
        /// </summary>
        public static void GetClientWeekDateRange(out DateTime sunday, out DateTime saturday, DateTime? date = null)
        {
            DateTime _date = date ?? DateTime.UtcNow;
            var clientTimezone = MVCUtilities.GetClientTimeZoneOffset();
            _date = _date.AddMinutes(clientTimezone);
            GetWeekDateRange(_date, out sunday, out saturday);
        }
        /// <summary>
        /// Returns last week's week start and end dates from today (UTC), adjusted for the client time zone.
        /// </summary>
        public static void GetClientLastWeekDateRange(out DateTime sunday, out DateTime saturday)
        {
            GetClientWeekDateRange(out sunday, out saturday, DateTime.UtcNow.AddDays(-7));
        }

        // --------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns a URL to call a web API.
        /// </summary>
        /// <param name="url">A 'UrlHelper' instance (usually 'WebViewPage.Url').</param>
        /// <param name="controller">The API controller name.</param>
        /// <param name="id">An ID to include with the URL, if any.</param>
        /// <param name="route">An optional route name. If not specified, then 'DefaultApi' is assumed.</param>
        /// <returns>A URL string.</returns>
        public static string API(this UrlHelper url, string controller, string id = null, string route = "DefaultApi")
        {
            if (id != null)
                return url.RouteUrl(route, new { httproute = "", controller = controller, id = id });
            else
                return url.RouteUrl(route, new { httproute = "", controller = controller });
        }

        // --------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns the text contents of the specified file. If the file path is not found, an attempt will be made to map it
        /// to the current path using 'HttpContext.Current.Server.MapPath' (if exists).
        /// </summary>
        /// <param name="filepath">The path+filename of the file to retrieve.</param>
        /// <returns></returns>
        public static string GetTextFile(string filepath, bool ignoreError)
        {
            if (!File.Exists(filepath) && HttpContext.Current != null)
                filepath = HttpContext.Current.Server.MapPath(filepath);
            if (ignoreError && !File.Exists(filepath))
                return "";
            return File.ReadAllText(filepath);
        }

        // --------------------------------------------------------------------------------------------------------------------
    }

    // ########################################################################################################################

    /// <summary>
    /// Tags a controller to prevent browser caching.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class NoCacheAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            filterContext.HttpContext.Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            filterContext.HttpContext.Response.Cache.SetValidUntilExpires(false);
            filterContext.HttpContext.Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
            filterContext.HttpContext.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            filterContext.HttpContext.Response.Cache.SetNoStore();

            base.OnResultExecuting(filterContext);
        }
    }

    // ########################################################################################################################
}