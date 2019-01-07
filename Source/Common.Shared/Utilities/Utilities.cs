#if (NETSTANDARD1_5 || NETSTANDARD1_6 || NETCOREAPP1_0 || DNXCORE50 || NETCORE45  || NETCORE451 || NETCORE50)
#define DOTNETCORE
#endif
// (see more framework monikers here: https://docs.microsoft.com/en-us/nuget/schema/target-frameworks)
// Defines:
//    WEBDEV - Support web development.
//    ComponentModel - Support component model related helpers.
//    ManagementObjects - Support utilities that use ManagementObjects (such as Utilities.KillProcessByOwner()).

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using LinqExpr = System.Linq.Expressions;

#if SILVERLIGHT
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows.Browser;
using System.Windows;
using Common.CollectionsAndLists;
#else
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Common.CollectionsAndLists;
using System.Data;
#endif

#if DOTNETCORE // (DNXCORE50: https://channel9.msdn.com/Events/dotnetConf/2015/ASPNET-5-Deep-Dive; 0:36)
using System.Net;
#else
using System.Web;
#endif

namespace Common
{
    internal static class _ExtensionMethods
    {
#if !DOTNETCORE
        /// <summary>
        /// .Net Core requires a call to '{Type}.GetTypeInfo()' in order to get certain type details,
        /// such as enumerating methods and properties. This is here for .Net Full compatibility.
        /// </summary>
        public static Type GetTypeInfo(this Type type) { return type; }

        /// <summary>
        /// .Net Core requires a call to '{Type}.GetMethodInfo()' in order to get certain delegate details.
        /// This is here for .Net Full compatibility.
        /// </summary>
        public static MethodInfo GetMethodInfo(this Delegate del) { return del?.Method; }
#endif

        /// <summary>
        /// Creates a delegate from a delegate reference using a specified delegate type
        /// and optional instance (leave the instance null for static methods).
        /// </summary>
        public static Delegate CreateDelegate(this Delegate del, Type type, object instance = null)
        {
#if DOTNETCORE
            return del.GetMethodInfo().CreateDelegate(type, instance);
#else
            return del.CreateDelegate(type, instance);
#endif
        }

        /// <summary>
        /// Creates a delegate from a MethodInfo reference using a specified delegate type
        /// and optional instance (leave the instance null for static methods).
        /// </summary>
        public static Delegate CreateDelegate(this MethodInfo m, Type type, object instance = null)
        {
#if DOTNETCORE
            return m.CreateDelegate(type, instance);
#else
            return Delegate.CreateDelegate(type, instance, m);
#endif
        }
    }
}

namespace Common
{
    // =========================================================================================================================

    /// <summary>
    /// This class contains global (shared) utility methods used by most applications.
    /// </summary>
    public static partial class Utilities
    {
        // ---------------------------------------------------------------------------------------------------------------------

#if !SCRIPTSHARP
        /// <summary>
        /// Gets 1 token and returns it.
        /// The tokens are valid as long as 'true' is return.
        /// If false is returned, and the returned 'error' argument is NOT empty, then an error occurred (i.e. missing end limiter).
        /// </summary>
        public static bool GetToken(ref string subject, string startDelimiter, string endDelimiter, ref int index1, ref int index2, ref string token, out string error)
        {
            error = "";

            index1 = index2 + 1; // (move to next char)

            if (index1 + startDelimiter.Length > subject.Length) return false; // (enough space left for start delimiter?)

            bool found = false;

            while (index1 + startDelimiter.Length <= subject.Length)
            {
                if (subject.Substring(index1, startDelimiter.Length) == startDelimiter)
                { found = true; break; } // (found first delimiter)
                index1++;
            }

            if (!found) return false;

            index2 = index1;

            if (index2 + endDelimiter.Length > subject.Length)
            { error = "Missing end delimiter '" + endDelimiter + "'."; return false; } // (enough space left for start delimiter?)

            found = false;
            while (index2 + endDelimiter.Length <= subject.Length)
            {
                if (subject.Substring(index2, endDelimiter.Length) == endDelimiter)
                { found = true; break; } // (found first delimiter)
                index2++;
            }

            if (!found)
            { error = "Missing end delimiter '" + endDelimiter + "'."; return false; } // (enough space left for start delimiter?)

            token = subject.Substring(index1 + startDelimiter.Length, index2 - endDelimiter.Length - index1);

            return true;
        }
        public static void GetTokenBegin(out int index1, out int index2)
        { index1 = 0; index2 = -1; }
#endif

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Null to Default (used mostly with database related data): Returns the value passed, or a default 
        /// value if the value passed is null, or equal to DBNull.Value.
        /// </summary>
        /// <remarks>Name was inspired from the VBA function 'NZ()' (Null to Zero; see https://support.office.com/en-us/article/Nz-Function-8ef85549-cc9c-438b-860a-7fd9f4c69b6c)</remarks>
        /// <param name="val">Value to check.</param>
        /// <param name="default_val">New value if "val" is null or DBNull.Value.</param>
        /// <returns></returns>
        public static string ND(object val, string default_val)
        { return (val == DBNull.Value || val == null) ? (default_val) : ((val is string) ? (string)val : val.ToString()); }
        public static T ND<T>(object val, T default_val) where T : class
        { return (val == DBNull.Value || val == null) ? (default_val) : ((T)val); }
        public static Int16 ND(object val, Int16 default_val)
        { return (val == DBNull.Value || val == null) ? (default_val) : (ToInt16(val, default_val) ?? default_val); }
        public static Int32 ND(object val, Int32 default_val)
        { return (val == DBNull.Value || val == null) ? (default_val) : (ToInt32(val, default_val) ?? default_val); }
        public static Int64 ND(object val, Int64 default_val)
        { return (val == DBNull.Value || val == null) ? (default_val) : (ToInt64(val, default_val) ?? default_val); }
        public static float ND(object val, float default_val)
        { return (val == DBNull.Value || val == null) ? (default_val) : (ToSingle(val, default_val) ?? default_val); }
        public static decimal ND(object val, decimal default_val)
        { return (val == DBNull.Value || val == null) ? (default_val) : (ToDecimal(val, default_val) ?? default_val); }
        public static bool ND(object val, bool default_val)
        { return (val == DBNull.Value || val == null) ? (default_val) : (ToBoolean(val, default_val) ?? default_val); }
        public static double ND(object val, double default_val)
        { return (val == DBNull.Value || val == null) ? (default_val) : (ToDouble(val, default_val) ?? default_val); }
        public static DateTime ND(object val, DateTime default_val)
        { return (val == DBNull.Value || val == null) ? (default_val) : (ToDateTime(val, default_val) ?? default_val); }

