// Copyright (c) 2011  Pantelis Georgiadis, Mobile Solutions
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// Author:
//    Pantelis Georgiadis (PantelisGeorgiadis@Gmail.com)

using System;
using System.Collections.Generic;
using Dicom;
using Dicom.Data;
using Dicom.Network;
using Dicom.Network.Client;
using Dicom.Imaging;

namespace Dicom.Network.Client
{
    public class DcmPrinterStatus
    {
        #region Private Members
        private DcmDataset _dataset;
        #endregion

        #region Public Constructors
        public DcmPrinterStatus(DcmDataset dataset)
        {
            _dataset = dataset;
        }
        #endregion

        #region Public Properties
        public DcmDataset Dataset
        {
            get { return _dataset; }
        }

        public string PrinterStatus
        {
            get { return _dataset.GetString(DicomTags.PrinterStatus, null); }
        }

        public string PrinterName
        {
            get { return _dataset.GetString(DicomTags.PrinterName, null); }
        }

        public string Manufacturer
        {
            get { return _dataset.GetString(DicomTags.Manufacturer, null); }
        }

        public string ManufacturersModelName
        {
            get { return _dataset.GetString(DicomTags.ManufacturersModelName, null); }
        }

        public string DeviceSerialNumber
        {
            get { return _dataset.GetString(DicomTags.DeviceSerialNumber, null); }
        }

        public string SoftwareVersions
        {
            get { return _dataset.GetString(DicomTags.SoftwareVersions, null); }
        }

        public DateTime DateTimeOfLastCalibration
        {
            get { return _dataset.GetDateTime(DicomTags.DateOfLastCalibration, DicomTags.TimeOfLastCalibration, DateTime.MinValue); }
        }
        #endregion
    }

    public delegate bool PrintNGetPrinterStatusResponseDelegate(DcmPrinterStatus printerstatus, DcmStatus status);

    public sealed class PrintClient : DcmClientBase
    {
        #region Private Members
        private List<String> _files;
        private int _numberOfCopies;
        private string _printPriority;
        private string _mediumType;
        private string _filmDestination;
        private string _filmSessionLabel;
        private string _ownerID;
        private string _imageDisplayFormat;
        private string _filmOrientation;
        private string _filmSizeID;
        private string _magnificationType;
        private ushort _maxDensity;
        private string _configurationInformation;
        private string _annotationDisplayFormatID;
        private string _smoothingType;
        private string _borderDensity;
        private string _emptyImageDensity;
        private ushort _minDensity;
        private string _trim;
        private ushort _illumination;
        private ushort _reflectedAmbientLight;
        private string _requestedResolutionID;

        private bool _queryPrinterStatus;

        private bool _supportsPrinting;
        private bool _acceptedPrinterStatus;
        private bool _supportsGrayscalePrinting;
        private bool _supportsColorPrinting;

        private DcmFilmSession _filmSession = null;
        private List<DcmFilmBox> _pendingFilmBoxResponses = null; 
        private List<DcmImageBox> _pendingImageBoxResponses = null; 
        #endregion

        #region Public Constructor
        public PrintClient()
            : base()
        {
			LogID = "Print SCU";
			CallingAE = "PRINT_SCU";
			CalledAE = "PRINT_SCP";

            _queryPrinterStatus = true;

            _supportsPrinting = false;
            _supportsGrayscalePrinting = false;
            _supportsColorPrinting = false;
            _acceptedPrinterStatus = true;

            _files = new List<String>();
            _pendingFilmBoxResponses = new List<DcmFilmBox>();
            _pendingImageBoxResponses = new List<DcmImageBox>();
		}
		#endregion

        #region Public Properties
        public PrintNGetPrinterStatusResponseDelegate OnPrintGetPrinterStatusResponse;

        public List<String> Files
        {
            get { return _files; }
        }

        public bool QueryPrinterStatus
        {
            get { return _queryPrinterStatus; }
            set { _queryPrinterStatus = value; }
        }

