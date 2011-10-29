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
using System.Text;

using Dicom.Data;
using Dicom.Network;

namespace Dicom.Network.Client
{
    public class CGetQuery
    {
        #region Private Members
        private DcmQueryRetrieveLevel _queryLevel;
        private string _patientId;
        private string _studyUid;
        private string _seriesUid;
        private string _instanceUid;
        private object _userState;
        #endregion

        #region Public Constructors
        public CGetQuery()
        {
            _queryLevel = DcmQueryRetrieveLevel.Study;
        }

        public CGetQuery(DcmQueryRetrieveLevel level)
        {
            _queryLevel = level;
        }

        public CGetQuery(DcmQueryRetrieveLevel level, string uid)
        {
            _queryLevel = level;
            if (_queryLevel == DcmQueryRetrieveLevel.Patient)
                _patientId = uid;
            else if (_queryLevel == DcmQueryRetrieveLevel.Study)
                _studyUid = uid;
            else if (_queryLevel == DcmQueryRetrieveLevel.Series)
                _seriesUid = uid;
            else
                _instanceUid = uid;
        }

        public CGetQuery(string studyUid)
        {
            _queryLevel = DcmQueryRetrieveLevel.Study;
            _studyUid = studyUid;
        }

        public CGetQuery(string studyUid, string seriesUid)
        {
            _queryLevel = DcmQueryRetrieveLevel.Series;
            _studyUid = studyUid;
            _seriesUid = seriesUid;
        }

        public CGetQuery(string studyUid, string seriesUid, string instUid)
        {
            _queryLevel = DcmQueryRetrieveLevel.Image;
            _studyUid = studyUid;
            _seriesUid = seriesUid;
            _instanceUid = instUid;
        }
        #endregion

        #region Public Properties
        public DcmQueryRetrieveLevel QueryRetrieveLevel
        {
            get { return _queryLevel; }
            set { _queryLevel = value; }
        }

        public string PatientID
        {
            get { return _patientId; }
            set { _patientId = value; }
        }

        public string StudyInstanceUID
        {
            get { return _studyUid; }
            set
            {
                _studyUid = value;
                if (_queryLevel < DcmQueryRetrieveLevel.Study)
                    _queryLevel = DcmQueryRetrieveLevel.Study;
            }
        }

        public string SeriesInstanceUID
        {
            get { return _seriesUid; }
            set
            {
                _seriesUid = value;
                if (_queryLevel < DcmQueryRetrieveLevel.Series)
                    _queryLevel = DcmQueryRetrieveLevel.Series;
            }
        }

        public string SOPInstanceUID
        {
            get { return _instanceUid; }
            set
            {
                _instanceUid = value;
                if (_queryLevel < DcmQueryRetrieveLevel.Image)
                    _queryLevel = DcmQueryRetrieveLevel.Image;
            }
        }

        public DcmDataset ToDataset()
        {
            DcmDataset dataset = new DcmDataset();
            switch (QueryRetrieveLevel)
            {
                case DcmQueryRetrieveLevel.Patient:
                    dataset.AddElementWithValue(DicomTags.QueryRetrieveLevel, "PATIENT");
                    dataset.AddElementWithValue(DicomTags.PatientID, PatientID);
                    break;
                case DcmQueryRetrieveLevel.Study:
                    dataset.AddElementWithValue(DicomTags.QueryRetrieveLevel, "STUDY");
                    if (!String.IsNullOrEmpty(PatientID))
                        dataset.AddElementWithValue(DicomTags.PatientID, PatientID);
                    dataset.AddElementWithValue(DicomTags.StudyInstanceUID, StudyInstanceUID);
                    break;
                case DcmQueryRetrieveLevel.Series:
                    dataset.AddElementWithValue(DicomTags.QueryRetrieveLevel, "SERIES");
                    if (!String.IsNullOrEmpty(PatientID))
                        dataset.AddElementWithValue(DicomTags.PatientID, PatientID);
                    if (!String.IsNullOrEmpty(StudyInstanceUID))
                        dataset.AddElementWithValue(DicomTags.StudyInstanceUID, StudyInstanceUID);
                    dataset.AddElementWithValue(DicomTags.SeriesInstanceUID, SeriesInstanceUID);
                    break;
                case DcmQueryRetrieveLevel.Image:
                    dataset.AddElementWithValue(DicomTags.QueryRetrieveLevel, "IMAGE");
                    if (!String.IsNullOrEmpty(PatientID))
                        dataset.AddElementWithValue(DicomTags.PatientID, PatientID);
                    if (!String.IsNullOrEmpty(StudyInstanceUID))
                        dataset.AddElementWithValue(DicomTags.StudyInstanceUID, StudyInstanceUID);
                    if (!String.IsNullOrEmpty(SeriesInstanceUID))
                        dataset.AddElementWithValue(DicomTags.SeriesInstanceUID, SeriesInstanceUID);
                    dataset.AddElementWithValue(DicomTags.SOPInstanceUID, SOPInstanceUID);
                    break;
                default:
                    break;
            }
            return dataset;
        }

