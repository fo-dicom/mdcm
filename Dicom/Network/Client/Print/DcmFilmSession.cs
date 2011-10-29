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
using Dicom.Data;

namespace Dicom.Network.Client
{
    public class DcmFilmSession
    {
        #region Private Members
        private DicomUID _sessionClass;
        private DicomUID _sopInstance;
        private DcmDataset _dataset;

        private List<DcmFilmBox> _boxes;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes new Basic Film Session
        /// </summary>
        /// <param name="sessionClass">Color or Grayscale Basic Print Management UID</param>
        public DcmFilmSession(DicomUID sessionClass)
        {
            _sessionClass = sessionClass;
            _dataset = new DcmDataset(DicomTransferSyntax.ImplicitVRLittleEndian);
            _boxes = new List<DcmFilmBox>();
        }

        /// <summary>
        /// Initializes new Basic Film Session
        /// </summary>
        /// <param name="sessionClass">Color or Grayscale Basic Print Management UID</param>
        /// <param name="sopInstance">SOP Instance UID</param>
        /// <param name="dataset">Dataset</param>
        public DcmFilmSession(DicomUID sessionClass, DicomUID sopInstance, DcmDataset dataset)
        {
            _sessionClass = sessionClass;
            _sopInstance = sopInstance;
            _dataset = dataset;
            _boxes = new List<DcmFilmBox>();

            if (_sopInstance == null || _sopInstance.UID == String.Empty)
                _sopInstance = DicomUID.Generate();
        }
        #endregion

        #region Public Properties
        /// <summary>Basic Film Box SOP</summary>
        public static readonly DicomUID SOPClassUID = DicomUID.BasicFilmSessionSOPClass;

        /// <summary>
        /// Color or Grayscale Basic Print Management UID
        /// </summary>
        public DicomUID SessionClassUID
        {
            get { return _sessionClass; }
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

        /// <summary>Basic Film Boxes</summary>
        public List<DcmFilmBox> BasicFilmBoxes
        {
            get { return _boxes; }
        }

        /// <summary>Number of copies to be printed for each film of the film session.</summary>
        public int NumberOfCopies
        {
            get { return _dataset.GetInt32(DicomTags.NumberOfCopies, 1); }
            set { _dataset.AddElementWithValue(DicomTags.NumberOfCopies, value); }
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
            get { return _dataset.GetString(DicomTags.PrintPriority, null); }
            set { _dataset.AddElementWithValue(DicomTags.PrintPriority, value); }
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
            get { return _dataset.GetString(DicomTags.MediumType, null); }
            set { _dataset.AddElementWithValue(DicomTags.MediumType, value); }
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
            get { return _dataset.GetString(DicomTags.FilmDestination, null); }
            set { _dataset.AddElementWithValue(DicomTags.FilmDestination, value); }
        }

        /// <summary>Human readable label that identifies the film session.</summary>
        public string FilmSessionLabel
        {
            get { return _dataset.GetString(DicomTags.FilmSessionLabel, null); }
            set { _dataset.AddElementWithValue(DicomTags.FilmSessionLabel, value); }
        }

        /// <summary>Amount of memory allocated for the film session.</summary>
        /// <remarks>Value is expressed in KB.</remarks>
        public int MemoryAllocation
        {
            get { return _dataset.GetInt32(DicomTags.MemoryAllocation, 0); }
            set { _dataset.AddElementWithValue(DicomTags.MemoryAllocation, value); }
        }

        /// <summary>Identification of the owner of the film session.</summary>
        public string OwnerID
        {
            get { return _dataset.GetString(DicomTags.OwnerID, null); }
            set { _dataset.AddElementWithValue(DicomTags.OwnerID, value); }
        }
        #endregion

        #region Public Methods
        public DcmFilmBox CreateFilmBox(DicomUID sopInstance, DcmDataset dataset)
        {
            DicomUID uid = sopInstance;
            if (uid == null || uid.UID == String.Empty)
                uid = DicomUID.Generate(SOPInstanceUID, _boxes.Count + 1);
            DcmFilmBox box = new DcmFilmBox(this, uid, dataset);
            _boxes.Add(box);
            return box;
        }

        public void DeleteFilmBox(DicomUID instUid)
        {
            for (int i = 0; i < _boxes.Count; i++)
            {
                if (_boxes[i].SOPInstanceUID.UID == instUid.UID)
                {
                    _boxes.RemoveAt(i);
                    return;
                }
            }
        }

        public DcmFilmBox FindFilmBox(DicomUID instUid)
        {
            foreach (DcmFilmBox box in _boxes)
            {
                if (box.SOPInstanceUID.UID == instUid.UID)
                    return box;
            }
            return null;
        }

        public DcmImageBox FindImageBox(DicomUID instUid)
        {
            foreach (DcmFilmBox filmBox in _boxes)
            {
                DcmImageBox imageBox = filmBox.FindImageBox(instUid);
                if (imageBox != null)
                    return imageBox;
            }
            return null;
        }

        public DcmFilmSession Clone()
        {
            return new DcmFilmSession(SessionClassUID, SOPInstanceUID, Dataset.Clone());
        }
        #endregion
    }
}
