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
using Dicom.Data;

namespace Dicom.Network.Client
{
    public class DcmImageBox
    {
        #region Private Members
        private DcmFilmBox _filmBox;
        private DicomUID _sopClass;
        private DicomUID _sopInstance;
        private DcmDataset _dataset;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes new Basic Image Box
        /// </summary>
        /// <param name="filmBox">Basic Film Box</param>
        /// <param name="sopClass">SOP Class UID</param>
        /// <param name="sopInstance">SOP Instance UID</param>
        public DcmImageBox(DcmFilmBox filmBox, DicomUID sopClass, DicomUID sopInstance)
        {
            _filmBox = filmBox;
            _sopClass = sopClass;
            _sopInstance = sopInstance;
            _dataset = new DcmDataset(DicomTransferSyntax.ImplicitVRLittleEndian);
        }

        /// <summary>
        /// Initializes new Basic Image Box
        /// </summary>
        /// <param name="filmBox">Basic Film Box</param>
        /// <param name="sopClass">SOP Class UID</param>
        /// <param name="sopInstance">SOP Instance UID</param>
        /// <param name="dataset">Dataset</param>
        public DcmImageBox(DcmFilmBox filmBox, DicomUID sopClass, DicomUID sopInstance, DcmDataset dataset)
        {
            _filmBox = filmBox;
            _sopClass = sopClass;
            _sopInstance = sopInstance;
            _dataset = dataset;
        }
        #endregion

        #region Public Properties
        /// <summary>Basic Color Image Box SOP</summary>
        public static readonly DicomUID ColorSOPClassUID = DicomUID.BasicColorImageBoxSOPClass;

        /// <summary>Basic Grayscale Image Box SOP</summary>
        public static readonly DicomUID GraySOPClassUID = DicomUID.BasicGrayscaleImageBoxSOPClass;

        /// <summary>SOP Class UID</summary>
        public DicomUID SOPClassUID
        {
            get { return _sopClass; }
        }

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

        /// <summary>Color or Grayscale Basic Image Sequence</summary>
        public DcmDataset ImageSequence
        {
            get
            {
                DcmItemSequence sq = null;
                if (_sopClass == ColorSOPClassUID)
                    sq = _dataset.GetSQ(DicomTags.BasicColorImageSequence);
                else
                    sq = _dataset.GetSQ(DicomTags.BasicGrayscaleImageSequence);


                if (sq != null && sq.SequenceItems.Count > 0)
                    return sq.SequenceItems[0].Dataset;

                return null;
            }
        }

        /// <summary>The position of the image on the film, based on Image Display 
        /// Format (2010,0010). See C.13.5.1 for specification.</summary>
        public ushort ImageBoxPosition
        {
            get { return _dataset.GetUInt16(DicomTags.ImageBoxPosition, 1); }
            set { _dataset.AddElementWithValue(DicomTags.ImageBoxPosition, value); }
        }

        /// <summary>Specifies whether minimum pixel values (after VOI LUT transformation) 
        /// are to printed black or white.</summary>
        /// <remarks>
        /// Enumerated Values:
        /// <list type="bullet"></list>
        /// <item>
        ///   <term>NORMAL</term>
        ///   <description>pixels shall be printed as specified by the Photometric Interpretation (0028,0004)</description>
        /// </item>
        /// <item>
        ///   <term>REVERSE</term>
        ///   <description>pixels shall be printed with the opposite polarity as specified by the Photometric 
        ///   Interpretation (0028,0004)</description>
        /// </item>
        /// 
        /// If Polarity (2020,0020) is not specified by the SCU, the SCP shall print with NORMAL polarity.
        /// </remarks>
        public string Polarity
        {
            get { return _dataset.GetString(DicomTags.Polarity, "NORMAL"); }
            set { _dataset.AddElementWithValue(DicomTags.Polarity, value); }
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
            get { return _dataset.GetString(DicomTags.MagnificationType, _filmBox.MagnificationType); }
            set { _dataset.AddElementWithValue(DicomTags.MagnificationType, value); }
        }