        // ... more of the same, but using nullable parameters ...
        public static bool ND(object val, bool? default_val) { return ND(val, default_val ?? false); }
        public static double ND(object val, double? default_val) { return ND(val, default_val ?? 0d); }
        public static decimal ND(object val, decimal? default_val) { return ND(val, default_val ?? 0m); }
        public static float ND(object val, float? default_val) { return ND(val, default_val ?? 0f); }
        public static Int16 ND(object val, Int16? default_val) { return ND(val, default_val ?? 0); }
        public static Int32 ND(object val, Int32? default_val) { return ND(val, default_val ?? 0); }
        public static Int64 ND(object val, Int64? default_val) { return ND(val, default_val ?? 0); }
        public static DateTime ND(object val, DateTime? default_val) { return ND(val, default_val ?? DateTime.MinValue); }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns true if the specified SQL text contains only select statements, and nothing else.
        /// </summary>
        public static bool IsSQLSelectOnlyStatement(string sql)
        {
            char[] tempStr = sql.ToUpper().ToCharArray();
            // ... convert any non-letters to spaces ...
            for (int i = 0; i < tempStr.Length; i++)
                if (tempStr[i] < 'A' || tempStr[i] > 'Z')
                    tempStr[i] = ' ';
            sql = new string(tempStr);
            // ... split "words" by spaces ...
            string[] parts = sql.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            // ... check for invalid words ...
            if (parts.Length == 0 || parts[0] != "SELECT") return false;
            foreach (string word in parts)
                if (word == "INSERT" || word == "DELETE" || word == "UPDATE" || word == "CREATE" || word == "DROP" || word == "ALTER") return false;
            // ... statement is ok ...
            return true;
        }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Create a field name from the specified table name.
        /// <para>Note: It is assumed that the table name is already valid.</para>
        /// </summary>
        /// <param name="tableName">Table name to create a field from.</param>
        /// <param name="asIDField">If the field name will represent an ID value for the table (usually "_id").</param>
        /// <param name="fieldSuffix">Text to append to the final result. ("opt_" is recommended)</param>
        /// <param name="foreignKeySuffix">Text to append to the final result for foreign keys. (i.e. on end of "opt_" is recommended)</param>
        public static string CreateColumnNameFromTableName(string tableName, bool asIDField, string fieldSuffix = "", string foreignKeySuffix = null, string optionsTablePrefix = null)
        {
            foreignKeySuffix = foreignKeySuffix ?? "_id";
            optionsTablePrefix = optionsTablePrefix ?? "opt_";

            if (string.IsNullOrEmpty(tableName) && string.IsNullOrEmpty(fieldSuffix))
                return "";

            tableName = ND(tableName, "").Trim().ToLower();
            if (!string.IsNullOrEmpty(fieldSuffix) && !fieldSuffix.StartsWith("_"))
                fieldSuffix = "_" + fieldSuffix;

            int i = tableName.IndexOf(".dbo.");
            if (i >= 0) tableName = tableName.Substring(i + 5);

            if (asIDField)
            {
                string endTag = foreignKeySuffix + fieldSuffix;
                if (tableName.EndsWith(endTag)) return tableName; else return tableName + endTag;
            }

            if (tableName.StartsWith(optionsTablePrefix))
                tableName = tableName.Substring(4);

            if (tableName.EndsWith("ies"))
                tableName = tableName.Substring(0, tableName.Length - 3) + "y";
            else if (tableName.EndsWith("ss")) { /*ignore*/ }
            else if (tableName.EndsWith("s") && tableName.Length > 1)
            {
                i = tableName.Length - 2; // (second last char)
                if (tableName[i] != 'a' && tableName[i] != 'i' && tableName[i] != 'o' && tableName[i] != 'u') // (remove after non-vowel, except for 'e')
                    tableName = tableName.Substring(0, tableName.Length - 1);
            }

            return tableName + fieldSuffix;
        }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Converts '_' characters into spaces, separating the words and removing reserved prefixes and suffixes (i.e. remove
        /// "opt_" [option list table prefix], "_id" and "_id_" [foreign keys]), and capitalizes the first letter.
        /// </summary>
        public static string PropertizeColumnName(string name, string defaultOptionsTablePrefix, string defaultKeyName)
        {
            defaultOptionsTablePrefix = defaultOptionsTablePrefix ?? "opt";
            defaultKeyName = defaultKeyName ?? "id";

            if (name.IsNullOrWhiteSpace()) return name;
            //??if (string.IsNullOrWhiteSpace(name))
            //    throw new ArgumentNullException("PropertizeColumnName(): name - cannot be null or empty.");

            name = name.Replace('_', ' ');

            List<string> words = name.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            if (words[0].ToLower() == defaultOptionsTablePrefix)
                words.RemoveAt(0);

            int i = words.LastIndexOf(defaultKeyName);
            if (i > 0)
            {
                if (i == words.Count - 1)
                    words.RemoveAt(i); // (remove "id" from end)
                else // ("id" was not at the end [or beginning], so move words after to beginning)
                {
                    words.RemoveAt(i); // (remove "id" first [words all move down by 1])
                    var moveCount = words.Count - i;
                    for (; moveCount > 0; moveCount--)
                    {
                        words.Insert(0, words[words.Count - 1]); // (copy end word to beginning)
                        words.RemoveAt(words.Count - 1); // (remove the end word copied from)
                    }
                }
            }

            for (i = 0; i < words.Count; i++)
                words[i] = char.ToUpper(words[i][0]) + (words[i].Length > 1 ? words[i].Substring(1) : "");
            return string.Join(" ", words.ToArray());
        }
        public static string PropertizeColumnName(string name) { return PropertizeColumnName(name, null, null); }

        // ---------------------------------------------------------------------------------------------------------------------

        public static bool IsBoolean(Type t)
        {
            return (t == typeof(bool) || t == typeof(Boolean));
        }

        public static bool IsDateTime(Type t)
        {
            return (t == typeof(DateTime));
        }
        public static bool IsDateTime(string text)
        {
            DateTime dt; return DateTime.TryParse(text, out dt);
        }

        public static bool IsInt(Type t)
        {
            return (t == typeof(SByte) || t == typeof(int) || t == typeof(Int16) || t == typeof(Int32) || t == typeof(Int64));
        }
        public static bool IsInt64(string text)
        {
            Int64 i; return Int64.TryParse(text, out i);
        }
        public static bool IsInt(string text)
        {
            int i; return int.TryParse(text, out i);
        }

        public static bool IsUInt(Type t)
        {
            return (t == typeof(Byte) || t == typeof(uint) || t == typeof(UInt16) || t == typeof(UInt32) || t == typeof(UInt64));
        }

        public static bool IsFloat(Type t)
        {
            return (t == typeof(float) || t == typeof(double) || t == typeof(decimal));
        }

        public static bool IsNumeric(Type t)
        {
            return (IsInt(t) || IsUInt(t) || IsFloat(t));
        }
        public static bool IsNumeric(string text)
        {
            return Regex.IsMatch(text, @"^[+|-]?\d+\.?\d*$");
            //decimal d; return decimal.TryParse(text, out d);
        }
        public static bool IsSimpleNumeric(string text)
        {
            // http://derekslager.com/blog/posts/2007/09/a-better-dotnet-regular-expression-tester.ashx
            return Regex.IsMatch(text, @"^(?:\+|\-)?\d+\.?\d*$");
        }

        public static bool IsString(Type t)
        {
            return (t == typeof(string) || t == typeof(String));
        }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns true if the string is only letters.
        /// </summary>
        public static bool IsAlpha(string s)
        {
            if (s.Length == 0) return false;
            for (int i = 0; i < s.Length; i++)
                if ((s[i] < 'a' || s[i] > 'z') && (s[i] < 'A' || s[i] > 'Z'))
                    return false;
            return true;
        }

        /// <summary>
        /// Returns true if the string is only letters or numbers.
        /// </summary>
        public static bool IsAlphaNumeric(string s)
        {
            if (s.Length == 0) return false;
            for (int i = 0; i < s.Length; i++)
                if ((s[i] < 'a' || s[i] > 'z') && (s[i] < 'A' || s[i] > 'Z') && (s[i] < '0' || s[i] > '9'))
                    return false;
            return true;
        }

        /// <summary>
        /// Returns true if the string is only letters, numbers, or underscores, and the first character is not a number.
        /// This is useful to validate strings to be used as code-based identifiers, database column names, etc.
        /// </summary>
        public static bool IsIdent(string s)
        {
            if (s.Length == 0 || (s[0] >= '0' && s[0] <= '9')) return false;
            for (int i = 0; i < s.Length; i++)
                if ((s[i] < 'a' || s[i] > 'z') && (s[i] < 'A' || s[i] > 'Z') && (s[i] < '0' || s[i] > '9') && s[i] != '_')
                    return false;
            return true;
        }

        /// <summary>
        /// Returns true if the string is only numbers.
        /// </summary>
        public static bool IsDigits(string s)
        {
            if (s.Length == 0) return false;
            for (int i = 0; i < s.Length; i++)
                if (s[i] < '0' || s[i] > '9')
                    return false;
            return true;
        }

        // ---------------------------------------------------------------------------------------------------------------------

        public static bool IsDatePartOnly(string date)
        {
            if (date.IsNullOrWhiteSpace()) return false;
            var dt = Utilities.ToDateTime(date, null);
            if (dt == null) return false;
            if (dt.Value.Date == DateTime.MinValue)
                return false;
            date = date.ToLower();
            return !date.Contains(":") && !date.Contains(";") && !date.Contains("am") && !date.Contains("pm");
        }
        // ---------------------------------------------------------------------------------------------------------------------

        public static bool IsTimePartOnly(string time)
        {
            return Regex.IsMatch(time, @"(^\s*((([01]?\d)|(2[0-3])):((0?\d)|([0-5]\d))(:((0?\d)|([0-5]\d)))?)\s*$)|(^\s*((([1][0-2])|\d)(:((0?\d)|([0-5]\d)))?(:((0?\d)|([0-5]\d)))?)\s*[apAP][mM]\s*$)");
        }

        // ---------------------------------------------------------------------------------------------------------------------
        // (Test Here: http://derekslager.com/blog/posts/2007/09/a-better-dotnet-regular-expression-tester.ashx)

        public static bool IsValidURL(string url)
        {
            return url != null && Regex.IsMatch(url, @"^(?:http://|https://)\w{2,}.\w{2,}");
        }

        // ---------------------------------------------------------------------------------------------------------------------

        public static bool IsValidEmailAddress(string email)
        {
            if (email != null)
            {
                int nFirstAT = email.IndexOf('@');
                int nLastAT = email.LastIndexOf('@');
                if ((nFirstAT > 0) && (nLastAT == nFirstAT) && (nFirstAT < (email.Length - 1)))
                {
                    // address is ok regarding the single @ sign
                    return Regex.IsMatch(email, @"^(?:[A-Za-z0-9_\-]+\.)*(?:[A-Za-z0-9_\-]+)@(?:[A-Za-z0-9_\-]+)(?:\.[A-Za-z]+)+$");
                }
            }
            return false;
        }

        // ---------------------------------------------------------------------------------------------------------------------

        public static bool IsValidPhoneNumber(string number)
        {
            return number != null && Regex.IsMatch(number, @"^((\+?\d{1,3}(-|.| )?\(?\d\)?(-|.| )?\d{1,5})|(\(?\d{2,6}\)?))(-|.| )?(\d{3,4})(-|.| )?(\d{4})(( x| ext)( |\d)?\d{1,5}){0,1}$");
        }

        // --------------------------------------------------------------------------------------------------------------------- 

        public static bool IsValidPasword(string password, int minCharacters, int maxCharacters, bool requireOneUpperCase, bool requireDigit, string validSymbols)
        {
            string requiredCharacters = "";
            if (requireOneUpperCase) requiredCharacters += @"(?=.*[a-z])(?=.*[A-Z])"; else requiredCharacters += @"(?=.*[A-Za-z])";
            if (requireDigit) requiredCharacters += @"(?=.*\d)";
            if (validSymbols != null)
            {
                validSymbols = validSymbols.Replace(@"\", @"\\").Replace("-", @"\-");
                requiredCharacters += @"(?=.*[" + validSymbols + @"])";
            }
            return password != null && password.Length <= maxCharacters
                && Regex.IsMatch(password, @"^.*(?=.{" + minCharacters + @",})" + requiredCharacters + @".*$");
            // http://nilangshah.wordpress.com/2007/06/26/password-validation-via-regular-expression/
            /*
             * - Must be at least 6 characters.
             * - Must contain at least one letter, one digit, and one special character.
             * - Valid special characters are: `~!@#$%^&_-+=|\:;',./?
             */
        }

        public static bool IsValidPasword(string password, int minCharacters, int maxCharacters)
        {
            return IsValidPasword(password, minCharacters, maxCharacters, true, true, @"`~!@#$%^&_-+=|\:;',./?");
        }
        public static bool IsValidPasword(string password)
        {
            return IsValidPasword(password, 6, 20, true, true, @"`~!@#$%^&_-+=|\:;',./?");
        }

        // ---------------------------------------------------------------------------------------------------------------------

        public static bool? ToBoolean(object value, bool? defaultValue)
        {
            if (value is bool) return (bool)value;
            string txt = ND(value, "").ToLower(); // (convert to string and test for 'true' state equivalent)
            if (txt == "true" || txt == "yes" || txt == "y" || txt == "1" || txt == "ok" || txt == "pass" || txt == "on") return true;
            if (txt == "false" || txt == "no" || txt == "n" || txt == "0" || txt == "cancel" || txt == "fail" || txt == "off") return false;
            return defaultValue;
        }
        public static Int16? ToInt16(object value, Int16? defaultValue)
        {
            if (value is Int16) return (Int16)value;
            string txt = ND(value, ""); // (convert to string, and then convert to expected type)
            Int16 convertedValue;
            if (Int16.TryParse(txt, out convertedValue))
                return convertedValue;
            return defaultValue;
        }
        public static Int32? ToInt32(object value, Int32? defaultValue)
        {
            if (value is Int32) return (Int32)value;
            if (value is Int16) return (Int16)value;
            string txt = ND(value, ""); // (convert to string, and then convert to expected type)
            Int32 convertedValue;
            if (Int32.TryParse(txt, out convertedValue))
                return convertedValue;
            return defaultValue;
        }
        public static Int64? ToInt64(object value, Int64? defaultValue)
        {
            if (value is Int64) return (Int64)value;
            if (value is Int32) return (Int32)value;
            if (value is Int16) return (Int16)value;
            string txt = ND(value, ""); // (convert to string, and then convert to expected type)
            Int64 convertedValue;
            if (Int64.TryParse(txt, out convertedValue))
                return convertedValue;
            return defaultValue;
        }
        public static Single? ToSingle(object value, Single? defaultValue)
        {
            if (value is Single) return (Single)value;
            string txt = ND(value, ""); // (convert to string, and then convert to expected type)
            Single convertedValue;
            if (Single.TryParse(txt, out convertedValue))
                return convertedValue;
            return defaultValue;
        }
        public static Double? ToDouble(object value, Double? defaultValue)
        {
            if (value is Double) return (Double)value;
            if (value is Single) return (Single)value;
            if (value is Decimal) return (Double)(Decimal)value;
            string txt = ND(value, ""); // (convert to string, and then convert to expected type)
            Double convertedValue;
            if (Double.TryParse(txt, out convertedValue))
                return convertedValue;
            return defaultValue;
        }
        public static Decimal? ToDecimal(object value, Decimal? defaultValue)
        {
            if (value is Decimal) return (Decimal)value;
            if (value is Double) return (Decimal)(Double)value;
            if (value is Single) return (Decimal)(Single)value;
            string txt = ND(value, ""); // (convert to string, and then convert to expected type)
            Decimal convertedValue;
            if (Decimal.TryParse(txt, out convertedValue))
                return convertedValue;
            return defaultValue;
        }
        public static DateTime? ToDateTime(object value, DateTime? defaultValue)
        {
            if (value is DateTime) return (DateTime)value;
            string txt = ND(value, ""); // (convert to string, and then convert to expected type)
            DateTime convertedValue;
            if (DateTime.TryParse(txt, out convertedValue))
                return convertedValue;
            return defaultValue;
        }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Extracts and returns all digits in the given string.
        /// The resulting sting is a consecutive sequence of numerical characters.
        /// </summary>
        public static string ExtractDigits(string text)
        {
            if (text == null) return null;
            var digits = "";
            for (var i = 0; i < text.Length; i++)
                if (text[i] >= '0' && text[i] <= '9')
                    digits += text[i];
            return digits;
        }

        /// <summary>
        /// Strips all digits out of the given text and returns the result.
        /// </summary>
        public static string StripDigits(string text)
        {
            if (text == null) return null;
            var newText = "";
            for (var i = 0; i < text.Length; i++)
                if (text[i] < '0' || text[i] > '9')
                    newText += text[i];
            return newText;
        }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Formats a date field value using conversion #107 (MMM dd, yyyy) format.
        /// </summary>
        /// <param name="fieldName">Name of the date field to format.</param>
        /// <param name="newFieldName">New name to use for the result set.</param>
        /// <returns></returns>
        public static string SQLDate107(string fieldName, string newFieldName)
        {
            if (fieldName == "") throw new Exception("SQLDate107(): 'fieldName' cannot be empty.");
            if (newFieldName == "") newFieldName = fieldName;
            return "CONVERT(VARCHAR(50), " + BracketSQLFieldName(fieldName) + ", 107) AS " + BracketSQLFieldName(newFieldName);
        }
        public static string SQLDate107(string fieldName)
        {
            return SQLDate107(fieldName, fieldName);
        }

        /// <summary>
        /// Generates a SELECT CASE query part using the supplied parameters.
        /// </summary>
        /// <param name="value">The value or field name which is the value to translate.</param>
        /// <param name="trueValue">The value that determines the true value part. Any other values select the false value part.</param>
        /// <param name="valueIfTrue">The value to use when a true value is a match.</param>
        /// <param name="valueIfFalse">The value to use when a true value is not a match.</param>
        /// <param name="newFieldName">The field name to hold the returned result.</param>
        /// <returns></returns>
        public static string SQLIIF(string value, string trueValue, string valueIfTrue, string valueIfFalse, string newFieldName)
        {
            if (newFieldName.Trim() != "") newFieldName = " AS " + BracketSQLFieldName(newFieldName); else newFieldName = "";
            return "(SELECT CASE " + value + " WHEN " + trueValue + " THEN " + valueIfTrue + " ELSE " + valueIfFalse + " END)" + newFieldName;
        }
        public static string SQLIIF(string fieldName, string trueValue, string valueIfTrue, string valueIfFalse)
        {
            return SQLIIF(fieldName, trueValue, valueIfTrue, valueIfFalse, fieldName);
        }

        /// <summary>
        /// Determines if a bracket is needed for a given field based on the characters.  Only "a-z", "A-Z", "0-9", and "_" are accepted.
        /// <para>Note: Key words are not checked.</para>
        /// </summary>
        public static string BracketSQLFieldName(string table_field)
        {
            string[] name_parts = table_field.Split('.');
            string name = name_parts[name_parts.Length - 1];
            if (name.StartsWith("[") && name.EndsWith("]")) return table_field;
            bool useBrackets = false;
            for (int i = 0; i < name.Length; i++)
                if ((name[i] < 'a' || name[i] > 'z') && (name[i] < 'A' || name[i] > 'Z') && (name[i] < '0' || name[i] > '9') && name[i] != '_')
                { useBrackets = true; break; }
            name_parts[name_parts.Length - 1] = (useBrackets) ? ("[" + name + "]") : (name);
            return string.Join(".", name_parts);
        }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Strips all postal codes found in the given address and returns the new address.
        /// When used with 'StripDigits()', this is helpful in displaying addresses to the public in a more private and secure manor (in this case 'StripDigits()' must be called LAST).
        /// </summary>
        public static string RemovePostalCode(string address)
        {
            if (address == null) return address;
            return Regex.Replace(address, @"\w\d\w\s+\d\w\d", "");
        }

        // ---------------------------------------------------------------------------------------------------------------------

        public static List<char> URLEncodableCharacters = new List<char> { '$', '&', '+', '-', ',', '/', ':', ';', '=', '?', '@', ' ', '?', '<', '>', '#', '%', '{', '}', '|', '\\', '^', '~', '[', ']', '`' };

        public static string URLEncode(string url)
        {
            if (url == null) return "";
            string newUrl = "";
            char c;
            for (int i = 0; i < url.Length; i++)
            {
                c = url[i];
                if (URLEncodableCharacters.Contains(c))
                    newUrl += "%" + ((int)c).ToString("x");
                else
                    newUrl += c;
            }
            return newUrl;
        }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Converts the given byte size to the shortest size possible by appending a unit suffix ("byte[s]", "Kb", "Mb", or "Gb") based on the given byte size value.
        /// </summary>
        public static string GetShortByteSizeDescription(Int64 byteSize)
        {
            if (byteSize < 1024)
                return Strings.S(byteSize, "byte", "s", "");
            else if (byteSize < 1024 * 1024)
                return Strings.S(byteSize / 1024, "Kb", "", "0.##");
            else if (byteSize < 1024 * 1024 * 1024)
                return Strings.S(byteSize / (1024 * 1024), "Mb", "", "0.##");
            else
                return Strings.S(byteSize / (1024 * 1024 * 1024), "Gb", "", "0.##");
        }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary> Strips HTML tags only. The resulting text is not decoded. </summary>
        /// <param name="text"> The text. </param>
        /// <returns> A string. </returns>
        public static string StripHTMLTags(string text)
        {
            return Regex.Replace(text, @"(<[^>]+>)", "");
        }

#if WEBDEV
        /// <summary> Strips HTML tags only. The resulting text is not decoded. </summary>
        /// <param name="text"> The text. </param>
        /// <returns> A string. </returns>
        public static string DecodeHTMLText(string text)
        {
#if DOTNETCORE
            WebUtility.
#else
            HttpUtility.
#endif
                        HtmlDecode(Regex.Replace(text, @"(<[^>]+>)", "")); //Regex.Replace(, @"&[^;]+?;", " ");
        }
#endif

        // ---------------------------------------------------------------------------------------------------------------------
    }

    // =========================================================================================================================

    public static class MathExt
    {
        // ---------------------------------------------------------------------------------------------------------------------

        public static double ToRad(double valueInDegrees) { return valueInDegrees * Math.PI / 180; }

        // ---------------------------------------------------------------------------------------------------------------------

        public static double GetMapDistance(double longitude1, double latitude1, double longitude2, double latitude2)
        {
            // (see http://www.movable-type.co.uk/scripts/latlong.html and http://stackoverflow.com/questions/27928/how-do-i-calculate-distance-between-two-latitude-longitude-points)

            double R = 6371d; // earth radius in km

            double dLat = ToRad(latitude2 - latitude1);
            double dLon = ToRad(longitude2 - longitude1);

            latitude1 = ToRad(latitude1);
            latitude2 = ToRad(latitude2);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(latitude1) * Math.Cos(latitude2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            double d = R * c;

            return d;
        }

        // ---------------------------------------------------------------------------------------------------------------------

        public static double GetDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }

        // ---------------------------------------------------------------------------------------------------------------------
    }

    // =========================================================================================================================

    public static partial class ExtentionMethods
    {
        // ---------------------------------------------------------------------------------------------------------------------

        public static bool Contains(this IEnumerable items, object item)
        {
            foreach (object _item in items)
                if (_item == item) return true;
            return false;
        }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Adds an array of items to the given list.
        /// </summary>
        /// <typeparam name="T">Item type.</typeparam>
        /// <param name="listItems">The list to add the items to.</param>
        /// <param name="items">The items to add.</param>
        public static void AddArray<T>(this IList<T> listItems, T[] items)
        {
            foreach (T _item in items)
                listItems.Add(_item);
        }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Adds and returns the specified item.
        /// The normal list 'Add()' method doesn't return the added item, which prevents short-hand code.
        /// </summary>
        /// <typeparam name="T">Item type.</typeparam>
        /// <param name="listItems">The list to add the item to.</param>
        /// <param name="item">The item to add.</param>
        public static T AddItem<T>(this IList<T> listItems, T item)
        {
            listItems.Add(item);
            return item;
        }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns true if character is either a letter, digit, or underscore.
        /// </summary>
        public static bool IsIdent(this char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        // ---------------------------------------------------------------------------------------------------------------------

        public static Uri Append(this Uri uri, object path)
        {
            var _path = Utilities.ND(path, "").Replace('\\', '/');
            Uri result = null;
            uri = uri.AbsoluteUri.ND().ApendIfNotExists("/").ToURI();
            if (Uri.TryCreate(uri, Utilities.ND(_path, ""), out result)) // (note: this also works with '..\' scenarios)
                return result;
            return new Uri(Path.Combine(uri.AbsoluteUri, _path).Replace('\\', '/')); // (note: this doesn't work with '..\' scenarios, so is only provided as a fall-back)
        }

        // ---------------------------------------------------------------------------------------------------------------------

        public static Uri ToURI(this string uri, string defaultIfNull) { return new Uri((uri ?? defaultIfNull) ?? ""); }
        public static Uri ToURI(this string uri) { return ToURI(uri, ""); }

        public static Uri ToRelativeURI(this string uri, string defaultIfNull) { return new Uri((uri ?? defaultIfNull) ?? "", UriKind.Relative); }
        public static Uri ToRelativeURI(this string uri) { return ToRelativeURI(uri, ""); }

        public static Uri ToAbsoluteURI(this string uri, string defaultIfNull) { return new Uri((uri ?? defaultIfNull) ?? "", UriKind.Absolute); }
        public static Uri ToAbsoluteURI(this string uri) { return ToAbsoluteURI(uri, ""); }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Null to Default: Return the string value, or a default value if null.
        /// </summary>
        public static string ND(this object str, string defaultIfNull) { return Utilities.ND(str, defaultIfNull); }
        /// <summary>
        /// Null to Default: Return the string value, or default to empty string if null.
        /// </summary>
        public static string ND(this object str) { return ND(str, string.Empty); }

        /// <summary>
        /// Null to Default: Return the string value, or a default value if 'null', and trim the result before returning.
        /// If the result before trimming is 'null', then nothing is trimmed, and 'null' is returned.
        /// </summary>
        public static string NDTrim(this object str, string defaultIfNull) { var result = Utilities.ND(str, defaultIfNull); return result != null ? result.Trim() : null; }
        /// <summary>
        /// Null to Default: Return the string value, or an empty string if 'null', and trim the result before returning.
        /// If the result before trimming is 'null', then nothing is trimmed, and an empty string is returned.
        /// </summary>
        public static string NDTrim(this object str) { return NDTrim(str, string.Empty); }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Appends a given string if it doesn't already exist at the end of the target.
        /// </summary>
        public static string ApendIfNotExists(this string str, string strToAppend)
        { return !(str ?? "").EndsWith(strToAppend) ? (str ?? "") + strToAppend : str; }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns the given string converted to the specified type 'T', and returns 'defaultValue' instead if conversion fails.
        /// </summary>
        public static T As<T>(this string str, T defaultValue)
        {
            try { return Types.ChangeType<T>(str); }
            catch { return defaultValue; }
        }
        public static T As<T>(this string str)
        { return As<T>(str, default(T)); }

        // ---------------------------------------------------------------------------------------------------------------------

#if !SILVERLIGHT && !DOTNETCORE
        /// <summary>
        /// Creates a new data column for a table.
        /// </summary>
        /// <param name="table">The table to create a column for.</param>
        /// <param name="columnName">The name of the column to create.</param>
        /// <param name="caption">A display friendly name for the column (otherwise the column name is used).</param>
        /// <param name="dataType">A "System" type for this column.</param>
        /// <param name="isKey">If true, the column is marked unique and readonly, and is appended to the array of primary keys.</param>
        public static DataColumn CreateColumn(this DataTable table, string columnName, string caption, Type dataType, bool isKey)
        {
            if (table != null)
            {
                var column = table.Columns.Contains(columnName) ? table.Columns[columnName] : table.Columns.Add(columnName, dataType);
                if (caption != null) column.Caption = caption;
                if (isKey)
                {
                    column.Unique = true;
                    column.ReadOnly = true;
                    var keys = table.PrimaryKey;
                    Array.Resize(ref keys, keys.Length + 1);
                    keys[keys.Length - 1] = column;
                    table.PrimaryKey = keys;
                }
                return column;
            }
            return null;
        }
        public static DataColumn CreateColumn(this DataTable table, string columnName, Type dataType, bool isKey) { return table.CreateColumn(columnName, null, dataType, isKey); }
        public static DataColumn CreateColumn(this DataTable table, string columnName, string caption, Type dataType) { return table.CreateColumn(columnName, caption, dataType, false); }
        public static DataColumn CreateColumn(this DataTable table, string columnName, Type dataType) { return table.CreateColumn(columnName, null, dataType, false); }
#endif

        // ---------------------------------------------------------------------------------------------------------------------

        public static T Value<K, T>(this IDictionary<K, T> _this, K key) { T v; if (_this != null && _this.TryGetValue(key, out v)) return v; else return default(T); }

        public static T Value<K, T>(this IDictionary<K, T> _this, K key, T defaultValue) { T v; if (_this != null && _this.TryGetValue(key, out v)) return v; else return defaultValue; }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns the first element of a sequence, or a default value if the sequence contains no elements, or is null.
        /// </summary>
        public static T FirstOrDefault<T>(this IEnumerable<T> _this, T defaultValue) { return _this != null ? _this.FirstOrDefault() : default(T); }

        // ---------------------------------------------------------------------------------------------------------------------
    }

    // =========================================================================================================================

    public static class LinqExtensions
    {
        // ---------------------------------------------------------------------------------------------------------------------
        // Copyright (C) 2007 Troy Magennis. All Rights Reserved.
        // You are free to use this material, however you do so AT YOUR OWN risk. 
        // You are prohibited from removing this disclaimer or copyright notice from any derivative works.
        // Remember to visit http://www.hookedonlinq.com - The LINQ wiki community project.

        /// <summary>
        /// Executes an Update statement block on all elements in an IEnumerable&lt;T> sequence.
        /// </summary>
        /// <typeparam name="TSource">The source element type.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="update">The update statement to execute for each element.</param>
        /// <returns>The number of records affected.</returns>
        public static int Update<TSource>(this IEnumerable<TSource> source, Action<TSource> update)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (update == null) throw new ArgumentNullException("update");
            if (typeof(TSource).GetTypeInfo().IsValueType)
                throw new NotSupportedException("value type elements are not supported by update.");

            int count = 0;
            foreach (TSource element in source)
            {
                update(element);
                count++;
            }
            return count;
        }
        // ---------------------------------------------------------------------------------------------------------------------
    }

    // =========================================================================================================================

    public static partial class Strings
    {
        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns true if the given object is null, or its string conversion results in an empty/null string.
        /// </summary>
        public static bool IsNullOrEmpty(object value) { return (value == null || string.IsNullOrEmpty(value.ToString())); }

        /// <summary>
        /// Returns true if the string value is null or contains white space (contains all characters less than or equal Unicode value 32).
        /// </summary>
        public static bool IsNullOrWhiteSpace(this string str)
        {
#if V2 || V3 || V3_5 // (this method exists in .NET 4.0+ as a method of the string class)
            if (str == null || str.Length == 0) return true;
            for (var i = 0; i < str.Length; i++)
                if ((int)str[i] <= 32) return true;
            return false;
#else
            return string.IsNullOrWhiteSpace(str);
#endif
        }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Selects the first non-null/empty string found in the parameter order given, and returns a default value if
        /// both are null/empty.
        /// </summary>
        public static string SelectNonEmptyString(string str1, string str2, string defaultValue)
        {
            return str1.IsNullOrWhiteSpace() ? (str2.IsNullOrWhiteSpace() ? defaultValue : str2) : str1;
        }
        public static string SelectNonEmptyString(string str1, string str2) { return SelectNonEmptyString(str1, str2, null); }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Convert a list of objects into strings and return the concatenated result.
        /// </summary>
        public static string Join(string separator, object[] objects)
        {
            string s = "";
            foreach (object o in objects)
            {
                if (s.Length > 0) s += separator;
                if (o != null)
                    s += o.ToString();
            }
            return s;
        }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Join two strings arrays into one big array. The new array is returned.
        /// </summary>
        public static string[] Join(string[] sa1, string[] sa2)
        {
            string[] strings = new string[sa1.Length + sa2.Length];
            CopyTo(sa1, strings, 0);
            CopyTo(sa2, strings, sa1.Length);
            return strings;
        }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Copies a given source string array into another (destination), returning the destination array.
        /// </summary>
        /// <param name="src">The array to copy.</param>
        /// <param name="dest">The target of the copy.</param>
        /// <param name="destIndex">The array index into the destination in which copy starts.</param>
        public static string[] CopyTo(string[] src, string[] dest, int destIndex)
        {
            for (int i = 0; i < src.Length; i++)
                dest[destIndex + i] = src[i];
            return dest;
        }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Copies the given string and string array to a new array. The new array is returned.
        /// </summary>
        public static string[] Add(string s, string[] strings)
        {
            string[] newStringArray = new string[strings.Length + 1];
            CopyTo(strings, newStringArray, 0);
            newStringArray[strings.Length] = s;
            return newStringArray;
        }

        // ---------------------------------------------------------------------------------------------------------------------

        public static string FormatNumber(int n, string format)
        {
            return n.ToString(format);
        }

        public static string FormatNumber(double n, string format)
        {
            return n.ToString(format);
        }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns the value plus the singular or plural of a word based on an integer value.
        /// </summary>
        /// <param name="value">Number value.</param>
        /// <param name="word">Base word, singular.</param>
        /// <param name="suffix_if_plural">Suffix to use if "value" is not 1.</param>
        /// <param name="numberFormatting">The number format, if any (optional).</param>
        public static string S(int value, string word, string suffix_if_plural, string numberFormatting)
        {
            if (value != 1) return (numberFormatting != null ? FormatNumber(value, numberFormatting) : value.ToString()) + " " + word + suffix_if_plural;
            return value + " " + word;
        }
        public static string S(int value, string word, string suffix_if_plural = "s") { return S(value, word, suffix_if_plural, null); }

        /// <summary>
        /// Returns the value plus the singular or plural of a word based on a 'double' value.
        /// </summary>
        /// <param name="value">Number value.</param>
        /// <param name="word">Base word, singular.</param>
        /// <param name="suffix_if_plural">Suffix to use if "value" is not 1.</param>
        /// <param name="numberFormatting">The number format, if any (optional).</param>
        public static string S(double value, string word, string suffix_if_plural, string numberFormatting)
        {
            if (value != 1) return (numberFormatting != null ? FormatNumber(value, numberFormatting) : value.ToString()) + " " + word + suffix_if_plural;
            return value + " " + word;
        }
        public static string S(double value, string word, string suffix_if_plural = "s") { return S(value, word, suffix_if_plural, null); }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Appends the source string to the target string and returns the result.
        /// If 'target' and 'source' are both not empty, then the delimiter is inserted between them, and the resulting string returned.
        /// </summary>
        /// <param name="target">The string to append to.</param>
        /// <param name="source">The string to append.</param>
        /// <param name="delimiter">If specified, the delimiter is placed between the target and source if the target is NOT empty.</param>
        /// <returns>The new string.</returns>
        public static string Append(string target, string source, string delimiter)
        {
            if (target == null) target = "";
            if (source == null) source = "";
            if (delimiter == null) delimiter = "";

            var targetEndsWithDel = target.EndsWith(delimiter);
            var sourceStartsWithDel = source.StartsWith(delimiter);

            if (!targetEndsWithDel && !sourceStartsWithDel)
                target += delimiter + source;
            else if (targetEndsWithDel && sourceStartsWithDel)
                target += source.Substring(delimiter.Length); // (have to remove the delimiter from one of them)
            else
                target += source; // (one or the other already contains the delimiter)

            return target;
        }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns the number of occurrences of the given character in the given string.
        /// </summary>
        /// <param name="str">The string to look in.</param>
        /// <param name="chr">The character to count.</param>
        public static int CharCount(string str, char chr)
        {
            int count = 0;
            if (!string.IsNullOrEmpty(str))
                for (int i = 0; i < str.Length; i++)
                    if (str[i] == chr) count++;
            return count;
        }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Performs a textual comparison, where the letter casing is ignored, and returns 'true' if the specified strings are a match.
        /// </summary>
        /// <param name="strA">The first string to compare.</param>
        /// <param name="strB">The second string to compare.</param>
        public static bool TextEqual(string strA, string strB)
        {
            return string.Compare(strA, strB, StringComparison.CurrentCultureIgnoreCase) == 0;
        }

        // ---------------------------------------------------------------------------------------------------------------------

        public static int GetChecksum(string str)
        {
            int checksum = 0;
            for (int i = 0; i < str.Length; i++)
                checksum += str[i];
            return checksum;
        }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns the given string up to a maximum of 'maxlength' characters.
        /// If more than 'maxlength' characters exist, an ellipse character is appended to the returned substring.
        /// </summary>
        public static string Limit(string text, uint maxLength, bool includeElipseInMaxLength)
        {
            if (maxLength == 0) return "";
            if (text.Length <= maxLength) return text;
            return text.Substring(0, (int)maxLength - (includeElipseInMaxLength ? 1 : 0)) + "…";
        }
        public static string Limit(string text, uint maxLength) { return Limit(text, maxLength, false); }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Fixes 2 or more letter words, making them all lowercase except for the first letter.
        /// </summary>
        /// <param name="text">The string to change.</param>
        /// <returns>The result.</returns>
        public static string Propertize(string text)
        {
            string propertizedText = "";

            if (!string.IsNullOrEmpty(text))
            {
                bool wordStart = false;

                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];
                    if (TextReader.IsAlpha(c))
                    {
                        if (!wordStart)
                        {
                            propertizedText += c.ToString().ToUpper();
                            wordStart = true;
                        }
                        else
                            propertizedText += c.ToString().ToLower();
                    }
                    else
                    {
                        propertizedText += c;
                        wordStart = false;
                    }
                }
            }

            return propertizedText.Trim();
        }