        /// <summary>Number of copies to be printed for each film of the film session.</summary>
        public int NumberOfCopies
        {
            get { return _numberOfCopies; }
            set { _numberOfCopies = value; }
        }

        /// <summary>Specifies the priority of the print job.</summary>
        /// <remarks>
        /// Enumerated values:
        /// <list type="bullet">
        /// <item><description>HIGH</description></item>
        /// <item><description>MED</description></item>
        /// <item><description>LOW</description></item>
        /// </list>
        /// </remarks>
        public string PrintPriority
        {
            get { return _printPriority; }
            set { _printPriority = value; }
        }

        /// <summary>Type of medium on which the print job will be printed.</summary>
        /// <remarks>
        /// Defined Terms:
        /// <list type="bullet">
        /// <item><description>PAPER</description></item>
        /// <item><description>CLEAR FILM</description></item>
        /// <item><description>BLUE FILM</description></item>
        /// <item><description>MAMMO CLEAR FILM</description></item>
        /// <item><description>MAMMO BLUE FILM</description></item>
        /// </list>
        /// </remarks>
        public string MediumType
        {
            get { return _mediumType; }
            set { _mediumType = value; }
        }

        /// <summary>Film destination.</summary>
        /// <remarks>
        /// Defined Terms:
        /// <list type="bullet">
        /// <item>
        ///   <term>MAGAZINE</term>
        ///   <description>the exposed film is stored in film magazine</description>
        /// </item>
        /// <item>
        ///   <term>PROCESSOR</term>
        ///   <description>the exposed film is developed in film processor</description>
        /// </item>
        /// <item>
        ///   <term>BIN_i</term>
        ///   <description>the exposed film is deposited in a sorter bin where “I” represents the bin 
        ///   number. Film sorter BINs shall be numbered sequentially starting from one and no maxium 
        ///   is placed on the number of BINs. The encoding of the BIN number shall not contain leading
        ///   zeros.</description>
        /// </item>
        /// </remarks>
        public string FilmDestination
        {
            get { return _filmDestination; }
            set { _filmDestination = value; }
        }

        /// <summary>Human readable label that identifies the film session.</summary>
        public string FilmSessionLabel
        {
            get { return _filmSessionLabel; }
            set { _filmSessionLabel = value; }
        }

        /// <summary>Identification of the owner of the film session.</summary>
        public string OwnerID
        {
            get { return _ownerID; }
            set { _ownerID = value; }
        }

        /// <summary>Type of image display format.</summary>
        /// <remarks>
        /// Enumerated Values:
        /// <list type="bullet">
        /// <item>
        ///   <term>STANDARD\C,R</term>
        ///   <description>film contains equal size rectangular image boxes with R rows of image 
        ///   boxes and C columns of image boxes; C and R are integers.</description>
        /// </item>
        /// <item>
        ///   <term>ROW\R1,R2,R3, etc.</term>
        ///   <description>film contains rows with equal size rectangular image boxes with R1 
        ///   image boxes in the first row, R2 image boxes in second row, R3 image boxes in third 
        ///   row, etc.; R1, R2, R3, etc. are integers.</description>
        /// </item>
        /// <item>
        ///   <term>COL\C1,C2,C3, etc.</term>
        ///   <description>film contains columns with equal size rectangular image boxes with C1 
        ///   image boxes in the first column, C2 image boxes in second column, C3 image boxes in 
        ///   third column, etc.; C1, C2, C3, etc. are integers.</description>
        /// </item>
        /// <item>
        ///   <term>SLIDE</term>
        ///   <description>film contains 35mm slides; the number of slides for a particular film 
        ///   size is configuration dependent.</description>
        /// </item>
        /// <item>
        ///   <term>SUPERSLIDE</term>
        ///   <description>film contains 40mm slides; the number of slides for a particular film 
        ///   size is configuration dependent.</description>
        /// </item>
        /// <item>
        ///   <term>CUSTOM\i</term>
        ///   <description>film contains a customized ordering of rectangular image boxes; i identifies 
        ///   the image display format; the definition of the image display formats is defined in the 
        ///   Conformance Statement; i is an integer.</description>
        /// </item>
        /// </list>
        /// </remarks>
        public string ImageDisplayFormat
        {
            get { return _imageDisplayFormat; }
            set { _imageDisplayFormat = value; }
        }

