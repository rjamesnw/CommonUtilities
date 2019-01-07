using System.IO;
using System.IO.IsolatedStorage;

namespace Common.WPF.Storage
{
    // ###############################################################################################################

    public static class IsolatedStorage
    {
        // -----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Saves the given text data to a file store based on a given filename and storage scope.
        /// </summary>
        public static bool Save(string fileName, string data, StorageScope storageScope = StorageScope.Application)
        {
            try
            {
                using (IsolatedStorageFile isf = (storageScope == StorageScope.Application ? IsolatedStorageFile.GetUserStoreForApplication() : IsolatedStorageFile.GetUserStoreForSite()))
                {
                    using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(fileName, FileMode.Create, isf))
                    {
                        using (StreamWriter sw = new StreamWriter(isfs))
                        {
                            sw.Write(data);
                            sw.Close();
                            return true;
                        }
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        // -----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Loads text from a file store based on the given filename and storage scope.
        /// </summary>
        public static string Load(string fileName, StorageScope storageScope = StorageScope.Application)
        {
            try
            {
                using (IsolatedStorageFile isf = (storageScope == StorageScope.Application ? IsolatedStorageFile.GetUserStoreForApplication() : IsolatedStorageFile.GetUserStoreForSite()))
                {
                    using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(fileName, FileMode.Open, isf))
                    {
                        using (StreamReader sr = new StreamReader(isfs))
                        {
                            string data = sr.ReadToEnd();
                            return data;
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        // -----------------------------------------------------------------------------------------------------------

        public enum StorageScope { Application, Domain };

        // -----------------------------------------------------------------------------------------------------------
    }

    // ###############################################################################################################
}