        // --------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Converts a string collection into a delimited string.
        /// </summary>
        /// <param name="strings">A collection of strings.</param>
        /// <param name="delimiter">If specified (not empty) is used as a string separator.</param>
        /// <returns></returns>
        public static string ToString(this List<string> strings, string delimiter)
        {
            return string.Join(delimiter, strings);
        }

        // --------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Search an array of strings for a "whole" match.
        /// </summary>
        /// <param name="strings">List of strings to search.</param>
        /// <param name="text">String to look for.</param>
        /// <param name="caseSensitive">If 'false', the search is not case sensitive.</param>
        public static int IndexOf(this string[] strings, string text, bool caseSensitive)
        {
            if (caseSensitive)
            {
                for (int i = 0; i < strings.Length; i++)
                    if (strings[i] == text)
                        return i;
            }
            else
            {
                text = text.ToLower();
                for (int i = 0; i < strings.Length; i++)
                    if (string.Compare(strings[i], text, true) == 0)
                        return i;
            }
            return -1;
        }

        // ---------------------------------------------------------------------------------------------------------------------
    }

    // =========================================================================================================================

    public static partial class Arrays
    {
        /// <summary>
        /// Concatenate a list of arrays. Specify one array for each parameter.
        /// To concatenate one list of arrays, use Join().
        /// </summary>
        /// <typeparam name="T">Array type for each argument.</typeparam>
        /// <param name="args">A concatenated array made form the specified arrays.</param>
        /// <returns></returns>
        public static T[] Concat<T>(params T[][] args)
        {
            return Join<T>(args);
        }
        /// <summary>
        /// Concatenate a list of arrays.
        /// </summary>
        /// <typeparam name="T">Array type for each argument.</typeparam>
        /// <param name="arrays">A concatenated array made form the specified arrays.</param>
        /// <returns></returns>
        public static T[] Join<T>(T[][] arrays)
        {
            if (arrays.Length == 0) return null;
            Int32 newLength = 0, i;
            for (i = 0; i < arrays.Length; i++)
                newLength += arrays[i].Length;
            T[] newArray = new T[newLength];
            T[] array;
            Int32 writeIndex = 0;
            for (i = 0; i < arrays.Length; i++)
            {
                array = arrays[i];
                Array.Copy(array, 0, newArray, writeIndex, array.Length);
                writeIndex += array.Length;
            }
            return newArray;
        }
        public static string Join<T>(IEnumerable<T> list)
        {
            string s = "";
            foreach (T item in list)
                s += item != null ? item.ToString() : "";
            return s;
        }