        /// <summary>Film orientation.</summary>
        /// <remarks>
        /// Enumerated Values:
        /// <list type="bullet">
        /// <item>
        ///   <term>PORTRAIT</term>
        ///   <description>vertical film position</description>
        /// </item>
        /// <item>
        ///   <term>LANDSCAPE</term>
        ///   <description>horizontal film position</description>
        /// </item>
        /// </list>
        /// </remarks>
        public string FilmOrientation
        {
            get { return _filmOrientation; }
            set { _filmOrientation = value; }
        }

        /// <summary> Film size identification.</summary>
        /// <remarks>
        /// Defined Terms:
        /// <list type="bullet">
        /// <item><description>8INX10IN</description></item>
        /// <item><description>8_5INX11IN</description></item>
        /// <item><description>10INX12IN</description></item>
        /// <item><description>10INX14IN</description></item>
        /// <item><description>11INX14IN</description></item>
        /// <item><description>11INX17IN</description></item>
        /// <item><description>14INX14IN</description></item>
        /// <item><description>14INX17IN</description></item>
        /// <item><description>24CMX24CM</description></item>
        /// <item><description>24CMX30CM</description></item>
        /// <item><description>A4</description></item>
        /// <item><description>A3</description></item>
        /// </list>
        /// 
        /// Notes:
        /// 10INX14IN corresponds with 25.7CMX36.4CM
        /// A4 corresponds with 210 x 297 millimeters
        /// A3 corresponds with 297 x 420 millimeters
        /// </remarks>
        public string FilmSizeID
        {
            get { return _filmSizeID; }
            set { _filmSizeID = value; }
        }

        /// <summary>Interpolation type by which the printer magnifies or decimates the image 
        /// in order to fit the image in the image box on film.</summary>
        /// <remarks>
        /// Defined Terms:
        /// <list type="bullet">
        /// <item><description>REPLICATE</description></item>
        /// <item><description>BILINEAR</description></item>
        /// <item><description>CUBIC</description></item>
        /// <item><description>NONE</description></item>
        /// </list>
        /// </remarks>
        public string MagnificationType
        {
            get { return _magnificationType; }
            set { _magnificationType = value; }
        }

        /// <summary>Maximum density of the images on the film, expressed in hundredths of 
        /// OD. If Max Density is higher than maximum printer density than Max Density is set 
        /// to maximum printer density.</summary>
        public ushort MaxDensity
        {
            get { return _maxDensity; }
            set { _maxDensity = value; }
        }

        /// <summary>Character string that contains either the ID of the printer configuration 
        /// table that contains a set of values for implementation specific print parameters 
        /// (e.g. perception LUT related parameters) or one or more configuration data values, 
        /// encoded as characters. If there are multiple configuration data values encoded in 
        /// the string, they shall be separated by backslashes. The definition of values shall 
        /// be contained in the SCP's Conformance Statement.</summary>
        /// <remarks>
        /// Defined Terms:
        /// <list type="">
        /// <item>
        ///   <term>CS000-CS999</term>
        ///   <description>Implementation specific curve type.</description></item>
        /// </list>
        /// 
        /// Note: It is recommended that for SCPs, CS000 represent the lowest contrast and CS999 
        /// the highest contrast levels available.
        /// </remarks>
        public string ConfigurationInformation
        {
            get { return _configurationInformation; }
            set { _configurationInformation = value; }
        }

        /// <summary>Identification of annotation display format. The definition of the annotation 
        /// display formats and the annotation box position sequence are defined in the Conformance 
        /// Statement.</summary>
        public string AnnotationDisplayFormatID
        {
            get { return _annotationDisplayFormatID; }
            set { _annotationDisplayFormatID = value; }
        }

