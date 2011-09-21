using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Dicom;
using Dicom.Codec;
using Dicom.Data;
using Dicom.Imaging;
using Dicom.Network;
using Dicom.Network.Client;

namespace SL.DicomToXml
{
    public partial class MainPage
    {
        #region FIELDS

        private DicomHostDialog dicomHostDialog;

        public static readonly DependencyProperty RawDumpProperty =
            DependencyProperty.Register("RawDump", typeof(string), typeof(MainPage), new PropertyMetadata(String.Empty));

        public static readonly DependencyProperty XmlDumpProperty =
            DependencyProperty.Register("XmlDump", typeof(string), typeof(MainPage), new PropertyMetadata(String.Empty));

        public static readonly DependencyProperty DicomImageProperty =
            DependencyProperty.Register("DicomImage", typeof(ImageSource), typeof(MainPage), new PropertyMetadata(null));

        public static readonly DependencyProperty LogProperty =
            DependencyProperty.Register("Log", typeof(string), typeof(MainPage), new PropertyMetadata(String.Empty));

        #endregion

        #region CONSTRUCTORS

        public MainPage()
        {
            InitializeComponent();
        }

        #endregion

        #region DEPENDENCY PROPERTIES

        public string RawDump
        {
            get { return (string)GetValue(RawDumpProperty); }
            set { SetValue(RawDumpProperty, value); }
        }

        public string XmlDump
        {
            get { return (string)GetValue(XmlDumpProperty); }
            set { SetValue(XmlDumpProperty, value); }
        }

        public ImageSource DicomImage
        {
            get { return (ImageSource)GetValue(DicomImageProperty); }
            set { SetValue(DicomImageProperty, value); }
        }

        public string Log
        {
            get { return (string)GetValue(LogProperty); }
            set { SetValue(LogProperty, value); }
        }
        
        #endregion

        #region METHODS

        private void mainPage_Loaded(object sender, RoutedEventArgs e)
        {
            dicomHostDialog = new DicomHostDialog();

            DcmRleCodec.Register();
            DcmJpegCodec.Register();

            Debug.InitializeIsolatedStorageDebugLogger();
            Debug.Log.Info(String.Empty);
        }

        private void fileNameButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = Resources["dicomFileDialog"] as OpenFileDialog;
            if (dlg != null && dlg.ShowDialog().GetValueOrDefault())
            {
                using (var memStream = new MemoryStream())
                {
                    using (var fileStream = dlg.File.OpenRead())
                    {
                        fileStream.CopyTo(memStream);
                    }

                    DicomFileFormat ff = new DicomFileFormat();
                    ff.Load(memStream, DicomReadOptions.Default);
                    if (ff.Dataset != null)
                    {
                        StringBuilder sb = new StringBuilder();
                        ff.Dataset.Dump(sb, String.Empty, DicomDumpOptions.Default);
                        RawDump = sb.ToString();

                        var xmlDoc = XDicom.ToXML(ff.Dataset, XDicomOptions.None);
                        var txtWriter = new StringWriter();
                        xmlDoc.Save(txtWriter);
                        XmlDump = txtWriter.ToString();

                        DicomImage = ff.Dataset.Contains(DicomTags.PixelData)
                                         ? GetImageSource(ff.Dataset)
                                         : null;
                    }
                    else
                    {
                        RawDump = XmlDump = String.Format(Resources["noDicomFileDataMsg"].ToString(), dlg.File.Name);
                        DicomImage = null;
                    }
                }
                UpdateLog();
            }
        }

        private static ImageSource GetImageSource(DcmDataset iDataset)
        {
            try
            {
                Debug.Log.Info("Image transfer syntax: {0}", iDataset.InternalTransferSyntax);
                return new DicomImage(iDataset).Render();
            }
            catch (Exception e)
            {
                Debug.Log.Error("Image display failed {0}, reason: {1}", e.StackTrace, e.Message);
                return null;
            }
        }

        private void UpdateLog()
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                try
                {
                    using (
                        var stream = new IsolatedStorageFileStream("Solution.Silverlight.log", FileMode.Open,
                                                                   FileAccess.Read, store))
                    {
                        var reader = new StreamReader(stream);
                        Log = reader.ReadToEnd();
                        reader.Close();
                    }
                }
                catch (IsolatedStorageException)
                {
                }
            }
        }

        #endregion

        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
            dicomHostDialog.Show();
        }

        private void getFromServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (dicomHostDialog.DialogResult.GetValueOrDefault())
            {
                var getFromServerDlg = new DicomServerGetDialog
                                           {
                                               DicomHost = dicomHostDialog.DicomHost,
                                               ServerPort = dicomHostDialog.ServerPort,
                                               CalledApplicationEntityTitle = dicomHostDialog.CalledApplicationEntityTitle,
                                               CallingApplicationEntityTitle =
                                                   dicomHostDialog.CallingApplicationEntityTitle
                                           };
                getFromServerDlg.Closed += getFromServerDlg_Closed;
                getFromServerDlg.Show();
            }
            else
            {
                Debug.Log.Warn("Connection to DICOM server has not been set up");
            }
            UpdateLog();
        }

        void getFromServerDlg_Closed(object sender, EventArgs e)
        {
            var dlg = sender as DicomServerGetDialog;
            if (dlg != null && dlg.DialogResult.GetValueOrDefault())
            {
                var dataset = dlg.GetSelectedDataset();
                if (dataset != null)
                {
                    StringBuilder sb = new StringBuilder();
                    dataset.Dump(sb, String.Empty, DicomDumpOptions.Default);
                    RawDump = sb.ToString();

                    var xmlDoc = XDicom.ToXML(dataset, XDicomOptions.None);
                    var txtWriter = new StringWriter();
                    xmlDoc.Save(txtWriter);
                    XmlDump = txtWriter.ToString();

                    DicomImage = dataset.Contains(DicomTags.PixelData)
                                     ? GetImageSource(dataset)
                                     : null;
                }
                else
                {
                    RawDump = XmlDump = String.Format(Resources["noDicomObjectDataMsg"].ToString(), dlg.SelectedImageSopInstanceUid);
                    DicomImage = null;
                }
            }
            UpdateLog();
        }
    }
}
