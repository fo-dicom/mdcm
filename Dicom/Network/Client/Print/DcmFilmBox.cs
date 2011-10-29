// mDCM: A C# DICOM library
//
// Copyright (c) 2006-2008  Colby Dillion
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
//    Colby Dillion (colby.dillion@gmail.com)

using System;
using System.Collections.Generic;
using Dicom;
using Dicom.Data;

namespace Dicom.Network.Client
{
    public class DcmFilmBox
    {
        #region Private Members
        private DcmFilmSession _session;
        private DicomUID _sopInstance;
        private DcmDataset _dataset;
        private List<DcmImageBox> _boxes;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes new Basic Film Box
        /// </summary>
        /// <param name="session">Basic Film Session</param>
        /// <param name="sopInstance">SOP Instance UID</param>
        public DcmFilmBox(DcmFilmSession session, DicomUID sopInstance)
        {
            _session = session;
            _sopInstance = sopInstance;
            _dataset = new DcmDataset(DicomTransferSyntax.ImplicitVRLittleEndian);
            _boxes = new List<DcmImageBox>();
        }

        /// <summary>
        /// Initializes new Basic Film Box
        /// </summary>
        /// <param name="session">Basic Film Session</param>
        /// <param name="sopInstance">SOP Instance UID</param>
        /// <param name="dataset">Dataset</param>
        public DcmFilmBox(DcmFilmSession session, DicomUID sopInstance, DcmDataset dataset)
        {
            _session = session;
            _sopInstance = sopInstance;
            _dataset = dataset;
            _boxes = new List<DcmImageBox>();
        }
        #endregion

        #region Public Properties
        /// <summary>Basic Film Session SOP</summary>
        public static readonly DicomUID SOPClassUID = DicomUID.BasicFilmBoxSOPClass;

        /// <summary>SOP Instance UID</summary>
        public DicomUID SOPInstanceUID
        {
            get { return _sopInstance; }
        }

        /// <summary>Basic Film Session data</summary>
        public DcmDataset Dataset
        {
            get { return _dataset; }
        }

        /// <summary>Basic Image Boxes</summary>
        public List<DcmImageBox> BasicImageBoxes
        {
            get { return _boxes; }
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
            get { return _dataset.GetValueString(DicomTags.ImageDisplayFormat); }
            set { _dataset.AddElementWithValue(DicomTags.ImageDisplayFormat, value); }
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
            get { return _dataset.GetString(DicomTags.FilmOrientation, "PORTRAIT"); }
            set { _dataset.AddElementWithValue(DicomTags.FilmOrientation, value); }
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
            get { return _dataset.GetString(DicomTags.FilmSizeID, "8_5INX11IN"); }
            set { _dataset.AddElementWithValue(DicomTags.FilmSizeID, value); }
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
            get { return _dataset.GetString(DicomTags.MagnificationType, "BILINEAR"); }
            set { _dataset.AddElementWithValue(DicomTags.MagnificationType, value); }
        }

        /// <summary>Maximum density of the images on the film, expressed in hundredths of 
        /// OD. If Max Density is higher than maximum printer density than Max Density is set 
        /// to maximum printer density.</summary>
        public ushort MaxDensity
        {
            get { return _dataset.GetUInt16(DicomTags.MaxDensity, 0); }
            set { _dataset.AddElementWithValue(DicomTags.MaxDensity, value); }
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
            get { return _dataset.GetString(DicomTags.ConfigurationInformation, String.Empty); }
            set { _dataset.AddElementWithValue(DicomTags.ConfigurationInformation, value); }
        }

        /// <summary>Identification of annotation display format. The definition of the annotation 
        /// display formats and the annotation box position sequence are defined in the Conformance 
        /// Statement.</summary>
        public string AnnotationDisplayFormatID
        {
            get { return _dataset.GetString(DicomTags.AnnotationDisplayFormatID, String.Empty); }
            set { _dataset.AddElementWithValue(DicomTags.AnnotationDisplayFormatID, value); }
        }

        /// <summary>Further specifies the type of the interpolation function. Values 
        /// are defined in Conformance Statement.
        /// 
        /// Only valid for Magnification Type (2010,0060) = CUBIC</summary>
        public string SmoothingType
        {
            get { return _dataset.GetString(DicomTags.SmoothingType, String.Empty); }
            set { _dataset.AddElementWithValue(DicomTags.SmoothingType, value); }
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
            get { return _dataset.GetString(DicomTags.BorderDensity, "BLACK"); }
            set { _dataset.AddElementWithValue(DicomTags.BorderDensity, value); }
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
            get { return _dataset.GetString(DicomTags.EmptyImageDensity, "BLACK"); }
            set { _dataset.AddElementWithValue(DicomTags.EmptyImageDensity, value); }
        }