        /// <summary>Further specifies the type of the interpolation function. Values 
        /// are defined in Conformance Statement.
        /// 
        /// Only valid for Magnification Type (2010,0060) = CUBIC</summary>
        public string SmoothingType
        {
            get { return _smoothingType; }
            set { _smoothingType = value; }
        }

        /// <summary>Density of the film areas surrounding and between images on the film.</summary>
        /// <remarks>
        /// Defined Terms:
        /// <list type="bullet">
        /// <item><description>BLACK</description></item>
        /// <item><description>WHITE</description></item>
        /// <item><description>i where i represents the desired density in hundredths of OD 
        /// (e.g. 150 corresponds with 1.5 OD)</description></item>
        /// </list>
        /// </remarks>
        public string BorderDensity
        {
            get { return _borderDensity; }
            set { _borderDensity = value; }
        }

        /// <summary>Density of the image box area on the film that contains no image.</summary>
        /// <remarks>
        /// Defined Terms:
        /// <list type="bullet">
        /// <item><description>BLACK</description></item>
        /// <item><description>WHITE</description></item>
        /// <item><description>i where i represents the desired density in hundredths of OD 
        /// (e.g. 150 corresponds with 1.5 OD)</description></item>
        /// </list>
        /// </remarks>
        public string EmptyImageDensity
        {
            get { return _emptyImageDensity; }
            set { _emptyImageDensity = value; }
        }

        /// <summary>Minimum density of the images on the film, expressed in hundredths of 
        /// OD. If Min Density is lower than minimum printer density than Min Density is set 
        /// to minimum printer density.</summary>
        public ushort MinDensity
        {
            get { return _minDensity; }
            set { _minDensity = value; }
        }

        /// <summary>Specifies whether a trim box shall be printed surrounding each image 
        /// on the film.</summary>
        /// <remarks>
        /// Enumerated Values:
        /// <list type="bullet">
        /// <item><description>YES</description></item>
        /// <item><description>NO</description></item>
        /// </list>
        /// </remarks>
        public string Trim
        {
            get { return _trim; }
            set { _trim = value; }
        }

        /// <summary>Luminance of lightbox illuminating a piece of transmissive film, or for 
        /// the case of reflective media, luminance obtainable from diffuse reflection of the 
        /// illumination present. Expressed as L0, in candelas per square meter (cd/m2).</summary>
        public ushort Illumination
        {
            get { return _illumination; }
            set { _illumination = value; }
        }

        /// <summary>For transmissive film, luminance contribution due to reflected ambient 
        /// light. Expressed as La, in candelas per square meter (cd/m2).</summary>
        public ushort ReflectedAmbientLight
        {
            get { return _reflectedAmbientLight; }
            set { _reflectedAmbientLight = value; }
        }

        /// <summary>Specifies the resolution at which images in this Film Box are to be printed.</summary>
        /// <remarks>
        /// Defined Terms:
        /// <list type="bullet">
        /// <item>
        ///   <term>STANDARD</term>
        ///   <description>approximately 4k x 5k printable pixels on a 14 x 17 inch film</description>
        /// </item>
        /// <item>
        ///   <term>HIGH</term>
        ///   <description>Approximately twice the resolution of STANDARD.</description>
        /// </item>
        /// </list>
        /// </remarks>
        public string RequestedResolutionID
        {
            get { return _requestedResolutionID; }
            set { _requestedResolutionID = value; }
        }
        #endregion

        #region Protected Overrides
        protected override void OnConnected()
        {
            DcmAssociate associate = new DcmAssociate();

            byte pcid = associate.AddPresentationContext(DicomUID.PrinterSOPClass);
            associate.AddTransferSyntax(pcid, DicomTransferSyntax.ExplicitVRLittleEndian);
            associate.AddTransferSyntax(pcid, DicomTransferSyntax.ImplicitVRLittleEndian);

            pcid = associate.AddPresentationContext(DicomUID.BasicGrayscalePrintManagementMetaSOPClass);
            associate.AddTransferSyntax(pcid, DicomTransferSyntax.ExplicitVRLittleEndian);
            associate.AddTransferSyntax(pcid, DicomTransferSyntax.ImplicitVRLittleEndian);

            pcid = associate.AddPresentationContext(DicomUID.BasicColorPrintManagementMetaSOPClass);
            associate.AddTransferSyntax(pcid, DicomTransferSyntax.ExplicitVRLittleEndian);
            associate.AddTransferSyntax(pcid, DicomTransferSyntax.ImplicitVRLittleEndian);

            associate.CalledAE = CalledAE;
            associate.CallingAE = CallingAE;
            associate.MaximumPduLength = MaxPduSize;

            SendAssociateRequest(associate);
        }