        /// <summary>Further specifies the type of the interpolation function. Values 
        /// are defined in Conformance Statement.
        /// 
        /// Only valid for Magnification Type (2010,0060) = CUBIC</summary>
        public string SmoothingType
        {
            get { return _dataset.GetString(DicomTags.SmoothingType, _filmBox.SmoothingType); }
            set { _dataset.AddElementWithValue(DicomTags.SmoothingType, value); }
        }

        /// <summary>Minimum density of the images on the film, expressed in hundredths of 
        /// OD. If Min Density is lower than minimum printer density than Min Density is set 
        /// to minimum printer density.</summary>
        public ushort MinDensity
        {
            get { return _dataset.GetUInt16(DicomTags.MinDensity, _filmBox.MinDensity); }
            set { _dataset.AddElementWithValue(DicomTags.MinDensity, value); }
        }

        /// <summary>Maximum density of the images on the film, expressed in hundredths of 
        /// OD. If Max Density is higher than maximum printer density than Max Density is set 
        /// to maximum printer density.</summary>
        public ushort MaxDensity
        {
            get { return _dataset.GetUInt16(DicomTags.MaxDensity, _filmBox.MaxDensity); }
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
        ///   <description>Implementation specific curve type.</description>
        /// </item>
        /// </list>
        /// 
        /// Note: It is recommended that for SCPs, CS000 represent the lowest contrast and CS999 
        /// the highest contrast levels available.
        /// </remarks>
        public string ConfigurationInformation
        {
            get { return _dataset.GetString(DicomTags.ConfigurationInformation, _filmBox.ConfigurationInformation); }
            set { _dataset.AddElementWithValue(DicomTags.ConfigurationInformation, value); }
        }

        /// <summary>Width (x-dimension) in mm of the image to be printed. This value overrides 
        /// the size that corresponds with optimal filling of the Image Box.</summary>
        public double RequestedImageSize
        {
            get { return _dataset.GetDouble(DicomTags.RequestedImageSize, 0.0); }
            set { _dataset.AddElementWithValue(DicomTags.RequestedImageSize, value); }
        }

        /// <summary>Specifies whether image pixels are to be decimated or cropped if the image 
        /// rows or columns is greater than the available printable pixels in an Image Box.</summary>
        /// <remarks>
        /// Decimation  means that a magnification factor &lt;1 is applied to the image. The method 
        /// of decimation shall be that specified by Magnification Type (2010,0060) or the SCP 
        /// default if not specified.
        /// 
        /// Cropping means that some image rows and/or columns are deleted before printing.
        /// 
        /// Enumerated Values:
        /// <list type="bullet">
        /// <item>
        ///   <term>DECIMATE</term>
        ///   <description>a magnification factor &lt;1 to be applied to the image.</description>
        /// </item>
        /// <item>
        ///   <term>CROP</term>
        ///   <description>some image rows and/or columns are to be deleted before printing. The 
        ///   specific algorithm for cropping shall be described in the SCP Conformance Statement.</description>
        /// </item>
        /// <item>
        ///   <term>FAIL</term>
        ///   <description>the SCP shall not crop or decimate</description>
        /// </item>
        /// </list>
        /// </remarks>
        public string RequestedDecimateCropBehavior
        {
            get { return _dataset.GetString(DicomTags.RequestedDecimateCropBehavior, "DECIMATE"); }
            set { _dataset.AddElementWithValue(DicomTags.RequestedDecimateCropBehavior, value); }
        }
        #endregion

        #region Public Methods
        public DcmImageBox Clone()
        {
            return new DcmImageBox(_filmBox, SOPClassUID, SOPInstanceUID, Dataset.Clone());
        }

        public void UpdateImageBox(DicomUID sopClassUID)
        {
            _sopClass = sopClassUID;
        }
        #endregion
    }
}
