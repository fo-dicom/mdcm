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
	public abstract class CFindQuery {
		#region Private Members
		private DcmQueryRetrieveLevel _queryLevel;
		private object _userState;
		#endregion

		#region Public Properties
		public DcmQueryRetrieveLevel QueryRetrieveLevel {
			get { return _queryLevel; }
			set { _queryLevel = value; }
		}

		public object UserState {
			get { return _userState; }
			set { _userState = value; }
		}
		#endregion

		#region Public Members
		public DcmDataset ToDataset(DicomTransferSyntax ts) {
			DcmDataset dataset = new DcmDataset(ts);
			if (QueryRetrieveLevel != DcmQueryRetrieveLevel.Worklist)
				dataset.AddElementWithValue(DicomTags.QueryRetrieveLevel, QueryRetrieveLevel.ToString().ToUpper());
			dataset.SaveDicomFields(this);
			AdditionalMembers(dataset);
			return dataset;
		}

		protected virtual void AdditionalMembers(DcmDataset dataset) {
		}
		#endregion
	}

	public class CFindResponse {
		#region Protected Members
		protected DcmDataset _dataset;
		#endregion

		#region Public Properties
		public DcmDataset Dataset {
			get { return _dataset; }
		}
		#endregion

		#region Public Members
		public virtual void FromDataset(DcmDataset dataset) {
			_dataset = dataset;
			dataset.LoadDicomFields(this);
		}
		#endregion
	}

	public class CFindClientT<Tq, Tr> : DcmClientBase
		where Tq : CFindQuery
		where Tr : CFindResponse {
		#region Private Members
		private DicomUID _findSopClass;
		private Queue<Tq> _queries;
		private Tq _current;
		#endregion

		#region Public Constructor
		public CFindClientT() : base() {
			LogID = "C-Find SCU";
			CallingAE = "FIND_SCU";
			CalledAE = "FIND_SCP";
			_queries = new Queue<Tq>();
			_findSopClass = DicomUID.StudyRootQueryRetrieveInformationModelFIND;
			_current = null;
		}
		#endregion

		#region Public Properties
		public delegate void CFindResponseDelegate(Tq query, Tr result);
		public CFindResponseDelegate OnCFindResponse;

		public delegate void CFindCompleteDelegate(Tq query);
		public CFindCompleteDelegate OnCFindComplete;

		public DicomUID FindSopClassUID {
			get { return _findSopClass; }
			set { _findSopClass = value; }
		}

		public Queue<Tq> FindQueries {
			get { return _queries; }
			set { _queries = value; }
		}
		#endregion

		#region Public Members
		public void AddQuery(Tq query) {
			_queries.Enqueue(query);
		}
		#endregion

		#region Protected Overrides
		protected override void OnConnected() {
			DcmAssociate associate = new DcmAssociate();

			byte pcid = associate.AddPresentationContext(FindSopClassUID);
			associate.AddTransferSyntax(pcid, DicomTransferSyntax.ExplicitVRLittleEndian);
			associate.AddTransferSyntax(pcid, DicomTransferSyntax.ImplicitVRLittleEndian);

			associate.CalledAE = CalledAE;
			associate.CallingAE = CallingAE;
			associate.MaximumPduLength = MaxPduSize;

			SendAssociateRequest(associate);
		}

		private void PerformQueryOrRelease() {
			if (FindQueries.Count > 0) {
				Tq query = FindQueries.Dequeue();
				_current = query;
				byte pcid = Associate.FindAbstractSyntax(FindSopClassUID);
				if (Associate.GetPresentationContextResult(pcid) == DcmPresContextResult.Accept) {
					DcmDataset dataset = query.ToDataset(Associate.GetAcceptedTransferSyntax(pcid));
					SendCFindRequest(pcid, NextMessageID(), Priority, dataset);
				}
				else {
					Log.Info("{0} <- Presentation context rejected: {1}", LogID, Associate.GetPresentationContextResult(pcid));
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

		protected override void OnReceiveCFindResponse(byte presentationID, ushort messageID, DcmDataset dataset, DcmStatus status) {
			if (status.State != DcmState.Pending) {
				if (OnCFindComplete != null) {
					OnCFindComplete(_current);
				}
				PerformQueryOrRelease();
			}
			else if (dataset != null) {
				if (OnCFindResponse != null) {
					Tr result = Activator.CreateInstance<Tr>();
					result.FromDataset(dataset);
					OnCFindResponse(_current, result);
				}
			}
		}
		#endregion
	}

	#region Patient
	public sealed class CFindPatientQuery : CFindQuery {
		#region Private Members
		private string _patientId;
		private string _patientName;
		#endregion

		#region Public Constructors
		public CFindPatientQuery() {
			QueryRetrieveLevel = DcmQueryRetrieveLevel.Patient;
		}
		#endregion

		#region Public Properties
		[DicomField(DicomConstTags.PatientID, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string PatientID {
			get { return _patientId; }
			set { _patientId = value; }
		}

		[DicomField(DicomConstTags.PatientsName, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string PatientsName {
			get { return _patientName; }
			set { _patientName = value; }
		}
		#endregion
	}

	public sealed class CFindPatientResponse : CFindResponse {
		#region Private Members
		private string _patientId;
		private string _patientName;
		#endregion

		#region Public Properties
		[DicomField(DicomConstTags.PatientID, DefaultValue = DicomFieldDefault.Default)]
		public string PatientID {
			get { return _patientId; }
			set { _patientId = value; }
		}

		[DicomField(DicomConstTags.PatientsName, DefaultValue = DicomFieldDefault.Default)]
		public string PatientsName {
			get { return _patientName; }
			set { _patientName = value; }
		}
		#endregion
	}

	public sealed class CFindPatientClient : CFindClientT<CFindPatientQuery, CFindPatientResponse> {
		public CFindPatientClient() : base() {
		}
	}
	#endregion

	#region Study
	public sealed class CFindStudyQuery : CFindQuery {
		#region Private Members
		private string _patientId;
		private string _patientName;
		private DcmDateRange _studyDate;
		private DcmDateRange _studyTime;
		private string _studyId;
		private string _accessionNumber;
		private string _studyInstanceUid;
		private string _modalitiesInStudy;
		private string _studyDescription;
		private string _institutionName;
		#endregion

		#region Public Constructors
		public CFindStudyQuery() {
			QueryRetrieveLevel = DcmQueryRetrieveLevel.Study;
			_studyDate = new DcmDateRange();
		}
		#endregion

		#region Public Properties
		[DicomField(DicomConstTags.PatientID, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string PatientID {
			get { return _patientId; }
			set { _patientId = value; }
		}

		[DicomField(DicomConstTags.PatientsName, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string PatientsName {
			get { return _patientName; }
			set { _patientName = value; }
		}

		[DicomField(DicomConstTags.StudyDate, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public DcmDateRange StudyDate {
			get { return _studyDate; }
			set { _studyDate = value; }
		}

		[DicomField(DicomConstTags.StudyTime, DefaultValue = DicomFieldDefault.MinValue, CreateEmptyElement = true)]
		public DcmDateRange StudyTime {
			get { return _studyTime; }
			set { _studyTime = value; }
		}

		[DicomField(DicomConstTags.StudyID, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string StudyID {
			get { return _studyId; }
			set { _studyId = value; }
		}

		[DicomField(DicomConstTags.AccessionNumber, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string AccessionNumber {
			get { return _accessionNumber; }
			set { _accessionNumber = value; }
		}

		[DicomField(DicomConstTags.StudyInstanceUID, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string StudyInstanceUID {
			get { return _studyInstanceUid; }
			set { _studyInstanceUid = value; }
		}

		[DicomField(DicomConstTags.ModalitiesInStudy, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string ModalitiesInStudy {
			get { return _modalitiesInStudy; }
			set { _modalitiesInStudy = value; }
		}

		[DicomField(DicomConstTags.StudyDescription, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string StudyDescription {
			get { return _studyDescription; }
			set { _studyDescription = value; }
		}

		[DicomField(DicomConstTags.InstitutionName, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string InstitutionName {
			get { return _institutionName; }
			set { _institutionName = value; }
		}
		#endregion

		#region Protected Members
		protected override void AdditionalMembers(DcmDataset dataset) {
			dataset.AddElement(DicomTags.Modality, DicomVR.CS);
			dataset.AddElement(DicomTags.PatientsBirthDate, DicomVR.DA);
			dataset.AddElement(DicomTags.PatientsSex, DicomVR.CS);
			//dataset.AddElement(DicomTags.SpecificCharacterSet, DicomVR.CS);
			dataset.AddElement(DicomTags.NumberOfStudyRelatedSeries, DicomVR.IS);
			dataset.AddElement(DicomTags.NumberOfStudyRelatedInstances, DicomVR.IS);
		}
		#endregion
	}

	public sealed class CFindStudyResponse : CFindResponse {
		#region Private Members
		private string _patientId;
		private string _patientName;
		private DateTime _patientBirthDate;
		private string _patientSex;
		private string _studyId;
		private string _accessionNumber;
		private string _studyInstanceUid;
		private string _modality;
		private string _modalitiesInStudy;
		private string _studyDescription;
		private string _institutionName;
		private DateTime _studyDate;
		private DateTime _studyTime;
		private int _numberOfStudyRelatedSeries;
		private int _numberOfStudyRelatedInstances;
		#endregion

		#region Public Properties
		[DicomField(DicomConstTags.PatientID, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string PatientID {
			get { return _patientId; }
			set { _patientId = value; }
		}

		[DicomField(DicomConstTags.PatientsName, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string PatientsName {
			get { return _patientName; }
			set { _patientName = value; }
		}

		[DicomField(DicomConstTags.PatientsBirthDate, DefaultValue = DicomFieldDefault.MinValue, CreateEmptyElement = true)]
		public DateTime PatientsBirthDate {
			get { return _patientBirthDate; }
			set { _patientBirthDate = value; }
		}

		[DicomField(DicomConstTags.PatientsSex, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string PatientsSex {
			get { return _patientSex; }
			set { _patientSex = value; }
		}

		[DicomField(DicomConstTags.StudyDate, DefaultValue = DicomFieldDefault.MinValue, CreateEmptyElement = true)]
		public DateTime StudyDate {
			get { return _studyDate; }
			set { _studyDate = value; }
		}

		[DicomField(DicomConstTags.StudyTime, DefaultValue = DicomFieldDefault.MinValue, CreateEmptyElement = true)]
		public DateTime StudyTime {
			get { return _studyTime; }
			set { _studyTime = value; }
		}

		[DicomField(DicomConstTags.StudyID, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string StudyID {
			get { return _studyId; }
			set { _studyId = value; }
		}

		[DicomField(DicomConstTags.AccessionNumber, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string AccessionNumber {
			get { return _accessionNumber; }
			set { _accessionNumber = value; }
		}

		[DicomField(DicomConstTags.StudyInstanceUID, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string StudyInstanceUID {
			get { return _studyInstanceUid; }
			set { _studyInstanceUid = value; }
		}

		[DicomField(DicomConstTags.Modality, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string Modality {
			get { return _modality; }
			set { _modality = value; }
		}

		[DicomField(DicomConstTags.ModalitiesInStudy, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string ModalitiesInStudy {
			get { return _modalitiesInStudy; }
			set { _modalitiesInStudy = value; }
		}

		[DicomField(DicomConstTags.StudyDescription, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string StudyDescription {
			get { return _studyDescription; }
			set { _studyDescription = value; }
		}

		[DicomField(DicomConstTags.InstitutionName, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string InstitutionName {
			get { return _institutionName; }
			set { _institutionName = value; }
		}

		[DicomField(DicomConstTags.NumberOfStudyRelatedSeries, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public int NumberOfStudyRelatedSeries {
			get { return _numberOfStudyRelatedSeries; }
			set { _numberOfStudyRelatedSeries = value; }
		}

		[DicomField(DicomConstTags.NumberOfStudyRelatedInstances, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public int NumberOfStudyRelatedInstances {
			get { return _numberOfStudyRelatedInstances; }
			set { _numberOfStudyRelatedInstances = value; }
		}
		#endregion
	}

	public sealed class CFindStudyClient : CFindClientT<CFindStudyQuery, CFindStudyResponse> {
		public CFindStudyClient() : base() {
			FindSopClassUID = DicomUID.StudyRootQueryRetrieveInformationModelFIND;
		}
	}
	#endregion

	#region Worklist
	public sealed class CFindWorklistQuery : CFindQuery {
	#region Private Members
		private string _patientId;
		private string _patientName;
		private string _accessionNumber;
		private string _modality;
		private string _stationAe;
		private DcmDateRange _scheduledStartDate;
		private DcmDateRange _scheduledStartTime;
		private string _requestedProcedureID;
		#endregion

	#region Public Constructors
		public CFindWorklistQuery() {
			QueryRetrieveLevel = DcmQueryRetrieveLevel.Worklist;
			ScheduledProcedureStepStartDate = new DcmDateRange();
		}
		#endregion

	#region Public Properties
		[DicomField(0x0010, 0x0010, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string PatientsName {
			get { return _patientName; }
			set { _patientName = value; }
		}

		[DicomField(0x0010, 0x0020, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string PatientID {
			get { return _patientId; }
			set { _patientId = value; }
		}

		[DicomField(0x0008, 0x0050, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string AccessionNumber {
			get { return _accessionNumber; }
			set { _accessionNumber = value; }
		}

		//[DicomField(0x0008, 0x0060, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string Modality {
			get { return _modality; }
			set { _modality = value; }
		}

		public string ScheduledStationAE {
			get { return _stationAe; }
			set { _stationAe = value; }
		}

		//[DicomField(0x0040, 0x0002, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public DcmDateRange ScheduledProcedureStepStartDate {
			get { return _scheduledStartDate; }
			set { _scheduledStartDate = value; }
		}

		public DcmDateRange ScheduledProcedureStepStartTime {
			get { return _scheduledStartTime; }
			set { _scheduledStartTime = value; }
		}

		[DicomField(0x0040, 0x1001, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string RequestedProcedureID {
			get { return _requestedProcedureID; }
			set { _requestedProcedureID = value; }
		}
		#endregion

	#region Protected Members
		protected override void AdditionalMembers(DcmDataset dataset) {
			dataset.AddElement(DicomTags.PatientsBirthDate);
			dataset.AddElement(DicomTags.PatientsSex);
			dataset.AddElement(DicomTags.PatientsAge);
			dataset.AddElement(DicomTags.PatientsSize);
			dataset.AddElement(DicomTags.PatientsWeight);
			dataset.AddElement(DicomTags.MedicalAlerts);
			dataset.AddElement(DicomTags.PregnancyStatus);
			dataset.AddElement(DicomTags.Allergies);//*Contrast allergies??
			dataset.AddElement(DicomTags.PatientComments);
			dataset.AddElement(DicomTags.SpecialNeeds);//*
			dataset.AddElement(DicomTags.PatientState);//*
			dataset.AddElement(DicomTags.CurrentPatientLocation);//*
			dataset.AddElement(DicomTags.InstitutionName);
			dataset.AddElement(DicomTags.AdmissionID);
			dataset.AddElement(DicomTags.AccessionNumber);
			dataset.AddElement(DicomTags.ReferringPhysiciansName);
			dataset.AddElement(DicomTags.AdmittingDiagnosesDescription);
			dataset.AddElement(DicomTags.RequestingPhysician);
			dataset.AddElement(DicomTags.StudyInstanceUID);
			dataset.AddElement(DicomTags.RequestedProcedureDescription);
			dataset.AddElement(DicomTags.RequestedProcedureID);
			dataset.AddElement(DicomTags.ReasonForTheRequestedProcedure);
			dataset.AddElement(DicomTags.RequestedProcedurePriority);

			dataset.AddElement(DicomTags.StudyDate);//*
			dataset.AddElement(DicomTags.StudyTime);//*

			//DicomTags.RequestedProcedureCodeSequence
			//DicomTags.ScheduledProtocolCodeSequence

			DcmItemSequenceItem sps = new DcmItemSequenceItem();
			sps.Dataset.AddElementWithValue(DicomTags.ScheduledStationAETitle, ScheduledStationAE);
			sps.Dataset.AddElement(DicomTags.ScheduledProcedureStepStartDate);
			sps.Dataset.GetDA(DicomTags.ScheduledProcedureStepStartDate).SetDateTimeRange(ScheduledProcedureStepStartDate);
			sps.Dataset.AddElement(DicomTags.ScheduledProcedureStepStartTime);
			sps.Dataset.GetTM(DicomTags.ScheduledProcedureStepStartTime).SetDateTimeRange(ScheduledProcedureStepStartTime);
			sps.Dataset.AddElementWithValue(DicomTags.Modality, Modality);
			sps.Dataset.AddElement(DicomTags.ScheduledPerformingPhysiciansName);
			sps.Dataset.AddElement(DicomTags.ScheduledProcedureStepDescription);
			sps.Dataset.AddElement(DicomTags.ScheduledProcedureStepLocation);
			sps.Dataset.AddElement(DicomTags.ScheduledProcedureStepID);

			DcmItemSequence sq = new DcmItemSequence(DicomTags.ScheduledProcedureStepSequence);
			sq.AddSequenceItem(sps);
			dataset.AddItem(sq);
		}
		#endregion
	}

	public sealed class CFindWorklistResponse : CFindResponse {
	#region Public Properties
		//public DcmDataset Dataset { get; internal set; }

		// Patient Identification
		[DicomField(0x0010, 0x0010, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string PatientsName { get; set; }

		[DicomField(0x0010, 0x0020, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string PatientID { get; set; }

		// Patient Demographic
		[DicomField(0x0010, 0x0030, DefaultValue = DicomFieldDefault.MinValue, CreateEmptyElement = true)]
		public DateTime PatientsBirthDate { get; set; }

		[DicomField(0x0010, 0x0040, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string PatientsSex { get; set; }

		[DicomField(0x0010, 0x1010, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string PatientsAge { get; set; }

		[DicomField(0x0010, 0x1020, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string PatientsSize { get; set; }

		[DicomField(0x0010, 0x1030, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string PatientsWeight { get; set; }

		[DicomField(0x0010, 0x2000, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string MedicalAlerts { get; set; }

		[DicomField(0x0010, 0x21C0, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public ushort PregnancyStatus { get; set; }

		[DicomField(0x0010, 0x4000, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string PatientComments { get; set; }

		// Visit Identification		
		[DicomField(0x0008, 0x0080, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string InstitutionName { get; set; }

		[DicomField(0x0038, 0x0010, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string AdmissionID { get; set; }

		// Imaging Service Request
		[DicomField(0x0008, 0x0050, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string AccessionNumber { get; set; }

		[DicomField(0x0008, 0x0090, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string ReferringPhysicianName { get; set; }

		[DicomField(0x0008, 0x1080, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string AdmittingDiagnosisDescription { get; set; }

		[DicomField(0x0032, 0x1032, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string RequestingPhysician { get; set; }

		// Requested Procedure		
		[DicomField(0x0020, 0x000D, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string StudyInstanceUID { get; set; }

		[DicomField(0x0032, 0x1060, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string RequestedProcedureDescription { get; set; }

		[DicomField(0x0040, 0x1001, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string RequestedProcedureID { get; set; }

		[DicomField(0x0040, 0x1002, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string ReasonForTheRequestedProcedure { get; set; }

		[DicomField(0x0040, 0x1003, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string RequestedProcedurePriority { get; set; }

		// Scheduled Procedure Step Sequence
		[DicomField(0x0008, 0x0060, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string Modality { get; set; }

		[DicomField(0x0040, 0x0001, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string ScheduledStationAETitle { get; set; }

		[DicomField(0x0040, 0x0002, DefaultValue = DicomFieldDefault.MinValue, CreateEmptyElement = true)]
		public DateTime ScheduledProcedureStartDate { get; set; }

		[DicomField(0x0040, 0x0003, DefaultValue = DicomFieldDefault.MinValue, CreateEmptyElement = true)]
		public DateTime ScheduledProcedureStartTime { get; set; }

		[DicomField(0x0040, 0x0006, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string ScheduledPerformingPhysicianName { get; set; }

		[DicomField(0x0040, 0x0007, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string ScheduledProcedureStepDescription { get; set; }

		[DicomField(0x0040, 0x0009, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string ScheduledProcedureStepID { get; set; }

		[DicomField(0x0040, 0x0011, DefaultValue = DicomFieldDefault.Default, CreateEmptyElement = true)]
		public string ScheduledProcedureStepLocation { get; set; }
		#endregion

	#region Protected Members
		public override void FromDataset(DcmDataset dataset) {
			//dataset.Dump();
			_dataset = dataset;
			dataset.LoadDicomFields(this);
			if (dataset.Contains(DicomTags.ScheduledProcedureStepSequence)) {
				DcmItemSequence sq = dataset.GetSQ(DicomTags.ScheduledProcedureStepSequence);
				if (sq.SequenceItems.Count > 0) {
					DcmItemSequenceItem sps = sq.SequenceItems[0];
					Modality = sps.Dataset.GetString(DicomTags.Modality, String.Empty);
					ScheduledStationAETitle = sps.Dataset.GetString(DicomTags.ScheduledStationAETitle, String.Empty);
					ScheduledProcedureStartDate = sps.Dataset.GetDateTime(DicomTags.ScheduledProcedureStepStartDate, 0, DateTime.MinValue);
					ScheduledProcedureStartTime = sps.Dataset.GetDateTime(DicomTags.ScheduledProcedureStepStartTime, 0, DateTime.MinValue);
					ScheduledPerformingPhysicianName = sps.Dataset.GetString(DicomTags.ScheduledPerformingPhysiciansName, String.Empty);
					ScheduledProcedureStepDescription = sps.Dataset.GetString(DicomTags.ScheduledProcedureStepDescription, String.Empty);
					ScheduledProcedureStepID = sps.Dataset.GetString(DicomTags.ScheduledProcedureStepID, String.Empty);
					ScheduledProcedureStepLocation = sps.Dataset.GetString(DicomTags.ScheduledProcedureStepLocation, String.Empty);
				}
			}
		}
		#endregion
	}

	public sealed class CFindWorklistClient : CFindClientT<CFindWorklistQuery, CFindWorklistResponse> {
		public CFindWorklistClient() : base() {
			FindSopClassUID = DicomUID.ModalityWorklistInformationModelFIND;
		}
	}
	#endregion

	#region CFindClient
	public class CFindClient : DcmClientBase {
		#region Private Members
		private DicomUID _findSopClass;
		private Queue<DcmDataset> _queries;
		private DcmDataset _current;
		#endregion

		#region Public Constructor
		public CFindClient()
			: base() {
			LogID = "C-Find SCU";
			CallingAE = "FIND_SCU";
			CalledAE = "FIND_SCP";
			_queries = new Queue<DcmDataset>();
			_findSopClass = DicomUID.StudyRootQueryRetrieveInformationModelFIND;
			_current = null;
		}
		#endregion

		#region Public Properties
		public delegate void CFindResponseDelegate(DcmDataset query, DcmDataset result);
		public CFindResponseDelegate OnCFindResponse;

		public delegate void CFindCompleteDelegate(DcmDataset query);
		public CFindCompleteDelegate OnCFindComplete;

		public DicomUID FindSopClassUID {
			get { return _findSopClass; }
			set { _findSopClass = value; }
		}

		public Queue<DcmDataset> FindQueries {
			get { return _queries; }
			set { _queries = value; }
		}
		#endregion

		#region Public Members
		public void AddQuery(DcmDataset query) {
			_queries.Enqueue(query);
		}
		#endregion

		#region Protected Overrides
		protected override void OnConnected() {
			DcmAssociate associate = new DcmAssociate();

			byte pcid = associate.AddPresentationContext(FindSopClassUID);
			//associate.AddTransferSyntax(pcid, DicomTransferSyntax.ExplicitVRLittleEndian);
			associate.AddTransferSyntax(pcid, DicomTransferSyntax.ImplicitVRLittleEndian);

			associate.CalledAE = CalledAE;
			associate.CallingAE = CallingAE;
			associate.MaximumPduLength = MaxPduSize;

			SendAssociateRequest(associate);
		}

		private void PerformQueryOrRelease() {
			if (FindQueries.Count > 0) {
				_current = FindQueries.Dequeue();
				byte pcid = Associate.FindAbstractSyntax(FindSopClassUID);
				if (Associate.GetPresentationContextResult(pcid) == DcmPresContextResult.Accept) {
					SendCFindRequest(pcid, NextMessageID(), Priority, _current);
				}
				else {
					Log.Info("{0} <- Presentation context rejected: {1}", LogID, Associate.GetPresentationContextResult(pcid));
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

		protected override void OnReceiveCFindResponse(byte presentationID, ushort messageID, DcmDataset dataset, DcmStatus status) {
			if (status.State != DcmState.Pending) {
				if (OnCFindComplete != null) {
					OnCFindComplete(_current);
				}
				PerformQueryOrRelease();
			}
			else if (dataset != null) {
				if (OnCFindResponse != null) {
					OnCFindResponse(_current, dataset);
				}
			}
		}
		#endregion
	}
	#endregion
}