        protected override void OnReceiveAssociateAccept(DcmAssociate association)
        {
            PerformPrinterQueryOrRelease();
        }

        private void PerformPrinterQueryOrRelease()
        {
            byte pcidPrinterSOPClass, pcidBasicGrayscalePrintManagementMetaSOPClass, 
                pcidBasicColorPrintManagementMetaSOPClass;

            if (_files.Count > 0)
            {
                pcidPrinterSOPClass = Associate.FindAbstractSyntax(DicomUID.PrinterSOPClass);
                pcidBasicGrayscalePrintManagementMetaSOPClass = Associate.FindAbstractSyntax(DicomUID.BasicGrayscalePrintManagementMetaSOPClass);
                pcidBasicColorPrintManagementMetaSOPClass = Associate.FindAbstractSyntax(DicomUID.BasicColorPrintManagementMetaSOPClass);

                if (Associate.GetPresentationContextResult(pcidPrinterSOPClass) == DcmPresContextResult.Accept)
                {
                    if (Associate.GetPresentationContextResult(pcidBasicGrayscalePrintManagementMetaSOPClass) == DcmPresContextResult.Accept)
                    {
                        _supportsGrayscalePrinting = true;
                        _supportsPrinting = true;
                    }
                    if (Associate.GetPresentationContextResult(pcidBasicColorPrintManagementMetaSOPClass) == DcmPresContextResult.Accept)
                    {
                        _supportsColorPrinting = true;
                        _supportsPrinting = true;
                    }
                }
                else
                {
                    _supportsPrinting = false;
                }

                if (_supportsPrinting == false)
                {
                    Log.Info("{0} -> Printing not supported", LogID);
                    SendReleaseRequest();
                    return;
                }

                if (_queryPrinterStatus == true)
                {
                    List<DicomTag> attributes = new List<DicomTag>();
                    attributes.Add(DicomTags.PrinterStatus);
                    attributes.Add(DicomTags.PrinterName);
                    attributes.Add(DicomTags.Manufacturer);
                    attributes.Add(DicomTags.ManufacturersModelName);
                    attributes.Add(DicomTags.DeviceSerialNumber);
                    attributes.Add(DicomTags.SoftwareVersions);
                    attributes.Add(DicomTags.DateOfLastCalibration);
                    attributes.Add(DicomTags.TimeOfLastCalibration);

                    SendNGetRequest(pcidPrinterSOPClass, 1, DicomUID.PrinterSOPClass, DicomUID.PrinterSOPInstance, attributes.ToArray());
                }
                else
                {
                    CreateFilmSession();
                }
            }
            else
            {
                SendReleaseRequest();
            }
        }

        protected override void OnReceiveNGetResponse(byte presentationID, ushort messageIdRespondedTo, DicomUID affectedClass, DicomUID affectedInstance,
            DcmDataset dataset, DcmStatus status)
        {
            if (OnPrintGetPrinterStatusResponse != null)
            {
                DcmPrinterStatus printerstatus = new DcmPrinterStatus(dataset);
                _acceptedPrinterStatus = OnPrintGetPrinterStatusResponse(printerstatus, status);
            }

            if (_acceptedPrinterStatus == false)
            {
                SendReleaseRequest();
                return;
            }

            CreateFilmSession();
        }