        /// <summary>Minimum density of the images on the film, expressed in hundredths of 
        /// OD. If Min Density is lower than minimum printer density than Min Density is set 
        /// to minimum printer density.</summary>
        public ushort MinDensity
        {
            get { return _dataset.GetUInt16(DicomTags.MinDensity, 0); }
            set { _dataset.AddElementWithValue(DicomTags.MinDensity, value); }
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
            get { return _dataset.GetString(DicomTags.Trim, "NO"); }
            set { _dataset.AddElementWithValue(DicomTags.Trim, value); }
        }

        /// <summary>Luminance of lightbox illuminating a piece of transmissive film, or for 
        /// the case of reflective media, luminance obtainable from diffuse reflection of the 
        /// illumination present. Expressed as L0, in candelas per square meter (cd/m2).</summary>
        public ushort Illumination
        {
            get { return _dataset.GetUInt16(DicomTags.Illumination, 0); }
            set { _dataset.AddElementWithValue(DicomTags.Illumination, value); }
        }

        /// <summary>For transmissive film, luminance contribution due to reflected ambient 
        /// light. Expressed as La, in candelas per square meter (cd/m2).</summary>
        public ushort ReflectedAmbientLight
        {
            get { return _dataset.GetUInt16(DicomTags.ReflectedAmbientLight, 0); }
            set { _dataset.AddElementWithValue(DicomTags.ReflectedAmbientLight, value); }
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
            get { return _dataset.GetString(DicomTags.RequestedResolutionID, "STANDARD"); }
            set { _dataset.AddElementWithValue(DicomTags.RequestedResolutionID, value); }
        }
        #endregion

        #region Public Methods
        public bool Initialize()
        {
            _dataset.AddItem(new DcmItemSequence(DicomTags.ReferencedImageBoxSequence));

            // Set Defaults
            FilmOrientation = FilmOrientation;
            FilmSizeID = FilmSizeID;
            MagnificationType = MagnificationType;
            MaxDensity = MaxDensity;
            BorderDensity = BorderDensity;
            EmptyImageDensity = EmptyImageDensity;
            MinDensity = MinDensity;
            Trim = Trim;
            RequestedResolutionID = RequestedResolutionID;

            string format = ImageDisplayFormat;

            if (String.IsNullOrEmpty(format))
            {
                Debug.Log.Error("No display format present in N-CREATE Basic Image Box dataset");
                return false;
            }

            string[] parts = format.Split('\\');

            if (parts[0] == "STANDARD" && parts.Length == 2)
            {
                parts = parts[1].Split(',');
                if (parts.Length == 2)
                {
                    try
                    {
                        int col = int.Parse(parts[0]);
                        int row = int.Parse(parts[1]);
                        for (int r = 0; r < row; r++)
                            for (int c = 0; c < col; c++)
                                CreateImageBox();
                        return true;
                    }
                    catch
                    {
                    }
                }

            }

            if ((parts[0] == "ROW" || parts[0] == "COL") && parts.Length == 2)
            {
                try
                {
                    parts = parts[1].Split(',');
                    foreach (string part in parts)
                    {
                        int count = int.Parse(part);
                        for (int i = 0; i < count; i++)
                            CreateImageBox();
                    }
                    return true;
                }
                catch
                {
                }
            }

            Debug.Log.Error("Unsupported image display format \"{0}\"", format);
            return false;
        }

        public DcmImageBox FindImageBox(DicomUID instUid)
        {
            foreach (DcmImageBox box in _boxes)
            {
                if (box.SOPInstanceUID.UID == instUid.UID)
                    return box;
            }
            return null;
        }

        public DcmFilmBox Clone()
        {
            DcmFilmBox box = new DcmFilmBox(_session, SOPInstanceUID, Dataset.Clone());
            foreach (DcmImageBox imageBox in BasicImageBoxes)
            {
                box.BasicImageBoxes.Add(imageBox.Clone());
            }
            return box;
        }
        #endregion

        #region Private Methods
        private void CreateImageBox()
        {
            DicomUID classUid = DicomUID.BasicGrayscaleImageBoxSOPClass;
            if (_session.SessionClassUID == DicomUID.BasicColorPrintManagementMetaSOPClass)
                classUid = DicomUID.BasicColorImageBoxSOPClass;

            DicomUID instUid = DicomUID.Generate(SOPInstanceUID, _boxes.Count + 1);

            DcmImageBox box = new DcmImageBox(this, classUid, instUid);
            box.ImageBoxPosition = (ushort)(_boxes.Count + 1);
            _boxes.Add(box);

            _dataset.AddReferenceSequenceItem(DicomTags.ReferencedImageBoxSequence, classUid, instUid);
        }
        #endregion
    }
}
