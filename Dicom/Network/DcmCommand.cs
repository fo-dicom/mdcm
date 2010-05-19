// mDCM: A C# DICOM library
//
// Copyright (c) 2006-2009  Colby Dillion
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
using System.Text;

using Dicom.Data;

namespace Dicom.Network {
	public enum DcmPriority : ushort {
		Low = 0x0002,
		Medium = 0x0000,
		High = 0x0001
	}

	public enum DcmCommandField : ushort {
		CStoreRequest = 0x0001,
		CStoreResponse = 0x8001,
		CGetRequest = 0x0010,
		CGetResponse = 0x8010,
		CFindRequest = 0x0020,
		CFindResponse = 0x8020,
		CMoveRequest = 0x0021,
		CMoveResponse = 0x8021,
		CEchoRequest = 0x0030,
		CEchoResponse = 0x8030,
		NEventReportRequest = 0x0100,
		NEventReportResponse = 0x8100,
		NGetRequest = 0x0110,
		NGetResponse = 0x8110,
		NSetRequest = 0x0120,
		NSetResponse = 0x8120,
		NActionRequest = 0x0130,
		NActionResponse = 0x8130,
		NCreateRequest = 0x0140,
		NCreateResponse = 0x8140,
		NDeleteRequest = 0x0150,
		NDeleteResponse = 0x8150,
		CCancelRequest = 0x0FFF
	}

	public class DcmCommand : DcmDataset {
		#region Public Constructors
		public DcmCommand() : base(DicomTransferSyntax.ImplicitVRLittleEndian) {
		}
		#endregion

		#region Public Properties
		public DicomUID AffectedSOPClassUID {
			get {
				return GetUID(DicomTags.AffectedSOPClassUID);
			}
			set {
				AddElementWithValue(DicomTags.AffectedSOPClassUID, value);
			}
		}

		public DicomUID RequestedSOPClassUID {
			get {
				return GetUID(DicomTags.RequestedSOPClassUID);
			}
			set {
				AddElementWithValue(DicomTags.RequestedSOPClassUID, value);
			}
		}

		public DcmCommandField CommandField {
			get {
				return (DcmCommandField)GetUInt16(DicomTags.CommandField, 0);
			}
			set {
				AddElementWithValue(DicomTags.CommandField, (ushort)value);
			}
		}

		public ushort MessageID {
			get {
				return GetUInt16(DicomTags.MessageID, 0);
			}
			set {
				AddElementWithValue(DicomTags.MessageID, value);
			}
		}

		public ushort MessageIDBeingRespondedTo {
			get {
				return GetUInt16(DicomTags.MessageIDBeingRespondedTo, 0);
			}
			set {
				AddElementWithValue(DicomTags.MessageIDBeingRespondedTo, value);
			}
		}

		public string MoveDestinationAE {
			get {
				return GetString(DicomTags.MoveDestination, null);
			}
			set {
				AddElementWithValue(DicomTags.MoveDestination, value);
			}
		}

		public DcmPriority Priority {
			get {
				return (DcmPriority)GetUInt16(DicomTags.Priority, 0);
			}
			set {
				AddElementWithValue(DicomTags.Priority, (ushort)value);
			}
		}

		public bool HasDataset {
			get {
				return DataSetType != (ushort)0x0101;
			}
			set {
				DataSetType = value ? (ushort)0x0202 : (ushort)0x0101;
			}
		}

		public ushort DataSetType {
			get {
				return GetUInt16(DicomTags.DataSetType, (ushort)0x0101);
			}
			set {
				AddElementWithValue(DicomTags.DataSetType, value);
			}
		}

		public DcmStatus Status {
			get {
				DcmStatus status = DcmStatus.Lookup(GetUInt16(DicomTags.Status, 0x0211));
				string comment = ErrorComment;
				if (!String.IsNullOrEmpty(comment))
					status = new DcmStatus(status, comment);
				return status;
			}
			set {
				AddElementWithValue(DicomTags.Status, value.Code);
			}
		}