        protected override void OnReceiveNCreateResponse(byte presentationID, ushort messageIdRespondedTo, DicomUID affectedClass, DicomUID affectedInstance,
            DcmDataset dataset, DcmStatus status)
        {
            if (_filmSession != null)
            {
                if (affectedClass == DicomUID.BasicFilmSessionSOPClass)
                {
                    if (status == DcmStatus.Success)
                    {
                        int filmBoxesCount = CalculateRequiredImageBoxes();
                        if (filmBoxesCount == 0)
                        {
                            SendReleaseRequest();
                            return;
                        }

                        for (int i = 0; i < filmBoxesCount; i++)
                        {
                            DicomUID uid = DicomUID.Generate();
                            DcmDataset filmBoxDataset = new DcmDataset(DicomTransferSyntax.ImplicitVRLittleEndian);
                            DcmFilmBox filmBox = _filmSession.CreateFilmBox(uid, filmBoxDataset.Clone());

                            filmBox.AnnotationDisplayFormatID = _annotationDisplayFormatID;
                            filmBox.BorderDensity = _borderDensity;
                            filmBox.ConfigurationInformation = _configurationInformation;
                            filmBox.EmptyImageDensity = _emptyImageDensity;
                            filmBox.FilmOrientation = _filmOrientation;
                            filmBox.FilmSizeID = _filmSizeID;
                            filmBox.Illumination = _illumination;
                            filmBox.ImageDisplayFormat = _imageDisplayFormat;
                            filmBox.MagnificationType = _magnificationType;
                            filmBox.MaxDensity = _maxDensity;
                            filmBox.MinDensity = _minDensity;
                            filmBox.ReflectedAmbientLight = _reflectedAmbientLight;
                            filmBox.RequestedResolutionID = _requestedResolutionID;
                            filmBox.SmoothingType = _smoothingType;
                            filmBox.Trim = _trim;

                            byte pcid = Associate.FindAbstractSyntax(DicomUID.BasicGrayscalePrintManagementMetaSOPClass);
                            SendNCreateRequest(pcid, NextMessageID(), DicomUID.BasicFilmBoxSOPClass, filmBox.SOPInstanceUID, filmBox.Dataset);
                        }
                        return;
                    }
                }

                if (affectedClass == DicomUID.BasicFilmBoxSOPClass)
                {
                    if (status == DcmStatus.Success)
                    {
                        DcmFilmBox filmBox = _filmSession.FindFilmBox(affectedInstance);
                        int filmBoxIndex = _filmSession.BasicFilmBoxes.IndexOf(filmBox);
                        if (filmBox != null)
                        {                         
                            DcmItemSequence referencedImageBoxSequenceList = null;
                            referencedImageBoxSequenceList = dataset.GetSQ(DicomTags.ReferencedImageBoxSequence);
                            if (referencedImageBoxSequenceList != null)
                            {
                                foreach (DcmItemSequenceItem item in referencedImageBoxSequenceList.SequenceItems)
                                {
                                    DicomUID referencedSOPInstanceUID = item.Dataset.GetUID(DicomTags.ReferencedSOPInstanceUID);
                                    if (referencedSOPInstanceUID != null)
                                    {
                                        DcmImageBox imageBox = new DcmImageBox(filmBox, DcmImageBox.GraySOPClassUID, referencedSOPInstanceUID);
                                        filmBox.BasicImageBoxes.Add(imageBox);
                                    }
                                }
                            }

                            _pendingImageBoxResponses.Clear();
                            if (filmBox.BasicImageBoxes.Count > 0)
                            {
                                int imageBoxIndex = 0;
                                int imagesPerFilmbox = CalculateImagesPreFilmBox();
                                foreach (DcmImageBox imageBox in filmBox.BasicImageBoxes)
                                {
                                    if (imagesPerFilmbox * filmBoxIndex + imageBoxIndex < _files.Count)
                                    {
                                        UpdateImageBox(imageBox, _files[imagesPerFilmbox * filmBoxIndex + imageBoxIndex], imageBoxIndex);
                                    }
                                    _pendingImageBoxResponses.Add(imageBox);
                                    imageBoxIndex++;

                                    byte pcid = Associate.FindAbstractSyntax(DicomUID.PrinterSOPClass);
                                    SendNSetRequest(pcid, NextMessageID(), imageBox.SOPClassUID, imageBox.SOPInstanceUID, imageBox.Dataset);
                                }
                            }
                            return;
                        }
                    }               
                }
            }

            SendAbort(DcmAbortSource.ServiceUser, DcmAbortReason.NotSpecified);
        }