        public static T[] Convert<T>(IList array)
        {
            if (array == null) return null;
            T[] convertedItems = new T[array.Count];
            for (int i = 0; i < array.Count; i++)
                convertedItems[i] = (T)System.Convert.ChangeType(array[i], typeof(T), CultureInfo.CurrentCulture);
            return convertedItems;
        }

        public static T[] ConvertWithDefaults<T>(IList array)
        {
            if (array == null) return null;
            T[] convertedItems = new T[array.Count];
            for (int i = 0; i < array.Count; i++)
            {
                try { convertedItems[i] = (T)System.Convert.ChangeType(array[i], typeof(T), CultureInfo.CurrentCulture); }
                catch { convertedItems[i] = default(T); }
            }
            return convertedItems;
        }

        /// <summary>
        /// Select an item from the end of the array.
        /// </summary>
        /// <typeparam name="T">Array type.</typeparam>
        /// <param name="items">The array.</param>
        /// <param name="index">0, or a negative value, that is the offset of the item to retrieve.</param>
        public static T FromEnd<T>(this T[] items, int index)
        {
            return items[items.Length - 1 + index];
        }
        /// <summary>
        /// Select an item from the end of the list.
        /// </summary>
        /// <typeparam name="T">List type.</typeparam>
        /// <param name="items">The list.</param>
        /// <param name="index">0, or a negative value, that is the offset of the item to retrieve.</param>
        public static T FromEnd<T>(this IList<T> items, int index)
        {
            return items[items.Count - 1 + index];
        }
    }

