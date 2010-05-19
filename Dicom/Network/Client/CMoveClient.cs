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
using System.Text;

using Dicom.Data;
using Dicom.Network;

namespace Dicom.Network.Client {
	public class CMoveQuery {
		#region Private Members
		private DcmQueryRetrieveLevel _queryLevel;
		private string _patientId;
		private string _studyUid;
		private string _seriesUid;
		private string _instanceUid;
		private object _userState;
		#endregion

		#region Public Constructors
		public CMoveQuery() {
			_queryLevel = DcmQueryRetrieveLevel.Study;
		}

		public CMoveQuery(DcmQueryRetrieveLevel level) {
			_queryLevel = level;
		}

		public CMoveQuery(DcmQueryRetrieveLevel level, string uid) {
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

		public CMoveQuery(string studyUid) {
			_queryLevel = DcmQueryRetrieveLevel.Study;
			_studyUid = studyUid;
		}

		public CMoveQuery(string studyUid, string seriesUid) {
			_queryLevel = DcmQueryRetrieveLevel.Series;
			_studyUid = studyUid;
			_seriesUid = seriesUid;
		}

		public CMoveQuery(string studyUid, string seriesUid, string instUid) {
			_queryLevel = DcmQueryRetrieveLevel.Image;
			_studyUid = studyUid;
			_seriesUid = seriesUid;
			_instanceUid = instUid;
		}
		#endregion

		#region Public Properties
		public DcmQueryRetrieveLevel QueryRetrieveLevel {
			get { return _queryLevel; }
			set { _queryLevel = value; }
		}

		public string PatientID {
			get { return _patientId; }
			set { _patientId = value; }
		}

		public string StudyInstanceUID {
			get { return _studyUid; }
			set {
				_studyUid = value;
				if (_queryLevel < DcmQueryRetrieveLevel.Study)
					_queryLevel = DcmQueryRetrieveLevel.Study;
			}
		}

		public string SeriesInstanceUID {
			get { return _seriesUid; }
			set {
				_seriesUid = value;
				if (_queryLevel < DcmQueryRetrieveLevel.Series)
					_queryLevel = DcmQueryRetrieveLevel.Series;
			}
		}

		public string SOPInstanceUID {
			get { return _instanceUid; }
			set {
				_instanceUid = value;
				if (_queryLevel < DcmQueryRetrieveLevel.Image)
					_queryLevel = DcmQueryRetrieveLevel.Image;
			}
		}

		public object UserState {
			get { return _userState; }
			set { _userState = value; }
		}
		#endregion
	}

	public delegate void CMoveResponseDelegate(CMoveQuery query, DcmDataset dataset, DcmStatus status, ushort remain, ushort complete, ushort warning, ushort failure);

	public sealed class CMoveClient : DcmClientBase  {
		#region Private Members
		private string _destAe;
		private DicomUID _moveSopClass;
		private Queue<CMoveQuery> _moveQueries;
		private CMoveQuery _current;
		#endregion

		#region Public Constructor
		public CMoveClient() : base() {
			LogID = "C-Move SCU";
			CallingAE = "MOVE_SCU";
			CalledAE = "MOVE_SCP";
			_moveSopClass = DicomUID.StudyRootQueryRetrieveInformationModelMOVE;
			_moveQueries = new Queue<CMoveQuery>();
			_current = null;
		}
		#endregion

		#region Public Properties
		public CMoveResponseDelegate OnCMoveResponse;

		public string DestinationAE {
			get { return _destAe; }
			set { _destAe = value; }
		}

		public DicomUID MoveSopClassUID {
			get { return _moveSopClass; }
			set { _moveSopClass = value; }
		}
		#endregion

		#region Public Members
		public void AddQuery(CMoveQuery query) {
			_moveQueries.Enqueue(query);
		}

		public void AddQuery(DcmQueryRetrieveLevel level, string instance) {
			AddQuery(new CMoveQuery(level, instance));
		}
		#endregion

		#region Protected Overrides
		protected override void OnConnected() {
			DcmAssociate associate = new DcmAssociate();

			byte pcid = associate.AddPresentationContext(_moveSopClass);
			associate.AddTransferSyntax(pcid, DicomTransferSyntax.ImplicitVRLittleEndian);

			associate.CalledAE = CalledAE;
			associate.CallingAE = CallingAE;
			associate.MaximumPduLength = MaxPduSize;

			SendAssociateRequest(associate);
		}

		private void PerformQueryOrRelease() {
			if (_moveQueries.Count > 0) {
				byte pcid = Associate.FindAbstractSyntax(MoveSopClassUID);
				if (Associate.GetPresentationContextResult(pcid) == DcmPresContextResult.Accept) {
					CMoveQuery query = _moveQueries.Dequeue();
					DcmDataset dataset = new DcmDataset(Associate.GetAcceptedTransferSyntax(pcid));
					switch (query.QueryRetrieveLevel) {
					case DcmQueryRetrieveLevel.Patient:
						dataset.AddElementWithValue(DicomTags.QueryRetrieveLevel, "PATIENT");
						dataset.AddElementWithValue(DicomTags.PatientID, query.PatientID);
						break;
					case DcmQueryRetrieveLevel.Study:
						dataset.AddElementWithValue(DicomTags.QueryRetrieveLevel, "STUDY");
						dataset.AddElementWithValue(DicomTags.PatientID, query.PatientID);
						dataset.AddElementWithValue(DicomTags.StudyInstanceUID, query.StudyInstanceUID);
						break;
					case DcmQueryRetrieveLevel.Series:
						dataset.AddElementWithValue(DicomTags.QueryRetrieveLevel, "SERIES");
						dataset.AddElementWithValue(DicomTags.PatientID, query.PatientID);
						dataset.AddElementWithValue(DicomTags.StudyInstanceUID, query.StudyInstanceUID);
						dataset.AddElementWithValue(DicomTags.SeriesInstanceUID, query.SeriesInstanceUID);
						break;
					case DcmQueryRetrieveLevel.Image:
						dataset.AddElementWithValue(DicomTags.QueryRetrieveLevel, "IMAGE");
						dataset.AddElementWithValue(DicomTags.PatientID, query.PatientID);
						dataset.AddElementWithValue(DicomTags.StudyInstanceUID, query.StudyInstanceUID);
						dataset.AddElementWithValue(DicomTags.SeriesInstanceUID, query.SeriesInstanceUID);
						dataset.AddElementWithValue(DicomTags.SOPInstanceUID, query.SOPInstanceUID);
						break;
					default:
						break;
					}
					_current = query;
					SendCMoveRequest(pcid, 1, DestinationAE, Priority, dataset);
				}
				else {
					Log.Info("{0} -> Presentation context rejected: {1}", LogID, Associate.GetPresentationContextResult(pcid));
					SendReleaseRequest();
				}
			}
			else {
				SendReleaseRequest();
			}
		}

		protected override void OnReceiveAssociateAccept(DcmAssociate association) {
			PerformQueryOrRelease();
		}

		protected override void OnReceiveCMoveResponse(byte presentationID, ushort messageID, DcmDataset dataset, 
			DcmStatus status, ushort remain, ushort complete, ushort warning, ushort failure) {
			if (OnCMoveResponse != null) {
				OnCMoveResponse(_current, dataset, status, remain, complete, warning, failure);
			}
			if (remain == 0 && status != DcmStatus.Pending) {
				PerformQueryOrRelease();
			}
		}
		#endregion
	}
}
