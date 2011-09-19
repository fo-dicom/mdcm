using System;
using System.Collections.ObjectModel;
using System.Windows;
using Dicom.Data;
using Dicom.Network;
using Dicom.Network.Client;
using Dicom.Network.Server;

namespace SL.DicomToXml
{
    public partial class DicomServerGetDialog
    {
        #region DELEGATES

        private delegate void ClearStudyResponsesDelegate();

        private delegate void AddToStudyResponsesDelegate(CFindStudyResponse response);

        private delegate void ClearSelectedStudyDatasetsDelegate();

        private delegate void AddToSelectedStudyDatasetsDelegate(DcmDataset dataset);

        #endregion

        #region FIELDS

        private DcmServer<CStoreService> _storeScp;

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StudyResponsesProperty =
            DependencyProperty.Register("StudyResponses", typeof(ObservableCollection<CFindStudyResponse>),
                                        typeof(DicomServerGetDialog),
                                        new PropertyMetadata(new ObservableCollection<CFindStudyResponse>()));

        // Using a DependencyProperty as the backing store for SelectedStudyDatasets.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedStudyDatasetsProperty =
            DependencyProperty.Register("SelectedStudyDatasets", typeof(ObservableCollection<DcmDataset>),
                                        typeof(DicomServerGetDialog),
                                        new PropertyMetadata(new ObservableCollection<DcmDataset>()));

        #endregion
        
        #region CONSTRUCTORS

        public DicomServerGetDialog()
        {
            InitializeComponent();
        }

        #endregion

        #region DEPENDENCY PROPERTIES

        public ObservableCollection<CFindStudyResponse> StudyResponses
        {
            get { return (ObservableCollection<CFindStudyResponse>)GetValue(StudyResponsesProperty); }
            set { SetValue(StudyResponsesProperty, value); }
        }

        public ObservableCollection<DcmDataset> SelectedStudyDatasets
        {
            get { return (ObservableCollection<DcmDataset>)GetValue(SelectedStudyDatasetsProperty); }
            set { SetValue(SelectedStudyDatasetsProperty, value); }
        }

        #endregion
        
        #region AUTO-IMPLEMENTED PROPERTIES

        public string DicomHost { get; set; }

        public int ServerPort { get; set; }

        public string CalledApplicationEntityTitle { get; set; }

        public string CallingApplicationEntityTitle { get; set; }

        #endregion
        
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void getStudiesButton_Click(object sender, RoutedEventArgs e)
        {
            var success = false;
            var message = "Unidentified failure.";

            try
            {
                Dispatcher.BeginInvoke(new ClearStudyResponsesDelegate(() => StudyResponses.Clear()));

                var findStudy = new CFindStudyClient
                {
                    CallingAE = CallingApplicationEntityTitle,
                    CalledAE = CalledApplicationEntityTitle
                };
                findStudy.OnCFindResponse +=
                    delegate(CFindStudyQuery query, CFindStudyResponse result)
                    {
                        if (result != null)
                            Dispatcher.BeginInvoke(new AddToStudyResponsesDelegate(r => StudyResponses.Add(r)), result);
                    };

                findStudy.AddQuery(new CFindStudyQuery());

                findStudy.Connect(DicomHost, ServerPort, DcmSocketType.TCP);
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
                MessageBox.Show(message, "C-FIND Study error", MessageBoxButton.OK);
            }
        }
    }
}