    // =========================================================================================================================

    public static partial class Objects
    {
        /// <summary>
        /// Get the property or field of the specified object.
        /// As the name suggests, a property is returned if found, otherwise a field is returned, but not both.
        /// </summary>
        public static void GetPropertyOrField(object obj, string fieldOrPropertyName, out PropertyInfo pi, out FieldInfo fi)
        {
            if (obj == null) { fi = null; pi = null; return; }
            pi = obj.GetType().GetProperty(fieldOrPropertyName);
            if (pi == null) pi = obj.GetType().GetProperty(fieldOrPropertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy); // (check static properties)
            fi = (pi != null) ? null : obj.GetType().GetField(fieldOrPropertyName);
            if (pi == null && fi == null) fi = obj.GetType().GetField(fieldOrPropertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy); // (check static fields)
        }
        /// <summary>
        /// For use with static properties or fields.
        /// </summary>
        public static void GetPropertyOrField(Type objType, string fieldOrPropertyName, out PropertyInfo pi, out FieldInfo fi)
        {
            if (objType == null) { fi = null; pi = null; return; }
            pi = objType.GetProperty(fieldOrPropertyName);
            if (pi == null) pi = objType.GetProperty(fieldOrPropertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy); // (check static properties)
            fi = (pi != null) ? null : objType.GetField(fieldOrPropertyName);
            if (pi == null && fi == null) fi = objType.GetField(fieldOrPropertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy); // (check static fields)
        }

        public static bool HasPropertyOrField(object obj, string fieldOrPropertyName)
        {
            PropertyInfo pInfo = null;
            FieldInfo fInfo = null;
            GetPropertyOrField(obj, fieldOrPropertyName, out pInfo, out fInfo);
            return pInfo != null || fInfo != null;
        }

        public static void GetFieldOrProperty(object obj, string fieldOrPropertyName, out FieldInfo fi, out PropertyInfo pi)
        {
            if (obj == null) { fi = null; pi = null; return; }
            fi = obj.GetType().GetField(fieldOrPropertyName);
            if (fi == null) fi = obj.GetType().GetField(fieldOrPropertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy); // (check static fields)
            pi = (fi != null) ? null : obj.GetType().GetProperty(fieldOrPropertyName);
            if (fi == null && pi == null) pi = obj.GetType().GetProperty(fieldOrPropertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy); // (check static properties)
        }
        public static void GetFieldOrProperty(Type objType, string fieldOrPropertyName, out FieldInfo fi, out PropertyInfo pi)
        {
            if (objType == null) { fi = null; pi = null; return; }
            fi = objType.GetField(fieldOrPropertyName);
            if (fi == null) fi = objType.GetField(fieldOrPropertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy); // (check static fields)
            pi = (fi != null) ? null : objType.GetProperty(fieldOrPropertyName);
            if (fi == null && pi == null) pi = objType.GetProperty(fieldOrPropertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy); // (check static properties)
        }

        public static bool HasFieldOrProperty(object obj, string fieldOrPropertyName)
        {
            FieldInfo fInfo = null;
            PropertyInfo pInfo = null;
            GetFieldOrProperty(obj, fieldOrPropertyName, out fInfo, out pInfo);
            return fInfo != null || pInfo != null;
        }

        public static bool GetPropertyOrFieldValue<T>(object subject, PropertyInfo pi, object[] piIndex, FieldInfo fi, out T value)
        {
            if (pi != null)
            { value = (T)pi.GetValue(subject, piIndex); return true; }
            else if (fi != null)
            { value = (T)fi.GetValue(subject); return true; }
            value = default(T);
            return false;
        }
        public static T GetPropertyOrFieldValue<T>(object subject, PropertyInfo pi, object[] piIndex, FieldInfo fi, T defaultValue)
        {
            T value;
            if (!GetPropertyOrFieldValue(subject, pi, piIndex, fi, out value))
                value = defaultValue;
            return value;
        }

        public static bool GetFieldOrPropertyValue<T>(object subject, FieldInfo fi, PropertyInfo pi, object[] piIndex, out T value)
        {
            if (fi != null)
            { value = (T)fi.GetValue(subject); return true; }
            else if (pi != null)
            { value = (T)pi.GetValue(subject, piIndex); return true; }
            value = default(T);
            return false;
        }
        public static T GetFieldOrPropertyValue<T>(object subject, FieldInfo fi, PropertyInfo pi, object[] piIndex, T defaultValue)
        {
            T value;
            if (!GetFieldOrPropertyValue(subject, fi, pi, piIndex, out value))
                value = defaultValue;
            return value;
        }

        public static bool GetPropertyOrFieldValue<T>(object subject, string name, out T value)
        {
            PropertyInfo pInfo = null;
            FieldInfo fInfo = null;
            GetPropertyOrField(subject, name, out pInfo, out fInfo);
            return GetPropertyOrFieldValue(subject, pInfo, null, fInfo, out value);
        }
        public static T GetPropertyOrFieldValue<T>(object subject, string name, T defaultValue)
        {
            T value;
            if (!GetPropertyOrFieldValue(subject, name, out value))
                value = defaultValue;
            return value;
        }

        public static bool GetFieldOrPropertyValue<T>(object subject, string name, out T value)
        {
            PropertyInfo pInfo = null;
            FieldInfo fInfo = null;
            GetFieldOrProperty(subject, name, out fInfo, out pInfo);
            return GetFieldOrPropertyValue(subject, fInfo, pInfo, null, out value);
        }
        public static T GetFieldOrPropertyValue<T>(object subject, string name, T defaultValue)
        {
            T value;
            if (!GetFieldOrPropertyValue(subject, name, out value))
                value = defaultValue;
            return value;
        }

        public static bool SetPropertyOrFieldValue<T>(object subject, PropertyInfo pi, object[] piIndex, FieldInfo fi, T value)
        {
            if (pi != null)
                pi.SetValue(subject, ((T)value is IConvertible ? Types.ChangeType((T)value, pi.PropertyType) : (T)value), piIndex);
            else if (fi != null)
                fi.SetValue(subject, ((T)value is IConvertible ? Types.ChangeType((T)value, fi.FieldType) : (T)value));
            else
                return false;
            return true;
        }

        public static bool SetFieldOrPropertyValue<T>(object subject, FieldInfo fi, PropertyInfo pi, object[] piIndex, T value)
        {
            if (fi != null)
                fi.SetValue(subject, ((T)value is IConvertible ? Types.ChangeType((T)value, fi.FieldType) : (T)value));
            else if (pi != null)
                pi.SetValue(subject, ((T)value is IConvertible ? Types.ChangeType((T)value, pi.PropertyType) : (T)value), piIndex);
            else
                return false;
            return true;
        }

        public static bool SetPropertyOrFieldValue<T>(object subject, string name, T value)
        {
            PropertyInfo pInfo = null;
            FieldInfo fInfo = null;
            Objects.GetPropertyOrField(subject, name, out pInfo, out fInfo);
            return SetPropertyOrFieldValue(subject, pInfo, null, fInfo, (T)value);
        }

        public static bool SetFieldOrPropertyValue<T>(object subject, string name, T value)
        {
            FieldInfo fInfo = null;
            PropertyInfo pInfo = null;
            Objects.GetFieldOrProperty(subject, name, out fInfo, out pInfo);
            return SetFieldOrPropertyValue(subject, fInfo, pInfo, null, (T)value);
        }

        public static T GetPropertyOrFieldValue<T>(object subject, string name)
        {
            object value;
            if (!GetPropertyOrFieldValue(subject, name, out value))
                value = default(T);
            if (value is T) return (T)value;
            return (T)Types.ChangeType(value, typeof(T), CultureInfo.CurrentCulture);
        }

