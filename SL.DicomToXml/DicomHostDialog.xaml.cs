using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Dicom.Network;
using Dicom.Network.Client;

namespace SL.DicomToXml
{
    public partial class DicomHostDialog
    {
        public DicomHostDialog()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            var success = false;
            var message = "Unidentified failure";

            var echoClient = new CEchoClient { CalledAE = calledAetTextBox.Text, CallingAE = callingAetTextBox.Text };
            echoClient.OnCEchoResponse += delegate(byte presentationId, ushort messageId, DcmStatus status)
                                              {
                                                  success = status.State == DcmState.Success;
                                                  message = success ? "Connection successful" : status.ErrorComment;
                                              };

            echoClient.Connect(hostTextBox.Text, Int32.Parse(portTextBox.Text), DcmSocketType.TCP);
            if (!echoClient.Wait())
            {
                success = false;
                message = "Connection failed";
            }

            MessageBox.Show(message, "C-ECHO result", MessageBoxButton.OK);
        }
    }
}