        protected override void OnReceiveNSetResponse(byte presentationID, ushort messageIdRespondedTo, DicomUID affectedClass, DicomUID affectedInstance,
            DcmDataset dataset, DcmStatus status)
        {
            if (_filmSession != null)
            {
                if (affectedClass == DicomUID.BasicColorImageBoxSOPClass ||
                    affectedClass == DicomUID.BasicGrayscaleImageBoxSOPClass)
                {
                    if (status == DcmStatus.Success)
                    {
                        DcmImageBox imageBox = _filmSession.FindImageBox(affectedInstance);
                        if (imageBox != null)
                        {
                            _pendingImageBoxResponses.Remove(imageBox);
                            if (_pendingImageBoxResponses.Count == 0)
                            {
                                byte pcid = Associate.FindAbstractSyntax(DicomUID.PrinterSOPClass);
                                SendNActionRequest(pcid, NextMessageID(), DicomUID.BasicFilmSessionSOPClass, _filmSession.SOPInstanceUID, 0x0001, null);                               
                            }
                            return;
                        }
                    }
                }
            }

            SendAbort(DcmAbortSource.ServiceUser, DcmAbortReason.NotSpecified);
        }

        protected override void OnReceiveNActionResponse(byte presentationID, ushort messageIdRespondedTo, DicomUID affectedClass, DicomUID affectedInstance,
            ushort actionTypeID, DcmDataset dataset, DcmStatus status)
        {
            if (_filmSession != null)
            {
                if (affectedClass == DicomUID.BasicFilmSessionSOPClass)
                {
                    if (status == DcmStatus.Success)
                    {
                        _pendingFilmBoxResponses.Clear();
                        byte pcid = Associate.FindAbstractSyntax(DicomUID.PrinterSOPClass);
                        foreach (DcmFilmBox filmBox in _filmSession.BasicFilmBoxes)
                        {
                            _pendingFilmBoxResponses.Add(filmBox);
                            SendNDeleteRequest(pcid, NextMessageID(), DicomUID.BasicFilmBoxSOPClass, filmBox.SOPInstanceUID);
                        }
                        return;
                    }
                    
                }
            }

            SendAbort(DcmAbortSource.ServiceUser, DcmAbortReason.NotSpecified);
        }

        protected override void OnReceiveNDeleteResponse(byte presentationID, ushort messageIdRespondedTo, DicomUID affectedClass, DicomUID affectedInstance, DcmStatus status)
        {
            if (_filmSession != null)
            {
                if (affectedClass == DicomUID.BasicFilmBoxSOPClass)
                {
                    if (status == DcmStatus.Success)
                    {
                        DcmFilmBox filmBox = _filmSession.FindFilmBox(affectedInstance);
                        if (filmBox != null)
                        {
                            _pendingFilmBoxResponses.Remove(filmBox);
                            if (_pendingFilmBoxResponses.Count == 0)
                            {
                                byte pcid = Associate.FindAbstractSyntax(DicomUID.PrinterSOPClass);
                                SendNDeleteRequest(pcid, NextMessageID(), DicomUID.BasicFilmSessionSOPClass, _filmSession.SOPInstanceUID);
                            }
                            return;
                        }
                    }
                }

                if (affectedClass == DicomUID.BasicFilmSessionSOPClass)
                {
                    if (status == DcmStatus.Success)
                    {
                        SendReleaseRequest();
                        return;
                    }
                }
            }

            SendAbort(DcmAbortSource.ServiceUser, DcmAbortReason.NotSpecified);
        }
        #endregion