        private static void _MergeData(object target, object source, string[] pathsToSkip, ref string path, ref bool modified, InteropCollection<object> referenceLevels)
        {
            if (target == null || source == null) return; // nothing to do

            // ... keep track of references to protect against cyclical cases ...

            if (referenceLevels.Contains(source))
                return;
            referenceLevels.Add(source);

            // ... if merge objects are of type IList or IEnumerable, then attempt to sync the lists,
            // otherwise abort ...

            try
            {
                if (source is IEnumerable && target is IList)
                {
                    Collections.SyncCollections((IList)target, (IEnumerable)source);
                    // ... continue on to check if there are any other fields/properties to consider ...
                }
            }
            catch
            {
                // ... error processing collection (error in user collection?), just ignore it ...
            }

            string _path = "";

            // ... merge changed properties first, in case they also modify public fields ...

            PropertyInfo[] properties = source.GetType().GetProperties();
            PropertyInfo targetPropInfo = null;
            foreach (PropertyInfo srcInfo in properties)
            {
                targetPropInfo = target.GetType().GetProperty(srcInfo.Name);
                if (targetPropInfo == null) continue; // (skip if target doesn't have this member)

                _path = (path.Length == 0 ? srcInfo.Name : path + "." + srcInfo.Name);
                if (pathsToSkip.Contains(_path))
                    continue; // (skip if the member name is one of the paths to skip)

                if (srcInfo.CanRead && targetPropInfo.CanRead && srcInfo.GetIndexParameters().Length == 0)
                {
                    object targetValue = targetPropInfo.GetValue(target, null);
                    object sourceValue = srcInfo.GetValue(source, null);

                    if (srcInfo.PropertyType.GetTypeInfo().IsClass && srcInfo.PropertyType != typeof(string)) // (although string is a class, it is a primitive type)
                    {
                        // ... attempt to create object on target if missing ...
                        if (sourceValue != null && targetValue == null)
                        {
                            try
                            {
                                targetValue = sourceValue.GetType().GetTypeInfo().Assembly.CreateInstance(sourceValue.GetType().FullName);
                                targetPropInfo.SetValue(target, targetValue, null);
                            }
                            catch { /* ... unable to instantiate the type, just skip it ... */ }
                        }

                        _MergeData(targetValue, sourceValue, pathsToSkip, ref _path, ref modified, referenceLevels);
                    }
                    else
                    {
                        try
                        {
                            if (targetPropInfo.CanWrite && (targetValue == null && sourceValue != null || targetValue != null && !targetValue.Equals(sourceValue)))
                            {
                                targetPropInfo.SetValue(target, sourceValue, null);
                                modified = true;
                            }
                        }
                        catch
                        {
                            // ... error processing property (possible custom-code errors), just skip it ...
                        }
                    }
                }
            }

            // ... merge fields that are different ...

            FieldInfo[] fields = source.GetType().GetFields();
            FieldInfo targetFieldInfo = null;
            foreach (FieldInfo info in fields)
            {
                targetFieldInfo = target.GetType().GetField(info.Name);
                if (targetFieldInfo == null) continue;

                _path = (path.Length == 0 ? info.Name : path + "." + info.Name);
                if (pathsToSkip.Contains(_path))
                    continue;

                if (info.FieldType.GetTypeInfo().IsClass && targetFieldInfo.FieldType.GetTypeInfo().IsClass)
                    _MergeData(targetFieldInfo.GetValue(target), info.GetValue(source), pathsToSkip, ref _path, ref modified, referenceLevels);
                else
                    if (!targetFieldInfo.IsInitOnly && !targetFieldInfo.IsLiteral &&
                        targetFieldInfo.GetValue(target) != info.GetValue(source))
                {
                    targetFieldInfo.SetValue(target, info.GetValue(source));
                    modified = true;
                }
            }

            // ... remove the reference added in this call (should be the last one) ...

            referenceLevels.RemoveAt(referenceLevels.Count - 1);
        }

        /// <summary>
        /// Merges the source values (all public fields and properties) into the target.
        /// Encapsulated class objects are also traversed.
        /// Any source fields and properties not found in the target are skipped.
        /// Returns true if any values were copied.
        /// </summary>
        /// <param name="target">Object to update.</param>
        /// <param name="source">Source of changed values.</param>
        /// <param name="pathsToSkip">Reference paths to skip (e.g. "Parent", "Child.SubChild.Prop", etc.). "Parent" is assumed if null is passed.</param>
        public static bool MergeData(object target, object source, string[] pathsToSkip)
        {
            if (target == null)
                throw new Exception("MergeData(): 'target' cannot be null.");
            if (source == null)
                throw new Exception("MergeData(): 'source' cannot be null.");
            if (pathsToSkip == null)
                pathsToSkip = new string[] { "Parent", "parent", "_parent" };

            string path = "";
            bool modified = false;

            InteropCollection<object> _referenceLevels = new InteropCollection<object>(100, 100); // (should be no more than 100 levels, but this list can grow just in case)

            _MergeData(target, source, pathsToSkip, ref path, ref modified, _referenceLevels);

            return modified;
        }
        public static bool MergeData(object target, object source)
        { return MergeData(target, source, null); }
    }

    // =========================================================================================================================

    public static partial class Collections
    {
        public enum MergeAction { Skip, Replace, Merge, Remove };

        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> list)
        {
            ObservableCollection<T> c = new ObservableCollection<T>();
            foreach (T item in list)
                c.Add(item);
            return c;
        }

        /// <summary>
        /// Synchronizes two IEnumerable lists based on a unique property value.
        /// The process uses reflection to get the unique values as strings, and then compares them.
        /// </summary>
        /// <param name="target">The list to update.</param>
        /// <param name="source">The list to synchronize with.</param>
        /// <param name="uniquePropertyName">A primary key name for the record objects associated with the specified list/collection type.</param>
        /// <param name="removeMissingTargetItems">Set true to remove any target items that are not found in the source.</param>
        /// <param name="addMissingSourceItems">Set true to add any source items now found in the target.</param>
        /// <param name="mergeAction">A primary key name for the record objects associated with the specified list/collection type.</param>
        /// <returns></returns>
        public static void SyncCollections<T>(IList<T> target, IEnumerable<T> source, string uniqueFieldOrPropertyName, bool removeMissingTargetItems, bool addMissingSourceItems, MergeAction mergeAction)
            where T : class
        {
            object targetUID, sourceUID;
            bool tarFieldOrPropFound, srcFieldOrPropFound;
            T targetItem, sourceItem;

            // ... remove the missing records first to allow reused of existing item locations ...

            if (removeMissingTargetItems || mergeAction != MergeAction.Skip) // ("short circuit" attempt, since this effectively does nothing)
                for (int i = target.Count() - 1; i >= 0; i--)
                {
                    targetItem = target[i];
                    if (targetItem == null) continue;
                    tarFieldOrPropFound = Objects.GetFieldOrPropertyValue<object>(targetItem, uniqueFieldOrPropertyName, out targetUID);
                    if (!tarFieldOrPropFound) throw new Exception("Field or property name '" + uniqueFieldOrPropertyName + "' was not found on object '" + typeof(T).Name + "'.");

                    sourceItem = GetUniqueCollectionItem(source, uniqueFieldOrPropertyName, targetUID);

                    if (sourceItem != null)
                    {
                        // (target item exists in source collection)
                        if (mergeAction == MergeAction.Merge)
                            Objects.MergeData(targetItem, sourceItem);
                        else if (mergeAction == MergeAction.Replace)
                            target[i] = sourceItem;
                        continue; // (MergeAction.Remove handled in the second pass below)
                    }

                    // ... not found, or merge action is 'Replace', remove it ...
                    if (removeMissingTargetItems)
                        target.RemoveAt(i);
                }

            // ... add any new items in the source list to the target list (in the correct order) ...
            // (an insert index is used in attempts to maintain the same order - assuming the target list came from the same order)

            if (addMissingSourceItems || mergeAction == MergeAction.Remove)
            {
                int itemIndex = -1, insertIndex = 0;
                for (int i = 0; i < source.Count(); i++)
                {
                    sourceItem = source.ElementAt(i);
                    if (sourceItem == null) continue;
                    srcFieldOrPropFound = Objects.GetFieldOrPropertyValue<object>(sourceItem, uniqueFieldOrPropertyName, out sourceUID);
                    if (!srcFieldOrPropFound) throw new Exception("Field or property name '" + uniqueFieldOrPropertyName + "' was not found on object '" + typeof(T).Name + "'.");

                    targetItem = GetUniqueCollectionItem(target, uniqueFieldOrPropertyName, sourceUID, out itemIndex);

                    if (targetItem != null) // (source exists in the target)
                    {
                        if (mergeAction == MergeAction.Remove)
                            target.RemoveAt(itemIndex); // (no merge, just delete item)
                        if (insertIndex > itemIndex) itemIndex--; // (move insert location back also)
                        insertIndex = itemIndex + 1; // (insert next new source item after target item skipped)
                        continue;
                    }

                    // ... not found, add it ...
                    if (addMissingSourceItems)
                        target.Insert(insertIndex++, sourceItem);

                    //if (addMissingSourceItems)
                    //{
                    //    if (insertIndex >= target.Count)
                    //        target.Add(sourceItem);
                    //    else
                    //        target.Insert(insertIndex, sourceItem);
                    //    insertIndex++;
                    //}
                }
            }
        }
        public static void SyncCollections<T>(IList<T> target, IEnumerable<T> source, string uniqueFieldOrPropertyName, bool removeMissingTargetItems, bool addMissingSourceItems) where T : class
        { SyncCollections(target, source, uniqueFieldOrPropertyName, removeMissingTargetItems, addMissingSourceItems, MergeAction.Merge); }
        public static void SyncCollections<T>(IList<T> target, IEnumerable<T> source, string uniqueFieldOrPropertyName, bool removeMissingTargetItems) where T : class
        { SyncCollections(target, source, uniqueFieldOrPropertyName, removeMissingTargetItems, true); }
        public static void SyncCollections<T>(IList<T> target, IEnumerable<T> source, string uniqueFieldOrPropertyName) where T : class
        { SyncCollections(target, source, uniqueFieldOrPropertyName, true); }

        /// <summary>
        /// Sync an IEnumerable source with an IList target.
        /// Existing items will be left alone, while non-existing items will be inserted/added accordingly.
        /// </summary>
        public static void SyncCollections(IList target, IEnumerable source)
        {
            // ... remove items first, just to free up internal item positions in the list ...
            object targetItem;
            for (int i = target.Count - 1; i >= 0; i--)
            {
                targetItem = target[i];
                if (!source.Contains(targetItem))
                    target.RemoveAt(i);
            }
            // ... insert missing items ...
            int targetItemIndex, insertIndex = 0;
            foreach (object sourceItem in source)
            {
                targetItemIndex = target.IndexOf(sourceItem);
                if (targetItemIndex == -1)
                {
                    target.Insert(insertIndex, sourceItem);
                    insertIndex++;
                }
                else
                {
                    insertIndex = targetItemIndex + 1; // (next item will insert by default before this position)
                }
            }
        }

