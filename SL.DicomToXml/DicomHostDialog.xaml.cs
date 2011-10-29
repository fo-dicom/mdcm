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
        #region FIELDS

        // Using a DependencyProperty as the backing store for DicomHost.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DicomHostProperty =
            DependencyProperty.Register("DicomHost", typeof(string), typeof(DicomHostDialog), new PropertyMetadata("server"));

        // Using a DependencyProperty as the backing store for ServerPort.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ServerPortProperty =
            DependencyProperty.Register("ServerPort", typeof(int), typeof(DicomHostDialog), new PropertyMetadata(104));

        // Using a DependencyProperty as the backing store for CalledApplicationEntityTitle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CalledApplicationEntityTitleProperty =
            DependencyProperty.Register("CalledApplicationEntityTitle", typeof(string), typeof(DicomHostDialog), new PropertyMetadata("COMMON"));

        // Using a DependencyProperty as the backing store for CallingApplicationEntityTitle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CallingApplicationEntityTitleProperty =
            DependencyProperty.Register("CallingApplicationEntityTitle", typeof(string), typeof(DicomHostDialog), new PropertyMetadata("ANYSCU"));

        #endregion

        #region CONSTRUCTORS

        public DicomHostDialog()
        {
            InitializeComponent();
        }

        #endregion

        #region DEPENDENCY PROPERTIES

        public string DicomHost
        {
            get { return (string)GetValue(DicomHostProperty); }
            set { SetValue(DicomHostProperty, value); }
        }

        public int ServerPort
        {
            get { return (int)GetValue(ServerPortProperty); }
            set { SetValue(ServerPortProperty, value); }
        }

        public string CalledApplicationEntityTitle
        {
            get { return (string)GetValue(CalledApplicationEntityTitleProperty); }
            set { SetValue(CalledApplicationEntityTitleProperty, value); }
        }

        public string CallingApplicationEntityTitle
        {
            get { return (string)GetValue(CallingApplicationEntityTitleProperty); }
            set { SetValue(CallingApplicationEntityTitleProperty, value); }
        }

        #endregion
        
        #region EVENT HANDLERS

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

        #endregion
    }
}

