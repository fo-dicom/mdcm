using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Dicom;
using Dicom.Data;
using Dicom.Imaging;
using Dicom.Network;
using Dicom.Network.Client;

namespace SL.DicomToXml
{
    public partial class MainPage : UserControl
    {
        #region FIELDS

        public static readonly DependencyProperty RawDumpProperty =
            DependencyProperty.Register("RawDump", typeof(string), typeof(MainPage), new PropertyMetadata(String.Empty));

        public static readonly DependencyProperty XmlDumpProperty =
            DependencyProperty.Register("XmlDump", typeof(string), typeof(MainPage), new PropertyMetadata(String.Empty));

        public static readonly DependencyProperty DicomImageProperty =
            DependencyProperty.Register("DicomImage", typeof(ImageSource), typeof(MainPage), new PropertyMetadata(null));

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

        #endregion

        #region METHODS

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

                        ff.Save("temp.dcm", DicomWriteOptions.Default);
                        SendDataToStoreScp("temp.dcm");
                    }
                    else
                    {
                        RawDump = XmlDump = String.Format(Resources["noDicomDataMsg"].ToString(), dlg.File.Name);
                        DicomImage = null;
                    }
                }
            }
        }

        private static ImageSource GetImageSource(DcmDataset iDataset)
        {
            try
            {
                return new DicomImage(iDataset).Render();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void SendDataToStoreScp(string iFileName)
        {
            CStoreClient scu = new CStoreClient
                                   {
                                       DisableFileStreaming = true,
                                       CallingAE = "STORE-SCU",
                                       CalledAE = "ANY-SCP",
                                       MaxPduSize = 16384,
                                       ConnectTimeout = 0,
                                       SocketTimeout = 30,
                                       DimseTimeout = 30,
                                       SerializedPresentationContexts = true,
                                       PreferredTransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian
                                   };
            scu.AddFile(iFileName);
            scu.Connect("localhost", 4502, DcmSocketType.TCP);
            if (!scu.Wait()) MessageBox.Show(scu.ErrorMessage);
        }

        #endregion
    }
}