        #region Private Methods
        private void CreateFilmSession()
        {
            DcmDataset fimSessionDataset = new DcmDataset(DicomTransferSyntax.ImplicitVRLittleEndian);
            _filmSession = new DcmFilmSession(DcmFilmSession.SOPClassUID,
                                                            DicomUID.Generate(), fimSessionDataset.Clone());
            _filmSession.FilmDestination = _filmDestination;
            _filmSession.FilmSessionLabel = _filmSessionLabel;
            _filmSession.MediumType = _mediumType;
            _filmSession.NumberOfCopies = _numberOfCopies;
            _filmSession.OwnerID = _ownerID;
            _filmSession.PrintPriority = _printPriority;

            byte pcid = Associate.FindAbstractSyntax(DicomUID.BasicGrayscalePrintManagementMetaSOPClass);
            SendNCreateRequest(pcid, NextMessageID(), DcmFilmSession.SOPClassUID, _filmSession.SOPInstanceUID, _filmSession.Dataset);
        }

        private int CalculateImagesPreFilmBox()
        {
            int cols = 0, rows = 0;

            if (String.IsNullOrEmpty(_imageDisplayFormat))
                return 0;

            string[] parts = _imageDisplayFormat.Split('\\');
            if (parts[0] == "STANDARD" && parts.Length == 2)
            {
                parts = parts[1].Split(',');
                if (parts.Length == 2)
                {
                    try
                    {
                        cols = int.Parse(parts[0]);
                        rows = int.Parse(parts[1]);
                    }
                    catch (Exception) { }
                }
            }

            return cols * rows;
        }

        private int CalculateRequiredImageBoxes()
        {
            int imageCount = _files.Count;

            if (imageCount == 0)
                return 0;

            int imagesPerFilmbox = CalculateImagesPreFilmBox();
            if (imagesPerFilmbox == 0)
                return 0;

            return ((imageCount % imagesPerFilmbox) != 0) ? imageCount / imagesPerFilmbox + 1 : imageCount / imagesPerFilmbox;
        }

        private void UpdateImageBox(DcmImageBox imageBox, String filename, int index)
        {
            try
            {
                DicomFileFormat ff = new DicomFileFormat();
                ff.Load(filename, DicomReadOptions.DefaultWithoutDeferredLoading);
                if (ff.Dataset != null)
                {
                    ff.Dataset.ChangeTransferSyntax(DicomTransferSyntax.ImplicitVRLittleEndian, null);
                    
                    DcmPixelData pixelData = new DcmPixelData(ff.Dataset);
                    PhotometricInterpretation pi = PhotometricInterpretation.Lookup(pixelData.PhotometricInterpretation);

                    // Grayscale only printer?
                    if (pi.IsColor == true && _supportsColorPrinting == false)
                    {
                        pixelData.Unload();
                        return;
                    }

                    // Color only printer?
                    if (pi.IsColor == false && _supportsGrayscalePrinting == false)
                    {
                        pixelData.Unload();
                        return;
                    }

                    DicomUID imageBoxSOPClassUID = null;
                    DcmItemSequence seq = null;
                    DcmItemSequenceItem item = new DcmItemSequenceItem();
                    pixelData.UpdateDataset(item.Dataset);
                    
                    if (pi.IsColor == true)
                    {
                        imageBoxSOPClassUID = DicomUID.BasicColorImageBoxSOPClass;
                        seq = new DcmItemSequence(DicomTags.BasicColorImageSequence);
                    }
                    else
                    {
                        imageBoxSOPClassUID = DicomUID.BasicGrayscaleImageBoxSOPClass;
                        seq = new DcmItemSequence(DicomTags.BasicGrayscaleImageSequence);
                    }
                    seq.AddSequenceItem(item);
                    imageBox.Dataset.AddItem(seq);

                    pixelData.Unload();

                    imageBox.UpdateImageBox(imageBoxSOPClassUID);
                    imageBox.ImageBoxPosition = (ushort)index;
                }
            }
            catch (Exception)
            {
            }
        }
        #endregion
    }
}