        /// <summary>
        /// Uses reflection to search for an item who's field value, named 'uniquePropertyName', matches the value given in 'uniqueID'.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="uniquePropertyName"></param>
        /// <returns></returns>
        public static T GetUniqueCollectionItem<T>(IEnumerable<T> collection, string uniqueFieldOrPropertyName, object uniqueID, out int index)
            where T : class
        {
            object uid;
            for (int i = 0; i < collection.Count(); i++)
            {
                T item = collection.ElementAt(i);
                if (Objects.GetFieldOrPropertyValue<object>(item, uniqueFieldOrPropertyName, out uid)
                    && uid != null && uid.Equals(uniqueID))
                {
                    index = i;
                    return item;
                }
            }

            index = -1;
            return null;
        }
        public static T GetUniqueCollectionItem<T>(IEnumerable<T> collection, string uniqueFieldOrPropertyName, object uniqueID) where T : class
        { int i; return GetUniqueCollectionItem(collection, uniqueFieldOrPropertyName, uniqueID, out i); }

        public static T GetSimilarItem<T>(IEnumerable<T> collection, object itemToLookFor, string uniquePropertyOrFieldName) where T : class
        {
            object valueToLookFor, uid;
            if (Objects.GetPropertyOrFieldValue<object>(itemToLookFor, uniquePropertyOrFieldName, out valueToLookFor)
                && valueToLookFor != null)
            {
                for (int i = 0; i < collection.Count(); i++)
                {
                    T item = collection.ElementAt(i);
                    if (Objects.GetPropertyOrFieldValue<object>(item, uniquePropertyOrFieldName, out uid)
                        && uid != null && valueToLookFor.Equals(uid))
                        return item;
                }
            }
            return null;
        }
    }

    // =========================================================================================================================

    public static partial class Exceptions
    {
        /// <summary>
        /// A simple utility method which formats and returns an exception error object.
        /// The stack trace is also included. The inner exceptions are also recursed and added.
        /// </summary>
        /// <param name="ex">The exception object with error message to format and return.</param>
        /// <param name="includeStackTrace">Set false to not include the stack trace.</param>
        /// <param name="includeLabels">Set false to exclude the "Message: " and "Stack: " sections labels.</param>
        public static string GetFullErrorMessage(this Exception ex, bool includeStackTrace, bool includeLabels) { return _GetFullErrorMessage(ex, "", includeStackTrace, includeLabels); }
        public static string GetFullErrorMessage(this Exception ex) { return _GetFullErrorMessage(ex, "", true, true); }

        static string _GetFullErrorMessage(Exception ex, string margin, bool includeStackTrace, bool includeLabels)
        {
            bool topMargin = string.IsNullOrEmpty(margin);
            var arrow = topMargin ? "" : "» ";
            string msg = margin + arrow + (includeLabels ? "Message: " : "") + ex.Message + "\r\n\r\n" + margin;
            if (includeStackTrace && !string.IsNullOrEmpty(ex.StackTrace)) msg += arrow + (includeLabels ? "Stack Trace: " : "") + ex.StackTrace;
            if (ex.InnerException != null)
                msg += "\r\n\r\n***Inner Exception ***\r\n" + _GetFullErrorMessage(ex.InnerException, margin + "==", includeStackTrace, includeLabels);
            return msg;
        }

        /// <summary>
        /// Returns the current exception and all inner exceptions.
        /// </summary>
        public static IEnumerable<Exception> AllExceptions(this Exception e)
        {
            while (e != null) { yield return e; e = e.InnerException; }
        }

        /// <summary>
        /// Returns the current exception including all inner exceptions that match the specified exception type.
        /// </summary>
        public static IEnumerable<Exception> ExceptionOf<TException>(this Exception e) where TException : Exception
        {
            while (e != null) { if (typeof(TException).IsAssignableFrom(e.GetType())) yield return e; e = e.InnerException; }
        }
    }

    // =========================================================================================================================

    /// <summary>
    /// Provides utility methods for types.
    /// This class was originally created to support the 'ThreadController" class's "Dispatch()" methods.
    /// </summary>
    public static partial class Types
    {
        // ---------------------------------------------------------------------------------------------------------------------

        public static object ChangeType(object value, Type targetType, IFormatProvider provider)
        {
            if (targetType == null)
                throw new ArgumentNullException("targetType");

            var targetTypeInfo = targetType.GetTypeInfo(); // (this is a fix to support .NET Core)

            if (targetTypeInfo.IsEnum)
            {
                return Enum.Parse(targetType, Convert.ToString(value), true);
            }
            else
            {
                Type underlyingType;

                if (provider == null)
                    provider = CultureInfo.CurrentCulture;

                if ((underlyingType = Nullable.GetUnderlyingType(targetType)) != null)
                {
                    // ... this is a nullable type target, so need to convert to underlying type first, then to a nullable type ...
                    if (value is string && string.IsNullOrEmpty((string)value))
                        return null; // (for nullable target types, convert empty strings to null)
                    else
                        value = ChangeType(value, underlyingType, provider); // (recursive call to convert to the underlying nullable type)
                    return Activator.CreateInstance(targetType, value);
                }
                else if (targetTypeInfo.IsValueType && (value == null || value is string && string.IsNullOrEmpty((string)value)))
                {
                    // (cannot set values to 'null')
                    if (targetType == typeof(bool))
                        value = false;
                    else if (targetType == typeof(DateTime))
                        value = DateTime.MinValue;
                    else
                        value = 0;
                }
                else if (value == null) return null;
                else if (value.GetType() == targetType) return value; // (same type as target!)
                else if (targetType == typeof(Boolean))
                {
                    if (value == null || value is string && ((string)value).IsNullOrWhiteSpace()) // (null or empty strings will be treated as 'false', but explicit text will try to be converted)
                        value = false;
                    else if ((value = Utilities.ToBoolean(value, null)) == null)
                        throw new InvalidCastException(string.Format("Types.ChangeType(): Cannot convert string value \"{0}\" to a boolean.", value));
                }
            }

            try
            {
                return Convert.ChangeType(value, targetType, provider);
            }
            catch (Exception ex)
            {
                throw new InvalidCastException(string.Format("Types.ChangeType(): Cannot convert value \"{0}\" (type: '{1}') to type '{2}'. If you are developing the source type yourself, implement the 'IConvertible' interface.", Utilities.ND(value, ""), value.GetType().FullName, targetTypeInfo.FullName), ex);
            }
        }
        public static object ChangeType(object value, Type targetType) { return ChangeType(value, targetType, null); }

        public static TargetType ChangeType<TargetType>(object value, IFormatProvider provider)
        { return (TargetType)ChangeType(value, typeof(TargetType), provider); }
        public static TargetType ChangeType<TargetType>(object value) { return ChangeType<TargetType>(value, null); }

        /// <summary>
        /// A structure which represents a 'typed' null value.
        /// This is required for cases where a type is just 'object', in which 'null' may be passed,
        /// but the type still needs to be known. An example usage is with methods that accept variable
        /// number of parameters, but need to know the argument type, even if null.
        /// </summary>
        public struct Null
        {
            public readonly Type Type;
            public Null(Type type)
            { if (type == null) throw new ArgumentNullException("type"); Type = type; }
        }

        /// <summary>
        /// If not null, returns either the argument, otherwise returns argument's 'null' type.
        /// This is needed in cases where an argument is null, but the argument type is needed.
        /// <para>
        /// Example: MyMethod(typeof(DateTime).Arg(value)); - If 'value' is null, then the type is passed instead as 'Types.Null'
        /// </para>
        /// </summary>
        /// <param name="type">Argument type.</param>
        /// <param name="value">Argument value.</param>
        /// <returns>Argument value, or the type if null.</returns>
        public static object Arg(this Type type, object value)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (value != null)
            {
                if (!type.IsAssignableFrom(value.GetType()))
                    throw new InvalidOperationException("Types.Arg(): Type of 'value' cannot be cast to '" + type.FullName + "'.");
                return value;
            }
            else return new Null(type);
        }

        /// <summary>
        /// Attempts to get the types of the values passed.
        /// If a value is 'null', then the call will fail, and 'null' will be returned.
        /// Note: This method recognizes Types.Null values.
        /// </summary>
        /// <param name="args">Argument values to get types for.</param>
        public static Type[] GetTypes(params object[] args)
        {
            if (args == null || args.Length == 0) return null;
            foreach (object arg in args)
                if (arg == null) return null;
            Type[] argTypes = new Type[args.Length];
            for (int i = 0; i < args.Length; i++)
                argTypes[i] = (args[i] is Null) ? ((Null)args[i]).Type : args[i].GetType();
            return argTypes;
        }

        /// <summary>
        /// Converts any Types.Null objects into simple 'null' references.
        /// This is helpful after using Types.GetTypes() on the same items - once the types are
        /// retrieved, this method helps to convert Types.Null items back to 'null'.
        /// </summary>
        public static object[] ConvertNullsToNullReferences(object[] items)
        {
            if (items != null)
                for (int i = 0; i < items.Length; i++)
                    if (items[i] is Null) items[i] = null;
            return items;
        }
    }

    // =========================================================================================================================

    namespace Delegates
    {
        public delegate object LateBoundMethod(object target, object[] arguments);

        public static class DelegateFactory
        {
            static Dictionary<string, Dictionary<string, LateBoundMethod>> _Delegates = new Dictionary<string, Dictionary<string, LateBoundMethod>>();

            // Notice: Thanks to Nate Kohari at kohari.org for the 'LateBoundMethod' coding examples.

            /// <summary>
            /// Generates a delegate method call wrapper using expressions.
            /// The resulting delegate is extremely fast, and is not limited by number of parameters.
            /// There is one caveat however, as it takes 3-4 ms to compile the required delegate before
            /// first use, after which the delegate is then cached for future requests.
            /// <para>
            /// Purpose: There is no simple way to create a delegate for unknown method signature types at run-time.
            /// This method is one way to overcome this limitation.
            /// </para>
            /// </summary>
            public static LateBoundMethod ToFastDelegateProxy(this MethodInfo method)
            {
                // ... try to pull from cache ...
                string hostType = method.DeclaringType.FullName;
                string methodSig = method.Name + Arrays.Join(method.GetParameters().Select(p => p.Name));
                if (_Delegates.ContainsKey(hostType))
                {
                    var hd = _Delegates[hostType];
                    if (hd.ContainsKey(methodSig))
                        return hd[methodSig];
                }
                else _Delegates[hostType] = new Dictionary<string, LateBoundMethod>();

                // ... create new delegate ...

                ParameterExpression instanceParameter = LinqExpr.Expression.Parameter(typeof(object), "target");
                ParameterExpression argumentsParameter = LinqExpr.Expression.Parameter(typeof(object[]), "arguments");

                MethodCallExpression call = LinqExpr.Expression.Call(
                    System.Linq.Expressions.Expression.Convert(instanceParameter, method.DeclaringType),
                    method,
                    _CreateParameterExpressions(method, argumentsParameter)
                    );

                Expression<LateBoundMethod> lambda = LinqExpr.Expression.Lambda<LateBoundMethod>(
                    LinqExpr.Expression.Convert(call, typeof(object)),
                    instanceParameter,
                    argumentsParameter
                    );

                // ... cache and return delegate ...

                return (_Delegates[hostType][methodSig] = lambda.Compile());
            }

            /// <summary>
            /// Using the supplied MethodInfo, creates the required expressions to copy values from the supplied array (argument) expression.
            /// </summary>
            private static LinqExpr.Expression[] _CreateParameterExpressions(MethodInfo method, ParameterExpression argumentsParameter)
            {
                return method.GetParameters().Select((parameter, index) =>
                  LinqExpr.Expression.Convert(
                    LinqExpr.Expression.ArrayIndex(argumentsParameter, LinqExpr.Expression.Constant(index)), parameter.ParameterType)).ToArray();
            }

            /// <summary>
            /// Builds a Delegate instance from the MethodInfo object and a target instance to invoke against.
            /// The Action/Func types are used to accomplish this, so the maximum arguments allowed are only 5.
            /// <para>
            /// Purpose: There is no simple way to create a delegate for unknown method signature types at run-time.
            /// This method is one way to overcome this limitation.
            /// </para>
            /// </summary>
            public static Delegate ToSimpleDelegate(this MethodInfo mi, object instance)
            {
                if (mi == null) throw new ArgumentNullException("mi");
                if (!mi.IsStatic && instance == null) throw new ArgumentNullException("instance (required for non-static types)");

                Type delegateType;

                var typeArgs = mi.GetParameters()
                    .Select(p => p.ParameterType)
                    .ToList();

                // builds a delegate type (note: max 5 parameters allowed in total)
                if (mi.ReturnType == typeof(void))
                    delegateType = System.Linq.Expressions.Expression.GetActionType(typeArgs.ToArray());
                else
                {
                    typeArgs.Add(mi.ReturnType);
                    delegateType = System.Linq.Expressions.Expression.GetFuncType(typeArgs.ToArray());
                }

                // creates a binded delegate if target is supplied
                var result = (instance == null)
                    ? mi.CreateDelegate(delegateType)
                    : mi.CreateDelegate(delegateType, instance);

                return result;
            }
        }
    }

    // =========================================================================================================================