		public DicomTag OffendingElement {
			get {
				return GetDcmTag(DicomTags.OffendingElement);
			}
			set {
				AddElementWithValue(DicomTags.OffendingElement, value);
			}
		}

		public string ErrorComment {
			get {
				return GetString(DicomTags.ErrorComment, null);
			}
			set {
				AddElementWithValue(DicomTags.ErrorComment, value);
			}
		}

		public ushort ErrorID {
			get {
				return GetUInt16(DicomTags.ErrorID, 0);
			}
			set {
				AddElementWithValue(DicomTags.ErrorID, value);
			}
		}

		public DicomUID AffectedSOPInstanceUID {
			get {
				return GetUID(DicomTags.AffectedSOPInstanceUID);
			}
			set {
				AddElementWithValue(DicomTags.AffectedSOPInstanceUID, value);
			}
		}

		public DicomUID RequestedSOPInstanceUID {
			get {
				return GetUID(DicomTags.RequestedSOPInstanceUID);
			}
			set {
				AddElementWithValue(DicomTags.RequestedSOPInstanceUID, value);
			}
		}

		public ushort EventTypeID {
			get {
				return GetUInt16(DicomTags.EventTypeID, 0);
			}
			set {
				AddElementWithValue(DicomTags.EventTypeID, value);
			}
		}

		public DcmAttributeTag AttributeIdentifierList {
			get {
				return GetAT(DicomTags.AttributeIdentifierList);
			}
			set {
				AddItem(value);
			}
		}

		public ushort ActionTypeID {
			get {
				return GetUInt16(DicomTags.ActionTypeID, 0);
			}
			set {
				AddElementWithValue(DicomTags.ActionTypeID, value);
			}
		}

		public ushort NumberOfRemainingSuboperations {
			get {
				return GetUInt16(DicomTags.NumberOfRemainingSuboperations, 0);
			}
			set {
				AddElementWithValue(DicomTags.NumberOfRemainingSuboperations, value);
			}
		}

		public ushort NumberOfCompletedSuboperations {
			get {
				return GetUInt16(DicomTags.NumberOfCompletedSuboperations, 0);
			}
			set {
				AddElementWithValue(DicomTags.NumberOfCompletedSuboperations, value);
			}
		}

		public ushort NumberOfFailedSuboperations {
			get {
				return GetUInt16(DicomTags.NumberOfFailedSuboperations, 0);
			}
			set {
				AddElementWithValue(DicomTags.NumberOfFailedSuboperations, value);
			}
		}

		public ushort NumberOfWarningSuboperations {
			get {
				return GetUInt16(DicomTags.NumberOfWarningSuboperations, 0);
			}
			set {
				AddElementWithValue(DicomTags.NumberOfWarningSuboperations, value);
			}
		}

		public string MoveOriginatorAE {
			get {
				return GetString(DicomTags.MoveOriginatorApplicationEntityTitle, null);
			}
			set {
				AddElementWithValue(DicomTags.MoveOriginatorApplicationEntityTitle, value);
			}
		}

		public ushort MoveOriginatorMessageID {
			get {
				return GetUInt16(DicomTags.MoveOriginatorMessageID, 0);
			}
			set {
				AddElementWithValue(DicomTags.MoveOriginatorMessageID, value);
			}
		}
		#endregion

		#region Public Methods
		public string GetErrorString() {
			StringBuilder sb = new StringBuilder();

			if (Contains(DicomTags.ErrorComment))
				sb.Append(ErrorComment);
			else {
				sb.Append("Unspecified");
			}

			if (Contains(DicomTags.OffendingElement)) {
				DcmAttributeTag at = GetAT(DicomTags.OffendingElement);
				sb.Append(" [");
				DicomTag[] tags = at.GetValues();
				for (int i = 0; i < tags.Length; i++) {
					if (i > 0)
						sb.Append("; ");
					sb.Append(tags[i].ToString());
					sb.Append(" ");
					sb.Append(tags[i].Entry.Name);
				}
				sb.Append("]");
			}

			return sb.ToString();
		}
		#endregion
	}
}
