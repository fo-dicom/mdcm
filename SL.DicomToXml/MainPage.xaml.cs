using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Dicom;
using Dicom.Data;
using Dicom.Imaging;

namespace SL.DicomToXml
{
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();
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
                        dicomFileDump.Text = sb.ToString();
/*
                        var xmlDoc = XDicom.ToXML(ff.Dataset, XDicomOptions.None);
                        var txtWriter = new StringWriter();
                        xmlDoc.Save(txtWriter);
                        dicomFileDump.Text = txtWriter.ToString();
*/
                        dicomImage.Source = ff.Dataset.Contains(DicomTags.PixelData)
                                                ? GetImageSource(ff.Dataset)
                                                : null;
                    }
                    else
                    {
                        dicomFileDump.Text = String.Format(Resources["noDicomDataMsg"].ToString(), dlg.File.Name);
                        dicomImage.Source = null;
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
    }
}
