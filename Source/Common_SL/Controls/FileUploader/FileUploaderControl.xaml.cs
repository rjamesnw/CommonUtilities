/*
Copyright 2003-2009 Virtual Chemistry, Inc. (VCI)
http://www.virtualchemistry.com
Using .Net, Silverlight, SharePoint and more to solve your tough problems in web-based data management.

Author: Peter Coley
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Browser;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Common;

namespace Vci.Silverlight.FileUploader
{
    [ScriptableType]
    public partial class FileUploaderControl : UserControl
    {
        private bool _multiSelect = true;
        protected FileCollection _files;

        public FileCollection Files { get { return _files; } }

        /// <summary>
        /// File filter -- see documentation for .net OpenFileDialog for details.
        /// </summary>
        public string FileFilter { get; set; }

        /// <summary>
        /// Path to the upload handler.
        /// </summary>
        public string UploadHandlerPath { get; set; }

        /// <summary>
        /// Maximum file size of an uploaded file in KB.  Defaults to no limit.
        /// </summary>
        public long MaxFileSizeKB { get; set; }

        /// <summary>
        /// The maximum number of files allowed to be selected.
        /// A value less then 0 (-1 is the default) means "no limit".
        /// </summary>
        public long MaxFiles { get { return _MaxFiles; } set { _MaxFiles = value; txtMaxFiles.Text = _MaxFiles >= 0 ? @" \ " + _MaxFiles : ""; } }
        long _MaxFiles;

        /// <summary>
        /// Chunk size in megabytes.  Defaults to 3 MB.
        /// </summary>
        public long ChunkSizeMB { get; set; }

        /// <summary>
        /// True if multiple files can be selected.  Defaults to true.
        /// </summary>
        public bool MultiSelect
        {
            get { return _multiSelect; }
            set
            {
                _multiSelect = value;
                txtEmptyMessage.Text = _multiSelect ? (string)txtEmptyMessage.Tag :
                    "Click 'Upload Files...' and choose a file to begin uploading, or drag-n-drop a file here.";
            }
        }

        /// <summary>
        /// Access to the scroll viewer -- used by Silverlight app that needs to host this in windowless mode
        /// and fix mouse capture bugs.
        /// </summary>
        public ScrollViewer ScrollViewer { get { return svFiles; } }

        /// <summary>
        /// This event is raised when 1 or more files are added to the file collection.
        /// </summary>
        [ScriptableMember]
        public event EventHandler FilesAdded; //<<

        ///// <summary>
        ///// This event is raised when the files begin uploading.
        ///// </summary>
        //[ScriptableMember]
        //??public event EventHandler UploadStarted; //<<

        public FileUploaderControl()
        {
            // Required to initialize variables
            InitializeComponent();

            txtEmptyMessage.Tag = txtEmptyMessage.Text;

            MaxFileSizeKB = -1;
            ChunkSizeMB = 3;
            MaxFiles = -1;

            MultiSelect = true;

            _files = new FileCollection { MaxUploads = 2 };

            icFiles.ItemsSource = _files;
            progressPercent.DataContext = _files;
            txtUploadedBytes.DataContext = _files;
            txtPercent.DataContext = _files;
            txtTotalFilesSelected.DataContext = _files;

            this.Loaded += new RoutedEventHandler(FileUploaderControl_Loaded);

            _files.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(_files_CollectionChanged);
            _files.PercentageChanged += new EventHandler(_files_PercentageChanged);
            _files.AllFilesFinished += new EventHandler(_files_AllFilesFinished);
            _files.ErrorOccurred += new EventHandler<UploadErrorOccurredEventArgs>(_files_ErrorOccurred);
        }

        void _files_ErrorOccurred(object sender, UploadErrorOccurredEventArgs e)
        {
            // not sure why this is necessary sometimes... the progress bar will get stuck after an error sometimes
            _files_PercentageChanged(sender, null);
        }

        void _files_PercentageChanged(object sender, EventArgs e)
        {
            // if the percentage is decreasing, don't use an animation
            if (_files.Percentage < progressPercent.Value)
                progressPercent.Value = _files.Percentage;
            else
            {
                sbProgressFrame.Value = _files.Percentage;
                sbProgress.Begin();
            }
        }

        void _files_AllFilesFinished(object sender, EventArgs e)
        {
            VisualStateManager.GoToState(this, "Finished", true);
        }

        void _files_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (_files.Count == 0)
            {
                VisualStateManager.GoToState(this, "Empty", true);
                btnChoose.IsEnabled = true;
            }
            else
            {
                // disable the upload button if only a single file can be uploaded
                btnChoose.IsEnabled = MultiSelect;

                if (_files.FirstOrDefault(f => !f.IsCompleted) != null)
                    VisualStateManager.GoToState(this, "Uploading", true);
                else
                    VisualStateManager.GoToState(this, "Finished", true);
            }

            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                Dispatching.QueueDispatch(Dispatching.Priority.Normal, () => { OnFilesAdded(); }, "_files_CollectionChanged");
        }

        private void FileUploaderControl_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= FileUploaderControl_Loaded;
            VisualStateManager.GoToState(this, "Empty", false);
        }

        /// <summary>
        /// Initialize this control from an InitParams collection provided to the silverlight plugin.  Also sets this control up
        /// to be accessed via javascript.
        /// </summary>
        /// <param name="InitParams"></param>
        public void InitializeFromInitParams(IDictionary<string, string> initParams)
        {
            HtmlPage.RegisterScriptableObject("UploaderControl", this);
            HtmlPage.RegisterScriptableObject("Files", _files);

            if (initParams.ContainsKey("UploadedFileProcessorType") && !string.IsNullOrEmpty(initParams["UploadedFileProcessorType"]))
                _files.UploadedFileProcessorType = initParams["UploadedFileProcessorType"];

            if (initParams.ContainsKey("MaxUploads") && !string.IsNullOrEmpty(initParams["MaxUploads"]))
                _files.MaxUploads = int.Parse(initParams["MaxUploads"]);

            if (initParams.ContainsKey("MaxFileSizeKB") && !string.IsNullOrEmpty(initParams["MaxFileSizeKB"]))
                MaxFileSizeKB = long.Parse(initParams["MaxFileSizeKB"]);

            if (initParams.ContainsKey("ChunkSizeMB") && !string.IsNullOrEmpty(initParams["ChunkSizeMB"]))
                ChunkSizeMB = long.Parse(initParams["ChunkSizeMB"]);

            if (initParams.ContainsKey("FileFilter") && !string.IsNullOrEmpty(initParams["FileFilter"]))
                FileFilter = initParams["FileFilter"];

            if (initParams.ContainsKey("UploadHandlerPath") && !string.IsNullOrEmpty(initParams["UploadHandlerPath"]))
                UploadHandlerPath = initParams["UploadHandlerPath"];

            if (initParams.ContainsKey("MultiSelect") && !string.IsNullOrEmpty(initParams["MultiSelect"]))
                MultiSelect = Convert.ToBoolean(initParams["MultiSelect"].ToLower());
        }

        private void btnChoose_Click(object sender, RoutedEventArgs e)
        {
            SelectUserFiles();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearFileList();
        }

        /// <summary>
        /// Open the select file dialog, and begin uploading files.
        /// </summary>
        private void SelectUserFiles()
        {
            if (MaxFiles >= 0 && _files.Count >= MaxFiles)
            {
                MessageBox.Show("The maximum number of files has been reached.");
                return;
            }

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = MultiSelect;

            try
            {
                // Check the file filter (filter is used to filter file extensions to select, for example only .jpg files)
                if (!string.IsNullOrEmpty(FileFilter))
                    ofd.Filter = FileFilter;
            }
            catch (ArgumentException ex)
            {
                // User supplied a wrong configuration filter
                throw new Exception("Wrong file filter configuration.", ex);
            }

            try
            {
                if (ofd.ShowDialog() == true)
                    AddFiles(ofd.Files.ToArray());
            }
            catch (System.Security.SecurityException ex) //<< A bug in Silverlight causes random failures if a debugger is attached. {http://forums.silverlight.net/t/203910.aspx/2/10}
            {
                return; // (do nothing)
            }
        }

        /// <summary>
        /// Adds the given list of files to the file collection.
        /// This method is handy for use with drag-n-drop scenarios where an array of type 'FileInfo' is used.
        /// This method returns the number of successful files added (those without error, or already exist, etc.).
        /// </summary>
        public int AddFiles(FileInfo[] files)
        {
            var count = 0;

            foreach (FileInfo file in files)
            {
                var existingUserFile = (from f in _files where f.FileName.ToLower() == file.Name.ToLower() select f).FirstOrDefault();

                if (existingUserFile == null)
                {
                    // create an object that represents a file being uploaded
                    UserFile userFile = CreateUserFileFromFileInfo(file);

                    // check the file size limit

                    if (MaxFileSizeKB > -1 && userFile.FileSize > MaxFileSizeKB * 1024) //<<
                    {
                        userFile.ErrorMessage = userFile.FileName + " exceeds the maximum file size of " + (MaxFileSizeKB).ToString() + "KB.";
                        userFile.State = Constants.FileStates.Error;
                    }
                    else if (MaxFiles >= 0 && _files.Count >= MaxFiles) //<<
                    {
                        userFile.ErrorMessage = "File limit has been reached (only " + Strings.S(MaxFiles, "file", "s") + " allowed).";
                        userFile.State = Constants.FileStates.Error;
                    }
                    else count++; //<<

                    _files.Add(userFile);
                }
            }

            return count;
        }

        public UserFile CreateUserFileFromFileInfo(FileInfo fileInfo)
        {
            // create an object that represents a file being uploaded
            UserFile userFile = new UserFile();
            userFile.FileName = fileInfo.Name;
            userFile.FileStream = fileInfo.OpenRead();
            userFile.UIDispatcher = this.Dispatcher;
            userFile.HttpUploader = true;
            userFile.UploadHandlerName = UploadHandlerPath;
            userFile.ChunkSizeMB = ChunkSizeMB;
            userFile.State = Constants.FileStates.Ready;
            return userFile;
        }

        /// <summary>
        /// Clear the file list
        /// </summary>
        [ScriptableMember]
        public void ClearFileList()
        {
            // clear all files that are completed (canceled, finished, error), or pending
            _files.Where(f => f.IsCompleted || f.State == Constants.FileStates.Pending).ToList().ForEach(f => _files.Remove(f));
        }

        private void OnFilesAdded()
        {
            if (FilesAdded != null)
                FilesAdded(this, null);
        }

        //??private void OnUploadStarted()
        //{
        //    if (UploadStarted != null)
        //        UploadStarted(this, null);
        //}

        private void LayoutRoot_Drop(object sender, DragEventArgs e)
        {
            LayoutRoot.Background = null;

            // ... get the list of files dropped ...

            FileInfo[] droppedFiles = e.Data.GetData(DataFormats.FileDrop) as FileInfo[];

            // ... add dropped files to the file uploader ...

            if (droppedFiles != null)
                AddFiles(droppedFiles);
        }

        private void LayoutRoot_DragEnter(object sender, DragEventArgs e)
        {
            LayoutRoot.Background = new SolidColorBrush(Color.FromArgb(128, 128, 255, 128));

            //??FileInfo[] droppedFiles = e.Data.GetData(DataFormats.FileDrop) as FileInfo[];
            //??LayoutRoot.Background = droppedFiles != null && droppedFiles.Length > 0 ?
            //    new SolidColorBrush(Colors.Green)
            //    : null;
        }

        private void LayoutRoot_DragLeave(object sender, DragEventArgs e)
        {
            LayoutRoot.Background = null;
        }
    }

    public class PercentConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string percent = "0%";

            if (value != null)
                percent = (int)value + "%";

            return percent;
        }

        // only use one-way binding for percentages
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}