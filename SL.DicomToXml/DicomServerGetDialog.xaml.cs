using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
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

        private delegate void ClearSelectedStudyImagesDelegate();

        private delegate void AddToSelectedStudyImagesDelegate(CFindImageResponse dataset);

        #endregion

        #region FIELDS

        private DcmServer<CStoreService> _storeScp;
        private DcmDataset _selectedDataset;

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StudyResponsesProperty =
            DependencyProperty.Register("StudyResponses", typeof(ObservableCollection<CFindStudyResponse>),
                                        typeof(DicomServerGetDialog),
                                        new PropertyMetadata(new ObservableCollection<CFindStudyResponse>()));

        // Using a DependencyProperty as the backing store for SelectedStudyDatasets.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedStudyImagesProperty =
            DependencyProperty.Register("SelectedStudyImages", typeof(ObservableCollection<CFindImageResponse>),
                                        typeof(DicomServerGetDialog),
                                        new PropertyMetadata(new ObservableCollection<CFindImageResponse>()));

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

        public ObservableCollection<CFindImageResponse> SelectedStudyImages
        {
            get { return (ObservableCollection<CFindImageResponse>)GetValue(SelectedStudyImagesProperty); }
            set { SetValue(SelectedStudyImagesProperty, value); }
        }

        #endregion
        
        #region AUTO-IMPLEMENTED PROPERTIES

        public string DicomHost { get; set; }

        public int ServerPort { get; set; }

        public string CalledApplicationEntityTitle { get; set; }

        public string CallingApplicationEntityTitle { get; set; }

        #endregion

        #region PROPERTIES

        public string SelectedImageSopInstanceUid
        {
            get
            {
                var selImage = imagesDataGrid.SelectedItem as CFindImageResponse;
                return selImage != null ? selImage.SopInstanceUid : String.Empty;
            }
        }

        #endregion
        
        #region METHODS

        public DcmDataset GetSelectedDataset()
        {
            var success = false;
            var message = "Unidentified failure.";

            try
            {
                var selImage = imagesDataGrid.SelectedItem as CFindImageResponse;

                if (selImage != null)
                {
                    var cGetImage = new CGetClient
                    {
                        CallingAE = CallingApplicationEntityTitle,
                        CalledAE = CalledApplicationEntityTitle,
                    };
                    cGetImage.OnCStoreRequest +=
                        delegate(byte presentationId, ushort messageId, DicomUID affectedInstance, DcmPriority priority,
                                 string moveAe, ushort moveMessageId, DcmDataset dataset, string fileName)
                            {
                                if (dataset != null)
                                {
                                    dataset.PreloadDeferredBuffers();
                                    _selectedDataset = dataset;
                                }
                                return new DcmStatus("0000", DcmState.Success, "Success");

                            };
                    cGetImage.AddQuery(new CGetQuery(selImage.StudyInstanceUid, selImage.SeriesInstanceUid,
                                                      selImage.SopInstanceUid));

                    cGetImage.Connect(DicomHost, ServerPort, DcmSocketType.TCP);
                    if (cGetImage.Wait())
                    {
                        success = true;
                    }
                    else
                    {
                        message = cGetImage.ErrorMessage;
                    }
                }
            }
            catch (Exception exception)
            {
                message = exception.Message;
            }

            if (!success)
            {
                MessageBox.Show(message, "C-GET error", MessageBoxButton.OK);
            }

            return _selectedDataset;
        }

        #endregion
        
        #region EVENT HANDLERS

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void studiesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var success = false;
            var message = "Unidentified failure.";

            try
            {
                Dispatcher.BeginInvoke(new ClearSelectedStudyImagesDelegate(() => SelectedStudyImages.Clear()));

                var selStudy = studiesDataGrid.SelectedItem as CFindStudyResponse;

                if (selStudy != null)
                {
                    var findImages = new CFindImageClient
                                         {
                                             CallingAE = CallingApplicationEntityTitle,
                                             CalledAE = CalledApplicationEntityTitle
                                         };
                    findImages.OnCFindResponse += delegate(CFindImageQuery query, CFindImageResponse result)
                                                      {
                                                          if (result != null)
                                                              Dispatcher.BeginInvoke(new AddToSelectedStudyImagesDelegate(r => SelectedStudyImages.Add(r)), result);
                                                      };
                                                       
                    findImages.AddQuery(new CFindImageQuery { StudyInstanceUid = selStudy.StudyInstanceUID });

                    findImages.Connect(DicomHost, ServerPort, DcmSocketType.TCP);
                    if (findImages.Wait())
                    {
                        success = true;
                    }
                    else
                    {
                        message = findImages.ErrorMessage;
                    }
                }
            }
            catch (Exception exception)
            {
                message = exception.Message;
            }

            if (!success)
            {
                MessageBox.Show(message, "C-FIND error", MessageBoxButton.OK);
            }
        }

        #endregion

        #region PRIVATE METHODS

        private void GetStudies()
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

        #endregion
    }
}

