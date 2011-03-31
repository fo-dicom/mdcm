using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using Dicom;
using Dicom.Data;

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
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    var origFs = dlg.File.OpenRead();
                    var tmpFs = store.CreateFile("tmp.bin");
                    origFs.CopyTo(tmpFs);
                    origFs.Close();

                    DicomFileFormat ff = new DicomFileFormat();
                    ff.Load(tmpFs, DicomReadOptions.Default);
                    var xmlDoc = XDicom.ToXML(ff.Dataset, XDicomOptions.Default);
                    var txtWriter = new StringWriter();
                    xmlDoc.Save(txtWriter);
                    dicomFileDump.Text = txtWriter.ToString();

                    tmpFs.Close();
                    store.DeleteFile("tmp.bin");
                }
            }
        }
    }
}
