using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Dicom;
using Dicom.Codec;
using Dicom.Data;
using Dicom.Imaging;
using Microsoft.Win32;

namespace Wpf.Dicom.Dump
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DicomCodec.RegisterCodecs();
            DicomCodec.RegisterExternalCodecs(".", "Dicom.Codec*.dll");

            if (File.Exists("dicom.dic"))
                DcmDictionary.ImportDictionary("dicom.dic");
            else
                DcmDictionary.LoadInternalDictionary();

            if (File.Exists("private.dic"))
                DcmDictionary.ImportDictionary("private.dic");

            Debug.InitializeConsoleDebugLogger();
        }

        private void LoadButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = Resources["dicomFileDialog"] as OpenFileDialog;
            if (dlg != null && dlg.ShowDialog().GetValueOrDefault())
            {
                using (var memStream = new MemoryStream())
                {
                    using (var fileStream = dlg.OpenFile())
                    {
                        fileStream.CopyTo(memStream);
                    }

                    var ff = new DicomFileFormat();
                    ff.Load(memStream, DicomReadOptions.Default);
                    if (ff.Dataset != null)
                    {
                        var xmlDoc = XDicom.ToXML(ff.Dataset, XDicomOptions.None);
                        var txtWriter = new StringWriter();
                        xmlDoc.Save(txtWriter);
                        dicomDumpTextBox.Text = txtWriter.ToString();

                        dicomImage.Source = ff.Dataset.Contains(DicomTags.PixelData)
                                         ? GetImageSource(ff.Dataset)
                                         : null;

/*
                        string tempDicomFile = "temp.dcm";
                        ff.Save(tempDicomFile, DicomWriteOptions.ExplicitLengthSequenceItem);
                        SendDataToStoreScp(tempDicomFile);

                        using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            if (store.FileExists(tempDicomFile)) store.DeleteFile(tempDicomFile);
                        }
*/
                    }
                    else
                    {
                        dicomDumpTextBox.Text = String.Format(Resources["noDicomDataMsg"].ToString(), dlg.FileName);
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

        private void getFromStoreButton_Click(object sender, RoutedEventArgs e)
        {
            var queryDlg = new DicomQueryDialog();
            if (queryDlg.ShowDialog().GetValueOrDefault())
            {
                
            }
        }
    }
}
