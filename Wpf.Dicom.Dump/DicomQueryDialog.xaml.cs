using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Dicom.Data;
using Dicom.Network;
using Dicom.Network.Client;
using Dicom.Network.Server;

namespace Wpf.Dicom.Dump
{
    /// <summary>
    /// Interaction logic for DicomQueryDialog.xaml
    /// </summary>
    public partial class DicomQueryDialog
    {
        private DcmServer<CStoreService> storeScp;

        private delegate void ClearStudyResponsesDelegate();

        private delegate void AddToStudyResponsesDelegate(CFindStudyResponse response);

        private delegate void ClearSelectedStudyDatasetsDelegate();

        private delegate void AddToSelectedStudyDatasetsDelegate(DcmDataset dataset);

        public ObservableCollection<CFindStudyResponse> StudyResponses
        {
            get { return (ObservableCollection<CFindStudyResponse>)GetValue(StudyResponsesProperty); }
            set { SetValue(StudyResponsesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StudyResponsesProperty =
            DependencyProperty.Register("StudyResponses", typeof(ObservableCollection<CFindStudyResponse>),
                                        typeof(DicomQueryDialog),
                                        new UIPropertyMetadata(new ObservableCollection<CFindStudyResponse>()));

        public ObservableCollection<DcmDataset> SelectedStudyDatasets
        {
            get { return (ObservableCollection<DcmDataset>)GetValue(SelectedStudyDatasetsProperty); }
            set { SetValue(SelectedStudyDatasetsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedStudyDatasets.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedStudyDatasetsProperty =
            DependencyProperty.Register("SelectedStudyDatasets", typeof(ObservableCollection<DcmDataset>),
                                        typeof(DicomQueryDialog),
                                        new UIPropertyMetadata(new ObservableCollection<DcmDataset>()));

        public DicomQueryDialog()
        {
            InitializeComponent();
        }

        private void QueryButtonClick(object sender, RoutedEventArgs e)
        {
            var success = false;
            var message = "Unidentified failure.";

            try
            {
                Dispatcher.BeginInvoke(new ClearStudyResponsesDelegate(() => StudyResponses.Clear()));

                var findStudy = new CFindStudyClient
                {
                    CallingAE = callingAetTextBox.Text,
                    CalledAE = calledAetTextBox.Text
                };
                findStudy.OnCFindResponse +=
                    delegate(CFindStudyQuery query, CFindStudyResponse result) { if (result != null)
                        Dispatcher.BeginInvoke(new AddToStudyResponsesDelegate(r => StudyResponses.Add(r)), result); };

                findStudy.AddQuery(new CFindStudyQuery());

                findStudy.Connect(hostTextBox.Text, Int32.Parse(portTextBox.Text), DcmSocketType.TCP);
                if (findStudy.Wait())
                {
                    success = true;
                }
                else
                {
                    message = findStudy.ErrorMessage;
                }
            }
            catch (Exception exception)
            {
                message = exception.Message;
            }

            if (!success)
            {
                MessageBox.Show(message, "C-FIND Study error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void getInstancesButton_Click(object sender, RoutedEventArgs e)
        {
            var success = false;
            var message = "Unidentified failure.";

            try
            {
                Dispatcher.BeginInvoke(new ClearSelectedStudyDatasetsDelegate(() => SelectedStudyDatasets.Clear()));

                var selStudy = studiesListBox.SelectedItem as CFindStudyResponse;

                if (selStudy != null)
                {
                    var moveStudy = new CMoveClient
                                        {
                                            CallingAE = callingAetTextBox.Text,
                                            CalledAE = calledAetTextBox.Text,
                                            DestinationAE = callingAetTextBox.Text
                                        };
                    moveStudy.OnCMoveResponse +=
                        delegate(CMoveQuery query, DcmDataset dataset, DcmStatus status, ushort remain, ushort complete,
                                 ushort warning, ushort failure)
                            {
                            };
                    moveStudy.AddQuery(DcmQueryRetrieveLevel.Study, selStudy.StudyInstanceUID);

                    moveStudy.Connect(hostTextBox.Text, Int32.Parse(portTextBox.Text), DcmSocketType.TCP);
                    if (moveStudy.Wait())
                    {
                        success = true;
                    }
                    else
                    {
                        message = moveStudy.ErrorMessage;
                    }
                }
            }
            catch (Exception exception)
            {
                message = exception.Message;
            }

            if (!success)
            {
                MessageBox.Show(message, "C-MOVE Study error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Main_Loaded(object sender, RoutedEventArgs e)
        {
            storeScp = new DcmServer<CStoreService>();
            storeScp.AddPort(104, DcmSocketType.TCP);

            storeScp.OnDicomClientCreated +=
                delegate(DcmServer<CStoreService> server, CStoreService client, DcmSocketType socketType)
                {
                    client.OnCStoreRequest +=
                        delegate(CStoreService client1, byte presentationId, ushort messageId,
                                 DicomUID affectedInstance, DcmPriority priority, string moveAe,
                                 ushort moveMessageId, DcmDataset dataset, string fileName)
                        {
                            if (dataset != null)
                                Dispatcher.BeginInvoke(
                                    new AddToSelectedStudyDatasetsDelegate(d => SelectedStudyDatasets.Add(d)),
                                    dataset);
                            return new DcmStatus("0000", DcmState.Success, "Success");
                        };

                };
            storeScp.Start();
        }

        private void Main_Unloaded(object sender, RoutedEventArgs e)
        {
            if (storeScp != null) storeScp.Stop();
        }

        public DcmDataset GetSelectedDicomDataset()
        {
            return instancesListBox.SelectedItem as DcmDataset;
        }
    }
}