#if SILVERLIGHT
    /// <summary>
    /// A simple stop watch class.
    /// </summary>
    public class Stopwatch
    {
        public static readonly bool IsHighResolution = false;
        public static readonly long Frequency = TimeSpan.TicksPerSecond;

        public TimeSpan Elapsed
        {
            get
            {
                if (!StartUtc.HasValue)
                {
                    return TimeSpan.Zero;
                }
                if (!EndUtc.HasValue)
                {
                    return (DateTime.UtcNow - StartUtc.Value);
                }
                return (EndUtc.Value - StartUtc.Value);
            }
        }

        public long ElapsedMilliseconds
        {
            get
            {
                return ElapsedTicks / TimeSpan.TicksPerMillisecond;
            }
        }
        public long ElapsedTicks { get { return Elapsed.Ticks; } }
        public bool IsRunning { get; private set; }
        private DateTime? StartUtc { get; set; }
        private DateTime? EndUtc { get; set; }

        public static long GetTimestamp()
        {
            return DateTime.UtcNow.Ticks;
        }

        public void Reset()
        {
            Stop();
            EndUtc = null;
            StartUtc = null;
        }

        public void Start()
        {
            if (IsRunning)
            {
                return;
            }
            if ((StartUtc.HasValue) &&
                (EndUtc.HasValue))
            {
                // Resume the timer from its previous state
                StartUtc = StartUtc.Value +
                    (DateTime.UtcNow - EndUtc.Value);
            }
            else
            {
                // Start a new time-interval from scratch
                StartUtc = DateTime.UtcNow;
            }
            IsRunning = true;
            EndUtc = null;
        }

        public void Stop()
        {
            if (IsRunning)
            {
                IsRunning = false;
                EndUtc = DateTime.UtcNow;
            }
        }

        public static Stopwatch StartNew()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            return stopwatch;
        }
    }
#endif

    // =========================================================================================================================
    // (Some implementation ideas below were from: http://diditwith.net/2007/03/23/SolvingTheProblemWithEventsWeakEventHandlers.aspx)

    /// <summary>
    /// Provides static utility methods for working with events.
    /// </summary>
    public static class EventHandling
    {
        // ---------------------------------------------------------------------------------------------------------------------
        // Weak references for system events.

        public delegate void UnregisterCallback<E>(EventHandler<E> eventHandler)
            where E : EventArgs;

        private interface IWeakEventHandler<E>
            where E : EventArgs
        {
            EventHandler<E> Handler { get; }
        }

        private class WeakEventHandler<T, E> : IWeakEventHandler<E>
            where T : class
            where E : EventArgs
        {
            private delegate void OpenEventHandler(T @this, object sender, E e);
            private WeakReference _TargetRef;
            private OpenEventHandler _OpenHandler;
            private EventHandler<E> _Handler;
            private UnregisterCallback<E> _Unregister;

            public WeakEventHandler(EventHandler<E> eventHandler, UnregisterCallback<E> unregister)
            {
                _TargetRef = new WeakReference(eventHandler.Target);
                _OpenHandler = (OpenEventHandler)eventHandler.GetMethodInfo().CreateDelegate(typeof(OpenEventHandler), null);
                _Handler = Invoke;
                _Unregister = unregister;
            }

            public void Invoke(object sender, E e)
            {
                T target = (T)_TargetRef.Target;

                if (target != null)
                    _OpenHandler.Invoke(target, sender, e);
                else if (_Unregister != null)
                {
                    _Unregister(_Handler);
                    _Unregister = null;
                }
            }

            public EventHandler<E> Handler
            {
                get { return _Handler; }
            }

            public static implicit operator EventHandler<E>(WeakEventHandler<T, E> weh)
            {
                return weh._Handler;
            }
        }

        /// <summary>
        /// Provides a means to attach event handlers (delegates) to events using weak references to the target object instance the delegate refers to.
        /// This allows the object instance targeted by a handler to be garbage collected, resulting in auto-removal from the event provider's invocation list.
        /// (Note: This is of no use, and is not even needed, if the event provider is the one to be garbage collected)
        /// <para>Proper usage is to use the "MakeWeak()" method as an extension method.</para>
        /// Example: public event EventHandler&lt;MyEventData&gt; MyEvent { add { value.MakeWeak(eh =&gt; _MyEvent -= eh); } }
        /// </summary>
        /// <typeparam name="TSender">Type of class that owns the event.</typeparam>
        /// <typeparam name="E">Type of the handler's data parameter.</typeparam>
        /// <param name="eventHandler">The handler delegate.</param>
        /// <param name="unregister">The call-back to execute when the target object of a delegate get's collected - this call is expected to un-register the handler.</param>
        public static EventHandler<E> MakeWeak<E>(this EventHandler<E> eventHandler, UnregisterCallback<E> unregister)
                  where E : EventArgs
        {
            if (eventHandler == null)
                throw new ArgumentNullException("eventHandler");

            var methodInfo = eventHandler.GetMethodInfo();

            if (methodInfo.IsStatic || eventHandler.Target == null)
                throw new ArgumentException("Only instance methods are supported.", "eventHandler");

            Type wehType = typeof(WeakEventHandler<,>).MakeGenericType(methodInfo.DeclaringType, typeof(E));
            ConstructorInfo wehConstructor = wehType.GetConstructor(new Type[] { typeof(EventHandler<E>), typeof(UnregisterCallback<E>) });

            IWeakEventHandler<E> weh = (IWeakEventHandler<E>)wehConstructor.Invoke(
              new object[] { eventHandler, unregister });

            return weh.Handler;
        }

        // ---------------------------------------------------------------------------------------------------------------------
        // Weak references for custom events.

        public static class CustomWeakHandlers<TSender>
        {
            public delegate void EventHandler<TData>(TSender sender, TData data);

            public delegate void UnregisterCallback<TData>(EventHandler<TData> eventHandler);

            private interface IWeakEventHandler<TData>
            {
                EventHandler<TData> Handler { get; }
            }

            private class WeakEventHandler<T, TData> : IWeakEventHandler<TData>
                where T : class
            {
                private delegate void OpenEventHandler(T @this, object sender, TData e);
                private WeakReference _TargetRef;
                private OpenEventHandler _OpenHandler;
                private EventHandler<TData> _Handler;
                private UnregisterCallback<TData> _Unregister;

                public WeakEventHandler(EventHandler<TData> eventHandler, UnregisterCallback<TData> unregister)
                {
                    _TargetRef = new WeakReference(eventHandler.Target);
                    _OpenHandler = (OpenEventHandler)eventHandler.CreateDelegate(typeof(OpenEventHandler), (object)null);
                    _Handler = Invoke;
                    _Unregister = unregister;
                }

                public void Invoke(TSender sender, TData e)
                {
                    T target = (T)_TargetRef.Target;

                    if (target != null)
                        _OpenHandler.Invoke(target, sender, e);
                    else if (_Unregister != null)
                    {
                        _Unregister(_Handler);
                        _Unregister = null;
                    }
                }

                public EventHandler<TData> Handler
                {
                    get { return _Handler; }
                }

                public static implicit operator EventHandler<TData>(WeakEventHandler<T, TData> weh)
                {
                    return weh._Handler;
                }
            }

            public static EventHandler<TData> MakeWeak<TData>(EventHandler<TData> eventHandler, UnregisterCallback<TData> unregister)
            {
                if (eventHandler == null)
                    throw new ArgumentNullException("eventHandler");

                var methodInfo = eventHandler.GetMethodInfo();

                if (methodInfo.IsStatic || eventHandler.Target == null)
                    throw new ArgumentException("Only instance methods are supported.", "eventHandler");

                Type wehType = typeof(WeakEventHandler<,>).MakeGenericType(typeof(TSender), methodInfo.DeclaringType, typeof(TData));
                ConstructorInfo wehConstructor = wehType.GetConstructor(new Type[] { typeof(EventHandler<TData>), typeof(UnregisterCallback<TData>) });

                IWeakEventHandler<TData> weh = (IWeakEventHandler<TData>)wehConstructor.Invoke(
                  new object[] { eventHandler, unregister });

                return weh.Handler;
            }

        }

        /// <summary>
        /// Provides a means to attach event handlers (delegates) to events using weak references to the target object instance the delegate refers to.
        /// This allows the object instance targeted by a handler to be garbage collected, resulting in auto-removal from the event provider's invocation list.
        /// (Note: This is of no use, and is not even needed, if the event provider is the one to be garbage collected)
        /// <para>Proper usage is to use the "MakeWeak()" method as an extension method.</para>
        /// Example: public event EventHandling.WeakHandlers&lt;TheSenderType&gt;.EventHandler&lt;MyEventData&gt; MyEvent { add { value.MakeWeak(eh =&gt; _MyEvent -= eh); } }
        /// </summary>
        /// <typeparam name="TSender">Type of class that owns the event.</typeparam>
        /// <typeparam name="E">Type of the handler's data parameter.</typeparam>
        /// <param name="eventHandler">The handler delegate.</param>
        /// <param name="unregister">The call-back to execute when the target object of a delegate get's collected - this call is expected t un-register the handler.</param>
        public static CustomWeakHandlers<TSender>.EventHandler<E> MakeWeak<TSender, E>(this CustomWeakHandlers<TSender>.EventHandler<E> eventHandler, CustomWeakHandlers<TSender>.UnregisterCallback<E> unregister)
        { return CustomWeakHandlers<TSender>.MakeWeak(eventHandler, unregister); } // (Note: Extended methods cannot be in nested classes!)

        // ---------------------------------------------------------------------------------------------------------------------
    }

    // =========================================================================================================================
}
