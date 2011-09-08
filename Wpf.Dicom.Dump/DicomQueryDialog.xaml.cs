using System;
using System.Windows;
using Dicom.Network;
using Dicom.Network.Client;

namespace Wpf.Dicom.Dump
{
    /// <summary>
    /// Interaction logic for DicomQueryDialog.xaml
    /// </summary>
    public partial class DicomQueryDialog
    {
        public DicomQueryDialog()
        {
            InitializeComponent();
        }

        private void testButton_Click(object sender, RoutedEventArgs e)
        {
            var success = false;
            var message = "Unidentified failure.";

            try
            {
                var echoScu = new CEchoClient { CallingAE = callingAetTextBox.Text, CalledAE = calledAetTextBox.Text };
                echoScu.OnCEchoResponse += delegate(byte presentationId, ushort messageId, DcmStatus status)
                {
                    message = status.ToString();
                };
                echoScu.Connect(hostTextBox.Text, Int32.Parse(portTextBox.Text), DcmSocketType.TCP);
                if (!(success = echoScu.Wait())) message = echoScu.ErrorMessage;
            }
            catch (Exception exception)
            {
                message = exception.Message;
            }

            MessageBox.Show(this, message, "DICOM C-Echo result", MessageBoxButton.OK,
                            success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