        public object UserState
        {
            get { return _userState; }
            set { _userState = value; }
        }
        #endregion
    }

    public delegate void CGetResponseDelegate(CGetQuery query, DcmDataset dataset, DcmStatus status, ushort remain, ushort complete, ushort warning, ushort failure);
    public delegate DcmStatus CStoreRequestDelegate(byte presentationID, ushort messageID, DicomUID affectedInstance, DcmPriority priority, string moveAE, ushort moveMessageID, DcmDataset dataset, string fileName);

    public sealed class CGetClient : DcmClientBase
    {
        #region Private Members
        private DicomUID _getSopClass;
        private Queue<CGetQuery> _getQueries;
        private CGetQuery _current;
        #endregion

        #region Public Constructor
        public CGetClient()
            : base()
        {
            LogID = "C-Get SCU";
            CallingAE = "GET_SCU";
            CalledAE = "GET_SCP";
            _getSopClass = DicomUID.StudyRootQueryRetrieveInformationModelGET;
            _getQueries = new Queue<CGetQuery>();
            _current = null;
        }
        #endregion

        #region Public Properties
        public CGetResponseDelegate OnCGetResponse;
        public CStoreRequestDelegate OnCStoreRequest;

        public DicomUID GetSopClassUID
        {
            get { return _getSopClass; }
            set { _getSopClass = value; }
        }
        #endregion

        #region Public Members
        public void AddQuery(CGetQuery query)
        {
            _getQueries.Enqueue(query);
        }

        public void AddQuery(DcmQueryRetrieveLevel level, string instance)
        {
            AddQuery(new CGetQuery(level, instance));
        }
        #endregion

        #region Private Members

        #endregion

        #region Protected Overrides
        protected override void OnConnected()
        {
            DcmAssociate associate = new DcmAssociate();

            byte pcid = associate.AddPresentationContext(_getSopClass);
            associate.AddTransferSyntax(pcid, DicomTransferSyntax.ExplicitVRLittleEndian);
            associate.AddTransferSyntax(pcid, DicomTransferSyntax.ImplicitVRLittleEndian);

            AddStoragePresentationContexts(associate);

            associate.CalledAE = CalledAE;
            associate.CallingAE = CallingAE;
            associate.MaximumPduLength = MaxPduSize;

            SendAssociateRequest(associate);
        }

        private void AddStoragePresentationContexts(DcmAssociate associate)
        {
            foreach (DicomUID uid in DicomUID.Entries.Values)
            {
                if (uid.Description.Contains("Storage"))
                {
                    byte pcid = associate.AddPresentationContext(uid);
                    associate.AddTransferSyntax(pcid, DicomTransferSyntax.ExplicitVRLittleEndian);
                    associate.AddTransferSyntax(pcid, DicomTransferSyntax.ImplicitVRLittleEndian);
                }
            }
        }

        private void PerformQueryOrRelease()
        {
            if (_getQueries.Count > 0)
            {
                byte pcid = Associate.FindAbstractSyntax(GetSopClassUID);
                if (Associate.GetPresentationContextResult(pcid) == DcmPresContextResult.Accept)
                {
                    _current = _getQueries.Dequeue();
                    SendCGetRequest(pcid, 1, Priority, _current.ToDataset());
                }
                else
                {
                    Log.Info("{0} -> Presentation context rejected: {1}", LogID, Associate.GetPresentationContextResult(pcid));
                    SendReleaseRequest();
                }
            }
            else
            {
                SendReleaseRequest();
            }
        }

        protected override void OnReceiveAssociateAccept(DcmAssociate association)
        {
            PerformQueryOrRelease();
        }

        protected override void OnReceiveCGetResponse(byte presentationID, ushort messageID, DcmDataset dataset,
            DcmStatus status, ushort remain, ushort complete, ushort warning, ushort failure)
        {
            if (OnCGetResponse != null)
            {
                OnCGetResponse(_current, dataset, status, remain, complete, warning, failure);
            }
            if (remain == 0 && status != DcmStatus.Pending)
            {
                PerformQueryOrRelease();
            }
        }

        protected override void OnReceiveCStoreRequest(byte presentationID, ushort messageID, DicomUID affectedInstance,
            DcmPriority priority, string moveAE, ushort moveMessageID, DcmDataset dataset, string fileName)
        {
            DcmStatus status = DcmStatus.Success;

            if (OnCStoreRequest != null)
            {
                status = OnCStoreRequest(presentationID, messageID, affectedInstance, priority, moveAE, moveMessageID, dataset, fileName);
            }

			SendCStoreResponse(presentationID, messageID, affectedInstance, status);
        }
        #endregion
    }
}
