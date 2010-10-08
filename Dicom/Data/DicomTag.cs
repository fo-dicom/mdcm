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
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;

namespace Dicom.Data {
	/// <summary>DICOM Tag</summary>
	[Serializable]
	public sealed class DicomTag : ISerializable {
		#region Private Members
		private ushort _g;
		private ushort _e;
		private uint _c;
		private string _p;
		private DcmDictionaryEntry _entry;
		#endregion

		#region Public Constructors
		public DicomTag(uint card) {
			_g = (ushort)(card >> 16);
			_e = (ushort)(card & 0xffff);
			_c = card;
			_p = String.Empty;
		}

		public DicomTag(ushort group, ushort element) {
			_g = group;
			_e = element;
			_c = (uint)_g << 16 | (uint)_e;
			_p = String.Empty;
		}

		public DicomTag(ushort group, ushort element, string creator) {
			_g = group;
			_e = element;
			_c = (uint)_g << 16 | (uint)_e;
			_p = creator ?? String.Empty;
		}

		private DicomTag(SerializationInfo info, StreamingContext context) {
			_g = info.GetUInt16("Group");
			_e = info.GetUInt16("Element");
			_c = (uint)_g << 16 | (uint)_e;
			_p = String.Empty;
		}
		#endregion

		#region Public Properties
		public ushort Group {
			get { return _g; }
		}
		public ushort Element {
			get { return _e; }
		}
		public uint Card {
			get { return _c; }
		}
		public byte PrivateGroup {
			get {
				if (!IsPrivate)
					return 0x00;

				if (Element < 0xff00)
					return (byte)(0x00ff & Element);

				return (byte)((Element & 0xff00) >> 8);
			}
		}
		public string PrivateCreator {
			get { return _p; }
		}
		public DcmDictionaryEntry Entry {
			get {
				if (_entry == null)
					_entry = DcmDictionary.Lookup(this);
				return _entry;
			}
		}
		public bool IsPrivate {
			get { return (_g & 1) == 1; }
		}
		#endregion

		#region Operators
		public static bool operator ==(DicomTag t1, DicomTag t2) {
			if ((object)t1 == null && (object)t2 == null)
				return true;
			if ((object)t1 == null || (object)t2 == null)
				return false;
			return t1.Card == t2.Card;
		}
		public static bool operator !=(DicomTag t1, DicomTag t2) {
			return !(t1 == t2);
		}
		public static bool operator <(DicomTag t1, DicomTag t2) {
			if ((object)t1 == null || (object)t2 == null)
				return false;
			if (t1.Group == t2.Group && t1.Element < t2.Element)
				return true;
			if (t1.Group < t2.Group)
				return true;
			return false;
		}
		public static bool operator >(DicomTag t1, DicomTag t2) {
			return !(t1 < t2);
		}
		public static bool operator <=(DicomTag t1, DicomTag t2) {
			if ((object)t1 == null || (object)t2 == null)
				return false;
			if (t1.Group == t2.Group && t1.Element <= t2.Element)
				return true;
			if (t1.Group < t2.Group)
				return true;
			return false;
		}
		public static bool operator >=(DicomTag t1, DicomTag t2) {
			if ((object)t1 == null || (object)t2 == null)
				return false;
			if (t1.Group == t2.Group && t1.Element >= t2.Element)
				return true;
			if (t1.Group > t2.Group)
				return true;
			return false;
		}

		public override bool Equals(object obj) {
			if (obj is DicomTag) {
				return ((DicomTag)obj).Card == Card;
			}
			return false;
		}

		public override int GetHashCode() {
			return (int)Card;
		}
		#endregion

		public static uint GetCard(ushort group, ushort element) {
			return (uint)group << 16 | (uint)element;
		}


		public static bool IsPrivateGroup(ushort group) {
			return (group & 1) == 1;
		}

		public static DicomTag GetPrivateCreatorTag(ushort group, ushort element) {
			return new DicomTag(group, (ushort)(element >> 8));
		}

		public override string ToString() {
			return String.Format("({0:x4},{1:x4})", _g, _e);
		}

		public static DicomTag Parse(string tag) {
			try {
				// clean extra characters
				tag = tag.Trim().Trim('(', ')');

				string[] parts = tag.Split(',');

				if (parts.Length != 2) {
					if (tag.Length == 8) {
						parts = new string[2];
						parts[0] = tag.Substring(0, 4);
						parts[1] = tag.Substring(4, 4);
					}
					else
						return null;
				}

				parts[0] = parts[0].Trim();
				parts[1] = parts[1].Trim();

				if (parts[0].Length != 4 ||
					parts[1].Length != 4)
					return null;

				ushort g = ushort.Parse(parts[0], NumberStyles.HexNumber);
				ushort e = ushort.Parse(parts[1], NumberStyles.HexNumber);

				return new DicomTag(g, e);
			}
			catch {
				return null;
			}
		}

		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
			info.AddValue("Group", Group);
			info.AddValue("Element", Element);
		}
	}

	/// <summary>Compares two DicomTag objects for sorting.</summary>
	public class DicomTagComparer : IComparer<DicomTag> {
		public int Compare(DicomTag x, DicomTag y) {
			return x.Card.CompareTo(y.Card);

			//if (x.Group != y.Group)
			//	return x.Group.CompareTo(y.Group);

			//if (x.IsPrivate && y.IsPrivate)
			//	return x.PrivateGroup.CompareTo(y.PrivateGroup);

			//return x.Element.CompareTo(y.Element);
		}
	}

	/// <summary>DICOM Tags</summary>
	public static class DicomTags {
		#region Internal Dictionary Quick Tags
		/// <summary>(0000,0001) VR=UL VM=1 Length to End (Retired)</summary>
		public static DicomTag LengthToEndRETIRED = new DicomTag(0x0000, 0x0001);

		/// <summary>(0000,0002) VR=UI VM=1 Affected SOP Class UID</summary>
		public static DicomTag AffectedSOPClassUID = new DicomTag(0x0000, 0x0002);

		/// <summary>(0000,0003) VR=UI VM=1 Requested SOP Class UID</summary>
		public static DicomTag RequestedSOPClassUID = new DicomTag(0x0000, 0x0003);

		/// <summary>(0000,0010) VR=CS VM=1 Recognition Code (Retired)</summary>
		public static DicomTag RecognitionCodeRETIRED = new DicomTag(0x0000, 0x0010);

		/// <summary>(0000,0100) VR=US VM=1 Command Field</summary>
		public static DicomTag CommandField = new DicomTag(0x0000, 0x0100);

		/// <summary>(0000,0110) VR=US VM=1 Message ID</summary>
		public static DicomTag MessageID = new DicomTag(0x0000, 0x0110);

		/// <summary>(0000,0120) VR=US VM=1 Message ID Being Responded To</summary>
		public static DicomTag MessageIDBeingRespondedTo = new DicomTag(0x0000, 0x0120);

		/// <summary>(0000,0200) VR=AE VM=1 Initiator (Retired)</summary>
		public static DicomTag InitiatorRETIRED = new DicomTag(0x0000, 0x0200);

		/// <summary>(0000,0300) VR=AE VM=1 Receiver (Retired)</summary>
		public static DicomTag ReceiverRETIRED = new DicomTag(0x0000, 0x0300);

		/// <summary>(0000,0400) VR=AE VM=1 Find Location (Retired)</summary>
		public static DicomTag FindLocationRETIRED = new DicomTag(0x0000, 0x0400);

		/// <summary>(0000,0600) VR=AE VM=1 Move Destination</summary>
		public static DicomTag MoveDestination = new DicomTag(0x0000, 0x0600);

		/// <summary>(0000,0700) VR=US VM=1 Priority</summary>
		public static DicomTag Priority = new DicomTag(0x0000, 0x0700);

		/// <summary>(0000,0800) VR=US VM=1 Data Set Type</summary>
		public static DicomTag DataSetType = new DicomTag(0x0000, 0x0800);

		/// <summary>(0000,0850) VR=US VM=1 Number of Matches (Retired)</summary>
		public static DicomTag NumberOfMatchesRETIRED = new DicomTag(0x0000, 0x0850);

		/// <summary>(0000,0860) VR=US VM=1 Response Sequence Number (Retired)</summary>
		public static DicomTag ResponseSequenceNumberRETIRED = new DicomTag(0x0000, 0x0860);

		/// <summary>(0000,0900) VR=US VM=1 Status</summary>
		public static DicomTag Status = new DicomTag(0x0000, 0x0900);

		/// <summary>(0000,0901) VR=AT VM=1-n Offending Element</summary>
		public static DicomTag OffendingElement = new DicomTag(0x0000, 0x0901);

		/// <summary>(0000,0902) VR=LO VM=1 Error Comment</summary>
		public static DicomTag ErrorComment = new DicomTag(0x0000, 0x0902);

		/// <summary>(0000,0903) VR=US VM=1 Error ID</summary>
		public static DicomTag ErrorID = new DicomTag(0x0000, 0x0903);

		/// <summary>(0000,1000) VR=UI VM=1 Affected SOP Instance UID</summary>
		public static DicomTag AffectedSOPInstanceUID = new DicomTag(0x0000, 0x1000);

		/// <summary>(0000,1001) VR=UI VM=1 Requested SOP Instance UID</summary>
		public static DicomTag RequestedSOPInstanceUID = new DicomTag(0x0000, 0x1001);

		/// <summary>(0000,1002) VR=US VM=1 Event Type ID</summary>
		public static DicomTag EventTypeID = new DicomTag(0x0000, 0x1002);

		/// <summary>(0000,1005) VR=AT VM=1-n Attribute Identifier List</summary>
		public static DicomTag AttributeIdentifierList = new DicomTag(0x0000, 0x1005);

		/// <summary>(0000,1008) VR=US VM=1 Action Type ID</summary>
		public static DicomTag ActionTypeID = new DicomTag(0x0000, 0x1008);

		/// <summary>(0000,1020) VR=US VM=1 Number of Remaining Sub-operations</summary>
		public static DicomTag NumberOfRemainingSuboperations = new DicomTag(0x0000, 0x1020);

		/// <summary>(0000,1021) VR=US VM=1 Number of Completed Sub-operations</summary>
		public static DicomTag NumberOfCompletedSuboperations = new DicomTag(0x0000, 0x1021);

		/// <summary>(0000,1022) VR=US VM=1 Number of Failed Sub-operations</summary>
		public static DicomTag NumberOfFailedSuboperations = new DicomTag(0x0000, 0x1022);

		/// <summary>(0000,1023) VR=US VM=1 Number of Warning Sub-operations</summary>
		public static DicomTag NumberOfWarningSuboperations = new DicomTag(0x0000, 0x1023);

		/// <summary>(0000,1030) VR=AE VM=1 Move Originator Application Entity Title</summary>
		public static DicomTag MoveOriginatorApplicationEntityTitle = new DicomTag(0x0000, 0x1030);

		/// <summary>(0000,1031) VR=US VM=1 Move Originator Message ID</summary>
		public static DicomTag MoveOriginatorMessageID = new DicomTag(0x0000, 0x1031);

		/// <summary>(0000,4000) VR=AT VM=1 DIALOG Receiver (Retired)</summary>
		public static DicomTag DIALOGReceiverRETIRED = new DicomTag(0x0000, 0x4000);

		/// <summary>(0000,4010) VR=AT VM=1 Terminal Type (Retired)</summary>
		public static DicomTag TerminalTypeRETIRED = new DicomTag(0x0000, 0x4010);

		/// <summary>(0000,5010) VR=SH VM=1 Message Set ID (Retired)</summary>
		public static DicomTag MessageSetIDRETIRED = new DicomTag(0x0000, 0x5010);

		/// <summary>(0000,5020) VR=SH VM=1 End Message ID (Retired)</summary>
		public static DicomTag EndMessageIDRETIRED = new DicomTag(0x0000, 0x5020);

		/// <summary>(0000,5110) VR=AT VM=1 Display Format (Retired)</summary>
		public static DicomTag DisplayFormatRETIRED = new DicomTag(0x0000, 0x5110);

		/// <summary>(0000,5120) VR=AT VM=1 Page Position ID (Retired)</summary>
		public static DicomTag PagePositionIDRETIRED = new DicomTag(0x0000, 0x5120);

		/// <summary>(0000,5130) VR=CS VM=1 Text Format ID (Retired)</summary>
		public static DicomTag TextFormatIDRETIRED = new DicomTag(0x0000, 0x5130);

		/// <summary>(0000,5140) VR=CS VM=1 Nor/Rev (Retired)</summary>
		public static DicomTag NorRevRETIRED = new DicomTag(0x0000, 0x5140);

		/// <summary>(0000,5150) VR=CS VM=1 Add Gray Scale (Retired)</summary>
		public static DicomTag AddGrayScaleRETIRED = new DicomTag(0x0000, 0x5150);

		/// <summary>(0000,5160) VR=CS VM=1 Borders (Retired)</summary>
		public static DicomTag BordersRETIRED = new DicomTag(0x0000, 0x5160);

		/// <summary>(0000,5170) VR=IS VM=1 Copies (Retired)</summary>
		public static DicomTag CopiesRETIRED = new DicomTag(0x0000, 0x5170);

		/// <summary>(0000,5180) VR=CS VM=1 Magnification Type (Retired)</summary>
		public static DicomTag MagnificationTypeRETIRED = new DicomTag(0x0000, 0x5180);

		/// <summary>(0000,5190) VR=CS VM=1 Erase (Retired)</summary>
		public static DicomTag EraseRETIRED = new DicomTag(0x0000, 0x5190);

		/// <summary>(0000,51a0) VR=CS VM=1 Print (Retired)</summary>
		public static DicomTag PrintRETIRED = new DicomTag(0x0000, 0x51a0);

		/// <summary>(0000,51b0) VR=US VM=1-n Overlays (Retired)</summary>
		public static DicomTag OverlaysRETIRED = new DicomTag(0x0000, 0x51b0);

		/// <summary>(0002,0001) VR=OB VM=1 File Meta Information Version</summary>
		public static DicomTag FileMetaInformationVersion = new DicomTag(0x0002, 0x0001);

		/// <summary>(0002,0002) VR=UI VM=1 Media Storage SOP Class UID</summary>
		public static DicomTag MediaStorageSOPClassUID = new DicomTag(0x0002, 0x0002);

		/// <summary>(0002,0003) VR=UI VM=1 Media Storage SOP Instance UID</summary>
		public static DicomTag MediaStorageSOPInstanceUID = new DicomTag(0x0002, 0x0003);

		/// <summary>(0002,0010) VR=UI VM=1 Transfer Syntax UID</summary>
		public static DicomTag TransferSyntaxUID = new DicomTag(0x0002, 0x0010);

		/// <summary>(0002,0012) VR=UI VM=1 Implementation Class UID</summary>
		public static DicomTag ImplementationClassUID = new DicomTag(0x0002, 0x0012);

		/// <summary>(0002,0013) VR=SH VM=1 Implementation Version Name</summary>
		public static DicomTag ImplementationVersionName = new DicomTag(0x0002, 0x0013);

		/// <summary>(0002,0016) VR=AE VM=1 Source Application Entity Title</summary>
		public static DicomTag SourceApplicationEntityTitle = new DicomTag(0x0002, 0x0016);

		/// <summary>(0002,0100) VR=UI VM=1 Private Information Creator UID</summary>
		public static DicomTag PrivateInformationCreatorUID = new DicomTag(0x0002, 0x0100);

		/// <summary>(0002,0102) VR=OB VM=1 Private Information</summary>
		public static DicomTag PrivateInformation = new DicomTag(0x0002, 0x0102);

		/// <summary>(0004,1130) VR=CS VM=1 File-set ID</summary>
		public static DicomTag FilesetID = new DicomTag(0x0004, 0x1130);

		/// <summary>(0004,1141) VR=CS VM=1-8 File-set Descriptor File ID</summary>
		public static DicomTag FilesetDescriptorFileID = new DicomTag(0x0004, 0x1141);

		/// <summary>(0004,1142) VR=CS VM=1 Specific Character Set of File-set Descriptor File</summary>
		public static DicomTag SpecificCharacterSetOfFilesetDescriptorFile = new DicomTag(0x0004, 0x1142);

		/// <summary>(0004,1200) VR=UL VM=1 Offset of the First Directory Record of the Root Directory Entity</summary>
		public static DicomTag OffsetOfTheFirstDirectoryRecordOfTheRootDirectoryEntity = new DicomTag(0x0004, 0x1200);

		/// <summary>(0004,1202) VR=UL VM=1 Offset of the Last Directory Record of the Root Directory Entity</summary>
		public static DicomTag OffsetOfTheLastDirectoryRecordOfTheRootDirectoryEntity = new DicomTag(0x0004, 0x1202);

		/// <summary>(0004,1212) VR=US VM=1 File-set Consistency Flag</summary>
		public static DicomTag FilesetConsistencyFlag = new DicomTag(0x0004, 0x1212);

		/// <summary>(0004,1220) VR=SQ VM=1 Directory Record Sequence</summary>
		public static DicomTag DirectoryRecordSequence = new DicomTag(0x0004, 0x1220);

		/// <summary>(0004,1400) VR=UL VM=1 Offset of the Next Directory Record</summary>
		public static DicomTag OffsetOfTheNextDirectoryRecord = new DicomTag(0x0004, 0x1400);

		/// <summary>(0004,1410) VR=US VM=1 Record In-use Flag</summary>
		public static DicomTag RecordInuseFlag = new DicomTag(0x0004, 0x1410);

		/// <summary>(0004,1420) VR=UL VM=1 Offset of Referenced Lower-Level Directory Entity</summary>
		public static DicomTag OffsetOfReferencedLowerLevelDirectoryEntity = new DicomTag(0x0004, 0x1420);

		/// <summary>(0004,1430) VR=CS VM=1 Directory Record Type</summary>
		public static DicomTag DirectoryRecordType = new DicomTag(0x0004, 0x1430);

		/// <summary>(0004,1432) VR=UI VM=1 Private Record UID</summary>
		public static DicomTag PrivateRecordUID = new DicomTag(0x0004, 0x1432);

		/// <summary>(0004,1500) VR=CS VM=1-8 Referenced File ID</summary>
		public static DicomTag ReferencedFileID = new DicomTag(0x0004, 0x1500);

		/// <summary>(0004,1504) VR=UL VM=1 MRDR Directory Record Offset (Retired)</summary>
		public static DicomTag MRDRDirectoryRecordOffsetRETIRED = new DicomTag(0x0004, 0x1504);

		/// <summary>(0004,1510) VR=UI VM=1 Referenced SOP Class UID in File</summary>
		public static DicomTag ReferencedSOPClassUIDInFile = new DicomTag(0x0004, 0x1510);

		/// <summary>(0004,1511) VR=UI VM=1 Referenced SOP Instance UID in File</summary>
		public static DicomTag ReferencedSOPInstanceUIDInFile = new DicomTag(0x0004, 0x1511);

		/// <summary>(0004,1512) VR=UI VM=1 Referenced Transfer Syntax UID in File</summary>
		public static DicomTag ReferencedTransferSyntaxUIDInFile = new DicomTag(0x0004, 0x1512);

		/// <summary>(0004,151a) VR=UI VM=1-n Referenced Related General SOP Class UID in File</summary>
		public static DicomTag ReferencedRelatedGeneralSOPClassUIDInFile = new DicomTag(0x0004, 0x151a);

		/// <summary>(0004,1600) VR=UL VM=1 Number of References (Retired)</summary>
		public static DicomTag NumberOfReferencesRETIRED = new DicomTag(0x0004, 0x1600);

		/// <summary>(0008,0001) VR=UL VM=1 Length to End (Retired)</summary>
		public static DicomTag LengthToEnd2RETIRED = new DicomTag(0x0008, 0x0001);

		/// <summary>(0008,0005) VR=CS VM=1-n Specific Character Set</summary>
		public static DicomTag SpecificCharacterSet = new DicomTag(0x0008, 0x0005);

		/// <summary>(0008,0008) VR=CS VM=2-n Image Type</summary>
		public static DicomTag ImageType = new DicomTag(0x0008, 0x0008);

		/// <summary>(0008,0010) VR=CS VM=1 Recognition Code (Retired)</summary>
		public static DicomTag RecognitionCode2RETIRED = new DicomTag(0x0008, 0x0010);

		/// <summary>(0008,0012) VR=DA VM=1 Instance Creation Date</summary>
		public static DicomTag InstanceCreationDate = new DicomTag(0x0008, 0x0012);

		/// <summary>(0008,0013) VR=TM VM=1 Instance Creation Time</summary>
		public static DicomTag InstanceCreationTime = new DicomTag(0x0008, 0x0013);

		/// <summary>(0008,0014) VR=UI VM=1 Instance Creator UID</summary>
		public static DicomTag InstanceCreatorUID = new DicomTag(0x0008, 0x0014);

		/// <summary>(0008,0016) VR=UI VM=1 SOP Class UID</summary>
		public static DicomTag SOPClassUID = new DicomTag(0x0008, 0x0016);

		/// <summary>(0008,0018) VR=UI VM=1 SOP Instance UID</summary>
		public static DicomTag SOPInstanceUID = new DicomTag(0x0008, 0x0018);

		/// <summary>(0008,001a) VR=UI VM=1-n Related General SOP Class UID</summary>
		public static DicomTag RelatedGeneralSOPClassUID = new DicomTag(0x0008, 0x001a);

		/// <summary>(0008,001b) VR=UI VM=1 Original Specialized SOP Class UID</summary>
		public static DicomTag OriginalSpecializedSOPClassUID = new DicomTag(0x0008, 0x001b);

		/// <summary>(0008,0020) VR=DA VM=1 Study Date</summary>
		public static DicomTag StudyDate = new DicomTag(0x0008, 0x0020);

		/// <summary>(0008,0021) VR=DA VM=1 Series Date</summary>
		public static DicomTag SeriesDate = new DicomTag(0x0008, 0x0021);

		/// <summary>(0008,0022) VR=DA VM=1 Acquisition Date</summary>
		public static DicomTag AcquisitionDate = new DicomTag(0x0008, 0x0022);

		/// <summary>(0008,0023) VR=DA VM=1 Content Date</summary>
		public static DicomTag ContentDate = new DicomTag(0x0008, 0x0023);

		/// <summary>(0008,0024) VR=DA VM=1 Overlay Date (Retired)</summary>
		public static DicomTag OverlayDateRETIRED = new DicomTag(0x0008, 0x0024);

		/// <summary>(0008,0025) VR=DA VM=1 Curve Date (Retired)</summary>
		public static DicomTag CurveDateRETIRED = new DicomTag(0x0008, 0x0025);

		/// <summary>(0008,002a) VR=DT VM=1 Acquisition DateTime</summary>
		public static DicomTag AcquisitionDateTime = new DicomTag(0x0008, 0x002a);

		/// <summary>(0008,0030) VR=TM VM=1 Study Time</summary>
		public static DicomTag StudyTime = new DicomTag(0x0008, 0x0030);

		/// <summary>(0008,0031) VR=TM VM=1 Series Time</summary>
		public static DicomTag SeriesTime = new DicomTag(0x0008, 0x0031);

		/// <summary>(0008,0032) VR=TM VM=1 Acquisition Time</summary>
		public static DicomTag AcquisitionTime = new DicomTag(0x0008, 0x0032);

		/// <summary>(0008,0033) VR=TM VM=1 Content Time</summary>
		public static DicomTag ContentTime = new DicomTag(0x0008, 0x0033);

		/// <summary>(0008,0034) VR=TM VM=1 Overlay Time (Retired)</summary>
		public static DicomTag OverlayTimeRETIRED = new DicomTag(0x0008, 0x0034);

		/// <summary>(0008,0035) VR=TM VM=1 Curve Time (Retired)</summary>
		public static DicomTag CurveTimeRETIRED = new DicomTag(0x0008, 0x0035);

		/// <summary>(0008,0040) VR=US VM=1 Data Set Type (Retired)</summary>
		public static DicomTag DataSetTypeRETIRED = new DicomTag(0x0008, 0x0040);

		/// <summary>(0008,0041) VR=LO VM=1 Data Set Subtype (Retired)</summary>
		public static DicomTag DataSetSubtypeRETIRED = new DicomTag(0x0008, 0x0041);

		/// <summary>(0008,0042) VR=CS VM=1 Nuclear Medicine Series Type (Retired)</summary>
		public static DicomTag NuclearMedicineSeriesTypeRETIRED = new DicomTag(0x0008, 0x0042);

		/// <summary>(0008,0050) VR=SH VM=1 Accession Number</summary>
		public static DicomTag AccessionNumber = new DicomTag(0x0008, 0x0050);

		/// <summary>(0008,0052) VR=CS VM=1 Query/Retrieve Level</summary>
		public static DicomTag QueryRetrieveLevel = new DicomTag(0x0008, 0x0052);

		/// <summary>(0008,0054) VR=AE VM=1-n Retrieve AE Title</summary>
		public static DicomTag RetrieveAETitle = new DicomTag(0x0008, 0x0054);

		/// <summary>(0008,0056) VR=CS VM=1 Instance Availability</summary>
		public static DicomTag InstanceAvailability = new DicomTag(0x0008, 0x0056);

		/// <summary>(0008,0058) VR=UI VM=1-n Failed SOP Instance UID List</summary>
		public static DicomTag FailedSOPInstanceUIDList = new DicomTag(0x0008, 0x0058);

		/// <summary>(0008,0060) VR=CS VM=1 Modality</summary>
		public static DicomTag Modality = new DicomTag(0x0008, 0x0060);

		/// <summary>(0008,0061) VR=CS VM=1-n Modalities in Study</summary>
		public static DicomTag ModalitiesInStudy = new DicomTag(0x0008, 0x0061);

		/// <summary>(0008,0062) VR=UI VM=1-n SOP Classes in Study</summary>
		public static DicomTag SOPClassesInStudy = new DicomTag(0x0008, 0x0062);

		/// <summary>(0008,0064) VR=CS VM=1 Conversion Type</summary>
		public static DicomTag ConversionType = new DicomTag(0x0008, 0x0064);

		/// <summary>(0008,0068) VR=CS VM=1 Presentation Intent Type</summary>
		public static DicomTag PresentationIntentType = new DicomTag(0x0008, 0x0068);

		/// <summary>(0008,0070) VR=LO VM=1 Manufacturer</summary>
		public static DicomTag Manufacturer = new DicomTag(0x0008, 0x0070);

		/// <summary>(0008,0080) VR=LO VM=1 Institution Name</summary>
		public static DicomTag InstitutionName = new DicomTag(0x0008, 0x0080);

		/// <summary>(0008,0081) VR=ST VM=1 Institution Address</summary>
		public static DicomTag InstitutionAddress = new DicomTag(0x0008, 0x0081);

		/// <summary>(0008,0082) VR=SQ VM=1 Institution Code Sequence</summary>
		public static DicomTag InstitutionCodeSequence = new DicomTag(0x0008, 0x0082);

		/// <summary>(0008,0090) VR=PN VM=1 Referring Physician's Name</summary>
		public static DicomTag ReferringPhysiciansName = new DicomTag(0x0008, 0x0090);

		/// <summary>(0008,0092) VR=ST VM=1 Referring Physician's Address</summary>
		public static DicomTag ReferringPhysiciansAddress = new DicomTag(0x0008, 0x0092);

		/// <summary>(0008,0094) VR=SH VM=1-n Referring Physician's Telephone Numbers</summary>
		public static DicomTag ReferringPhysiciansTelephoneNumbers = new DicomTag(0x0008, 0x0094);

		/// <summary>(0008,0096) VR=SQ VM=1 Referring Physician Identification Sequence</summary>
		public static DicomTag ReferringPhysicianIdentificationSequence = new DicomTag(0x0008, 0x0096);

		/// <summary>(0008,0100) VR=SH VM=1 Code Value</summary>
		public static DicomTag CodeValue = new DicomTag(0x0008, 0x0100);

		/// <summary>(0008,0102) VR=SH VM=1 Coding Scheme Designator</summary>
		public static DicomTag CodingSchemeDesignator = new DicomTag(0x0008, 0x0102);

		/// <summary>(0008,0103) VR=SH VM=1 Coding Scheme Version</summary>
		public static DicomTag CodingSchemeVersion = new DicomTag(0x0008, 0x0103);

		/// <summary>(0008,0104) VR=LO VM=1 Code Meaning</summary>
		public static DicomTag CodeMeaning = new DicomTag(0x0008, 0x0104);

		/// <summary>(0008,0105) VR=CS VM=1 Mapping Resource</summary>
		public static DicomTag MappingResource = new DicomTag(0x0008, 0x0105);

		/// <summary>(0008,0106) VR=DT VM=1 Context Group Version</summary>
		public static DicomTag ContextGroupVersion = new DicomTag(0x0008, 0x0106);

		/// <summary>(0008,0107) VR=DT VM=1 Context Group Local Version</summary>
		public static DicomTag ContextGroupLocalVersion = new DicomTag(0x0008, 0x0107);

		/// <summary>(0008,010b) VR=CS VM=1 Context Group Extension Flag</summary>
		public static DicomTag ContextGroupExtensionFlag = new DicomTag(0x0008, 0x010b);

		/// <summary>(0008,010c) VR=UI VM=1 Coding Scheme UID</summary>
		public static DicomTag CodingSchemeUID = new DicomTag(0x0008, 0x010c);

		/// <summary>(0008,010d) VR=UI VM=1 Context Group Extension Creator UID</summary>
		public static DicomTag ContextGroupExtensionCreatorUID = new DicomTag(0x0008, 0x010d);

		/// <summary>(0008,010f) VR=CS VM=1 Context Identifier</summary>
		public static DicomTag ContextIdentifier = new DicomTag(0x0008, 0x010f);

		/// <summary>(0008,0110) VR=SQ VM=1 Coding Scheme Identification Sequence</summary>
		public static DicomTag CodingSchemeIdentificationSequence = new DicomTag(0x0008, 0x0110);

		/// <summary>(0008,0112) VR=LO VM=1 Coding Scheme Registry</summary>
		public static DicomTag CodingSchemeRegistry = new DicomTag(0x0008, 0x0112);

		/// <summary>(0008,0114) VR=ST VM=1 Coding Scheme External ID</summary>
		public static DicomTag CodingSchemeExternalID = new DicomTag(0x0008, 0x0114);

		/// <summary>(0008,0115) VR=ST VM=1 Coding Scheme Name</summary>
		public static DicomTag CodingSchemeName = new DicomTag(0x0008, 0x0115);

		/// <summary>(0008,0116) VR=ST VM=1 Coding Scheme Responsible Organization</summary>
		public static DicomTag CodingSchemeResponsibleOrganization = new DicomTag(0x0008, 0x0116);

		/// <summary>(0008,0201) VR=SH VM=1 Timezone Offset From UTC</summary>
		public static DicomTag TimezoneOffsetFromUTC = new DicomTag(0x0008, 0x0201);

		/// <summary>(0008,1000) VR=AE VM=1 Network ID (Retired)</summary>
		public static DicomTag NetworkIDRETIRED = new DicomTag(0x0008, 0x1000);

		/// <summary>(0008,1010) VR=SH VM=1 Station Name</summary>
		public static DicomTag StationName = new DicomTag(0x0008, 0x1010);

		/// <summary>(0008,1030) VR=LO VM=1 Study Description</summary>
		public static DicomTag StudyDescription = new DicomTag(0x0008, 0x1030);

		/// <summary>(0008,1032) VR=SQ VM=1 Procedure Code Sequence</summary>
		public static DicomTag ProcedureCodeSequence = new DicomTag(0x0008, 0x1032);

		/// <summary>(0008,103e) VR=LO VM=1 Series Description</summary>
		public static DicomTag SeriesDescription = new DicomTag(0x0008, 0x103e);

		/// <summary>(0008,1040) VR=LO VM=1 Institutional Department Name</summary>
		public static DicomTag InstitutionalDepartmentName = new DicomTag(0x0008, 0x1040);

		/// <summary>(0008,1048) VR=PN VM=1-n Physician(s) of Record</summary>
		public static DicomTag PhysiciansOfRecord = new DicomTag(0x0008, 0x1048);

		/// <summary>(0008,1049) VR=SQ VM=1 Physician(s) of Record Identification Sequence</summary>
		public static DicomTag PhysiciansOfRecordIdentificationSequence = new DicomTag(0x0008, 0x1049);

		/// <summary>(0008,1050) VR=PN VM=1-n Performing Physician's Name</summary>
		public static DicomTag PerformingPhysiciansName = new DicomTag(0x0008, 0x1050);

		/// <summary>(0008,1052) VR=SQ VM=1 Performing Physician Identification Sequence</summary>
		public static DicomTag PerformingPhysicianIdentificationSequence = new DicomTag(0x0008, 0x1052);

		/// <summary>(0008,1060) VR=PN VM=1-n Name of Physician(s) Reading Study</summary>
		public static DicomTag NameOfPhysiciansReadingStudy = new DicomTag(0x0008, 0x1060);

		/// <summary>(0008,1062) VR=SQ VM=1 Physician(s) Reading Study Identification Sequence</summary>
		public static DicomTag PhysiciansReadingStudyIdentificationSequence = new DicomTag(0x0008, 0x1062);

		/// <summary>(0008,1070) VR=PN VM=1-n Operators' Name</summary>
		public static DicomTag OperatorsName = new DicomTag(0x0008, 0x1070);

		/// <summary>(0008,1072) VR=SQ VM=1 Operator Identification Sequence</summary>
		public static DicomTag OperatorIdentificationSequence = new DicomTag(0x0008, 0x1072);

		/// <summary>(0008,1080) VR=LO VM=1-n Admitting Diagnoses Description</summary>
		public static DicomTag AdmittingDiagnosesDescription = new DicomTag(0x0008, 0x1080);

		/// <summary>(0008,1084) VR=SQ VM=1 Admitting Diagnoses Code Sequence</summary>
		public static DicomTag AdmittingDiagnosesCodeSequence = new DicomTag(0x0008, 0x1084);

		/// <summary>(0008,1090) VR=LO VM=1 Manufacturer's Model Name</summary>
		public static DicomTag ManufacturersModelName = new DicomTag(0x0008, 0x1090);

		/// <summary>(0008,1100) VR=SQ VM=1 Referenced Results Sequence (Retired)</summary>
		public static DicomTag ReferencedResultsSequenceRETIRED = new DicomTag(0x0008, 0x1100);

		/// <summary>(0008,1110) VR=SQ VM=1 Referenced Study Sequence</summary>
		public static DicomTag ReferencedStudySequence = new DicomTag(0x0008, 0x1110);

		/// <summary>(0008,1111) VR=SQ VM=1 Referenced Performed Procedure Step Sequence</summary>
		public static DicomTag ReferencedPerformedProcedureStepSequence = new DicomTag(0x0008, 0x1111);

		/// <summary>(0008,1115) VR=SQ VM=1 Referenced Series Sequence</summary>
		public static DicomTag ReferencedSeriesSequence = new DicomTag(0x0008, 0x1115);

		/// <summary>(0008,1120) VR=SQ VM=1 Referenced Patient Sequence</summary>
		public static DicomTag ReferencedPatientSequence = new DicomTag(0x0008, 0x1120);

		/// <summary>(0008,1125) VR=SQ VM=1 Referenced Visit Sequence</summary>
		public static DicomTag ReferencedVisitSequence = new DicomTag(0x0008, 0x1125);

		/// <summary>(0008,1130) VR=SQ VM=1 Referenced Overlay Sequence (Retired)</summary>
		public static DicomTag ReferencedOverlaySequenceRETIRED = new DicomTag(0x0008, 0x1130);

		/// <summary>(0008,113a) VR=SQ VM=1 Referenced Waveform Sequence</summary>
		public static DicomTag ReferencedWaveformSequence = new DicomTag(0x0008, 0x113a);

		/// <summary>(0008,1140) VR=SQ VM=1 Referenced Image Sequence</summary>
		public static DicomTag ReferencedImageSequence = new DicomTag(0x0008, 0x1140);

		/// <summary>(0008,1145) VR=SQ VM=1 Referenced Curve Sequence (Retired)</summary>
		public static DicomTag ReferencedCurveSequenceRETIRED = new DicomTag(0x0008, 0x1145);

		/// <summary>(0008,114a) VR=SQ VM=1 Referenced Instance Sequence</summary>
		public static DicomTag ReferencedInstanceSequence = new DicomTag(0x0008, 0x114a);

		/// <summary>(0008,114b) VR=SQ VM=1 Referenced Real World Value Mapping Instance Sequence</summary>
		public static DicomTag ReferencedRealWorldValueMappingInstanceSequence = new DicomTag(0x0008, 0x114b);

		/// <summary>(0008,1150) VR=UI VM=1 Referenced SOP Class UID</summary>
		public static DicomTag ReferencedSOPClassUID = new DicomTag(0x0008, 0x1150);

		/// <summary>(0008,1155) VR=UI VM=1 Referenced SOP Instance UID</summary>
		public static DicomTag ReferencedSOPInstanceUID = new DicomTag(0x0008, 0x1155);

		/// <summary>(0008,115a) VR=UI VM=1-n SOP Classes Supported</summary>
		public static DicomTag SOPClassesSupported = new DicomTag(0x0008, 0x115a);

		/// <summary>(0008,1160) VR=IS VM=1-n Referenced Frame Number</summary>
		public static DicomTag ReferencedFrameNumber = new DicomTag(0x0008, 0x1160);

		/// <summary>(0008,1195) VR=UI VM=1 Transaction UID</summary>
		public static DicomTag TransactionUID = new DicomTag(0x0008, 0x1195);

		/// <summary>(0008,1197) VR=US VM=1 Failure Reason</summary>
		public static DicomTag FailureReason = new DicomTag(0x0008, 0x1197);

		/// <summary>(0008,1198) VR=SQ VM=1 Failed SOP Sequence</summary>
		public static DicomTag FailedSOPSequence = new DicomTag(0x0008, 0x1198);

		/// <summary>(0008,1199) VR=SQ VM=1 Referenced SOP Sequence</summary>
		public static DicomTag ReferencedSOPSequence = new DicomTag(0x0008, 0x1199);

		/// <summary>(0008,1200) VR=SQ VM=1 Studies Containing Other Referenced Instances Sequence</summary>
		public static DicomTag StudiesContainingOtherReferencedInstancesSequence = new DicomTag(0x0008, 0x1200);

		/// <summary>(0008,1250) VR=SQ VM=1 Related Series Sequence</summary>
		public static DicomTag RelatedSeriesSequence = new DicomTag(0x0008, 0x1250);

		/// <summary>(0008,2110) VR=CS VM=1 Lossy Image Compression (Retired) (Retired)</summary>
		public static DicomTag LossyImageCompressionRetiredRETIRED = new DicomTag(0x0008, 0x2110);

		/// <summary>(0008,2111) VR=ST VM=1 Derivation Description</summary>
		public static DicomTag DerivationDescription = new DicomTag(0x0008, 0x2111);

		/// <summary>(0008,2112) VR=SQ VM=1 Source Image Sequence</summary>
		public static DicomTag SourceImageSequence = new DicomTag(0x0008, 0x2112);

		/// <summary>(0008,2120) VR=SH VM=1 Stage Name</summary>
		public static DicomTag StageName = new DicomTag(0x0008, 0x2120);

		/// <summary>(0008,2122) VR=IS VM=1 Stage Number</summary>
		public static DicomTag StageNumber = new DicomTag(0x0008, 0x2122);

		/// <summary>(0008,2124) VR=IS VM=1 Number of Stages</summary>
		public static DicomTag NumberOfStages = new DicomTag(0x0008, 0x2124);

		/// <summary>(0008,2127) VR=SH VM=1 View Name</summary>
		public static DicomTag ViewName = new DicomTag(0x0008, 0x2127);

		/// <summary>(0008,2128) VR=IS VM=1 View Number</summary>
		public static DicomTag ViewNumber = new DicomTag(0x0008, 0x2128);

		/// <summary>(0008,2129) VR=IS VM=1 Number of Event Timers</summary>
		public static DicomTag NumberOfEventTimers = new DicomTag(0x0008, 0x2129);

		/// <summary>(0008,212a) VR=IS VM=1 Number of Views in Stage</summary>
		public static DicomTag NumberOfViewsInStage = new DicomTag(0x0008, 0x212a);

		/// <summary>(0008,2130) VR=DS VM=1-n Event Elapsed Time(s)</summary>
		public static DicomTag EventElapsedTimes = new DicomTag(0x0008, 0x2130);

		/// <summary>(0008,2132) VR=LO VM=1-n Event Timer Name(s)</summary>
		public static DicomTag EventTimerNames = new DicomTag(0x0008, 0x2132);

		/// <summary>(0008,2142) VR=IS VM=1 Start Trim</summary>
		public static DicomTag StartTrim = new DicomTag(0x0008, 0x2142);

		/// <summary>(0008,2143) VR=IS VM=1 Stop Trim</summary>
		public static DicomTag StopTrim = new DicomTag(0x0008, 0x2143);

		/// <summary>(0008,2144) VR=IS VM=1 Recommended Display Frame Rate</summary>
		public static DicomTag RecommendedDisplayFrameRate = new DicomTag(0x0008, 0x2144);

		/// <summary>(0008,2200) VR=CS VM=1 Transducer Position (Retired)</summary>
		public static DicomTag TransducerPositionRETIRED = new DicomTag(0x0008, 0x2200);

		/// <summary>(0008,2204) VR=CS VM=1 Transducer Orientation (Retired)</summary>
		public static DicomTag TransducerOrientationRETIRED = new DicomTag(0x0008, 0x2204);

		/// <summary>(0008,2208) VR=CS VM=1 Anatomic Structure (Retired)</summary>
		public static DicomTag AnatomicStructureRETIRED = new DicomTag(0x0008, 0x2208);

		/// <summary>(0008,2218) VR=SQ VM=1 Anatomic Region Sequence</summary>
		public static DicomTag AnatomicRegionSequence = new DicomTag(0x0008, 0x2218);

		/// <summary>(0008,2220) VR=SQ VM=1 Anatomic Region Modifier Sequence</summary>
		public static DicomTag AnatomicRegionModifierSequence = new DicomTag(0x0008, 0x2220);

		/// <summary>(0008,2228) VR=SQ VM=1 Primary Anatomic Structure Sequence</summary>
		public static DicomTag PrimaryAnatomicStructureSequence = new DicomTag(0x0008, 0x2228);

		/// <summary>(0008,2229) VR=SQ VM=1 Anatomic Structure, Space or Region Sequence</summary>
		public static DicomTag AnatomicStructureSpaceOrRegionSequence = new DicomTag(0x0008, 0x2229);

		/// <summary>(0008,2230) VR=SQ VM=1 Primary Anatomic Structure Modifier Sequence</summary>
		public static DicomTag PrimaryAnatomicStructureModifierSequence = new DicomTag(0x0008, 0x2230);

		/// <summary>(0008,2240) VR=SQ VM=1 Transducer Position Sequence (Retired)</summary>
		public static DicomTag TransducerPositionSequenceRETIRED = new DicomTag(0x0008, 0x2240);

		/// <summary>(0008,2242) VR=SQ VM=1 Transducer Position Modifier Sequence (Retired)</summary>
		public static DicomTag TransducerPositionModifierSequenceRETIRED = new DicomTag(0x0008, 0x2242);

		/// <summary>(0008,2244) VR=SQ VM=1 Transducer Orientation Sequence (Retired)</summary>
		public static DicomTag TransducerOrientationSequenceRETIRED = new DicomTag(0x0008, 0x2244);

		/// <summary>(0008,2246) VR=SQ VM=1 Transducer Orientation Modifier Sequence (Retired)</summary>
		public static DicomTag TransducerOrientationModifierSequenceRETIRED = new DicomTag(0x0008, 0x2246);

		/// <summary>(0008,2251) VR=SQ VM=1 Anatomic Structure Space Or Region Code Sequence (Trial) (Retired)</summary>
		public static DicomTag AnatomicStructureSpaceOrRegionCodeSequenceTrialRETIRED = new DicomTag(0x0008, 0x2251);

		/// <summary>(0008,2253) VR=SQ VM=1 Anatomic Portal Of Entrance Code Sequence (Trial) (Retired)</summary>
		public static DicomTag AnatomicPortalOfEntranceCodeSequenceTrialRETIRED = new DicomTag(0x0008, 0x2253);

		/// <summary>(0008,2255) VR=SQ VM=1 Anatomic Approach Direction Code Sequence (Trial) (Retired)</summary>
		public static DicomTag AnatomicApproachDirectionCodeSequenceTrialRETIRED = new DicomTag(0x0008, 0x2255);

		/// <summary>(0008,2256) VR=ST VM=1 Anatomic Perspective Description (Trial) (Retired)</summary>
		public static DicomTag AnatomicPerspectiveDescriptionTrialRETIRED = new DicomTag(0x0008, 0x2256);

		/// <summary>(0008,2257) VR=SQ VM=1 Anatomic Perspective Code Sequence (Trial) (Retired)</summary>
		public static DicomTag AnatomicPerspectiveCodeSequenceTrialRETIRED = new DicomTag(0x0008, 0x2257);

		/// <summary>(0008,2258) VR=ST VM=1 Anatomic Location Of Examining Instrument Description (Trial) (Retired)</summary>
		public static DicomTag AnatomicLocationOfExaminingInstrumentDescriptionTrialRETIRED = new DicomTag(0x0008, 0x2258);

		/// <summary>(0008,2259) VR=SQ VM=1 Anatomic Location Of Examining Instrument Code Sequence (Trial) (Retired)</summary>
		public static DicomTag AnatomicLocationOfExaminingInstrumentCodeSequenceTrialRETIRED = new DicomTag(0x0008, 0x2259);

		/// <summary>(0008,225a) VR=SQ VM=1 Anatomic Structure Space Or Region Modifier Code Sequence (Trial) (Retired)</summary>
		public static DicomTag AnatomicStructureSpaceOrRegionModifierCodeSequenceTrialRETIRED = new DicomTag(0x0008, 0x225a);

		/// <summary>(0008,225c) VR=SQ VM=1 OnAxis Background Anatomic Structure Code Sequence (Trial) (Retired)</summary>
		public static DicomTag OnAxisBackgroundAnatomicStructureCodeSequenceTrialRETIRED = new DicomTag(0x0008, 0x225c);

		/// <summary>(0008,3001) VR=SQ VM=1 Alternate Representation Sequence</summary>
		public static DicomTag AlternateRepresentationSequence = new DicomTag(0x0008, 0x3001);

		/// <summary>(0008,3010) VR=UI VM=1 Irradiation Event UID</summary>
		public static DicomTag IrradiationEventUID = new DicomTag(0x0008, 0x3010);

		/// <summary>(0008,4000) VR=LT VM=1 Identifying Comments (Retired)</summary>
		public static DicomTag IdentifyingCommentsRETIRED = new DicomTag(0x0008, 0x4000);

		/// <summary>(0008,9007) VR=CS VM=4 Frame Type</summary>
		public static DicomTag FrameType = new DicomTag(0x0008, 0x9007);

		/// <summary>(0008,9092) VR=SQ VM=1 Referenced Image Evidence Sequence</summary>
		public static DicomTag ReferencedImageEvidenceSequence = new DicomTag(0x0008, 0x9092);

		/// <summary>(0008,9121) VR=SQ VM=1 Referenced Raw Data Sequence</summary>
		public static DicomTag ReferencedRawDataSequence = new DicomTag(0x0008, 0x9121);

		/// <summary>(0008,9123) VR=UI VM=1 Creator-Version UID</summary>
		public static DicomTag CreatorVersionUID = new DicomTag(0x0008, 0x9123);

		/// <summary>(0008,9124) VR=SQ VM=1 Derivation Image Sequence</summary>
		public static DicomTag DerivationImageSequence = new DicomTag(0x0008, 0x9124);

		/// <summary>(0008,9154) VR=SQ VM=1 Source Image Evidence Sequence</summary>
		public static DicomTag SourceImageEvidenceSequence = new DicomTag(0x0008, 0x9154);

		/// <summary>(0008,9205) VR=CS VM=1 Pixel Presentation</summary>
		public static DicomTag PixelPresentation = new DicomTag(0x0008, 0x9205);

		/// <summary>(0008,9206) VR=CS VM=1 Volumetric Properties</summary>
		public static DicomTag VolumetricProperties = new DicomTag(0x0008, 0x9206);

		/// <summary>(0008,9207) VR=CS VM=1 Volume Based Calculation Technique</summary>
		public static DicomTag VolumeBasedCalculationTechnique = new DicomTag(0x0008, 0x9207);

		/// <summary>(0008,9208) VR=CS VM=1 Complex Image Component</summary>
		public static DicomTag ComplexImageComponent = new DicomTag(0x0008, 0x9208);

		/// <summary>(0008,9209) VR=CS VM=1 Acquisition Contrast</summary>
		public static DicomTag AcquisitionContrast = new DicomTag(0x0008, 0x9209);

		/// <summary>(0008,9215) VR=SQ VM=1 Derivation Code Sequence</summary>
		public static DicomTag DerivationCodeSequence = new DicomTag(0x0008, 0x9215);

		/// <summary>(0008,9237) VR=SQ VM=1 Referenced Grayscale Presentation State Sequence</summary>
		public static DicomTag ReferencedGrayscalePresentationStateSequence = new DicomTag(0x0008, 0x9237);

		/// <summary>(0008,9410) VR=SQ VM=1 Referenced Other Plane Sequence</summary>
		public static DicomTag ReferencedOtherPlaneSequence = new DicomTag(0x0008, 0x9410);

		/// <summary>(0008,9458) VR=SQ VM=1 Frame Display Sequence</summary>
		public static DicomTag FrameDisplaySequence = new DicomTag(0x0008, 0x9458);

		/// <summary>(0008,9459) VR=FL VM=1 Recommended Display Frame Rate in Float</summary>
		public static DicomTag RecommendedDisplayFrameRateInFloat = new DicomTag(0x0008, 0x9459);

		/// <summary>(0008,9460) VR=CS VM=1 Skip Frame Range Flag</summary>
		public static DicomTag SkipFrameRangeFlag = new DicomTag(0x0008, 0x9460);

		/// <summary>(0010,0010) VR=PN VM=1 Patient's Name</summary>
		public static DicomTag PatientsName = new DicomTag(0x0010, 0x0010);

		/// <summary>(0010,0020) VR=LO VM=1 Patient ID</summary>
		public static DicomTag PatientID = new DicomTag(0x0010, 0x0020);

		/// <summary>(0010,0021) VR=LO VM=1 Issuer of Patient ID</summary>
		public static DicomTag IssuerOfPatientID = new DicomTag(0x0010, 0x0021);

		/// <summary>(0010,0022) VR=CS VM=1 Type of Patient ID</summary>
		public static DicomTag TypeOfPatientID = new DicomTag(0x0010, 0x0022);

		/// <summary>(0010,0030) VR=DA VM=1 Patient's Birth Date</summary>
		public static DicomTag PatientsBirthDate = new DicomTag(0x0010, 0x0030);

		/// <summary>(0010,0032) VR=TM VM=1 Patient's Birth Time</summary>
		public static DicomTag PatientsBirthTime = new DicomTag(0x0010, 0x0032);

		/// <summary>(0010,0040) VR=CS VM=1 Patient's Sex</summary>
		public static DicomTag PatientsSex = new DicomTag(0x0010, 0x0040);

		/// <summary>(0010,0050) VR=SQ VM=1 Patient's Insurance Plan Code Sequence</summary>
		public static DicomTag PatientsInsurancePlanCodeSequence = new DicomTag(0x0010, 0x0050);

		/// <summary>(0010,0101) VR=SQ VM=1 Patient's Primary Language Code Sequence</summary>
		public static DicomTag PatientsPrimaryLanguageCodeSequence = new DicomTag(0x0010, 0x0101);

		/// <summary>(0010,0102) VR=SQ VM=1 Patient's Primary Language Code Modifier Sequence</summary>
		public static DicomTag PatientsPrimaryLanguageCodeModifierSequence = new DicomTag(0x0010, 0x0102);

		/// <summary>(0010,1000) VR=LO VM=1-n Other Patient IDs</summary>
		public static DicomTag OtherPatientIDs = new DicomTag(0x0010, 0x1000);

		/// <summary>(0010,1001) VR=PN VM=1-n Other Patient Names</summary>
		public static DicomTag OtherPatientNames = new DicomTag(0x0010, 0x1001);

		/// <summary>(0010,1002) VR=SQ VM=1 Other Patient IDs Sequence</summary>
		public static DicomTag OtherPatientIDsSequence = new DicomTag(0x0010, 0x1002);

		/// <summary>(0010,1005) VR=PN VM=1 Patient's Birth Name</summary>
		public static DicomTag PatientsBirthName = new DicomTag(0x0010, 0x1005);

		/// <summary>(0010,1010) VR=AS VM=1 Patient's Age</summary>
		public static DicomTag PatientsAge = new DicomTag(0x0010, 0x1010);

		/// <summary>(0010,1020) VR=DS VM=1 Patient's Size</summary>
		public static DicomTag PatientsSize = new DicomTag(0x0010, 0x1020);

		/// <summary>(0010,1030) VR=DS VM=1 Patient's Weight</summary>
		public static DicomTag PatientsWeight = new DicomTag(0x0010, 0x1030);

		/// <summary>(0010,1040) VR=LO VM=1 Patient's Address</summary>
		public static DicomTag PatientsAddress = new DicomTag(0x0010, 0x1040);

		/// <summary>(0010,1050) VR=LO VM=1-n Insurance Plan Identification (Retired)</summary>
		public static DicomTag InsurancePlanIdentificationRETIRED = new DicomTag(0x0010, 0x1050);

		/// <summary>(0010,1060) VR=PN VM=1 Patient's Mother's Birth Name</summary>
		public static DicomTag PatientsMothersBirthName = new DicomTag(0x0010, 0x1060);

		/// <summary>(0010,1080) VR=LO VM=1 Military Rank</summary>
		public static DicomTag MilitaryRank = new DicomTag(0x0010, 0x1080);

		/// <summary>(0010,1081) VR=LO VM=1 Branch of Service</summary>
		public static DicomTag BranchOfService = new DicomTag(0x0010, 0x1081);

		/// <summary>(0010,1090) VR=LO VM=1 Medical Record Locator</summary>
		public static DicomTag MedicalRecordLocator = new DicomTag(0x0010, 0x1090);

		/// <summary>(0010,2000) VR=LO VM=1-n Medical Alerts</summary>
		public static DicomTag MedicalAlerts = new DicomTag(0x0010, 0x2000);

		/// <summary>(0010,2110) VR=LO VM=1-n Allergies</summary>
		public static DicomTag Allergies = new DicomTag(0x0010, 0x2110);

		/// <summary>(0010,2150) VR=LO VM=1 Country of Residence</summary>
		public static DicomTag CountryOfResidence = new DicomTag(0x0010, 0x2150);

		/// <summary>(0010,2152) VR=LO VM=1 Region of Residence</summary>
		public static DicomTag RegionOfResidence = new DicomTag(0x0010, 0x2152);

		/// <summary>(0010,2154) VR=SH VM=1-n Patient's Telephone Numbers</summary>
		public static DicomTag PatientsTelephoneNumbers = new DicomTag(0x0010, 0x2154);

		/// <summary>(0010,2160) VR=SH VM=1 Ethnic Group</summary>
		public static DicomTag EthnicGroup = new DicomTag(0x0010, 0x2160);

		/// <summary>(0010,2180) VR=SH VM=1 Occupation</summary>
		public static DicomTag Occupation = new DicomTag(0x0010, 0x2180);

		/// <summary>(0010,21a0) VR=CS VM=1 Smoking Status</summary>
		public static DicomTag SmokingStatus = new DicomTag(0x0010, 0x21a0);

		/// <summary>(0010,21b0) VR=LT VM=1 Additional Patient History</summary>
		public static DicomTag AdditionalPatientHistory = new DicomTag(0x0010, 0x21b0);

		/// <summary>(0010,21c0) VR=US VM=1 Pregnancy Status</summary>
		public static DicomTag PregnancyStatus = new DicomTag(0x0010, 0x21c0);

		/// <summary>(0010,21d0) VR=DA VM=1 Last Menstrual Date</summary>
		public static DicomTag LastMenstrualDate = new DicomTag(0x0010, 0x21d0);

		/// <summary>(0010,21f0) VR=LO VM=1 Patient's Religious Preference</summary>
		public static DicomTag PatientsReligiousPreference = new DicomTag(0x0010, 0x21f0);

		/// <summary>(0010,2201) VR=LO VM=1 Patient Species Description</summary>
		public static DicomTag PatientSpeciesDescription = new DicomTag(0x0010, 0x2201);

		/// <summary>(0010,2202) VR=SQ VM=1 Patient Species Code Sequence</summary>
		public static DicomTag PatientSpeciesCodeSequence = new DicomTag(0x0010, 0x2202);

		/// <summary>(0010,2203) VR=CS VM=1 Patient's Sex Neutered</summary>
		public static DicomTag PatientsSexNeutered = new DicomTag(0x0010, 0x2203);

		/// <summary>(0010,2292) VR=LO VM=1 Patient Breed Description</summary>
		public static DicomTag PatientBreedDescription = new DicomTag(0x0010, 0x2292);

		/// <summary>(0010,2293) VR=SQ VM=1 Patient Breed Code Sequence</summary>
		public static DicomTag PatientBreedCodeSequence = new DicomTag(0x0010, 0x2293);

		/// <summary>(0010,2294) VR=SQ VM=1 Breed Registration Sequence</summary>
		public static DicomTag BreedRegistrationSequence = new DicomTag(0x0010, 0x2294);

		/// <summary>(0010,2295) VR=LO VM=1 Breed Registration Number</summary>
		public static DicomTag BreedRegistrationNumber = new DicomTag(0x0010, 0x2295);

		/// <summary>(0010,2296) VR=SQ VM=1 Breed Registry Code Sequence</summary>
		public static DicomTag BreedRegistryCodeSequence = new DicomTag(0x0010, 0x2296);

		/// <summary>(0010,2297) VR=PN VM=1 Responsible Person</summary>
		public static DicomTag ResponsiblePerson = new DicomTag(0x0010, 0x2297);

		/// <summary>(0010,2298) VR=CS VM=1 Responsible Person Role</summary>
		public static DicomTag ResponsiblePersonRole = new DicomTag(0x0010, 0x2298);

		/// <summary>(0010,2299) VR=LO VM=1 Responsible Organization</summary>
		public static DicomTag ResponsibleOrganization = new DicomTag(0x0010, 0x2299);

		/// <summary>(0010,4000) VR=LT VM=1 Patient Comments</summary>
		public static DicomTag PatientComments = new DicomTag(0x0010, 0x4000);

		/// <summary>(0010,9431) VR=FL VM=1 Examined Body Thickness</summary>
		public static DicomTag ExaminedBodyThickness = new DicomTag(0x0010, 0x9431);

		/// <summary>(0012,0010) VR=LO VM=1 Clinical Trial Sponsor Name</summary>
		public static DicomTag ClinicalTrialSponsorName = new DicomTag(0x0012, 0x0010);

		/// <summary>(0012,0020) VR=LO VM=1 Clinical Trial Protocol ID</summary>
		public static DicomTag ClinicalTrialProtocolID = new DicomTag(0x0012, 0x0020);

		/// <summary>(0012,0021) VR=LO VM=1 Clinical Trial Protocol Name</summary>
		public static DicomTag ClinicalTrialProtocolName = new DicomTag(0x0012, 0x0021);

		/// <summary>(0012,0030) VR=LO VM=1 Clinical Trial Site ID</summary>
		public static DicomTag ClinicalTrialSiteID = new DicomTag(0x0012, 0x0030);

		/// <summary>(0012,0031) VR=LO VM=1 Clinical Trial Site Name</summary>
		public static DicomTag ClinicalTrialSiteName = new DicomTag(0x0012, 0x0031);

		/// <summary>(0012,0040) VR=LO VM=1 Clinical Trial Subject ID</summary>
		public static DicomTag ClinicalTrialSubjectID = new DicomTag(0x0012, 0x0040);

		/// <summary>(0012,0042) VR=LO VM=1 Clinical Trial Subject Reading ID</summary>
		public static DicomTag ClinicalTrialSubjectReadingID = new DicomTag(0x0012, 0x0042);

		/// <summary>(0012,0050) VR=LO VM=1 Clinical Trial Time Point ID</summary>
		public static DicomTag ClinicalTrialTimePointID = new DicomTag(0x0012, 0x0050);

		/// <summary>(0012,0051) VR=ST VM=1 Clinical Trial Time Point Description</summary>
		public static DicomTag ClinicalTrialTimePointDescription = new DicomTag(0x0012, 0x0051);

		/// <summary>(0012,0060) VR=LO VM=1 Clinical Trial Coordinating Center Name</summary>
		public static DicomTag ClinicalTrialCoordinatingCenterName = new DicomTag(0x0012, 0x0060);

		/// <summary>(0012,0062) VR=CS VM=1 Patient Identity Removed</summary>
		public static DicomTag PatientIdentityRemoved = new DicomTag(0x0012, 0x0062);

		/// <summary>(0012,0063) VR=LO VM=1-n De-identification Method</summary>
		public static DicomTag DeidentificationMethod = new DicomTag(0x0012, 0x0063);

		/// <summary>(0012,0064) VR=SQ VM=1 De-identification Method Code Sequence</summary>
		public static DicomTag DeidentificationMethodCodeSequence = new DicomTag(0x0012, 0x0064);

		/// <summary>(0012,0071) VR=LO VM=1 Clinical Trial Series ID</summary>
		public static DicomTag ClinicalTrialSeriesID = new DicomTag(0x0012, 0x0071);

		/// <summary>(0012,0072) VR=LO VM=1 Clinical Trial Series Description</summary>
		public static DicomTag ClinicalTrialSeriesDescription = new DicomTag(0x0012, 0x0072);

		/// <summary>(0018,0010) VR=LO VM=1 Contrast/Bolus Agent</summary>
		public static DicomTag ContrastBolusAgent = new DicomTag(0x0018, 0x0010);

		/// <summary>(0018,0012) VR=SQ VM=1 Contrast/Bolus Agent Sequence</summary>
		public static DicomTag ContrastBolusAgentSequence = new DicomTag(0x0018, 0x0012);

		/// <summary>(0018,0014) VR=SQ VM=1 Contrast/Bolus Administration Route Sequence</summary>
		public static DicomTag ContrastBolusAdministrationRouteSequence = new DicomTag(0x0018, 0x0014);

		/// <summary>(0018,0015) VR=CS VM=1 Body Part Examined</summary>
		public static DicomTag BodyPartExamined = new DicomTag(0x0018, 0x0015);

		/// <summary>(0018,0020) VR=CS VM=1-n Scanning Sequence</summary>
		public static DicomTag ScanningSequence = new DicomTag(0x0018, 0x0020);

		/// <summary>(0018,0021) VR=CS VM=1-n Sequence Variant</summary>
		public static DicomTag SequenceVariant = new DicomTag(0x0018, 0x0021);

		/// <summary>(0018,0022) VR=CS VM=1-n Scan Options</summary>
		public static DicomTag ScanOptions = new DicomTag(0x0018, 0x0022);

		/// <summary>(0018,0023) VR=CS VM=1 MR Acquisition Type</summary>
		public static DicomTag MRAcquisitionType = new DicomTag(0x0018, 0x0023);

		/// <summary>(0018,0024) VR=SH VM=1 Sequence Name</summary>
		public static DicomTag SequenceName = new DicomTag(0x0018, 0x0024);

		/// <summary>(0018,0025) VR=CS VM=1 Angio Flag</summary>
		public static DicomTag AngioFlag = new DicomTag(0x0018, 0x0025);

		/// <summary>(0018,0026) VR=SQ VM=1 Intervention Drug Information Sequence</summary>
		public static DicomTag InterventionDrugInformationSequence = new DicomTag(0x0018, 0x0026);

		/// <summary>(0018,0027) VR=TM VM=1 Intervention Drug Stop Time</summary>
		public static DicomTag InterventionDrugStopTime = new DicomTag(0x0018, 0x0027);

		/// <summary>(0018,0028) VR=DS VM=1 Intervention Drug Dose</summary>
		public static DicomTag InterventionDrugDose = new DicomTag(0x0018, 0x0028);

		/// <summary>(0018,0029) VR=SQ VM=1 Intervention Drug Sequence</summary>
		public static DicomTag InterventionDrugSequence = new DicomTag(0x0018, 0x0029);

		/// <summary>(0018,002a) VR=SQ VM=1 Additional Drug Sequence</summary>
		public static DicomTag AdditionalDrugSequence = new DicomTag(0x0018, 0x002a);

		/// <summary>(0018,0030) VR=LO VM=1-n Radionuclide (Retired)</summary>
		public static DicomTag RadionuclideRETIRED = new DicomTag(0x0018, 0x0030);

		/// <summary>(0018,0031) VR=LO VM=1 Radiopharmaceutical</summary>
		public static DicomTag Radiopharmaceutical = new DicomTag(0x0018, 0x0031);

		/// <summary>(0018,0032) VR=DS VM=1 Energy Window Centerline (Retired)</summary>
		public static DicomTag EnergyWindowCenterlineRETIRED = new DicomTag(0x0018, 0x0032);

		/// <summary>(0018,0033) VR=DS VM=1-n Energy Window Total Width (Retired)</summary>
		public static DicomTag EnergyWindowTotalWidthRETIRED = new DicomTag(0x0018, 0x0033);

		/// <summary>(0018,0034) VR=LO VM=1 Intervention Drug Name</summary>
		public static DicomTag InterventionDrugName = new DicomTag(0x0018, 0x0034);

		/// <summary>(0018,0035) VR=TM VM=1 Intervention Drug Start Time</summary>
		public static DicomTag InterventionDrugStartTime = new DicomTag(0x0018, 0x0035);

		/// <summary>(0018,0036) VR=SQ VM=1 Intervention Sequence</summary>
		public static DicomTag InterventionSequence = new DicomTag(0x0018, 0x0036);

		/// <summary>(0018,0037) VR=CS VM=1 Therapy Type (Retired)</summary>
		public static DicomTag TherapyTypeRETIRED = new DicomTag(0x0018, 0x0037);

		/// <summary>(0018,0038) VR=CS VM=1 Intervention Status</summary>
		public static DicomTag InterventionStatus = new DicomTag(0x0018, 0x0038);

		/// <summary>(0018,0039) VR=CS VM=1 Therapy Description (Retired)</summary>
		public static DicomTag TherapyDescriptionRETIRED = new DicomTag(0x0018, 0x0039);

		/// <summary>(0018,003a) VR=ST VM=1 Intervention Description</summary>
		public static DicomTag InterventionDescription = new DicomTag(0x0018, 0x003a);

		/// <summary>(0018,0040) VR=IS VM=1 Cine Rate</summary>
		public static DicomTag CineRate = new DicomTag(0x0018, 0x0040);

		/// <summary>(0018,0050) VR=DS VM=1 Slice Thickness</summary>
		public static DicomTag SliceThickness = new DicomTag(0x0018, 0x0050);

		/// <summary>(0018,0060) VR=DS VM=1 KVP</summary>
		public static DicomTag KVP = new DicomTag(0x0018, 0x0060);

		/// <summary>(0018,0070) VR=IS VM=1 Counts Accumulated</summary>
		public static DicomTag CountsAccumulated = new DicomTag(0x0018, 0x0070);

		/// <summary>(0018,0071) VR=CS VM=1 Acquisition Termination Condition</summary>
		public static DicomTag AcquisitionTerminationCondition = new DicomTag(0x0018, 0x0071);

		/// <summary>(0018,0072) VR=DS VM=1 Effective Duration</summary>
		public static DicomTag EffectiveDuration = new DicomTag(0x0018, 0x0072);

		/// <summary>(0018,0073) VR=CS VM=1 Acquisition Start Condition</summary>
		public static DicomTag AcquisitionStartCondition = new DicomTag(0x0018, 0x0073);

		/// <summary>(0018,0074) VR=IS VM=1 Acquisition Start Condition Data</summary>
		public static DicomTag AcquisitionStartConditionData = new DicomTag(0x0018, 0x0074);

		/// <summary>(0018,0075) VR=IS VM=1 Acquisition Termination Condition Data</summary>
		public static DicomTag AcquisitionTerminationConditionData = new DicomTag(0x0018, 0x0075);

		/// <summary>(0018,0080) VR=DS VM=1 Repetition Time</summary>
		public static DicomTag RepetitionTime = new DicomTag(0x0018, 0x0080);

		/// <summary>(0018,0081) VR=DS VM=1 Echo Time</summary>
		public static DicomTag EchoTime = new DicomTag(0x0018, 0x0081);

		/// <summary>(0018,0082) VR=DS VM=1 Inversion Time</summary>
		public static DicomTag InversionTime = new DicomTag(0x0018, 0x0082);

		/// <summary>(0018,0083) VR=DS VM=1 Number of Averages</summary>
		public static DicomTag NumberOfAverages = new DicomTag(0x0018, 0x0083);

		/// <summary>(0018,0084) VR=DS VM=1 Imaging Frequency</summary>
		public static DicomTag ImagingFrequency = new DicomTag(0x0018, 0x0084);

		/// <summary>(0018,0085) VR=SH VM=1 Imaged Nucleus</summary>
		public static DicomTag ImagedNucleus = new DicomTag(0x0018, 0x0085);

		/// <summary>(0018,0086) VR=IS VM=1-n Echo Number(s)</summary>
		public static DicomTag EchoNumbers = new DicomTag(0x0018, 0x0086);

		/// <summary>(0018,0087) VR=DS VM=1 Magnetic Field Strength</summary>
		public static DicomTag MagneticFieldStrength = new DicomTag(0x0018, 0x0087);

		/// <summary>(0018,0088) VR=DS VM=1 Spacing Between Slices</summary>
		public static DicomTag SpacingBetweenSlices = new DicomTag(0x0018, 0x0088);

		/// <summary>(0018,0089) VR=IS VM=1 Number of Phase Encoding Steps</summary>
		public static DicomTag NumberOfPhaseEncodingSteps = new DicomTag(0x0018, 0x0089);

		/// <summary>(0018,0090) VR=DS VM=1 Data Collection Diameter</summary>
		public static DicomTag DataCollectionDiameter = new DicomTag(0x0018, 0x0090);

		/// <summary>(0018,0091) VR=IS VM=1 Echo Train Length</summary>
		public static DicomTag EchoTrainLength = new DicomTag(0x0018, 0x0091);

		/// <summary>(0018,0093) VR=DS VM=1 Percent Sampling</summary>
		public static DicomTag PercentSampling = new DicomTag(0x0018, 0x0093);

		/// <summary>(0018,0094) VR=DS VM=1 Percent Phase Field of View</summary>
		public static DicomTag PercentPhaseFieldOfView = new DicomTag(0x0018, 0x0094);

		/// <summary>(0018,0095) VR=DS VM=1 Pixel Bandwidth</summary>
		public static DicomTag PixelBandwidth = new DicomTag(0x0018, 0x0095);

		/// <summary>(0018,1000) VR=LO VM=1 Device Serial Number</summary>
		public static DicomTag DeviceSerialNumber = new DicomTag(0x0018, 0x1000);

		/// <summary>(0018,1002) VR=UI VM=1 Device UID</summary>
		public static DicomTag DeviceUID = new DicomTag(0x0018, 0x1002);

		/// <summary>(0018,1003) VR=LO VM=1 Device ID</summary>
		public static DicomTag DeviceID = new DicomTag(0x0018, 0x1003);

		/// <summary>(0018,1004) VR=LO VM=1 Plate ID</summary>
		public static DicomTag PlateID = new DicomTag(0x0018, 0x1004);

		/// <summary>(0018,1005) VR=LO VM=1 Generator ID</summary>
		public static DicomTag GeneratorID = new DicomTag(0x0018, 0x1005);

		/// <summary>(0018,1006) VR=LO VM=1 Grid ID</summary>
		public static DicomTag GridID = new DicomTag(0x0018, 0x1006);

		/// <summary>(0018,1007) VR=LO VM=1 Cassette ID</summary>
		public static DicomTag CassetteID = new DicomTag(0x0018, 0x1007);

		/// <summary>(0018,1008) VR=LO VM=1 Gantry ID</summary>
		public static DicomTag GantryID = new DicomTag(0x0018, 0x1008);

		/// <summary>(0018,1010) VR=LO VM=1 Secondary Capture Device ID</summary>
		public static DicomTag SecondaryCaptureDeviceID = new DicomTag(0x0018, 0x1010);

		/// <summary>(0018,1011) VR=LO VM=1 Hardcopy Creation Device ID (Retired)</summary>
		public static DicomTag HardcopyCreationDeviceIDRETIRED = new DicomTag(0x0018, 0x1011);

		/// <summary>(0018,1012) VR=DA VM=1 Date of Secondary Capture</summary>
		public static DicomTag DateOfSecondaryCapture = new DicomTag(0x0018, 0x1012);

		/// <summary>(0018,1014) VR=TM VM=1 Time of Secondary Capture</summary>
		public static DicomTag TimeOfSecondaryCapture = new DicomTag(0x0018, 0x1014);

		/// <summary>(0018,1016) VR=LO VM=1 Secondary Capture Device Manufacturers</summary>
		public static DicomTag SecondaryCaptureDeviceManufacturers = new DicomTag(0x0018, 0x1016);

		/// <summary>(0018,1017) VR=LO VM=1 Hardcopy Device Manufacturer (Retired)</summary>
		public static DicomTag HardcopyDeviceManufacturerRETIRED = new DicomTag(0x0018, 0x1017);

		/// <summary>(0018,1018) VR=LO VM=1 Secondary Capture Device Manufacturer's Model Name</summary>
		public static DicomTag SecondaryCaptureDeviceManufacturersModelName = new DicomTag(0x0018, 0x1018);

		/// <summary>(0018,1019) VR=LO VM=1-n Secondary Capture Device Software Version(s)</summary>
		public static DicomTag SecondaryCaptureDeviceSoftwareVersions = new DicomTag(0x0018, 0x1019);

		/// <summary>(0018,101a) VR=LO VM=1-n Hardcopy Device Software Version (Retired)</summary>
		public static DicomTag HardcopyDeviceSoftwareVersionRETIRED = new DicomTag(0x0018, 0x101a);

		/// <summary>(0018,101b) VR=LO VM=1 Hardcopy Device Manufacturer's Model Name (Retired)</summary>
		public static DicomTag HardcopyDeviceManufacturersModelNameRETIRED = new DicomTag(0x0018, 0x101b);

		/// <summary>(0018,1020) VR=LO VM=1-n Software Version(s)</summary>
		public static DicomTag SoftwareVersions = new DicomTag(0x0018, 0x1020);

		/// <summary>(0018,1022) VR=SH VM=1 Video Image Format Acquired</summary>
		public static DicomTag VideoImageFormatAcquired = new DicomTag(0x0018, 0x1022);

		/// <summary>(0018,1023) VR=LO VM=1 Digital Image Format Acquired</summary>
		public static DicomTag DigitalImageFormatAcquired = new DicomTag(0x0018, 0x1023);

		/// <summary>(0018,1030) VR=LO VM=1 Protocol Name</summary>
		public static DicomTag ProtocolName = new DicomTag(0x0018, 0x1030);

		/// <summary>(0018,1040) VR=LO VM=1 Contrast/Bolus Route</summary>
		public static DicomTag ContrastBolusRoute = new DicomTag(0x0018, 0x1040);

		/// <summary>(0018,1041) VR=DS VM=1 Contrast/Bolus Volume</summary>
		public static DicomTag ContrastBolusVolume = new DicomTag(0x0018, 0x1041);

		/// <summary>(0018,1042) VR=TM VM=1 Contrast/Bolus Start Time</summary>
		public static DicomTag ContrastBolusStartTime = new DicomTag(0x0018, 0x1042);

		/// <summary>(0018,1043) VR=TM VM=1 Contrast/Bolus Stop Time</summary>
		public static DicomTag ContrastBolusStopTime = new DicomTag(0x0018, 0x1043);

		/// <summary>(0018,1044) VR=DS VM=1 Contrast/Bolus Total Dose</summary>
		public static DicomTag ContrastBolusTotalDose = new DicomTag(0x0018, 0x1044);

		/// <summary>(0018,1045) VR=IS VM=1 Syringe Counts</summary>
		public static DicomTag SyringeCounts = new DicomTag(0x0018, 0x1045);

		/// <summary>(0018,1046) VR=DS VM=1-n Contrast Flow Rate</summary>
		public static DicomTag ContrastFlowRate = new DicomTag(0x0018, 0x1046);

		/// <summary>(0018,1047) VR=DS VM=1-n Contrast Flow Duration</summary>
		public static DicomTag ContrastFlowDuration = new DicomTag(0x0018, 0x1047);

		/// <summary>(0018,1048) VR=CS VM=1 Contrast/Bolus Ingredient</summary>
		public static DicomTag ContrastBolusIngredient = new DicomTag(0x0018, 0x1048);

		/// <summary>(0018,1049) VR=DS VM=1 Contrast/Bolus Ingredient Concentration</summary>
		public static DicomTag ContrastBolusIngredientConcentration = new DicomTag(0x0018, 0x1049);

		/// <summary>(0018,1050) VR=DS VM=1 Spatial Resolution</summary>
		public static DicomTag SpatialResolution = new DicomTag(0x0018, 0x1050);

		/// <summary>(0018,1060) VR=DS VM=1 Trigger Time</summary>
		public static DicomTag TriggerTime = new DicomTag(0x0018, 0x1060);

		/// <summary>(0018,1061) VR=LO VM=1 Trigger Source or Type</summary>
		public static DicomTag TriggerSourceOrType = new DicomTag(0x0018, 0x1061);

		/// <summary>(0018,1062) VR=IS VM=1 Nominal Interval</summary>
		public static DicomTag NominalInterval = new DicomTag(0x0018, 0x1062);

		/// <summary>(0018,1063) VR=DS VM=1 Frame Time</summary>
		public static DicomTag FrameTime = new DicomTag(0x0018, 0x1063);

		/// <summary>(0018,1064) VR=LO VM=1 Cardiac Framing Type</summary>
		public static DicomTag CardiacFramingType = new DicomTag(0x0018, 0x1064);

		/// <summary>(0018,1065) VR=DS VM=1-n Frame Time Vector</summary>
		public static DicomTag FrameTimeVector = new DicomTag(0x0018, 0x1065);

		/// <summary>(0018,1066) VR=DS VM=1 Frame Delay</summary>
		public static DicomTag FrameDelay = new DicomTag(0x0018, 0x1066);

		/// <summary>(0018,1067) VR=DS VM=1 Image Trigger Delay</summary>
		public static DicomTag ImageTriggerDelay = new DicomTag(0x0018, 0x1067);

		/// <summary>(0018,1068) VR=DS VM=1 Multiplex Group Time Offset</summary>
		public static DicomTag MultiplexGroupTimeOffset = new DicomTag(0x0018, 0x1068);

		/// <summary>(0018,1069) VR=DS VM=1 Trigger Time Offset</summary>
		public static DicomTag TriggerTimeOffset = new DicomTag(0x0018, 0x1069);

		/// <summary>(0018,106a) VR=CS VM=1 Synchronization Trigger</summary>
		public static DicomTag SynchronizationTrigger = new DicomTag(0x0018, 0x106a);

		/// <summary>(0018,106c) VR=US VM=2 Synchronization Channel</summary>
		public static DicomTag SynchronizationChannel = new DicomTag(0x0018, 0x106c);

		/// <summary>(0018,106e) VR=UL VM=1 Trigger Sample Position</summary>
		public static DicomTag TriggerSamplePosition = new DicomTag(0x0018, 0x106e);

		/// <summary>(0018,1070) VR=LO VM=1 Radiopharmaceutical Route</summary>
		public static DicomTag RadiopharmaceuticalRoute = new DicomTag(0x0018, 0x1070);

		/// <summary>(0018,1071) VR=DS VM=1 Radiopharmaceutical Volume</summary>
		public static DicomTag RadiopharmaceuticalVolume = new DicomTag(0x0018, 0x1071);

		/// <summary>(0018,1072) VR=TM VM=1 Radiopharmaceutical Start Time</summary>
		public static DicomTag RadiopharmaceuticalStartTime = new DicomTag(0x0018, 0x1072);

		/// <summary>(0018,1073) VR=TM VM=1 Radiopharmaceutical Stop Time</summary>
		public static DicomTag RadiopharmaceuticalStopTime = new DicomTag(0x0018, 0x1073);

		/// <summary>(0018,1074) VR=DS VM=1 Radionuclide Total Dose</summary>
		public static DicomTag RadionuclideTotalDose = new DicomTag(0x0018, 0x1074);

		/// <summary>(0018,1075) VR=DS VM=1 Radionuclide Half Life</summary>
		public static DicomTag RadionuclideHalfLife = new DicomTag(0x0018, 0x1075);

		/// <summary>(0018,1076) VR=DS VM=1 Radionuclide Positron Fraction</summary>
		public static DicomTag RadionuclidePositronFraction = new DicomTag(0x0018, 0x1076);

		/// <summary>(0018,1077) VR=DS VM=1 Radiopharmaceutical Specific Activity</summary>
		public static DicomTag RadiopharmaceuticalSpecificActivity = new DicomTag(0x0018, 0x1077);

		/// <summary>(0018,1078) VR=DT VM=1 Radiopharmaceutical Start DateTime</summary>
		public static DicomTag RadiopharmaceuticalStartDateTime = new DicomTag(0x0018, 0x1078);

		/// <summary>(0018,1079) VR=DT VM=1 Radiopharmaceutical Stop DateTime</summary>
		public static DicomTag RadiopharmaceuticalStopDateTime = new DicomTag(0x0018, 0x1079);

		/// <summary>(0018,1080) VR=CS VM=1 Beat Rejection Flag</summary>
		public static DicomTag BeatRejectionFlag = new DicomTag(0x0018, 0x1080);

		/// <summary>(0018,1081) VR=IS VM=1 Low R-R Value</summary>
		public static DicomTag LowRRValue = new DicomTag(0x0018, 0x1081);

		/// <summary>(0018,1082) VR=IS VM=1 High R-R Value</summary>
		public static DicomTag HighRRValue = new DicomTag(0x0018, 0x1082);

		/// <summary>(0018,1083) VR=IS VM=1 Intervals Acquired</summary>
		public static DicomTag IntervalsAcquired = new DicomTag(0x0018, 0x1083);

		/// <summary>(0018,1084) VR=IS VM=1 Intervals Rejected</summary>
		public static DicomTag IntervalsRejected = new DicomTag(0x0018, 0x1084);

		/// <summary>(0018,1085) VR=LO VM=1 PVC Rejection</summary>
		public static DicomTag PVCRejection = new DicomTag(0x0018, 0x1085);

		/// <summary>(0018,1086) VR=IS VM=1 Skip Beats</summary>
		public static DicomTag SkipBeats = new DicomTag(0x0018, 0x1086);

		/// <summary>(0018,1088) VR=IS VM=1 Heart Rate</summary>
		public static DicomTag HeartRate = new DicomTag(0x0018, 0x1088);

		/// <summary>(0018,1090) VR=IS VM=1 Cardiac Number of Images</summary>
		public static DicomTag CardiacNumberOfImages = new DicomTag(0x0018, 0x1090);

		/// <summary>(0018,1094) VR=IS VM=1 Trigger Window</summary>
		public static DicomTag TriggerWindow = new DicomTag(0x0018, 0x1094);

		/// <summary>(0018,1100) VR=DS VM=1 Reconstruction Diameter</summary>
		public static DicomTag ReconstructionDiameter = new DicomTag(0x0018, 0x1100);

		/// <summary>(0018,1110) VR=DS VM=1 Distance Source to Detector</summary>
		public static DicomTag DistanceSourceToDetector = new DicomTag(0x0018, 0x1110);

		/// <summary>(0018,1111) VR=DS VM=1 Distance Source to Patient</summary>
		public static DicomTag DistanceSourceToPatient = new DicomTag(0x0018, 0x1111);

		/// <summary>(0018,1114) VR=DS VM=1 Estimated Radiographic Magnification Factor</summary>
		public static DicomTag EstimatedRadiographicMagnificationFactor = new DicomTag(0x0018, 0x1114);

		/// <summary>(0018,1120) VR=DS VM=1 Gantry/Detector Tilt</summary>
		public static DicomTag GantryDetectorTilt = new DicomTag(0x0018, 0x1120);

		/// <summary>(0018,1121) VR=DS VM=1 Gantry/Detector Slew</summary>
		public static DicomTag GantryDetectorSlew = new DicomTag(0x0018, 0x1121);

		/// <summary>(0018,1130) VR=DS VM=1 Table Height</summary>
		public static DicomTag TableHeight = new DicomTag(0x0018, 0x1130);

		/// <summary>(0018,1131) VR=DS VM=1 Table Traverse</summary>
		public static DicomTag TableTraverse = new DicomTag(0x0018, 0x1131);

		/// <summary>(0018,1134) VR=CS VM=1 Table Motion</summary>
		public static DicomTag TableMotion = new DicomTag(0x0018, 0x1134);

		/// <summary>(0018,1135) VR=DS VM=1-n Table Vertical Increment</summary>
		public static DicomTag TableVerticalIncrement = new DicomTag(0x0018, 0x1135);

		/// <summary>(0018,1136) VR=DS VM=1-n Table Lateral Increment</summary>
		public static DicomTag TableLateralIncrement = new DicomTag(0x0018, 0x1136);

		/// <summary>(0018,1137) VR=DS VM=1-n Table Longitudinal Increment</summary>
		public static DicomTag TableLongitudinalIncrement = new DicomTag(0x0018, 0x1137);

		/// <summary>(0018,1138) VR=DS VM=1 Table Angle</summary>
		public static DicomTag TableAngle = new DicomTag(0x0018, 0x1138);

		/// <summary>(0018,113a) VR=CS VM=1 Table Type</summary>
		public static DicomTag TableType = new DicomTag(0x0018, 0x113a);

		/// <summary>(0018,1140) VR=CS VM=1 Rotation Direction</summary>
		public static DicomTag RotationDirection = new DicomTag(0x0018, 0x1140);

		/// <summary>(0018,1141) VR=DS VM=1 Angular Position (Retired)</summary>
		public static DicomTag AngularPositionRETIRED = new DicomTag(0x0018, 0x1141);

		/// <summary>(0018,1142) VR=DS VM=1-n Radial Position</summary>
		public static DicomTag RadialPosition = new DicomTag(0x0018, 0x1142);

		/// <summary>(0018,1143) VR=DS VM=1 Scan Arc</summary>
		public static DicomTag ScanArc = new DicomTag(0x0018, 0x1143);

		/// <summary>(0018,1144) VR=DS VM=1 Angular Step</summary>
		public static DicomTag AngularStep = new DicomTag(0x0018, 0x1144);

		/// <summary>(0018,1145) VR=DS VM=1 Center of Rotation Offset</summary>
		public static DicomTag CenterOfRotationOffset = new DicomTag(0x0018, 0x1145);

		/// <summary>(0018,1146) VR=DS VM=1-n Rotation Offset (Retired)</summary>
		public static DicomTag RotationOffsetRETIRED = new DicomTag(0x0018, 0x1146);

		/// <summary>(0018,1147) VR=CS VM=1 Field of View Shape</summary>
		public static DicomTag FieldOfViewShape = new DicomTag(0x0018, 0x1147);

		/// <summary>(0018,1149) VR=IS VM=1-2 Field of View Dimension(s)</summary>
		public static DicomTag FieldOfViewDimensions = new DicomTag(0x0018, 0x1149);

		/// <summary>(0018,1150) VR=IS VM=1 Exposure Time</summary>
		public static DicomTag ExposureTime = new DicomTag(0x0018, 0x1150);

		/// <summary>(0018,1151) VR=IS VM=1 X-Ray Tube Current</summary>
		public static DicomTag XRayTubeCurrent = new DicomTag(0x0018, 0x1151);

		/// <summary>(0018,1152) VR=IS VM=1 Exposure</summary>
		public static DicomTag Exposure = new DicomTag(0x0018, 0x1152);

		/// <summary>(0018,1153) VR=IS VM=1 Exposure in uAs</summary>
		public static DicomTag ExposureInMicroAs = new DicomTag(0x0018, 0x1153);

		/// <summary>(0018,1154) VR=DS VM=1 Average Pulse Width</summary>
		public static DicomTag AveragePulseWidth = new DicomTag(0x0018, 0x1154);

		/// <summary>(0018,1155) VR=CS VM=1 Radiation Setting</summary>
		public static DicomTag RadiationSetting = new DicomTag(0x0018, 0x1155);

		/// <summary>(0018,1156) VR=CS VM=1 Rectification Type</summary>
		public static DicomTag RectificationType = new DicomTag(0x0018, 0x1156);

		/// <summary>(0018,115a) VR=CS VM=1 Radiation Mode</summary>
		public static DicomTag RadiationMode = new DicomTag(0x0018, 0x115a);

		/// <summary>(0018,115e) VR=DS VM=1 Image and Fluoroscopy Area Dose Product</summary>
		public static DicomTag ImageAndFluoroscopyAreaDoseProduct = new DicomTag(0x0018, 0x115e);

		/// <summary>(0018,1160) VR=SH VM=1 Filter Type</summary>
		public static DicomTag FilterType = new DicomTag(0x0018, 0x1160);

		/// <summary>(0018,1161) VR=LO VM=1-n Type of Filters</summary>
		public static DicomTag TypeOfFilters = new DicomTag(0x0018, 0x1161);

		/// <summary>(0018,1162) VR=DS VM=1 Intensifier Size</summary>
		public static DicomTag IntensifierSize = new DicomTag(0x0018, 0x1162);

		/// <summary>(0018,1164) VR=DS VM=2 Imager Pixel Spacing</summary>
		public static DicomTag ImagerPixelSpacing = new DicomTag(0x0018, 0x1164);

		/// <summary>(0018,1166) VR=CS VM=1-n Grid</summary>
		public static DicomTag Grid = new DicomTag(0x0018, 0x1166);

		/// <summary>(0018,1170) VR=IS VM=1 Generator Power</summary>
		public static DicomTag GeneratorPower = new DicomTag(0x0018, 0x1170);

		/// <summary>(0018,1180) VR=SH VM=1 Collimator/grid Name</summary>
		public static DicomTag CollimatorgridName = new DicomTag(0x0018, 0x1180);

		/// <summary>(0018,1181) VR=CS VM=1 Collimator Type</summary>
		public static DicomTag CollimatorType = new DicomTag(0x0018, 0x1181);

		/// <summary>(0018,1182) VR=IS VM=1-2 Focal Distance</summary>
		public static DicomTag FocalDistance = new DicomTag(0x0018, 0x1182);

		/// <summary>(0018,1183) VR=DS VM=1-2 X Focus Center</summary>
		public static DicomTag XFocusCenter = new DicomTag(0x0018, 0x1183);

		/// <summary>(0018,1184) VR=DS VM=1-2 Y Focus Center</summary>
		public static DicomTag YFocusCenter = new DicomTag(0x0018, 0x1184);

		/// <summary>(0018,1190) VR=DS VM=1-n Focal Spot(s)</summary>
		public static DicomTag FocalSpots = new DicomTag(0x0018, 0x1190);

		/// <summary>(0018,1191) VR=CS VM=1 Anode Target Material</summary>
		public static DicomTag AnodeTargetMaterial = new DicomTag(0x0018, 0x1191);

		/// <summary>(0018,11a0) VR=DS VM=1 Body Part Thickness</summary>
		public static DicomTag BodyPartThickness = new DicomTag(0x0018, 0x11a0);

		/// <summary>(0018,11a2) VR=DS VM=1 Compression Force</summary>
		public static DicomTag CompressionForce = new DicomTag(0x0018, 0x11a2);

		/// <summary>(0018,1200) VR=DA VM=1-n Date of Last Calibration</summary>
		public static DicomTag DateOfLastCalibration = new DicomTag(0x0018, 0x1200);

		/// <summary>(0018,1201) VR=TM VM=1-n Time of Last Calibration</summary>
		public static DicomTag TimeOfLastCalibration = new DicomTag(0x0018, 0x1201);

		/// <summary>(0018,1210) VR=SH VM=1-n Convolution Kernel</summary>
		public static DicomTag ConvolutionKernel = new DicomTag(0x0018, 0x1210);

		/// <summary>(0018,1240) VR=IS VM=1-n Upper/Lower Pixel Values (Retired)</summary>
		public static DicomTag UpperLowerPixelValuesRETIRED = new DicomTag(0x0018, 0x1240);

		/// <summary>(0018,1242) VR=IS VM=1 Actual Frame Duration</summary>
		public static DicomTag ActualFrameDuration = new DicomTag(0x0018, 0x1242);

		/// <summary>(0018,1243) VR=IS VM=1 Count Rate</summary>
		public static DicomTag CountRate = new DicomTag(0x0018, 0x1243);

		/// <summary>(0018,1244) VR=US VM=1 Preferred Playback Sequencing</summary>
		public static DicomTag PreferredPlaybackSequencing = new DicomTag(0x0018, 0x1244);

		/// <summary>(0018,1250) VR=SH VM=1 Receive Coil Name</summary>
		public static DicomTag ReceiveCoilName = new DicomTag(0x0018, 0x1250);

		/// <summary>(0018,1251) VR=SH VM=1 Transmit Coil Name</summary>
		public static DicomTag TransmitCoilName = new DicomTag(0x0018, 0x1251);

		/// <summary>(0018,1260) VR=SH VM=1 Plate Type</summary>
		public static DicomTag PlateType = new DicomTag(0x0018, 0x1260);

		/// <summary>(0018,1261) VR=LO VM=1 Phosphor Type</summary>
		public static DicomTag PhosphorType = new DicomTag(0x0018, 0x1261);

		/// <summary>(0018,1300) VR=DS VM=1 Scan Velocity</summary>
		public static DicomTag ScanVelocity = new DicomTag(0x0018, 0x1300);

		/// <summary>(0018,1301) VR=CS VM=1-n Whole Body Technique</summary>
		public static DicomTag WholeBodyTechnique = new DicomTag(0x0018, 0x1301);

		/// <summary>(0018,1302) VR=IS VM=1 Scan Length</summary>
		public static DicomTag ScanLength = new DicomTag(0x0018, 0x1302);

		/// <summary>(0018,1310) VR=US VM=4 Acquisition Matrix</summary>
		public static DicomTag AcquisitionMatrix = new DicomTag(0x0018, 0x1310);

		/// <summary>(0018,1312) VR=CS VM=1 In-plane Phase Encoding Direction</summary>
		public static DicomTag InplanePhaseEncodingDirection = new DicomTag(0x0018, 0x1312);

		/// <summary>(0018,1314) VR=DS VM=1 Flip Angle</summary>
		public static DicomTag FlipAngle = new DicomTag(0x0018, 0x1314);

		/// <summary>(0018,1315) VR=CS VM=1 Variable Flip Angle Flag</summary>
		public static DicomTag VariableFlipAngleFlag = new DicomTag(0x0018, 0x1315);

		/// <summary>(0018,1316) VR=DS VM=1 SAR</summary>
		public static DicomTag SAR = new DicomTag(0x0018, 0x1316);

		/// <summary>(0018,1318) VR=DS VM=1 dB/dt</summary>
		public static DicomTag DBdt = new DicomTag(0x0018, 0x1318);

		/// <summary>(0018,1400) VR=LO VM=1 Acquisition Device Processing Description</summary>
		public static DicomTag AcquisitionDeviceProcessingDescription = new DicomTag(0x0018, 0x1400);

		/// <summary>(0018,1401) VR=LO VM=1 Acquisition Device Processing Code</summary>
		public static DicomTag AcquisitionDeviceProcessingCode = new DicomTag(0x0018, 0x1401);

		/// <summary>(0018,1402) VR=CS VM=1 Cassette Orientation</summary>
		public static DicomTag CassetteOrientation = new DicomTag(0x0018, 0x1402);

		/// <summary>(0018,1403) VR=CS VM=1 Cassette Size</summary>
		public static DicomTag CassetteSize = new DicomTag(0x0018, 0x1403);

		/// <summary>(0018,1404) VR=US VM=1 Exposures on Plate</summary>
		public static DicomTag ExposuresOnPlate = new DicomTag(0x0018, 0x1404);

		/// <summary>(0018,1405) VR=IS VM=1 Relative X-Ray Exposure</summary>
		public static DicomTag RelativeXRayExposure = new DicomTag(0x0018, 0x1405);

		/// <summary>(0018,1450) VR=DS VM=1 Column Angulation</summary>
		public static DicomTag ColumnAngulation = new DicomTag(0x0018, 0x1450);

		/// <summary>(0018,1460) VR=DS VM=1 Tomo Layer Height</summary>
		public static DicomTag TomoLayerHeight = new DicomTag(0x0018, 0x1460);

		/// <summary>(0018,1470) VR=DS VM=1 Tomo Angle</summary>
		public static DicomTag TomoAngle = new DicomTag(0x0018, 0x1470);

		/// <summary>(0018,1480) VR=DS VM=1 Tomo Time</summary>
		public static DicomTag TomoTime = new DicomTag(0x0018, 0x1480);

		/// <summary>(0018,1490) VR=CS VM=1 Tomo Type</summary>
		public static DicomTag TomoType = new DicomTag(0x0018, 0x1490);

		/// <summary>(0018,1491) VR=CS VM=1 Tomo Class</summary>
		public static DicomTag TomoClass = new DicomTag(0x0018, 0x1491);

		/// <summary>(0018,1495) VR=IS VM=1 Number of Tomosynthesis Source Images</summary>
		public static DicomTag NumberOfTomosynthesisSourceImages = new DicomTag(0x0018, 0x1495);

		/// <summary>(0018,1500) VR=CS VM=1 Positioner Motion</summary>
		public static DicomTag PositionerMotion = new DicomTag(0x0018, 0x1500);

		/// <summary>(0018,1508) VR=CS VM=1 Positioner Type</summary>
		public static DicomTag PositionerType = new DicomTag(0x0018, 0x1508);

		/// <summary>(0018,1510) VR=DS VM=1 Positioner Primary Angle</summary>
		public static DicomTag PositionerPrimaryAngle = new DicomTag(0x0018, 0x1510);

		/// <summary>(0018,1511) VR=DS VM=1 Positioner Secondary Angle</summary>
		public static DicomTag PositionerSecondaryAngle = new DicomTag(0x0018, 0x1511);

		/// <summary>(0018,1520) VR=DS VM=1-n Positioner Primary Angle Increment</summary>
		public static DicomTag PositionerPrimaryAngleIncrement = new DicomTag(0x0018, 0x1520);

		/// <summary>(0018,1521) VR=DS VM=1-n Positioner Secondary Angle Increment</summary>
		public static DicomTag PositionerSecondaryAngleIncrement = new DicomTag(0x0018, 0x1521);

		/// <summary>(0018,1530) VR=DS VM=1 Detector Primary Angle</summary>
		public static DicomTag DetectorPrimaryAngle = new DicomTag(0x0018, 0x1530);

		/// <summary>(0018,1531) VR=DS VM=1 Detector Secondary Angle</summary>
		public static DicomTag DetectorSecondaryAngle = new DicomTag(0x0018, 0x1531);

		/// <summary>(0018,1600) VR=CS VM=1-3 Shutter Shape</summary>
		public static DicomTag ShutterShape = new DicomTag(0x0018, 0x1600);

		/// <summary>(0018,1602) VR=IS VM=1 Shutter Left Vertical Edge</summary>
		public static DicomTag ShutterLeftVerticalEdge = new DicomTag(0x0018, 0x1602);

		/// <summary>(0018,1604) VR=IS VM=1 Shutter Right Vertical Edge</summary>
		public static DicomTag ShutterRightVerticalEdge = new DicomTag(0x0018, 0x1604);

		/// <summary>(0018,1606) VR=IS VM=1 Shutter Upper Horizontal Edge</summary>
		public static DicomTag ShutterUpperHorizontalEdge = new DicomTag(0x0018, 0x1606);

		/// <summary>(0018,1608) VR=IS VM=1 Shutter Lower Horizontal Edge</summary>
		public static DicomTag ShutterLowerHorizontalEdge = new DicomTag(0x0018, 0x1608);

		/// <summary>(0018,1610) VR=IS VM=2 Center of Circular Shutter</summary>
		public static DicomTag CenterOfCircularShutter = new DicomTag(0x0018, 0x1610);

		/// <summary>(0018,1612) VR=IS VM=1 Radius of Circular Shutter</summary>
		public static DicomTag RadiusOfCircularShutter = new DicomTag(0x0018, 0x1612);

		/// <summary>(0018,1620) VR=IS VM=2-2n Vertices of the Polygonal Shutter</summary>
		public static DicomTag VerticesOfThePolygonalShutter = new DicomTag(0x0018, 0x1620);

		/// <summary>(0018,1622) VR=US VM=1 Shutter Presentation Value</summary>
		public static DicomTag ShutterPresentationValue = new DicomTag(0x0018, 0x1622);

		/// <summary>(0018,1623) VR=US VM=1 Shutter Overlay Group</summary>
		public static DicomTag ShutterOverlayGroup = new DicomTag(0x0018, 0x1623);

		/// <summary>(0018,1624) VR=US VM=3 Shutter Presentation Color CIELab Value</summary>
		public static DicomTag ShutterPresentationColorCIELabValue = new DicomTag(0x0018, 0x1624);

		/// <summary>(0018,1700) VR=CS VM=1-3 Collimator Shape</summary>
		public static DicomTag CollimatorShape = new DicomTag(0x0018, 0x1700);

		/// <summary>(0018,1702) VR=IS VM=1 Collimator Left Vertical Edge</summary>
		public static DicomTag CollimatorLeftVerticalEdge = new DicomTag(0x0018, 0x1702);

		/// <summary>(0018,1704) VR=IS VM=1 Collimator Right Vertical Edge</summary>
		public static DicomTag CollimatorRightVerticalEdge = new DicomTag(0x0018, 0x1704);

		/// <summary>(0018,1706) VR=IS VM=1 Collimator Upper Horizontal Edge</summary>
		public static DicomTag CollimatorUpperHorizontalEdge = new DicomTag(0x0018, 0x1706);

		/// <summary>(0018,1708) VR=IS VM=1 Collimator Lower Horizontal Edge</summary>
		public static DicomTag CollimatorLowerHorizontalEdge = new DicomTag(0x0018, 0x1708);

		/// <summary>(0018,1710) VR=IS VM=2 Center of Circular Collimator</summary>
		public static DicomTag CenterOfCircularCollimator = new DicomTag(0x0018, 0x1710);

		/// <summary>(0018,1712) VR=IS VM=1 Radius of Circular Collimator</summary>
		public static DicomTag RadiusOfCircularCollimator = new DicomTag(0x0018, 0x1712);

		/// <summary>(0018,1720) VR=IS VM=2-2n Vertices of the Polygonal Collimator</summary>
		public static DicomTag VerticesOfThePolygonalCollimator = new DicomTag(0x0018, 0x1720);

		/// <summary>(0018,1800) VR=CS VM=1 Acquisition Time Synchronized</summary>
		public static DicomTag AcquisitionTimeSynchronized = new DicomTag(0x0018, 0x1800);

		/// <summary>(0018,1801) VR=SH VM=1 Time Source</summary>
		public static DicomTag TimeSource = new DicomTag(0x0018, 0x1801);

		/// <summary>(0018,1802) VR=CS VM=1 Time Distribution Protocol</summary>
		public static DicomTag TimeDistributionProtocol = new DicomTag(0x0018, 0x1802);

		/// <summary>(0018,1803) VR=LO VM=1 NTP Source Address</summary>
		public static DicomTag NTPSourceAddress = new DicomTag(0x0018, 0x1803);

		/// <summary>(0018,2001) VR=IS VM=1-n Page Number Vector</summary>
		public static DicomTag PageNumberVector = new DicomTag(0x0018, 0x2001);

		/// <summary>(0018,2002) VR=SH VM=1-n Frame Label Vector</summary>
		public static DicomTag FrameLabelVector = new DicomTag(0x0018, 0x2002);

		/// <summary>(0018,2003) VR=DS VM=1-n Frame Primary Angle Vector</summary>
		public static DicomTag FramePrimaryAngleVector = new DicomTag(0x0018, 0x2003);

		/// <summary>(0018,2004) VR=DS VM=1-n Frame Secondary Angle Vector</summary>
		public static DicomTag FrameSecondaryAngleVector = new DicomTag(0x0018, 0x2004);

		/// <summary>(0018,2005) VR=DS VM=1-n Slice Location Vector</summary>
		public static DicomTag SliceLocationVector = new DicomTag(0x0018, 0x2005);

		/// <summary>(0018,2006) VR=SH VM=1-n Display Window Label Vector</summary>
		public static DicomTag DisplayWindowLabelVector = new DicomTag(0x0018, 0x2006);

		/// <summary>(0018,2010) VR=DS VM=2 Nominal Scanned Pixel Spacing</summary>
		public static DicomTag NominalScannedPixelSpacing = new DicomTag(0x0018, 0x2010);

		/// <summary>(0018,2020) VR=CS VM=1 Digitizing Device Transport Direction</summary>
		public static DicomTag DigitizingDeviceTransportDirection = new DicomTag(0x0018, 0x2020);

		/// <summary>(0018,2030) VR=DS VM=1 Rotation of Scanned Film</summary>
		public static DicomTag RotationOfScannedFilm = new DicomTag(0x0018, 0x2030);

		/// <summary>(0018,3100) VR=CS VM=1 IVUS Acquisition</summary>
		public static DicomTag IVUSAcquisition = new DicomTag(0x0018, 0x3100);

		/// <summary>(0018,3101) VR=DS VM=1 IVUS Pullback Rate</summary>
		public static DicomTag IVUSPullbackRate = new DicomTag(0x0018, 0x3101);

		/// <summary>(0018,3102) VR=DS VM=1 IVUS Gated Rate</summary>
		public static DicomTag IVUSGatedRate = new DicomTag(0x0018, 0x3102);

		/// <summary>(0018,3103) VR=IS VM=1 IVUS Pullback Start Frame Number</summary>
		public static DicomTag IVUSPullbackStartFrameNumber = new DicomTag(0x0018, 0x3103);

		/// <summary>(0018,3104) VR=IS VM=1 IVUS Pullback Stop Frame Number</summary>
		public static DicomTag IVUSPullbackStopFrameNumber = new DicomTag(0x0018, 0x3104);

		/// <summary>(0018,3105) VR=IS VM=1-n Lesion Number</summary>
		public static DicomTag LesionNumber = new DicomTag(0x0018, 0x3105);

		/// <summary>(0018,4000) VR=LT VM=1 Acquisition Comments (Retired)</summary>
		public static DicomTag AcquisitionCommentsRETIRED = new DicomTag(0x0018, 0x4000);

		/// <summary>(0018,5000) VR=SH VM=1-n Output Power</summary>
		public static DicomTag OutputPower = new DicomTag(0x0018, 0x5000);

		/// <summary>(0018,5010) VR=LO VM=3 Transducer Data</summary>
		public static DicomTag TransducerData = new DicomTag(0x0018, 0x5010);

		/// <summary>(0018,5012) VR=DS VM=1 Focus Depth</summary>
		public static DicomTag FocusDepth = new DicomTag(0x0018, 0x5012);

		/// <summary>(0018,5020) VR=LO VM=1 Processing Function</summary>
		public static DicomTag ProcessingFunction = new DicomTag(0x0018, 0x5020);

		/// <summary>(0018,5021) VR=LO VM=1 Postprocessing Function (Retired)</summary>
		public static DicomTag PostprocessingFunctionRETIRED = new DicomTag(0x0018, 0x5021);

		/// <summary>(0018,5022) VR=DS VM=1 Mechanical Index</summary>
		public static DicomTag MechanicalIndex = new DicomTag(0x0018, 0x5022);

		/// <summary>(0018,5024) VR=DS VM=1 Bone Thermal Index</summary>
		public static DicomTag BoneThermalIndex = new DicomTag(0x0018, 0x5024);

		/// <summary>(0018,5026) VR=DS VM=1 Cranial Thermal Index</summary>
		public static DicomTag CranialThermalIndex = new DicomTag(0x0018, 0x5026);

		/// <summary>(0018,5027) VR=DS VM=1 Soft Tissue Thermal Index</summary>
		public static DicomTag SoftTissueThermalIndex = new DicomTag(0x0018, 0x5027);

		/// <summary>(0018,5028) VR=DS VM=1 Soft Tissue-focus Thermal Index</summary>
		public static DicomTag SoftTissuefocusThermalIndex = new DicomTag(0x0018, 0x5028);

		/// <summary>(0018,5029) VR=DS VM=1 Soft Tissue-surface Thermal Index</summary>
		public static DicomTag SoftTissuesurfaceThermalIndex = new DicomTag(0x0018, 0x5029);

		/// <summary>(0018,5030) VR=DS VM=1 Dynamic Range (Retired)</summary>
		public static DicomTag DynamicRangeRETIRED = new DicomTag(0x0018, 0x5030);

		/// <summary>(0018,5040) VR=DS VM=1 Total Gain (Retired)</summary>
		public static DicomTag TotalGainRETIRED = new DicomTag(0x0018, 0x5040);

		/// <summary>(0018,5050) VR=IS VM=1 Depth of Scan Field</summary>
		public static DicomTag DepthOfScanField = new DicomTag(0x0018, 0x5050);

		/// <summary>(0018,5100) VR=CS VM=1 Patient Position</summary>
		public static DicomTag PatientPosition = new DicomTag(0x0018, 0x5100);

		/// <summary>(0018,5101) VR=CS VM=1 View Position</summary>
		public static DicomTag ViewPosition = new DicomTag(0x0018, 0x5101);

		/// <summary>(0018,5104) VR=SQ VM=1 Projection Eponymous Name Code Sequence</summary>
		public static DicomTag ProjectionEponymousNameCodeSequence = new DicomTag(0x0018, 0x5104);

		/// <summary>(0018,5210) VR=DS VM=6 Image Transformation Matrix (Retired)</summary>
		public static DicomTag ImageTransformationMatrixRETIRED = new DicomTag(0x0018, 0x5210);

		/// <summary>(0018,5212) VR=DS VM=3 Image Translation Vector (Retired)</summary>
		public static DicomTag ImageTranslationVectorRETIRED = new DicomTag(0x0018, 0x5212);

		/// <summary>(0018,6000) VR=DS VM=1 Sensitivity</summary>
		public static DicomTag Sensitivity = new DicomTag(0x0018, 0x6000);

		/// <summary>(0018,6011) VR=SQ VM=1 Sequence of Ultrasound Regions</summary>
		public static DicomTag SequenceOfUltrasoundRegions = new DicomTag(0x0018, 0x6011);

		/// <summary>(0018,6012) VR=US VM=1 Region Spatial Format</summary>
		public static DicomTag RegionSpatialFormat = new DicomTag(0x0018, 0x6012);

		/// <summary>(0018,6014) VR=US VM=1 Region Data Type</summary>
		public static DicomTag RegionDataType = new DicomTag(0x0018, 0x6014);

		/// <summary>(0018,6016) VR=UL VM=1 Region Flags</summary>
		public static DicomTag RegionFlags = new DicomTag(0x0018, 0x6016);

		/// <summary>(0018,6018) VR=UL VM=1 Region Location Min X0</summary>
		public static DicomTag RegionLocationMinX0 = new DicomTag(0x0018, 0x6018);

		/// <summary>(0018,601a) VR=UL VM=1 Region Location Min Y0</summary>
		public static DicomTag RegionLocationMinY0 = new DicomTag(0x0018, 0x601a);

		/// <summary>(0018,601c) VR=UL VM=1 Region Location Max X1</summary>
		public static DicomTag RegionLocationMaxX1 = new DicomTag(0x0018, 0x601c);

		/// <summary>(0018,601e) VR=UL VM=1 Region Location Max Y1</summary>
		public static DicomTag RegionLocationMaxY1 = new DicomTag(0x0018, 0x601e);

		/// <summary>(0018,6020) VR=SL VM=1 Reference Pixel X0</summary>
		public static DicomTag ReferencePixelX0 = new DicomTag(0x0018, 0x6020);

		/// <summary>(0018,6022) VR=SL VM=1 Reference Pixel Y0</summary>
		public static DicomTag ReferencePixelY0 = new DicomTag(0x0018, 0x6022);

		/// <summary>(0018,6024) VR=US VM=1 Physical Units X Direction</summary>
		public static DicomTag PhysicalUnitsXDirection = new DicomTag(0x0018, 0x6024);

		/// <summary>(0018,6026) VR=US VM=1 Physical Units Y Direction</summary>
		public static DicomTag PhysicalUnitsYDirection = new DicomTag(0x0018, 0x6026);

		/// <summary>(0018,6028) VR=FD VM=1 Reference Pixel Physical Value X</summary>
		public static DicomTag ReferencePixelPhysicalValueX = new DicomTag(0x0018, 0x6028);

		/// <summary>(0018,602a) VR=FD VM=1 Reference Pixel Physical Value Y</summary>
		public static DicomTag ReferencePixelPhysicalValueY = new DicomTag(0x0018, 0x602a);

		/// <summary>(0018,602c) VR=FD VM=1 Physical Delta X</summary>
		public static DicomTag PhysicalDeltaX = new DicomTag(0x0018, 0x602c);

		/// <summary>(0018,602e) VR=FD VM=1 Physical Delta Y</summary>
		public static DicomTag PhysicalDeltaY = new DicomTag(0x0018, 0x602e);

		/// <summary>(0018,6030) VR=UL VM=1 Transducer Frequency</summary>
		public static DicomTag TransducerFrequency = new DicomTag(0x0018, 0x6030);

		/// <summary>(0018,6031) VR=CS VM=1 Transducer Type</summary>
		public static DicomTag TransducerType = new DicomTag(0x0018, 0x6031);

		/// <summary>(0018,6032) VR=UL VM=1 Pulse Repetition Frequency</summary>
		public static DicomTag PulseRepetitionFrequency = new DicomTag(0x0018, 0x6032);

		/// <summary>(0018,6034) VR=FD VM=1 Doppler Correction Angle</summary>
		public static DicomTag DopplerCorrectionAngle = new DicomTag(0x0018, 0x6034);

		/// <summary>(0018,6036) VR=FD VM=1 Steering Angle</summary>
		public static DicomTag SteeringAngle = new DicomTag(0x0018, 0x6036);

		/// <summary>(0018,6038) VR=UL VM=1 Doppler Sample Volume X Position (Retired) (Retired)</summary>
		public static DicomTag DopplerSampleVolumeXPositionRetiredRETIRED = new DicomTag(0x0018, 0x6038);

		/// <summary>(0018,6039) VR=SL VM=1 Doppler Sample Volume X Position</summary>
		public static DicomTag DopplerSampleVolumeXPosition = new DicomTag(0x0018, 0x6039);

		/// <summary>(0018,603a) VR=UL VM=1 Doppler Sample Volume Y Position (Retired) (Retired)</summary>
		public static DicomTag DopplerSampleVolumeYPositionRetiredRETIRED = new DicomTag(0x0018, 0x603a);

		/// <summary>(0018,603b) VR=SL VM=1 Doppler Sample Volume Y Position</summary>
		public static DicomTag DopplerSampleVolumeYPosition = new DicomTag(0x0018, 0x603b);

		/// <summary>(0018,603c) VR=UL VM=1 TM-Line Position X0 (Retired) (Retired)</summary>
		public static DicomTag TMLinePositionX0RetiredRETIRED = new DicomTag(0x0018, 0x603c);

		/// <summary>(0018,603d) VR=SL VM=1 TM-Line Position X0</summary>
		public static DicomTag TMLinePositionX0 = new DicomTag(0x0018, 0x603d);

		/// <summary>(0018,603e) VR=UL VM=1 TM-Line Position Y0 (Retired) (Retired)</summary>
		public static DicomTag TMLinePositionY0RetiredRETIRED = new DicomTag(0x0018, 0x603e);

		/// <summary>(0018,603f) VR=SL VM=1 TM-Line Position Y0</summary>
		public static DicomTag TMLinePositionY0 = new DicomTag(0x0018, 0x603f);

		/// <summary>(0018,6040) VR=UL VM=1 TM-Line Position X1 (Retired) (Retired)</summary>
		public static DicomTag TMLinePositionX1RetiredRETIRED = new DicomTag(0x0018, 0x6040);

		/// <summary>(0018,6041) VR=SL VM=1 TM-Line Position X1</summary>
		public static DicomTag TMLinePositionX1 = new DicomTag(0x0018, 0x6041);

		/// <summary>(0018,6042) VR=UL VM=1 TM-Line Position Y1 (Retired) (Retired)</summary>
		public static DicomTag TMLinePositionY1RetiredRETIRED = new DicomTag(0x0018, 0x6042);

		/// <summary>(0018,6043) VR=SL VM=1 TM-Line Position Y1</summary>
		public static DicomTag TMLinePositionY1 = new DicomTag(0x0018, 0x6043);

		/// <summary>(0018,6044) VR=US VM=1 Pixel Component Organization</summary>
		public static DicomTag PixelComponentOrganization = new DicomTag(0x0018, 0x6044);

		/// <summary>(0018,6046) VR=UL VM=1 Pixel Component Mask</summary>
		public static DicomTag PixelComponentMask = new DicomTag(0x0018, 0x6046);

		/// <summary>(0018,6048) VR=UL VM=1 Pixel Component Range Start</summary>
		public static DicomTag PixelComponentRangeStart = new DicomTag(0x0018, 0x6048);

		/// <summary>(0018,604a) VR=UL VM=1 Pixel Component Range Stop</summary>
		public static DicomTag PixelComponentRangeStop = new DicomTag(0x0018, 0x604a);

		/// <summary>(0018,604c) VR=US VM=1 Pixel Component Physical Units</summary>
		public static DicomTag PixelComponentPhysicalUnits = new DicomTag(0x0018, 0x604c);

		/// <summary>(0018,604e) VR=US VM=1 Pixel Component Data Type</summary>
		public static DicomTag PixelComponentDataType = new DicomTag(0x0018, 0x604e);

		/// <summary>(0018,6050) VR=UL VM=1 Number of Table Break Points</summary>
		public static DicomTag NumberOfTableBreakPoints = new DicomTag(0x0018, 0x6050);

		/// <summary>(0018,6052) VR=UL VM=1-n Table of X Break Points</summary>
		public static DicomTag TableOfXBreakPoints = new DicomTag(0x0018, 0x6052);

		/// <summary>(0018,6054) VR=FD VM=1-n Table of Y Break Points</summary>
		public static DicomTag TableOfYBreakPoints = new DicomTag(0x0018, 0x6054);

		/// <summary>(0018,6056) VR=UL VM=1 Number of Table Entries</summary>
		public static DicomTag NumberOfTableEntries = new DicomTag(0x0018, 0x6056);

		/// <summary>(0018,6058) VR=UL VM=1-n Table of Pixel Values</summary>
		public static DicomTag TableOfPixelValues = new DicomTag(0x0018, 0x6058);

		/// <summary>(0018,605a) VR=FL VM=1-n Table of Parameter Values</summary>
		public static DicomTag TableOfParameterValues = new DicomTag(0x0018, 0x605a);

		/// <summary>(0018,6060) VR=FL VM=1-n R Wave Time Vector</summary>
		public static DicomTag RWaveTimeVector = new DicomTag(0x0018, 0x6060);

		/// <summary>(0018,7000) VR=CS VM=1 Detector Conditions Nominal Flag</summary>
		public static DicomTag DetectorConditionsNominalFlag = new DicomTag(0x0018, 0x7000);

		/// <summary>(0018,7001) VR=DS VM=1 Detector Temperature</summary>
		public static DicomTag DetectorTemperature = new DicomTag(0x0018, 0x7001);

		/// <summary>(0018,7004) VR=CS VM=1 Detector Type</summary>
		public static DicomTag DetectorType = new DicomTag(0x0018, 0x7004);

		/// <summary>(0018,7005) VR=CS VM=1 Detector Configuration</summary>
		public static DicomTag DetectorConfiguration = new DicomTag(0x0018, 0x7005);

		/// <summary>(0018,7006) VR=LT VM=1 Detector Description</summary>
		public static DicomTag DetectorDescription = new DicomTag(0x0018, 0x7006);

		/// <summary>(0018,7008) VR=LT VM=1 Detector Mode</summary>
		public static DicomTag DetectorMode = new DicomTag(0x0018, 0x7008);

		/// <summary>(0018,700a) VR=SH VM=1 Detector ID</summary>
		public static DicomTag DetectorID = new DicomTag(0x0018, 0x700a);

		/// <summary>(0018,700c) VR=DA VM=1 Date of Last Detector Calibration</summary>
		public static DicomTag DateOfLastDetectorCalibration = new DicomTag(0x0018, 0x700c);

		/// <summary>(0018,700e) VR=TM VM=1 Time of Last Detector Calibration</summary>
		public static DicomTag TimeOfLastDetectorCalibration = new DicomTag(0x0018, 0x700e);

		/// <summary>(0018,7010) VR=IS VM=1 Exposures on Detector Since Last Calibration</summary>
		public static DicomTag ExposuresOnDetectorSinceLastCalibration = new DicomTag(0x0018, 0x7010);

		/// <summary>(0018,7011) VR=IS VM=1 Exposures on Detector Since Manufactured</summary>
		public static DicomTag ExposuresOnDetectorSinceManufactured = new DicomTag(0x0018, 0x7011);

		/// <summary>(0018,7012) VR=DS VM=1 Detector Time Since Last Exposure</summary>
		public static DicomTag DetectorTimeSinceLastExposure = new DicomTag(0x0018, 0x7012);

		/// <summary>(0018,7014) VR=DS VM=1 Detector Active Time</summary>
		public static DicomTag DetectorActiveTime = new DicomTag(0x0018, 0x7014);

		/// <summary>(0018,7016) VR=DS VM=1 Detector Activation Offset From Exposure</summary>
		public static DicomTag DetectorActivationOffsetFromExposure = new DicomTag(0x0018, 0x7016);

		/// <summary>(0018,701a) VR=DS VM=2 Detector Binning</summary>
		public static DicomTag DetectorBinning = new DicomTag(0x0018, 0x701a);

		/// <summary>(0018,7020) VR=DS VM=2 Detector Element Physical Size</summary>
		public static DicomTag DetectorElementPhysicalSize = new DicomTag(0x0018, 0x7020);

		/// <summary>(0018,7022) VR=DS VM=2 Detector Element Spacing</summary>
		public static DicomTag DetectorElementSpacing = new DicomTag(0x0018, 0x7022);

		/// <summary>(0018,7024) VR=CS VM=1 Detector Active Shape</summary>
		public static DicomTag DetectorActiveShape = new DicomTag(0x0018, 0x7024);

		/// <summary>(0018,7026) VR=DS VM=1-2 Detector Active Dimension(s)</summary>
		public static DicomTag DetectorActiveDimensions = new DicomTag(0x0018, 0x7026);

		/// <summary>(0018,7028) VR=DS VM=2 Detector Active Origin</summary>
		public static DicomTag DetectorActiveOrigin = new DicomTag(0x0018, 0x7028);

		/// <summary>(0018,702a) VR=LO VM=1 Detector Manufacturer Name</summary>
		public static DicomTag DetectorManufacturerName = new DicomTag(0x0018, 0x702a);

		/// <summary>(0018,702b) VR=LO VM=1 Detector Manufacturer's Model Name</summary>
		public static DicomTag DetectorManufacturersModelName = new DicomTag(0x0018, 0x702b);

		/// <summary>(0018,7030) VR=DS VM=2 Field of View Origin</summary>
		public static DicomTag FieldOfViewOrigin = new DicomTag(0x0018, 0x7030);

		/// <summary>(0018,7032) VR=DS VM=1 Field of View Rotation</summary>
		public static DicomTag FieldOfViewRotation = new DicomTag(0x0018, 0x7032);

		/// <summary>(0018,7034) VR=CS VM=1 Field of View Horizontal Flip</summary>
		public static DicomTag FieldOfViewHorizontalFlip = new DicomTag(0x0018, 0x7034);

		/// <summary>(0018,7040) VR=LT VM=1 Grid Absorbing Material</summary>
		public static DicomTag GridAbsorbingMaterial = new DicomTag(0x0018, 0x7040);

		/// <summary>(0018,7041) VR=LT VM=1 Grid Spacing Material</summary>
		public static DicomTag GridSpacingMaterial = new DicomTag(0x0018, 0x7041);

		/// <summary>(0018,7042) VR=DS VM=1 Grid Thickness</summary>
		public static DicomTag GridThickness = new DicomTag(0x0018, 0x7042);

		/// <summary>(0018,7044) VR=DS VM=1 Grid Pitch</summary>
		public static DicomTag GridPitch = new DicomTag(0x0018, 0x7044);

		/// <summary>(0018,7046) VR=IS VM=2 Grid Aspect Ratio</summary>
		public static DicomTag GridAspectRatio = new DicomTag(0x0018, 0x7046);

		/// <summary>(0018,7048) VR=DS VM=1 Grid Period</summary>
		public static DicomTag GridPeriod = new DicomTag(0x0018, 0x7048);

		/// <summary>(0018,704c) VR=DS VM=1 Grid Focal Distance</summary>
		public static DicomTag GridFocalDistance = new DicomTag(0x0018, 0x704c);

		/// <summary>(0018,7050) VR=CS VM=1-n Filter Material</summary>
		public static DicomTag FilterMaterial = new DicomTag(0x0018, 0x7050);

		/// <summary>(0018,7052) VR=DS VM=1-n Filter Thickness Minimum</summary>
		public static DicomTag FilterThicknessMinimum = new DicomTag(0x0018, 0x7052);

		/// <summary>(0018,7054) VR=DS VM=1-n Filter Thickness Maximum</summary>
		public static DicomTag FilterThicknessMaximum = new DicomTag(0x0018, 0x7054);

		/// <summary>(0018,7060) VR=CS VM=1 Exposure Control Mode</summary>
		public static DicomTag ExposureControlMode = new DicomTag(0x0018, 0x7060);

		/// <summary>(0018,7062) VR=LT VM=1 Exposure Control Mode Description</summary>
		public static DicomTag ExposureControlModeDescription = new DicomTag(0x0018, 0x7062);

		/// <summary>(0018,7064) VR=CS VM=1 Exposure Status</summary>
		public static DicomTag ExposureStatus = new DicomTag(0x0018, 0x7064);

		/// <summary>(0018,7065) VR=DS VM=1 Phototimer Setting</summary>
		public static DicomTag PhototimerSetting = new DicomTag(0x0018, 0x7065);

		/// <summary>(0018,8150) VR=DS VM=1 Exposure Time in uS</summary>
		public static DicomTag ExposureTimeInMicroS = new DicomTag(0x0018, 0x8150);

		/// <summary>(0018,8151) VR=DS VM=1 X-Ray Tube Current in uA</summary>
		public static DicomTag XRayTubeCurrentInMicroA = new DicomTag(0x0018, 0x8151);

		/// <summary>(0018,9004) VR=CS VM=1 Content Qualification</summary>
		public static DicomTag ContentQualification = new DicomTag(0x0018, 0x9004);

		/// <summary>(0018,9005) VR=SH VM=1 Pulse Sequence Name</summary>
		public static DicomTag PulseSequenceName = new DicomTag(0x0018, 0x9005);

		/// <summary>(0018,9006) VR=SQ VM=1 MR Imaging Modifier Sequence</summary>
		public static DicomTag MRImagingModifierSequence = new DicomTag(0x0018, 0x9006);

		/// <summary>(0018,9008) VR=CS VM=1 Echo Pulse Sequence</summary>
		public static DicomTag EchoPulseSequence = new DicomTag(0x0018, 0x9008);

		/// <summary>(0018,9009) VR=CS VM=1 Inversion Recovery</summary>
		public static DicomTag InversionRecovery = new DicomTag(0x0018, 0x9009);

		/// <summary>(0018,9010) VR=CS VM=1 Flow Compensation</summary>
		public static DicomTag FlowCompensation = new DicomTag(0x0018, 0x9010);

		/// <summary>(0018,9011) VR=CS VM=1 Multiple Spin Echo</summary>
		public static DicomTag MultipleSpinEcho = new DicomTag(0x0018, 0x9011);

		/// <summary>(0018,9012) VR=CS VM=1 Multi-planar Excitation</summary>
		public static DicomTag MultiplanarExcitation = new DicomTag(0x0018, 0x9012);

		/// <summary>(0018,9014) VR=CS VM=1 Phase Contrast</summary>
		public static DicomTag PhaseContrast = new DicomTag(0x0018, 0x9014);

		/// <summary>(0018,9015) VR=CS VM=1 Time of Flight Contrast</summary>
		public static DicomTag TimeOfFlightContrast = new DicomTag(0x0018, 0x9015);

		/// <summary>(0018,9016) VR=CS VM=1 Spoiling</summary>
		public static DicomTag Spoiling = new DicomTag(0x0018, 0x9016);

		/// <summary>(0018,9017) VR=CS VM=1 Steady State Pulse Sequence</summary>
		public static DicomTag SteadyStatePulseSequence = new DicomTag(0x0018, 0x9017);

		/// <summary>(0018,9018) VR=CS VM=1 Echo Planar Pulse Sequence</summary>
		public static DicomTag EchoPlanarPulseSequence = new DicomTag(0x0018, 0x9018);

		/// <summary>(0018,9019) VR=FD VM=1 Tag Angle First Axis</summary>
		public static DicomTag TagAngleFirstAxis = new DicomTag(0x0018, 0x9019);

		/// <summary>(0018,9020) VR=CS VM=1 Magnetization Transfer</summary>
		public static DicomTag MagnetizationTransfer = new DicomTag(0x0018, 0x9020);

		/// <summary>(0018,9021) VR=CS VM=1 T2 Preparation</summary>
		public static DicomTag T2Preparation = new DicomTag(0x0018, 0x9021);

		/// <summary>(0018,9022) VR=CS VM=1 Blood Signal Nulling</summary>
		public static DicomTag BloodSignalNulling = new DicomTag(0x0018, 0x9022);

		/// <summary>(0018,9024) VR=CS VM=1 Saturation Recovery</summary>
		public static DicomTag SaturationRecovery = new DicomTag(0x0018, 0x9024);

		/// <summary>(0018,9025) VR=CS VM=1 Spectrally Selected Suppression</summary>
		public static DicomTag SpectrallySelectedSuppression = new DicomTag(0x0018, 0x9025);

		/// <summary>(0018,9026) VR=CS VM=1 Spectrally Selected Excitation</summary>
		public static DicomTag SpectrallySelectedExcitation = new DicomTag(0x0018, 0x9026);

		/// <summary>(0018,9027) VR=CS VM=1 Spatial Pre-saturation</summary>
		public static DicomTag SpatialPresaturation = new DicomTag(0x0018, 0x9027);

		/// <summary>(0018,9028) VR=CS VM=1 Tagging</summary>
		public static DicomTag Tagging = new DicomTag(0x0018, 0x9028);

		/// <summary>(0018,9029) VR=CS VM=1 Oversampling Phase</summary>
		public static DicomTag OversamplingPhase = new DicomTag(0x0018, 0x9029);

		/// <summary>(0018,9030) VR=FD VM=1 Tag Spacing First Dimension</summary>
		public static DicomTag TagSpacingFirstDimension = new DicomTag(0x0018, 0x9030);

		/// <summary>(0018,9032) VR=CS VM=1 Geometry of k-Space Traversal</summary>
		public static DicomTag GeometryOfKSpaceTraversal = new DicomTag(0x0018, 0x9032);

		/// <summary>(0018,9033) VR=CS VM=1 Segmented k-Space Traversal</summary>
		public static DicomTag SegmentedKSpaceTraversal = new DicomTag(0x0018, 0x9033);

		/// <summary>(0018,9034) VR=CS VM=1 Rectilinear Phase Encode Reordering</summary>
		public static DicomTag RectilinearPhaseEncodeReordering = new DicomTag(0x0018, 0x9034);

		/// <summary>(0018,9035) VR=FD VM=1 Tag Thickness</summary>
		public static DicomTag TagThickness = new DicomTag(0x0018, 0x9035);

		/// <summary>(0018,9036) VR=CS VM=1 Partial Fourier Direction</summary>
		public static DicomTag PartialFourierDirection = new DicomTag(0x0018, 0x9036);

		/// <summary>(0018,9037) VR=CS VM=1 Cardiac Synchronization Technique</summary>
		public static DicomTag CardiacSynchronizationTechnique = new DicomTag(0x0018, 0x9037);

		/// <summary>(0018,9041) VR=LO VM=1 Receive Coil Manufacturer Name</summary>
		public static DicomTag ReceiveCoilManufacturerName = new DicomTag(0x0018, 0x9041);

		/// <summary>(0018,9042) VR=SQ VM=1 MR Receive Coil Sequence</summary>
		public static DicomTag MRReceiveCoilSequence = new DicomTag(0x0018, 0x9042);

		/// <summary>(0018,9043) VR=CS VM=1 Receive Coil Type</summary>
		public static DicomTag ReceiveCoilType = new DicomTag(0x0018, 0x9043);

		/// <summary>(0018,9044) VR=CS VM=1 Quadrature Receive Coil</summary>
		public static DicomTag QuadratureReceiveCoil = new DicomTag(0x0018, 0x9044);

		/// <summary>(0018,9045) VR=SQ VM=1 Multi-Coil Definition Sequence</summary>
		public static DicomTag MultiCoilDefinitionSequence = new DicomTag(0x0018, 0x9045);

		/// <summary>(0018,9046) VR=LO VM=1 Multi-Coil Configuration</summary>
		public static DicomTag MultiCoilConfiguration = new DicomTag(0x0018, 0x9046);

		/// <summary>(0018,9047) VR=SH VM=1 Multi-Coil Element Name</summary>
		public static DicomTag MultiCoilElementName = new DicomTag(0x0018, 0x9047);

		/// <summary>(0018,9048) VR=CS VM=1 Multi-Coil Element Used</summary>
		public static DicomTag MultiCoilElementUsed = new DicomTag(0x0018, 0x9048);

		/// <summary>(0018,9049) VR=SQ VM=1 MR Transmit Coil Sequence</summary>
		public static DicomTag MRTransmitCoilSequence = new DicomTag(0x0018, 0x9049);

		/// <summary>(0018,9050) VR=LO VM=1 Transmit Coil Manufacturer Name</summary>
		public static DicomTag TransmitCoilManufacturerName = new DicomTag(0x0018, 0x9050);

		/// <summary>(0018,9051) VR=CS VM=1 Transmit Coil Type</summary>
		public static DicomTag TransmitCoilType = new DicomTag(0x0018, 0x9051);

		/// <summary>(0018,9052) VR=FD VM=1-2 Spectral Width</summary>
		public static DicomTag SpectralWidth = new DicomTag(0x0018, 0x9052);

		/// <summary>(0018,9053) VR=FD VM=1-2 Chemical Shift Reference</summary>
		public static DicomTag ChemicalShiftReference = new DicomTag(0x0018, 0x9053);

		/// <summary>(0018,9054) VR=CS VM=1 Volume Localization Technique</summary>
		public static DicomTag VolumeLocalizationTechnique = new DicomTag(0x0018, 0x9054);

		/// <summary>(0018,9058) VR=US VM=1 MR Acquisition Frequency Encoding Steps</summary>
		public static DicomTag MRAcquisitionFrequencyEncodingSteps = new DicomTag(0x0018, 0x9058);

		/// <summary>(0018,9059) VR=CS VM=1 De-coupling</summary>
		public static DicomTag Decoupling = new DicomTag(0x0018, 0x9059);

		/// <summary>(0018,9060) VR=CS VM=1-2 De-coupled Nucleus</summary>
		public static DicomTag DecoupledNucleus = new DicomTag(0x0018, 0x9060);

		/// <summary>(0018,9061) VR=FD VM=1-2 De-coupling Frequency</summary>
		public static DicomTag DecouplingFrequency = new DicomTag(0x0018, 0x9061);

		/// <summary>(0018,9062) VR=CS VM=1 De-coupling Method</summary>
		public static DicomTag DecouplingMethod = new DicomTag(0x0018, 0x9062);

		/// <summary>(0018,9063) VR=FD VM=1-2 De-coupling Chemical Shift Reference</summary>
		public static DicomTag DecouplingChemicalShiftReference = new DicomTag(0x0018, 0x9063);

		/// <summary>(0018,9064) VR=CS VM=1 k-space Filtering</summary>
		public static DicomTag KspaceFiltering = new DicomTag(0x0018, 0x9064);

		/// <summary>(0018,9065) VR=CS VM=1-2 Time Domain Filtering</summary>
		public static DicomTag TimeDomainFiltering = new DicomTag(0x0018, 0x9065);

		/// <summary>(0018,9066) VR=US VM=1-2 Number of Zero fills</summary>
		public static DicomTag NumberOfZeroFills = new DicomTag(0x0018, 0x9066);

		/// <summary>(0018,9067) VR=CS VM=1 Baseline Correction</summary>
		public static DicomTag BaselineCorrection = new DicomTag(0x0018, 0x9067);

		/// <summary>(0018,9069) VR=FD VM=1 Parallel Reduction Factor In-plane</summary>
		public static DicomTag ParallelReductionFactorInplane = new DicomTag(0x0018, 0x9069);

		/// <summary>(0018,9070) VR=FD VM=1 Cardiac R-R Interval Specified</summary>
		public static DicomTag CardiacRRIntervalSpecified = new DicomTag(0x0018, 0x9070);

		/// <summary>(0018,9073) VR=FD VM=1 Acquisition Duration</summary>
		public static DicomTag AcquisitionDuration = new DicomTag(0x0018, 0x9073);

		/// <summary>(0018,9074) VR=DT VM=1 Frame Acquisition DateTime</summary>
		public static DicomTag FrameAcquisitionDateTime = new DicomTag(0x0018, 0x9074);

		/// <summary>(0018,9075) VR=CS VM=1 Diffusion Directionality</summary>
		public static DicomTag DiffusionDirectionality = new DicomTag(0x0018, 0x9075);

		/// <summary>(0018,9076) VR=SQ VM=1 Diffusion Gradient Direction Sequence</summary>
		public static DicomTag DiffusionGradientDirectionSequence = new DicomTag(0x0018, 0x9076);

		/// <summary>(0018,9077) VR=CS VM=1 Parallel Acquisition</summary>
		public static DicomTag ParallelAcquisition = new DicomTag(0x0018, 0x9077);

		/// <summary>(0018,9078) VR=CS VM=1 Parallel Acquisition Technique</summary>
		public static DicomTag ParallelAcquisitionTechnique = new DicomTag(0x0018, 0x9078);

		/// <summary>(0018,9079) VR=FD VM=1-n Inversion Times</summary>
		public static DicomTag InversionTimes = new DicomTag(0x0018, 0x9079);

		/// <summary>(0018,9080) VR=ST VM=1 Metabolite Map Description</summary>
		public static DicomTag MetaboliteMapDescription = new DicomTag(0x0018, 0x9080);

		/// <summary>(0018,9081) VR=CS VM=1 Partial Fourier</summary>
		public static DicomTag PartialFourier = new DicomTag(0x0018, 0x9081);

		/// <summary>(0018,9082) VR=FD VM=1 Effective Echo Time</summary>
		public static DicomTag EffectiveEchoTime = new DicomTag(0x0018, 0x9082);

		/// <summary>(0018,9083) VR=SQ VM=1 Metabolite Map Code Sequence</summary>
		public static DicomTag MetaboliteMapCodeSequence = new DicomTag(0x0018, 0x9083);

		/// <summary>(0018,9084) VR=SQ VM=1 Chemical Shift Sequence</summary>
		public static DicomTag ChemicalShiftSequence = new DicomTag(0x0018, 0x9084);

		/// <summary>(0018,9085) VR=CS VM=1 Cardiac Signal Source</summary>
		public static DicomTag CardiacSignalSource = new DicomTag(0x0018, 0x9085);

		/// <summary>(0018,9087) VR=FD VM=1 Diffusion b-value</summary>
		public static DicomTag DiffusionBvalue = new DicomTag(0x0018, 0x9087);

		/// <summary>(0018,9089) VR=FD VM=3 Diffusion Gradient Orientation</summary>
		public static DicomTag DiffusionGradientOrientation = new DicomTag(0x0018, 0x9089);

		/// <summary>(0018,9090) VR=FD VM=3 Velocity Encoding Direction</summary>
		public static DicomTag VelocityEncodingDirection = new DicomTag(0x0018, 0x9090);

		/// <summary>(0018,9091) VR=FD VM=1 Velocity Encoding Minimum Value</summary>
		public static DicomTag VelocityEncodingMinimumValue = new DicomTag(0x0018, 0x9091);

		/// <summary>(0018,9093) VR=US VM=1 Number of k-Space Trajectories</summary>
		public static DicomTag NumberOfKSpaceTrajectories = new DicomTag(0x0018, 0x9093);

		/// <summary>(0018,9094) VR=CS VM=1 Coverage of k-Space</summary>
		public static DicomTag CoverageOfKSpace = new DicomTag(0x0018, 0x9094);

		/// <summary>(0018,9095) VR=UL VM=1 Spectroscopy Acquisition Phase Rows</summary>
		public static DicomTag SpectroscopyAcquisitionPhaseRows = new DicomTag(0x0018, 0x9095);

		/// <summary>(0018,9096) VR=FD VM=1 Parallel Reduction Factor In-plane (Retired) (Retired)</summary>
		public static DicomTag ParallelReductionFactorInplaneRetiredRETIRED = new DicomTag(0x0018, 0x9096);

		/// <summary>(0018,9098) VR=FD VM=1-2 Transmitter Frequency</summary>
		public static DicomTag TransmitterFrequency = new DicomTag(0x0018, 0x9098);

		/// <summary>(0018,9100) VR=CS VM=1-2 Resonant Nucleus</summary>
		public static DicomTag ResonantNucleus = new DicomTag(0x0018, 0x9100);

		/// <summary>(0018,9101) VR=CS VM=1 Frequency Correction</summary>
		public static DicomTag FrequencyCorrection = new DicomTag(0x0018, 0x9101);

		/// <summary>(0018,9103) VR=SQ VM=1 MR Spectroscopy FOV/Geometry Sequence</summary>
		public static DicomTag MRSpectroscopyFOVGeometrySequence = new DicomTag(0x0018, 0x9103);

		/// <summary>(0018,9104) VR=FD VM=1 Slab Thickness</summary>
		public static DicomTag SlabThickness = new DicomTag(0x0018, 0x9104);

		/// <summary>(0018,9105) VR=FD VM=3 Slab Orientation</summary>
		public static DicomTag SlabOrientation = new DicomTag(0x0018, 0x9105);

		/// <summary>(0018,9106) VR=FD VM=3 Mid Slab Position</summary>
		public static DicomTag MidSlabPosition = new DicomTag(0x0018, 0x9106);

		/// <summary>(0018,9107) VR=SQ VM=1 MR Spatial Saturation Sequence</summary>
		public static DicomTag MRSpatialSaturationSequence = new DicomTag(0x0018, 0x9107);

		/// <summary>(0018,9112) VR=SQ VM=1 MR Timing and Related Parameters Sequence</summary>
		public static DicomTag MRTimingAndRelatedParametersSequence = new DicomTag(0x0018, 0x9112);

		/// <summary>(0018,9114) VR=SQ VM=1 MR Echo Sequence</summary>
		public static DicomTag MREchoSequence = new DicomTag(0x0018, 0x9114);

		/// <summary>(0018,9115) VR=SQ VM=1 MR Modifier Sequence</summary>
		public static DicomTag MRModifierSequence = new DicomTag(0x0018, 0x9115);

		/// <summary>(0018,9117) VR=SQ VM=1 MR Diffusion Sequence</summary>
		public static DicomTag MRDiffusionSequence = new DicomTag(0x0018, 0x9117);

		/// <summary>(0018,9118) VR=SQ VM=1 Cardiac Synchronization Sequence</summary>
		public static DicomTag CardiacSynchronizationSequence = new DicomTag(0x0018, 0x9118);

		/// <summary>(0018,9119) VR=SQ VM=1 MR Averages Sequence</summary>
		public static DicomTag MRAveragesSequence = new DicomTag(0x0018, 0x9119);

		/// <summary>(0018,9125) VR=SQ VM=1 MR FOV/Geometry Sequence</summary>
		public static DicomTag MRFOVGeometrySequence = new DicomTag(0x0018, 0x9125);

		/// <summary>(0018,9126) VR=SQ VM=1 Volume Localization Sequence</summary>
		public static DicomTag VolumeLocalizationSequence = new DicomTag(0x0018, 0x9126);

		/// <summary>(0018,9127) VR=UL VM=1 Spectroscopy Acquisition Data Columns</summary>
		public static DicomTag SpectroscopyAcquisitionDataColumns = new DicomTag(0x0018, 0x9127);

		/// <summary>(0018,9147) VR=CS VM=1 Diffusion Anisotropy Type</summary>
		public static DicomTag DiffusionAnisotropyType = new DicomTag(0x0018, 0x9147);

		/// <summary>(0018,9151) VR=DT VM=1 Frame Reference DateTime</summary>
		public static DicomTag FrameReferenceDateTime = new DicomTag(0x0018, 0x9151);

		/// <summary>(0018,9152) VR=SQ VM=1 MR Metabolite Map Sequence</summary>
		public static DicomTag MRMetaboliteMapSequence = new DicomTag(0x0018, 0x9152);

		/// <summary>(0018,9155) VR=FD VM=1 Parallel Reduction Factor out-of-plane</summary>
		public static DicomTag ParallelReductionFactorOutofplane = new DicomTag(0x0018, 0x9155);

		/// <summary>(0018,9159) VR=UL VM=1 Spectroscopy Acquisition Out-of-plane Phase Steps</summary>
		public static DicomTag SpectroscopyAcquisitionOutofplanePhaseSteps = new DicomTag(0x0018, 0x9159);

		/// <summary>(0018,9166) VR=CS VM=1 Bulk Motion Status (Retired)</summary>
		public static DicomTag BulkMotionStatusRETIRED = new DicomTag(0x0018, 0x9166);

		/// <summary>(0018,9168) VR=FD VM=1 Parallel Reduction Factor Second In-plane</summary>
		public static DicomTag ParallelReductionFactorSecondInplane = new DicomTag(0x0018, 0x9168);

		/// <summary>(0018,9169) VR=CS VM=1 Cardiac Beat Rejection Technique</summary>
		public static DicomTag CardiacBeatRejectionTechnique = new DicomTag(0x0018, 0x9169);

		/// <summary>(0018,9170) VR=CS VM=1 Respiratory Motion Compensation Technique</summary>
		public static DicomTag RespiratoryMotionCompensationTechnique = new DicomTag(0x0018, 0x9170);

		/// <summary>(0018,9171) VR=CS VM=1 Respiratory Signal Source</summary>
		public static DicomTag RespiratorySignalSource = new DicomTag(0x0018, 0x9171);

		/// <summary>(0018,9172) VR=CS VM=1 Bulk Motion Compensation Technique</summary>
		public static DicomTag BulkMotionCompensationTechnique = new DicomTag(0x0018, 0x9172);

		/// <summary>(0018,9173) VR=CS VM=1 Bulk Motion Signal Source</summary>
		public static DicomTag BulkMotionSignalSource = new DicomTag(0x0018, 0x9173);

		/// <summary>(0018,9174) VR=CS VM=1 Applicable Safety Standard Agency</summary>
		public static DicomTag ApplicableSafetyStandardAgency = new DicomTag(0x0018, 0x9174);

		/// <summary>(0018,9175) VR=LO VM=1 Applicable Safety Standard Description</summary>
		public static DicomTag ApplicableSafetyStandardDescription = new DicomTag(0x0018, 0x9175);

		/// <summary>(0018,9176) VR=SQ VM=1 Operating Mode Sequence</summary>
		public static DicomTag OperatingModeSequence = new DicomTag(0x0018, 0x9176);

		/// <summary>(0018,9177) VR=CS VM=1 Operating Mode Type</summary>
		public static DicomTag OperatingModeType = new DicomTag(0x0018, 0x9177);

		/// <summary>(0018,9178) VR=CS VM=1 Operating Mode</summary>
		public static DicomTag OperatingMode = new DicomTag(0x0018, 0x9178);

		/// <summary>(0018,9179) VR=CS VM=1 Specific Absorption Rate Definition</summary>
		public static DicomTag SpecificAbsorptionRateDefinition = new DicomTag(0x0018, 0x9179);

		/// <summary>(0018,9180) VR=CS VM=1 Gradient Output Type</summary>
		public static DicomTag GradientOutputType = new DicomTag(0x0018, 0x9180);

		/// <summary>(0018,9181) VR=FD VM=1 Specific Absorption Rate Value</summary>
		public static DicomTag SpecificAbsorptionRateValue = new DicomTag(0x0018, 0x9181);

		/// <summary>(0018,9182) VR=FD VM=1 Gradient Output</summary>
		public static DicomTag GradientOutput = new DicomTag(0x0018, 0x9182);

		/// <summary>(0018,9183) VR=CS VM=1 Flow Compensation Direction</summary>
		public static DicomTag FlowCompensationDirection = new DicomTag(0x0018, 0x9183);

		/// <summary>(0018,9184) VR=FD VM=1 Tagging Delay</summary>
		public static DicomTag TaggingDelay = new DicomTag(0x0018, 0x9184);

		/// <summary>(0018,9185) VR=ST VM=1 Respiratory Motion Compensation Technique Description</summary>
		public static DicomTag RespiratoryMotionCompensationTechniqueDescription = new DicomTag(0x0018, 0x9185);

		/// <summary>(0018,9186) VR=SH VM=1 Respiratory Signal Source ID</summary>
		public static DicomTag RespiratorySignalSourceID = new DicomTag(0x0018, 0x9186);

		/// <summary>(0018,9195) VR=FD VM=1 Chemical Shifts Minimum Integration Limit in Hz (Retired)</summary>
		public static DicomTag ChemicalShiftsMinimumIntegrationLimitInHzRETIRED = new DicomTag(0x0018, 0x9195);

		/// <summary>(0018,9196) VR=FD VM=1 Chemical Shifts Maximum Integration Limit in Hz (Retired)</summary>
		public static DicomTag ChemicalShiftsMaximumIntegrationLimitInHzRETIRED = new DicomTag(0x0018, 0x9196);

		/// <summary>(0018,9197) VR=SQ VM=1 MR Velocity Encoding Sequence</summary>
		public static DicomTag MRVelocityEncodingSequence = new DicomTag(0x0018, 0x9197);

		/// <summary>(0018,9198) VR=CS VM=1 First Order Phase Correction</summary>
		public static DicomTag FirstOrderPhaseCorrection = new DicomTag(0x0018, 0x9198);

		/// <summary>(0018,9199) VR=CS VM=1 Water Referenced Phase Correction</summary>
		public static DicomTag WaterReferencedPhaseCorrection = new DicomTag(0x0018, 0x9199);

		/// <summary>(0018,9200) VR=CS VM=1 MR Spectroscopy Acquisition Type</summary>
		public static DicomTag MRSpectroscopyAcquisitionType = new DicomTag(0x0018, 0x9200);

		/// <summary>(0018,9214) VR=CS VM=1 Respiratory Cycle Position</summary>
		public static DicomTag RespiratoryCyclePosition = new DicomTag(0x0018, 0x9214);

		/// <summary>(0018,9217) VR=FD VM=1 Velocity Encoding Maximum Value</summary>
		public static DicomTag VelocityEncodingMaximumValue = new DicomTag(0x0018, 0x9217);

		/// <summary>(0018,9218) VR=FD VM=1 Tag Spacing Second Dimension</summary>
		public static DicomTag TagSpacingSecondDimension = new DicomTag(0x0018, 0x9218);

		/// <summary>(0018,9219) VR=SS VM=1 Tag Angle Second Axis</summary>
		public static DicomTag TagAngleSecondAxis = new DicomTag(0x0018, 0x9219);

		/// <summary>(0018,9220) VR=FD VM=1 Frame Acquisition Duration</summary>
		public static DicomTag FrameAcquisitionDuration = new DicomTag(0x0018, 0x9220);

		/// <summary>(0018,9226) VR=SQ VM=1 MR Image Frame Type Sequence</summary>
		public static DicomTag MRImageFrameTypeSequence = new DicomTag(0x0018, 0x9226);

		/// <summary>(0018,9227) VR=SQ VM=1 MR Spectroscopy Frame Type Sequence</summary>
		public static DicomTag MRSpectroscopyFrameTypeSequence = new DicomTag(0x0018, 0x9227);

		/// <summary>(0018,9231) VR=US VM=1 MR Acquisition Phase Encoding Steps in-plane</summary>
		public static DicomTag MRAcquisitionPhaseEncodingStepsInplane = new DicomTag(0x0018, 0x9231);

		/// <summary>(0018,9232) VR=US VM=1 MR Acquisition Phase Encoding Steps out-of-plane</summary>
		public static DicomTag MRAcquisitionPhaseEncodingStepsOutofplane = new DicomTag(0x0018, 0x9232);

		/// <summary>(0018,9234) VR=UL VM=1 Spectroscopy Acquisition Phase Columns</summary>
		public static DicomTag SpectroscopyAcquisitionPhaseColumns = new DicomTag(0x0018, 0x9234);

		/// <summary>(0018,9236) VR=CS VM=1 Cardiac Cycle Position</summary>
		public static DicomTag CardiacCyclePosition = new DicomTag(0x0018, 0x9236);

		/// <summary>(0018,9239) VR=SQ VM=1 Specific Absorption Rate Sequence</summary>
		public static DicomTag SpecificAbsorptionRateSequence = new DicomTag(0x0018, 0x9239);

		/// <summary>(0018,9240) VR=US VM=1 RF Echo Train Length</summary>
		public static DicomTag RFEchoTrainLength = new DicomTag(0x0018, 0x9240);

		/// <summary>(0018,9241) VR=US VM=1 Gradient Echo Train Length</summary>
		public static DicomTag GradientEchoTrainLength = new DicomTag(0x0018, 0x9241);

		/// <summary>(0018,9295) VR=FD VM=1 Chemical Shifts Minimum Integration Limit in ppm</summary>
		public static DicomTag ChemicalShiftsMinimumIntegrationLimitInPpm = new DicomTag(0x0018, 0x9295);

		/// <summary>(0018,9296) VR=FD VM=1 Chemical Shifts Maximum Integration Limit in ppm</summary>
		public static DicomTag ChemicalShiftsMaximumIntegrationLimitInPpm = new DicomTag(0x0018, 0x9296);

		/// <summary>(0018,9301) VR=SQ VM=1 CT Acquisition Type Sequence</summary>
		public static DicomTag CTAcquisitionTypeSequence = new DicomTag(0x0018, 0x9301);

		/// <summary>(0018,9302) VR=CS VM=1 Acquisition Type</summary>
		public static DicomTag AcquisitionType = new DicomTag(0x0018, 0x9302);

		/// <summary>(0018,9303) VR=FD VM=1 Tube Angle</summary>
		public static DicomTag TubeAngle = new DicomTag(0x0018, 0x9303);

		/// <summary>(0018,9304) VR=SQ VM=1 CT Acquisition Details Sequence</summary>
		public static DicomTag CTAcquisitionDetailsSequence = new DicomTag(0x0018, 0x9304);

		/// <summary>(0018,9305) VR=FD VM=1 Revolution Time</summary>
		public static DicomTag RevolutionTime = new DicomTag(0x0018, 0x9305);

		/// <summary>(0018,9306) VR=FD VM=1 Single Collimation Width</summary>
		public static DicomTag SingleCollimationWidth = new DicomTag(0x0018, 0x9306);

		/// <summary>(0018,9307) VR=FD VM=1 Total Collimation Width</summary>
		public static DicomTag TotalCollimationWidth = new DicomTag(0x0018, 0x9307);

		/// <summary>(0018,9308) VR=SQ VM=1 CT Table Dynamics Sequence</summary>
		public static DicomTag CTTableDynamicsSequence = new DicomTag(0x0018, 0x9308);

		/// <summary>(0018,9309) VR=FD VM=1 Table Speed</summary>
		public static DicomTag TableSpeed = new DicomTag(0x0018, 0x9309);

		/// <summary>(0018,9310) VR=FD VM=1 Table Feed per Rotation</summary>
		public static DicomTag TableFeedPerRotation = new DicomTag(0x0018, 0x9310);

		/// <summary>(0018,9311) VR=FD VM=1 Spiral Pitch Factor</summary>
		public static DicomTag SpiralPitchFactor = new DicomTag(0x0018, 0x9311);

		/// <summary>(0018,9312) VR=SQ VM=1 CT Geometry Sequence</summary>
		public static DicomTag CTGeometrySequence = new DicomTag(0x0018, 0x9312);

		/// <summary>(0018,9313) VR=FD VM=3 Data Collection Center (Patient)</summary>
		public static DicomTag DataCollectionCenterPatient = new DicomTag(0x0018, 0x9313);

		/// <summary>(0018,9314) VR=SQ VM=1 CT Reconstruction Sequence</summary>
		public static DicomTag CTReconstructionSequence = new DicomTag(0x0018, 0x9314);

		/// <summary>(0018,9315) VR=CS VM=1 Reconstruction Algorithm</summary>
		public static DicomTag ReconstructionAlgorithm = new DicomTag(0x0018, 0x9315);

		/// <summary>(0018,9316) VR=CS VM=1 Convolution Kernel Group</summary>
		public static DicomTag ConvolutionKernelGroup = new DicomTag(0x0018, 0x9316);

		/// <summary>(0018,9317) VR=FD VM=2 Reconstruction Field of View</summary>
		public static DicomTag ReconstructionFieldOfView = new DicomTag(0x0018, 0x9317);

		/// <summary>(0018,9318) VR=FD VM=3 Reconstruction Target Center (Patient)</summary>
		public static DicomTag ReconstructionTargetCenterPatient = new DicomTag(0x0018, 0x9318);

		/// <summary>(0018,9319) VR=FD VM=1 Reconstruction Angle</summary>
		public static DicomTag ReconstructionAngle = new DicomTag(0x0018, 0x9319);

		/// <summary>(0018,9320) VR=SH VM=1 Image Filter</summary>
		public static DicomTag ImageFilter = new DicomTag(0x0018, 0x9320);

		/// <summary>(0018,9321) VR=SQ VM=1 CT Exposure Sequence</summary>
		public static DicomTag CTExposureSequence = new DicomTag(0x0018, 0x9321);

		/// <summary>(0018,9322) VR=FD VM=2 Reconstruction Pixel Spacing</summary>
		public static DicomTag ReconstructionPixelSpacing = new DicomTag(0x0018, 0x9322);

		/// <summary>(0018,9323) VR=CS VM=1 Exposure Modulation Type</summary>
		public static DicomTag ExposureModulationType = new DicomTag(0x0018, 0x9323);

		/// <summary>(0018,9324) VR=FD VM=1 Estimated Dose Saving</summary>
		public static DicomTag EstimatedDoseSaving = new DicomTag(0x0018, 0x9324);

		/// <summary>(0018,9325) VR=SQ VM=1 CT X-Ray Details Sequence</summary>
		public static DicomTag CTXRayDetailsSequence = new DicomTag(0x0018, 0x9325);

		/// <summary>(0018,9326) VR=SQ VM=1 CT Position Sequence</summary>
		public static DicomTag CTPositionSequence = new DicomTag(0x0018, 0x9326);

		/// <summary>(0018,9327) VR=FD VM=1 Table Position</summary>
		public static DicomTag TablePosition = new DicomTag(0x0018, 0x9327);

		/// <summary>(0018,9328) VR=FD VM=1 Exposure Time in ms</summary>
		public static DicomTag ExposureTimeInMs = new DicomTag(0x0018, 0x9328);

		/// <summary>(0018,9329) VR=SQ VM=1 CT Image Frame Type Sequence</summary>
		public static DicomTag CTImageFrameTypeSequence = new DicomTag(0x0018, 0x9329);

		/// <summary>(0018,9330) VR=FD VM=1 X-Ray Tube Current in mA</summary>
		public static DicomTag XRayTubeCurrentInMA = new DicomTag(0x0018, 0x9330);

		/// <summary>(0018,9332) VR=FD VM=1 Exposure in mAs</summary>
		public static DicomTag ExposureInMAs = new DicomTag(0x0018, 0x9332);

		/// <summary>(0018,9333) VR=CS VM=1 Constant Volume Flag</summary>
		public static DicomTag ConstantVolumeFlag = new DicomTag(0x0018, 0x9333);

		/// <summary>(0018,9334) VR=CS VM=1 Fluoroscopy Flag</summary>
		public static DicomTag FluoroscopyFlag = new DicomTag(0x0018, 0x9334);

		/// <summary>(0018,9335) VR=FD VM=1 Distance Source to Data Collection Center</summary>
		public static DicomTag DistanceSourceToDataCollectionCenter = new DicomTag(0x0018, 0x9335);

		/// <summary>(0018,9337) VR=US VM=1 Contrast/Bolus Agent Number</summary>
		public static DicomTag ContrastBolusAgentNumber = new DicomTag(0x0018, 0x9337);

		/// <summary>(0018,9338) VR=SQ VM=1 Contrast/Bolus Ingredient Code Sequence</summary>
		public static DicomTag ContrastBolusIngredientCodeSequence = new DicomTag(0x0018, 0x9338);

		/// <summary>(0018,9340) VR=SQ VM=1 Contrast Administration Profile Sequence</summary>
		public static DicomTag ContrastAdministrationProfileSequence = new DicomTag(0x0018, 0x9340);

		/// <summary>(0018,9341) VR=SQ VM=1 Contrast/Bolus Usage Sequence</summary>
		public static DicomTag ContrastBolusUsageSequence = new DicomTag(0x0018, 0x9341);

		/// <summary>(0018,9342) VR=CS VM=1 Contrast/Bolus Agent Administered</summary>
		public static DicomTag ContrastBolusAgentAdministered = new DicomTag(0x0018, 0x9342);

		/// <summary>(0018,9343) VR=CS VM=1 Contrast/Bolus Agent Detected</summary>
		public static DicomTag ContrastBolusAgentDetected = new DicomTag(0x0018, 0x9343);

		/// <summary>(0018,9344) VR=CS VM=1 Contrast/Bolus Agent Phase</summary>
		public static DicomTag ContrastBolusAgentPhase = new DicomTag(0x0018, 0x9344);

		/// <summary>(0018,9345) VR=FD VM=1 CTDIvol</summary>
		public static DicomTag CTDIvol = new DicomTag(0x0018, 0x9345);

		/// <summary>(0018,9346) VR=SQ VM=1 CTDI Phantom Type Code Sequence</summary>
		public static DicomTag CTDIPhantomTypeCodeSequence = new DicomTag(0x0018, 0x9346);

		/// <summary>(0018,9351) VR=FL VM=1 Calcium Scoring Mass Factor Patient</summary>
		public static DicomTag CalciumScoringMassFactorPatient = new DicomTag(0x0018, 0x9351);

		/// <summary>(0018,9352) VR=FL VM=3 Calcium Scoring Mass Factor Device</summary>
		public static DicomTag CalciumScoringMassFactorDevice = new DicomTag(0x0018, 0x9352);

		/// <summary>(0018,9360) VR=SQ VM=1 CT Additional X-Ray Source Sequence</summary>
		public static DicomTag CTAdditionalXRaySourceSequence = new DicomTag(0x0018, 0x9360);

		/// <summary>(0018,9401) VR=SQ VM=1 Projection Pixel Calibration Sequence</summary>
		public static DicomTag ProjectionPixelCalibrationSequence = new DicomTag(0x0018, 0x9401);

		/// <summary>(0018,9402) VR=FL VM=1 Distance Source to Isocenter</summary>
		public static DicomTag DistanceSourceToIsocenter = new DicomTag(0x0018, 0x9402);

		/// <summary>(0018,9403) VR=FL VM=1 Distance Object to Table Top</summary>
		public static DicomTag DistanceObjectToTableTop = new DicomTag(0x0018, 0x9403);

		/// <summary>(0018,9404) VR=FL VM=2 Object Pixel Spacing in Center of Beam</summary>
		public static DicomTag ObjectPixelSpacingInCenterOfBeam = new DicomTag(0x0018, 0x9404);

		/// <summary>(0018,9405) VR=SQ VM=1 Positioner Position Sequence</summary>
		public static DicomTag PositionerPositionSequence = new DicomTag(0x0018, 0x9405);

		/// <summary>(0018,9406) VR=SQ VM=1 Table Position Sequence</summary>
		public static DicomTag TablePositionSequence = new DicomTag(0x0018, 0x9406);

		/// <summary>(0018,9407) VR=SQ VM=1 Collimator Shape Sequence</summary>
		public static DicomTag CollimatorShapeSequence = new DicomTag(0x0018, 0x9407);

		/// <summary>(0018,9412) VR=SQ VM=1 XA/XRF Frame Characteristics Sequence</summary>
		public static DicomTag XAXRFFrameCharacteristicsSequence = new DicomTag(0x0018, 0x9412);

		/// <summary>(0018,9417) VR=SQ VM=1 Frame Acquisition Sequence</summary>
		public static DicomTag FrameAcquisitionSequence = new DicomTag(0x0018, 0x9417);

		/// <summary>(0018,9420) VR=CS VM=1 X-Ray Receptor Type</summary>
		public static DicomTag XRayReceptorType = new DicomTag(0x0018, 0x9420);

		/// <summary>(0018,9423) VR=LO VM=1 Acquisition Protocol Name</summary>
		public static DicomTag AcquisitionProtocolName = new DicomTag(0x0018, 0x9423);

		/// <summary>(0018,9424) VR=LT VM=1 Acquisition Protocol Description</summary>
		public static DicomTag AcquisitionProtocolDescription = new DicomTag(0x0018, 0x9424);

		/// <summary>(0018,9425) VR=CS VM=1 Contrast/Bolus Ingredient Opaque</summary>
		public static DicomTag ContrastBolusIngredientOpaque = new DicomTag(0x0018, 0x9425);

		/// <summary>(0018,9426) VR=FL VM=1 Distance Receptor Plane to Detector Housing</summary>
		public static DicomTag DistanceReceptorPlaneToDetectorHousing = new DicomTag(0x0018, 0x9426);

		/// <summary>(0018,9427) VR=CS VM=1 Intensifier Active Shape</summary>
		public static DicomTag IntensifierActiveShape = new DicomTag(0x0018, 0x9427);

		/// <summary>(0018,9428) VR=FL VM=1-2 Intensifier Active Dimension(s)</summary>
		public static DicomTag IntensifierActiveDimensions = new DicomTag(0x0018, 0x9428);

		/// <summary>(0018,9429) VR=FL VM=2 Physical Detector Size</summary>
		public static DicomTag PhysicalDetectorSize = new DicomTag(0x0018, 0x9429);

		/// <summary>(0018,9430) VR=US VM=2 Position of Isocenter Projection</summary>
		public static DicomTag PositionOfIsocenterProjection = new DicomTag(0x0018, 0x9430);

		/// <summary>(0018,9432) VR=SQ VM=1 Field of View Sequence</summary>
		public static DicomTag FieldOfViewSequence = new DicomTag(0x0018, 0x9432);

		/// <summary>(0018,9433) VR=LO VM=1 Field of View Description</summary>
		public static DicomTag FieldOfViewDescription = new DicomTag(0x0018, 0x9433);

		/// <summary>(0018,9434) VR=SQ VM=1 Exposure Control Sensing Regions Sequence</summary>
		public static DicomTag ExposureControlSensingRegionsSequence = new DicomTag(0x0018, 0x9434);

		/// <summary>(0018,9435) VR=CS VM=1 Exposure Control Sensing Region Shape</summary>
		public static DicomTag ExposureControlSensingRegionShape = new DicomTag(0x0018, 0x9435);

		/// <summary>(0018,9436) VR=SS VM=1 Exposure Control Sensing Region Left Vertical Edge</summary>
		public static DicomTag ExposureControlSensingRegionLeftVerticalEdge = new DicomTag(0x0018, 0x9436);

		/// <summary>(0018,9437) VR=SS VM=1 Exposure Control Sensing Region Right Vertical Edge</summary>
		public static DicomTag ExposureControlSensingRegionRightVerticalEdge = new DicomTag(0x0018, 0x9437);

		/// <summary>(0018,9438) VR=SS VM=1 Exposure Control Sensing Region Upper Horizontal Edge</summary>
		public static DicomTag ExposureControlSensingRegionUpperHorizontalEdge = new DicomTag(0x0018, 0x9438);

		/// <summary>(0018,9439) VR=SS VM=1 Exposure Control Sensing Region Lower Horizontal Edge</summary>
		public static DicomTag ExposureControlSensingRegionLowerHorizontalEdge = new DicomTag(0x0018, 0x9439);

		/// <summary>(0018,9440) VR=SS VM=2 Center of Circular Exposure Control Sensing Region</summary>
		public static DicomTag CenterOfCircularExposureControlSensingRegion = new DicomTag(0x0018, 0x9440);

		/// <summary>(0018,9441) VR=US VM=1 Radius of Circular Exposure Control Sensing Region</summary>
		public static DicomTag RadiusOfCircularExposureControlSensingRegion = new DicomTag(0x0018, 0x9441);

		/// <summary>(0018,9442) VR=SS VM=2-n Vertices of the Polygonal Exposure Control Sensing Region</summary>
		public static DicomTag VerticesOfThePolygonalExposureControlSensingRegion = new DicomTag(0x0018, 0x9442);

		/// <summary>(0018,9445) VR=NONE VM=  (Retired)</summary>
		public static DicomTag RETIRED = new DicomTag(0x0018, 0x9445);

		/// <summary>(0018,9447) VR=FL VM=1 Column Angulation (Patient)</summary>
		public static DicomTag ColumnAngulationPatient = new DicomTag(0x0018, 0x9447);

		/// <summary>(0018,9449) VR=FL VM=1 Beam Angle</summary>
		public static DicomTag BeamAngle = new DicomTag(0x0018, 0x9449);

		/// <summary>(0018,9451) VR=SQ VM=1 Frame Detector Parameters Sequence</summary>
		public static DicomTag FrameDetectorParametersSequence = new DicomTag(0x0018, 0x9451);

		/// <summary>(0018,9452) VR=FL VM=1 Calculated Anatomy Thickness</summary>
		public static DicomTag CalculatedAnatomyThickness = new DicomTag(0x0018, 0x9452);

		/// <summary>(0018,9455) VR=SQ VM=1 Calibration Sequence</summary>
		public static DicomTag CalibrationSequence = new DicomTag(0x0018, 0x9455);

		/// <summary>(0018,9456) VR=SQ VM=1 Object Thickness Sequence</summary>
		public static DicomTag ObjectThicknessSequence = new DicomTag(0x0018, 0x9456);

		/// <summary>(0018,9457) VR=CS VM=1 Plane Identification</summary>
		public static DicomTag PlaneIdentification = new DicomTag(0x0018, 0x9457);

		/// <summary>(0018,9461) VR=FL VM=1-2 Field of View Dimension(s) in Float</summary>
		public static DicomTag FieldOfViewDimensionsInFloat = new DicomTag(0x0018, 0x9461);

		/// <summary>(0018,9462) VR=SQ VM=1 Isocenter Reference System Sequence</summary>
		public static DicomTag IsocenterReferenceSystemSequence = new DicomTag(0x0018, 0x9462);

		/// <summary>(0018,9463) VR=FL VM=1 Positioner Isocenter Primary Angle</summary>
		public static DicomTag PositionerIsocenterPrimaryAngle = new DicomTag(0x0018, 0x9463);

		/// <summary>(0018,9464) VR=FL VM=1 Positioner Isocenter Secondary Angle</summary>
		public static DicomTag PositionerIsocenterSecondaryAngle = new DicomTag(0x0018, 0x9464);

		/// <summary>(0018,9465) VR=FL VM=1 Positioner Isocenter Detector Rotation Angle</summary>
		public static DicomTag PositionerIsocenterDetectorRotationAngle = new DicomTag(0x0018, 0x9465);

		/// <summary>(0018,9466) VR=FL VM=1 Table X Position to Isocenter</summary>
		public static DicomTag TableXPositionToIsocenter = new DicomTag(0x0018, 0x9466);

		/// <summary>(0018,9467) VR=FL VM=1 Table Y Position to Isocenter</summary>
		public static DicomTag TableYPositionToIsocenter = new DicomTag(0x0018, 0x9467);

		/// <summary>(0018,9468) VR=FL VM=1 Table Z Position to Isocenter</summary>
		public static DicomTag TableZPositionToIsocenter = new DicomTag(0x0018, 0x9468);

		/// <summary>(0018,9469) VR=FL VM=1 Table Horizontal Rotation Angle</summary>
		public static DicomTag TableHorizontalRotationAngle = new DicomTag(0x0018, 0x9469);

		/// <summary>(0018,9470) VR=FL VM=1 Table Head Tilt Angle</summary>
		public static DicomTag TableHeadTiltAngle = new DicomTag(0x0018, 0x9470);

		/// <summary>(0018,9471) VR=FL VM=1 Table Cradle Tilt Angle</summary>
		public static DicomTag TableCradleTiltAngle = new DicomTag(0x0018, 0x9471);

		/// <summary>(0018,9472) VR=SQ VM=1 Frame Display Shutter Sequence</summary>
		public static DicomTag FrameDisplayShutterSequence = new DicomTag(0x0018, 0x9472);

		/// <summary>(0018,9473) VR=FL VM=1 Acquired Image Area Dose Product</summary>
		public static DicomTag AcquiredImageAreaDoseProduct = new DicomTag(0x0018, 0x9473);

		/// <summary>(0018,9474) VR=CS VM=1 C-arm Positioner Tabletop Relationship</summary>
		public static DicomTag CarmPositionerTabletopRelationship = new DicomTag(0x0018, 0x9474);

		/// <summary>(0018,9476) VR=SQ VM=1 X-Ray Geometry Sequence</summary>
		public static DicomTag XRayGeometrySequence = new DicomTag(0x0018, 0x9476);

		/// <summary>(0018,9477) VR=SQ VM=1 Irradiation Event Identification Sequence</summary>
		public static DicomTag IrradiationEventIdentificationSequence = new DicomTag(0x0018, 0x9477);

		/// <summary>(0018,9504) VR=SQ VM=1 X-Ray 3D Frame Type Sequence</summary>
		public static DicomTag XRay3DFrameTypeSequence = new DicomTag(0x0018, 0x9504);

		/// <summary>(0018,9506) VR=SQ VM=1 Contributing Sources Sequence</summary>
		public static DicomTag ContributingSourcesSequence = new DicomTag(0x0018, 0x9506);

		/// <summary>(0018,9507) VR=SQ VM=1 X-Ray 3D Acquisition Sequence</summary>
		public static DicomTag XRay3DAcquisitionSequence = new DicomTag(0x0018, 0x9507);

		/// <summary>(0018,9508) VR=FL VM=1 Primary Positioner Scan Arc</summary>
		public static DicomTag PrimaryPositionerScanArc = new DicomTag(0x0018, 0x9508);

		/// <summary>(0018,9509) VR=FL VM=1 Secondary Positioner Scan Arc</summary>
		public static DicomTag SecondaryPositionerScanArc = new DicomTag(0x0018, 0x9509);

		/// <summary>(0018,9510) VR=FL VM=1 Primary Positioner Scan Start Angle</summary>
		public static DicomTag PrimaryPositionerScanStartAngle = new DicomTag(0x0018, 0x9510);

		/// <summary>(0018,9511) VR=FL VM=1 Secondary Positioner Scan Start Angle</summary>
		public static DicomTag SecondaryPositionerScanStartAngle = new DicomTag(0x0018, 0x9511);

		/// <summary>(0018,9514) VR=FL VM=1 Primary Positioner Increment</summary>
		public static DicomTag PrimaryPositionerIncrement = new DicomTag(0x0018, 0x9514);

		/// <summary>(0018,9515) VR=FL VM=1 Secondary Positioner Increment</summary>
		public static DicomTag SecondaryPositionerIncrement = new DicomTag(0x0018, 0x9515);

		/// <summary>(0018,9516) VR=DT VM=1 Start Acquisition DateTime</summary>
		public static DicomTag StartAcquisitionDateTime = new DicomTag(0x0018, 0x9516);

		/// <summary>(0018,9517) VR=DT VM=1 End Acquisition DateTime</summary>
		public static DicomTag EndAcquisitionDateTime = new DicomTag(0x0018, 0x9517);

		/// <summary>(0018,9524) VR=LO VM=1 Application Name</summary>
		public static DicomTag ApplicationName = new DicomTag(0x0018, 0x9524);

		/// <summary>(0018,9525) VR=LO VM=1 Application Version</summary>
		public static DicomTag ApplicationVersion = new DicomTag(0x0018, 0x9525);

		/// <summary>(0018,9526) VR=LO VM=1 Application Manufacturer</summary>
		public static DicomTag ApplicationManufacturer = new DicomTag(0x0018, 0x9526);

		/// <summary>(0018,9527) VR=CS VM=1 Algorithm Type</summary>
		public static DicomTag AlgorithmType = new DicomTag(0x0018, 0x9527);

		/// <summary>(0018,9528) VR=LO VM=1 Algorithm Description</summary>
		public static DicomTag AlgorithmDescription = new DicomTag(0x0018, 0x9528);

		/// <summary>(0018,9530) VR=SQ VM=1 X-Ray 3D Reconstruction Sequence</summary>
		public static DicomTag XRay3DReconstructionSequence = new DicomTag(0x0018, 0x9530);

		/// <summary>(0018,9531) VR=LO VM=1 Reconstruction Description</summary>
		public static DicomTag ReconstructionDescription = new DicomTag(0x0018, 0x9531);

		/// <summary>(0018,9538) VR=SQ VM=1 Per Projection Acquisition Sequence</summary>
		public static DicomTag PerProjectionAcquisitionSequence = new DicomTag(0x0018, 0x9538);

		/// <summary>(0018,9601) VR=SQ VM=1 Diffusion b-matrix Sequence</summary>
		public static DicomTag DiffusionBmatrixSequence = new DicomTag(0x0018, 0x9601);

		/// <summary>(0018,9602) VR=FD VM=1 Diffusion b-value XX</summary>
		public static DicomTag DiffusionBvalueXX = new DicomTag(0x0018, 0x9602);

		/// <summary>(0018,9603) VR=FD VM=1 Diffusion b-value XY</summary>
		public static DicomTag DiffusionBvalueXY = new DicomTag(0x0018, 0x9603);

		/// <summary>(0018,9604) VR=FD VM=1 Diffusion b-value XZ</summary>
		public static DicomTag DiffusionBvalueXZ = new DicomTag(0x0018, 0x9604);

		/// <summary>(0018,9605) VR=FD VM=1 Diffusion b-value YY</summary>
		public static DicomTag DiffusionBvalueYY = new DicomTag(0x0018, 0x9605);

		/// <summary>(0018,9606) VR=FD VM=1 Diffusion b-value YZ</summary>
		public static DicomTag DiffusionBvalueYZ = new DicomTag(0x0018, 0x9606);

		/// <summary>(0018,9607) VR=FD VM=1 Diffusion b-value ZZ</summary>
		public static DicomTag DiffusionBvalueZZ = new DicomTag(0x0018, 0x9607);

		/// <summary>(0018,a001) VR=SQ VM=1 Contributing Equipment Sequence</summary>
		public static DicomTag ContributingEquipmentSequence = new DicomTag(0x0018, 0xa001);

		/// <summary>(0018,a002) VR=DT VM=1 Contribution Date Time</summary>
		public static DicomTag ContributionDateTime = new DicomTag(0x0018, 0xa002);

		/// <summary>(0018,a003) VR=ST VM=1 Contribution Description</summary>
		public static DicomTag ContributionDescription = new DicomTag(0x0018, 0xa003);

		/// <summary>(0020,000d) VR=UI VM=1 Study Instance UID</summary>
		public static DicomTag StudyInstanceUID = new DicomTag(0x0020, 0x000d);

		/// <summary>(0020,000e) VR=UI VM=1 Series Instance UID</summary>
		public static DicomTag SeriesInstanceUID = new DicomTag(0x0020, 0x000e);

		/// <summary>(0020,0010) VR=SH VM=1 Study ID</summary>
		public static DicomTag StudyID = new DicomTag(0x0020, 0x0010);

		/// <summary>(0020,0011) VR=IS VM=1 Series Number</summary>
		public static DicomTag SeriesNumber = new DicomTag(0x0020, 0x0011);

		/// <summary>(0020,0012) VR=IS VM=1 Acquisition Number</summary>
		public static DicomTag AcquisitionNumber = new DicomTag(0x0020, 0x0012);

		/// <summary>(0020,0013) VR=IS VM=1 Instance Number</summary>
		public static DicomTag InstanceNumber = new DicomTag(0x0020, 0x0013);

		/// <summary>(0020,0014) VR=IS VM=1 Isotope Number (Retired)</summary>
		public static DicomTag IsotopeNumberRETIRED = new DicomTag(0x0020, 0x0014);

		/// <summary>(0020,0015) VR=IS VM=1 Phase Number (Retired)</summary>
		public static DicomTag PhaseNumberRETIRED = new DicomTag(0x0020, 0x0015);

		/// <summary>(0020,0016) VR=IS VM=1 Interval Number (Retired)</summary>
		public static DicomTag IntervalNumberRETIRED = new DicomTag(0x0020, 0x0016);

		/// <summary>(0020,0017) VR=IS VM=1 Time Slot Number (Retired)</summary>
		public static DicomTag TimeSlotNumberRETIRED = new DicomTag(0x0020, 0x0017);

		/// <summary>(0020,0018) VR=IS VM=1 Angle Number (Retired)</summary>
		public static DicomTag AngleNumberRETIRED = new DicomTag(0x0020, 0x0018);

		/// <summary>(0020,0019) VR=IS VM=1 Item Number</summary>
		public static DicomTag ItemNumber = new DicomTag(0x0020, 0x0019);

		/// <summary>(0020,0020) VR=CS VM=2 Patient Orientation</summary>
		public static DicomTag PatientOrientation = new DicomTag(0x0020, 0x0020);

		/// <summary>(0020,0022) VR=IS VM=1 Overlay Number (Retired)</summary>
		public static DicomTag OverlayNumberRETIRED = new DicomTag(0x0020, 0x0022);

		/// <summary>(0020,0024) VR=IS VM=1 Curve Number (Retired)</summary>
		public static DicomTag CurveNumberRETIRED = new DicomTag(0x0020, 0x0024);

		/// <summary>(0020,0026) VR=IS VM=1 Lookup Table Number (Retired)</summary>
		public static DicomTag LookupTableNumberRETIRED = new DicomTag(0x0020, 0x0026);

		/// <summary>(0020,0030) VR=DS VM=3 Image Position (Retired)</summary>
		public static DicomTag ImagePositionRETIRED = new DicomTag(0x0020, 0x0030);

		/// <summary>(0020,0032) VR=DS VM=3 Image Position (Patient)</summary>
		public static DicomTag ImagePositionPatient = new DicomTag(0x0020, 0x0032);

		/// <summary>(0020,0035) VR=DS VM=6 Image Orientation (Retired)</summary>
		public static DicomTag ImageOrientationRETIRED = new DicomTag(0x0020, 0x0035);

		/// <summary>(0020,0037) VR=DS VM=6 Image Orientation (Patient)</summary>
		public static DicomTag ImageOrientationPatient = new DicomTag(0x0020, 0x0037);

		/// <summary>(0020,0050) VR=DS VM=1 Location (Retired)</summary>
		public static DicomTag LocationRETIRED = new DicomTag(0x0020, 0x0050);

		/// <summary>(0020,0052) VR=UI VM=1 Frame of Reference UID</summary>
		public static DicomTag FrameOfReferenceUID = new DicomTag(0x0020, 0x0052);

		/// <summary>(0020,0060) VR=CS VM=1 Laterality</summary>
		public static DicomTag Laterality = new DicomTag(0x0020, 0x0060);

		/// <summary>(0020,0062) VR=CS VM=1 Image Laterality</summary>
		public static DicomTag ImageLaterality = new DicomTag(0x0020, 0x0062);

		/// <summary>(0020,0070) VR=LO VM=1 Image Geometry Type (Retired)</summary>
		public static DicomTag ImageGeometryTypeRETIRED = new DicomTag(0x0020, 0x0070);

		/// <summary>(0020,0080) VR=CS VM=1-n Masking Image (Retired)</summary>
		public static DicomTag MaskingImageRETIRED = new DicomTag(0x0020, 0x0080);

		/// <summary>(0020,0100) VR=IS VM=1 Temporal Position Identifier</summary>
		public static DicomTag TemporalPositionIdentifier = new DicomTag(0x0020, 0x0100);

		/// <summary>(0020,0105) VR=IS VM=1 Number of Temporal Positions</summary>
		public static DicomTag NumberOfTemporalPositions = new DicomTag(0x0020, 0x0105);

		/// <summary>(0020,0110) VR=DS VM=1 Temporal Resolution</summary>
		public static DicomTag TemporalResolution = new DicomTag(0x0020, 0x0110);

		/// <summary>(0020,0200) VR=UI VM=1 Synchronization Frame of Reference UID</summary>
		public static DicomTag SynchronizationFrameOfReferenceUID = new DicomTag(0x0020, 0x0200);

		/// <summary>(0020,1000) VR=IS VM=1 Series in Study (Retired)</summary>
		public static DicomTag SeriesInStudyRETIRED = new DicomTag(0x0020, 0x1000);

		/// <summary>(0020,1001) VR=IS VM=1 Acquisitions in Series (Retired)</summary>
		public static DicomTag AcquisitionsInSeriesRETIRED = new DicomTag(0x0020, 0x1001);

		/// <summary>(0020,1002) VR=IS VM=1 Images in Acquisition</summary>
		public static DicomTag ImagesInAcquisition = new DicomTag(0x0020, 0x1002);

		/// <summary>(0020,1003) VR=IS VM=1 Images in Series (Retired)</summary>
		public static DicomTag ImagesInSeriesRETIRED = new DicomTag(0x0020, 0x1003);

		/// <summary>(0020,1004) VR=IS VM=1 Acquisitions in Study (Retired)</summary>
		public static DicomTag AcquisitionsInStudyRETIRED = new DicomTag(0x0020, 0x1004);

		/// <summary>(0020,1005) VR=IS VM=1 Images in Study (Retired)</summary>
		public static DicomTag ImagesInStudyRETIRED = new DicomTag(0x0020, 0x1005);

		/// <summary>(0020,1020) VR=CS VM=1-n Reference (Retired)</summary>
		public static DicomTag ReferenceRETIRED = new DicomTag(0x0020, 0x1020);

		/// <summary>(0020,1040) VR=LO VM=1 Position Reference Indicator</summary>
		public static DicomTag PositionReferenceIndicator = new DicomTag(0x0020, 0x1040);

		/// <summary>(0020,1041) VR=DS VM=1 Slice Location</summary>
		public static DicomTag SliceLocation = new DicomTag(0x0020, 0x1041);

		/// <summary>(0020,1070) VR=IS VM=1-n Other Study Numbers (Retired)</summary>
		public static DicomTag OtherStudyNumbersRETIRED = new DicomTag(0x0020, 0x1070);

		/// <summary>(0020,1200) VR=IS VM=1 Number of Patient Related Studies</summary>
		public static DicomTag NumberOfPatientRelatedStudies = new DicomTag(0x0020, 0x1200);

		/// <summary>(0020,1202) VR=IS VM=1 Number of Patient Related Series</summary>
		public static DicomTag NumberOfPatientRelatedSeries = new DicomTag(0x0020, 0x1202);

		/// <summary>(0020,1204) VR=IS VM=1 Number of Patient Related Instances</summary>
		public static DicomTag NumberOfPatientRelatedInstances = new DicomTag(0x0020, 0x1204);

		/// <summary>(0020,1206) VR=IS VM=1 Number of Study Related Series</summary>
		public static DicomTag NumberOfStudyRelatedSeries = new DicomTag(0x0020, 0x1206);

		/// <summary>(0020,1208) VR=IS VM=1 Number of Study Related Instances</summary>
		public static DicomTag NumberOfStudyRelatedInstances = new DicomTag(0x0020, 0x1208);

		/// <summary>(0020,1209) VR=IS VM=1 Number of Series Related Instances</summary>
		public static DicomTag NumberOfSeriesRelatedInstances = new DicomTag(0x0020, 0x1209);

		/// <summary>(0020,3100) VR=CS VM=1-n Source Image IDs (Retired)</summary>
		public static DicomTag SourceImageIDsRETIRED = new DicomTag(0x0020, 0x3100);

		/// <summary>(0020,3401) VR=CS VM=1 Modifying Device ID (Retired)</summary>
		public static DicomTag ModifyingDeviceIDRETIRED = new DicomTag(0x0020, 0x3401);

		/// <summary>(0020,3402) VR=CS VM=1 Modified Image ID (Retired)</summary>
		public static DicomTag ModifiedImageIDRETIRED = new DicomTag(0x0020, 0x3402);

		/// <summary>(0020,3403) VR=DA VM=1 Modified Image Date (Retired)</summary>
		public static DicomTag ModifiedImageDateRETIRED = new DicomTag(0x0020, 0x3403);

		/// <summary>(0020,3404) VR=LO VM=1 Modifying Device Manufacturer (Retired)</summary>
		public static DicomTag ModifyingDeviceManufacturerRETIRED = new DicomTag(0x0020, 0x3404);

		/// <summary>(0020,3405) VR=TM VM=1 Modified Image Time (Retired)</summary>
		public static DicomTag ModifiedImageTimeRETIRED = new DicomTag(0x0020, 0x3405);

		/// <summary>(0020,3406) VR=LO VM=1 Modified Image Description (Retired)</summary>
		public static DicomTag ModifiedImageDescriptionRETIRED = new DicomTag(0x0020, 0x3406);

		/// <summary>(0020,4000) VR=LT VM=1 Image Comments</summary>
		public static DicomTag ImageComments = new DicomTag(0x0020, 0x4000);

		/// <summary>(0020,5000) VR=AT VM=1-n Original Image Identification (Retired)</summary>
		public static DicomTag OriginalImageIdentificationRETIRED = new DicomTag(0x0020, 0x5000);

		/// <summary>(0020,5002) VR=CS VM=1-n Original Image Identification Nomenclature (Retired)</summary>
		public static DicomTag OriginalImageIdentificationNomenclatureRETIRED = new DicomTag(0x0020, 0x5002);

		/// <summary>(0020,9056) VR=SH VM=1 Stack ID</summary>
		public static DicomTag StackID = new DicomTag(0x0020, 0x9056);

		/// <summary>(0020,9057) VR=UL VM=1 In-Stack Position Number</summary>
		public static DicomTag InStackPositionNumber = new DicomTag(0x0020, 0x9057);

		/// <summary>(0020,9071) VR=SQ VM=1 Frame Anatomy Sequence</summary>
		public static DicomTag FrameAnatomySequence = new DicomTag(0x0020, 0x9071);

		/// <summary>(0020,9072) VR=CS VM=1 Frame Laterality</summary>
		public static DicomTag FrameLaterality = new DicomTag(0x0020, 0x9072);

		/// <summary>(0020,9111) VR=SQ VM=1 Frame Content Sequence</summary>
		public static DicomTag FrameContentSequence = new DicomTag(0x0020, 0x9111);

		/// <summary>(0020,9113) VR=SQ VM=1 Plane Position Sequence</summary>
		public static DicomTag PlanePositionSequence = new DicomTag(0x0020, 0x9113);

		/// <summary>(0020,9116) VR=SQ VM=1 Plane Orientation Sequence</summary>
		public static DicomTag PlaneOrientationSequence = new DicomTag(0x0020, 0x9116);

		/// <summary>(0020,9128) VR=UL VM=1 Temporal Position Index</summary>
		public static DicomTag TemporalPositionIndex = new DicomTag(0x0020, 0x9128);

		/// <summary>(0020,9153) VR=FD VM=1 Nominal Cardiac Trigger Delay Time</summary>
		public static DicomTag NominalCardiacTriggerDelayTime = new DicomTag(0x0020, 0x9153);

		/// <summary>(0020,9156) VR=US VM=1 Frame Acquisition Number</summary>
		public static DicomTag FrameAcquisitionNumber = new DicomTag(0x0020, 0x9156);

		/// <summary>(0020,9157) VR=UL VM=1-n Dimension Index Values</summary>
		public static DicomTag DimensionIndexValues = new DicomTag(0x0020, 0x9157);

		/// <summary>(0020,9158) VR=LT VM=1 Frame Comments</summary>
		public static DicomTag FrameComments = new DicomTag(0x0020, 0x9158);

		/// <summary>(0020,9161) VR=UI VM=1 Concatenation UID</summary>
		public static DicomTag ConcatenationUID = new DicomTag(0x0020, 0x9161);

		/// <summary>(0020,9162) VR=US VM=1 In-concatenation Number</summary>
		public static DicomTag InconcatenationNumber = new DicomTag(0x0020, 0x9162);

		/// <summary>(0020,9163) VR=US VM=1 In-concatenation Total Number</summary>
		public static DicomTag InconcatenationTotalNumber = new DicomTag(0x0020, 0x9163);

		/// <summary>(0020,9164) VR=UI VM=1 Dimension Organization UID</summary>
		public static DicomTag DimensionOrganizationUID = new DicomTag(0x0020, 0x9164);

		/// <summary>(0020,9165) VR=AT VM=1 Dimension Index Pointer</summary>
		public static DicomTag DimensionIndexPointer = new DicomTag(0x0020, 0x9165);

		/// <summary>(0020,9167) VR=AT VM=1 Functional Group Pointer</summary>
		public static DicomTag FunctionalGroupPointer = new DicomTag(0x0020, 0x9167);

		/// <summary>(0020,9213) VR=LO VM=1 Dimension Index Private Creator</summary>
		public static DicomTag DimensionIndexPrivateCreator = new DicomTag(0x0020, 0x9213);

		/// <summary>(0020,9221) VR=SQ VM=1 Dimension Organization Sequence</summary>
		public static DicomTag DimensionOrganizationSequence = new DicomTag(0x0020, 0x9221);

		/// <summary>(0020,9222) VR=SQ VM=1 Dimension Index Sequence</summary>
		public static DicomTag DimensionIndexSequence = new DicomTag(0x0020, 0x9222);

		/// <summary>(0020,9228) VR=UL VM=1 Concatenation Frame Offset Number</summary>
		public static DicomTag ConcatenationFrameOffsetNumber = new DicomTag(0x0020, 0x9228);

		/// <summary>(0020,9238) VR=LO VM=1 Functional Group Private Creator</summary>
		public static DicomTag FunctionalGroupPrivateCreator = new DicomTag(0x0020, 0x9238);

		/// <summary>(0020,9241) VR=FL VM=1 Nominal Percentage of Cardiac Phase</summary>
		public static DicomTag NominalPercentageOfCardiacPhase = new DicomTag(0x0020, 0x9241);

		/// <summary>(0020,9245) VR=FL VM=1 Nominal Percentage of Respiratory Phase</summary>
		public static DicomTag NominalPercentageOfRespiratoryPhase = new DicomTag(0x0020, 0x9245);

		/// <summary>(0020,9246) VR=FL VM=1 Starting Respiratory Amplitude</summary>
		public static DicomTag StartingRespiratoryAmplitude = new DicomTag(0x0020, 0x9246);

		/// <summary>(0020,9247) VR=CS VM=1 Starting Respiratory Phase</summary>
		public static DicomTag StartingRespiratoryPhase = new DicomTag(0x0020, 0x9247);

		/// <summary>(0020,9248) VR=FL VM=1 Ending Respiratory Amplitude</summary>
		public static DicomTag EndingRespiratoryAmplitude = new DicomTag(0x0020, 0x9248);

		/// <summary>(0020,9249) VR=CS VM=1 Ending Respiratory Phase</summary>
		public static DicomTag EndingRespiratoryPhase = new DicomTag(0x0020, 0x9249);

		/// <summary>(0020,9250) VR=CS VM=1 Respiratory Trigger Type</summary>
		public static DicomTag RespiratoryTriggerType = new DicomTag(0x0020, 0x9250);

		/// <summary>(0020,9251) VR=FD VM=1 R - R Interval Time Nominal</summary>
		public static DicomTag RRIntervalTimeNominal = new DicomTag(0x0020, 0x9251);

		/// <summary>(0020,9252) VR=FD VM=1 Actual Cardiac Trigger Delay Time</summary>
		public static DicomTag ActualCardiacTriggerDelayTime = new DicomTag(0x0020, 0x9252);

		/// <summary>(0020,9253) VR=SQ VM=1 Respiratory Synchronization Sequence</summary>
		public static DicomTag RespiratorySynchronizationSequence = new DicomTag(0x0020, 0x9253);

		/// <summary>(0020,9254) VR=FD VM=1 Respiratory Interval Time</summary>
		public static DicomTag RespiratoryIntervalTime = new DicomTag(0x0020, 0x9254);

		/// <summary>(0020,9255) VR=FD VM=1 Nominal Respiratory Trigger Delay Time</summary>
		public static DicomTag NominalRespiratoryTriggerDelayTime = new DicomTag(0x0020, 0x9255);

		/// <summary>(0020,9256) VR=FD VM=1 Respiratory Trigger Delay Threshold</summary>
		public static DicomTag RespiratoryTriggerDelayThreshold = new DicomTag(0x0020, 0x9256);

		/// <summary>(0020,9257) VR=FD VM=1 Actual Respiratory Trigger Delay Time</summary>
		public static DicomTag ActualRespiratoryTriggerDelayTime = new DicomTag(0x0020, 0x9257);

		/// <summary>(0020,9421) VR=LO VM=1 Dimension Description Label</summary>
		public static DicomTag DimensionDescriptionLabel = new DicomTag(0x0020, 0x9421);

		/// <summary>(0020,9450) VR=SQ VM=1 Patient Orientation in Frame Sequence</summary>
		public static DicomTag PatientOrientationInFrameSequence = new DicomTag(0x0020, 0x9450);

		/// <summary>(0020,9453) VR=LO VM=1 Frame Label</summary>
		public static DicomTag FrameLabel = new DicomTag(0x0020, 0x9453);

		/// <summary>(0020,9518) VR=US VM=1-n Acquisition Index</summary>
		public static DicomTag AcquisitionIndex = new DicomTag(0x0020, 0x9518);

		/// <summary>(0020,9529) VR=SQ VM=1 Contributing SOP Instances Reference Sequence</summary>
		public static DicomTag ContributingSOPInstancesReferenceSequence = new DicomTag(0x0020, 0x9529);

		/// <summary>(0020,9536) VR=US VM=1 Reconstruction Index</summary>
		public static DicomTag ReconstructionIndex = new DicomTag(0x0020, 0x9536);

		/// <summary>(0022,0001) VR=US VM=1 Light Path Filter Pass-Through Wavelength</summary>
		public static DicomTag LightPathFilterPassThroughWavelength = new DicomTag(0x0022, 0x0001);

		/// <summary>(0022,0002) VR=US VM=2 Light Path Filter Pass Band</summary>
		public static DicomTag LightPathFilterPassBand = new DicomTag(0x0022, 0x0002);

		/// <summary>(0022,0003) VR=US VM=1 Image Path Filter Pass-Through Wavelength</summary>
		public static DicomTag ImagePathFilterPassThroughWavelength = new DicomTag(0x0022, 0x0003);

		/// <summary>(0022,0004) VR=US VM=2 Image Path Filter Pass Band</summary>
		public static DicomTag ImagePathFilterPassBand = new DicomTag(0x0022, 0x0004);

		/// <summary>(0022,0005) VR=CS VM=1 Patient Eye Movement Commanded</summary>
		public static DicomTag PatientEyeMovementCommanded = new DicomTag(0x0022, 0x0005);

		/// <summary>(0022,0006) VR=SQ VM=1 Patient Eye Movement Command Code Sequence</summary>
		public static DicomTag PatientEyeMovementCommandCodeSequence = new DicomTag(0x0022, 0x0006);

		/// <summary>(0022,0007) VR=FL VM=1 Spherical Lens Power</summary>
		public static DicomTag SphericalLensPower = new DicomTag(0x0022, 0x0007);

		/// <summary>(0022,0008) VR=FL VM=1 Cylinder Lens Power</summary>
		public static DicomTag CylinderLensPower = new DicomTag(0x0022, 0x0008);

		/// <summary>(0022,0009) VR=FL VM=1 Cylinder Axis</summary>
		public static DicomTag CylinderAxis = new DicomTag(0x0022, 0x0009);

		/// <summary>(0022,000a) VR=FL VM=1 Emmetropic Magnification</summary>
		public static DicomTag EmmetropicMagnification = new DicomTag(0x0022, 0x000a);

		/// <summary>(0022,000b) VR=FL VM=1 Intra Ocular Pressure</summary>
		public static DicomTag IntraOcularPressure = new DicomTag(0x0022, 0x000b);

		/// <summary>(0022,000c) VR=FL VM=1 Horizontal Field of View</summary>
		public static DicomTag HorizontalFieldOfView = new DicomTag(0x0022, 0x000c);

		/// <summary>(0022,000d) VR=CS VM=1 Pupil Dilated</summary>
		public static DicomTag PupilDilated = new DicomTag(0x0022, 0x000d);

		/// <summary>(0022,000e) VR=FL VM=1 Degree of Dilation</summary>
		public static DicomTag DegreeOfDilation = new DicomTag(0x0022, 0x000e);

		/// <summary>(0022,0010) VR=FL VM=1 Stereo Baseline Angle</summary>
		public static DicomTag StereoBaselineAngle = new DicomTag(0x0022, 0x0010);

		/// <summary>(0022,0011) VR=FL VM=1 Stereo Baseline Displacement</summary>
		public static DicomTag StereoBaselineDisplacement = new DicomTag(0x0022, 0x0011);

		/// <summary>(0022,0012) VR=FL VM=1 Stereo Horizontal Pixel Offset</summary>
		public static DicomTag StereoHorizontalPixelOffset = new DicomTag(0x0022, 0x0012);

		/// <summary>(0022,0013) VR=FL VM=1 Stereo Vertical Pixel Offset</summary>
		public static DicomTag StereoVerticalPixelOffset = new DicomTag(0x0022, 0x0013);

		/// <summary>(0022,0014) VR=FL VM=1 Stereo Rotation</summary>
		public static DicomTag StereoRotation = new DicomTag(0x0022, 0x0014);

		/// <summary>(0022,0015) VR=SQ VM=1 Acquisition Device Type Code Sequence</summary>
		public static DicomTag AcquisitionDeviceTypeCodeSequence = new DicomTag(0x0022, 0x0015);

		/// <summary>(0022,0016) VR=SQ VM=1 Illumination Type Code Sequence</summary>
		public static DicomTag IlluminationTypeCodeSequence = new DicomTag(0x0022, 0x0016);

		/// <summary>(0022,0017) VR=SQ VM=1 Light Path Filter Type Stack Code Sequence</summary>
		public static DicomTag LightPathFilterTypeStackCodeSequence = new DicomTag(0x0022, 0x0017);

		/// <summary>(0022,0018) VR=SQ VM=1 Image Path Filter Type Stack Code Sequence</summary>
		public static DicomTag ImagePathFilterTypeStackCodeSequence = new DicomTag(0x0022, 0x0018);

		/// <summary>(0022,0019) VR=SQ VM=1 Lenses Code Sequence</summary>
		public static DicomTag LensesCodeSequence = new DicomTag(0x0022, 0x0019);

		/// <summary>(0022,001a) VR=SQ VM=1 Channel Description Code Sequence</summary>
		public static DicomTag ChannelDescriptionCodeSequence = new DicomTag(0x0022, 0x001a);

		/// <summary>(0022,001b) VR=SQ VM=1 Refractive State Sequence</summary>
		public static DicomTag RefractiveStateSequence = new DicomTag(0x0022, 0x001b);

		/// <summary>(0022,001c) VR=SQ VM=1 Mydriatic Agent Code Sequence</summary>
		public static DicomTag MydriaticAgentCodeSequence = new DicomTag(0x0022, 0x001c);

		/// <summary>(0022,001d) VR=SQ VM=1 Relative Image Position Code Sequence</summary>
		public static DicomTag RelativeImagePositionCodeSequence = new DicomTag(0x0022, 0x001d);

		/// <summary>(0022,0020) VR=SQ VM=1 Stereo Pairs Sequence</summary>
		public static DicomTag StereoPairsSequence = new DicomTag(0x0022, 0x0020);

		/// <summary>(0022,0021) VR=SQ VM=1 Left Image Sequence</summary>
		public static DicomTag LeftImageSequence = new DicomTag(0x0022, 0x0021);

		/// <summary>(0022,0022) VR=SQ VM=1 Right Image Sequence</summary>
		public static DicomTag RightImageSequence = new DicomTag(0x0022, 0x0022);

		/// <summary>(0022,0030) VR=FL VM=1 Axial Length of the Eye</summary>
		public static DicomTag AxialLengthOfTheEye = new DicomTag(0x0022, 0x0030);

		/// <summary>(0022,0031) VR=SQ VM=1 Ophthalmic Frame Location Sequence</summary>
		public static DicomTag OphthalmicFrameLocationSequence = new DicomTag(0x0022, 0x0031);

		/// <summary>(0022,0032) VR=FL VM=2-2n Reference Coordinates</summary>
		public static DicomTag ReferenceCoordinates = new DicomTag(0x0022, 0x0032);

		/// <summary>(0022,0035) VR=FL VM=1 Depth Spatial Resolution</summary>
		public static DicomTag DepthSpatialResolution = new DicomTag(0x0022, 0x0035);

		/// <summary>(0022,0036) VR=FL VM=1 Maximum Depth Distortion</summary>
		public static DicomTag MaximumDepthDistortion = new DicomTag(0x0022, 0x0036);

		/// <summary>(0022,0037) VR=FL VM=1 Along-scan Spatial Resolution</summary>
		public static DicomTag AlongscanSpatialResolution = new DicomTag(0x0022, 0x0037);

		/// <summary>(0022,0038) VR=FL VM=1 Maximum Along-scan Distortion</summary>
		public static DicomTag MaximumAlongscanDistortion = new DicomTag(0x0022, 0x0038);

		/// <summary>(0022,0039) VR=CS VM=1 Ophthalmic Image Orientation</summary>
		public static DicomTag OphthalmicImageOrientation = new DicomTag(0x0022, 0x0039);

		/// <summary>(0022,0041) VR=FL VM=1 Depth of Transverse Image</summary>
		public static DicomTag DepthOfTransverseImage = new DicomTag(0x0022, 0x0041);

		/// <summary>(0022,0042) VR=SQ VM=1 Mydriatic Agent Concentration Units Sequence</summary>
		public static DicomTag MydriaticAgentConcentrationUnitsSequence = new DicomTag(0x0022, 0x0042);

		/// <summary>(0022,0048) VR=FL VM=1 Across-scan Spatial Resolution</summary>
		public static DicomTag AcrossscanSpatialResolution = new DicomTag(0x0022, 0x0048);

		/// <summary>(0022,0049) VR=FL VM=1 Maximum Across-scan Distortion</summary>
		public static DicomTag MaximumAcrossscanDistortion = new DicomTag(0x0022, 0x0049);

		/// <summary>(0022,004e) VR=DS VM=1 Mydriatic Agent Concentration</summary>
		public static DicomTag MydriaticAgentConcentration = new DicomTag(0x0022, 0x004e);

		/// <summary>(0022,0055) VR=FL VM=1 Illumination Wave Length</summary>
		public static DicomTag IlluminationWaveLength = new DicomTag(0x0022, 0x0055);

		/// <summary>(0022,0056) VR=FL VM=1 Illumination Power</summary>
		public static DicomTag IlluminationPower = new DicomTag(0x0022, 0x0056);

		/// <summary>(0022,0057) VR=FL VM=1 Illumination Bandwidth</summary>
		public static DicomTag IlluminationBandwidth = new DicomTag(0x0022, 0x0057);

		/// <summary>(0022,0058) VR=SQ VM=1 Mydriatic Agent Sequence</summary>
		public static DicomTag MydriaticAgentSequence = new DicomTag(0x0022, 0x0058);

		/// <summary>(0028,0002) VR=US VM=1 Samples per Pixel</summary>
		public static DicomTag SamplesPerPixel = new DicomTag(0x0028, 0x0002);

		/// <summary>(0028,0003) VR=US VM=1 Samples per Pixel Used</summary>
		public static DicomTag SamplesPerPixelUsed = new DicomTag(0x0028, 0x0003);

		/// <summary>(0028,0004) VR=CS VM=1 Photometric Interpretation</summary>
		public static DicomTag PhotometricInterpretation = new DicomTag(0x0028, 0x0004);

		/// <summary>(0028,0005) VR=US VM=1 Image Dimensions (Retired)</summary>
		public static DicomTag ImageDimensionsRETIRED = new DicomTag(0x0028, 0x0005);

		/// <summary>(0028,0006) VR=US VM=1 Planar Configuration</summary>
		public static DicomTag PlanarConfiguration = new DicomTag(0x0028, 0x0006);

		/// <summary>(0028,0008) VR=IS VM=1 Number of Frames</summary>
		public static DicomTag NumberOfFrames = new DicomTag(0x0028, 0x0008);

		/// <summary>(0028,0009) VR=AT VM=1-n Frame Increment Pointer</summary>
		public static DicomTag FrameIncrementPointer = new DicomTag(0x0028, 0x0009);

		/// <summary>(0028,000a) VR=AT VM=1-n Frame Dimension Pointer</summary>
		public static DicomTag FrameDimensionPointer = new DicomTag(0x0028, 0x000a);

		/// <summary>(0028,0010) VR=US VM=1 Rows</summary>
		public static DicomTag Rows = new DicomTag(0x0028, 0x0010);

		/// <summary>(0028,0011) VR=US VM=1 Columns</summary>
		public static DicomTag Columns = new DicomTag(0x0028, 0x0011);

		/// <summary>(0028,0012) VR=US VM=1 Planes (Retired)</summary>
		public static DicomTag PlanesRETIRED = new DicomTag(0x0028, 0x0012);

		/// <summary>(0028,0014) VR=US VM=1 Ultrasound Color Data Present</summary>
		public static DicomTag UltrasoundColorDataPresent = new DicomTag(0x0028, 0x0014);

		/// <summary>(0028,0030) VR=DS VM=2 Pixel Spacing</summary>
		public static DicomTag PixelSpacing = new DicomTag(0x0028, 0x0030);

		/// <summary>(0028,0031) VR=DS VM=2 Zoom Factor</summary>
		public static DicomTag ZoomFactor = new DicomTag(0x0028, 0x0031);

		/// <summary>(0028,0032) VR=DS VM=2 Zoom Center</summary>
		public static DicomTag ZoomCenter = new DicomTag(0x0028, 0x0032);

		/// <summary>(0028,0034) VR=IS VM=2 Pixel Aspect Ratio</summary>
		public static DicomTag PixelAspectRatio = new DicomTag(0x0028, 0x0034);

		/// <summary>(0028,0040) VR=CS VM=1 Image Format (Retired)</summary>
		public static DicomTag ImageFormatRETIRED = new DicomTag(0x0028, 0x0040);

		/// <summary>(0028,0050) VR=LO VM=1-n Manipulated Image (Retired)</summary>
		public static DicomTag ManipulatedImageRETIRED = new DicomTag(0x0028, 0x0050);

		/// <summary>(0028,0051) VR=CS VM=1-n Corrected Image</summary>
		public static DicomTag CorrectedImage = new DicomTag(0x0028, 0x0051);

		/// <summary>(0028,005f) VR=LO VM=1 Compression Recognition Code (Retired)</summary>
		public static DicomTag CompressionRecognitionCodeRETIRED = new DicomTag(0x0028, 0x005f);

		/// <summary>(0028,0060) VR=CS VM=1 Compression Code (Retired)</summary>
		public static DicomTag CompressionCodeRETIRED = new DicomTag(0x0028, 0x0060);

		/// <summary>(0028,0061) VR=SH VM=1 Compression Originator (Retired)</summary>
		public static DicomTag CompressionOriginatorRETIRED = new DicomTag(0x0028, 0x0061);

		/// <summary>(0028,0062) VR=LO VM=1 Compression Label (Retired)</summary>
		public static DicomTag CompressionLabelRETIRED = new DicomTag(0x0028, 0x0062);

		/// <summary>(0028,0063) VR=SH VM=1 Compression Description (Retired)</summary>
		public static DicomTag CompressionDescriptionRETIRED = new DicomTag(0x0028, 0x0063);

		/// <summary>(0028,0065) VR=CS VM=1-n Compression Sequence (Retired)</summary>
		public static DicomTag CompressionSequenceRETIRED = new DicomTag(0x0028, 0x0065);

		/// <summary>(0028,0066) VR=AT VM=1-n Compression Step Pointers (Retired)</summary>
		public static DicomTag CompressionStepPointersRETIRED = new DicomTag(0x0028, 0x0066);

		/// <summary>(0028,0068) VR=US VM=1 Repeat Interval (Retired)</summary>
		public static DicomTag RepeatIntervalRETIRED = new DicomTag(0x0028, 0x0068);

		/// <summary>(0028,0069) VR=US VM=1 Bits Grouped (Retired)</summary>
		public static DicomTag BitsGroupedRETIRED = new DicomTag(0x0028, 0x0069);

		/// <summary>(0028,0070) VR=US VM=1-n Perimeter Table (Retired)</summary>
		public static DicomTag PerimeterTableRETIRED = new DicomTag(0x0028, 0x0070);

		/// <summary>(0028,0071) VR=US/SS VM=1 Perimeter Value (Retired)</summary>
		public static DicomTag PerimeterValueRETIRED = new DicomTag(0x0028, 0x0071);

		/// <summary>(0028,0080) VR=US VM=1 Predictor Rows (Retired)</summary>
		public static DicomTag PredictorRowsRETIRED = new DicomTag(0x0028, 0x0080);

		/// <summary>(0028,0081) VR=US VM=1 Predictor Columns (Retired)</summary>
		public static DicomTag PredictorColumnsRETIRED = new DicomTag(0x0028, 0x0081);

		/// <summary>(0028,0082) VR=US VM=1-n Predictor Constants (Retired)</summary>
		public static DicomTag PredictorConstantsRETIRED = new DicomTag(0x0028, 0x0082);

		/// <summary>(0028,0090) VR=CS VM=1 Blocked Pixels (Retired)</summary>
		public static DicomTag BlockedPixelsRETIRED = new DicomTag(0x0028, 0x0090);

		/// <summary>(0028,0091) VR=US VM=1 Block Rows (Retired)</summary>
		public static DicomTag BlockRowsRETIRED = new DicomTag(0x0028, 0x0091);

		/// <summary>(0028,0092) VR=US VM=1 Block Columns (Retired)</summary>
		public static DicomTag BlockColumnsRETIRED = new DicomTag(0x0028, 0x0092);

		/// <summary>(0028,0093) VR=US VM=1 Row Overlap (Retired)</summary>
		public static DicomTag RowOverlapRETIRED = new DicomTag(0x0028, 0x0093);

		/// <summary>(0028,0094) VR=US VM=1 Column Overlap (Retired)</summary>
		public static DicomTag ColumnOverlapRETIRED = new DicomTag(0x0028, 0x0094);

		/// <summary>(0028,0100) VR=US VM=1 Bits Allocated</summary>
		public static DicomTag BitsAllocated = new DicomTag(0x0028, 0x0100);

		/// <summary>(0028,0101) VR=US VM=1 Bits Stored</summary>
		public static DicomTag BitsStored = new DicomTag(0x0028, 0x0101);

		/// <summary>(0028,0102) VR=US VM=1 High Bit</summary>
		public static DicomTag HighBit = new DicomTag(0x0028, 0x0102);

		/// <summary>(0028,0103) VR=US VM=1 Pixel Representation</summary>
		public static DicomTag PixelRepresentation = new DicomTag(0x0028, 0x0103);

		/// <summary>(0028,0104) VR=US/SS VM=1 Smallest Valid Pixel Value (Retired)</summary>
		public static DicomTag SmallestValidPixelValueRETIRED = new DicomTag(0x0028, 0x0104);

		/// <summary>(0028,0105) VR=US/SS VM=1 Largest Valid Pixel Value (Retired)</summary>
		public static DicomTag LargestValidPixelValueRETIRED = new DicomTag(0x0028, 0x0105);

		/// <summary>(0028,0106) VR=US/SS VM=1 Smallest Image Pixel Value</summary>
		public static DicomTag SmallestImagePixelValue = new DicomTag(0x0028, 0x0106);

		/// <summary>(0028,0107) VR=US/SS VM=1 Largest Image Pixel Value</summary>
		public static DicomTag LargestImagePixelValue = new DicomTag(0x0028, 0x0107);

		/// <summary>(0028,0108) VR=US/SS VM=1 Smallest Pixel Value in Series</summary>
		public static DicomTag SmallestPixelValueInSeries = new DicomTag(0x0028, 0x0108);

		/// <summary>(0028,0109) VR=US/SS VM=1 Largest Pixel Value in Series</summary>
		public static DicomTag LargestPixelValueInSeries = new DicomTag(0x0028, 0x0109);

		/// <summary>(0028,0110) VR=US/SS VM=1 Smallest Image Pixel Value in Plane (Retired)</summary>
		public static DicomTag SmallestImagePixelValueInPlaneRETIRED = new DicomTag(0x0028, 0x0110);

		/// <summary>(0028,0111) VR=US/SS VM=1 Largest Image Pixel Value in Plane (Retired)</summary>
		public static DicomTag LargestImagePixelValueInPlaneRETIRED = new DicomTag(0x0028, 0x0111);

		/// <summary>(0028,0120) VR=US/SS VM=1 Pixel Padding Value</summary>
		public static DicomTag PixelPaddingValue = new DicomTag(0x0028, 0x0120);

		/// <summary>(0028,0121) VR=US/SS VM=1 Pixel Padding Range Limit</summary>
		public static DicomTag PixelPaddingRangeLimit = new DicomTag(0x0028, 0x0121);

		/// <summary>(0028,0200) VR=US VM=1 Image Location (Retired)</summary>
		public static DicomTag ImageLocationRETIRED = new DicomTag(0x0028, 0x0200);

		/// <summary>(0028,0300) VR=CS VM=1 Quality Control Image</summary>
		public static DicomTag QualityControlImage = new DicomTag(0x0028, 0x0300);

		/// <summary>(0028,0301) VR=CS VM=1 Burned In Annotation</summary>
		public static DicomTag BurnedInAnnotation = new DicomTag(0x0028, 0x0301);

		/// <summary>(0028,0400) VR=LO VM=1 Transform Label (Retired)</summary>
		public static DicomTag TransformLabelRETIRED = new DicomTag(0x0028, 0x0400);

		/// <summary>(0028,0401) VR=LO VM=1 Transform Version Number (Retired)</summary>
		public static DicomTag TransformVersionNumberRETIRED = new DicomTag(0x0028, 0x0401);

		/// <summary>(0028,0402) VR=US VM=1 Number of Transform Steps (Retired)</summary>
		public static DicomTag NumberOfTransformStepsRETIRED = new DicomTag(0x0028, 0x0402);

		/// <summary>(0028,0403) VR=LO VM=1-n Sequence of Compressed Data (Retired)</summary>
		public static DicomTag SequenceOfCompressedDataRETIRED = new DicomTag(0x0028, 0x0403);

		/// <summary>(0028,0404) VR=AT VM=1-n Details of Coefficients (Retired)</summary>
		public static DicomTag DetailsOfCoefficientsRETIRED = new DicomTag(0x0028, 0x0404);

		/// <summary>(0028,0400) VR=US VM=1 Rows For Nth Order Coefficients (Retired)</summary>
		public static DicomTag RowsForNthOrderCoefficientsRETIRED = new DicomTag(0x0028, 0x0400);

		/// <summary>(0028,0401) VR=US VM=1 Columns For Nth Order Coefficients (Retired)</summary>
		public static DicomTag ColumnsForNthOrderCoefficientsRETIRED = new DicomTag(0x0028, 0x0401);

		/// <summary>(0028,0402) VR=LO VM=1-n Coefficient Coding (Retired)</summary>
		public static DicomTag CoefficientCodingRETIRED = new DicomTag(0x0028, 0x0402);

		/// <summary>(0028,0403) VR=AT VM=1-n Coefficient Coding Pointers (Retired)</summary>
		public static DicomTag CoefficientCodingPointersRETIRED = new DicomTag(0x0028, 0x0403);

		/// <summary>(0028,0700) VR=LO VM=1 DCT Label (Retired)</summary>
		public static DicomTag DCTLabelRETIRED = new DicomTag(0x0028, 0x0700);

		/// <summary>(0028,0701) VR=CS VM=1-n Data Block Description (Retired)</summary>
		public static DicomTag DataBlockDescriptionRETIRED = new DicomTag(0x0028, 0x0701);

		/// <summary>(0028,0702) VR=AT VM=1-n Data Block (Retired)</summary>
		public static DicomTag DataBlockRETIRED = new DicomTag(0x0028, 0x0702);

		/// <summary>(0028,0710) VR=US VM=1 Normalization Factor Format (Retired)</summary>
		public static DicomTag NormalizationFactorFormatRETIRED = new DicomTag(0x0028, 0x0710);

		/// <summary>(0028,0720) VR=US VM=1 Zonal Map Number Format (Retired)</summary>
		public static DicomTag ZonalMapNumberFormatRETIRED = new DicomTag(0x0028, 0x0720);

		/// <summary>(0028,0721) VR=AT VM=1-n Zonal Map Location (Retired)</summary>
		public static DicomTag ZonalMapLocationRETIRED = new DicomTag(0x0028, 0x0721);

		/// <summary>(0028,0722) VR=US VM=1 Zonal Map Format (Retired)</summary>
		public static DicomTag ZonalMapFormatRETIRED = new DicomTag(0x0028, 0x0722);

		/// <summary>(0028,0730) VR=US VM=1 Adaptive Map Format (Retired)</summary>
		public static DicomTag AdaptiveMapFormatRETIRED = new DicomTag(0x0028, 0x0730);

		/// <summary>(0028,0740) VR=US VM=1 Code Number Format (Retired)</summary>
		public static DicomTag CodeNumberFormatRETIRED = new DicomTag(0x0028, 0x0740);

		/// <summary>(0028,0800) VR=CS VM=1-n Code Label (Retired)</summary>
		public static DicomTag CodeLabelRETIRED = new DicomTag(0x0028, 0x0800);

		/// <summary>(0028,0802) VR=US VM=1 Number of Table (Retired)</summary>
		public static DicomTag NumberOfTableRETIRED = new DicomTag(0x0028, 0x0802);

		/// <summary>(0028,0803) VR=AT VM=1-n Code Table Location (Retired)</summary>
		public static DicomTag CodeTableLocationRETIRED = new DicomTag(0x0028, 0x0803);

		/// <summary>(0028,0804) VR=US VM=1 Bits For Code Word (Retired)</summary>
		public static DicomTag BitsForCodeWordRETIRED = new DicomTag(0x0028, 0x0804);

		/// <summary>(0028,0808) VR=AT VM=1-n Image Data Location (Retired)</summary>
		public static DicomTag ImageDataLocationRETIRED = new DicomTag(0x0028, 0x0808);

		/// <summary>(0028,0a02) VR=CS VM=1 Pixel Spacing Calibration Type</summary>
		public static DicomTag PixelSpacingCalibrationType = new DicomTag(0x0028, 0x0a02);

		/// <summary>(0028,0a04) VR=LO VM=1 Pixel Spacing Calibration Description</summary>
		public static DicomTag PixelSpacingCalibrationDescription = new DicomTag(0x0028, 0x0a04);

		/// <summary>(0028,1040) VR=CS VM=1 Pixel Intensity Relationship</summary>
		public static DicomTag PixelIntensityRelationship = new DicomTag(0x0028, 0x1040);

		/// <summary>(0028,1041) VR=SS VM=1 Pixel Intensity Relationship Sign</summary>
		public static DicomTag PixelIntensityRelationshipSign = new DicomTag(0x0028, 0x1041);

		/// <summary>(0028,1050) VR=DS VM=1-n Window Center</summary>
		public static DicomTag WindowCenter = new DicomTag(0x0028, 0x1050);

		/// <summary>(0028,1051) VR=DS VM=1-n Window Width</summary>
		public static DicomTag WindowWidth = new DicomTag(0x0028, 0x1051);

		/// <summary>(0028,1052) VR=DS VM=1 Rescale Intercept</summary>
		public static DicomTag RescaleIntercept = new DicomTag(0x0028, 0x1052);

		/// <summary>(0028,1053) VR=DS VM=1 Rescale Slope</summary>
		public static DicomTag RescaleSlope = new DicomTag(0x0028, 0x1053);

		/// <summary>(0028,1054) VR=LO VM=1 Rescale Type</summary>
		public static DicomTag RescaleType = new DicomTag(0x0028, 0x1054);

		/// <summary>(0028,1055) VR=LO VM=1-n Window Center &amp; Width Explanation</summary>
		public static DicomTag WindowCenterWidthExplanation = new DicomTag(0x0028, 0x1055);

		/// <summary>(0028,1056) VR=CS VM=1 VOI LUT Function</summary>
		public static DicomTag VOILUTFunction = new DicomTag(0x0028, 0x1056);

		/// <summary>(0028,1080) VR=CS VM=1 Gray Scale (Retired)</summary>
		public static DicomTag GrayScaleRETIRED = new DicomTag(0x0028, 0x1080);

		/// <summary>(0028,1090) VR=CS VM=1 Recommended Viewing Mode</summary>
		public static DicomTag RecommendedViewingMode = new DicomTag(0x0028, 0x1090);

		/// <summary>(0028,1100) VR=US/SS VM=3 Gray Lookup Table Descriptor (Retired)</summary>
		public static DicomTag GrayLookupTableDescriptorRETIRED = new DicomTag(0x0028, 0x1100);

		/// <summary>(0028,1101) VR=US/SS VM=3 Red Palette Color Lookup Table Descriptor</summary>
		public static DicomTag RedPaletteColorLookupTableDescriptor = new DicomTag(0x0028, 0x1101);

		/// <summary>(0028,1102) VR=US/SS VM=3 Green Palette Color Lookup Table Descriptor</summary>
		public static DicomTag GreenPaletteColorLookupTableDescriptor = new DicomTag(0x0028, 0x1102);

		/// <summary>(0028,1103) VR=US/SS VM=3 Blue Palette Color Lookup Table Descriptor</summary>
		public static DicomTag BluePaletteColorLookupTableDescriptor = new DicomTag(0x0028, 0x1103);

		/// <summary>(0028,1111) VR=US/SS VM=4 Large Red Palette Color Lookup Table Descriptor (Retired)</summary>
		public static DicomTag LargeRedPaletteColorLookupTableDescriptorRETIRED = new DicomTag(0x0028, 0x1111);

		/// <summary>(0028,1112) VR=US/SS VM=4 Large Green Palette Color Lookup Table Descriptor (Retired)</summary>
		public static DicomTag LargeGreenPaletteColorLookupTableDescriptorRETIRED = new DicomTag(0x0028, 0x1112);

		/// <summary>(0028,1113) VR=US/SS VM=4 Large Blue Palette Color Lookup Table Descriptor (Retired)</summary>
		public static DicomTag LargeBluePaletteColorLookupTableDescriptorRETIRED = new DicomTag(0x0028, 0x1113);

		/// <summary>(0028,1199) VR=UI VM=1 Palette Color Lookup Table UID</summary>
		public static DicomTag PaletteColorLookupTableUID = new DicomTag(0x0028, 0x1199);

		/// <summary>(0028,1200) VR=US/SS/OW VM=1-n Gray Lookup Table Data (Retired)</summary>
		public static DicomTag GrayLookupTableDataRETIRED = new DicomTag(0x0028, 0x1200);

		/// <summary>(0028,1201) VR=OW VM=1 Red Palette Color Lookup Table Data</summary>
		public static DicomTag RedPaletteColorLookupTableData = new DicomTag(0x0028, 0x1201);

		/// <summary>(0028,1202) VR=OW VM=1 Green Palette Color Lookup Table Data</summary>
		public static DicomTag GreenPaletteColorLookupTableData = new DicomTag(0x0028, 0x1202);

		/// <summary>(0028,1203) VR=OW VM=1 Blue Palette Color Lookup Table Data</summary>
		public static DicomTag BluePaletteColorLookupTableData = new DicomTag(0x0028, 0x1203);

		/// <summary>(0028,1211) VR=OW VM=1 Large Red Palette Color Lookup Table Data (Retired)</summary>
		public static DicomTag LargeRedPaletteColorLookupTableDataRETIRED = new DicomTag(0x0028, 0x1211);

		/// <summary>(0028,1212) VR=OW VM=1 Large Green Palette Color Lookup Table Data (Retired)</summary>
		public static DicomTag LargeGreenPaletteColorLookupTableDataRETIRED = new DicomTag(0x0028, 0x1212);

		/// <summary>(0028,1213) VR=OW VM=1 Large Blue Palette Color Lookup Table Data (Retired)</summary>
		public static DicomTag LargeBluePaletteColorLookupTableDataRETIRED = new DicomTag(0x0028, 0x1213);

		/// <summary>(0028,1214) VR=UI VM=1 Large Palette Color Lookup Table UID (Retired)</summary>
		public static DicomTag LargePaletteColorLookupTableUIDRETIRED = new DicomTag(0x0028, 0x1214);

		/// <summary>(0028,1221) VR=OW VM=1 Segmented Red Palette Color Lookup Table Data</summary>
		public static DicomTag SegmentedRedPaletteColorLookupTableData = new DicomTag(0x0028, 0x1221);

		/// <summary>(0028,1222) VR=OW VM=1 Segmented Green Palette Color Lookup Table Data</summary>
		public static DicomTag SegmentedGreenPaletteColorLookupTableData = new DicomTag(0x0028, 0x1222);

		/// <summary>(0028,1223) VR=OW VM=1 Segmented Blue Palette Color Lookup Table Data</summary>
		public static DicomTag SegmentedBluePaletteColorLookupTableData = new DicomTag(0x0028, 0x1223);

		/// <summary>(0028,1300) VR=CS VM=1 Implant Present</summary>
		public static DicomTag ImplantPresent = new DicomTag(0x0028, 0x1300);

		/// <summary>(0028,1350) VR=CS VM=1 Partial View</summary>
		public static DicomTag PartialView = new DicomTag(0x0028, 0x1350);

		/// <summary>(0028,1351) VR=ST VM=1 Partial View Description</summary>
		public static DicomTag PartialViewDescription = new DicomTag(0x0028, 0x1351);

		/// <summary>(0028,1352) VR=SQ VM=1 Partial View Code Sequence</summary>
		public static DicomTag PartialViewCodeSequence = new DicomTag(0x0028, 0x1352);

		/// <summary>(0028,135a) VR=CS VM=1 Spatial Locations Preserved</summary>
		public static DicomTag SpatialLocationsPreserved = new DicomTag(0x0028, 0x135a);

		/// <summary>(0028,2000) VR=OB VM=1 ICC Profile</summary>
		public static DicomTag ICCProfile = new DicomTag(0x0028, 0x2000);

		/// <summary>(0028,2110) VR=CS VM=1 Lossy Image Compression</summary>
		public static DicomTag LossyImageCompression = new DicomTag(0x0028, 0x2110);

		/// <summary>(0028,2112) VR=DS VM=1-n Lossy Image Compression Ratio</summary>
		public static DicomTag LossyImageCompressionRatio = new DicomTag(0x0028, 0x2112);

		/// <summary>(0028,2114) VR=CS VM=1-n Lossy Image Compression Method</summary>
		public static DicomTag LossyImageCompressionMethod = new DicomTag(0x0028, 0x2114);

		/// <summary>(0028,3000) VR=SQ VM=1 Modality LUT Sequence</summary>
		public static DicomTag ModalityLUTSequence = new DicomTag(0x0028, 0x3000);

		/// <summary>(0028,3002) VR=US/SS VM=3 LUT Descriptor</summary>
		public static DicomTag LUTDescriptor = new DicomTag(0x0028, 0x3002);

		/// <summary>(0028,3003) VR=LO VM=1 LUT Explanation</summary>
		public static DicomTag LUTExplanation = new DicomTag(0x0028, 0x3003);

		/// <summary>(0028,3004) VR=LO VM=1 Modality LUT Type</summary>
		public static DicomTag ModalityLUTType = new DicomTag(0x0028, 0x3004);

		/// <summary>(0028,3006) VR=US/SS/OW VM=1-n LUT Data</summary>
		public static DicomTag LUTData = new DicomTag(0x0028, 0x3006);

		/// <summary>(0028,3010) VR=SQ VM=1 VOI LUT Sequence</summary>
		public static DicomTag VOILUTSequence = new DicomTag(0x0028, 0x3010);

		/// <summary>(0028,3110) VR=SQ VM=1 Softcopy VOI LUT Sequence</summary>
		public static DicomTag SoftcopyVOILUTSequence = new DicomTag(0x0028, 0x3110);

		/// <summary>(0028,4000) VR=LT VM=1 Image Presentation Comments (Retired)</summary>
		public static DicomTag ImagePresentationCommentsRETIRED = new DicomTag(0x0028, 0x4000);

		/// <summary>(0028,5000) VR=SQ VM=1 Bi-Plane Acquisition Sequence (Retired)</summary>
		public static DicomTag BiPlaneAcquisitionSequenceRETIRED = new DicomTag(0x0028, 0x5000);

		/// <summary>(0028,6010) VR=US VM=1 Representative Frame Number</summary>
		public static DicomTag RepresentativeFrameNumber = new DicomTag(0x0028, 0x6010);

		/// <summary>(0028,6020) VR=US VM=1-n Frame Numbers of Interest (FOI)</summary>
		public static DicomTag FrameNumbersOfInterestFOI = new DicomTag(0x0028, 0x6020);

		/// <summary>(0028,6022) VR=LO VM=1-n Frame(s) of Interest Description</summary>
		public static DicomTag FramesOfInterestDescription = new DicomTag(0x0028, 0x6022);

		/// <summary>(0028,6023) VR=CS VM=1-n Frame of Interest Type</summary>
		public static DicomTag FrameOfInterestType = new DicomTag(0x0028, 0x6023);

		/// <summary>(0028,6030) VR=US VM=1-n Mask Pointer(s) (Retired)</summary>
		public static DicomTag MaskPointersRETIRED = new DicomTag(0x0028, 0x6030);

		/// <summary>(0028,6040) VR=US VM=1-n R Wave Pointer</summary>
		public static DicomTag RWavePointer = new DicomTag(0x0028, 0x6040);

		/// <summary>(0028,6100) VR=SQ VM=1 Mask Subtraction Sequence</summary>
		public static DicomTag MaskSubtractionSequence = new DicomTag(0x0028, 0x6100);

		/// <summary>(0028,6101) VR=CS VM=1 Mask Operation</summary>
		public static DicomTag MaskOperation = new DicomTag(0x0028, 0x6101);

		/// <summary>(0028,6102) VR=US VM=2-2n Applicable Frame Range</summary>
		public static DicomTag ApplicableFrameRange = new DicomTag(0x0028, 0x6102);

		/// <summary>(0028,6110) VR=US VM=1-n Mask Frame Numbers</summary>
		public static DicomTag MaskFrameNumbers = new DicomTag(0x0028, 0x6110);

		/// <summary>(0028,6112) VR=US VM=1 Contrast Frame Averaging</summary>
		public static DicomTag ContrastFrameAveraging = new DicomTag(0x0028, 0x6112);

		/// <summary>(0028,6114) VR=FL VM=2 Mask Sub-pixel Shift</summary>
		public static DicomTag MaskSubpixelShift = new DicomTag(0x0028, 0x6114);

		/// <summary>(0028,6120) VR=SS VM=1 TID Offset</summary>
		public static DicomTag TIDOffset = new DicomTag(0x0028, 0x6120);

		/// <summary>(0028,6190) VR=ST VM=1 Mask Operation Explanation</summary>
		public static DicomTag MaskOperationExplanation = new DicomTag(0x0028, 0x6190);

		/// <summary>(0028,7fe0) VR=UT VM=1 Pixel Data Provider URL</summary>
		public static DicomTag PixelDataProviderURL = new DicomTag(0x0028, 0x7fe0);

		/// <summary>(0028,9001) VR=UL VM=1 Data Point Rows</summary>
		public static DicomTag DataPointRows = new DicomTag(0x0028, 0x9001);

		/// <summary>(0028,9002) VR=UL VM=1 Data Point Columns</summary>
		public static DicomTag DataPointColumns = new DicomTag(0x0028, 0x9002);

		/// <summary>(0028,9003) VR=CS VM=1 Signal Domain Columns</summary>
		public static DicomTag SignalDomainColumns = new DicomTag(0x0028, 0x9003);

		/// <summary>(0028,9099) VR=US VM=1 Largest Monochrome Pixel Value (Retired)</summary>
		public static DicomTag LargestMonochromePixelValueRETIRED = new DicomTag(0x0028, 0x9099);

		/// <summary>(0028,9108) VR=CS VM=1 Data Representation</summary>
		public static DicomTag DataRepresentation = new DicomTag(0x0028, 0x9108);

		/// <summary>(0028,9110) VR=SQ VM=1 Pixel Measures Sequence</summary>
		public static DicomTag PixelMeasuresSequence = new DicomTag(0x0028, 0x9110);

		/// <summary>(0028,9132) VR=SQ VM=1 Frame VOI LUT Sequence</summary>
		public static DicomTag FrameVOILUTSequence = new DicomTag(0x0028, 0x9132);

		/// <summary>(0028,9145) VR=SQ VM=1 Pixel Value Transformation Sequence</summary>
		public static DicomTag PixelValueTransformationSequence = new DicomTag(0x0028, 0x9145);

		/// <summary>(0028,9235) VR=CS VM=1 Signal Domain Rows</summary>
		public static DicomTag SignalDomainRows = new DicomTag(0x0028, 0x9235);

		/// <summary>(0028,9411) VR=FL VM=1 Display Filter Percentage</summary>
		public static DicomTag DisplayFilterPercentage = new DicomTag(0x0028, 0x9411);

		/// <summary>(0028,9415) VR=SQ VM=1 Frame Pixel Shift Sequence</summary>
		public static DicomTag FramePixelShiftSequence = new DicomTag(0x0028, 0x9415);

		/// <summary>(0028,9416) VR=US VM=1 Subtraction Item ID</summary>
		public static DicomTag SubtractionItemID = new DicomTag(0x0028, 0x9416);

		/// <summary>(0028,9422) VR=SQ VM=1 Pixel Intensity Relationship LUT Sequence</summary>
		public static DicomTag PixelIntensityRelationshipLUTSequence = new DicomTag(0x0028, 0x9422);

		/// <summary>(0028,9443) VR=SQ VM=1 Frame Pixel Data Properties Sequence</summary>
		public static DicomTag FramePixelDataPropertiesSequence = new DicomTag(0x0028, 0x9443);

		/// <summary>(0028,9444) VR=CS VM=1 Geometrical Properties</summary>
		public static DicomTag GeometricalProperties = new DicomTag(0x0028, 0x9444);

		/// <summary>(0028,9445) VR=FL VM=1 Geometric Maximum Distortion</summary>
		public static DicomTag GeometricMaximumDistortion = new DicomTag(0x0028, 0x9445);

		/// <summary>(0028,9446) VR=CS VM=1-n Image Processing Applied</summary>
		public static DicomTag ImageProcessingApplied = new DicomTag(0x0028, 0x9446);

		/// <summary>(0028,9454) VR=CS VM=1 Mask Selection Mode</summary>
		public static DicomTag MaskSelectionMode = new DicomTag(0x0028, 0x9454);

		/// <summary>(0028,9474) VR=CS VM=1 LUT Function</summary>
		public static DicomTag LUTFunction = new DicomTag(0x0028, 0x9474);

		/// <summary>(0028,9520) VR=DS VM=16 Image to Equipment Mapping Matrix</summary>
		public static DicomTag ImageToEquipmentMappingMatrix = new DicomTag(0x0028, 0x9520);

		/// <summary>(0028,9537) VR=CS VM=1 Equipment Coordinate System Identification</summary>
		public static DicomTag EquipmentCoordinateSystemIdentification = new DicomTag(0x0028, 0x9537);

		/// <summary>(0032,000a) VR=CS VM=1 Study Status ID (Retired)</summary>
		public static DicomTag StudyStatusIDRETIRED = new DicomTag(0x0032, 0x000a);

		/// <summary>(0032,000c) VR=CS VM=1 Study Priority ID (Retired)</summary>
		public static DicomTag StudyPriorityIDRETIRED = new DicomTag(0x0032, 0x000c);

		/// <summary>(0032,0012) VR=LO VM=1 Study ID Issuer (Retired)</summary>
		public static DicomTag StudyIDIssuerRETIRED = new DicomTag(0x0032, 0x0012);

		/// <summary>(0032,0032) VR=DA VM=1 Study Verified Date (Retired)</summary>
		public static DicomTag StudyVerifiedDateRETIRED = new DicomTag(0x0032, 0x0032);

		/// <summary>(0032,0033) VR=TM VM=1 Study Verified Time (Retired)</summary>
		public static DicomTag StudyVerifiedTimeRETIRED = new DicomTag(0x0032, 0x0033);

		/// <summary>(0032,0034) VR=DA VM=1 Study Read Date (Retired)</summary>
		public static DicomTag StudyReadDateRETIRED = new DicomTag(0x0032, 0x0034);

		/// <summary>(0032,0035) VR=TM VM=1 Study Read Time (Retired)</summary>
		public static DicomTag StudyReadTimeRETIRED = new DicomTag(0x0032, 0x0035);

		/// <summary>(0032,1000) VR=DA VM=1 Scheduled Study Start Date (Retired)</summary>
		public static DicomTag ScheduledStudyStartDateRETIRED = new DicomTag(0x0032, 0x1000);

		/// <summary>(0032,1001) VR=TM VM=1 Scheduled Study Start Time (Retired)</summary>
		public static DicomTag ScheduledStudyStartTimeRETIRED = new DicomTag(0x0032, 0x1001);

		/// <summary>(0032,1010) VR=DA VM=1 Scheduled Study Stop Date (Retired)</summary>
		public static DicomTag ScheduledStudyStopDateRETIRED = new DicomTag(0x0032, 0x1010);

		/// <summary>(0032,1011) VR=TM VM=1 Scheduled Study Stop Time (Retired)</summary>
		public static DicomTag ScheduledStudyStopTimeRETIRED = new DicomTag(0x0032, 0x1011);

		/// <summary>(0032,1020) VR=LO VM=1 Scheduled Study Location (Retired)</summary>
		public static DicomTag ScheduledStudyLocationRETIRED = new DicomTag(0x0032, 0x1020);

		/// <summary>(0032,1021) VR=AE VM=1-n Scheduled Study Location AE Title (Retired)</summary>
		public static DicomTag ScheduledStudyLocationAETitleRETIRED = new DicomTag(0x0032, 0x1021);

		/// <summary>(0032,1030) VR=LO VM=1 Reason for Study (Retired)</summary>
		public static DicomTag ReasonForStudyRETIRED = new DicomTag(0x0032, 0x1030);

		/// <summary>(0032,1031) VR=SQ VM=1 Requesting Physician Identification Sequence</summary>
		public static DicomTag RequestingPhysicianIdentificationSequence = new DicomTag(0x0032, 0x1031);

		/// <summary>(0032,1032) VR=PN VM=1 Requesting Physician</summary>
		public static DicomTag RequestingPhysician = new DicomTag(0x0032, 0x1032);

		/// <summary>(0032,1033) VR=LO VM=1 Requesting Service</summary>
		public static DicomTag RequestingService = new DicomTag(0x0032, 0x1033);

		/// <summary>(0032,1040) VR=DA VM=1 Study Arrival Date (Retired)</summary>
		public static DicomTag StudyArrivalDateRETIRED = new DicomTag(0x0032, 0x1040);

		/// <summary>(0032,1041) VR=TM VM=1 Study Arrival Time (Retired)</summary>
		public static DicomTag StudyArrivalTimeRETIRED = new DicomTag(0x0032, 0x1041);

		/// <summary>(0032,1050) VR=DA VM=1 Study Completion Date (Retired)</summary>
		public static DicomTag StudyCompletionDateRETIRED = new DicomTag(0x0032, 0x1050);

		/// <summary>(0032,1051) VR=TM VM=1 Study Completion Time (Retired)</summary>
		public static DicomTag StudyCompletionTimeRETIRED = new DicomTag(0x0032, 0x1051);

		/// <summary>(0032,1055) VR=CS VM=1 Study Component Status ID (Retired)</summary>
		public static DicomTag StudyComponentStatusIDRETIRED = new DicomTag(0x0032, 0x1055);

		/// <summary>(0032,1060) VR=LO VM=1 Requested Procedure Description</summary>
		public static DicomTag RequestedProcedureDescription = new DicomTag(0x0032, 0x1060);

		/// <summary>(0032,1064) VR=SQ VM=1 Requested Procedure Code Sequence</summary>
		public static DicomTag RequestedProcedureCodeSequence = new DicomTag(0x0032, 0x1064);

		/// <summary>(0032,1070) VR=LO VM=1 Requested Contrast Agent</summary>
		public static DicomTag RequestedContrastAgent = new DicomTag(0x0032, 0x1070);

		/// <summary>(0032,4000) VR=LT VM=1 Study Comments (Retired)</summary>
		public static DicomTag StudyCommentsRETIRED = new DicomTag(0x0032, 0x4000);

		/// <summary>(0038,0004) VR=SQ VM=1 Referenced Patient Alias Sequence</summary>
		public static DicomTag ReferencedPatientAliasSequence = new DicomTag(0x0038, 0x0004);

		/// <summary>(0038,0008) VR=CS VM=1 Visit Status ID</summary>
		public static DicomTag VisitStatusID = new DicomTag(0x0038, 0x0008);

		/// <summary>(0038,0010) VR=LO VM=1 Admission ID</summary>
		public static DicomTag AdmissionID = new DicomTag(0x0038, 0x0010);

		/// <summary>(0038,0011) VR=LO VM=1 Issuer of Admission ID</summary>
		public static DicomTag IssuerOfAdmissionID = new DicomTag(0x0038, 0x0011);

		/// <summary>(0038,0016) VR=LO VM=1 Route of Admissions</summary>
		public static DicomTag RouteOfAdmissions = new DicomTag(0x0038, 0x0016);

		/// <summary>(0038,001a) VR=DA VM=1 Scheduled Admission Date (Retired)</summary>
		public static DicomTag ScheduledAdmissionDateRETIRED = new DicomTag(0x0038, 0x001a);

		/// <summary>(0038,001b) VR=TM VM=1 Scheduled Admission Time (Retired)</summary>
		public static DicomTag ScheduledAdmissionTimeRETIRED = new DicomTag(0x0038, 0x001b);

		/// <summary>(0038,001c) VR=DA VM=1 Scheduled Discharge Date (Retired)</summary>
		public static DicomTag ScheduledDischargeDateRETIRED = new DicomTag(0x0038, 0x001c);

		/// <summary>(0038,001d) VR=TM VM=1 Scheduled Discharge Time (Retired)</summary>
		public static DicomTag ScheduledDischargeTimeRETIRED = new DicomTag(0x0038, 0x001d);

		/// <summary>(0038,001e) VR=LO VM=1 Scheduled Patient Institution Residence (Retired)</summary>
		public static DicomTag ScheduledPatientInstitutionResidenceRETIRED = new DicomTag(0x0038, 0x001e);

		/// <summary>(0038,0020) VR=DA VM=1 Admitting Date</summary>
		public static DicomTag AdmittingDate = new DicomTag(0x0038, 0x0020);

		/// <summary>(0038,0021) VR=TM VM=1 Admitting Time</summary>
		public static DicomTag AdmittingTime = new DicomTag(0x0038, 0x0021);

		/// <summary>(0038,0030) VR=DA VM=1 Discharge Date (Retired)</summary>
		public static DicomTag DischargeDateRETIRED = new DicomTag(0x0038, 0x0030);

		/// <summary>(0038,0032) VR=TM VM=1 Discharge Time (Retired)</summary>
		public static DicomTag DischargeTimeRETIRED = new DicomTag(0x0038, 0x0032);

		/// <summary>(0038,0040) VR=LO VM=1 Discharge Diagnosis Description (Retired)</summary>
		public static DicomTag DischargeDiagnosisDescriptionRETIRED = new DicomTag(0x0038, 0x0040);

		/// <summary>(0038,0044) VR=SQ VM=1 Discharge Diagnosis Code Sequence (Retired)</summary>
		public static DicomTag DischargeDiagnosisCodeSequenceRETIRED = new DicomTag(0x0038, 0x0044);

		/// <summary>(0038,0050) VR=LO VM=1 Special Needs</summary>
		public static DicomTag SpecialNeeds = new DicomTag(0x0038, 0x0050);

		/// <summary>(0038,0060) VR=LO VM=1 Service Episode ID</summary>
		public static DicomTag ServiceEpisodeID = new DicomTag(0x0038, 0x0060);

		/// <summary>(0038,0061) VR=LO VM=1 Issuer of Service Episode ID</summary>
		public static DicomTag IssuerOfServiceEpisodeID = new DicomTag(0x0038, 0x0061);

		/// <summary>(0038,0062) VR=LO VM=1 Service Episode Description</summary>
		public static DicomTag ServiceEpisodeDescription = new DicomTag(0x0038, 0x0062);

		/// <summary>(0038,0100) VR=SQ VM=1 Pertinent Documents Sequence</summary>
		public static DicomTag PertinentDocumentsSequence = new DicomTag(0x0038, 0x0100);

		/// <summary>(0038,0300) VR=LO VM=1 Current Patient Location</summary>
		public static DicomTag CurrentPatientLocation = new DicomTag(0x0038, 0x0300);

		/// <summary>(0038,0400) VR=LO VM=1 Patient's Institution Residence</summary>
		public static DicomTag PatientsInstitutionResidence = new DicomTag(0x0038, 0x0400);

		/// <summary>(0038,0500) VR=LO VM=1 Patient State</summary>
		public static DicomTag PatientState = new DicomTag(0x0038, 0x0500);

		/// <summary>(0038,0502) VR=SQ VM=1 Patient Clinical Trial Participation Sequence</summary>
		public static DicomTag PatientClinicalTrialParticipationSequence = new DicomTag(0x0038, 0x0502);

		/// <summary>(0038,4000) VR=LT VM=1 Visit Comments</summary>
		public static DicomTag VisitComments = new DicomTag(0x0038, 0x4000);

		/// <summary>(003a,0004) VR=CS VM=1 Waveform Originality</summary>
		public static DicomTag WaveformOriginality = new DicomTag(0x003a, 0x0004);

		/// <summary>(003a,0005) VR=US VM=1 Number of Waveform Channels</summary>
		public static DicomTag NumberOfWaveformChannels = new DicomTag(0x003a, 0x0005);

		/// <summary>(003a,0010) VR=UL VM=1 Number of Waveform Samples</summary>
		public static DicomTag NumberOfWaveformSamples = new DicomTag(0x003a, 0x0010);

		/// <summary>(003a,001a) VR=DS VM=1 Sampling Frequency</summary>
		public static DicomTag SamplingFrequency = new DicomTag(0x003a, 0x001a);

		/// <summary>(003a,0020) VR=SH VM=1 Multiplex Group Label</summary>
		public static DicomTag MultiplexGroupLabel = new DicomTag(0x003a, 0x0020);

		/// <summary>(003a,0200) VR=SQ VM=1 Channel Definition Sequence</summary>
		public static DicomTag ChannelDefinitionSequence = new DicomTag(0x003a, 0x0200);

		/// <summary>(003a,0202) VR=IS VM=1 Waveform Channel Number</summary>
		public static DicomTag WaveformChannelNumber = new DicomTag(0x003a, 0x0202);

		/// <summary>(003a,0203) VR=SH VM=1 Channel Label</summary>
		public static DicomTag ChannelLabel = new DicomTag(0x003a, 0x0203);

		/// <summary>(003a,0205) VR=CS VM=1-n Channel Status</summary>
		public static DicomTag ChannelStatus = new DicomTag(0x003a, 0x0205);

		/// <summary>(003a,0208) VR=SQ VM=1 Channel Source Sequence</summary>
		public static DicomTag ChannelSourceSequence = new DicomTag(0x003a, 0x0208);

		/// <summary>(003a,0209) VR=SQ VM=1 Channel Source Modifiers Sequence</summary>
		public static DicomTag ChannelSourceModifiersSequence = new DicomTag(0x003a, 0x0209);

		/// <summary>(003a,020a) VR=SQ VM=1 Source Waveform Sequence</summary>
		public static DicomTag SourceWaveformSequence = new DicomTag(0x003a, 0x020a);

		/// <summary>(003a,020c) VR=LO VM=1 Channel Derivation Description</summary>
		public static DicomTag ChannelDerivationDescription = new DicomTag(0x003a, 0x020c);

		/// <summary>(003a,0210) VR=DS VM=1 Channel Sensitivity</summary>
		public static DicomTag ChannelSensitivity = new DicomTag(0x003a, 0x0210);

		/// <summary>(003a,0211) VR=SQ VM=1 Channel Sensitivity Units Sequence</summary>
		public static DicomTag ChannelSensitivityUnitsSequence = new DicomTag(0x003a, 0x0211);

		/// <summary>(003a,0212) VR=DS VM=1 Channel Sensitivity Correction Factor</summary>
		public static DicomTag ChannelSensitivityCorrectionFactor = new DicomTag(0x003a, 0x0212);

		/// <summary>(003a,0213) VR=DS VM=1 Channel Baseline</summary>
		public static DicomTag ChannelBaseline = new DicomTag(0x003a, 0x0213);

		/// <summary>(003a,0214) VR=DS VM=1 Channel Time Skew</summary>
		public static DicomTag ChannelTimeSkew = new DicomTag(0x003a, 0x0214);

		/// <summary>(003a,0215) VR=DS VM=1 Channel Sample Skew</summary>
		public static DicomTag ChannelSampleSkew = new DicomTag(0x003a, 0x0215);

		/// <summary>(003a,0218) VR=DS VM=1 Channel Offset</summary>
		public static DicomTag ChannelOffset = new DicomTag(0x003a, 0x0218);

		/// <summary>(003a,021a) VR=US VM=1 Waveform Bits Stored</summary>
		public static DicomTag WaveformBitsStored = new DicomTag(0x003a, 0x021a);

		/// <summary>(003a,0220) VR=DS VM=1 Filter Low Frequency</summary>
		public static DicomTag FilterLowFrequency = new DicomTag(0x003a, 0x0220);

		/// <summary>(003a,0221) VR=DS VM=1 Filter High Frequency</summary>
		public static DicomTag FilterHighFrequency = new DicomTag(0x003a, 0x0221);

		/// <summary>(003a,0222) VR=DS VM=1 Notch Filter Frequency</summary>
		public static DicomTag NotchFilterFrequency = new DicomTag(0x003a, 0x0222);

		/// <summary>(003a,0223) VR=DS VM=1 Notch Filter Bandwidth</summary>
		public static DicomTag NotchFilterBandwidth = new DicomTag(0x003a, 0x0223);

		/// <summary>(003a,0230) VR=FL VM=1 Waveform Data Display Scale</summary>
		public static DicomTag WaveformDataDisplayScale = new DicomTag(0x003a, 0x0230);

		/// <summary>(003a,0231) VR=US VM=3 Waveform Display Background CIELab Value</summary>
		public static DicomTag WaveformDisplayBackgroundCIELabValue = new DicomTag(0x003a, 0x0231);

		/// <summary>(003a,0240) VR=SQ VM=1 Waveform Presentation Group Sequence</summary>
		public static DicomTag WaveformPresentationGroupSequence = new DicomTag(0x003a, 0x0240);

		/// <summary>(003a,0241) VR=US VM=1 Presentation Group Number</summary>
		public static DicomTag PresentationGroupNumber = new DicomTag(0x003a, 0x0241);

		/// <summary>(003a,0242) VR=SQ VM=1 Channel Display Sequence</summary>
		public static DicomTag ChannelDisplaySequence = new DicomTag(0x003a, 0x0242);

		/// <summary>(003a,0244) VR=US VM=3 Channel Recommended Display CIELab Value</summary>
		public static DicomTag ChannelRecommendedDisplayCIELabValue = new DicomTag(0x003a, 0x0244);

		/// <summary>(003a,0245) VR=FL VM=1 Channel Position</summary>
		public static DicomTag ChannelPosition = new DicomTag(0x003a, 0x0245);

		/// <summary>(003a,0246) VR=CS VM=1 Display Shading Flag</summary>
		public static DicomTag DisplayShadingFlag = new DicomTag(0x003a, 0x0246);

		/// <summary>(003a,0247) VR=FL VM=1 Fractional Channel Display Scale</summary>
		public static DicomTag FractionalChannelDisplayScale = new DicomTag(0x003a, 0x0247);

		/// <summary>(003a,0248) VR=FL VM=1 Absolute Channel Display Scale</summary>
		public static DicomTag AbsoluteChannelDisplayScale = new DicomTag(0x003a, 0x0248);

		/// <summary>(003a,0300) VR=SQ VM=1 Multiplexed Audio Channels Description Code Sequence</summary>
		public static DicomTag MultiplexedAudioChannelsDescriptionCodeSequence = new DicomTag(0x003a, 0x0300);

		/// <summary>(003a,0301) VR=IS VM=1 Channel Identification Code</summary>
		public static DicomTag ChannelIdentificationCode = new DicomTag(0x003a, 0x0301);

		/// <summary>(003a,0302) VR=CS VM=1 Channel Mode</summary>
		public static DicomTag ChannelMode = new DicomTag(0x003a, 0x0302);

		/// <summary>(0040,0001) VR=AE VM=1-n Scheduled Station AE Title</summary>
		public static DicomTag ScheduledStationAETitle = new DicomTag(0x0040, 0x0001);

		/// <summary>(0040,0002) VR=DA VM=1 Scheduled Procedure Step Start Date</summary>
		public static DicomTag ScheduledProcedureStepStartDate = new DicomTag(0x0040, 0x0002);

		/// <summary>(0040,0003) VR=TM VM=1 Scheduled Procedure Step Start Time</summary>
		public static DicomTag ScheduledProcedureStepStartTime = new DicomTag(0x0040, 0x0003);

		/// <summary>(0040,0004) VR=DA VM=1 Scheduled Procedure Step End Date</summary>
		public static DicomTag ScheduledProcedureStepEndDate = new DicomTag(0x0040, 0x0004);

		/// <summary>(0040,0005) VR=TM VM=1 Scheduled Procedure Step End Time</summary>
		public static DicomTag ScheduledProcedureStepEndTime = new DicomTag(0x0040, 0x0005);

		/// <summary>(0040,0006) VR=PN VM=1 Scheduled Performing Physician's Name</summary>
		public static DicomTag ScheduledPerformingPhysiciansName = new DicomTag(0x0040, 0x0006);

		/// <summary>(0040,0007) VR=LO VM=1 Scheduled Procedure Step Description</summary>
		public static DicomTag ScheduledProcedureStepDescription = new DicomTag(0x0040, 0x0007);

		/// <summary>(0040,0008) VR=SQ VM=1 Scheduled Protocol Code Sequence</summary>
		public static DicomTag ScheduledProtocolCodeSequence = new DicomTag(0x0040, 0x0008);

		/// <summary>(0040,0009) VR=SH VM=1 Scheduled Procedure Step ID</summary>
		public static DicomTag ScheduledProcedureStepID = new DicomTag(0x0040, 0x0009);

		/// <summary>(0040,000a) VR=SQ VM=1 Stage Code Sequence</summary>
		public static DicomTag StageCodeSequence = new DicomTag(0x0040, 0x000a);

		/// <summary>(0040,000b) VR=SQ VM=1 Scheduled Performing Physician Identification Sequence</summary>
		public static DicomTag ScheduledPerformingPhysicianIdentificationSequence = new DicomTag(0x0040, 0x000b);

		/// <summary>(0040,0010) VR=SH VM=1-n Scheduled Station Name</summary>
		public static DicomTag ScheduledStationName = new DicomTag(0x0040, 0x0010);

		/// <summary>(0040,0011) VR=SH VM=1 Scheduled Procedure Step Location</summary>
		public static DicomTag ScheduledProcedureStepLocation = new DicomTag(0x0040, 0x0011);

		/// <summary>(0040,0012) VR=LO VM=1 Pre-Medication</summary>
		public static DicomTag PreMedication = new DicomTag(0x0040, 0x0012);

		/// <summary>(0040,0020) VR=CS VM=1 Scheduled Procedure Step Status</summary>
		public static DicomTag ScheduledProcedureStepStatus = new DicomTag(0x0040, 0x0020);

		/// <summary>(0040,0100) VR=SQ VM=1 Scheduled Procedure Step Sequence</summary>
		public static DicomTag ScheduledProcedureStepSequence = new DicomTag(0x0040, 0x0100);

		/// <summary>(0040,0220) VR=SQ VM=1 Referenced Non-Image Composite SOP Instance Sequence</summary>
		public static DicomTag ReferencedNonImageCompositeSOPInstanceSequence = new DicomTag(0x0040, 0x0220);

		/// <summary>(0040,0241) VR=AE VM=1 Performed Station AE Title</summary>
		public static DicomTag PerformedStationAETitle = new DicomTag(0x0040, 0x0241);

		/// <summary>(0040,0242) VR=SH VM=1 Performed Station Name</summary>
		public static DicomTag PerformedStationName = new DicomTag(0x0040, 0x0242);

		/// <summary>(0040,0243) VR=SH VM=1 Performed Location</summary>
		public static DicomTag PerformedLocation = new DicomTag(0x0040, 0x0243);

		/// <summary>(0040,0244) VR=DA VM=1 Performed Procedure Step Start Date</summary>
		public static DicomTag PerformedProcedureStepStartDate = new DicomTag(0x0040, 0x0244);

		/// <summary>(0040,0245) VR=TM VM=1 Performed Procedure Step Start Time</summary>
		public static DicomTag PerformedProcedureStepStartTime = new DicomTag(0x0040, 0x0245);

		/// <summary>(0040,0250) VR=DA VM=1 Performed Procedure Step End Date</summary>
		public static DicomTag PerformedProcedureStepEndDate = new DicomTag(0x0040, 0x0250);

		/// <summary>(0040,0251) VR=TM VM=1 Performed Procedure Step End Time</summary>
		public static DicomTag PerformedProcedureStepEndTime = new DicomTag(0x0040, 0x0251);

		/// <summary>(0040,0252) VR=CS VM=1 Performed Procedure Step Status</summary>
		public static DicomTag PerformedProcedureStepStatus = new DicomTag(0x0040, 0x0252);

		/// <summary>(0040,0253) VR=SH VM=1 Performed Procedure Step ID</summary>
		public static DicomTag PerformedProcedureStepID = new DicomTag(0x0040, 0x0253);

		/// <summary>(0040,0254) VR=LO VM=1 Performed Procedure Step Description</summary>
		public static DicomTag PerformedProcedureStepDescription = new DicomTag(0x0040, 0x0254);

		/// <summary>(0040,0255) VR=LO VM=1 Performed Procedure Type Description</summary>
		public static DicomTag PerformedProcedureTypeDescription = new DicomTag(0x0040, 0x0255);

		/// <summary>(0040,0260) VR=SQ VM=1 Performed Protocol Code Sequence</summary>
		public static DicomTag PerformedProtocolCodeSequence = new DicomTag(0x0040, 0x0260);

		/// <summary>(0040,0270) VR=SQ VM=1 Scheduled Step Attributes Sequence</summary>
		public static DicomTag ScheduledStepAttributesSequence = new DicomTag(0x0040, 0x0270);

		/// <summary>(0040,0275) VR=SQ VM=1 Request Attributes Sequence</summary>
		public static DicomTag RequestAttributesSequence = new DicomTag(0x0040, 0x0275);

		/// <summary>(0040,0280) VR=ST VM=1 Comments on the Performed Procedure Step</summary>
		public static DicomTag CommentsOnThePerformedProcedureStep = new DicomTag(0x0040, 0x0280);

		/// <summary>(0040,0281) VR=SQ VM=1 Performed Procedure Step Discontinuation Reason Code Sequence</summary>
		public static DicomTag PerformedProcedureStepDiscontinuationReasonCodeSequence = new DicomTag(0x0040, 0x0281);

		/// <summary>(0040,0293) VR=SQ VM=1 Quantity Sequence</summary>
		public static DicomTag QuantitySequence = new DicomTag(0x0040, 0x0293);

		/// <summary>(0040,0294) VR=DS VM=1 Quantity</summary>
		public static DicomTag Quantity = new DicomTag(0x0040, 0x0294);

		/// <summary>(0040,0295) VR=SQ VM=1 Measuring Units Sequence</summary>
		public static DicomTag MeasuringUnitsSequence = new DicomTag(0x0040, 0x0295);

		/// <summary>(0040,0296) VR=SQ VM=1 Billing Item Sequence</summary>
		public static DicomTag BillingItemSequence = new DicomTag(0x0040, 0x0296);

		/// <summary>(0040,0300) VR=US VM=1 Total Time of Fluoroscopy</summary>
		public static DicomTag TotalTimeOfFluoroscopy = new DicomTag(0x0040, 0x0300);

		/// <summary>(0040,0301) VR=US VM=1 Total Number of Exposures</summary>
		public static DicomTag TotalNumberOfExposures = new DicomTag(0x0040, 0x0301);

		/// <summary>(0040,0302) VR=US VM=1 Entrance Dose</summary>
		public static DicomTag EntranceDose = new DicomTag(0x0040, 0x0302);

		/// <summary>(0040,0303) VR=US VM=1-2 Exposed Area</summary>
		public static DicomTag ExposedArea = new DicomTag(0x0040, 0x0303);

		/// <summary>(0040,0306) VR=DS VM=1 Distance Source to Entrance</summary>
		public static DicomTag DistanceSourceToEntrance = new DicomTag(0x0040, 0x0306);

		/// <summary>(0040,0307) VR=DS VM=1 Distance Source to Support (Retired)</summary>
		public static DicomTag DistanceSourceToSupportRETIRED = new DicomTag(0x0040, 0x0307);

		/// <summary>(0040,030e) VR=SQ VM=1 Exposure Dose Sequence</summary>
		public static DicomTag ExposureDoseSequence = new DicomTag(0x0040, 0x030e);

		/// <summary>(0040,0310) VR=ST VM=1 Comments on Radiation Dose</summary>
		public static DicomTag CommentsOnRadiationDose = new DicomTag(0x0040, 0x0310);

		/// <summary>(0040,0312) VR=DS VM=1 X-Ray Output</summary>
		public static DicomTag XRayOutput = new DicomTag(0x0040, 0x0312);

		/// <summary>(0040,0314) VR=DS VM=1 Half Value Layer</summary>
		public static DicomTag HalfValueLayer = new DicomTag(0x0040, 0x0314);

		/// <summary>(0040,0316) VR=DS VM=1 Organ Dose</summary>
		public static DicomTag OrganDose = new DicomTag(0x0040, 0x0316);

		/// <summary>(0040,0318) VR=CS VM=1 Organ Exposed</summary>
		public static DicomTag OrganExposed = new DicomTag(0x0040, 0x0318);

		/// <summary>(0040,0320) VR=SQ VM=1 Billing Procedure Step Sequence</summary>
		public static DicomTag BillingProcedureStepSequence = new DicomTag(0x0040, 0x0320);

		/// <summary>(0040,0321) VR=SQ VM=1 Film Consumption Sequence</summary>
		public static DicomTag FilmConsumptionSequence = new DicomTag(0x0040, 0x0321);

		/// <summary>(0040,0324) VR=SQ VM=1 Billing Supplies and Devices Sequence</summary>
		public static DicomTag BillingSuppliesAndDevicesSequence = new DicomTag(0x0040, 0x0324);

		/// <summary>(0040,0330) VR=SQ VM=1 Referenced Procedure Step Sequence (Retired)</summary>
		public static DicomTag ReferencedProcedureStepSequenceRETIRED = new DicomTag(0x0040, 0x0330);

		/// <summary>(0040,0340) VR=SQ VM=1 Performed Series Sequence</summary>
		public static DicomTag PerformedSeriesSequence = new DicomTag(0x0040, 0x0340);

		/// <summary>(0040,0400) VR=LT VM=1 Comments on the Scheduled Procedure Step</summary>
		public static DicomTag CommentsOnTheScheduledProcedureStep = new DicomTag(0x0040, 0x0400);

		/// <summary>(0040,0440) VR=SQ VM=1 Protocol Context Sequence</summary>
		public static DicomTag ProtocolContextSequence = new DicomTag(0x0040, 0x0440);

		/// <summary>(0040,0441) VR=SQ VM=1 Content Item Modifier Sequence</summary>
		public static DicomTag ContentItemModifierSequence = new DicomTag(0x0040, 0x0441);

		/// <summary>(0040,050a) VR=LO VM=1 Specimen Accession Number</summary>
		public static DicomTag SpecimenAccessionNumber = new DicomTag(0x0040, 0x050a);

		/// <summary>(0040,0550) VR=SQ VM=1 Specimen Sequence</summary>
		public static DicomTag SpecimenSequence = new DicomTag(0x0040, 0x0550);

		/// <summary>(0040,0551) VR=LO VM=1 Specimen Identifier</summary>
		public static DicomTag SpecimenIdentifier = new DicomTag(0x0040, 0x0551);

		/// <summary>(0040,0552) VR=SQ VM=1 Specimen Description Sequence - Trial (Retired)</summary>
		public static DicomTag SpecimenDescriptionSequenceTrialRETIRED = new DicomTag(0x0040, 0x0552);

		/// <summary>(0040,0553) VR=ST VM=1 Specimen Description - Trial (Retired)</summary>
		public static DicomTag SpecimenDescriptionTrialRETIRED = new DicomTag(0x0040, 0x0553);

		/// <summary>(0040,0555) VR=SQ VM=1 Acquisition Context Sequence</summary>
		public static DicomTag AcquisitionContextSequence = new DicomTag(0x0040, 0x0555);

		/// <summary>(0040,0556) VR=ST VM=1 Acquisition Context Description</summary>
		public static DicomTag AcquisitionContextDescription = new DicomTag(0x0040, 0x0556);

		/// <summary>(0040,059a) VR=SQ VM=1 Specimen Type Code Sequence</summary>
		public static DicomTag SpecimenTypeCodeSequence = new DicomTag(0x0040, 0x059a);

		/// <summary>(0040,06fa) VR=LO VM=1 Slide Identifier</summary>
		public static DicomTag SlideIdentifier = new DicomTag(0x0040, 0x06fa);

		/// <summary>(0040,071a) VR=SQ VM=1 Image Center Point Coordinates Sequence</summary>
		public static DicomTag ImageCenterPointCoordinatesSequence = new DicomTag(0x0040, 0x071a);

		/// <summary>(0040,072a) VR=DS VM=1 X offset in Slide Coordinate System</summary>
		public static DicomTag XOffsetInSlideCoordinateSystem = new DicomTag(0x0040, 0x072a);

		/// <summary>(0040,073a) VR=DS VM=1 Y offset in Slide Coordinate System</summary>
		public static DicomTag YOffsetInSlideCoordinateSystem = new DicomTag(0x0040, 0x073a);

		/// <summary>(0040,074a) VR=DS VM=1 Z offset in Slide Coordinate System</summary>
		public static DicomTag ZOffsetInSlideCoordinateSystem = new DicomTag(0x0040, 0x074a);

		/// <summary>(0040,08d8) VR=SQ VM=1 Pixel Spacing Sequence</summary>
		public static DicomTag PixelSpacingSequence = new DicomTag(0x0040, 0x08d8);

		/// <summary>(0040,08da) VR=SQ VM=1 Coordinate System Axis Code Sequence</summary>
		public static DicomTag CoordinateSystemAxisCodeSequence = new DicomTag(0x0040, 0x08da);

		/// <summary>(0040,08ea) VR=SQ VM=1 Measurement Units Code Sequence</summary>
		public static DicomTag MeasurementUnitsCodeSequence = new DicomTag(0x0040, 0x08ea);

		/// <summary>(0040,09f8) VR=SQ VM=1 Vital Stain Code Sequence - Trial (Retired)</summary>
		public static DicomTag VitalStainCodeSequenceTrialRETIRED = new DicomTag(0x0040, 0x09f8);

		/// <summary>(0040,1001) VR=SH VM=1 Requested Procedure ID</summary>
		public static DicomTag RequestedProcedureID = new DicomTag(0x0040, 0x1001);

		/// <summary>(0040,1002) VR=LO VM=1 Reason for the Requested Procedure</summary>
		public static DicomTag ReasonForTheRequestedProcedure = new DicomTag(0x0040, 0x1002);

		/// <summary>(0040,1003) VR=SH VM=1 Requested Procedure Priority</summary>
		public static DicomTag RequestedProcedurePriority = new DicomTag(0x0040, 0x1003);

		/// <summary>(0040,1004) VR=LO VM=1 Patient Transport Arrangements</summary>
		public static DicomTag PatientTransportArrangements = new DicomTag(0x0040, 0x1004);

		/// <summary>(0040,1005) VR=LO VM=1 Requested Procedure Location</summary>
		public static DicomTag RequestedProcedureLocation = new DicomTag(0x0040, 0x1005);

		/// <summary>(0040,1006) VR=SH VM=1 Placer Order Number / Procedure (Retired)</summary>
		public static DicomTag PlacerOrderNumberProcedureRETIRED = new DicomTag(0x0040, 0x1006);

		/// <summary>(0040,1007) VR=SH VM=1 Filler Order Number / Procedure (Retired)</summary>
		public static DicomTag FillerOrderNumberProcedureRETIRED = new DicomTag(0x0040, 0x1007);

		/// <summary>(0040,1008) VR=LO VM=1 Confidentiality Code</summary>
		public static DicomTag ConfidentialityCode = new DicomTag(0x0040, 0x1008);

		/// <summary>(0040,1009) VR=SH VM=1 Reporting Priority</summary>
		public static DicomTag ReportingPriority = new DicomTag(0x0040, 0x1009);

		/// <summary>(0040,100a) VR=SQ VM=1 Reason for Requested Procedure Code Sequence</summary>
		public static DicomTag ReasonForRequestedProcedureCodeSequence = new DicomTag(0x0040, 0x100a);

		/// <summary>(0040,1010) VR=PN VM=1-n Names of Intended Recipients of Results</summary>
		public static DicomTag NamesOfIntendedRecipientsOfResults = new DicomTag(0x0040, 0x1010);

		/// <summary>(0040,1011) VR=SQ VM=1 Intended Recipients of Results Identification Sequence</summary>
		public static DicomTag IntendedRecipientsOfResultsIdentificationSequence = new DicomTag(0x0040, 0x1011);

		/// <summary>(0040,1101) VR=SQ VM=1 Person Identification Code Sequence</summary>
		public static DicomTag PersonIdentificationCodeSequence = new DicomTag(0x0040, 0x1101);

		/// <summary>(0040,1102) VR=ST VM=1 Person's Address</summary>
		public static DicomTag PersonsAddress = new DicomTag(0x0040, 0x1102);

		/// <summary>(0040,1103) VR=LO VM=1-n Person's Telephone Numbers</summary>
		public static DicomTag PersonsTelephoneNumbers = new DicomTag(0x0040, 0x1103);

		/// <summary>(0040,1400) VR=LT VM=1 Requested Procedure Comments</summary>
		public static DicomTag RequestedProcedureComments = new DicomTag(0x0040, 0x1400);

		/// <summary>(0040,2001) VR=LO VM=1 Reason for the Imaging Service Request (Retired)</summary>
		public static DicomTag ReasonForTheImagingServiceRequestRETIRED = new DicomTag(0x0040, 0x2001);

		/// <summary>(0040,2004) VR=DA VM=1 Issue Date of Imaging Service Request</summary>
		public static DicomTag IssueDateOfImagingServiceRequest = new DicomTag(0x0040, 0x2004);

		/// <summary>(0040,2005) VR=TM VM=1 Issue Time of Imaging Service Request</summary>
		public static DicomTag IssueTimeOfImagingServiceRequest = new DicomTag(0x0040, 0x2005);

		/// <summary>(0040,2006) VR=SH VM=1 Placer Order Number / Imaging Service Request (Retired) (Retired)</summary>
		public static DicomTag PlacerOrderNumberImagingServiceRequestRetiredRETIRED = new DicomTag(0x0040, 0x2006);

		/// <summary>(0040,2007) VR=SH VM=1 Filler Order Number / Imaging Service Request (Retired) (Retired)</summary>
		public static DicomTag FillerOrderNumberImagingServiceRequestRetiredRETIRED = new DicomTag(0x0040, 0x2007);

		/// <summary>(0040,2008) VR=PN VM=1 Order Entered By</summary>
		public static DicomTag OrderEnteredBy = new DicomTag(0x0040, 0x2008);

		/// <summary>(0040,2009) VR=SH VM=1 Order Enterer's Location</summary>
		public static DicomTag OrderEnterersLocation = new DicomTag(0x0040, 0x2009);

		/// <summary>(0040,2010) VR=SH VM=1 Order Callback Phone Number</summary>
		public static DicomTag OrderCallbackPhoneNumber = new DicomTag(0x0040, 0x2010);

		/// <summary>(0040,2016) VR=LO VM=1 Placer Order Number / Imaging Service Request</summary>
		public static DicomTag PlacerOrderNumberImagingServiceRequest = new DicomTag(0x0040, 0x2016);

		/// <summary>(0040,2017) VR=LO VM=1 Filler Order Number / Imaging Service Request</summary>
		public static DicomTag FillerOrderNumberImagingServiceRequest = new DicomTag(0x0040, 0x2017);

		/// <summary>(0040,2400) VR=LT VM=1 Imaging Service Request Comments</summary>
		public static DicomTag ImagingServiceRequestComments = new DicomTag(0x0040, 0x2400);

		/// <summary>(0040,3001) VR=LO VM=1 Confidentiality Constraint on Patient Data Description</summary>
		public static DicomTag ConfidentialityConstraintOnPatientDataDescription = new DicomTag(0x0040, 0x3001);

		/// <summary>(0040,4001) VR=CS VM=1 General Purpose Scheduled Procedure Step Status</summary>
		public static DicomTag GeneralPurposeScheduledProcedureStepStatus = new DicomTag(0x0040, 0x4001);

		/// <summary>(0040,4002) VR=CS VM=1 General Purpose Performed Procedure Step Status</summary>
		public static DicomTag GeneralPurposePerformedProcedureStepStatus = new DicomTag(0x0040, 0x4002);

		/// <summary>(0040,4003) VR=CS VM=1 General Purpose Scheduled Procedure Step Priority</summary>
		public static DicomTag GeneralPurposeScheduledProcedureStepPriority = new DicomTag(0x0040, 0x4003);

		/// <summary>(0040,4004) VR=SQ VM=1 Scheduled Processing Applications Code Sequence</summary>
		public static DicomTag ScheduledProcessingApplicationsCodeSequence = new DicomTag(0x0040, 0x4004);

		/// <summary>(0040,4005) VR=DT VM=1 Scheduled Procedure Step Start Date and Time</summary>
		public static DicomTag ScheduledProcedureStepStartDateAndTime = new DicomTag(0x0040, 0x4005);

		/// <summary>(0040,4006) VR=CS VM=1 Multiple Copies Flag</summary>
		public static DicomTag MultipleCopiesFlag = new DicomTag(0x0040, 0x4006);

		/// <summary>(0040,4007) VR=SQ VM=1 Performed Processing Applications Code Sequence</summary>
		public static DicomTag PerformedProcessingApplicationsCodeSequence = new DicomTag(0x0040, 0x4007);

		/// <summary>(0040,4009) VR=SQ VM=1 Human Performer Code Sequence</summary>
		public static DicomTag HumanPerformerCodeSequence = new DicomTag(0x0040, 0x4009);

		/// <summary>(0040,4010) VR=DT VM=1 Scheduled Procedure Step Modification Date and Time</summary>
		public static DicomTag ScheduledProcedureStepModificationDateAndTime = new DicomTag(0x0040, 0x4010);

		/// <summary>(0040,4011) VR=DT VM=1 Expected Completion Date and Time</summary>
		public static DicomTag ExpectedCompletionDateAndTime = new DicomTag(0x0040, 0x4011);

		/// <summary>(0040,4015) VR=SQ VM=1 Resulting General Purpose Performed Procedure Steps Sequence</summary>
		public static DicomTag ResultingGeneralPurposePerformedProcedureStepsSequence = new DicomTag(0x0040, 0x4015);

		/// <summary>(0040,4016) VR=SQ VM=1 Referenced General Purpose Scheduled Procedure Step Sequence</summary>
		public static DicomTag ReferencedGeneralPurposeScheduledProcedureStepSequence = new DicomTag(0x0040, 0x4016);

		/// <summary>(0040,4018) VR=SQ VM=1 Scheduled Workitem Code Sequence</summary>
		public static DicomTag ScheduledWorkitemCodeSequence = new DicomTag(0x0040, 0x4018);

		/// <summary>(0040,4019) VR=SQ VM=1 Performed Workitem Code Sequence</summary>
		public static DicomTag PerformedWorkitemCodeSequence = new DicomTag(0x0040, 0x4019);

		/// <summary>(0040,4020) VR=CS VM=1 Input Availability Flag</summary>
		public static DicomTag InputAvailabilityFlag = new DicomTag(0x0040, 0x4020);

		/// <summary>(0040,4021) VR=SQ VM=1 Input Information Sequence</summary>
		public static DicomTag InputInformationSequence = new DicomTag(0x0040, 0x4021);

		/// <summary>(0040,4022) VR=SQ VM=1 Relevant Information Sequence</summary>
		public static DicomTag RelevantInformationSequence = new DicomTag(0x0040, 0x4022);

		/// <summary>(0040,4023) VR=UI VM=1 Referenced General Purpose Scheduled Procedure Step Transaction UID</summary>
		public static DicomTag ReferencedGeneralPurposeScheduledProcedureStepTransactionUID = new DicomTag(0x0040, 0x4023);

		/// <summary>(0040,4025) VR=SQ VM=1 Scheduled Station Name Code Sequence</summary>
		public static DicomTag ScheduledStationNameCodeSequence = new DicomTag(0x0040, 0x4025);

		/// <summary>(0040,4026) VR=SQ VM=1 Scheduled Station Class Code Sequence</summary>
		public static DicomTag ScheduledStationClassCodeSequence = new DicomTag(0x0040, 0x4026);

		/// <summary>(0040,4027) VR=SQ VM=1 Scheduled Station Geographic Location Code Sequence</summary>
		public static DicomTag ScheduledStationGeographicLocationCodeSequence = new DicomTag(0x0040, 0x4027);

		/// <summary>(0040,4028) VR=SQ VM=1 Performed Station Name Code Sequence</summary>
		public static DicomTag PerformedStationNameCodeSequence = new DicomTag(0x0040, 0x4028);

		/// <summary>(0040,4029) VR=SQ VM=1 Performed Station Class Code Sequence</summary>
		public static DicomTag PerformedStationClassCodeSequence = new DicomTag(0x0040, 0x4029);

		/// <summary>(0040,4030) VR=SQ VM=1 Performed Station Geographic Location Code Sequence</summary>
		public static DicomTag PerformedStationGeographicLocationCodeSequence = new DicomTag(0x0040, 0x4030);

		/// <summary>(0040,4031) VR=SQ VM=1 Requested Subsequent Workitem Code Sequence</summary>
		public static DicomTag RequestedSubsequentWorkitemCodeSequence = new DicomTag(0x0040, 0x4031);

		/// <summary>(0040,4032) VR=SQ VM=1 Non-DICOM Output Code Sequence</summary>
		public static DicomTag NonDICOMOutputCodeSequence = new DicomTag(0x0040, 0x4032);

		/// <summary>(0040,4033) VR=SQ VM=1 Output Information Sequence</summary>
		public static DicomTag OutputInformationSequence = new DicomTag(0x0040, 0x4033);

		/// <summary>(0040,4034) VR=SQ VM=1 Scheduled Human Performers Sequence</summary>
		public static DicomTag ScheduledHumanPerformersSequence = new DicomTag(0x0040, 0x4034);

		/// <summary>(0040,4035) VR=SQ VM=1 Actual Human Performers Sequence</summary>
		public static DicomTag ActualHumanPerformersSequence = new DicomTag(0x0040, 0x4035);

		/// <summary>(0040,4036) VR=LO VM=1 Human Performer's Organization</summary>
		public static DicomTag HumanPerformersOrganization = new DicomTag(0x0040, 0x4036);

		/// <summary>(0040,4037) VR=PN VM=1 Human Performer's Name</summary>
		public static DicomTag HumanPerformersName = new DicomTag(0x0040, 0x4037);

		/// <summary>(0040,8302) VR=DS VM=1 Entrance Dose in mGy</summary>
		public static DicomTag EntranceDoseInMGy = new DicomTag(0x0040, 0x8302);

		/// <summary>(0040,9094) VR=SQ VM=1 Referenced Image Real World Value Mapping Sequence</summary>
		public static DicomTag ReferencedImageRealWorldValueMappingSequence = new DicomTag(0x0040, 0x9094);

		/// <summary>(0040,9096) VR=SQ VM=1 Real World Value Mapping Sequence</summary>
		public static DicomTag RealWorldValueMappingSequence = new DicomTag(0x0040, 0x9096);

		/// <summary>(0040,9098) VR=SQ VM=1 Pixel Value Mapping Code Sequence</summary>
		public static DicomTag PixelValueMappingCodeSequence = new DicomTag(0x0040, 0x9098);

		/// <summary>(0040,9210) VR=SH VM=1 LUT Label</summary>
		public static DicomTag LUTLabel = new DicomTag(0x0040, 0x9210);

		/// <summary>(0040,9211) VR=US/SS VM=1 Real World Value Last Value Mapped</summary>
		public static DicomTag RealWorldValueLastValueMapped = new DicomTag(0x0040, 0x9211);

		/// <summary>(0040,9212) VR=FD VM=1-n Real World Value LUT Data</summary>
		public static DicomTag RealWorldValueLUTData = new DicomTag(0x0040, 0x9212);

		/// <summary>(0040,9216) VR=US/SS VM=1 Real World Value First Value Mapped</summary>
		public static DicomTag RealWorldValueFirstValueMapped = new DicomTag(0x0040, 0x9216);

		/// <summary>(0040,9224) VR=FD VM=1 Real World Value Intercept</summary>
		public static DicomTag RealWorldValueIntercept = new DicomTag(0x0040, 0x9224);

		/// <summary>(0040,9225) VR=FD VM=1 Real World Value Slope</summary>
		public static DicomTag RealWorldValueSlope = new DicomTag(0x0040, 0x9225);

		/// <summary>(0040,a010) VR=CS VM=1 Relationship Type</summary>
		public static DicomTag RelationshipType = new DicomTag(0x0040, 0xa010);

		/// <summary>(0040,a027) VR=LO VM=1 Verifying Organization</summary>
		public static DicomTag VerifyingOrganization = new DicomTag(0x0040, 0xa027);

		/// <summary>(0040,a030) VR=DT VM=1 Verification Date Time</summary>
		public static DicomTag VerificationDateTime = new DicomTag(0x0040, 0xa030);

		/// <summary>(0040,a032) VR=DT VM=1 Observation Date Time</summary>
		public static DicomTag ObservationDateTime = new DicomTag(0x0040, 0xa032);

		/// <summary>(0040,a040) VR=CS VM=1 Value Type</summary>
		public static DicomTag ValueType = new DicomTag(0x0040, 0xa040);

		/// <summary>(0040,a043) VR=SQ VM=1 Concept Name Code Sequence</summary>
		public static DicomTag ConceptNameCodeSequence = new DicomTag(0x0040, 0xa043);

		/// <summary>(0040,a050) VR=CS VM=1 Continuity Of Content</summary>
		public static DicomTag ContinuityOfContent = new DicomTag(0x0040, 0xa050);

		/// <summary>(0040,a073) VR=SQ VM=1 Verifying Observer Sequence</summary>
		public static DicomTag VerifyingObserverSequence = new DicomTag(0x0040, 0xa073);

		/// <summary>(0040,a075) VR=PN VM=1 Verifying Observer Name</summary>
		public static DicomTag VerifyingObserverName = new DicomTag(0x0040, 0xa075);

		/// <summary>(0040,a078) VR=SQ VM=1 Author Observer Sequence</summary>
		public static DicomTag AuthorObserverSequence = new DicomTag(0x0040, 0xa078);

		/// <summary>(0040,a07a) VR=SQ VM=1 Participant Sequence</summary>
		public static DicomTag ParticipantSequence = new DicomTag(0x0040, 0xa07a);

		/// <summary>(0040,a07c) VR=SQ VM=1 Custodial Organization Sequence</summary>
		public static DicomTag CustodialOrganizationSequence = new DicomTag(0x0040, 0xa07c);

		/// <summary>(0040,a080) VR=CS VM=1 Participation Type</summary>
		public static DicomTag ParticipationType = new DicomTag(0x0040, 0xa080);

		/// <summary>(0040,a082) VR=DT VM=1 Participation DateTime</summary>
		public static DicomTag ParticipationDateTime = new DicomTag(0x0040, 0xa082);

		/// <summary>(0040,a084) VR=CS VM=1 Observer Type</summary>
		public static DicomTag ObserverType = new DicomTag(0x0040, 0xa084);

		/// <summary>(0040,a088) VR=SQ VM=1 Verifying Observer Identification Code Sequence</summary>
		public static DicomTag VerifyingObserverIdentificationCodeSequence = new DicomTag(0x0040, 0xa088);

		/// <summary>(0040,a090) VR=SQ VM=1 Equivalent CDA Document Sequence (Retired)</summary>
		public static DicomTag EquivalentCDADocumentSequenceRETIRED = new DicomTag(0x0040, 0xa090);

		/// <summary>(0040,a0b0) VR=US VM=2-2n Referenced Waveform Channels</summary>
		public static DicomTag ReferencedWaveformChannels = new DicomTag(0x0040, 0xa0b0);

		/// <summary>(0040,a120) VR=DT VM=1 DateTime</summary>
		public static DicomTag DateTime = new DicomTag(0x0040, 0xa120);

		/// <summary>(0040,a121) VR=DA VM=1 Date</summary>
		public static DicomTag Date = new DicomTag(0x0040, 0xa121);

		/// <summary>(0040,a122) VR=TM VM=1 Time</summary>
		public static DicomTag Time = new DicomTag(0x0040, 0xa122);

		/// <summary>(0040,a123) VR=PN VM=1 Person Name</summary>
		public static DicomTag PersonName = new DicomTag(0x0040, 0xa123);

		/// <summary>(0040,a124) VR=UI VM=1 UID</summary>
		public static DicomTag UID = new DicomTag(0x0040, 0xa124);

		/// <summary>(0040,a130) VR=CS VM=1 Temporal Range Type</summary>
		public static DicomTag TemporalRangeType = new DicomTag(0x0040, 0xa130);

		/// <summary>(0040,a132) VR=UL VM=1-n Referenced Sample Positions</summary>
		public static DicomTag ReferencedSamplePositions = new DicomTag(0x0040, 0xa132);

		/// <summary>(0040,a136) VR=US VM=1-n Referenced Frame Numbers</summary>
		public static DicomTag ReferencedFrameNumbers = new DicomTag(0x0040, 0xa136);

		/// <summary>(0040,a138) VR=DS VM=1-n Referenced Time Offsets</summary>
		public static DicomTag ReferencedTimeOffsets = new DicomTag(0x0040, 0xa138);

		/// <summary>(0040,a13a) VR=DT VM=1-n Referenced DateTime</summary>
		public static DicomTag ReferencedDateTime = new DicomTag(0x0040, 0xa13a);

		/// <summary>(0040,a160) VR=UT VM=1 Text Value</summary>
		public static DicomTag TextValue = new DicomTag(0x0040, 0xa160);

		/// <summary>(0040,a168) VR=SQ VM=1 Concept Code Sequence</summary>
		public static DicomTag ConceptCodeSequence = new DicomTag(0x0040, 0xa168);

		/// <summary>(0040,a170) VR=SQ VM=1 Purpose of Reference Code Sequence</summary>
		public static DicomTag PurposeOfReferenceCodeSequence = new DicomTag(0x0040, 0xa170);

		/// <summary>(0040,a180) VR=US VM=1 Annotation Group Number</summary>
		public static DicomTag AnnotationGroupNumber = new DicomTag(0x0040, 0xa180);

		/// <summary>(0040,a195) VR=SQ VM=1 Modifier Code Sequence</summary>
		public static DicomTag ModifierCodeSequence = new DicomTag(0x0040, 0xa195);

		/// <summary>(0040,a300) VR=SQ VM=1 Measured Value Sequence</summary>
		public static DicomTag MeasuredValueSequence = new DicomTag(0x0040, 0xa300);

		/// <summary>(0040,a301) VR=SQ VM=1 Numeric Value Qualifier Code Sequence</summary>
		public static DicomTag NumericValueQualifierCodeSequence = new DicomTag(0x0040, 0xa301);

		/// <summary>(0040,a30a) VR=DS VM=1-n Numeric Value</summary>
		public static DicomTag NumericValue = new DicomTag(0x0040, 0xa30a);

		/// <summary>(0040,a353) VR=ST VM=1 Address - Trial (Retired)</summary>
		public static DicomTag AddressTrialRETIRED = new DicomTag(0x0040, 0xa353);

		/// <summary>(0040,a354) VR=LO VM=1 Telephone Number - Trial (Retired)</summary>
		public static DicomTag TelephoneNumberTrialRETIRED = new DicomTag(0x0040, 0xa354);

		/// <summary>(0040,a360) VR=SQ VM=1 Predecessor Documents Sequence</summary>
		public static DicomTag PredecessorDocumentsSequence = new DicomTag(0x0040, 0xa360);

		/// <summary>(0040,a370) VR=SQ VM=1 Referenced Request Sequence</summary>
		public static DicomTag ReferencedRequestSequence = new DicomTag(0x0040, 0xa370);

		/// <summary>(0040,a372) VR=SQ VM=1 Performed Procedure Code Sequence</summary>
		public static DicomTag PerformedProcedureCodeSequence = new DicomTag(0x0040, 0xa372);

		/// <summary>(0040,a375) VR=SQ VM=1 Current Requested Procedure Evidence Sequence</summary>
		public static DicomTag CurrentRequestedProcedureEvidenceSequence = new DicomTag(0x0040, 0xa375);

		/// <summary>(0040,a385) VR=SQ VM=1 Pertinent Other Evidence Sequence</summary>
		public static DicomTag PertinentOtherEvidenceSequence = new DicomTag(0x0040, 0xa385);

		/// <summary>(0040,a390) VR=SQ VM=1 HL7 Structured Document Reference Sequence</summary>
		public static DicomTag HL7StructuredDocumentReferenceSequence = new DicomTag(0x0040, 0xa390);

		/// <summary>(0040,a491) VR=CS VM=1 Completion Flag</summary>
		public static DicomTag CompletionFlag = new DicomTag(0x0040, 0xa491);

		/// <summary>(0040,a492) VR=LO VM=1 Completion Flag Description</summary>
		public static DicomTag CompletionFlagDescription = new DicomTag(0x0040, 0xa492);

		/// <summary>(0040,a493) VR=CS VM=1 Verification Flag</summary>
		public static DicomTag VerificationFlag = new DicomTag(0x0040, 0xa493);

		/// <summary>(0040,a494) VR=CS VM=1 Archive Requested</summary>
		public static DicomTag ArchiveRequested = new DicomTag(0x0040, 0xa494);

		/// <summary>(0040,a504) VR=SQ VM=1 Content Template Sequence</summary>
		public static DicomTag ContentTemplateSequence = new DicomTag(0x0040, 0xa504);

		/// <summary>(0040,a525) VR=SQ VM=1 Identical Documents Sequence</summary>
		public static DicomTag IdenticalDocumentsSequence = new DicomTag(0x0040, 0xa525);

		/// <summary>(0040,a730) VR=SQ VM=1 Content Sequence</summary>
		public static DicomTag ContentSequence = new DicomTag(0x0040, 0xa730);

		/// <summary>(0040,b020) VR=SQ VM=1 Annotation Sequence</summary>
		public static DicomTag AnnotationSequence = new DicomTag(0x0040, 0xb020);

		/// <summary>(0040,db00) VR=CS VM=1 Template Identifier</summary>
		public static DicomTag TemplateIdentifier = new DicomTag(0x0040, 0xdb00);

		/// <summary>(0040,db06) VR=DT VM=1 Template Version (Retired)</summary>
		public static DicomTag TemplateVersionRETIRED = new DicomTag(0x0040, 0xdb06);

		/// <summary>(0040,db07) VR=DT VM=1 Template Local Version (Retired)</summary>
		public static DicomTag TemplateLocalVersionRETIRED = new DicomTag(0x0040, 0xdb07);

		/// <summary>(0040,db0b) VR=CS VM=1 Template Extension Flag (Retired)</summary>
		public static DicomTag TemplateExtensionFlagRETIRED = new DicomTag(0x0040, 0xdb0b);

		/// <summary>(0040,db0c) VR=UI VM=1 Template Extension Organization UID (Retired)</summary>
		public static DicomTag TemplateExtensionOrganizationUIDRETIRED = new DicomTag(0x0040, 0xdb0c);

		/// <summary>(0040,db0d) VR=UI VM=1 Template Extension Creator UID (Retired)</summary>
		public static DicomTag TemplateExtensionCreatorUIDRETIRED = new DicomTag(0x0040, 0xdb0d);

		/// <summary>(0040,db73) VR=UL VM=1-n Referenced Content Item Identifier</summary>
		public static DicomTag ReferencedContentItemIdentifier = new DicomTag(0x0040, 0xdb73);

		/// <summary>(0040,e001) VR=ST VM=1 HL7 Instance Identifier</summary>
		public static DicomTag HL7InstanceIdentifier = new DicomTag(0x0040, 0xe001);

		/// <summary>(0040,e004) VR=DT VM=1 HL7 Document Effective Time</summary>
		public static DicomTag HL7DocumentEffectiveTime = new DicomTag(0x0040, 0xe004);

		/// <summary>(0040,e006) VR=SQ VM=1 HL7 Document Type Code Sequence</summary>
		public static DicomTag HL7DocumentTypeCodeSequence = new DicomTag(0x0040, 0xe006);

		/// <summary>(0040,e010) VR=UT VM=1 Retrieve URI</summary>
		public static DicomTag RetrieveURI = new DicomTag(0x0040, 0xe010);

		/// <summary>(0042,0010) VR=ST VM=1 Document Title</summary>
		public static DicomTag DocumentTitle = new DicomTag(0x0042, 0x0010);

		/// <summary>(0042,0011) VR=OB VM=1 Encapsulated Document</summary>
		public static DicomTag EncapsulatedDocument = new DicomTag(0x0042, 0x0011);

		/// <summary>(0042,0012) VR=LO VM=1 MIME Type of Encapsulated Document</summary>
		public static DicomTag MIMETypeOfEncapsulatedDocument = new DicomTag(0x0042, 0x0012);

		/// <summary>(0042,0013) VR=SQ VM=1 Source Instance Sequence</summary>
		public static DicomTag SourceInstanceSequence = new DicomTag(0x0042, 0x0013);

		/// <summary>(0042,0014) VR=LO VM=1-n List of MIME Types</summary>
		public static DicomTag ListOfMIMETypes = new DicomTag(0x0042, 0x0014);

		/// <summary>(0044,0001) VR=ST VM=1 Product Package Identifier</summary>
		public static DicomTag ProductPackageIdentifier = new DicomTag(0x0044, 0x0001);

		/// <summary>(0044,0002) VR=CS VM=1 Substance Administration Approval</summary>
		public static DicomTag SubstanceAdministrationApproval = new DicomTag(0x0044, 0x0002);

		/// <summary>(0044,0003) VR=LT VM=1 Approval Status Further Description</summary>
		public static DicomTag ApprovalStatusFurtherDescription = new DicomTag(0x0044, 0x0003);

		/// <summary>(0044,0004) VR=DT VM=1 Approval Status DateTime</summary>
		public static DicomTag ApprovalStatusDateTime = new DicomTag(0x0044, 0x0004);

		/// <summary>(0044,0007) VR=SQ VM=1 Product Type Code Sequence</summary>
		public static DicomTag ProductTypeCodeSequence = new DicomTag(0x0044, 0x0007);

		/// <summary>(0044,0008) VR=LO VM=1-n Product Name</summary>
		public static DicomTag ProductName = new DicomTag(0x0044, 0x0008);

		/// <summary>(0044,0009) VR=LT VM=1 Product Description</summary>
		public static DicomTag ProductDescription = new DicomTag(0x0044, 0x0009);

		/// <summary>(0044,000a) VR=LO VM=1 Product Lot Identifier</summary>
		public static DicomTag ProductLotIdentifier = new DicomTag(0x0044, 0x000a);

		/// <summary>(0044,000b) VR=DT VM=1 Product Expiration DateTime</summary>
		public static DicomTag ProductExpirationDateTime = new DicomTag(0x0044, 0x000b);

		/// <summary>(0044,0010) VR=DT VM=1 Substance Administration DateTime</summary>
		public static DicomTag SubstanceAdministrationDateTime = new DicomTag(0x0044, 0x0010);

		/// <summary>(0044,0011) VR=LO VM=1 Substance Administration Notes</summary>
		public static DicomTag SubstanceAdministrationNotes = new DicomTag(0x0044, 0x0011);

		/// <summary>(0044,0012) VR=LO VM=1 Substance Administration Device ID</summary>
		public static DicomTag SubstanceAdministrationDeviceID = new DicomTag(0x0044, 0x0012);

		/// <summary>(0044,0013) VR=SQ VM=1 Product Parameter Sequence</summary>
		public static DicomTag ProductParameterSequence = new DicomTag(0x0044, 0x0013);

		/// <summary>(0044,0019) VR=SQ VM=1 Substance Administration Parameter Sequence</summary>
		public static DicomTag SubstanceAdministrationParameterSequence = new DicomTag(0x0044, 0x0019);

		/// <summary>(0050,0004) VR=CS VM=1 Calibration Image</summary>
		public static DicomTag CalibrationImage = new DicomTag(0x0050, 0x0004);

		/// <summary>(0050,0010) VR=SQ VM=1 Device Sequence</summary>
		public static DicomTag DeviceSequence = new DicomTag(0x0050, 0x0010);

		/// <summary>(0050,0014) VR=DS VM=1 Device Length</summary>
		public static DicomTag DeviceLength = new DicomTag(0x0050, 0x0014);

		/// <summary>(0050,0016) VR=DS VM=1 Device Diameter</summary>
		public static DicomTag DeviceDiameter = new DicomTag(0x0050, 0x0016);

		/// <summary>(0050,0017) VR=CS VM=1 Device Diameter Units</summary>
		public static DicomTag DeviceDiameterUnits = new DicomTag(0x0050, 0x0017);

		/// <summary>(0050,0018) VR=DS VM=1 Device Volume</summary>
		public static DicomTag DeviceVolume = new DicomTag(0x0050, 0x0018);

		/// <summary>(0050,0019) VR=DS VM=1 Intermarker Distance</summary>
		public static DicomTag IntermarkerDistance = new DicomTag(0x0050, 0x0019);

		/// <summary>(0050,0020) VR=LO VM=1 Device Description</summary>
		public static DicomTag DeviceDescription = new DicomTag(0x0050, 0x0020);

		/// <summary>(0054,0010) VR=US VM=1-n Energy Window Vector</summary>
		public static DicomTag EnergyWindowVector = new DicomTag(0x0054, 0x0010);

		/// <summary>(0054,0011) VR=US VM=1 Number of Energy Windows</summary>
		public static DicomTag NumberOfEnergyWindows = new DicomTag(0x0054, 0x0011);

		/// <summary>(0054,0012) VR=SQ VM=1 Energy Window Information Sequence</summary>
		public static DicomTag EnergyWindowInformationSequence = new DicomTag(0x0054, 0x0012);

		/// <summary>(0054,0013) VR=SQ VM=1 Energy Window Range Sequence</summary>
		public static DicomTag EnergyWindowRangeSequence = new DicomTag(0x0054, 0x0013);

		/// <summary>(0054,0014) VR=DS VM=1 Energy Window Lower Limit</summary>
		public static DicomTag EnergyWindowLowerLimit = new DicomTag(0x0054, 0x0014);

		/// <summary>(0054,0015) VR=DS VM=1 Energy Window Upper Limit</summary>
		public static DicomTag EnergyWindowUpperLimit = new DicomTag(0x0054, 0x0015);

		/// <summary>(0054,0016) VR=SQ VM=1 Radiopharmaceutical Information Sequence</summary>
		public static DicomTag RadiopharmaceuticalInformationSequence = new DicomTag(0x0054, 0x0016);

		/// <summary>(0054,0017) VR=IS VM=1 Residual Syringe Counts</summary>
		public static DicomTag ResidualSyringeCounts = new DicomTag(0x0054, 0x0017);

		/// <summary>(0054,0018) VR=SH VM=1 Energy Window Name</summary>
		public static DicomTag EnergyWindowName = new DicomTag(0x0054, 0x0018);

		/// <summary>(0054,0020) VR=US VM=1-n Detector Vector</summary>
		public static DicomTag DetectorVector = new DicomTag(0x0054, 0x0020);

		/// <summary>(0054,0021) VR=US VM=1 Number of Detectors</summary>
		public static DicomTag NumberOfDetectors = new DicomTag(0x0054, 0x0021);

		/// <summary>(0054,0022) VR=SQ VM=1 Detector Information Sequence</summary>
		public static DicomTag DetectorInformationSequence = new DicomTag(0x0054, 0x0022);

		/// <summary>(0054,0030) VR=US VM=1-n Phase Vector</summary>
		public static DicomTag PhaseVector = new DicomTag(0x0054, 0x0030);

		/// <summary>(0054,0031) VR=US VM=1 Number of Phases</summary>
		public static DicomTag NumberOfPhases = new DicomTag(0x0054, 0x0031);

		/// <summary>(0054,0032) VR=SQ VM=1 Phase Information Sequence</summary>
		public static DicomTag PhaseInformationSequence = new DicomTag(0x0054, 0x0032);

		/// <summary>(0054,0033) VR=US VM=1 Number of Frames in Phase</summary>
		public static DicomTag NumberOfFramesInPhase = new DicomTag(0x0054, 0x0033);

		/// <summary>(0054,0036) VR=IS VM=1 Phase Delay</summary>
		public static DicomTag PhaseDelay = new DicomTag(0x0054, 0x0036);

		/// <summary>(0054,0038) VR=IS VM=1 Pause Between Frames</summary>
		public static DicomTag PauseBetweenFrames = new DicomTag(0x0054, 0x0038);

		/// <summary>(0054,0039) VR=CS VM=1 Phase Description</summary>
		public static DicomTag PhaseDescription = new DicomTag(0x0054, 0x0039);

		/// <summary>(0054,0050) VR=US VM=1-n Rotation Vector</summary>
		public static DicomTag RotationVector = new DicomTag(0x0054, 0x0050);

		/// <summary>(0054,0051) VR=US VM=1 Number of Rotations</summary>
		public static DicomTag NumberOfRotations = new DicomTag(0x0054, 0x0051);

		/// <summary>(0054,0052) VR=SQ VM=1 Rotation Information Sequence</summary>
		public static DicomTag RotationInformationSequence = new DicomTag(0x0054, 0x0052);

		/// <summary>(0054,0053) VR=US VM=1 Number of Frames in Rotation</summary>
		public static DicomTag NumberOfFramesInRotation = new DicomTag(0x0054, 0x0053);

		/// <summary>(0054,0060) VR=US VM=1-n R-R Interval Vector</summary>
		public static DicomTag RRIntervalVector = new DicomTag(0x0054, 0x0060);

		/// <summary>(0054,0061) VR=US VM=1 Number of R-R Intervals</summary>
		public static DicomTag NumberOfRRIntervals = new DicomTag(0x0054, 0x0061);

		/// <summary>(0054,0062) VR=SQ VM=1 Gated Information Sequence</summary>
		public static DicomTag GatedInformationSequence = new DicomTag(0x0054, 0x0062);

		/// <summary>(0054,0063) VR=SQ VM=1 Data Information Sequence</summary>
		public static DicomTag DataInformationSequence = new DicomTag(0x0054, 0x0063);

		/// <summary>(0054,0070) VR=US VM=1-n Time Slot Vector</summary>
		public static DicomTag TimeSlotVector = new DicomTag(0x0054, 0x0070);

		/// <summary>(0054,0071) VR=US VM=1 Number of Time Slots</summary>
		public static DicomTag NumberOfTimeSlots = new DicomTag(0x0054, 0x0071);

		/// <summary>(0054,0072) VR=SQ VM=1 Time Slot Information Sequence</summary>
		public static DicomTag TimeSlotInformationSequence = new DicomTag(0x0054, 0x0072);

		/// <summary>(0054,0073) VR=DS VM=1 Time Slot Time</summary>
		public static DicomTag TimeSlotTime = new DicomTag(0x0054, 0x0073);

		/// <summary>(0054,0080) VR=US VM=1-n Slice Vector</summary>
		public static DicomTag SliceVector = new DicomTag(0x0054, 0x0080);

		/// <summary>(0054,0081) VR=US VM=1 Number of Slices</summary>
		public static DicomTag NumberOfSlices = new DicomTag(0x0054, 0x0081);

		/// <summary>(0054,0090) VR=US VM=1-n Angular View Vector</summary>
		public static DicomTag AngularViewVector = new DicomTag(0x0054, 0x0090);

		/// <summary>(0054,0100) VR=US VM=1-n Time Slice Vector</summary>
		public static DicomTag TimeSliceVector = new DicomTag(0x0054, 0x0100);

		/// <summary>(0054,0101) VR=US VM=1 Number of Time Slices</summary>
		public static DicomTag NumberOfTimeSlices = new DicomTag(0x0054, 0x0101);

		/// <summary>(0054,0200) VR=DS VM=1 Start Angle</summary>
		public static DicomTag StartAngle = new DicomTag(0x0054, 0x0200);

		/// <summary>(0054,0202) VR=CS VM=1 Type of Detector Motion</summary>
		public static DicomTag TypeOfDetectorMotion = new DicomTag(0x0054, 0x0202);

		/// <summary>(0054,0210) VR=IS VM=1-n Trigger Vector</summary>
		public static DicomTag TriggerVector = new DicomTag(0x0054, 0x0210);

		/// <summary>(0054,0211) VR=US VM=1 Number of Triggers in Phase</summary>
		public static DicomTag NumberOfTriggersInPhase = new DicomTag(0x0054, 0x0211);

		/// <summary>(0054,0220) VR=SQ VM=1 View Code Sequence</summary>
		public static DicomTag ViewCodeSequence = new DicomTag(0x0054, 0x0220);

		/// <summary>(0054,0222) VR=SQ VM=1 View Modifier Code Sequence</summary>
		public static DicomTag ViewModifierCodeSequence = new DicomTag(0x0054, 0x0222);

		/// <summary>(0054,0300) VR=SQ VM=1 Radionuclide Code Sequence</summary>
		public static DicomTag RadionuclideCodeSequence = new DicomTag(0x0054, 0x0300);

		/// <summary>(0054,0302) VR=SQ VM=1 Administration Route Code Sequence</summary>
		public static DicomTag AdministrationRouteCodeSequence = new DicomTag(0x0054, 0x0302);

		/// <summary>(0054,0304) VR=SQ VM=1 Radiopharmaceutical Code Sequence</summary>
		public static DicomTag RadiopharmaceuticalCodeSequence = new DicomTag(0x0054, 0x0304);

		/// <summary>(0054,0306) VR=SQ VM=1 Calibration Data Sequence</summary>
		public static DicomTag CalibrationDataSequence = new DicomTag(0x0054, 0x0306);

		/// <summary>(0054,0308) VR=US VM=1 Energy Window Number</summary>
		public static DicomTag EnergyWindowNumber = new DicomTag(0x0054, 0x0308);

		/// <summary>(0054,0400) VR=SH VM=1 Image ID</summary>
		public static DicomTag ImageID = new DicomTag(0x0054, 0x0400);

		/// <summary>(0054,0410) VR=SQ VM=1 Patient Orientation Code Sequence</summary>
		public static DicomTag PatientOrientationCodeSequence = new DicomTag(0x0054, 0x0410);

		/// <summary>(0054,0412) VR=SQ VM=1 Patient Orientation Modifier Code Sequence</summary>
		public static DicomTag PatientOrientationModifierCodeSequence = new DicomTag(0x0054, 0x0412);

		/// <summary>(0054,0414) VR=SQ VM=1 Patient Gantry Relationship Code Sequence</summary>
		public static DicomTag PatientGantryRelationshipCodeSequence = new DicomTag(0x0054, 0x0414);

		/// <summary>(0054,0500) VR=CS VM=1 Slice Progression Direction</summary>
		public static DicomTag SliceProgressionDirection = new DicomTag(0x0054, 0x0500);

		/// <summary>(0054,1000) VR=CS VM=2 Series Type</summary>
		public static DicomTag SeriesType = new DicomTag(0x0054, 0x1000);

		/// <summary>(0054,1001) VR=CS VM=1 Units</summary>
		public static DicomTag Units = new DicomTag(0x0054, 0x1001);

		/// <summary>(0054,1002) VR=CS VM=1 Counts Source</summary>
		public static DicomTag CountsSource = new DicomTag(0x0054, 0x1002);

		/// <summary>(0054,1004) VR=CS VM=1 Reprojection Method</summary>
		public static DicomTag ReprojectionMethod = new DicomTag(0x0054, 0x1004);

		/// <summary>(0054,1100) VR=CS VM=1 Randoms Correction Method</summary>
		public static DicomTag RandomsCorrectionMethod = new DicomTag(0x0054, 0x1100);

		/// <summary>(0054,1101) VR=LO VM=1 Attenuation Correction Method</summary>
		public static DicomTag AttenuationCorrectionMethod = new DicomTag(0x0054, 0x1101);

		/// <summary>(0054,1102) VR=CS VM=1 Decay Correction</summary>
		public static DicomTag DecayCorrection = new DicomTag(0x0054, 0x1102);

		/// <summary>(0054,1103) VR=LO VM=1 Reconstruction Method</summary>
		public static DicomTag ReconstructionMethod = new DicomTag(0x0054, 0x1103);

		/// <summary>(0054,1104) VR=LO VM=1 Detector Lines of Response Used</summary>
		public static DicomTag DetectorLinesOfResponseUsed = new DicomTag(0x0054, 0x1104);

		/// <summary>(0054,1105) VR=LO VM=1 Scatter Correction Method</summary>
		public static DicomTag ScatterCorrectionMethod = new DicomTag(0x0054, 0x1105);

		/// <summary>(0054,1200) VR=DS VM=1 Axial Acceptance</summary>
		public static DicomTag AxialAcceptance = new DicomTag(0x0054, 0x1200);

		/// <summary>(0054,1201) VR=IS VM=2 Axial Mash</summary>
		public static DicomTag AxialMash = new DicomTag(0x0054, 0x1201);

		/// <summary>(0054,1202) VR=IS VM=1 Transverse Mash</summary>
		public static DicomTag TransverseMash = new DicomTag(0x0054, 0x1202);

		/// <summary>(0054,1203) VR=DS VM=2 Detector Element Size</summary>
		public static DicomTag DetectorElementSize = new DicomTag(0x0054, 0x1203);

		/// <summary>(0054,1210) VR=DS VM=1 Coincidence Window Width</summary>
		public static DicomTag CoincidenceWindowWidth = new DicomTag(0x0054, 0x1210);

		/// <summary>(0054,1220) VR=CS VM=1-n Secondary Counts Type</summary>
		public static DicomTag SecondaryCountsType = new DicomTag(0x0054, 0x1220);

		/// <summary>(0054,1300) VR=DS VM=1 Frame Reference Time</summary>
		public static DicomTag FrameReferenceTime = new DicomTag(0x0054, 0x1300);

		/// <summary>(0054,1310) VR=IS VM=1 Primary (Prompts) Counts Accumulated</summary>
		public static DicomTag PrimaryPromptsCountsAccumulated = new DicomTag(0x0054, 0x1310);

		/// <summary>(0054,1311) VR=IS VM=1-n Secondary Counts Accumulated</summary>
		public static DicomTag SecondaryCountsAccumulated = new DicomTag(0x0054, 0x1311);

		/// <summary>(0054,1320) VR=DS VM=1 Slice Sensitivity Factor</summary>
		public static DicomTag SliceSensitivityFactor = new DicomTag(0x0054, 0x1320);

		/// <summary>(0054,1321) VR=DS VM=1 Decay Factor</summary>
		public static DicomTag DecayFactor = new DicomTag(0x0054, 0x1321);

		/// <summary>(0054,1322) VR=DS VM=1 Dose Calibration Factor</summary>
		public static DicomTag DoseCalibrationFactor = new DicomTag(0x0054, 0x1322);

		/// <summary>(0054,1323) VR=DS VM=1 Scatter Fraction Factor</summary>
		public static DicomTag ScatterFractionFactor = new DicomTag(0x0054, 0x1323);

		/// <summary>(0054,1324) VR=DS VM=1 Dead Time Factor</summary>
		public static DicomTag DeadTimeFactor = new DicomTag(0x0054, 0x1324);

		/// <summary>(0054,1330) VR=US VM=1 Image Index</summary>
		public static DicomTag ImageIndex = new DicomTag(0x0054, 0x1330);

		/// <summary>(0054,1400) VR=CS VM=1-n Counts Included (Retired)</summary>
		public static DicomTag CountsIncludedRETIRED = new DicomTag(0x0054, 0x1400);

		/// <summary>(0054,1401) VR=CS VM=1 Dead Time Correction Flag (Retired)</summary>
		public static DicomTag DeadTimeCorrectionFlagRETIRED = new DicomTag(0x0054, 0x1401);

		/// <summary>(0060,3000) VR=SQ VM=1 Histogram Sequence</summary>
		public static DicomTag HistogramSequence = new DicomTag(0x0060, 0x3000);

		/// <summary>(0060,3002) VR=US VM=1 Histogram Number of Bins</summary>
		public static DicomTag HistogramNumberOfBins = new DicomTag(0x0060, 0x3002);

		/// <summary>(0060,3004) VR=US/SS VM=1 Histogram First Bin Value</summary>
		public static DicomTag HistogramFirstBinValue = new DicomTag(0x0060, 0x3004);

		/// <summary>(0060,3006) VR=US/SS VM=1 Histogram Last Bin Value</summary>
		public static DicomTag HistogramLastBinValue = new DicomTag(0x0060, 0x3006);

		/// <summary>(0060,3008) VR=US VM=1 Histogram Bin Width</summary>
		public static DicomTag HistogramBinWidth = new DicomTag(0x0060, 0x3008);

		/// <summary>(0060,3010) VR=LO VM=1 Histogram Explanation</summary>
		public static DicomTag HistogramExplanation = new DicomTag(0x0060, 0x3010);

		/// <summary>(0060,3020) VR=UL VM=1-n Histogram Data</summary>
		public static DicomTag HistogramData = new DicomTag(0x0060, 0x3020);

		/// <summary>(0062,0001) VR=CS VM=1 Segmentation Type</summary>
		public static DicomTag SegmentationType = new DicomTag(0x0062, 0x0001);

		/// <summary>(0062,0002) VR=SQ VM=1 Segment Sequence</summary>
		public static DicomTag SegmentSequence = new DicomTag(0x0062, 0x0002);

		/// <summary>(0062,0003) VR=SQ VM=1 Segmented Property Category Code Sequence</summary>
		public static DicomTag SegmentedPropertyCategoryCodeSequence = new DicomTag(0x0062, 0x0003);

		/// <summary>(0062,0004) VR=US VM=1 Segment Number</summary>
		public static DicomTag SegmentNumber = new DicomTag(0x0062, 0x0004);

		/// <summary>(0062,0005) VR=LO VM=1 Segment Label</summary>
		public static DicomTag SegmentLabel = new DicomTag(0x0062, 0x0005);

		/// <summary>(0062,0006) VR=ST VM=1 Segment Description</summary>
		public static DicomTag SegmentDescription = new DicomTag(0x0062, 0x0006);

		/// <summary>(0062,0008) VR=CS VM=1 Segment Algorithm Type</summary>
		public static DicomTag SegmentAlgorithmType = new DicomTag(0x0062, 0x0008);

		/// <summary>(0062,0009) VR=LO VM=1 Segment Algorithm Name</summary>
		public static DicomTag SegmentAlgorithmName = new DicomTag(0x0062, 0x0009);

		/// <summary>(0062,000a) VR=SQ VM=1 Segment Identification Sequence</summary>
		public static DicomTag SegmentIdentificationSequence = new DicomTag(0x0062, 0x000a);

		/// <summary>(0062,000b) VR=US VM=1-n Referenced Segment Number</summary>
		public static DicomTag ReferencedSegmentNumber = new DicomTag(0x0062, 0x000b);

		/// <summary>(0062,000c) VR=US VM=1 Recommended Display Grayscale Value</summary>
		public static DicomTag RecommendedDisplayGrayscaleValue = new DicomTag(0x0062, 0x000c);

		/// <summary>(0062,000d) VR=US VM=3 Recommended Display CIELab Value</summary>
		public static DicomTag RecommendedDisplayCIELabValue = new DicomTag(0x0062, 0x000d);

		/// <summary>(0062,000e) VR=US VM=1 Maximum Fractional Value</summary>
		public static DicomTag MaximumFractionalValue = new DicomTag(0x0062, 0x000e);

		/// <summary>(0062,000f) VR=SQ VM=1 Segmented Property Type Code Sequence</summary>
		public static DicomTag SegmentedPropertyTypeCodeSequence = new DicomTag(0x0062, 0x000f);

		/// <summary>(0062,0010) VR=CS VM=1 Segmentation Fractional Type</summary>
		public static DicomTag SegmentationFractionalType = new DicomTag(0x0062, 0x0010);

		/// <summary>(0064,0002) VR=SQ VM=1 Deformable Registration Sequence</summary>
		public static DicomTag DeformableRegistrationSequence = new DicomTag(0x0064, 0x0002);

		/// <summary>(0064,0003) VR=UI VM=1 Source Frame of Reference UID</summary>
		public static DicomTag SourceFrameOfReferenceUID = new DicomTag(0x0064, 0x0003);

		/// <summary>(0064,0005) VR=SQ VM=1 Deformable Registration Grid Sequence</summary>
		public static DicomTag DeformableRegistrationGridSequence = new DicomTag(0x0064, 0x0005);

		/// <summary>(0064,0007) VR=UL VM=3 Grid Dimensions</summary>
		public static DicomTag GridDimensions = new DicomTag(0x0064, 0x0007);

		/// <summary>(0064,0008) VR=FD VM=3 Grid Resolution</summary>
		public static DicomTag GridResolution = new DicomTag(0x0064, 0x0008);

		/// <summary>(0064,0009) VR=OF VM=1 Vector Grid Data</summary>
		public static DicomTag VectorGridData = new DicomTag(0x0064, 0x0009);

		/// <summary>(0064,000f) VR=SQ VM=1 Pre Deformation Matrix Registration Sequence</summary>
		public static DicomTag PreDeformationMatrixRegistrationSequence = new DicomTag(0x0064, 0x000f);

		/// <summary>(0064,0010) VR=SQ VM=1 Post Deformation Matrix Registration Sequence</summary>
		public static DicomTag PostDeformationMatrixRegistrationSequence = new DicomTag(0x0064, 0x0010);

		/// <summary>(0070,0001) VR=SQ VM=1 Graphic Annotation Sequence</summary>
		public static DicomTag GraphicAnnotationSequence = new DicomTag(0x0070, 0x0001);

		/// <summary>(0070,0002) VR=CS VM=1 Graphic Layer</summary>
		public static DicomTag GraphicLayer = new DicomTag(0x0070, 0x0002);

		/// <summary>(0070,0003) VR=CS VM=1 Bounding Box Annotation Units</summary>
		public static DicomTag BoundingBoxAnnotationUnits = new DicomTag(0x0070, 0x0003);

		/// <summary>(0070,0004) VR=CS VM=1 Anchor Point Annotation Units</summary>
		public static DicomTag AnchorPointAnnotationUnits = new DicomTag(0x0070, 0x0004);

		/// <summary>(0070,0005) VR=CS VM=1 Graphic Annotation Units</summary>
		public static DicomTag GraphicAnnotationUnits = new DicomTag(0x0070, 0x0005);

		/// <summary>(0070,0006) VR=ST VM=1 Unformatted Text Value</summary>
		public static DicomTag UnformattedTextValue = new DicomTag(0x0070, 0x0006);

		/// <summary>(0070,0008) VR=SQ VM=1 Text Object Sequence</summary>
		public static DicomTag TextObjectSequence = new DicomTag(0x0070, 0x0008);

		/// <summary>(0070,0009) VR=SQ VM=1 Graphic Object Sequence</summary>
		public static DicomTag GraphicObjectSequence = new DicomTag(0x0070, 0x0009);

		/// <summary>(0070,0010) VR=FL VM=2 Bounding Box Top Left Hand Corner</summary>
		public static DicomTag BoundingBoxTopLeftHandCorner = new DicomTag(0x0070, 0x0010);

		/// <summary>(0070,0011) VR=FL VM=2 Bounding Box Bottom Right Hand Corner</summary>
		public static DicomTag BoundingBoxBottomRightHandCorner = new DicomTag(0x0070, 0x0011);

		/// <summary>(0070,0012) VR=CS VM=1 Bounding Box Text Horizontal Justification</summary>
		public static DicomTag BoundingBoxTextHorizontalJustification = new DicomTag(0x0070, 0x0012);

		/// <summary>(0070,0014) VR=FL VM=2 Anchor Point</summary>
		public static DicomTag AnchorPoint = new DicomTag(0x0070, 0x0014);

		/// <summary>(0070,0015) VR=CS VM=1 Anchor Point Visibility</summary>
		public static DicomTag AnchorPointVisibility = new DicomTag(0x0070, 0x0015);

		/// <summary>(0070,0020) VR=US VM=1 Graphic Dimensions</summary>
		public static DicomTag GraphicDimensions = new DicomTag(0x0070, 0x0020);

		/// <summary>(0070,0021) VR=US VM=1 Number of Graphic Points</summary>
		public static DicomTag NumberOfGraphicPoints = new DicomTag(0x0070, 0x0021);

		/// <summary>(0070,0022) VR=FL VM=2-n Graphic Data</summary>
		public static DicomTag GraphicData = new DicomTag(0x0070, 0x0022);

		/// <summary>(0070,0023) VR=CS VM=1 Graphic Type</summary>
		public static DicomTag GraphicType = new DicomTag(0x0070, 0x0023);

		/// <summary>(0070,0024) VR=CS VM=1 Graphic Filled</summary>
		public static DicomTag GraphicFilled = new DicomTag(0x0070, 0x0024);

		/// <summary>(0070,0040) VR=IS VM=1 Image Rotation (Retired) (Retired)</summary>
		public static DicomTag ImageRotationRetiredRETIRED = new DicomTag(0x0070, 0x0040);

		/// <summary>(0070,0041) VR=CS VM=1 Image Horizontal Flip</summary>
		public static DicomTag ImageHorizontalFlip = new DicomTag(0x0070, 0x0041);

		/// <summary>(0070,0042) VR=US VM=1 Image Rotation</summary>
		public static DicomTag ImageRotation = new DicomTag(0x0070, 0x0042);

		/// <summary>(0070,0050) VR=US VM=2 Displayed Area Top Left Hand Corner (Trial) (Retired)</summary>
		public static DicomTag DisplayedAreaTopLeftHandCornerTrialRETIRED = new DicomTag(0x0070, 0x0050);

		/// <summary>(0070,0051) VR=US VM=2 Displayed Area Bottom Right Hand Corner (Trial) (Retired)</summary>
		public static DicomTag DisplayedAreaBottomRightHandCornerTrialRETIRED = new DicomTag(0x0070, 0x0051);

		/// <summary>(0070,0052) VR=SL VM=2 Displayed Area Top Left Hand Corner</summary>
		public static DicomTag DisplayedAreaTopLeftHandCorner = new DicomTag(0x0070, 0x0052);

		/// <summary>(0070,0053) VR=SL VM=2 Displayed Area Bottom Right Hand Corner</summary>
		public static DicomTag DisplayedAreaBottomRightHandCorner = new DicomTag(0x0070, 0x0053);

		/// <summary>(0070,005a) VR=SQ VM=1 Displayed Area Selection Sequence</summary>
		public static DicomTag DisplayedAreaSelectionSequence = new DicomTag(0x0070, 0x005a);

		/// <summary>(0070,0060) VR=SQ VM=1 Graphic Layer Sequence</summary>
		public static DicomTag GraphicLayerSequence = new DicomTag(0x0070, 0x0060);

		/// <summary>(0070,0062) VR=IS VM=1 Graphic Layer Order</summary>
		public static DicomTag GraphicLayerOrder = new DicomTag(0x0070, 0x0062);

		/// <summary>(0070,0066) VR=US VM=1 Graphic Layer Recommended Display Grayscale Value</summary>
		public static DicomTag GraphicLayerRecommendedDisplayGrayscaleValue = new DicomTag(0x0070, 0x0066);

		/// <summary>(0070,0067) VR=US VM=3 Graphic Layer Recommended Display RGB Value (Retired)</summary>
		public static DicomTag GraphicLayerRecommendedDisplayRGBValueRETIRED = new DicomTag(0x0070, 0x0067);

		/// <summary>(0070,0068) VR=LO VM=1 Graphic Layer Description</summary>
		public static DicomTag GraphicLayerDescription = new DicomTag(0x0070, 0x0068);

		/// <summary>(0070,0080) VR=CS VM=1 Content Label</summary>
		public static DicomTag ContentLabel = new DicomTag(0x0070, 0x0080);

		/// <summary>(0070,0081) VR=LO VM=1 Content Description</summary>
		public static DicomTag ContentDescription = new DicomTag(0x0070, 0x0081);

		/// <summary>(0070,0082) VR=DA VM=1 Presentation Creation Date</summary>
		public static DicomTag PresentationCreationDate = new DicomTag(0x0070, 0x0082);

		/// <summary>(0070,0083) VR=TM VM=1 Presentation Creation Time</summary>
		public static DicomTag PresentationCreationTime = new DicomTag(0x0070, 0x0083);

		/// <summary>(0070,0084) VR=PN VM=1 Content Creator's Name</summary>
		public static DicomTag ContentCreatorsName = new DicomTag(0x0070, 0x0084);

		/// <summary>(0070,0086) VR=SQ VM=1 Content Creator's Identification Code Sequence</summary>
		public static DicomTag ContentCreatorsIdentificationCodeSequence = new DicomTag(0x0070, 0x0086);

		/// <summary>(0070,0100) VR=CS VM=1 Presentation Size Mode</summary>
		public static DicomTag PresentationSizeMode = new DicomTag(0x0070, 0x0100);

		/// <summary>(0070,0101) VR=DS VM=2 Presentation Pixel Spacing</summary>
		public static DicomTag PresentationPixelSpacing = new DicomTag(0x0070, 0x0101);

		/// <summary>(0070,0102) VR=IS VM=2 Presentation Pixel Aspect Ratio</summary>
		public static DicomTag PresentationPixelAspectRatio = new DicomTag(0x0070, 0x0102);

		/// <summary>(0070,0103) VR=FL VM=1 Presentation Pixel Magnification Ratio</summary>
		public static DicomTag PresentationPixelMagnificationRatio = new DicomTag(0x0070, 0x0103);

		/// <summary>(0070,0306) VR=CS VM=1 Shape Type</summary>
		public static DicomTag ShapeType = new DicomTag(0x0070, 0x0306);

		/// <summary>(0070,0308) VR=SQ VM=1 Registration Sequence</summary>
		public static DicomTag RegistrationSequence = new DicomTag(0x0070, 0x0308);

		/// <summary>(0070,0309) VR=SQ VM=1 Matrix Registration Sequence</summary>
		public static DicomTag MatrixRegistrationSequence = new DicomTag(0x0070, 0x0309);

		/// <summary>(0070,030a) VR=SQ VM=1 Matrix Sequence</summary>
		public static DicomTag MatrixSequence = new DicomTag(0x0070, 0x030a);

		/// <summary>(0070,030c) VR=CS VM=1 Frame of Reference Transformation Matrix Type</summary>
		public static DicomTag FrameOfReferenceTransformationMatrixType = new DicomTag(0x0070, 0x030c);

		/// <summary>(0070,030d) VR=SQ VM=1 Registration Type Code Sequence</summary>
		public static DicomTag RegistrationTypeCodeSequence = new DicomTag(0x0070, 0x030d);

		/// <summary>(0070,030f) VR=ST VM=1 Fiducial Description</summary>
		public static DicomTag FiducialDescription = new DicomTag(0x0070, 0x030f);

		/// <summary>(0070,0310) VR=SH VM=1 Fiducial Identifier</summary>
		public static DicomTag FiducialIdentifier = new DicomTag(0x0070, 0x0310);

		/// <summary>(0070,0311) VR=SQ VM=1 Fiducial Identifier Code Sequence</summary>
		public static DicomTag FiducialIdentifierCodeSequence = new DicomTag(0x0070, 0x0311);

		/// <summary>(0070,0312) VR=FD VM=1 Contour Uncertainty Radius</summary>
		public static DicomTag ContourUncertaintyRadius = new DicomTag(0x0070, 0x0312);

		/// <summary>(0070,0314) VR=SQ VM=1 Used Fiducials Sequence</summary>
		public static DicomTag UsedFiducialsSequence = new DicomTag(0x0070, 0x0314);

		/// <summary>(0070,0318) VR=SQ VM=1 Graphic Coordinates Data Sequence</summary>
		public static DicomTag GraphicCoordinatesDataSequence = new DicomTag(0x0070, 0x0318);

		/// <summary>(0070,031a) VR=UI VM=1 Fiducial UID</summary>
		public static DicomTag FiducialUID = new DicomTag(0x0070, 0x031a);

		/// <summary>(0070,031c) VR=SQ VM=1 Fiducial Set Sequence</summary>
		public static DicomTag FiducialSetSequence = new DicomTag(0x0070, 0x031c);

		/// <summary>(0070,031e) VR=SQ VM=1 Fiducial Sequence</summary>
		public static DicomTag FiducialSequence = new DicomTag(0x0070, 0x031e);

		/// <summary>(0070,0401) VR=US VM=3 Graphic Layer Recommended Display CIELab Value</summary>
		public static DicomTag GraphicLayerRecommendedDisplayCIELabValue = new DicomTag(0x0070, 0x0401);

		/// <summary>(0070,0402) VR=SQ VM=1 Blending Sequence</summary>
		public static DicomTag BlendingSequence = new DicomTag(0x0070, 0x0402);

		/// <summary>(0070,0403) VR=FL VM=1 Relative Opacity</summary>
		public static DicomTag RelativeOpacity = new DicomTag(0x0070, 0x0403);

		/// <summary>(0070,0404) VR=SQ VM=1 Referenced Spatial Registration Sequence</summary>
		public static DicomTag ReferencedSpatialRegistrationSequence = new DicomTag(0x0070, 0x0404);

		/// <summary>(0070,0405) VR=CS VM=1 Blending Position</summary>
		public static DicomTag BlendingPosition = new DicomTag(0x0070, 0x0405);

		/// <summary>(0072,0002) VR=SH VM=1 Hanging Protocol Name</summary>
		public static DicomTag HangingProtocolName = new DicomTag(0x0072, 0x0002);

		/// <summary>(0072,0004) VR=LO VM=1 Hanging Protocol Description</summary>
		public static DicomTag HangingProtocolDescription = new DicomTag(0x0072, 0x0004);

		/// <summary>(0072,0006) VR=CS VM=1 Hanging Protocol Level</summary>
		public static DicomTag HangingProtocolLevel = new DicomTag(0x0072, 0x0006);

		/// <summary>(0072,0008) VR=LO VM=1 Hanging Protocol Creator</summary>
		public static DicomTag HangingProtocolCreator = new DicomTag(0x0072, 0x0008);

		/// <summary>(0072,000a) VR=DT VM=1 Hanging Protocol Creation DateTime</summary>
		public static DicomTag HangingProtocolCreationDateTime = new DicomTag(0x0072, 0x000a);

		/// <summary>(0072,000c) VR=SQ VM=1 Hanging Protocol Definition Sequence</summary>
		public static DicomTag HangingProtocolDefinitionSequence = new DicomTag(0x0072, 0x000c);

		/// <summary>(0072,000e) VR=SQ VM=1 Hanging Protocol User Identification Code Sequence</summary>
		public static DicomTag HangingProtocolUserIdentificationCodeSequence = new DicomTag(0x0072, 0x000e);

		/// <summary>(0072,0010) VR=LO VM=1 Hanging Protocol User Group Name</summary>
		public static DicomTag HangingProtocolUserGroupName = new DicomTag(0x0072, 0x0010);

		/// <summary>(0072,0012) VR=SQ VM=1 Source Hanging Protocol Sequence</summary>
		public static DicomTag SourceHangingProtocolSequence = new DicomTag(0x0072, 0x0012);

		/// <summary>(0072,0014) VR=US VM=1 Number of Priors Referenced</summary>
		public static DicomTag NumberOfPriorsReferenced = new DicomTag(0x0072, 0x0014);

		/// <summary>(0072,0020) VR=SQ VM=1 Image Sets Sequence</summary>
		public static DicomTag ImageSetsSequence = new DicomTag(0x0072, 0x0020);

		/// <summary>(0072,0022) VR=SQ VM=1 Image Set Selector Sequence</summary>
		public static DicomTag ImageSetSelectorSequence = new DicomTag(0x0072, 0x0022);

		/// <summary>(0072,0024) VR=CS VM=1 Image Set Selector Usage Flag</summary>
		public static DicomTag ImageSetSelectorUsageFlag = new DicomTag(0x0072, 0x0024);

		/// <summary>(0072,0026) VR=AT VM=1 Selector Attribute</summary>
		public static DicomTag SelectorAttribute = new DicomTag(0x0072, 0x0026);

		/// <summary>(0072,0028) VR=US VM=1 Selector Value Number</summary>
		public static DicomTag SelectorValueNumber = new DicomTag(0x0072, 0x0028);

		/// <summary>(0072,0030) VR=SQ VM=1 Time Based Image Sets Sequence</summary>
		public static DicomTag TimeBasedImageSetsSequence = new DicomTag(0x0072, 0x0030);

		/// <summary>(0072,0032) VR=US VM=1 Image Set Number</summary>
		public static DicomTag ImageSetNumber = new DicomTag(0x0072, 0x0032);

		/// <summary>(0072,0034) VR=CS VM=1 Image Set Selector Category</summary>
		public static DicomTag ImageSetSelectorCategory = new DicomTag(0x0072, 0x0034);

		/// <summary>(0072,0038) VR=US VM=2 Relative Time</summary>
		public static DicomTag RelativeTime = new DicomTag(0x0072, 0x0038);

		/// <summary>(0072,003a) VR=CS VM=1 Relative Time Units</summary>
		public static DicomTag RelativeTimeUnits = new DicomTag(0x0072, 0x003a);

		/// <summary>(0072,003c) VR=SS VM=2 Abstract Prior Value</summary>
		public static DicomTag AbstractPriorValue = new DicomTag(0x0072, 0x003c);

		/// <summary>(0072,003e) VR=SQ VM=1 Abstract Prior Code Sequence</summary>
		public static DicomTag AbstractPriorCodeSequence = new DicomTag(0x0072, 0x003e);

		/// <summary>(0072,0040) VR=LO VM=1 Image Set Label</summary>
		public static DicomTag ImageSetLabel = new DicomTag(0x0072, 0x0040);

		/// <summary>(0072,0050) VR=CS VM=1 Selector Attribute VR</summary>
		public static DicomTag SelectorAttributeVR = new DicomTag(0x0072, 0x0050);

		/// <summary>(0072,0052) VR=AT VM=1 Selector Sequence Pointer</summary>
		public static DicomTag SelectorSequencePointer = new DicomTag(0x0072, 0x0052);

		/// <summary>(0072,0054) VR=LO VM=1 Selector Sequence Pointer Private Creator</summary>
		public static DicomTag SelectorSequencePointerPrivateCreator = new DicomTag(0x0072, 0x0054);

		/// <summary>(0072,0056) VR=LO VM=1 Selector Attribute Private Creator</summary>
		public static DicomTag SelectorAttributePrivateCreator = new DicomTag(0x0072, 0x0056);

		/// <summary>(0072,0060) VR=AT VM=1-n Selector AT Value</summary>
		public static DicomTag SelectorATValue = new DicomTag(0x0072, 0x0060);

		/// <summary>(0072,0062) VR=CS VM=1-n Selector CS Value</summary>
		public static DicomTag SelectorCSValue = new DicomTag(0x0072, 0x0062);

		/// <summary>(0072,0064) VR=IS VM=1-n Selector IS Value</summary>
		public static DicomTag SelectorISValue = new DicomTag(0x0072, 0x0064);

		/// <summary>(0072,0066) VR=LO VM=1-n Selector LO Value</summary>
		public static DicomTag SelectorLOValue = new DicomTag(0x0072, 0x0066);

		/// <summary>(0072,0068) VR=LT VM=1 Selector LT Value</summary>
		public static DicomTag SelectorLTValue = new DicomTag(0x0072, 0x0068);

		/// <summary>(0072,006a) VR=PN VM=1-n Selector PN Value</summary>
		public static DicomTag SelectorPNValue = new DicomTag(0x0072, 0x006a);

		/// <summary>(0072,006c) VR=SH VM=1-n Selector SH Value</summary>
		public static DicomTag SelectorSHValue = new DicomTag(0x0072, 0x006c);

		/// <summary>(0072,006e) VR=ST VM=1 Selector ST Value</summary>
		public static DicomTag SelectorSTValue = new DicomTag(0x0072, 0x006e);

		/// <summary>(0072,0070) VR=UT VM=1 Selector UT Value</summary>
		public static DicomTag SelectorUTValue = new DicomTag(0x0072, 0x0070);

		/// <summary>(0072,0072) VR=DS VM=1-n Selector DS Value</summary>
		public static DicomTag SelectorDSValue = new DicomTag(0x0072, 0x0072);

		/// <summary>(0072,0074) VR=FD VM=1-n Selector FD Value</summary>
		public static DicomTag SelectorFDValue = new DicomTag(0x0072, 0x0074);

		/// <summary>(0072,0076) VR=FL VM=1-n Selector FL Value</summary>
		public static DicomTag SelectorFLValue = new DicomTag(0x0072, 0x0076);

		/// <summary>(0072,0078) VR=UL VM=1-n Selector UL Value</summary>
		public static DicomTag SelectorULValue = new DicomTag(0x0072, 0x0078);

		/// <summary>(0072,007a) VR=US VM=1-n Selector US Value</summary>
		public static DicomTag SelectorUSValue = new DicomTag(0x0072, 0x007a);

		/// <summary>(0072,007c) VR=SL VM=1-n Selector SL Value</summary>
		public static DicomTag SelectorSLValue = new DicomTag(0x0072, 0x007c);

		/// <summary>(0072,007e) VR=SS VM=1-n Selector SS Value</summary>
		public static DicomTag SelectorSSValue = new DicomTag(0x0072, 0x007e);

		/// <summary>(0072,0080) VR=SQ VM=1 Selector Code Sequence Value</summary>
		public static DicomTag SelectorCodeSequenceValue = new DicomTag(0x0072, 0x0080);

		/// <summary>(0072,0100) VR=US VM=1 Number of Screens</summary>
		public static DicomTag NumberOfScreens = new DicomTag(0x0072, 0x0100);

		/// <summary>(0072,0102) VR=SQ VM=1 Nominal Screen Definition Sequence</summary>
		public static DicomTag NominalScreenDefinitionSequence = new DicomTag(0x0072, 0x0102);

		/// <summary>(0072,0104) VR=US VM=1 Number of Vertical Pixels</summary>
		public static DicomTag NumberOfVerticalPixels = new DicomTag(0x0072, 0x0104);

		/// <summary>(0072,0106) VR=US VM=1 Number of Horizontal Pixels</summary>
		public static DicomTag NumberOfHorizontalPixels = new DicomTag(0x0072, 0x0106);

		/// <summary>(0072,0108) VR=FD VM=4 Display Environment Spatial Position</summary>
		public static DicomTag DisplayEnvironmentSpatialPosition = new DicomTag(0x0072, 0x0108);

		/// <summary>(0072,010a) VR=US VM=1 Screen Minimum Grayscale Bit Depth</summary>
		public static DicomTag ScreenMinimumGrayscaleBitDepth = new DicomTag(0x0072, 0x010a);

		/// <summary>(0072,010c) VR=US VM=1 Screen Minimum Color Bit Depth</summary>
		public static DicomTag ScreenMinimumColorBitDepth = new DicomTag(0x0072, 0x010c);

		/// <summary>(0072,010e) VR=US VM=1 Application Maximum Repaint Time</summary>
		public static DicomTag ApplicationMaximumRepaintTime = new DicomTag(0x0072, 0x010e);

		/// <summary>(0072,0200) VR=SQ VM=1 Display Sets Sequence</summary>
		public static DicomTag DisplaySetsSequence = new DicomTag(0x0072, 0x0200);

		/// <summary>(0072,0202) VR=US VM=1 Display Set Number</summary>
		public static DicomTag DisplaySetNumber = new DicomTag(0x0072, 0x0202);

		/// <summary>(0072,0203) VR=LO VM=1 Display Set Label</summary>
		public static DicomTag DisplaySetLabel = new DicomTag(0x0072, 0x0203);

		/// <summary>(0072,0204) VR=US VM=1 Display Set Presentation Group</summary>
		public static DicomTag DisplaySetPresentationGroup = new DicomTag(0x0072, 0x0204);

		/// <summary>(0072,0206) VR=LO VM=1 Display Set Presentation Group Description</summary>
		public static DicomTag DisplaySetPresentationGroupDescription = new DicomTag(0x0072, 0x0206);

		/// <summary>(0072,0208) VR=CS VM=1 Partial Data Display Handling</summary>
		public static DicomTag PartialDataDisplayHandling = new DicomTag(0x0072, 0x0208);

		/// <summary>(0072,0210) VR=SQ VM=1 Synchronized Scrolling Sequence</summary>
		public static DicomTag SynchronizedScrollingSequence = new DicomTag(0x0072, 0x0210);

		/// <summary>(0072,0212) VR=US VM=2-n Display Set Scrolling Group</summary>
		public static DicomTag DisplaySetScrollingGroup = new DicomTag(0x0072, 0x0212);

		/// <summary>(0072,0214) VR=SQ VM=1 Navigation Indicator Sequence</summary>
		public static DicomTag NavigationIndicatorSequence = new DicomTag(0x0072, 0x0214);

		/// <summary>(0072,0216) VR=US VM=1 Navigation Display Set</summary>
		public static DicomTag NavigationDisplaySet = new DicomTag(0x0072, 0x0216);

		/// <summary>(0072,0218) VR=US VM=1-n Reference Display Sets</summary>
		public static DicomTag ReferenceDisplaySets = new DicomTag(0x0072, 0x0218);

		/// <summary>(0072,0300) VR=SQ VM=1 Image Boxes Sequence</summary>
		public static DicomTag ImageBoxesSequence = new DicomTag(0x0072, 0x0300);

		/// <summary>(0072,0302) VR=US VM=1 Image Box Number</summary>
		public static DicomTag ImageBoxNumber = new DicomTag(0x0072, 0x0302);

		/// <summary>(0072,0304) VR=CS VM=1 Image Box Layout Type</summary>
		public static DicomTag ImageBoxLayoutType = new DicomTag(0x0072, 0x0304);

		/// <summary>(0072,0306) VR=US VM=1 Image Box Tile Horizontal Dimension</summary>
		public static DicomTag ImageBoxTileHorizontalDimension = new DicomTag(0x0072, 0x0306);

		/// <summary>(0072,0308) VR=US VM=1 Image Box Tile Vertical Dimension</summary>
		public static DicomTag ImageBoxTileVerticalDimension = new DicomTag(0x0072, 0x0308);

		/// <summary>(0072,0310) VR=CS VM=1 Image Box Scroll Direction</summary>
		public static DicomTag ImageBoxScrollDirection = new DicomTag(0x0072, 0x0310);

		/// <summary>(0072,0312) VR=CS VM=1 Image Box Small Scroll Type</summary>
		public static DicomTag ImageBoxSmallScrollType = new DicomTag(0x0072, 0x0312);

		/// <summary>(0072,0314) VR=US VM=1 Image Box Small Scroll Amount</summary>
		public static DicomTag ImageBoxSmallScrollAmount = new DicomTag(0x0072, 0x0314);

		/// <summary>(0072,0316) VR=CS VM=1 Image Box Large Scroll Type</summary>
		public static DicomTag ImageBoxLargeScrollType = new DicomTag(0x0072, 0x0316);

		/// <summary>(0072,0318) VR=US VM=1 Image Box Large Scroll Amount</summary>
		public static DicomTag ImageBoxLargeScrollAmount = new DicomTag(0x0072, 0x0318);

		/// <summary>(0072,0320) VR=US VM=1 Image Box Overlap Priority</summary>
		public static DicomTag ImageBoxOverlapPriority = new DicomTag(0x0072, 0x0320);

		/// <summary>(0072,0330) VR=FD VM=1 Cine Relative to Real-Time</summary>
		public static DicomTag CineRelativeToRealTime = new DicomTag(0x0072, 0x0330);

		/// <summary>(0072,0400) VR=SQ VM=1 Filter Operations Sequence</summary>
		public static DicomTag FilterOperationsSequence = new DicomTag(0x0072, 0x0400);

		/// <summary>(0072,0402) VR=CS VM=1 Filter-by Category</summary>
		public static DicomTag FilterbyCategory = new DicomTag(0x0072, 0x0402);

		/// <summary>(0072,0404) VR=CS VM=1 Filter-by Attribute Presence</summary>
		public static DicomTag FilterbyAttributePresence = new DicomTag(0x0072, 0x0404);

		/// <summary>(0072,0406) VR=CS VM=1 Filter-by Operator</summary>
		public static DicomTag FilterbyOperator = new DicomTag(0x0072, 0x0406);

		/// <summary>(0072,0500) VR=CS VM=1 Blending Operation Type</summary>
		public static DicomTag BlendingOperationType = new DicomTag(0x0072, 0x0500);

		/// <summary>(0072,0510) VR=CS VM=1 Reformatting Operation Type</summary>
		public static DicomTag ReformattingOperationType = new DicomTag(0x0072, 0x0510);

		/// <summary>(0072,0512) VR=FD VM=1 Reformatting Thickness</summary>
		public static DicomTag ReformattingThickness = new DicomTag(0x0072, 0x0512);

		/// <summary>(0072,0514) VR=FD VM=1 Reformatting Interval</summary>
		public static DicomTag ReformattingInterval = new DicomTag(0x0072, 0x0514);

		/// <summary>(0072,0516) VR=CS VM=1 Reformatting Operation Initial View Direction</summary>
		public static DicomTag ReformattingOperationInitialViewDirection = new DicomTag(0x0072, 0x0516);

		/// <summary>(0072,0520) VR=CS VM=1-n 3D Rendering Type</summary>
		public static DicomTag RenderingType3D = new DicomTag(0x0072, 0x0520);

		/// <summary>(0072,0600) VR=SQ VM=1 Sorting Operations Sequence</summary>
		public static DicomTag SortingOperationsSequence = new DicomTag(0x0072, 0x0600);

		/// <summary>(0072,0602) VR=CS VM=1 Sort-by Category</summary>
		public static DicomTag SortbyCategory = new DicomTag(0x0072, 0x0602);

		/// <summary>(0072,0604) VR=CS VM=1 Sorting Direction</summary>
		public static DicomTag SortingDirection = new DicomTag(0x0072, 0x0604);

		/// <summary>(0072,0700) VR=CS VM=2 Display Set Patient Orientation</summary>
		public static DicomTag DisplaySetPatientOrientation = new DicomTag(0x0072, 0x0700);

		/// <summary>(0072,0702) VR=CS VM=1 VOI Type</summary>
		public static DicomTag VOIType = new DicomTag(0x0072, 0x0702);

		/// <summary>(0072,0704) VR=CS VM=1 Pseudo-color Type</summary>
		public static DicomTag PseudocolorType = new DicomTag(0x0072, 0x0704);

		/// <summary>(0072,0706) VR=CS VM=1 Show Grayscale Inverted</summary>
		public static DicomTag ShowGrayscaleInverted = new DicomTag(0x0072, 0x0706);

		/// <summary>(0072,0710) VR=CS VM=1 Show Image True Size Flag</summary>
		public static DicomTag ShowImageTrueSizeFlag = new DicomTag(0x0072, 0x0710);

		/// <summary>(0072,0712) VR=CS VM=1 Show Graphic Annotation Flag</summary>
		public static DicomTag ShowGraphicAnnotationFlag = new DicomTag(0x0072, 0x0712);

		/// <summary>(0072,0714) VR=CS VM=1 Show Patient Demographics Flag</summary>
		public static DicomTag ShowPatientDemographicsFlag = new DicomTag(0x0072, 0x0714);

		/// <summary>(0072,0716) VR=CS VM=1 Show Acquisition Techniques Flag</summary>
		public static DicomTag ShowAcquisitionTechniquesFlag = new DicomTag(0x0072, 0x0716);

		/// <summary>(0072,0717) VR=CS VM=1 Display Set Horizontal Justification</summary>
		public static DicomTag DisplaySetHorizontalJustification = new DicomTag(0x0072, 0x0717);

		/// <summary>(0072,0718) VR=CS VM=1 Display Set Vertical Justification</summary>
		public static DicomTag DisplaySetVerticalJustification = new DicomTag(0x0072, 0x0718);

		/// <summary>(0074,1000) VR=CS VM=1 Unified Procedure Step State</summary>
		public static DicomTag UnifiedProcedureStepState = new DicomTag(0x0074, 0x1000);

		/// <summary>(0074,1002) VR=SQ VM=1 UPS Progress Information Sequence</summary>
		public static DicomTag UPSProgressInformationSequence = new DicomTag(0x0074, 0x1002);

		/// <summary>(0074,1004) VR=DS VM=1 Unified Procedure Step Progress</summary>
		public static DicomTag UnifiedProcedureStepProgress = new DicomTag(0x0074, 0x1004);

		/// <summary>(0074,1006) VR=ST VM=1 Unified Procedure Step Progress Description</summary>
		public static DicomTag UnifiedProcedureStepProgressDescription = new DicomTag(0x0074, 0x1006);

		/// <summary>(0074,1008) VR=SQ VM=1 Unified Procedure Step Communications URI Sequence</summary>
		public static DicomTag UnifiedProcedureStepCommunicationsURISequence = new DicomTag(0x0074, 0x1008);

		/// <summary>(0074,100a) VR=ST VM=1 Contact URI</summary>
		public static DicomTag ContactURI = new DicomTag(0x0074, 0x100a);

		/// <summary>(0074,100c) VR=LO VM=1 Contact Display Name</summary>
		public static DicomTag ContactDisplayName = new DicomTag(0x0074, 0x100c);

		/// <summary>(0074,100e) VR=SQ VM=1 Unified Procedure Step Discontinuation Reason Code Sequence</summary>
		public static DicomTag UnifiedProcedureStepDiscontinuationReasonCodeSequence = new DicomTag(0x0074, 0x100e);

		/// <summary>(0074,1020) VR=SQ VM=1 Beam Task Sequence</summary>
		public static DicomTag BeamTaskSequence = new DicomTag(0x0074, 0x1020);

		/// <summary>(0074,1022) VR=CS VM=1 Beam Task Type</summary>
		public static DicomTag BeamTaskType = new DicomTag(0x0074, 0x1022);

		/// <summary>(0074,1024) VR=IS VM=1 Beam Order Index</summary>
		public static DicomTag BeamOrderIndex = new DicomTag(0x0074, 0x1024);

		/// <summary>(0074,1030) VR=SQ VM=1 Delivery Verification Image Sequence</summary>
		public static DicomTag DeliveryVerificationImageSequence = new DicomTag(0x0074, 0x1030);

		/// <summary>(0074,1032) VR=CS VM=1 Verification Image Timing</summary>
		public static DicomTag VerificationImageTiming = new DicomTag(0x0074, 0x1032);

		/// <summary>(0074,1034) VR=CS VM=1 Double Exposure Flag</summary>
		public static DicomTag DoubleExposureFlag = new DicomTag(0x0074, 0x1034);

		/// <summary>(0074,1036) VR=CS VM=1 Double Exposure Ordering</summary>
		public static DicomTag DoubleExposureOrdering = new DicomTag(0x0074, 0x1036);

		/// <summary>(0074,1038) VR=DS VM=1 Double Exposure Meterset</summary>
		public static DicomTag DoubleExposureMeterset = new DicomTag(0x0074, 0x1038);

		/// <summary>(0074,103a) VR=DS VM=4 Double Exposure Field Delta</summary>
		public static DicomTag DoubleExposureFieldDelta = new DicomTag(0x0074, 0x103a);

		/// <summary>(0074,1040) VR=SQ VM=1 Related Reference RT Image Sequence</summary>
		public static DicomTag RelatedReferenceRTImageSequence = new DicomTag(0x0074, 0x1040);

		/// <summary>(0074,1042) VR=SQ VM=1 General Machine Verification Sequence</summary>
		public static DicomTag GeneralMachineVerificationSequence = new DicomTag(0x0074, 0x1042);

		/// <summary>(0074,1044) VR=SQ VM=1 Conventional Machine Verification Sequence</summary>
		public static DicomTag ConventionalMachineVerificationSequence = new DicomTag(0x0074, 0x1044);

		/// <summary>(0074,1046) VR=SQ VM=1 Ion Machine Verification Sequence</summary>
		public static DicomTag IonMachineVerificationSequence = new DicomTag(0x0074, 0x1046);

		/// <summary>(0074,1048) VR=SQ VM=1 Failed Attributes Sequence</summary>
		public static DicomTag FailedAttributesSequence = new DicomTag(0x0074, 0x1048);

		/// <summary>(0074,104a) VR=SQ VM=1 Overridden Attributes Sequence</summary>
		public static DicomTag OverriddenAttributesSequence = new DicomTag(0x0074, 0x104a);

		/// <summary>(0074,104c) VR=SQ VM=1 Conventional Control Point Verification Sequence</summary>
		public static DicomTag ConventionalControlPointVerificationSequence = new DicomTag(0x0074, 0x104c);

		/// <summary>(0074,104e) VR=SQ VM=1 Ion Control Point Verification Sequence</summary>
		public static DicomTag IonControlPointVerificationSequence = new DicomTag(0x0074, 0x104e);

		/// <summary>(0074,1050) VR=SQ VM=1 Attribute Occurrence Sequence</summary>
		public static DicomTag AttributeOccurrenceSequence = new DicomTag(0x0074, 0x1050);

		/// <summary>(0074,1052) VR=AT VM=1 Attribute Occurrence Pointer</summary>
		public static DicomTag AttributeOccurrencePointer = new DicomTag(0x0074, 0x1052);

		/// <summary>(0074,1054) VR=UL VM=1 Attribute Item Selector</summary>
		public static DicomTag AttributeItemSelector = new DicomTag(0x0074, 0x1054);

		/// <summary>(0074,1056) VR=LO VM=1 Attribute Occurrence Private Creator</summary>
		public static DicomTag AttributeOccurrencePrivateCreator = new DicomTag(0x0074, 0x1056);

		/// <summary>(0074,1200) VR=CS VM=1 Scheduled Procedure Step Priority</summary>
		public static DicomTag ScheduledProcedureStepPriority = new DicomTag(0x0074, 0x1200);

		/// <summary>(0074,1202) VR=LO VM=1 Worklist Label</summary>
		public static DicomTag WorklistLabel = new DicomTag(0x0074, 0x1202);

		/// <summary>(0074,1204) VR=LO VM=1 Procedure Step Label</summary>
		public static DicomTag ProcedureStepLabel = new DicomTag(0x0074, 0x1204);

		/// <summary>(0074,1210) VR=SQ VM=1 Scheduled Processing Parameters Sequence</summary>
		public static DicomTag ScheduledProcessingParametersSequence = new DicomTag(0x0074, 0x1210);

		/// <summary>(0074,1212) VR=SQ VM=1 Performed Processing Parameters Sequence</summary>
		public static DicomTag PerformedProcessingParametersSequence = new DicomTag(0x0074, 0x1212);

		/// <summary>(0074,1216) VR=SQ VM=1 UPS Performed Procedure Sequence</summary>
		public static DicomTag UPSPerformedProcedureSequence = new DicomTag(0x0074, 0x1216);

		/// <summary>(0074,1220) VR=SQ VM=1 Related Procedure Step Sequence</summary>
		public static DicomTag RelatedProcedureStepSequence = new DicomTag(0x0074, 0x1220);

		/// <summary>(0074,1222) VR=LO VM=1 Procedure Step Relationship Type</summary>
		public static DicomTag ProcedureStepRelationshipType = new DicomTag(0x0074, 0x1222);

		/// <summary>(0074,1230) VR=LO VM=1 Deletion Lock</summary>
		public static DicomTag DeletionLock = new DicomTag(0x0074, 0x1230);

		/// <summary>(0074,1234) VR=AE VM=1 Receiving AE</summary>
		public static DicomTag ReceivingAE = new DicomTag(0x0074, 0x1234);

		/// <summary>(0074,1236) VR=AE VM=1 Requesting AE</summary>
		public static DicomTag RequestingAE = new DicomTag(0x0074, 0x1236);

		/// <summary>(0074,1238) VR=LT VM=1 Reason for Cancellation</summary>
		public static DicomTag ReasonForCancellation = new DicomTag(0x0074, 0x1238);

		/// <summary>(0074,1242) VR=CS VM=1 SCP Status</summary>
		public static DicomTag SCPStatus = new DicomTag(0x0074, 0x1242);

		/// <summary>(0074,1244) VR=CS VM=1 Subscription List Status</summary>
		public static DicomTag SubscriptionListStatus = new DicomTag(0x0074, 0x1244);

		/// <summary>(0074,1246) VR=CS VM=1 UPS List Status</summary>
		public static DicomTag UPSListStatus = new DicomTag(0x0074, 0x1246);

		/// <summary>(0088,0130) VR=SH VM=1 Storage Media File-set ID</summary>
		public static DicomTag StorageMediaFilesetID = new DicomTag(0x0088, 0x0130);

		/// <summary>(0088,0140) VR=UI VM=1 Storage Media File-set UID</summary>
		public static DicomTag StorageMediaFilesetUID = new DicomTag(0x0088, 0x0140);

		/// <summary>(0088,0200) VR=SQ VM=1 Icon Image Sequence</summary>
		public static DicomTag IconImageSequence = new DicomTag(0x0088, 0x0200);

		/// <summary>(0088,0904) VR=LO VM=1 Topic Title (Retired)</summary>
		public static DicomTag TopicTitleRETIRED = new DicomTag(0x0088, 0x0904);

		/// <summary>(0088,0906) VR=ST VM=1 Topic Subject (Retired)</summary>
		public static DicomTag TopicSubjectRETIRED = new DicomTag(0x0088, 0x0906);

		/// <summary>(0088,0910) VR=LO VM=1 Topic Author (Retired)</summary>
		public static DicomTag TopicAuthorRETIRED = new DicomTag(0x0088, 0x0910);

		/// <summary>(0088,0912) VR=LO VM=1-32 Topic Keywords (Retired)</summary>
		public static DicomTag TopicKeywordsRETIRED = new DicomTag(0x0088, 0x0912);

		/// <summary>(0100,0410) VR=CS VM=1 SOP Instance Status</summary>
		public static DicomTag SOPInstanceStatus = new DicomTag(0x0100, 0x0410);

		/// <summary>(0100,0420) VR=DT VM=1 SOP Authorization Date and Time</summary>
		public static DicomTag SOPAuthorizationDateAndTime = new DicomTag(0x0100, 0x0420);

		/// <summary>(0100,0424) VR=LT VM=1 SOP Authorization Comment</summary>
		public static DicomTag SOPAuthorizationComment = new DicomTag(0x0100, 0x0424);

		/// <summary>(0100,0426) VR=LO VM=1 Authorization Equipment Certification Number</summary>
		public static DicomTag AuthorizationEquipmentCertificationNumber = new DicomTag(0x0100, 0x0426);

		/// <summary>(0400,0005) VR=US VM=1 MAC ID Number</summary>
		public static DicomTag MACIDNumber = new DicomTag(0x0400, 0x0005);

		/// <summary>(0400,0010) VR=UI VM=1 MAC Calculation Transfer Syntax UID</summary>
		public static DicomTag MACCalculationTransferSyntaxUID = new DicomTag(0x0400, 0x0010);

		/// <summary>(0400,0015) VR=CS VM=1 MAC Algorithm</summary>
		public static DicomTag MACAlgorithm = new DicomTag(0x0400, 0x0015);

		/// <summary>(0400,0020) VR=AT VM=1-n Data Elements Signed</summary>
		public static DicomTag DataElementsSigned = new DicomTag(0x0400, 0x0020);

		/// <summary>(0400,0100) VR=UI VM=1 Digital Signature UID</summary>
		public static DicomTag DigitalSignatureUID = new DicomTag(0x0400, 0x0100);

		/// <summary>(0400,0105) VR=DT VM=1 Digital Signature DateTime</summary>
		public static DicomTag DigitalSignatureDateTime = new DicomTag(0x0400, 0x0105);

		/// <summary>(0400,0110) VR=CS VM=1 Certificate Type</summary>
		public static DicomTag CertificateType = new DicomTag(0x0400, 0x0110);

		/// <summary>(0400,0115) VR=OB VM=1 Certificate of Signer</summary>
		public static DicomTag CertificateOfSigner = new DicomTag(0x0400, 0x0115);

		/// <summary>(0400,0120) VR=OB VM=1 Signature</summary>
		public static DicomTag Signature = new DicomTag(0x0400, 0x0120);

		/// <summary>(0400,0305) VR=CS VM=1 Certified Timestamp Type</summary>
		public static DicomTag CertifiedTimestampType = new DicomTag(0x0400, 0x0305);

		/// <summary>(0400,0310) VR=OB VM=1 Certified Timestamp</summary>
		public static DicomTag CertifiedTimestamp = new DicomTag(0x0400, 0x0310);

		/// <summary>(0400,0401) VR=SQ VM=1 Digital Signature Purpose Code Sequence</summary>
		public static DicomTag DigitalSignaturePurposeCodeSequence = new DicomTag(0x0400, 0x0401);

		/// <summary>(0400,0402) VR=SQ VM=1 Referenced Digital Signature Sequence</summary>
		public static DicomTag ReferencedDigitalSignatureSequence = new DicomTag(0x0400, 0x0402);

		/// <summary>(0400,0403) VR=SQ VM=1 Referenced SOP Instance MAC Sequence</summary>
		public static DicomTag ReferencedSOPInstanceMACSequence = new DicomTag(0x0400, 0x0403);

		/// <summary>(0400,0404) VR=OB VM=1 MAC</summary>
		public static DicomTag MAC = new DicomTag(0x0400, 0x0404);

		/// <summary>(0400,0500) VR=SQ VM=1 Encrypted Attributes Sequence</summary>
		public static DicomTag EncryptedAttributesSequence = new DicomTag(0x0400, 0x0500);

		/// <summary>(0400,0510) VR=UI VM=1 Encrypted Content Transfer Syntax UID</summary>
		public static DicomTag EncryptedContentTransferSyntaxUID = new DicomTag(0x0400, 0x0510);

		/// <summary>(0400,0520) VR=OB VM=1 Encrypted Content</summary>
		public static DicomTag EncryptedContent = new DicomTag(0x0400, 0x0520);

		/// <summary>(0400,0550) VR=SQ VM=1 Modified Attributes Sequence</summary>
		public static DicomTag ModifiedAttributesSequence = new DicomTag(0x0400, 0x0550);

		/// <summary>(0400,0561) VR=SQ VM=1 Original Attributes Sequence</summary>
		public static DicomTag OriginalAttributesSequence = new DicomTag(0x0400, 0x0561);

		/// <summary>(0400,0562) VR=DT VM=1 Attribute Modification DateTime</summary>
		public static DicomTag AttributeModificationDateTime = new DicomTag(0x0400, 0x0562);

		/// <summary>(0400,0563) VR=LO VM=1 Modifying System</summary>
		public static DicomTag ModifyingSystem = new DicomTag(0x0400, 0x0563);

		/// <summary>(0400,0564) VR=LO VM=1 Source of Previous Values</summary>
		public static DicomTag SourceOfPreviousValues = new DicomTag(0x0400, 0x0564);

		/// <summary>(0400,0565) VR=CS VM=1 Reason for the Attribute Modification</summary>
		public static DicomTag ReasonForTheAttributeModification = new DicomTag(0x0400, 0x0565);

		/// <summary>(1000,0000) VR=US VM=3 Escape Triplet (Retired)</summary>
		public static DicomTag EscapeTripletRETIRED = new DicomTag(0x1000, 0x0000);

		/// <summary>(1000,0001) VR=US VM=3 Run Length Triplet (Retired)</summary>
		public static DicomTag RunLengthTripletRETIRED = new DicomTag(0x1000, 0x0001);

		/// <summary>(1000,0002) VR=US VM=1 Huffman Table Size (Retired)</summary>
		public static DicomTag HuffmanTableSizeRETIRED = new DicomTag(0x1000, 0x0002);

		/// <summary>(1000,0003) VR=US VM=3 Huffman Table Triplet (Retired)</summary>
		public static DicomTag HuffmanTableTripletRETIRED = new DicomTag(0x1000, 0x0003);

		/// <summary>(1000,0004) VR=US VM=1 Shift Table Size (Retired)</summary>
		public static DicomTag ShiftTableSizeRETIRED = new DicomTag(0x1000, 0x0004);

		/// <summary>(1000,0005) VR=US VM=3 Shift Table Triplet (Retired)</summary>
		public static DicomTag ShiftTableTripletRETIRED = new DicomTag(0x1000, 0x0005);

		/// <summary>(1010,0000) VR=US VM=1-n Zonal Map (Retired)</summary>
		public static DicomTag ZonalMapRETIRED = new DicomTag(0x1010, 0x0000);

		/// <summary>(2000,0010) VR=IS VM=1 Number of Copies</summary>
		public static DicomTag NumberOfCopies = new DicomTag(0x2000, 0x0010);

		/// <summary>(2000,001e) VR=SQ VM=1 Printer Configuration Sequence</summary>
		public static DicomTag PrinterConfigurationSequence = new DicomTag(0x2000, 0x001e);

		/// <summary>(2000,0020) VR=CS VM=1 Print Priority</summary>
		public static DicomTag PrintPriority = new DicomTag(0x2000, 0x0020);

		/// <summary>(2000,0030) VR=CS VM=1 Medium Type</summary>
		public static DicomTag MediumType = new DicomTag(0x2000, 0x0030);

		/// <summary>(2000,0040) VR=CS VM=1 Film Destination</summary>
		public static DicomTag FilmDestination = new DicomTag(0x2000, 0x0040);

		/// <summary>(2000,0050) VR=LO VM=1 Film Session Label</summary>
		public static DicomTag FilmSessionLabel = new DicomTag(0x2000, 0x0050);

		/// <summary>(2000,0060) VR=IS VM=1 Memory Allocation</summary>
		public static DicomTag MemoryAllocation = new DicomTag(0x2000, 0x0060);

		/// <summary>(2000,0061) VR=IS VM=1 Maximum Memory Allocation</summary>
		public static DicomTag MaximumMemoryAllocation = new DicomTag(0x2000, 0x0061);

		/// <summary>(2000,0062) VR=CS VM=1 Color Image Printing Flag (Retired)</summary>
		public static DicomTag ColorImagePrintingFlagRETIRED = new DicomTag(0x2000, 0x0062);

		/// <summary>(2000,0063) VR=CS VM=1 Collation Flag (Retired)</summary>
		public static DicomTag CollationFlagRETIRED = new DicomTag(0x2000, 0x0063);

		/// <summary>(2000,0065) VR=CS VM=1 Annotation Flag (Retired)</summary>
		public static DicomTag AnnotationFlagRETIRED = new DicomTag(0x2000, 0x0065);

		/// <summary>(2000,0067) VR=CS VM=1 Image Overlay Flag (Retired)</summary>
		public static DicomTag ImageOverlayFlagRETIRED = new DicomTag(0x2000, 0x0067);

		/// <summary>(2000,0069) VR=CS VM=1 Presentation LUT Flag (Retired)</summary>
		public static DicomTag PresentationLUTFlagRETIRED = new DicomTag(0x2000, 0x0069);

		/// <summary>(2000,006a) VR=CS VM=1 Image Box Presentation LUT Flag (Retired)</summary>
		public static DicomTag ImageBoxPresentationLUTFlagRETIRED = new DicomTag(0x2000, 0x006a);

		/// <summary>(2000,00a0) VR=US VM=1 Memory Bit Depth</summary>
		public static DicomTag MemoryBitDepth = new DicomTag(0x2000, 0x00a0);

		/// <summary>(2000,00a1) VR=US VM=1 Printing Bit Depth</summary>
		public static DicomTag PrintingBitDepth = new DicomTag(0x2000, 0x00a1);

		/// <summary>(2000,00a2) VR=SQ VM=1 Media Installed Sequence</summary>
		public static DicomTag MediaInstalledSequence = new DicomTag(0x2000, 0x00a2);

		/// <summary>(2000,00a4) VR=SQ VM=1 Other Media Available Sequence</summary>
		public static DicomTag OtherMediaAvailableSequence = new DicomTag(0x2000, 0x00a4);

		/// <summary>(2000,00a8) VR=SQ VM=1 Supported Image Display Formats Sequence</summary>
		public static DicomTag SupportedImageDisplayFormatsSequence = new DicomTag(0x2000, 0x00a8);

		/// <summary>(2000,0500) VR=SQ VM=1 Referenced Film Box Sequence</summary>
		public static DicomTag ReferencedFilmBoxSequence = new DicomTag(0x2000, 0x0500);

		/// <summary>(2000,0510) VR=SQ VM=1 Referenced Stored Print Sequence (Retired)</summary>
		public static DicomTag ReferencedStoredPrintSequenceRETIRED = new DicomTag(0x2000, 0x0510);

		/// <summary>(2010,0010) VR=ST VM=1 Image Display Format</summary>
		public static DicomTag ImageDisplayFormat = new DicomTag(0x2010, 0x0010);

		/// <summary>(2010,0030) VR=CS VM=1 Annotation Display Format ID</summary>
		public static DicomTag AnnotationDisplayFormatID = new DicomTag(0x2010, 0x0030);

		/// <summary>(2010,0040) VR=CS VM=1 Film Orientation</summary>
		public static DicomTag FilmOrientation = new DicomTag(0x2010, 0x0040);

		/// <summary>(2010,0050) VR=CS VM=1 Film Size ID</summary>
		public static DicomTag FilmSizeID = new DicomTag(0x2010, 0x0050);

		/// <summary>(2010,0052) VR=CS VM=1 Printer Resolution ID</summary>
		public static DicomTag PrinterResolutionID = new DicomTag(0x2010, 0x0052);

		/// <summary>(2010,0054) VR=CS VM=1 Default Printer Resolution ID</summary>
		public static DicomTag DefaultPrinterResolutionID = new DicomTag(0x2010, 0x0054);

		/// <summary>(2010,0060) VR=CS VM=1 Magnification Type</summary>
		public static DicomTag MagnificationType = new DicomTag(0x2010, 0x0060);

		/// <summary>(2010,0080) VR=CS VM=1 Smoothing Type</summary>
		public static DicomTag SmoothingType = new DicomTag(0x2010, 0x0080);

		/// <summary>(2010,00a6) VR=CS VM=1 Default Magnification Type</summary>
		public static DicomTag DefaultMagnificationType = new DicomTag(0x2010, 0x00a6);

		/// <summary>(2010,00a7) VR=CS VM=1-n Other Magnification Types Available</summary>
		public static DicomTag OtherMagnificationTypesAvailable = new DicomTag(0x2010, 0x00a7);

		/// <summary>(2010,00a8) VR=CS VM=1 Default Smoothing Type</summary>
		public static DicomTag DefaultSmoothingType = new DicomTag(0x2010, 0x00a8);

		/// <summary>(2010,00a9) VR=CS VM=1-n Other Smoothing Types Available</summary>
		public static DicomTag OtherSmoothingTypesAvailable = new DicomTag(0x2010, 0x00a9);

		/// <summary>(2010,0100) VR=CS VM=1 Border Density</summary>
		public static DicomTag BorderDensity = new DicomTag(0x2010, 0x0100);

		/// <summary>(2010,0110) VR=CS VM=1 Empty Image Density</summary>
		public static DicomTag EmptyImageDensity = new DicomTag(0x2010, 0x0110);

		/// <summary>(2010,0120) VR=US VM=1 Min Density</summary>
		public static DicomTag MinDensity = new DicomTag(0x2010, 0x0120);

		/// <summary>(2010,0130) VR=US VM=1 Max Density</summary>
		public static DicomTag MaxDensity = new DicomTag(0x2010, 0x0130);

		/// <summary>(2010,0140) VR=CS VM=1 Trim</summary>
		public static DicomTag Trim = new DicomTag(0x2010, 0x0140);

		/// <summary>(2010,0150) VR=ST VM=1 Configuration Information</summary>
		public static DicomTag ConfigurationInformation = new DicomTag(0x2010, 0x0150);

		/// <summary>(2010,0152) VR=LT VM=1 Configuration Information Description</summary>
		public static DicomTag ConfigurationInformationDescription = new DicomTag(0x2010, 0x0152);

		/// <summary>(2010,0154) VR=IS VM=1 Maximum Collated Films</summary>
		public static DicomTag MaximumCollatedFilms = new DicomTag(0x2010, 0x0154);

		/// <summary>(2010,015e) VR=US VM=1 Illumination</summary>
		public static DicomTag Illumination = new DicomTag(0x2010, 0x015e);

		/// <summary>(2010,0160) VR=US VM=1 Reflected Ambient Light</summary>
		public static DicomTag ReflectedAmbientLight = new DicomTag(0x2010, 0x0160);

		/// <summary>(2010,0376) VR=DS VM=2 Printer Pixel Spacing</summary>
		public static DicomTag PrinterPixelSpacing = new DicomTag(0x2010, 0x0376);

		/// <summary>(2010,0500) VR=SQ VM=1 Referenced Film Session Sequence</summary>
		public static DicomTag ReferencedFilmSessionSequence = new DicomTag(0x2010, 0x0500);

		/// <summary>(2010,0510) VR=SQ VM=1 Referenced Image Box Sequence</summary>
		public static DicomTag ReferencedImageBoxSequence = new DicomTag(0x2010, 0x0510);

		/// <summary>(2010,0520) VR=SQ VM=1 Referenced Basic Annotation Box Sequence</summary>
		public static DicomTag ReferencedBasicAnnotationBoxSequence = new DicomTag(0x2010, 0x0520);

		/// <summary>(2020,0010) VR=US VM=1 Image Box Position</summary>
		public static DicomTag ImageBoxPosition = new DicomTag(0x2020, 0x0010);

		/// <summary>(2020,0020) VR=CS VM=1 Polarity</summary>
		public static DicomTag Polarity = new DicomTag(0x2020, 0x0020);

		/// <summary>(2020,0030) VR=DS VM=1 Requested Image Size</summary>
		public static DicomTag RequestedImageSize = new DicomTag(0x2020, 0x0030);

		/// <summary>(2020,0040) VR=CS VM=1 Requested Decimate/Crop Behavior</summary>
		public static DicomTag RequestedDecimateCropBehavior = new DicomTag(0x2020, 0x0040);

		/// <summary>(2020,0050) VR=CS VM=1 Requested Resolution ID</summary>
		public static DicomTag RequestedResolutionID = new DicomTag(0x2020, 0x0050);

		/// <summary>(2020,00a0) VR=CS VM=1 Requested Image Size Flag</summary>
		public static DicomTag RequestedImageSizeFlag = new DicomTag(0x2020, 0x00a0);

		/// <summary>(2020,00a2) VR=CS VM=1 Decimate/Crop Result</summary>
		public static DicomTag DecimateCropResult = new DicomTag(0x2020, 0x00a2);

		/// <summary>(2020,0110) VR=SQ VM=1 Basic Grayscale Image Sequence</summary>
		public static DicomTag BasicGrayscaleImageSequence = new DicomTag(0x2020, 0x0110);

		/// <summary>(2020,0111) VR=SQ VM=1 Basic Color Image Sequence</summary>
		public static DicomTag BasicColorImageSequence = new DicomTag(0x2020, 0x0111);

		/// <summary>(2020,0130) VR=SQ VM=1 Referenced Image Overlay Box Sequence (Retired)</summary>
		public static DicomTag ReferencedImageOverlayBoxSequenceRETIRED = new DicomTag(0x2020, 0x0130);

		/// <summary>(2020,0140) VR=SQ VM=1 Referenced VOI LUT Box Sequence (Retired)</summary>
		public static DicomTag ReferencedVOILUTBoxSequenceRETIRED = new DicomTag(0x2020, 0x0140);

		/// <summary>(2030,0010) VR=US VM=1 Annotation Position</summary>
		public static DicomTag AnnotationPosition = new DicomTag(0x2030, 0x0010);

		/// <summary>(2030,0020) VR=LO VM=1 Text String</summary>
		public static DicomTag TextString = new DicomTag(0x2030, 0x0020);

		/// <summary>(2040,0010) VR=SQ VM=1 Referenced Overlay Plane Sequence (Retired)</summary>
		public static DicomTag ReferencedOverlayPlaneSequenceRETIRED = new DicomTag(0x2040, 0x0010);

		/// <summary>(2040,0011) VR=US VM=1-99 Referenced Overlay Plane Groups (Retired)</summary>
		public static DicomTag ReferencedOverlayPlaneGroupsRETIRED = new DicomTag(0x2040, 0x0011);

		/// <summary>(2040,0020) VR=SQ VM=1 Overlay Pixel Data Sequence (Retired)</summary>
		public static DicomTag OverlayPixelDataSequenceRETIRED = new DicomTag(0x2040, 0x0020);

		/// <summary>(2040,0060) VR=CS VM=1 Overlay Magnification Type (Retired)</summary>
		public static DicomTag OverlayMagnificationTypeRETIRED = new DicomTag(0x2040, 0x0060);

		/// <summary>(2040,0070) VR=CS VM=1 Overlay Smoothing Type (Retired)</summary>
		public static DicomTag OverlaySmoothingTypeRETIRED = new DicomTag(0x2040, 0x0070);

		/// <summary>(2040,0072) VR=CS VM=1 Overlay or Image Magnification (Retired)</summary>
		public static DicomTag OverlayOrImageMagnificationRETIRED = new DicomTag(0x2040, 0x0072);

		/// <summary>(2040,0074) VR=US VM=1 Magnify to Number of Columns (Retired)</summary>
		public static DicomTag MagnifyToNumberOfColumnsRETIRED = new DicomTag(0x2040, 0x0074);

		/// <summary>(2040,0080) VR=CS VM=1 Overlay Foreground Density (Retired)</summary>
		public static DicomTag OverlayForegroundDensityRETIRED = new DicomTag(0x2040, 0x0080);

		/// <summary>(2040,0082) VR=CS VM=1 Overlay Background Density (Retired)</summary>
		public static DicomTag OverlayBackgroundDensityRETIRED = new DicomTag(0x2040, 0x0082);

		/// <summary>(2040,0090) VR=CS VM=1 Overlay Mode (Retired)</summary>
		public static DicomTag OverlayModeRETIRED = new DicomTag(0x2040, 0x0090);

		/// <summary>(2040,0100) VR=CS VM=1 Threshold Density (Retired)</summary>
		public static DicomTag ThresholdDensityRETIRED = new DicomTag(0x2040, 0x0100);

		/// <summary>(2040,0500) VR=SQ VM=1 Referenced Image Box Sequence (Retired) (Retired)</summary>
		public static DicomTag ReferencedImageBoxSequenceRetiredRETIRED = new DicomTag(0x2040, 0x0500);

		/// <summary>(2050,0010) VR=SQ VM=1 Presentation LUT Sequence</summary>
		public static DicomTag PresentationLUTSequence = new DicomTag(0x2050, 0x0010);

		/// <summary>(2050,0020) VR=CS VM=1 Presentation LUT Shape</summary>
		public static DicomTag PresentationLUTShape = new DicomTag(0x2050, 0x0020);

		/// <summary>(2050,0500) VR=SQ VM=1 Referenced Presentation LUT Sequence</summary>
		public static DicomTag ReferencedPresentationLUTSequence = new DicomTag(0x2050, 0x0500);

		/// <summary>(2100,0010) VR=SH VM=1 Print Job ID (Retired)</summary>
		public static DicomTag PrintJobIDRETIRED = new DicomTag(0x2100, 0x0010);

		/// <summary>(2100,0020) VR=CS VM=1 Execution Status</summary>
		public static DicomTag ExecutionStatus = new DicomTag(0x2100, 0x0020);

		/// <summary>(2100,0030) VR=CS VM=1 Execution Status Info</summary>
		public static DicomTag ExecutionStatusInfo = new DicomTag(0x2100, 0x0030);

		/// <summary>(2100,0040) VR=DA VM=1 Creation Date</summary>
		public static DicomTag CreationDate = new DicomTag(0x2100, 0x0040);

		/// <summary>(2100,0050) VR=TM VM=1 Creation Time</summary>
		public static DicomTag CreationTime = new DicomTag(0x2100, 0x0050);

		/// <summary>(2100,0070) VR=AE VM=1 Originator</summary>
		public static DicomTag Originator = new DicomTag(0x2100, 0x0070);

		/// <summary>(2100,0140) VR=AE VM=1 Destination AE (Retired)</summary>
		public static DicomTag DestinationAERETIRED = new DicomTag(0x2100, 0x0140);

		/// <summary>(2100,0160) VR=SH VM=1 Owner ID</summary>
		public static DicomTag OwnerID = new DicomTag(0x2100, 0x0160);

		/// <summary>(2100,0170) VR=IS VM=1 Number of Films</summary>
		public static DicomTag NumberOfFilms = new DicomTag(0x2100, 0x0170);

		/// <summary>(2100,0500) VR=SQ VM=1 Referenced Print Job Sequence (Pull Stored Print) (Retired)</summary>
		public static DicomTag ReferencedPrintJobSequencePullStoredPrintRETIRED = new DicomTag(0x2100, 0x0500);

		/// <summary>(2110,0010) VR=CS VM=1 Printer Status</summary>
		public static DicomTag PrinterStatus = new DicomTag(0x2110, 0x0010);

		/// <summary>(2110,0020) VR=CS VM=1 Printer Status Info</summary>
		public static DicomTag PrinterStatusInfo = new DicomTag(0x2110, 0x0020);

		/// <summary>(2110,0030) VR=LO VM=1 Printer Name</summary>
		public static DicomTag PrinterName = new DicomTag(0x2110, 0x0030);

		/// <summary>(2110,0099) VR=SH VM=1 Print Queue ID (Retired)</summary>
		public static DicomTag PrintQueueIDRETIRED = new DicomTag(0x2110, 0x0099);

		/// <summary>(2120,0010) VR=CS VM=1 Queue Status (Retired)</summary>
		public static DicomTag QueueStatusRETIRED = new DicomTag(0x2120, 0x0010);

		/// <summary>(2120,0050) VR=SQ VM=1 Print Job Description Sequence (Retired)</summary>
		public static DicomTag PrintJobDescriptionSequenceRETIRED = new DicomTag(0x2120, 0x0050);

		/// <summary>(2120,0070) VR=SQ VM=1 Referenced Print Job Sequence (Retired)</summary>
		public static DicomTag ReferencedPrintJobSequenceRETIRED = new DicomTag(0x2120, 0x0070);

		/// <summary>(2130,0010) VR=SQ VM=1 Print Management Capabilities Sequence (Retired)</summary>
		public static DicomTag PrintManagementCapabilitiesSequenceRETIRED = new DicomTag(0x2130, 0x0010);

		/// <summary>(2130,0015) VR=SQ VM=1 Printer Characteristics Sequence (Retired)</summary>
		public static DicomTag PrinterCharacteristicsSequenceRETIRED = new DicomTag(0x2130, 0x0015);

		/// <summary>(2130,0030) VR=SQ VM=1 Film Box Content Sequence (Retired)</summary>
		public static DicomTag FilmBoxContentSequenceRETIRED = new DicomTag(0x2130, 0x0030);

		/// <summary>(2130,0040) VR=SQ VM=1 Image Box Content Sequence (Retired)</summary>
		public static DicomTag ImageBoxContentSequenceRETIRED = new DicomTag(0x2130, 0x0040);

		/// <summary>(2130,0050) VR=SQ VM=1 Annotation Content Sequence (Retired)</summary>
		public static DicomTag AnnotationContentSequenceRETIRED = new DicomTag(0x2130, 0x0050);

		/// <summary>(2130,0060) VR=SQ VM=1 Image Overlay Box Content Sequence (Retired)</summary>
		public static DicomTag ImageOverlayBoxContentSequenceRETIRED = new DicomTag(0x2130, 0x0060);

		/// <summary>(2130,0080) VR=SQ VM=1 Presentation LUT Content Sequence (Retired)</summary>
		public static DicomTag PresentationLUTContentSequenceRETIRED = new DicomTag(0x2130, 0x0080);

		/// <summary>(2130,00a0) VR=SQ VM=1 Proposed Study Sequence (Retired)</summary>
		public static DicomTag ProposedStudySequenceRETIRED = new DicomTag(0x2130, 0x00a0);

		/// <summary>(2130,00c0) VR=SQ VM=1 Original Image Sequence (Retired)</summary>
		public static DicomTag OriginalImageSequenceRETIRED = new DicomTag(0x2130, 0x00c0);

		/// <summary>(2200,0001) VR=CS VM=1 Label Using Information Extracted From Instances</summary>
		public static DicomTag LabelUsingInformationExtractedFromInstances = new DicomTag(0x2200, 0x0001);

		/// <summary>(2200,0002) VR=UT VM=1 Label Text</summary>
		public static DicomTag LabelText = new DicomTag(0x2200, 0x0002);

		/// <summary>(2200,0003) VR=CS VM=1 Label Style Selection</summary>
		public static DicomTag LabelStyleSelection = new DicomTag(0x2200, 0x0003);

		/// <summary>(2200,0004) VR=LT VM=1 Media Disposition</summary>
		public static DicomTag MediaDisposition = new DicomTag(0x2200, 0x0004);

		/// <summary>(2200,0005) VR=LT VM=1 Barcode Value</summary>
		public static DicomTag BarcodeValue = new DicomTag(0x2200, 0x0005);

		/// <summary>(2200,0006) VR=CS VM=1 Barcode Symbology</summary>
		public static DicomTag BarcodeSymbology = new DicomTag(0x2200, 0x0006);

		/// <summary>(2200,0007) VR=CS VM=1 Allow Media Splitting</summary>
		public static DicomTag AllowMediaSplitting = new DicomTag(0x2200, 0x0007);

		/// <summary>(2200,0008) VR=CS VM=1 Include Non-DICOM Objects</summary>
		public static DicomTag IncludeNonDICOMObjects = new DicomTag(0x2200, 0x0008);

		/// <summary>(2200,0009) VR=CS VM=1 Include Display Application</summary>
		public static DicomTag IncludeDisplayApplication = new DicomTag(0x2200, 0x0009);

		/// <summary>(2200,000a) VR=CS VM=1 Preserve Composite Instances After Media Creation</summary>
		public static DicomTag PreserveCompositeInstancesAfterMediaCreation = new DicomTag(0x2200, 0x000a);

		/// <summary>(2200,000b) VR=US VM=1 Total Number of Pieces of Media Created</summary>
		public static DicomTag TotalNumberOfPiecesOfMediaCreated = new DicomTag(0x2200, 0x000b);

		/// <summary>(2200,000c) VR=LO VM=1 Requested Media Application Profile</summary>
		public static DicomTag RequestedMediaApplicationProfile = new DicomTag(0x2200, 0x000c);

		/// <summary>(2200,000d) VR=SQ VM=1 Referenced Storage Media Sequence</summary>
		public static DicomTag ReferencedStorageMediaSequence = new DicomTag(0x2200, 0x000d);

		/// <summary>(2200,000e) VR=AT VM=1-n Failure Attributes</summary>
		public static DicomTag FailureAttributes = new DicomTag(0x2200, 0x000e);

		/// <summary>(2200,000f) VR=CS VM=1 Allow Lossy Compression</summary>
		public static DicomTag AllowLossyCompression = new DicomTag(0x2200, 0x000f);

		/// <summary>(2200,0020) VR=CS VM=1 Request Priority</summary>
		public static DicomTag RequestPriority = new DicomTag(0x2200, 0x0020);

		/// <summary>(3002,0002) VR=SH VM=1 RT Image Label</summary>
		public static DicomTag RTImageLabel = new DicomTag(0x3002, 0x0002);

		/// <summary>(3002,0003) VR=LO VM=1 RT Image Name</summary>
		public static DicomTag RTImageName = new DicomTag(0x3002, 0x0003);

		/// <summary>(3002,0004) VR=ST VM=1 RT Image Description</summary>
		public static DicomTag RTImageDescription = new DicomTag(0x3002, 0x0004);

		/// <summary>(3002,000a) VR=CS VM=1 Reported Values Origin</summary>
		public static DicomTag ReportedValuesOrigin = new DicomTag(0x3002, 0x000a);

		/// <summary>(3002,000c) VR=CS VM=1 RT Image Plane</summary>
		public static DicomTag RTImagePlane = new DicomTag(0x3002, 0x000c);

		/// <summary>(3002,000d) VR=DS VM=3 X-Ray Image Receptor Translation</summary>
		public static DicomTag XRayImageReceptorTranslation = new DicomTag(0x3002, 0x000d);

		/// <summary>(3002,000e) VR=DS VM=1 X-Ray Image Receptor Angle</summary>
		public static DicomTag XRayImageReceptorAngle = new DicomTag(0x3002, 0x000e);

		/// <summary>(3002,0010) VR=DS VM=6 RT Image Orientation</summary>
		public static DicomTag RTImageOrientation = new DicomTag(0x3002, 0x0010);

		/// <summary>(3002,0011) VR=DS VM=2 Image Plane Pixel Spacing</summary>
		public static DicomTag ImagePlanePixelSpacing = new DicomTag(0x3002, 0x0011);

		/// <summary>(3002,0012) VR=DS VM=2 RT Image Position</summary>
		public static DicomTag RTImagePosition = new DicomTag(0x3002, 0x0012);

		/// <summary>(3002,0020) VR=SH VM=1 Radiation Machine Name</summary>
		public static DicomTag RadiationMachineName = new DicomTag(0x3002, 0x0020);

		/// <summary>(3002,0022) VR=DS VM=1 Radiation Machine SAD</summary>
		public static DicomTag RadiationMachineSAD = new DicomTag(0x3002, 0x0022);

		/// <summary>(3002,0024) VR=DS VM=1 Radiation Machine SSD</summary>
		public static DicomTag RadiationMachineSSD = new DicomTag(0x3002, 0x0024);

		/// <summary>(3002,0026) VR=DS VM=1 RT Image SID</summary>
		public static DicomTag RTImageSID = new DicomTag(0x3002, 0x0026);

		/// <summary>(3002,0028) VR=DS VM=1 Source to Reference Object Distance</summary>
		public static DicomTag SourceToReferenceObjectDistance = new DicomTag(0x3002, 0x0028);

		/// <summary>(3002,0029) VR=IS VM=1 Fraction Number</summary>
		public static DicomTag FractionNumber = new DicomTag(0x3002, 0x0029);

		/// <summary>(3002,0030) VR=SQ VM=1 Exposure Sequence</summary>
		public static DicomTag ExposureSequence = new DicomTag(0x3002, 0x0030);

		/// <summary>(3002,0032) VR=DS VM=1 Meterset Exposure</summary>
		public static DicomTag MetersetExposure = new DicomTag(0x3002, 0x0032);

		/// <summary>(3002,0034) VR=DS VM=4 Diaphragm Position</summary>
		public static DicomTag DiaphragmPosition = new DicomTag(0x3002, 0x0034);

		/// <summary>(3002,0040) VR=SQ VM=1 Fluence Map Sequence</summary>
		public static DicomTag FluenceMapSequence = new DicomTag(0x3002, 0x0040);

		/// <summary>(3002,0041) VR=CS VM=1 Fluence Data Source</summary>
		public static DicomTag FluenceDataSource = new DicomTag(0x3002, 0x0041);

		/// <summary>(3002,0042) VR=DS VM=1 Fluence Data Scale</summary>
		public static DicomTag FluenceDataScale = new DicomTag(0x3002, 0x0042);

		/// <summary>(3004,0001) VR=CS VM=1 DVH Type</summary>
		public static DicomTag DVHType = new DicomTag(0x3004, 0x0001);

		/// <summary>(3004,0002) VR=CS VM=1 Dose Units</summary>
		public static DicomTag DoseUnits = new DicomTag(0x3004, 0x0002);

		/// <summary>(3004,0004) VR=CS VM=1 Dose Type</summary>
		public static DicomTag DoseType = new DicomTag(0x3004, 0x0004);

		/// <summary>(3004,0006) VR=LO VM=1 Dose Comment</summary>
		public static DicomTag DoseComment = new DicomTag(0x3004, 0x0006);

		/// <summary>(3004,0008) VR=DS VM=3 Normalization Point</summary>
		public static DicomTag NormalizationPoint = new DicomTag(0x3004, 0x0008);

		/// <summary>(3004,000a) VR=CS VM=1 Dose Summation Type</summary>
		public static DicomTag DoseSummationType = new DicomTag(0x3004, 0x000a);

		/// <summary>(3004,000c) VR=DS VM=2-n Grid Frame Offset Vector</summary>
		public static DicomTag GridFrameOffsetVector = new DicomTag(0x3004, 0x000c);

		/// <summary>(3004,000e) VR=DS VM=1 Dose Grid Scaling</summary>
		public static DicomTag DoseGridScaling = new DicomTag(0x3004, 0x000e);

		/// <summary>(3004,0010) VR=SQ VM=1 RT Dose ROI Sequence</summary>
		public static DicomTag RTDoseROISequence = new DicomTag(0x3004, 0x0010);

		/// <summary>(3004,0012) VR=DS VM=1 Dose Value</summary>
		public static DicomTag DoseValue = new DicomTag(0x3004, 0x0012);

		/// <summary>(3004,0014) VR=CS VM=1-3 Tissue Heterogeneity Correction</summary>
		public static DicomTag TissueHeterogeneityCorrection = new DicomTag(0x3004, 0x0014);

		/// <summary>(3004,0040) VR=DS VM=3 DVH Normalization Point</summary>
		public static DicomTag DVHNormalizationPoint = new DicomTag(0x3004, 0x0040);

		/// <summary>(3004,0042) VR=DS VM=1 DVH Normalization Dose Value</summary>
		public static DicomTag DVHNormalizationDoseValue = new DicomTag(0x3004, 0x0042);

		/// <summary>(3004,0050) VR=SQ VM=1 DVH Sequence</summary>
		public static DicomTag DVHSequence = new DicomTag(0x3004, 0x0050);

		/// <summary>(3004,0052) VR=DS VM=1 DVH Dose Scaling</summary>
		public static DicomTag DVHDoseScaling = new DicomTag(0x3004, 0x0052);

		/// <summary>(3004,0054) VR=CS VM=1 DVH Volume Units</summary>
		public static DicomTag DVHVolumeUnits = new DicomTag(0x3004, 0x0054);

		/// <summary>(3004,0056) VR=IS VM=1 DVH Number of Bins</summary>
		public static DicomTag DVHNumberOfBins = new DicomTag(0x3004, 0x0056);

		/// <summary>(3004,0058) VR=DS VM=2-2n DVH Data</summary>
		public static DicomTag DVHData = new DicomTag(0x3004, 0x0058);

		/// <summary>(3004,0060) VR=SQ VM=1 DVH Referenced ROI Sequence</summary>
		public static DicomTag DVHReferencedROISequence = new DicomTag(0x3004, 0x0060);

		/// <summary>(3004,0062) VR=CS VM=1 DVH ROI Contribution Type</summary>
		public static DicomTag DVHROIContributionType = new DicomTag(0x3004, 0x0062);

		/// <summary>(3004,0070) VR=DS VM=1 DVH Minimum Dose</summary>
		public static DicomTag DVHMinimumDose = new DicomTag(0x3004, 0x0070);

		/// <summary>(3004,0072) VR=DS VM=1 DVH Maximum Dose</summary>
		public static DicomTag DVHMaximumDose = new DicomTag(0x3004, 0x0072);

		/// <summary>(3004,0074) VR=DS VM=1 DVH Mean Dose</summary>
		public static DicomTag DVHMeanDose = new DicomTag(0x3004, 0x0074);

		/// <summary>(3006,0002) VR=SH VM=1 Structure Set Label</summary>
		public static DicomTag StructureSetLabel = new DicomTag(0x3006, 0x0002);

		/// <summary>(3006,0004) VR=LO VM=1 Structure Set Name</summary>
		public static DicomTag StructureSetName = new DicomTag(0x3006, 0x0004);

		/// <summary>(3006,0006) VR=ST VM=1 Structure Set Description</summary>
		public static DicomTag StructureSetDescription = new DicomTag(0x3006, 0x0006);

		/// <summary>(3006,0008) VR=DA VM=1 Structure Set Date</summary>
		public static DicomTag StructureSetDate = new DicomTag(0x3006, 0x0008);

		/// <summary>(3006,0009) VR=TM VM=1 Structure Set Time</summary>
		public static DicomTag StructureSetTime = new DicomTag(0x3006, 0x0009);

		/// <summary>(3006,0010) VR=SQ VM=1 Referenced Frame of Reference Sequence</summary>
		public static DicomTag ReferencedFrameOfReferenceSequence = new DicomTag(0x3006, 0x0010);

		/// <summary>(3006,0012) VR=SQ VM=1 RT Referenced Study Sequence</summary>
		public static DicomTag RTReferencedStudySequence = new DicomTag(0x3006, 0x0012);

		/// <summary>(3006,0014) VR=SQ VM=1 RT Referenced Series Sequence</summary>
		public static DicomTag RTReferencedSeriesSequence = new DicomTag(0x3006, 0x0014);

		/// <summary>(3006,0016) VR=SQ VM=1 Contour Image Sequence</summary>
		public static DicomTag ContourImageSequence = new DicomTag(0x3006, 0x0016);

		/// <summary>(3006,0020) VR=SQ VM=1 Structure Set ROI Sequence</summary>
		public static DicomTag StructureSetROISequence = new DicomTag(0x3006, 0x0020);

		/// <summary>(3006,0022) VR=IS VM=1 ROI Number</summary>
		public static DicomTag ROINumber = new DicomTag(0x3006, 0x0022);

		/// <summary>(3006,0024) VR=UI VM=1 Referenced Frame of Reference UID</summary>
		public static DicomTag ReferencedFrameOfReferenceUID = new DicomTag(0x3006, 0x0024);

		/// <summary>(3006,0026) VR=LO VM=1 ROI Name</summary>
		public static DicomTag ROIName = new DicomTag(0x3006, 0x0026);

		/// <summary>(3006,0028) VR=ST VM=1 ROI Description</summary>
		public static DicomTag ROIDescription = new DicomTag(0x3006, 0x0028);

		/// <summary>(3006,002a) VR=IS VM=3 ROI Display Color</summary>
		public static DicomTag ROIDisplayColor = new DicomTag(0x3006, 0x002a);

		/// <summary>(3006,002c) VR=DS VM=1 ROI Volume</summary>
		public static DicomTag ROIVolume = new DicomTag(0x3006, 0x002c);

		/// <summary>(3006,0030) VR=SQ VM=1 RT Related ROI Sequence</summary>
		public static DicomTag RTRelatedROISequence = new DicomTag(0x3006, 0x0030);

		/// <summary>(3006,0033) VR=CS VM=1 RT ROI Relationship</summary>
		public static DicomTag RTROIRelationship = new DicomTag(0x3006, 0x0033);

		/// <summary>(3006,0036) VR=CS VM=1 ROI Generation Algorithm</summary>
		public static DicomTag ROIGenerationAlgorithm = new DicomTag(0x3006, 0x0036);

		/// <summary>(3006,0038) VR=LO VM=1 ROI Generation Description</summary>
		public static DicomTag ROIGenerationDescription = new DicomTag(0x3006, 0x0038);

		/// <summary>(3006,0039) VR=SQ VM=1 ROI Contour Sequence</summary>
		public static DicomTag ROIContourSequence = new DicomTag(0x3006, 0x0039);

		/// <summary>(3006,0040) VR=SQ VM=1 Contour Sequence</summary>
		public static DicomTag ContourSequence = new DicomTag(0x3006, 0x0040);

		/// <summary>(3006,0042) VR=CS VM=1 Contour Geometric Type</summary>
		public static DicomTag ContourGeometricType = new DicomTag(0x3006, 0x0042);

		/// <summary>(3006,0044) VR=DS VM=1 Contour Slab Thickness</summary>
		public static DicomTag ContourSlabThickness = new DicomTag(0x3006, 0x0044);

		/// <summary>(3006,0045) VR=DS VM=3 Contour Offset Vector</summary>
		public static DicomTag ContourOffsetVector = new DicomTag(0x3006, 0x0045);

		/// <summary>(3006,0046) VR=IS VM=1 Number of Contour Points</summary>
		public static DicomTag NumberOfContourPoints = new DicomTag(0x3006, 0x0046);

		/// <summary>(3006,0048) VR=IS VM=1 Contour Number</summary>
		public static DicomTag ContourNumber = new DicomTag(0x3006, 0x0048);

		/// <summary>(3006,0049) VR=IS VM=1-n Attached Contours</summary>
		public static DicomTag AttachedContours = new DicomTag(0x3006, 0x0049);

		/// <summary>(3006,0050) VR=DS VM=3-3n Contour Data</summary>
		public static DicomTag ContourData = new DicomTag(0x3006, 0x0050);

		/// <summary>(3006,0080) VR=SQ VM=1 RT ROI Observations Sequence</summary>
		public static DicomTag RTROIObservationsSequence = new DicomTag(0x3006, 0x0080);

		/// <summary>(3006,0082) VR=IS VM=1 Observation Number</summary>
		public static DicomTag ObservationNumber = new DicomTag(0x3006, 0x0082);

		/// <summary>(3006,0084) VR=IS VM=1 Referenced ROI Number</summary>
		public static DicomTag ReferencedROINumber = new DicomTag(0x3006, 0x0084);

		/// <summary>(3006,0085) VR=SH VM=1 ROI Observation Label</summary>
		public static DicomTag ROIObservationLabel = new DicomTag(0x3006, 0x0085);

		/// <summary>(3006,0086) VR=SQ VM=1 RT ROI Identification Code Sequence</summary>
		public static DicomTag RTROIIdentificationCodeSequence = new DicomTag(0x3006, 0x0086);

		/// <summary>(3006,0088) VR=ST VM=1 ROI Observation Description</summary>
		public static DicomTag ROIObservationDescription = new DicomTag(0x3006, 0x0088);

		/// <summary>(3006,00a0) VR=SQ VM=1 Related RT ROI Observations Sequence</summary>
		public static DicomTag RelatedRTROIObservationsSequence = new DicomTag(0x3006, 0x00a0);

		/// <summary>(3006,00a4) VR=CS VM=1 RT ROI Interpreted Type</summary>
		public static DicomTag RTROIInterpretedType = new DicomTag(0x3006, 0x00a4);

		/// <summary>(3006,00a6) VR=PN VM=1 ROI Interpreter</summary>
		public static DicomTag ROIInterpreter = new DicomTag(0x3006, 0x00a6);

		/// <summary>(3006,00b0) VR=SQ VM=1 ROI Physical Properties Sequence</summary>
		public static DicomTag ROIPhysicalPropertiesSequence = new DicomTag(0x3006, 0x00b0);

		/// <summary>(3006,00b2) VR=CS VM=1 ROI Physical Property</summary>
		public static DicomTag ROIPhysicalProperty = new DicomTag(0x3006, 0x00b2);

		/// <summary>(3006,00b4) VR=DS VM=1 ROI Physical Property Value</summary>
		public static DicomTag ROIPhysicalPropertyValue = new DicomTag(0x3006, 0x00b4);

		/// <summary>(3006,00b6) VR=SQ VM=1 ROI Elemental Composition Sequence</summary>
		public static DicomTag ROIElementalCompositionSequence = new DicomTag(0x3006, 0x00b6);

		/// <summary>(3006,00b7) VR=US VM=1 ROI Elemental Composition Atomic Number</summary>
		public static DicomTag ROIElementalCompositionAtomicNumber = new DicomTag(0x3006, 0x00b7);

		/// <summary>(3006,00b8) VR=FL VM=1 ROI Elemental Composition Atomic Mass Fraction</summary>
		public static DicomTag ROIElementalCompositionAtomicMassFraction = new DicomTag(0x3006, 0x00b8);

		/// <summary>(3006,00c0) VR=SQ VM=1 Frame of Reference Relationship Sequence</summary>
		public static DicomTag FrameOfReferenceRelationshipSequence = new DicomTag(0x3006, 0x00c0);

		/// <summary>(3006,00c2) VR=UI VM=1 Related Frame of Reference UID</summary>
		public static DicomTag RelatedFrameOfReferenceUID = new DicomTag(0x3006, 0x00c2);

		/// <summary>(3006,00c4) VR=CS VM=1 Frame of Reference Transformation Type</summary>
		public static DicomTag FrameOfReferenceTransformationType = new DicomTag(0x3006, 0x00c4);

		/// <summary>(3006,00c6) VR=DS VM=16 Frame of Reference Transformation Matrix</summary>
		public static DicomTag FrameOfReferenceTransformationMatrix = new DicomTag(0x3006, 0x00c6);

		/// <summary>(3006,00c8) VR=LO VM=1 Frame of Reference Transformation Comment</summary>
		public static DicomTag FrameOfReferenceTransformationComment = new DicomTag(0x3006, 0x00c8);

		/// <summary>(3008,0010) VR=SQ VM=1 Measured Dose Reference Sequence</summary>
		public static DicomTag MeasuredDoseReferenceSequence = new DicomTag(0x3008, 0x0010);

		/// <summary>(3008,0012) VR=ST VM=1 Measured Dose Description</summary>
		public static DicomTag MeasuredDoseDescription = new DicomTag(0x3008, 0x0012);

		/// <summary>(3008,0014) VR=CS VM=1 Measured Dose Type</summary>
		public static DicomTag MeasuredDoseType = new DicomTag(0x3008, 0x0014);

		/// <summary>(3008,0016) VR=DS VM=1 Measured Dose Value</summary>
		public static DicomTag MeasuredDoseValue = new DicomTag(0x3008, 0x0016);

		/// <summary>(3008,0020) VR=SQ VM=1 Treatment Session Beam Sequence</summary>
		public static DicomTag TreatmentSessionBeamSequence = new DicomTag(0x3008, 0x0020);

		/// <summary>(3008,0021) VR=SQ VM=1 Treatment Session Ion Beam Sequence</summary>
		public static DicomTag TreatmentSessionIonBeamSequence = new DicomTag(0x3008, 0x0021);

		/// <summary>(3008,0022) VR=IS VM=1 Current Fraction Number</summary>
		public static DicomTag CurrentFractionNumber = new DicomTag(0x3008, 0x0022);

		/// <summary>(3008,0024) VR=DA VM=1 Treatment Control Point Date</summary>
		public static DicomTag TreatmentControlPointDate = new DicomTag(0x3008, 0x0024);

		/// <summary>(3008,0025) VR=TM VM=1 Treatment Control Point Time</summary>
		public static DicomTag TreatmentControlPointTime = new DicomTag(0x3008, 0x0025);

		/// <summary>(3008,002a) VR=CS VM=1 Treatment Termination Status</summary>
		public static DicomTag TreatmentTerminationStatus = new DicomTag(0x3008, 0x002a);

		/// <summary>(3008,002b) VR=SH VM=1 Treatment Termination Code</summary>
		public static DicomTag TreatmentTerminationCode = new DicomTag(0x3008, 0x002b);

		/// <summary>(3008,002c) VR=CS VM=1 Treatment Verification Status</summary>
		public static DicomTag TreatmentVerificationStatus = new DicomTag(0x3008, 0x002c);

		/// <summary>(3008,0030) VR=SQ VM=1 Referenced Treatment Record Sequence</summary>
		public static DicomTag ReferencedTreatmentRecordSequence = new DicomTag(0x3008, 0x0030);

		/// <summary>(3008,0032) VR=DS VM=1 Specified Primary Meterset</summary>
		public static DicomTag SpecifiedPrimaryMeterset = new DicomTag(0x3008, 0x0032);

		/// <summary>(3008,0033) VR=DS VM=1 Specified Secondary Meterset</summary>
		public static DicomTag SpecifiedSecondaryMeterset = new DicomTag(0x3008, 0x0033);

		/// <summary>(3008,0036) VR=DS VM=1 Delivered Primary Meterset</summary>
		public static DicomTag DeliveredPrimaryMeterset = new DicomTag(0x3008, 0x0036);

		/// <summary>(3008,0037) VR=DS VM=1 Delivered Secondary Meterset</summary>
		public static DicomTag DeliveredSecondaryMeterset = new DicomTag(0x3008, 0x0037);

		/// <summary>(3008,003a) VR=DS VM=1 Specified Treatment Time</summary>
		public static DicomTag SpecifiedTreatmentTime = new DicomTag(0x3008, 0x003a);

		/// <summary>(3008,003b) VR=DS VM=1 Delivered Treatment Time</summary>
		public static DicomTag DeliveredTreatmentTime = new DicomTag(0x3008, 0x003b);

		/// <summary>(3008,0040) VR=SQ VM=1 Control Point Delivery Sequence</summary>
		public static DicomTag ControlPointDeliverySequence = new DicomTag(0x3008, 0x0040);

		/// <summary>(3008,0041) VR=SQ VM=1 Ion Control Point Delivery Sequence</summary>
		public static DicomTag IonControlPointDeliverySequence = new DicomTag(0x3008, 0x0041);

		/// <summary>(3008,0042) VR=DS VM=1 Specified Meterset</summary>
		public static DicomTag SpecifiedMeterset = new DicomTag(0x3008, 0x0042);

		/// <summary>(3008,0044) VR=DS VM=1 Delivered Meterset</summary>
		public static DicomTag DeliveredMeterset = new DicomTag(0x3008, 0x0044);

		/// <summary>(3008,0045) VR=FL VM=1 Meterset Rate Set</summary>
		public static DicomTag MetersetRateSet = new DicomTag(0x3008, 0x0045);

		/// <summary>(3008,0046) VR=FL VM=1 Meterset Rate Delivered</summary>
		public static DicomTag MetersetRateDelivered = new DicomTag(0x3008, 0x0046);

		/// <summary>(3008,0047) VR=FL VM=1-n Scan Spot Metersets Delivered</summary>
		public static DicomTag ScanSpotMetersetsDelivered = new DicomTag(0x3008, 0x0047);

		/// <summary>(3008,0048) VR=DS VM=1 Dose Rate Delivered</summary>
		public static DicomTag DoseRateDelivered = new DicomTag(0x3008, 0x0048);

		/// <summary>(3008,0050) VR=SQ VM=1 Treatment Summary Calculated Dose Reference Sequence</summary>
		public static DicomTag TreatmentSummaryCalculatedDoseReferenceSequence = new DicomTag(0x3008, 0x0050);

		/// <summary>(3008,0052) VR=DS VM=1 Cumulative Dose to Dose Reference</summary>
		public static DicomTag CumulativeDoseToDoseReference = new DicomTag(0x3008, 0x0052);

		/// <summary>(3008,0054) VR=DA VM=1 First Treatment Date</summary>
		public static DicomTag FirstTreatmentDate = new DicomTag(0x3008, 0x0054);

		/// <summary>(3008,0056) VR=DA VM=1 Most Recent Treatment Date</summary>
		public static DicomTag MostRecentTreatmentDate = new DicomTag(0x3008, 0x0056);

		/// <summary>(3008,005a) VR=IS VM=1 Number of Fractions Delivered</summary>
		public static DicomTag NumberOfFractionsDelivered = new DicomTag(0x3008, 0x005a);

		/// <summary>(3008,0060) VR=SQ VM=1 Override Sequence</summary>
		public static DicomTag OverrideSequence = new DicomTag(0x3008, 0x0060);

		/// <summary>(3008,0061) VR=AT VM=1 Parameter Sequence Pointer</summary>
		public static DicomTag ParameterSequencePointer = new DicomTag(0x3008, 0x0061);

		/// <summary>(3008,0062) VR=AT VM=1 Override Parameter Pointer</summary>
		public static DicomTag OverrideParameterPointer = new DicomTag(0x3008, 0x0062);

		/// <summary>(3008,0063) VR=IS VM=1 Parameter Item Index</summary>
		public static DicomTag ParameterItemIndex = new DicomTag(0x3008, 0x0063);

		/// <summary>(3008,0064) VR=IS VM=1 Measured Dose Reference Number</summary>
		public static DicomTag MeasuredDoseReferenceNumber = new DicomTag(0x3008, 0x0064);

		/// <summary>(3008,0065) VR=AT VM=1 Parameter Pointer</summary>
		public static DicomTag ParameterPointer = new DicomTag(0x3008, 0x0065);

		/// <summary>(3008,0066) VR=ST VM=1 Override Reason</summary>
		public static DicomTag OverrideReason = new DicomTag(0x3008, 0x0066);

		/// <summary>(3008,0068) VR=SQ VM=1 Corrected Parameter Sequence</summary>
		public static DicomTag CorrectedParameterSequence = new DicomTag(0x3008, 0x0068);

		/// <summary>(3008,006a) VR=FL VM=1 Correction Value</summary>
		public static DicomTag CorrectionValue = new DicomTag(0x3008, 0x006a);

		/// <summary>(3008,0070) VR=SQ VM=1 Calculated Dose Reference Sequence</summary>
		public static DicomTag CalculatedDoseReferenceSequence = new DicomTag(0x3008, 0x0070);

		/// <summary>(3008,0072) VR=IS VM=1 Calculated Dose Reference Number</summary>
		public static DicomTag CalculatedDoseReferenceNumber = new DicomTag(0x3008, 0x0072);

		/// <summary>(3008,0074) VR=ST VM=1 Calculated Dose Reference Description</summary>
		public static DicomTag CalculatedDoseReferenceDescription = new DicomTag(0x3008, 0x0074);

		/// <summary>(3008,0076) VR=DS VM=1 Calculated Dose Reference Dose Value</summary>
		public static DicomTag CalculatedDoseReferenceDoseValue = new DicomTag(0x3008, 0x0076);

		/// <summary>(3008,0078) VR=DS VM=1 Start Meterset</summary>
		public static DicomTag StartMeterset = new DicomTag(0x3008, 0x0078);

		/// <summary>(3008,007a) VR=DS VM=1 End Meterset</summary>
		public static DicomTag EndMeterset = new DicomTag(0x3008, 0x007a);

		/// <summary>(3008,0080) VR=SQ VM=1 Referenced Measured Dose Reference Sequence</summary>
		public static DicomTag ReferencedMeasuredDoseReferenceSequence = new DicomTag(0x3008, 0x0080);

		/// <summary>(3008,0082) VR=IS VM=1 Referenced Measured Dose Reference Number</summary>
		public static DicomTag ReferencedMeasuredDoseReferenceNumber = new DicomTag(0x3008, 0x0082);

		/// <summary>(3008,0090) VR=SQ VM=1 Referenced Calculated Dose Reference Sequence</summary>
		public static DicomTag ReferencedCalculatedDoseReferenceSequence = new DicomTag(0x3008, 0x0090);

		/// <summary>(3008,0092) VR=IS VM=1 Referenced Calculated Dose Reference Number</summary>
		public static DicomTag ReferencedCalculatedDoseReferenceNumber = new DicomTag(0x3008, 0x0092);

		/// <summary>(3008,00a0) VR=SQ VM=1 Beam Limiting Device Leaf Pairs Sequence</summary>
		public static DicomTag BeamLimitingDeviceLeafPairsSequence = new DicomTag(0x3008, 0x00a0);

		/// <summary>(3008,00b0) VR=SQ VM=1 Recorded Wedge Sequence</summary>
		public static DicomTag RecordedWedgeSequence = new DicomTag(0x3008, 0x00b0);

		/// <summary>(3008,00c0) VR=SQ VM=1 Recorded Compensator Sequence</summary>
		public static DicomTag RecordedCompensatorSequence = new DicomTag(0x3008, 0x00c0);

		/// <summary>(3008,00d0) VR=SQ VM=1 Recorded Block Sequence</summary>
		public static DicomTag RecordedBlockSequence = new DicomTag(0x3008, 0x00d0);

		/// <summary>(3008,00e0) VR=SQ VM=1 Treatment Summary Measured Dose Reference Sequence</summary>
		public static DicomTag TreatmentSummaryMeasuredDoseReferenceSequence = new DicomTag(0x3008, 0x00e0);

		/// <summary>(3008,00f0) VR=SQ VM=1 Recorded Snout Sequence</summary>
		public static DicomTag RecordedSnoutSequence = new DicomTag(0x3008, 0x00f0);

		/// <summary>(3008,00f2) VR=SQ VM=1 Recorded Range Shifter Sequence</summary>
		public static DicomTag RecordedRangeShifterSequence = new DicomTag(0x3008, 0x00f2);

		/// <summary>(3008,00f4) VR=SQ VM=1 Recorded Lateral Spreading Device Sequence</summary>
		public static DicomTag RecordedLateralSpreadingDeviceSequence = new DicomTag(0x3008, 0x00f4);

		/// <summary>(3008,00f6) VR=SQ VM=1 Recorded Range Modulator Sequence</summary>
		public static DicomTag RecordedRangeModulatorSequence = new DicomTag(0x3008, 0x00f6);

		/// <summary>(3008,0100) VR=SQ VM=1 Recorded Source Sequence</summary>
		public static DicomTag RecordedSourceSequence = new DicomTag(0x3008, 0x0100);

		/// <summary>(3008,0105) VR=LO VM=1 Source Serial Number</summary>
		public static DicomTag SourceSerialNumber = new DicomTag(0x3008, 0x0105);

		/// <summary>(3008,0110) VR=SQ VM=1 Treatment Session Application Setup Sequence</summary>
		public static DicomTag TreatmentSessionApplicationSetupSequence = new DicomTag(0x3008, 0x0110);

		/// <summary>(3008,0116) VR=CS VM=1 Application Setup Check</summary>
		public static DicomTag ApplicationSetupCheck = new DicomTag(0x3008, 0x0116);

		/// <summary>(3008,0120) VR=SQ VM=1 Recorded Brachy Accessory Device Sequence</summary>
		public static DicomTag RecordedBrachyAccessoryDeviceSequence = new DicomTag(0x3008, 0x0120);

		/// <summary>(3008,0122) VR=IS VM=1 Referenced Brachy Accessory Device Number</summary>
		public static DicomTag ReferencedBrachyAccessoryDeviceNumber = new DicomTag(0x3008, 0x0122);

		/// <summary>(3008,0130) VR=SQ VM=1 Recorded Channel Sequence</summary>
		public static DicomTag RecordedChannelSequence = new DicomTag(0x3008, 0x0130);

		/// <summary>(3008,0132) VR=DS VM=1 Specified Channel Total Time</summary>
		public static DicomTag SpecifiedChannelTotalTime = new DicomTag(0x3008, 0x0132);

		/// <summary>(3008,0134) VR=DS VM=1 Delivered Channel Total Time</summary>
		public static DicomTag DeliveredChannelTotalTime = new DicomTag(0x3008, 0x0134);

		/// <summary>(3008,0136) VR=IS VM=1 Specified Number of Pulses</summary>
		public static DicomTag SpecifiedNumberOfPulses = new DicomTag(0x3008, 0x0136);

		/// <summary>(3008,0138) VR=IS VM=1 Delivered Number of Pulses</summary>
		public static DicomTag DeliveredNumberOfPulses = new DicomTag(0x3008, 0x0138);

		/// <summary>(3008,013a) VR=DS VM=1 Specified Pulse Repetition Interval</summary>
		public static DicomTag SpecifiedPulseRepetitionInterval = new DicomTag(0x3008, 0x013a);

		/// <summary>(3008,013c) VR=DS VM=1 Delivered Pulse Repetition Interval</summary>
		public static DicomTag DeliveredPulseRepetitionInterval = new DicomTag(0x3008, 0x013c);

		/// <summary>(3008,0140) VR=SQ VM=1 Recorded Source Applicator Sequence</summary>
		public static DicomTag RecordedSourceApplicatorSequence = new DicomTag(0x3008, 0x0140);

		/// <summary>(3008,0142) VR=IS VM=1 Referenced Source Applicator Number</summary>
		public static DicomTag ReferencedSourceApplicatorNumber = new DicomTag(0x3008, 0x0142);

		/// <summary>(3008,0150) VR=SQ VM=1 Recorded Channel Shield Sequence</summary>
		public static DicomTag RecordedChannelShieldSequence = new DicomTag(0x3008, 0x0150);

		/// <summary>(3008,0152) VR=IS VM=1 Referenced Channel Shield Number</summary>
		public static DicomTag ReferencedChannelShieldNumber = new DicomTag(0x3008, 0x0152);

		/// <summary>(3008,0160) VR=SQ VM=1 Brachy Control Point Delivered Sequence</summary>
		public static DicomTag BrachyControlPointDeliveredSequence = new DicomTag(0x3008, 0x0160);

		/// <summary>(3008,0162) VR=DA VM=1 Safe Position Exit Date</summary>
		public static DicomTag SafePositionExitDate = new DicomTag(0x3008, 0x0162);

		/// <summary>(3008,0164) VR=TM VM=1 Safe Position Exit Time</summary>
		public static DicomTag SafePositionExitTime = new DicomTag(0x3008, 0x0164);

		/// <summary>(3008,0166) VR=DA VM=1 Safe Position Return Date</summary>
		public static DicomTag SafePositionReturnDate = new DicomTag(0x3008, 0x0166);

		/// <summary>(3008,0168) VR=TM VM=1 Safe Position Return Time</summary>
		public static DicomTag SafePositionReturnTime = new DicomTag(0x3008, 0x0168);

		/// <summary>(3008,0200) VR=CS VM=1 Current Treatment Status</summary>
		public static DicomTag CurrentTreatmentStatus = new DicomTag(0x3008, 0x0200);

		/// <summary>(3008,0202) VR=ST VM=1 Treatment Status Comment</summary>
		public static DicomTag TreatmentStatusComment = new DicomTag(0x3008, 0x0202);

		/// <summary>(3008,0220) VR=SQ VM=1 Fraction Group Summary Sequence</summary>
		public static DicomTag FractionGroupSummarySequence = new DicomTag(0x3008, 0x0220);

		/// <summary>(3008,0223) VR=IS VM=1 Referenced Fraction Number</summary>
		public static DicomTag ReferencedFractionNumber = new DicomTag(0x3008, 0x0223);

		/// <summary>(3008,0224) VR=CS VM=1 Fraction Group Type</summary>
		public static DicomTag FractionGroupType = new DicomTag(0x3008, 0x0224);

		/// <summary>(3008,0230) VR=CS VM=1 Beam Stopper Position</summary>
		public static DicomTag BeamStopperPosition = new DicomTag(0x3008, 0x0230);

		/// <summary>(3008,0240) VR=SQ VM=1 Fraction Status Summary Sequence</summary>
		public static DicomTag FractionStatusSummarySequence = new DicomTag(0x3008, 0x0240);

		/// <summary>(3008,0250) VR=DA VM=1 Treatment Date</summary>
		public static DicomTag TreatmentDate = new DicomTag(0x3008, 0x0250);

		/// <summary>(3008,0251) VR=TM VM=1 Treatment Time</summary>
		public static DicomTag TreatmentTime = new DicomTag(0x3008, 0x0251);

		/// <summary>(300a,0002) VR=SH VM=1 RT Plan Label</summary>
		public static DicomTag RTPlanLabel = new DicomTag(0x300a, 0x0002);

		/// <summary>(300a,0003) VR=LO VM=1 RT Plan Name</summary>
		public static DicomTag RTPlanName = new DicomTag(0x300a, 0x0003);

		/// <summary>(300a,0004) VR=ST VM=1 RT Plan Description</summary>
		public static DicomTag RTPlanDescription = new DicomTag(0x300a, 0x0004);

		/// <summary>(300a,0006) VR=DA VM=1 RT Plan Date</summary>
		public static DicomTag RTPlanDate = new DicomTag(0x300a, 0x0006);

		/// <summary>(300a,0007) VR=TM VM=1 RT Plan Time</summary>
		public static DicomTag RTPlanTime = new DicomTag(0x300a, 0x0007);

		/// <summary>(300a,0009) VR=LO VM=1-n Treatment Protocols</summary>
		public static DicomTag TreatmentProtocols = new DicomTag(0x300a, 0x0009);

		/// <summary>(300a,000a) VR=CS VM=1 Plan Intent</summary>
		public static DicomTag PlanIntent = new DicomTag(0x300a, 0x000a);

		/// <summary>(300a,000b) VR=LO VM=1-n Treatment Sites</summary>
		public static DicomTag TreatmentSites = new DicomTag(0x300a, 0x000b);

		/// <summary>(300a,000c) VR=CS VM=1 RT Plan Geometry</summary>
		public static DicomTag RTPlanGeometry = new DicomTag(0x300a, 0x000c);

		/// <summary>(300a,000e) VR=ST VM=1 Prescription Description</summary>
		public static DicomTag PrescriptionDescription = new DicomTag(0x300a, 0x000e);

		/// <summary>(300a,0010) VR=SQ VM=1 Dose Reference Sequence</summary>
		public static DicomTag DoseReferenceSequence = new DicomTag(0x300a, 0x0010);

		/// <summary>(300a,0012) VR=IS VM=1 Dose Reference Number</summary>
		public static DicomTag DoseReferenceNumber = new DicomTag(0x300a, 0x0012);

		/// <summary>(300a,0013) VR=UI VM=1 Dose Reference UID</summary>
		public static DicomTag DoseReferenceUID = new DicomTag(0x300a, 0x0013);

		/// <summary>(300a,0014) VR=CS VM=1 Dose Reference Structure Type</summary>
		public static DicomTag DoseReferenceStructureType = new DicomTag(0x300a, 0x0014);

		/// <summary>(300a,0015) VR=CS VM=1 Nominal Beam Energy Unit</summary>
		public static DicomTag NominalBeamEnergyUnit = new DicomTag(0x300a, 0x0015);

		/// <summary>(300a,0016) VR=LO VM=1 Dose Reference Description</summary>
		public static DicomTag DoseReferenceDescription = new DicomTag(0x300a, 0x0016);

		/// <summary>(300a,0018) VR=DS VM=3 Dose Reference Point Coordinates</summary>
		public static DicomTag DoseReferencePointCoordinates = new DicomTag(0x300a, 0x0018);

		/// <summary>(300a,001a) VR=DS VM=1 Nominal Prior Dose</summary>
		public static DicomTag NominalPriorDose = new DicomTag(0x300a, 0x001a);

		/// <summary>(300a,0020) VR=CS VM=1 Dose Reference Type</summary>
		public static DicomTag DoseReferenceType = new DicomTag(0x300a, 0x0020);

		/// <summary>(300a,0021) VR=DS VM=1 Constraint Weight</summary>
		public static DicomTag ConstraintWeight = new DicomTag(0x300a, 0x0021);

		/// <summary>(300a,0022) VR=DS VM=1 Delivery Warning Dose</summary>
		public static DicomTag DeliveryWarningDose = new DicomTag(0x300a, 0x0022);

		/// <summary>(300a,0023) VR=DS VM=1 Delivery Maximum Dose</summary>
		public static DicomTag DeliveryMaximumDose = new DicomTag(0x300a, 0x0023);

		/// <summary>(300a,0025) VR=DS VM=1 Target Minimum Dose</summary>
		public static DicomTag TargetMinimumDose = new DicomTag(0x300a, 0x0025);

		/// <summary>(300a,0026) VR=DS VM=1 Target Prescription Dose</summary>
		public static DicomTag TargetPrescriptionDose = new DicomTag(0x300a, 0x0026);

		/// <summary>(300a,0027) VR=DS VM=1 Target Maximum Dose</summary>
		public static DicomTag TargetMaximumDose = new DicomTag(0x300a, 0x0027);

		/// <summary>(300a,0028) VR=DS VM=1 Target Underdose Volume Fraction</summary>
		public static DicomTag TargetUnderdoseVolumeFraction = new DicomTag(0x300a, 0x0028);

		/// <summary>(300a,002a) VR=DS VM=1 Organ at Risk Full-volume Dose</summary>
		public static DicomTag OrganAtRiskFullvolumeDose = new DicomTag(0x300a, 0x002a);

		/// <summary>(300a,002b) VR=DS VM=1 Organ at Risk Limit Dose</summary>
		public static DicomTag OrganAtRiskLimitDose = new DicomTag(0x300a, 0x002b);

		/// <summary>(300a,002c) VR=DS VM=1 Organ at Risk Maximum Dose</summary>
		public static DicomTag OrganAtRiskMaximumDose = new DicomTag(0x300a, 0x002c);

		/// <summary>(300a,002d) VR=DS VM=1 Organ at Risk Overdose Volume Fraction</summary>
		public static DicomTag OrganAtRiskOverdoseVolumeFraction = new DicomTag(0x300a, 0x002d);

		/// <summary>(300a,0040) VR=SQ VM=1 Tolerance Table Sequence</summary>
		public static DicomTag ToleranceTableSequence = new DicomTag(0x300a, 0x0040);

		/// <summary>(300a,0042) VR=IS VM=1 Tolerance Table Number</summary>
		public static DicomTag ToleranceTableNumber = new DicomTag(0x300a, 0x0042);

		/// <summary>(300a,0043) VR=SH VM=1 Tolerance Table Label</summary>
		public static DicomTag ToleranceTableLabel = new DicomTag(0x300a, 0x0043);

		/// <summary>(300a,0044) VR=DS VM=1 Gantry Angle Tolerance</summary>
		public static DicomTag GantryAngleTolerance = new DicomTag(0x300a, 0x0044);

		/// <summary>(300a,0046) VR=DS VM=1 Beam Limiting Device Angle Tolerance</summary>
		public static DicomTag BeamLimitingDeviceAngleTolerance = new DicomTag(0x300a, 0x0046);

		/// <summary>(300a,0048) VR=SQ VM=1 Beam Limiting Device Tolerance Sequence</summary>
		public static DicomTag BeamLimitingDeviceToleranceSequence = new DicomTag(0x300a, 0x0048);

		/// <summary>(300a,004a) VR=DS VM=1 Beam Limiting Device Position Tolerance</summary>
		public static DicomTag BeamLimitingDevicePositionTolerance = new DicomTag(0x300a, 0x004a);

		/// <summary>(300a,004b) VR=FL VM=1 Snout Position Tolerance</summary>
		public static DicomTag SnoutPositionTolerance = new DicomTag(0x300a, 0x004b);

		/// <summary>(300a,004c) VR=DS VM=1 Patient Support Angle Tolerance</summary>
		public static DicomTag PatientSupportAngleTolerance = new DicomTag(0x300a, 0x004c);

		/// <summary>(300a,004e) VR=DS VM=1 Table Top Eccentric Angle Tolerance</summary>
		public static DicomTag TableTopEccentricAngleTolerance = new DicomTag(0x300a, 0x004e);

		/// <summary>(300a,004f) VR=FL VM=1 Table Top Pitch Angle Tolerance</summary>
		public static DicomTag TableTopPitchAngleTolerance = new DicomTag(0x300a, 0x004f);

		/// <summary>(300a,0050) VR=FL VM=1 Table Top Roll Angle Tolerance</summary>
		public static DicomTag TableTopRollAngleTolerance = new DicomTag(0x300a, 0x0050);

		/// <summary>(300a,0051) VR=DS VM=1 Table Top Vertical Position Tolerance</summary>
		public static DicomTag TableTopVerticalPositionTolerance = new DicomTag(0x300a, 0x0051);

		/// <summary>(300a,0052) VR=DS VM=1 Table Top Longitudinal Position Tolerance</summary>
		public static DicomTag TableTopLongitudinalPositionTolerance = new DicomTag(0x300a, 0x0052);

		/// <summary>(300a,0053) VR=DS VM=1 Table Top Lateral Position Tolerance</summary>
		public static DicomTag TableTopLateralPositionTolerance = new DicomTag(0x300a, 0x0053);

		/// <summary>(300a,0055) VR=CS VM=1 RT Plan Relationship</summary>
		public static DicomTag RTPlanRelationship = new DicomTag(0x300a, 0x0055);

		/// <summary>(300a,0070) VR=SQ VM=1 Fraction Group Sequence</summary>
		public static DicomTag FractionGroupSequence = new DicomTag(0x300a, 0x0070);

		/// <summary>(300a,0071) VR=IS VM=1 Fraction Group Number</summary>
		public static DicomTag FractionGroupNumber = new DicomTag(0x300a, 0x0071);

		/// <summary>(300a,0072) VR=LO VM=1 Fraction Group Description</summary>
		public static DicomTag FractionGroupDescription = new DicomTag(0x300a, 0x0072);

		/// <summary>(300a,0078) VR=IS VM=1 Number of Fractions Planned</summary>
		public static DicomTag NumberOfFractionsPlanned = new DicomTag(0x300a, 0x0078);

		/// <summary>(300a,0079) VR=IS VM=1 Number of Fraction Pattern Digits Per Day</summary>
		public static DicomTag NumberOfFractionPatternDigitsPerDay = new DicomTag(0x300a, 0x0079);

		/// <summary>(300a,007a) VR=IS VM=1 Repeat Fraction Cycle Length</summary>
		public static DicomTag RepeatFractionCycleLength = new DicomTag(0x300a, 0x007a);

		/// <summary>(300a,007b) VR=LT VM=1 Fraction Pattern</summary>
		public static DicomTag FractionPattern = new DicomTag(0x300a, 0x007b);

		/// <summary>(300a,0080) VR=IS VM=1 Number of Beams</summary>
		public static DicomTag NumberOfBeams = new DicomTag(0x300a, 0x0080);

		/// <summary>(300a,0082) VR=DS VM=3 Beam Dose Specification Point</summary>
		public static DicomTag BeamDoseSpecificationPoint = new DicomTag(0x300a, 0x0082);

		/// <summary>(300a,0084) VR=DS VM=1 Beam Dose</summary>
		public static DicomTag BeamDose = new DicomTag(0x300a, 0x0084);

		/// <summary>(300a,0086) VR=DS VM=1 Beam Meterset</summary>
		public static DicomTag BeamMeterset = new DicomTag(0x300a, 0x0086);

		/// <summary>(300a,0088) VR=FL VM=1 Beam Dose Point Depth</summary>
		public static DicomTag BeamDosePointDepth = new DicomTag(0x300a, 0x0088);

		/// <summary>(300a,0089) VR=FL VM=1 Beam Dose Point Equivalent Depth</summary>
		public static DicomTag BeamDosePointEquivalentDepth = new DicomTag(0x300a, 0x0089);

		/// <summary>(300a,008a) VR=FL VM=1 Beam Dose Point SSD</summary>
		public static DicomTag BeamDosePointSSD = new DicomTag(0x300a, 0x008a);

		/// <summary>(300a,00a0) VR=IS VM=1 Number of Brachy Application Setups</summary>
		public static DicomTag NumberOfBrachyApplicationSetups = new DicomTag(0x300a, 0x00a0);

		/// <summary>(300a,00a2) VR=DS VM=3 Brachy Application Setup Dose Specification Point</summary>
		public static DicomTag BrachyApplicationSetupDoseSpecificationPoint = new DicomTag(0x300a, 0x00a2);

		/// <summary>(300a,00a4) VR=DS VM=1 Brachy Application Setup Dose</summary>
		public static DicomTag BrachyApplicationSetupDose = new DicomTag(0x300a, 0x00a4);

		/// <summary>(300a,00b0) VR=SQ VM=1 Beam Sequence</summary>
		public static DicomTag BeamSequence = new DicomTag(0x300a, 0x00b0);

		/// <summary>(300a,00b2) VR=SH VM=1 Treatment Machine Name</summary>
		public static DicomTag TreatmentMachineName = new DicomTag(0x300a, 0x00b2);

		/// <summary>(300a,00b3) VR=CS VM=1 Primary Dosimeter Unit</summary>
		public static DicomTag PrimaryDosimeterUnit = new DicomTag(0x300a, 0x00b3);

		/// <summary>(300a,00b4) VR=DS VM=1 Source-Axis Distance</summary>
		public static DicomTag SourceAxisDistance = new DicomTag(0x300a, 0x00b4);

		/// <summary>(300a,00b6) VR=SQ VM=1 Beam Limiting Device Sequence</summary>
		public static DicomTag BeamLimitingDeviceSequence = new DicomTag(0x300a, 0x00b6);

		/// <summary>(300a,00b8) VR=CS VM=1 RT Beam Limiting Device Type</summary>
		public static DicomTag RTBeamLimitingDeviceType = new DicomTag(0x300a, 0x00b8);

		/// <summary>(300a,00ba) VR=DS VM=1 Source to Beam Limiting Device Distance</summary>
		public static DicomTag SourceToBeamLimitingDeviceDistance = new DicomTag(0x300a, 0x00ba);

		/// <summary>(300a,00bb) VR=FL VM=1 Isocenter to Beam Limiting Device Distance</summary>
		public static DicomTag IsocenterToBeamLimitingDeviceDistance = new DicomTag(0x300a, 0x00bb);

		/// <summary>(300a,00bc) VR=IS VM=1 Number of Leaf/Jaw Pairs</summary>
		public static DicomTag NumberOfLeafJawPairs = new DicomTag(0x300a, 0x00bc);

		/// <summary>(300a,00be) VR=DS VM=3-n Leaf Position Boundaries</summary>
		public static DicomTag LeafPositionBoundaries = new DicomTag(0x300a, 0x00be);

		/// <summary>(300a,00c0) VR=IS VM=1 Beam Number</summary>
		public static DicomTag BeamNumber = new DicomTag(0x300a, 0x00c0);

		/// <summary>(300a,00c2) VR=LO VM=1 Beam Name</summary>
		public static DicomTag BeamName = new DicomTag(0x300a, 0x00c2);

		/// <summary>(300a,00c3) VR=ST VM=1 Beam Description</summary>
		public static DicomTag BeamDescription = new DicomTag(0x300a, 0x00c3);

		/// <summary>(300a,00c4) VR=CS VM=1 Beam Type</summary>
		public static DicomTag BeamType = new DicomTag(0x300a, 0x00c4);

		/// <summary>(300a,00c6) VR=CS VM=1 Radiation Type</summary>
		public static DicomTag RadiationType = new DicomTag(0x300a, 0x00c6);

		/// <summary>(300a,00c7) VR=CS VM=1 High-Dose Technique Type</summary>
		public static DicomTag HighDoseTechniqueType = new DicomTag(0x300a, 0x00c7);

		/// <summary>(300a,00c8) VR=IS VM=1 Reference Image Number</summary>
		public static DicomTag ReferenceImageNumber = new DicomTag(0x300a, 0x00c8);

		/// <summary>(300a,00ca) VR=SQ VM=1 Planned Verification Image Sequence</summary>
		public static DicomTag PlannedVerificationImageSequence = new DicomTag(0x300a, 0x00ca);

		/// <summary>(300a,00cc) VR=LO VM=1-n Imaging Device-Specific Acquisition Parameters</summary>
		public static DicomTag ImagingDeviceSpecificAcquisitionParameters = new DicomTag(0x300a, 0x00cc);

		/// <summary>(300a,00ce) VR=CS VM=1 Treatment Delivery Type</summary>
		public static DicomTag TreatmentDeliveryType = new DicomTag(0x300a, 0x00ce);

		/// <summary>(300a,00d0) VR=IS VM=1 Number of Wedges</summary>
		public static DicomTag NumberOfWedges = new DicomTag(0x300a, 0x00d0);

		/// <summary>(300a,00d1) VR=SQ VM=1 Wedge Sequence</summary>
		public static DicomTag WedgeSequence = new DicomTag(0x300a, 0x00d1);

		/// <summary>(300a,00d2) VR=IS VM=1 Wedge Number</summary>
		public static DicomTag WedgeNumber = new DicomTag(0x300a, 0x00d2);

		/// <summary>(300a,00d3) VR=CS VM=1 Wedge Type</summary>
		public static DicomTag WedgeType = new DicomTag(0x300a, 0x00d3);

		/// <summary>(300a,00d4) VR=SH VM=1 Wedge ID</summary>
		public static DicomTag WedgeID = new DicomTag(0x300a, 0x00d4);

		/// <summary>(300a,00d5) VR=IS VM=1 Wedge Angle</summary>
		public static DicomTag WedgeAngle = new DicomTag(0x300a, 0x00d5);

		/// <summary>(300a,00d6) VR=DS VM=1 Wedge Factor</summary>
		public static DicomTag WedgeFactor = new DicomTag(0x300a, 0x00d6);

		/// <summary>(300a,00d7) VR=FL VM=1 Total Wedge Tray Water-Equivalent Thickness</summary>
		public static DicomTag TotalWedgeTrayWaterEquivalentThickness = new DicomTag(0x300a, 0x00d7);

		/// <summary>(300a,00d8) VR=DS VM=1 Wedge Orientation</summary>
		public static DicomTag WedgeOrientation = new DicomTag(0x300a, 0x00d8);

		/// <summary>(300a,00d9) VR=FL VM=1 Isocenter to Wedge Tray Distance</summary>
		public static DicomTag IsocenterToWedgeTrayDistance = new DicomTag(0x300a, 0x00d9);

		/// <summary>(300a,00da) VR=DS VM=1 Source to Wedge Tray Distance</summary>
		public static DicomTag SourceToWedgeTrayDistance = new DicomTag(0x300a, 0x00da);

		/// <summary>(300a,00db) VR=FL VM=1 Wedge Thin Edge Position</summary>
		public static DicomTag WedgeThinEdgePosition = new DicomTag(0x300a, 0x00db);

		/// <summary>(300a,00dc) VR=SH VM=1 Bolus ID</summary>
		public static DicomTag BolusID = new DicomTag(0x300a, 0x00dc);

		/// <summary>(300a,00dd) VR=ST VM=1 Bolus Description</summary>
		public static DicomTag BolusDescription = new DicomTag(0x300a, 0x00dd);

		/// <summary>(300a,00e0) VR=IS VM=1 Number of Compensators</summary>
		public static DicomTag NumberOfCompensators = new DicomTag(0x300a, 0x00e0);

		/// <summary>(300a,00e1) VR=SH VM=1 Material ID</summary>
		public static DicomTag MaterialID = new DicomTag(0x300a, 0x00e1);

		/// <summary>(300a,00e2) VR=DS VM=1 Total Compensator Tray Factor</summary>
		public static DicomTag TotalCompensatorTrayFactor = new DicomTag(0x300a, 0x00e2);

		/// <summary>(300a,00e3) VR=SQ VM=1 Compensator Sequence</summary>
		public static DicomTag CompensatorSequence = new DicomTag(0x300a, 0x00e3);

		/// <summary>(300a,00e4) VR=IS VM=1 Compensator Number</summary>
		public static DicomTag CompensatorNumber = new DicomTag(0x300a, 0x00e4);

		/// <summary>(300a,00e5) VR=SH VM=1 Compensator ID</summary>
		public static DicomTag CompensatorID = new DicomTag(0x300a, 0x00e5);

		/// <summary>(300a,00e6) VR=DS VM=1 Source to Compensator Tray Distance</summary>
		public static DicomTag SourceToCompensatorTrayDistance = new DicomTag(0x300a, 0x00e6);

		/// <summary>(300a,00e7) VR=IS VM=1 Compensator Rows</summary>
		public static DicomTag CompensatorRows = new DicomTag(0x300a, 0x00e7);

		/// <summary>(300a,00e8) VR=IS VM=1 Compensator Columns</summary>
		public static DicomTag CompensatorColumns = new DicomTag(0x300a, 0x00e8);

		/// <summary>(300a,00e9) VR=DS VM=2 Compensator Pixel Spacing</summary>
		public static DicomTag CompensatorPixelSpacing = new DicomTag(0x300a, 0x00e9);

		/// <summary>(300a,00ea) VR=DS VM=2 Compensator Position</summary>
		public static DicomTag CompensatorPosition = new DicomTag(0x300a, 0x00ea);

		/// <summary>(300a,00eb) VR=DS VM=1-n Compensator Transmission Data</summary>
		public static DicomTag CompensatorTransmissionData = new DicomTag(0x300a, 0x00eb);

		/// <summary>(300a,00ec) VR=DS VM=1-n Compensator Thickness Data</summary>
		public static DicomTag CompensatorThicknessData = new DicomTag(0x300a, 0x00ec);

		/// <summary>(300a,00ed) VR=IS VM=1 Number of Boli</summary>
		public static DicomTag NumberOfBoli = new DicomTag(0x300a, 0x00ed);

		/// <summary>(300a,00ee) VR=CS VM=1 Compensator Type</summary>
		public static DicomTag CompensatorType = new DicomTag(0x300a, 0x00ee);

		/// <summary>(300a,00f0) VR=IS VM=1 Number of Blocks</summary>
		public static DicomTag NumberOfBlocks = new DicomTag(0x300a, 0x00f0);

		/// <summary>(300a,00f2) VR=DS VM=1 Total Block Tray Factor</summary>
		public static DicomTag TotalBlockTrayFactor = new DicomTag(0x300a, 0x00f2);

		/// <summary>(300a,00f3) VR=FL VM=1 Total Block Tray Water-Equivalent Thickness</summary>
		public static DicomTag TotalBlockTrayWaterEquivalentThickness = new DicomTag(0x300a, 0x00f3);

		/// <summary>(300a,00f4) VR=SQ VM=1 Block Sequence</summary>
		public static DicomTag BlockSequence = new DicomTag(0x300a, 0x00f4);

		/// <summary>(300a,00f5) VR=SH VM=1 Block Tray ID</summary>
		public static DicomTag BlockTrayID = new DicomTag(0x300a, 0x00f5);

		/// <summary>(300a,00f6) VR=DS VM=1 Source to Block Tray Distance</summary>
		public static DicomTag SourceToBlockTrayDistance = new DicomTag(0x300a, 0x00f6);

		/// <summary>(300a,00f7) VR=FL VM=1 Isocenter to Block Tray Distance</summary>
		public static DicomTag IsocenterToBlockTrayDistance = new DicomTag(0x300a, 0x00f7);

		/// <summary>(300a,00f8) VR=CS VM=1 Block Type</summary>
		public static DicomTag BlockType = new DicomTag(0x300a, 0x00f8);

		/// <summary>(300a,00f9) VR=LO VM=1 Accessory Code</summary>
		public static DicomTag AccessoryCode = new DicomTag(0x300a, 0x00f9);

		/// <summary>(300a,00fa) VR=CS VM=1 Block Divergence</summary>
		public static DicomTag BlockDivergence = new DicomTag(0x300a, 0x00fa);

		/// <summary>(300a,00fb) VR=CS VM=1 Block Mounting Position</summary>
		public static DicomTag BlockMountingPosition = new DicomTag(0x300a, 0x00fb);

		/// <summary>(300a,00fc) VR=IS VM=1 Block Number</summary>
		public static DicomTag BlockNumber = new DicomTag(0x300a, 0x00fc);

		/// <summary>(300a,00fe) VR=LO VM=1 Block Name</summary>
		public static DicomTag BlockName = new DicomTag(0x300a, 0x00fe);

		/// <summary>(300a,0100) VR=DS VM=1 Block Thickness</summary>
		public static DicomTag BlockThickness = new DicomTag(0x300a, 0x0100);

		/// <summary>(300a,0102) VR=DS VM=1 Block Transmission</summary>
		public static DicomTag BlockTransmission = new DicomTag(0x300a, 0x0102);

		/// <summary>(300a,0104) VR=IS VM=1 Block Number of Points</summary>
		public static DicomTag BlockNumberOfPoints = new DicomTag(0x300a, 0x0104);

		/// <summary>(300a,0106) VR=DS VM=2-2n Block Data</summary>
		public static DicomTag BlockData = new DicomTag(0x300a, 0x0106);

		/// <summary>(300a,0107) VR=SQ VM=1 Applicator Sequence</summary>
		public static DicomTag ApplicatorSequence = new DicomTag(0x300a, 0x0107);

		/// <summary>(300a,0108) VR=SH VM=1 Applicator ID</summary>
		public static DicomTag ApplicatorID = new DicomTag(0x300a, 0x0108);

		/// <summary>(300a,0109) VR=CS VM=1 Applicator Type</summary>
		public static DicomTag ApplicatorType = new DicomTag(0x300a, 0x0109);

		/// <summary>(300a,010a) VR=LO VM=1 Applicator Description</summary>
		public static DicomTag ApplicatorDescription = new DicomTag(0x300a, 0x010a);

		/// <summary>(300a,010c) VR=DS VM=1 Cumulative Dose Reference Coefficient</summary>
		public static DicomTag CumulativeDoseReferenceCoefficient = new DicomTag(0x300a, 0x010c);

		/// <summary>(300a,010e) VR=DS VM=1 Final Cumulative Meterset Weight</summary>
		public static DicomTag FinalCumulativeMetersetWeight = new DicomTag(0x300a, 0x010e);

		/// <summary>(300a,0110) VR=IS VM=1 Number of Control Points</summary>
		public static DicomTag NumberOfControlPoints = new DicomTag(0x300a, 0x0110);

		/// <summary>(300a,0111) VR=SQ VM=1 Control Point Sequence</summary>
		public static DicomTag ControlPointSequence = new DicomTag(0x300a, 0x0111);

		/// <summary>(300a,0112) VR=IS VM=1 Control Point Index</summary>
		public static DicomTag ControlPointIndex = new DicomTag(0x300a, 0x0112);

		/// <summary>(300a,0114) VR=DS VM=1 Nominal Beam Energy</summary>
		public static DicomTag NominalBeamEnergy = new DicomTag(0x300a, 0x0114);

		/// <summary>(300a,0115) VR=DS VM=1 Dose Rate Set</summary>
		public static DicomTag DoseRateSet = new DicomTag(0x300a, 0x0115);

		/// <summary>(300a,0116) VR=SQ VM=1 Wedge Position Sequence</summary>
		public static DicomTag WedgePositionSequence = new DicomTag(0x300a, 0x0116);

		/// <summary>(300a,0118) VR=CS VM=1 Wedge Position</summary>
		public static DicomTag WedgePosition = new DicomTag(0x300a, 0x0118);

		/// <summary>(300a,011a) VR=SQ VM=1 Beam Limiting Device Position Sequence</summary>
		public static DicomTag BeamLimitingDevicePositionSequence = new DicomTag(0x300a, 0x011a);

		/// <summary>(300a,011c) VR=DS VM=2-2n Leaf/Jaw Positions</summary>
		public static DicomTag LeafJawPositions = new DicomTag(0x300a, 0x011c);

		/// <summary>(300a,011e) VR=DS VM=1 Gantry Angle</summary>
		public static DicomTag GantryAngle = new DicomTag(0x300a, 0x011e);

		/// <summary>(300a,011f) VR=CS VM=1 Gantry Rotation Direction</summary>
		public static DicomTag GantryRotationDirection = new DicomTag(0x300a, 0x011f);

		/// <summary>(300a,0120) VR=DS VM=1 Beam Limiting Device Angle</summary>
		public static DicomTag BeamLimitingDeviceAngle = new DicomTag(0x300a, 0x0120);

		/// <summary>(300a,0121) VR=CS VM=1 Beam Limiting Device Rotation Direction</summary>
		public static DicomTag BeamLimitingDeviceRotationDirection = new DicomTag(0x300a, 0x0121);

		/// <summary>(300a,0122) VR=DS VM=1 Patient Support Angle</summary>
		public static DicomTag PatientSupportAngle = new DicomTag(0x300a, 0x0122);

		/// <summary>(300a,0123) VR=CS VM=1 Patient Support Rotation Direction</summary>
		public static DicomTag PatientSupportRotationDirection = new DicomTag(0x300a, 0x0123);

		/// <summary>(300a,0124) VR=DS VM=1 Table Top Eccentric Axis Distance</summary>
		public static DicomTag TableTopEccentricAxisDistance = new DicomTag(0x300a, 0x0124);

		/// <summary>(300a,0125) VR=DS VM=1 Table Top Eccentric Angle</summary>
		public static DicomTag TableTopEccentricAngle = new DicomTag(0x300a, 0x0125);

		/// <summary>(300a,0126) VR=CS VM=1 Table Top Eccentric Rotation Direction</summary>
		public static DicomTag TableTopEccentricRotationDirection = new DicomTag(0x300a, 0x0126);

		/// <summary>(300a,0128) VR=DS VM=1 Table Top Vertical Position</summary>
		public static DicomTag TableTopVerticalPosition = new DicomTag(0x300a, 0x0128);

		/// <summary>(300a,0129) VR=DS VM=1 Table Top Longitudinal Position</summary>
		public static DicomTag TableTopLongitudinalPosition = new DicomTag(0x300a, 0x0129);

		/// <summary>(300a,012a) VR=DS VM=1 Table Top Lateral Position</summary>
		public static DicomTag TableTopLateralPosition = new DicomTag(0x300a, 0x012a);

		/// <summary>(300a,012c) VR=DS VM=3 Isocenter Position</summary>
		public static DicomTag IsocenterPosition = new DicomTag(0x300a, 0x012c);

		/// <summary>(300a,012e) VR=DS VM=3 Surface Entry Point</summary>
		public static DicomTag SurfaceEntryPoint = new DicomTag(0x300a, 0x012e);

		/// <summary>(300a,0130) VR=DS VM=1 Source to Surface Distance</summary>
		public static DicomTag SourceToSurfaceDistance = new DicomTag(0x300a, 0x0130);

		/// <summary>(300a,0134) VR=DS VM=1 Cumulative Meterset Weight</summary>
		public static DicomTag CumulativeMetersetWeight = new DicomTag(0x300a, 0x0134);

		/// <summary>(300a,0140) VR=FL VM=1 Table Top Pitch Angle</summary>
		public static DicomTag TableTopPitchAngle = new DicomTag(0x300a, 0x0140);

		/// <summary>(300a,0142) VR=CS VM=1 Table Top Pitch Rotation Direction</summary>
		public static DicomTag TableTopPitchRotationDirection = new DicomTag(0x300a, 0x0142);

		/// <summary>(300a,0144) VR=FL VM=1 Table Top Roll Angle</summary>
		public static DicomTag TableTopRollAngle = new DicomTag(0x300a, 0x0144);

		/// <summary>(300a,0146) VR=CS VM=1 Table Top Roll Rotation Direction</summary>
		public static DicomTag TableTopRollRotationDirection = new DicomTag(0x300a, 0x0146);

		/// <summary>(300a,0148) VR=FL VM=1 Head Fixation Angle</summary>
		public static DicomTag HeadFixationAngle = new DicomTag(0x300a, 0x0148);

		/// <summary>(300a,014a) VR=FL VM=1 Gantry Pitch Angle</summary>
		public static DicomTag GantryPitchAngle = new DicomTag(0x300a, 0x014a);

		/// <summary>(300a,014c) VR=CS VM=1 Gantry Pitch Rotation Direction</summary>
		public static DicomTag GantryPitchRotationDirection = new DicomTag(0x300a, 0x014c);

		/// <summary>(300a,014e) VR=FL VM=1 Gantry Pitch Angle Tolerance</summary>
		public static DicomTag GantryPitchAngleTolerance = new DicomTag(0x300a, 0x014e);

		/// <summary>(300a,0180) VR=SQ VM=1 Patient Setup Sequence</summary>
		public static DicomTag PatientSetupSequence = new DicomTag(0x300a, 0x0180);

		/// <summary>(300a,0182) VR=IS VM=1 Patient Setup Number</summary>
		public static DicomTag PatientSetupNumber = new DicomTag(0x300a, 0x0182);

		/// <summary>(300a,0183) VR=LO VM=1 Patient Setup Label</summary>
		public static DicomTag PatientSetupLabel = new DicomTag(0x300a, 0x0183);

		/// <summary>(300a,0184) VR=LO VM=1 Patient Additional Position</summary>
		public static DicomTag PatientAdditionalPosition = new DicomTag(0x300a, 0x0184);

		/// <summary>(300a,0190) VR=SQ VM=1 Fixation Device Sequence</summary>
		public static DicomTag FixationDeviceSequence = new DicomTag(0x300a, 0x0190);

		/// <summary>(300a,0192) VR=CS VM=1 Fixation Device Type</summary>
		public static DicomTag FixationDeviceType = new DicomTag(0x300a, 0x0192);

		/// <summary>(300a,0194) VR=SH VM=1 Fixation Device Label</summary>
		public static DicomTag FixationDeviceLabel = new DicomTag(0x300a, 0x0194);

		/// <summary>(300a,0196) VR=ST VM=1 Fixation Device Description</summary>
		public static DicomTag FixationDeviceDescription = new DicomTag(0x300a, 0x0196);

		/// <summary>(300a,0198) VR=SH VM=1 Fixation Device Position</summary>
		public static DicomTag FixationDevicePosition = new DicomTag(0x300a, 0x0198);

		/// <summary>(300a,0199) VR=FL VM=1 Fixation Device Pitch Angle</summary>
		public static DicomTag FixationDevicePitchAngle = new DicomTag(0x300a, 0x0199);

		/// <summary>(300a,019a) VR=FL VM=1 Fixation Device Roll Angle</summary>
		public static DicomTag FixationDeviceRollAngle = new DicomTag(0x300a, 0x019a);

		/// <summary>(300a,01a0) VR=SQ VM=1 Shielding Device Sequence</summary>
		public static DicomTag ShieldingDeviceSequence = new DicomTag(0x300a, 0x01a0);

		/// <summary>(300a,01a2) VR=CS VM=1 Shielding Device Type</summary>
		public static DicomTag ShieldingDeviceType = new DicomTag(0x300a, 0x01a2);

		/// <summary>(300a,01a4) VR=SH VM=1 Shielding Device Label</summary>
		public static DicomTag ShieldingDeviceLabel = new DicomTag(0x300a, 0x01a4);

		/// <summary>(300a,01a6) VR=ST VM=1 Shielding Device Description</summary>
		public static DicomTag ShieldingDeviceDescription = new DicomTag(0x300a, 0x01a6);

		/// <summary>(300a,01a8) VR=SH VM=1 Shielding Device Position</summary>
		public static DicomTag ShieldingDevicePosition = new DicomTag(0x300a, 0x01a8);

		/// <summary>(300a,01b0) VR=CS VM=1 Setup Technique</summary>
		public static DicomTag SetupTechnique = new DicomTag(0x300a, 0x01b0);

		/// <summary>(300a,01b2) VR=ST VM=1 Setup Technique Description</summary>
		public static DicomTag SetupTechniqueDescription = new DicomTag(0x300a, 0x01b2);

		/// <summary>(300a,01b4) VR=SQ VM=1 Setup Device Sequence</summary>
		public static DicomTag SetupDeviceSequence = new DicomTag(0x300a, 0x01b4);

		/// <summary>(300a,01b6) VR=CS VM=1 Setup Device Type</summary>
		public static DicomTag SetupDeviceType = new DicomTag(0x300a, 0x01b6);

		/// <summary>(300a,01b8) VR=SH VM=1 Setup Device Label</summary>
		public static DicomTag SetupDeviceLabel = new DicomTag(0x300a, 0x01b8);

		/// <summary>(300a,01ba) VR=ST VM=1 Setup Device Description</summary>
		public static DicomTag SetupDeviceDescription = new DicomTag(0x300a, 0x01ba);

		/// <summary>(300a,01bc) VR=DS VM=1 Setup Device Parameter</summary>
		public static DicomTag SetupDeviceParameter = new DicomTag(0x300a, 0x01bc);

		/// <summary>(300a,01d0) VR=ST VM=1 Setup Reference Description</summary>
		public static DicomTag SetupReferenceDescription = new DicomTag(0x300a, 0x01d0);

		/// <summary>(300a,01d2) VR=DS VM=1 Table Top Vertical Setup Displacement</summary>
		public static DicomTag TableTopVerticalSetupDisplacement = new DicomTag(0x300a, 0x01d2);

		/// <summary>(300a,01d4) VR=DS VM=1 Table Top Longitudinal Setup Displacement</summary>
		public static DicomTag TableTopLongitudinalSetupDisplacement = new DicomTag(0x300a, 0x01d4);

		/// <summary>(300a,01d6) VR=DS VM=1 Table Top Lateral Setup Displacement</summary>
		public static DicomTag TableTopLateralSetupDisplacement = new DicomTag(0x300a, 0x01d6);

		/// <summary>(300a,0200) VR=CS VM=1 Brachy Treatment Technique</summary>
		public static DicomTag BrachyTreatmentTechnique = new DicomTag(0x300a, 0x0200);

		/// <summary>(300a,0202) VR=CS VM=1 Brachy Treatment Type</summary>
		public static DicomTag BrachyTreatmentType = new DicomTag(0x300a, 0x0202);

		/// <summary>(300a,0206) VR=SQ VM=1 Treatment Machine Sequence</summary>
		public static DicomTag TreatmentMachineSequence = new DicomTag(0x300a, 0x0206);

		/// <summary>(300a,0210) VR=SQ VM=1 Source Sequence</summary>
		public static DicomTag SourceSequence = new DicomTag(0x300a, 0x0210);

		/// <summary>(300a,0212) VR=IS VM=1 Source Number</summary>
		public static DicomTag SourceNumber = new DicomTag(0x300a, 0x0212);

		/// <summary>(300a,0214) VR=CS VM=1 Source Type</summary>
		public static DicomTag SourceType = new DicomTag(0x300a, 0x0214);

		/// <summary>(300a,0216) VR=LO VM=1 Source Manufacturer</summary>
		public static DicomTag SourceManufacturer = new DicomTag(0x300a, 0x0216);

		/// <summary>(300a,0218) VR=DS VM=1 Active Source Diameter</summary>
		public static DicomTag ActiveSourceDiameter = new DicomTag(0x300a, 0x0218);

		/// <summary>(300a,021a) VR=DS VM=1 Active Source Length</summary>
		public static DicomTag ActiveSourceLength = new DicomTag(0x300a, 0x021a);

		/// <summary>(300a,0222) VR=DS VM=1 Source Encapsulation Nominal Thickness</summary>
		public static DicomTag SourceEncapsulationNominalThickness = new DicomTag(0x300a, 0x0222);

		/// <summary>(300a,0224) VR=DS VM=1 Source Encapsulation Nominal Transmission</summary>
		public static DicomTag SourceEncapsulationNominalTransmission = new DicomTag(0x300a, 0x0224);

		/// <summary>(300a,0226) VR=LO VM=1 Source Isotope Name</summary>
		public static DicomTag SourceIsotopeName = new DicomTag(0x300a, 0x0226);

		/// <summary>(300a,0228) VR=DS VM=1 Source Isotope Half Life</summary>
		public static DicomTag SourceIsotopeHalfLife = new DicomTag(0x300a, 0x0228);

		/// <summary>(300a,0229) VR=CS VM=1 Source Strength Units</summary>
		public static DicomTag SourceStrengthUnits = new DicomTag(0x300a, 0x0229);

		/// <summary>(300a,022a) VR=DS VM=1 Reference Air Kerma Rate</summary>
		public static DicomTag ReferenceAirKermaRate = new DicomTag(0x300a, 0x022a);

		/// <summary>(300a,022b) VR=DS VM=1 Source Strength</summary>
		public static DicomTag SourceStrength = new DicomTag(0x300a, 0x022b);

		/// <summary>(300a,022c) VR=DA VM=1 Source Strength Reference Date</summary>
		public static DicomTag SourceStrengthReferenceDate = new DicomTag(0x300a, 0x022c);

		/// <summary>(300a,022e) VR=TM VM=1 Source Strength Reference Time</summary>
		public static DicomTag SourceStrengthReferenceTime = new DicomTag(0x300a, 0x022e);

		/// <summary>(300a,0230) VR=SQ VM=1 Application Setup Sequence</summary>
		public static DicomTag ApplicationSetupSequence = new DicomTag(0x300a, 0x0230);

		/// <summary>(300a,0232) VR=CS VM=1 Application Setup Type</summary>
		public static DicomTag ApplicationSetupType = new DicomTag(0x300a, 0x0232);

		/// <summary>(300a,0234) VR=IS VM=1 Application Setup Number</summary>
		public static DicomTag ApplicationSetupNumber = new DicomTag(0x300a, 0x0234);

		/// <summary>(300a,0236) VR=LO VM=1 Application Setup Name</summary>
		public static DicomTag ApplicationSetupName = new DicomTag(0x300a, 0x0236);

		/// <summary>(300a,0238) VR=LO VM=1 Application Setup Manufacturer</summary>
		public static DicomTag ApplicationSetupManufacturer = new DicomTag(0x300a, 0x0238);

		/// <summary>(300a,0240) VR=IS VM=1 Template Number</summary>
		public static DicomTag TemplateNumber = new DicomTag(0x300a, 0x0240);

		/// <summary>(300a,0242) VR=SH VM=1 Template Type</summary>
		public static DicomTag TemplateType = new DicomTag(0x300a, 0x0242);

		/// <summary>(300a,0244) VR=LO VM=1 Template Name</summary>
		public static DicomTag TemplateName = new DicomTag(0x300a, 0x0244);

		/// <summary>(300a,0250) VR=DS VM=1 Total Reference Air Kerma</summary>
		public static DicomTag TotalReferenceAirKerma = new DicomTag(0x300a, 0x0250);

		/// <summary>(300a,0260) VR=SQ VM=1 Brachy Accessory Device Sequence</summary>
		public static DicomTag BrachyAccessoryDeviceSequence = new DicomTag(0x300a, 0x0260);

		/// <summary>(300a,0262) VR=IS VM=1 Brachy Accessory Device Number</summary>
		public static DicomTag BrachyAccessoryDeviceNumber = new DicomTag(0x300a, 0x0262);

		/// <summary>(300a,0263) VR=SH VM=1 Brachy Accessory Device ID</summary>
		public static DicomTag BrachyAccessoryDeviceID = new DicomTag(0x300a, 0x0263);

		/// <summary>(300a,0264) VR=CS VM=1 Brachy Accessory Device Type</summary>
		public static DicomTag BrachyAccessoryDeviceType = new DicomTag(0x300a, 0x0264);

		/// <summary>(300a,0266) VR=LO VM=1 Brachy Accessory Device Name</summary>
		public static DicomTag BrachyAccessoryDeviceName = new DicomTag(0x300a, 0x0266);

		/// <summary>(300a,026a) VR=DS VM=1 Brachy Accessory Device Nominal Thickness</summary>
		public static DicomTag BrachyAccessoryDeviceNominalThickness = new DicomTag(0x300a, 0x026a);

		/// <summary>(300a,026c) VR=DS VM=1 Brachy Accessory Device Nominal Transmission</summary>
		public static DicomTag BrachyAccessoryDeviceNominalTransmission = new DicomTag(0x300a, 0x026c);

		/// <summary>(300a,0280) VR=SQ VM=1 Channel Sequence</summary>
		public static DicomTag ChannelSequence = new DicomTag(0x300a, 0x0280);

		/// <summary>(300a,0282) VR=IS VM=1 Channel Number</summary>
		public static DicomTag ChannelNumber = new DicomTag(0x300a, 0x0282);

		/// <summary>(300a,0284) VR=DS VM=1 Channel Length</summary>
		public static DicomTag ChannelLength = new DicomTag(0x300a, 0x0284);

		/// <summary>(300a,0286) VR=DS VM=1 Channel Total Time</summary>
		public static DicomTag ChannelTotalTime = new DicomTag(0x300a, 0x0286);

		/// <summary>(300a,0288) VR=CS VM=1 Source Movement Type</summary>
		public static DicomTag SourceMovementType = new DicomTag(0x300a, 0x0288);

		/// <summary>(300a,028a) VR=IS VM=1 Number of Pulses</summary>
		public static DicomTag NumberOfPulses = new DicomTag(0x300a, 0x028a);

		/// <summary>(300a,028c) VR=DS VM=1 Pulse Repetition Interval</summary>
		public static DicomTag PulseRepetitionInterval = new DicomTag(0x300a, 0x028c);

		/// <summary>(300a,0290) VR=IS VM=1 Source Applicator Number</summary>
		public static DicomTag SourceApplicatorNumber = new DicomTag(0x300a, 0x0290);

		/// <summary>(300a,0291) VR=SH VM=1 Source Applicator ID</summary>
		public static DicomTag SourceApplicatorID = new DicomTag(0x300a, 0x0291);

		/// <summary>(300a,0292) VR=CS VM=1 Source Applicator Type</summary>
		public static DicomTag SourceApplicatorType = new DicomTag(0x300a, 0x0292);

		/// <summary>(300a,0294) VR=LO VM=1 Source Applicator Name</summary>
		public static DicomTag SourceApplicatorName = new DicomTag(0x300a, 0x0294);

		/// <summary>(300a,0296) VR=DS VM=1 Source Applicator Length</summary>
		public static DicomTag SourceApplicatorLength = new DicomTag(0x300a, 0x0296);

		/// <summary>(300a,0298) VR=LO VM=1 Source Applicator Manufacturer</summary>
		public static DicomTag SourceApplicatorManufacturer = new DicomTag(0x300a, 0x0298);

		/// <summary>(300a,029c) VR=DS VM=1 Source Applicator Wall Nominal Thickness</summary>
		public static DicomTag SourceApplicatorWallNominalThickness = new DicomTag(0x300a, 0x029c);

		/// <summary>(300a,029e) VR=DS VM=1 Source Applicator Wall Nominal Transmission</summary>
		public static DicomTag SourceApplicatorWallNominalTransmission = new DicomTag(0x300a, 0x029e);

		/// <summary>(300a,02a0) VR=DS VM=1 Source Applicator Step Size</summary>
		public static DicomTag SourceApplicatorStepSize = new DicomTag(0x300a, 0x02a0);

		/// <summary>(300a,02a2) VR=IS VM=1 Transfer Tube Number</summary>
		public static DicomTag TransferTubeNumber = new DicomTag(0x300a, 0x02a2);

		/// <summary>(300a,02a4) VR=DS VM=1 Transfer Tube Length</summary>
		public static DicomTag TransferTubeLength = new DicomTag(0x300a, 0x02a4);

		/// <summary>(300a,02b0) VR=SQ VM=1 Channel Shield Sequence</summary>
		public static DicomTag ChannelShieldSequence = new DicomTag(0x300a, 0x02b0);

		/// <summary>(300a,02b2) VR=IS VM=1 Channel Shield Number</summary>
		public static DicomTag ChannelShieldNumber = new DicomTag(0x300a, 0x02b2);

		/// <summary>(300a,02b3) VR=SH VM=1 Channel Shield ID</summary>
		public static DicomTag ChannelShieldID = new DicomTag(0x300a, 0x02b3);

		/// <summary>(300a,02b4) VR=LO VM=1 Channel Shield Name</summary>
		public static DicomTag ChannelShieldName = new DicomTag(0x300a, 0x02b4);

		/// <summary>(300a,02b8) VR=DS VM=1 Channel Shield Nominal Thickness</summary>
		public static DicomTag ChannelShieldNominalThickness = new DicomTag(0x300a, 0x02b8);

		/// <summary>(300a,02ba) VR=DS VM=1 Channel Shield Nominal Transmission</summary>
		public static DicomTag ChannelShieldNominalTransmission = new DicomTag(0x300a, 0x02ba);

		/// <summary>(300a,02c8) VR=DS VM=1 Final Cumulative Time Weight</summary>
		public static DicomTag FinalCumulativeTimeWeight = new DicomTag(0x300a, 0x02c8);

		/// <summary>(300a,02d0) VR=SQ VM=1 Brachy Control Point Sequence</summary>
		public static DicomTag BrachyControlPointSequence = new DicomTag(0x300a, 0x02d0);

		/// <summary>(300a,02d2) VR=DS VM=1 Control Point Relative Position</summary>
		public static DicomTag ControlPointRelativePosition = new DicomTag(0x300a, 0x02d2);

		/// <summary>(300a,02d4) VR=DS VM=3 Control Point 3D Position</summary>
		public static DicomTag ControlPoint3DPosition = new DicomTag(0x300a, 0x02d4);

		/// <summary>(300a,02d6) VR=DS VM=1 Cumulative Time Weight</summary>
		public static DicomTag CumulativeTimeWeight = new DicomTag(0x300a, 0x02d6);

		/// <summary>(300a,02e0) VR=CS VM=1 Compensator Divergence</summary>
		public static DicomTag CompensatorDivergence = new DicomTag(0x300a, 0x02e0);

		/// <summary>(300a,02e1) VR=CS VM=1 Compensator Mounting Position</summary>
		public static DicomTag CompensatorMountingPosition = new DicomTag(0x300a, 0x02e1);

		/// <summary>(300a,02e2) VR=DS VM=1-n Source to Compensator Distance</summary>
		public static DicomTag SourceToCompensatorDistance = new DicomTag(0x300a, 0x02e2);

		/// <summary>(300a,02e3) VR=FL VM=1 Total Compensator Tray Water-Equivalent Thickness</summary>
		public static DicomTag TotalCompensatorTrayWaterEquivalentThickness = new DicomTag(0x300a, 0x02e3);

		/// <summary>(300a,02e4) VR=FL VM=1 Isocenter to Compensator Tray Distance</summary>
		public static DicomTag IsocenterToCompensatorTrayDistance = new DicomTag(0x300a, 0x02e4);

		/// <summary>(300a,02e5) VR=FL VM=1 Compensator Column Offset</summary>
		public static DicomTag CompensatorColumnOffset = new DicomTag(0x300a, 0x02e5);

		/// <summary>(300a,02e6) VR=FL VM=1-n Isocenter to Compensator Distances</summary>
		public static DicomTag IsocenterToCompensatorDistances = new DicomTag(0x300a, 0x02e6);

		/// <summary>(300a,02e7) VR=FL VM=1 Compensator Relative Stopping Power Ratio</summary>
		public static DicomTag CompensatorRelativeStoppingPowerRatio = new DicomTag(0x300a, 0x02e7);

		/// <summary>(300a,02e8) VR=FL VM=1 Compensator Milling Tool Diameter</summary>
		public static DicomTag CompensatorMillingToolDiameter = new DicomTag(0x300a, 0x02e8);

		/// <summary>(300a,02ea) VR=SQ VM=1 Ion Range Compensator Sequence</summary>
		public static DicomTag IonRangeCompensatorSequence = new DicomTag(0x300a, 0x02ea);

		/// <summary>(300a,02eb) VR=LT VM=1 Compensator Description</summary>
		public static DicomTag CompensatorDescription = new DicomTag(0x300a, 0x02eb);

		/// <summary>(300a,0302) VR=IS VM=1 Radiation Mass Number</summary>
		public static DicomTag RadiationMassNumber = new DicomTag(0x300a, 0x0302);

		/// <summary>(300a,0304) VR=IS VM=1 Radiation Atomic Number</summary>
		public static DicomTag RadiationAtomicNumber = new DicomTag(0x300a, 0x0304);

		/// <summary>(300a,0306) VR=SS VM=1 Radiation Charge State</summary>
		public static DicomTag RadiationChargeState = new DicomTag(0x300a, 0x0306);

		/// <summary>(300a,0308) VR=CS VM=1 Scan Mode</summary>
		public static DicomTag ScanMode = new DicomTag(0x300a, 0x0308);

		/// <summary>(300a,030a) VR=FL VM=2 Virtual Source-Axis Distances</summary>
		public static DicomTag VirtualSourceAxisDistances = new DicomTag(0x300a, 0x030a);

		/// <summary>(300a,030c) VR=SQ VM=1 Snout Sequence</summary>
		public static DicomTag SnoutSequence = new DicomTag(0x300a, 0x030c);

		/// <summary>(300a,030d) VR=FL VM=1 Snout Position</summary>
		public static DicomTag SnoutPosition = new DicomTag(0x300a, 0x030d);

		/// <summary>(300a,030f) VR=SH VM=1 Snout ID</summary>
		public static DicomTag SnoutID = new DicomTag(0x300a, 0x030f);

		/// <summary>(300a,0312) VR=IS VM=1 Number of Range Shifters</summary>
		public static DicomTag NumberOfRangeShifters = new DicomTag(0x300a, 0x0312);

		/// <summary>(300a,0314) VR=SQ VM=1 Range Shifter Sequence</summary>
		public static DicomTag RangeShifterSequence = new DicomTag(0x300a, 0x0314);

		/// <summary>(300a,0316) VR=IS VM=1 Range Shifter Number</summary>
		public static DicomTag RangeShifterNumber = new DicomTag(0x300a, 0x0316);

		/// <summary>(300a,0318) VR=SH VM=1 Range Shifter ID</summary>
		public static DicomTag RangeShifterID = new DicomTag(0x300a, 0x0318);

		/// <summary>(300a,0320) VR=CS VM=1 Range Shifter Type</summary>
		public static DicomTag RangeShifterType = new DicomTag(0x300a, 0x0320);

		/// <summary>(300a,0322) VR=LO VM=1 Range Shifter Description</summary>
		public static DicomTag RangeShifterDescription = new DicomTag(0x300a, 0x0322);

		/// <summary>(300a,0330) VR=IS VM=1 Number of Lateral Spreading Devices</summary>
		public static DicomTag NumberOfLateralSpreadingDevices = new DicomTag(0x300a, 0x0330);

		/// <summary>(300a,0332) VR=SQ VM=1 Lateral Spreading Device Sequence</summary>
		public static DicomTag LateralSpreadingDeviceSequence = new DicomTag(0x300a, 0x0332);

		/// <summary>(300a,0334) VR=IS VM=1 Lateral Spreading Device Number</summary>
		public static DicomTag LateralSpreadingDeviceNumber = new DicomTag(0x300a, 0x0334);

		/// <summary>(300a,0336) VR=SH VM=1 Lateral Spreading Device ID</summary>
		public static DicomTag LateralSpreadingDeviceID = new DicomTag(0x300a, 0x0336);

		/// <summary>(300a,0338) VR=CS VM=1 Lateral Spreading Device Type</summary>
		public static DicomTag LateralSpreadingDeviceType = new DicomTag(0x300a, 0x0338);

		/// <summary>(300a,033a) VR=LO VM=1 Lateral Spreading Device Description</summary>
		public static DicomTag LateralSpreadingDeviceDescription = new DicomTag(0x300a, 0x033a);

		/// <summary>(300a,033c) VR=FL VM=1 Lateral Spreading Device Water Equivalent Thickness</summary>
		public static DicomTag LateralSpreadingDeviceWaterEquivalentThickness = new DicomTag(0x300a, 0x033c);

		/// <summary>(300a,0340) VR=IS VM=1 Number of Range Modulators</summary>
		public static DicomTag NumberOfRangeModulators = new DicomTag(0x300a, 0x0340);

		/// <summary>(300a,0342) VR=SQ VM=1 Range Modulator Sequence</summary>
		public static DicomTag RangeModulatorSequence = new DicomTag(0x300a, 0x0342);

		/// <summary>(300a,0344) VR=IS VM=1 Range Modulator Number</summary>
		public static DicomTag RangeModulatorNumber = new DicomTag(0x300a, 0x0344);

		/// <summary>(300a,0346) VR=SH VM=1 Range Modulator ID</summary>
		public static DicomTag RangeModulatorID = new DicomTag(0x300a, 0x0346);

		/// <summary>(300a,0348) VR=CS VM=1 Range Modulator Type</summary>
		public static DicomTag RangeModulatorType = new DicomTag(0x300a, 0x0348);

		/// <summary>(300a,034a) VR=LO VM=1 Range Modulator Description</summary>
		public static DicomTag RangeModulatorDescription = new DicomTag(0x300a, 0x034a);

		/// <summary>(300a,034c) VR=SH VM=1 Beam Current Modulation ID</summary>
		public static DicomTag BeamCurrentModulationID = new DicomTag(0x300a, 0x034c);

		/// <summary>(300a,0350) VR=CS VM=1 Patient Support Type</summary>
		public static DicomTag PatientSupportType = new DicomTag(0x300a, 0x0350);

		/// <summary>(300a,0352) VR=SH VM=1 Patient Support ID</summary>
		public static DicomTag PatientSupportID = new DicomTag(0x300a, 0x0352);

		/// <summary>(300a,0354) VR=LO VM=1 Patient Support Accessory Code</summary>
		public static DicomTag PatientSupportAccessoryCode = new DicomTag(0x300a, 0x0354);

		/// <summary>(300a,0356) VR=FL VM=1 Fixation Light Azimuthal Angle</summary>
		public static DicomTag FixationLightAzimuthalAngle = new DicomTag(0x300a, 0x0356);

		/// <summary>(300a,0358) VR=FL VM=1 Fixation Light Polar Angle</summary>
		public static DicomTag FixationLightPolarAngle = new DicomTag(0x300a, 0x0358);

		/// <summary>(300a,035a) VR=FL VM=1 Meterset Rate</summary>
		public static DicomTag MetersetRate = new DicomTag(0x300a, 0x035a);

		/// <summary>(300a,0360) VR=SQ VM=1 Range Shifter Settings Sequence</summary>
		public static DicomTag RangeShifterSettingsSequence = new DicomTag(0x300a, 0x0360);

		/// <summary>(300a,0362) VR=LO VM=1 Range Shifter Setting</summary>
		public static DicomTag RangeShifterSetting = new DicomTag(0x300a, 0x0362);

		/// <summary>(300a,0364) VR=FL VM=1 Isocenter to Range Shifter Distance</summary>
		public static DicomTag IsocenterToRangeShifterDistance = new DicomTag(0x300a, 0x0364);

		/// <summary>(300a,0366) VR=FL VM=1 Range Shifter Water Equivalent Thickness</summary>
		public static DicomTag RangeShifterWaterEquivalentThickness = new DicomTag(0x300a, 0x0366);

		/// <summary>(300a,0370) VR=SQ VM=1 Lateral Spreading Device Settings Sequence</summary>
		public static DicomTag LateralSpreadingDeviceSettingsSequence = new DicomTag(0x300a, 0x0370);

		/// <summary>(300a,0372) VR=LO VM=1 Lateral Spreading Device Setting</summary>
		public static DicomTag LateralSpreadingDeviceSetting = new DicomTag(0x300a, 0x0372);

		/// <summary>(300a,0374) VR=FL VM=1 Isocenter to Lateral Spreading Device Distance</summary>
		public static DicomTag IsocenterToLateralSpreadingDeviceDistance = new DicomTag(0x300a, 0x0374);

		/// <summary>(300a,0380) VR=SQ VM=1 Range Modulator Settings Sequence</summary>
		public static DicomTag RangeModulatorSettingsSequence = new DicomTag(0x300a, 0x0380);

		/// <summary>(300a,0382) VR=FL VM=1 Range Modulator Gating Start Value</summary>
		public static DicomTag RangeModulatorGatingStartValue = new DicomTag(0x300a, 0x0382);

		/// <summary>(300a,0384) VR=FL VM=1 Range Modulator Gating Stop Value</summary>
		public static DicomTag RangeModulatorGatingStopValue = new DicomTag(0x300a, 0x0384);

		/// <summary>(300a,0386) VR=FL VM=1 Range Modulator Gating Start Water Equivalent Thickness</summary>
		public static DicomTag RangeModulatorGatingStartWaterEquivalentThickness = new DicomTag(0x300a, 0x0386);

		/// <summary>(300a,0388) VR=FL VM=1 Range Modulator Gating Stop Water Equivalent Thickness</summary>
		public static DicomTag RangeModulatorGatingStopWaterEquivalentThickness = new DicomTag(0x300a, 0x0388);

		/// <summary>(300a,038a) VR=FL VM=1 Isocenter to Range Modulator Distance</summary>
		public static DicomTag IsocenterToRangeModulatorDistance = new DicomTag(0x300a, 0x038a);

		/// <summary>(300a,0390) VR=SH VM=1 Scan Spot Tune ID</summary>
		public static DicomTag ScanSpotTuneID = new DicomTag(0x300a, 0x0390);

		/// <summary>(300a,0392) VR=IS VM=1 Number of Scan Spot Positions</summary>
		public static DicomTag NumberOfScanSpotPositions = new DicomTag(0x300a, 0x0392);

		/// <summary>(300a,0394) VR=FL VM=1-n Scan Spot Position Map</summary>
		public static DicomTag ScanSpotPositionMap = new DicomTag(0x300a, 0x0394);

		/// <summary>(300a,0396) VR=FL VM=1-n Scan Spot Meterset Weights</summary>
		public static DicomTag ScanSpotMetersetWeights = new DicomTag(0x300a, 0x0396);

		/// <summary>(300a,0398) VR=FL VM=2 Scanning Spot Size</summary>
		public static DicomTag ScanningSpotSize = new DicomTag(0x300a, 0x0398);

		/// <summary>(300a,039a) VR=IS VM=1 Number of Paintings</summary>
		public static DicomTag NumberOfPaintings = new DicomTag(0x300a, 0x039a);

		/// <summary>(300a,03a0) VR=SQ VM=1 Ion Tolerance Table Sequence</summary>
		public static DicomTag IonToleranceTableSequence = new DicomTag(0x300a, 0x03a0);

		/// <summary>(300a,03a2) VR=SQ VM=1 Ion Beam Sequence</summary>
		public static DicomTag IonBeamSequence = new DicomTag(0x300a, 0x03a2);

		/// <summary>(300a,03a4) VR=SQ VM=1 Ion Beam Limiting Device Sequence</summary>
		public static DicomTag IonBeamLimitingDeviceSequence = new DicomTag(0x300a, 0x03a4);

		/// <summary>(300a,03a6) VR=SQ VM=1 Ion Block Sequence</summary>
		public static DicomTag IonBlockSequence = new DicomTag(0x300a, 0x03a6);

		/// <summary>(300a,03a8) VR=SQ VM=1 Ion Control Point Sequence</summary>
		public static DicomTag IonControlPointSequence = new DicomTag(0x300a, 0x03a8);

		/// <summary>(300a,03aa) VR=SQ VM=1 Ion Wedge Sequence</summary>
		public static DicomTag IonWedgeSequence = new DicomTag(0x300a, 0x03aa);

		/// <summary>(300a,03ac) VR=SQ VM=1 Ion Wedge Position Sequence</summary>
		public static DicomTag IonWedgePositionSequence = new DicomTag(0x300a, 0x03ac);

		/// <summary>(300a,0401) VR=SQ VM=1 Referenced Setup Image Sequence</summary>
		public static DicomTag ReferencedSetupImageSequence = new DicomTag(0x300a, 0x0401);

		/// <summary>(300a,0402) VR=ST VM=1 Setup Image Comment</summary>
		public static DicomTag SetupImageComment = new DicomTag(0x300a, 0x0402);

		/// <summary>(300a,0410) VR=SQ VM=1 Motion Synchronization Sequence</summary>
		public static DicomTag MotionSynchronizationSequence = new DicomTag(0x300a, 0x0410);

		/// <summary>(300a,0412) VR=FL VM=3 Control Point Orientation</summary>
		public static DicomTag ControlPointOrientation = new DicomTag(0x300a, 0x0412);

		/// <summary>(300a,0420) VR=SQ VM=1 General Accessory Sequence</summary>
		public static DicomTag GeneralAccessorySequence = new DicomTag(0x300a, 0x0420);

		/// <summary>(300a,0421) VR=CS VM=1 General Accessory ID</summary>
		public static DicomTag GeneralAccessoryID = new DicomTag(0x300a, 0x0421);

		/// <summary>(300a,0422) VR=ST VM=1 General Accessory Description</summary>
		public static DicomTag GeneralAccessoryDescription = new DicomTag(0x300a, 0x0422);

		/// <summary>(300a,0423) VR=SH VM=1 General Accessory Type</summary>
		public static DicomTag GeneralAccessoryType = new DicomTag(0x300a, 0x0423);

		/// <summary>(300a,0424) VR=IS VM=1 General Accessory Number</summary>
		public static DicomTag GeneralAccessoryNumber = new DicomTag(0x300a, 0x0424);

		/// <summary>(300c,0002) VR=SQ VM=1 Referenced RT Plan Sequence</summary>
		public static DicomTag ReferencedRTPlanSequence = new DicomTag(0x300c, 0x0002);

		/// <summary>(300c,0004) VR=SQ VM=1 Referenced Beam Sequence</summary>
		public static DicomTag ReferencedBeamSequence = new DicomTag(0x300c, 0x0004);

		/// <summary>(300c,0006) VR=IS VM=1 Referenced Beam Number</summary>
		public static DicomTag ReferencedBeamNumber = new DicomTag(0x300c, 0x0006);

		/// <summary>(300c,0007) VR=IS VM=1 Referenced Reference Image Number</summary>
		public static DicomTag ReferencedReferenceImageNumber = new DicomTag(0x300c, 0x0007);

		/// <summary>(300c,0008) VR=DS VM=1 Start Cumulative Meterset Weight</summary>
		public static DicomTag StartCumulativeMetersetWeight = new DicomTag(0x300c, 0x0008);

		/// <summary>(300c,0009) VR=DS VM=1 End Cumulative Meterset Weight</summary>
		public static DicomTag EndCumulativeMetersetWeight = new DicomTag(0x300c, 0x0009);

		/// <summary>(300c,000a) VR=SQ VM=1 Referenced Brachy Application Setup Sequence</summary>
		public static DicomTag ReferencedBrachyApplicationSetupSequence = new DicomTag(0x300c, 0x000a);

		/// <summary>(300c,000c) VR=IS VM=1 Referenced Brachy Application Setup Number</summary>
		public static DicomTag ReferencedBrachyApplicationSetupNumber = new DicomTag(0x300c, 0x000c);

		/// <summary>(300c,000e) VR=IS VM=1 Referenced Source Number</summary>
		public static DicomTag ReferencedSourceNumber = new DicomTag(0x300c, 0x000e);

		/// <summary>(300c,0020) VR=SQ VM=1 Referenced Fraction Group Sequence</summary>
		public static DicomTag ReferencedFractionGroupSequence = new DicomTag(0x300c, 0x0020);

		/// <summary>(300c,0022) VR=IS VM=1 Referenced Fraction Group Number</summary>
		public static DicomTag ReferencedFractionGroupNumber = new DicomTag(0x300c, 0x0022);

		/// <summary>(300c,0040) VR=SQ VM=1 Referenced Verification Image Sequence</summary>
		public static DicomTag ReferencedVerificationImageSequence = new DicomTag(0x300c, 0x0040);

		/// <summary>(300c,0042) VR=SQ VM=1 Referenced Reference Image Sequence</summary>
		public static DicomTag ReferencedReferenceImageSequence = new DicomTag(0x300c, 0x0042);

		/// <summary>(300c,0050) VR=SQ VM=1 Referenced Dose Reference Sequence</summary>
		public static DicomTag ReferencedDoseReferenceSequence = new DicomTag(0x300c, 0x0050);

		/// <summary>(300c,0051) VR=IS VM=1 Referenced Dose Reference Number</summary>
		public static DicomTag ReferencedDoseReferenceNumber = new DicomTag(0x300c, 0x0051);

		/// <summary>(300c,0055) VR=SQ VM=1 Brachy Referenced Dose Reference Sequence</summary>
		public static DicomTag BrachyReferencedDoseReferenceSequence = new DicomTag(0x300c, 0x0055);

		/// <summary>(300c,0060) VR=SQ VM=1 Referenced Structure Set Sequence</summary>
		public static DicomTag ReferencedStructureSetSequence = new DicomTag(0x300c, 0x0060);

		/// <summary>(300c,006a) VR=IS VM=1 Referenced Patient Setup Number</summary>
		public static DicomTag ReferencedPatientSetupNumber = new DicomTag(0x300c, 0x006a);

		/// <summary>(300c,0080) VR=SQ VM=1 Referenced Dose Sequence</summary>
		public static DicomTag ReferencedDoseSequence = new DicomTag(0x300c, 0x0080);

		/// <summary>(300c,00a0) VR=IS VM=1 Referenced Tolerance Table Number</summary>
		public static DicomTag ReferencedToleranceTableNumber = new DicomTag(0x300c, 0x00a0);

		/// <summary>(300c,00b0) VR=SQ VM=1 Referenced Bolus Sequence</summary>
		public static DicomTag ReferencedBolusSequence = new DicomTag(0x300c, 0x00b0);

		/// <summary>(300c,00c0) VR=IS VM=1 Referenced Wedge Number</summary>
		public static DicomTag ReferencedWedgeNumber = new DicomTag(0x300c, 0x00c0);

		/// <summary>(300c,00d0) VR=IS VM=1 Referenced Compensator Number</summary>
		public static DicomTag ReferencedCompensatorNumber = new DicomTag(0x300c, 0x00d0);

		/// <summary>(300c,00e0) VR=IS VM=1 Referenced Block Number</summary>
		public static DicomTag ReferencedBlockNumber = new DicomTag(0x300c, 0x00e0);

		/// <summary>(300c,00f0) VR=IS VM=1 Referenced Control Point Index</summary>
		public static DicomTag ReferencedControlPointIndex = new DicomTag(0x300c, 0x00f0);

		/// <summary>(300c,00f2) VR=SQ VM=1 Referenced Control Point Sequence</summary>
		public static DicomTag ReferencedControlPointSequence = new DicomTag(0x300c, 0x00f2);

		/// <summary>(300c,00f4) VR=IS VM=1 Referenced Start Control Point Index</summary>
		public static DicomTag ReferencedStartControlPointIndex = new DicomTag(0x300c, 0x00f4);

		/// <summary>(300c,00f6) VR=IS VM=1 Referenced Stop Control Point Index</summary>
		public static DicomTag ReferencedStopControlPointIndex = new DicomTag(0x300c, 0x00f6);

		/// <summary>(300c,0100) VR=IS VM=1 Referenced Range Shifter Number</summary>
		public static DicomTag ReferencedRangeShifterNumber = new DicomTag(0x300c, 0x0100);

		/// <summary>(300c,0102) VR=IS VM=1 Referenced Lateral Spreading Device Number</summary>
		public static DicomTag ReferencedLateralSpreadingDeviceNumber = new DicomTag(0x300c, 0x0102);

		/// <summary>(300c,0104) VR=IS VM=1 Referenced Range Modulator Number</summary>
		public static DicomTag ReferencedRangeModulatorNumber = new DicomTag(0x300c, 0x0104);

		/// <summary>(300e,0002) VR=CS VM=1 Approval Status</summary>
		public static DicomTag ApprovalStatus = new DicomTag(0x300e, 0x0002);

		/// <summary>(300e,0004) VR=DA VM=1 Review Date</summary>
		public static DicomTag ReviewDate = new DicomTag(0x300e, 0x0004);

		/// <summary>(300e,0005) VR=TM VM=1 Review Time</summary>
		public static DicomTag ReviewTime = new DicomTag(0x300e, 0x0005);

		/// <summary>(300e,0008) VR=PN VM=1 Reviewer Name</summary>
		public static DicomTag ReviewerName = new DicomTag(0x300e, 0x0008);

		/// <summary>(4000,0010) VR=LT VM=1 Arbitrary (Retired)</summary>
		public static DicomTag ArbitraryRETIRED = new DicomTag(0x4000, 0x0010);

		/// <summary>(4000,4000) VR=LT VM=1 Text Comments (Retired)</summary>
		public static DicomTag TextCommentsRETIRED = new DicomTag(0x4000, 0x4000);

		/// <summary>(4008,0040) VR=SH VM=1 Results ID (Retired)</summary>
		public static DicomTag ResultsIDRETIRED = new DicomTag(0x4008, 0x0040);

		/// <summary>(4008,0042) VR=LO VM=1 Results ID Issuer (Retired)</summary>
		public static DicomTag ResultsIDIssuerRETIRED = new DicomTag(0x4008, 0x0042);

		/// <summary>(4008,0050) VR=SQ VM=1 Referenced Interpretation Sequence (Retired)</summary>
		public static DicomTag ReferencedInterpretationSequenceRETIRED = new DicomTag(0x4008, 0x0050);

		/// <summary>(4008,0100) VR=DA VM=1 Interpretation Recorded Date (Retired)</summary>
		public static DicomTag InterpretationRecordedDateRETIRED = new DicomTag(0x4008, 0x0100);

		/// <summary>(4008,0101) VR=TM VM=1 Interpretation Recorded Time (Retired)</summary>
		public static DicomTag InterpretationRecordedTimeRETIRED = new DicomTag(0x4008, 0x0101);

		/// <summary>(4008,0102) VR=PN VM=1 Interpretation Recorder (Retired)</summary>
		public static DicomTag InterpretationRecorderRETIRED = new DicomTag(0x4008, 0x0102);

		/// <summary>(4008,0103) VR=LO VM=1 Reference to Recorded Sound (Retired)</summary>
		public static DicomTag ReferenceToRecordedSoundRETIRED = new DicomTag(0x4008, 0x0103);

		/// <summary>(4008,0108) VR=DA VM=1 Interpretation Transcription Date (Retired)</summary>
		public static DicomTag InterpretationTranscriptionDateRETIRED = new DicomTag(0x4008, 0x0108);

		/// <summary>(4008,0109) VR=TM VM=1 Interpretation Transcription Time (Retired)</summary>
		public static DicomTag InterpretationTranscriptionTimeRETIRED = new DicomTag(0x4008, 0x0109);

		/// <summary>(4008,010a) VR=PN VM=1 Interpretation Transcriber (Retired)</summary>
		public static DicomTag InterpretationTranscriberRETIRED = new DicomTag(0x4008, 0x010a);

		/// <summary>(4008,010b) VR=ST VM=1 Interpretation Text (Retired)</summary>
		public static DicomTag InterpretationTextRETIRED = new DicomTag(0x4008, 0x010b);

		/// <summary>(4008,010c) VR=PN VM=1 Interpretation Author (Retired)</summary>
		public static DicomTag InterpretationAuthorRETIRED = new DicomTag(0x4008, 0x010c);

		/// <summary>(4008,0111) VR=SQ VM=1 Interpretation Approver Sequence (Retired)</summary>
		public static DicomTag InterpretationApproverSequenceRETIRED = new DicomTag(0x4008, 0x0111);

		/// <summary>(4008,0112) VR=DA VM=1 Interpretation Approval Date (Retired)</summary>
		public static DicomTag InterpretationApprovalDateRETIRED = new DicomTag(0x4008, 0x0112);

		/// <summary>(4008,0113) VR=TM VM=1 Interpretation Approval Time (Retired)</summary>
		public static DicomTag InterpretationApprovalTimeRETIRED = new DicomTag(0x4008, 0x0113);

		/// <summary>(4008,0114) VR=PN VM=1 Physician Approving Interpretation (Retired)</summary>
		public static DicomTag PhysicianApprovingInterpretationRETIRED = new DicomTag(0x4008, 0x0114);

		/// <summary>(4008,0115) VR=LT VM=1 Interpretation Diagnosis Description (Retired)</summary>
		public static DicomTag InterpretationDiagnosisDescriptionRETIRED = new DicomTag(0x4008, 0x0115);

		/// <summary>(4008,0117) VR=SQ VM=1 Interpretation Diagnosis Code Sequence (Retired)</summary>
		public static DicomTag InterpretationDiagnosisCodeSequenceRETIRED = new DicomTag(0x4008, 0x0117);

		/// <summary>(4008,0118) VR=SQ VM=1 Results Distribution List Sequence (Retired)</summary>
		public static DicomTag ResultsDistributionListSequenceRETIRED = new DicomTag(0x4008, 0x0118);

		/// <summary>(4008,0119) VR=PN VM=1 Distribution Name (Retired)</summary>
		public static DicomTag DistributionNameRETIRED = new DicomTag(0x4008, 0x0119);

		/// <summary>(4008,011a) VR=LO VM=1 Distribution Address (Retired)</summary>
		public static DicomTag DistributionAddressRETIRED = new DicomTag(0x4008, 0x011a);

		/// <summary>(4008,0200) VR=SH VM=1 Interpretation ID (Retired)</summary>
		public static DicomTag InterpretationIDRETIRED = new DicomTag(0x4008, 0x0200);

		/// <summary>(4008,0202) VR=LO VM=1 Interpretation ID Issuer (Retired)</summary>
		public static DicomTag InterpretationIDIssuerRETIRED = new DicomTag(0x4008, 0x0202);

		/// <summary>(4008,0210) VR=CS VM=1 Interpretation Type ID (Retired)</summary>
		public static DicomTag InterpretationTypeIDRETIRED = new DicomTag(0x4008, 0x0210);

		/// <summary>(4008,0212) VR=CS VM=1 Interpretation Status ID (Retired)</summary>
		public static DicomTag InterpretationStatusIDRETIRED = new DicomTag(0x4008, 0x0212);

		/// <summary>(4008,0300) VR=ST VM=1 Impressions (Retired)</summary>
		public static DicomTag ImpressionsRETIRED = new DicomTag(0x4008, 0x0300);

		/// <summary>(4008,4000) VR=ST VM=1 Results Comments (Retired)</summary>
		public static DicomTag ResultsCommentsRETIRED = new DicomTag(0x4008, 0x4000);

		/// <summary>(4ffe,0001) VR=SQ VM=1 MAC Parameters Sequence</summary>
		public static DicomTag MACParametersSequence = new DicomTag(0x4ffe, 0x0001);

		/// <summary>(5000,0005) VR=US VM=1 Curve Dimensions (Retired)</summary>
		public static DicomTag CurveDimensionsRETIRED = new DicomTag(0x5000, 0x0005);

		/// <summary>(5000,0010) VR=US VM=1 Number of Points (Retired)</summary>
		public static DicomTag NumberOfPointsRETIRED = new DicomTag(0x5000, 0x0010);

		/// <summary>(5000,0020) VR=CS VM=1 Type of Data (Retired)</summary>
		public static DicomTag TypeOfDataRETIRED = new DicomTag(0x5000, 0x0020);

		/// <summary>(5000,0022) VR=LO VM=1 Curve Description (Retired)</summary>
		public static DicomTag CurveDescriptionRETIRED = new DicomTag(0x5000, 0x0022);

		/// <summary>(5000,0030) VR=SH VM=1-n Axis Units (Retired)</summary>
		public static DicomTag AxisUnitsRETIRED = new DicomTag(0x5000, 0x0030);

		/// <summary>(5000,0040) VR=SH VM=1-n Axis Labels (Retired)</summary>
		public static DicomTag AxisLabelsRETIRED = new DicomTag(0x5000, 0x0040);

		/// <summary>(5000,0103) VR=US VM=1 Data Value Representation (Retired)</summary>
		public static DicomTag DataValueRepresentationRETIRED = new DicomTag(0x5000, 0x0103);

		/// <summary>(5000,0104) VR=US VM=1-n Minimum Coordinate Value (Retired)</summary>
		public static DicomTag MinimumCoordinateValueRETIRED = new DicomTag(0x5000, 0x0104);

		/// <summary>(5000,0105) VR=US VM=1-n Maximum Coordinate Value (Retired)</summary>
		public static DicomTag MaximumCoordinateValueRETIRED = new DicomTag(0x5000, 0x0105);

		/// <summary>(5000,0106) VR=SH VM=1-n Curve Range (Retired)</summary>
		public static DicomTag CurveRangeRETIRED = new DicomTag(0x5000, 0x0106);

		/// <summary>(5000,0110) VR=US VM=1-n Curve Data Descriptor (Retired)</summary>
		public static DicomTag CurveDataDescriptorRETIRED = new DicomTag(0x5000, 0x0110);

		/// <summary>(5000,0112) VR=US VM=1-n Coordinate Start Value (Retired)</summary>
		public static DicomTag CoordinateStartValueRETIRED = new DicomTag(0x5000, 0x0112);

		/// <summary>(5000,0114) VR=US VM=1-n Coordinate Step Value (Retired)</summary>
		public static DicomTag CoordinateStepValueRETIRED = new DicomTag(0x5000, 0x0114);

		/// <summary>(5000,1001) VR=CS VM=1 Curve Activation Layer (Retired)</summary>
		public static DicomTag CurveActivationLayerRETIRED = new DicomTag(0x5000, 0x1001);

		/// <summary>(5000,2000) VR=US VM=1 Audio Type (Retired)</summary>
		public static DicomTag AudioTypeRETIRED = new DicomTag(0x5000, 0x2000);

		/// <summary>(5000,2002) VR=US VM=1 Audio Sample Format (Retired)</summary>
		public static DicomTag AudioSampleFormatRETIRED = new DicomTag(0x5000, 0x2002);

		/// <summary>(5000,2004) VR=US VM=1 Number of Channels (Retired)</summary>
		public static DicomTag NumberOfChannelsRETIRED = new DicomTag(0x5000, 0x2004);

		/// <summary>(5000,2006) VR=UL VM=1 Number of Samples (Retired)</summary>
		public static DicomTag NumberOfSamplesRETIRED = new DicomTag(0x5000, 0x2006);

		/// <summary>(5000,2008) VR=UL VM=1 Sample Rate (Retired)</summary>
		public static DicomTag SampleRateRETIRED = new DicomTag(0x5000, 0x2008);

		/// <summary>(5000,200a) VR=UL VM=1 Total Time (Retired)</summary>
		public static DicomTag TotalTimeRETIRED = new DicomTag(0x5000, 0x200a);

		/// <summary>(5000,200c) VR=OB/OW VM=1 Audio Sample Data (Retired)</summary>
		public static DicomTag AudioSampleDataRETIRED = new DicomTag(0x5000, 0x200c);

		/// <summary>(5000,200e) VR=LT VM=1 Audio Comments (Retired)</summary>
		public static DicomTag AudioCommentsRETIRED = new DicomTag(0x5000, 0x200e);

		/// <summary>(5000,2500) VR=LO VM=1 Curve Label (Retired)</summary>
		public static DicomTag CurveLabelRETIRED = new DicomTag(0x5000, 0x2500);

		/// <summary>(5000,2600) VR=SQ VM=1 Curve Referenced Overlay Sequence (Retired)</summary>
		public static DicomTag CurveReferencedOverlaySequenceRETIRED = new DicomTag(0x5000, 0x2600);

		/// <summary>(5000,2610) VR=US VM=1 Curve Referenced Overlay Group (Retired)</summary>
		public static DicomTag CurveReferencedOverlayGroupRETIRED = new DicomTag(0x5000, 0x2610);

		/// <summary>(5000,3000) VR=OB/OW VM=1 Curve Data (Retired)</summary>
		public static DicomTag CurveDataRETIRED = new DicomTag(0x5000, 0x3000);

		/// <summary>(5200,9229) VR=SQ VM=1 Shared Functional Groups Sequence</summary>
		public static DicomTag SharedFunctionalGroupsSequence = new DicomTag(0x5200, 0x9229);

		/// <summary>(5200,9230) VR=SQ VM=1 Per-frame Functional Groups Sequence</summary>
		public static DicomTag PerframeFunctionalGroupsSequence = new DicomTag(0x5200, 0x9230);

		/// <summary>(5400,0100) VR=SQ VM=1 Waveform Sequence</summary>
		public static DicomTag WaveformSequence = new DicomTag(0x5400, 0x0100);

		/// <summary>(5400,0110) VR=OB/OW VM=1 Channel Minimum Value</summary>
		public static DicomTag ChannelMinimumValue = new DicomTag(0x5400, 0x0110);

		/// <summary>(5400,0112) VR=OB/OW VM=1 Channel Maximum Value</summary>
		public static DicomTag ChannelMaximumValue = new DicomTag(0x5400, 0x0112);

		/// <summary>(5400,1004) VR=US VM=1 Waveform Bits Allocated</summary>
		public static DicomTag WaveformBitsAllocated = new DicomTag(0x5400, 0x1004);

		/// <summary>(5400,1006) VR=CS VM=1 Waveform Sample Interpretation</summary>
		public static DicomTag WaveformSampleInterpretation = new DicomTag(0x5400, 0x1006);

		/// <summary>(5400,100a) VR=OB/OW VM=1 Waveform Padding Value</summary>
		public static DicomTag WaveformPaddingValue = new DicomTag(0x5400, 0x100a);

		/// <summary>(5400,1010) VR=OB/OW VM=1 Waveform Data</summary>
		public static DicomTag WaveformData = new DicomTag(0x5400, 0x1010);

		/// <summary>(5600,0010) VR=OF VM=1 First Order Phase Correction Angle</summary>
		public static DicomTag FirstOrderPhaseCorrectionAngle = new DicomTag(0x5600, 0x0010);

		/// <summary>(5600,0020) VR=OF VM=1 Spectroscopy Data</summary>
		public static DicomTag SpectroscopyData = new DicomTag(0x5600, 0x0020);

		/// <summary>(6000,0010) VR=US VM=1 Overlay Rows</summary>
		public static DicomTag OverlayRows = new DicomTag(0x6000, 0x0010);

		/// <summary>(6000,0011) VR=US VM=1 Overlay Columns</summary>
		public static DicomTag OverlayColumns = new DicomTag(0x6000, 0x0011);

		/// <summary>(6000,0012) VR=US VM=1 Overlay Planes (Retired)</summary>
		public static DicomTag OverlayPlanesRETIRED = new DicomTag(0x6000, 0x0012);

		/// <summary>(6000,0015) VR=IS VM=1 Number of Frames in Overlay</summary>
		public static DicomTag NumberOfFramesInOverlay = new DicomTag(0x6000, 0x0015);

		/// <summary>(6000,0022) VR=LO VM=1 Overlay Description</summary>
		public static DicomTag OverlayDescription = new DicomTag(0x6000, 0x0022);

		/// <summary>(6000,0040) VR=CS VM=1 Overlay Type</summary>
		public static DicomTag OverlayType = new DicomTag(0x6000, 0x0040);

		/// <summary>(6000,0045) VR=LO VM=1 Overlay Subtype</summary>
		public static DicomTag OverlaySubtype = new DicomTag(0x6000, 0x0045);

		/// <summary>(6000,0050) VR=SS VM=2 Overlay Origin</summary>
		public static DicomTag OverlayOrigin = new DicomTag(0x6000, 0x0050);

		/// <summary>(6000,0051) VR=US VM=1 Image Frame Origin</summary>
		public static DicomTag ImageFrameOrigin = new DicomTag(0x6000, 0x0051);

		/// <summary>(6000,0052) VR=US VM=1 Overlay Plane Origin (Retired)</summary>
		public static DicomTag OverlayPlaneOriginRETIRED = new DicomTag(0x6000, 0x0052);

		/// <summary>(6000,0060) VR=CS VM=1 Overlay Compression Code (Retired)</summary>
		public static DicomTag OverlayCompressionCodeRETIRED = new DicomTag(0x6000, 0x0060);

		/// <summary>(6000,0061) VR=SH VM=1 Overlay Compression Originator (Retired)</summary>
		public static DicomTag OverlayCompressionOriginatorRETIRED = new DicomTag(0x6000, 0x0061);

		/// <summary>(6000,0062) VR=SH VM=1 Overlay Compression Label (Retired)</summary>
		public static DicomTag OverlayCompressionLabelRETIRED = new DicomTag(0x6000, 0x0062);

		/// <summary>(6000,0063) VR=CS VM=1 Overlay Compression Description (Retired)</summary>
		public static DicomTag OverlayCompressionDescriptionRETIRED = new DicomTag(0x6000, 0x0063);

		/// <summary>(6000,0066) VR=AT VM=1-n Overlay Compression Step Pointers (Retired)</summary>
		public static DicomTag OverlayCompressionStepPointersRETIRED = new DicomTag(0x6000, 0x0066);

		/// <summary>(6000,0068) VR=US VM=1 Overlay Repeat Interval (Retired)</summary>
		public static DicomTag OverlayRepeatIntervalRETIRED = new DicomTag(0x6000, 0x0068);

		/// <summary>(6000,0069) VR=US VM=1 Overlay Bits Grouped (Retired)</summary>
		public static DicomTag OverlayBitsGroupedRETIRED = new DicomTag(0x6000, 0x0069);

		/// <summary>(6000,0100) VR=US VM=1 Overlay Bits Allocated</summary>
		public static DicomTag OverlayBitsAllocated = new DicomTag(0x6000, 0x0100);

		/// <summary>(6000,0102) VR=US VM=1 Overlay Bit Position</summary>
		public static DicomTag OverlayBitPosition = new DicomTag(0x6000, 0x0102);

		/// <summary>(6000,0110) VR=CS VM=1 Overlay Format (Retired)</summary>
		public static DicomTag OverlayFormatRETIRED = new DicomTag(0x6000, 0x0110);

		/// <summary>(6000,0200) VR=US VM=1 Overlay Location (Retired)</summary>
		public static DicomTag OverlayLocationRETIRED = new DicomTag(0x6000, 0x0200);

		/// <summary>(6000,0800) VR=CS VM=1-n Overlay Code Label (Retired)</summary>
		public static DicomTag OverlayCodeLabelRETIRED = new DicomTag(0x6000, 0x0800);

		/// <summary>(6000,0802) VR=US VM=1 Overlay Number of Tables (Retired)</summary>
		public static DicomTag OverlayNumberOfTablesRETIRED = new DicomTag(0x6000, 0x0802);

		/// <summary>(6000,0803) VR=AT VM=1-n Overlay Code Table Location (Retired)</summary>
		public static DicomTag OverlayCodeTableLocationRETIRED = new DicomTag(0x6000, 0x0803);

		/// <summary>(6000,0804) VR=US VM=1 Overlay Bits For Code Word (Retired)</summary>
		public static DicomTag OverlayBitsForCodeWordRETIRED = new DicomTag(0x6000, 0x0804);

		/// <summary>(6000,1001) VR=CS VM=1 Overlay Activation Layer</summary>
		public static DicomTag OverlayActivationLayer = new DicomTag(0x6000, 0x1001);

		/// <summary>(6000,1100) VR=US VM=1 Overlay Descriptor - Gray (Retired)</summary>
		public static DicomTag OverlayDescriptorGrayRETIRED = new DicomTag(0x6000, 0x1100);

		/// <summary>(6000,1101) VR=US VM=1 Overlay Descriptor - Red (Retired)</summary>
		public static DicomTag OverlayDescriptorRedRETIRED = new DicomTag(0x6000, 0x1101);

		/// <summary>(6000,1102) VR=US VM=1 Overlay Descriptor - Green (Retired)</summary>
		public static DicomTag OverlayDescriptorGreenRETIRED = new DicomTag(0x6000, 0x1102);

		/// <summary>(6000,1103) VR=US VM=1 Overlay Descriptor - Blue (Retired)</summary>
		public static DicomTag OverlayDescriptorBlueRETIRED = new DicomTag(0x6000, 0x1103);

		/// <summary>(6000,1200) VR=US VM=1-n Overlays - Gray (Retired)</summary>
		public static DicomTag OverlaysGrayRETIRED = new DicomTag(0x6000, 0x1200);

		/// <summary>(6000,1201) VR=US VM=1-n Overlays - Red (Retired)</summary>
		public static DicomTag OverlaysRedRETIRED = new DicomTag(0x6000, 0x1201);

		/// <summary>(6000,1202) VR=US VM=1-n Overlays - Green (Retired)</summary>
		public static DicomTag OverlaysGreenRETIRED = new DicomTag(0x6000, 0x1202);

		/// <summary>(6000,1203) VR=US VM=1-n Overlays - Blue (Retired)</summary>
		public static DicomTag OverlaysBlueRETIRED = new DicomTag(0x6000, 0x1203);

		/// <summary>(6000,1301) VR=IS VM=1 ROI Area</summary>
		public static DicomTag ROIArea = new DicomTag(0x6000, 0x1301);

		/// <summary>(6000,1302) VR=DS VM=1 ROI Mean</summary>
		public static DicomTag ROIMean = new DicomTag(0x6000, 0x1302);

		/// <summary>(6000,1303) VR=DS VM=1 ROI Standard Deviation</summary>
		public static DicomTag ROIStandardDeviation = new DicomTag(0x6000, 0x1303);

		/// <summary>(6000,1500) VR=LO VM=1 Overlay Label</summary>
		public static DicomTag OverlayLabel = new DicomTag(0x6000, 0x1500);

		/// <summary>(6000,3000) VR=OB/OW VM=1 Overlay Data</summary>
		public static DicomTag OverlayData = new DicomTag(0x6000, 0x3000);

		/// <summary>(6000,4000) VR=LT VM=1 Overlay Comments (Retired)</summary>
		public static DicomTag OverlayCommentsRETIRED = new DicomTag(0x6000, 0x4000);

		/// <summary>(7fe0,0010) VR=OB/OW VM=1 Pixel Data</summary>
		public static DicomTag PixelData = new DicomTag(0x7fe0, 0x0010);

		/// <summary>(7fe0,0020) VR=OW VM=1 Coefficients SDVN (Retired)</summary>
		public static DicomTag CoefficientsSDVNRETIRED = new DicomTag(0x7fe0, 0x0020);

		/// <summary>(7fe0,0030) VR=OW VM=1 Coefficients SDHN (Retired)</summary>
		public static DicomTag CoefficientsSDHNRETIRED = new DicomTag(0x7fe0, 0x0030);

		/// <summary>(7fe0,0040) VR=OW VM=1 Coefficients SDDN (Retired)</summary>
		public static DicomTag CoefficientsSDDNRETIRED = new DicomTag(0x7fe0, 0x0040);

		/// <summary>(7f00,0010) VR=OB/OW VM=1 Variable Pixel Data (Retired)</summary>
		public static DicomTag VariablePixelDataRETIRED = new DicomTag(0x7f00, 0x0010);

		/// <summary>(7f00,0011) VR=US VM=1 Variable Next Data Group (Retired)</summary>
		public static DicomTag VariableNextDataGroupRETIRED = new DicomTag(0x7f00, 0x0011);

		/// <summary>(7f00,0020) VR=OW VM=1 Variable Coefficients SDVN (Retired)</summary>
		public static DicomTag VariableCoefficientsSDVNRETIRED = new DicomTag(0x7f00, 0x0020);

		/// <summary>(7f00,0030) VR=OW VM=1 Variable Coefficients SDHN (Retired)</summary>
		public static DicomTag VariableCoefficientsSDHNRETIRED = new DicomTag(0x7f00, 0x0030);

		/// <summary>(7f00,0040) VR=OW VM=1 Variable Coefficients SDDN (Retired)</summary>
		public static DicomTag VariableCoefficientsSDDNRETIRED = new DicomTag(0x7f00, 0x0040);

		/// <summary>(fffa,fffa) VR=SQ VM=1 Digital Signatures Sequence</summary>
		public static DicomTag DigitalSignaturesSequence = new DicomTag(0xfffa, 0xfffa);

		/// <summary>(fffc,fffc) VR=OB VM=1 Data Set Trailing Padding</summary>
		public static DicomTag DataSetTrailingPadding = new DicomTag(0xfffc, 0xfffc);

		/// <summary>(fffe,e000) VR=NONE VM=1 Item</summary>
		public static DicomTag Item = new DicomTag(0xfffe, 0xe000);

		/// <summary>(fffe,e00d) VR=NONE VM=1 Item Delimitation Item</summary>
		public static DicomTag ItemDelimitationItem = new DicomTag(0xfffe, 0xe00d);

		/// <summary>(fffe,e0dd) VR=NONE VM=1 Sequence Delimitation Item</summary>
		public static DicomTag SequenceDelimitationItem = new DicomTag(0xfffe, 0xe0dd);
		#endregion
	}

	/// <summary>Const DICOM Tag cards for use with DicomFieldAttribute</summary>
	public static class DicomConstTags {
		#region Const Tags
		/// <summary>(0000,0002) VR=UI VM=1 Affected SOP Class UID</summary>
		public const uint AffectedSOPClassUID = 0x00000002;

		/// <summary>(0000,0003) VR=UI VM=1 Requested SOP Class UID</summary>
		public const uint RequestedSOPClassUID = 0x00000003;

		/// <summary>(0000,0100) VR=US VM=1 Command Field</summary>
		public const uint CommandField = 0x00000100;

		/// <summary>(0000,0110) VR=US VM=1 Message ID</summary>
		public const uint MessageID = 0x00000110;

		/// <summary>(0000,0120) VR=US VM=1 Message ID Being Responded To</summary>
		public const uint MessageIDBeingRespondedTo = 0x00000120;

		/// <summary>(0000,0600) VR=AE VM=1 Move Destination</summary>
		public const uint MoveDestination = 0x00000600;

		/// <summary>(0000,0700) VR=US VM=1 Priority</summary>
		public const uint Priority = 0x00000700;

		/// <summary>(0000,0800) VR=US VM=1 Data Set Type</summary>
		public const uint DataSetType = 0x00000800;

		/// <summary>(0000,0900) VR=US VM=1 Status</summary>
		public const uint Status = 0x00000900;

		/// <summary>(0000,0901) VR=AT VM=1-n Offending Element</summary>
		public const uint OffendingElement = 0x00000901;

		/// <summary>(0000,0902) VR=LO VM=1 Error Comment</summary>
		public const uint ErrorComment = 0x00000902;

		/// <summary>(0000,0903) VR=US VM=1 Error ID</summary>
		public const uint ErrorID = 0x00000903;

		/// <summary>(0000,1000) VR=UI VM=1 Affected SOP Instance UID</summary>
		public const uint AffectedSOPInstanceUID = 0x00001000;

		/// <summary>(0000,1001) VR=UI VM=1 Requested SOP Instance UID</summary>
		public const uint RequestedSOPInstanceUID = 0x00001001;

		/// <summary>(0000,1002) VR=US VM=1 Event Type ID</summary>
		public const uint EventTypeID = 0x00001002;

		/// <summary>(0000,1005) VR=AT VM=1-n Attribute Identifier List</summary>
		public const uint AttributeIdentifierList = 0x00001005;

		/// <summary>(0000,1008) VR=US VM=1 Action Type ID</summary>
		public const uint ActionTypeID = 0x00001008;

		/// <summary>(0000,1020) VR=US VM=1 Number of Remaining Sub-operations</summary>
		public const uint NumberOfRemainingSuboperations = 0x00001020;

		/// <summary>(0000,1021) VR=US VM=1 Number of Completed Sub-operations</summary>
		public const uint NumberOfCompletedSuboperations = 0x00001021;

		/// <summary>(0000,1022) VR=US VM=1 Number of Failed Sub-operations</summary>
		public const uint NumberOfFailedSuboperations = 0x00001022;

		/// <summary>(0000,1023) VR=US VM=1 Number of Warning Sub-operations</summary>
		public const uint NumberOfWarningSuboperations = 0x00001023;

		/// <summary>(0000,1030) VR=AE VM=1 Move Originator Application Entity Title</summary>
		public const uint MoveOriginatorApplicationEntityTitle = 0x00001030;

		/// <summary>(0000,1031) VR=US VM=1 Move Originator Message ID</summary>
		public const uint MoveOriginatorMessageID = 0x00001031;

		/// <summary>(0002,0001) VR=OB VM=1 File Meta Information Version</summary>
		public const uint FileMetaInformationVersion = 0x00020001;

		/// <summary>(0002,0002) VR=UI VM=1 Media Storage SOP Class UID</summary>
		public const uint MediaStorageSOPClassUID = 0x00020002;

		/// <summary>(0002,0003) VR=UI VM=1 Media Storage SOP Instance UID</summary>
		public const uint MediaStorageSOPInstanceUID = 0x00020003;

		/// <summary>(0002,0010) VR=UI VM=1 Transfer Syntax UID</summary>
		public const uint TransferSyntaxUID = 0x00020010;

		/// <summary>(0002,0012) VR=UI VM=1 Implementation Class UID</summary>
		public const uint ImplementationClassUID = 0x00020012;

		/// <summary>(0002,0013) VR=SH VM=1 Implementation Version Name</summary>
		public const uint ImplementationVersionName = 0x00020013;

		/// <summary>(0002,0016) VR=AE VM=1 Source Application Entity Title</summary>
		public const uint SourceApplicationEntityTitle = 0x00020016;

		/// <summary>(0002,0100) VR=UI VM=1 Private Information Creator UID</summary>
		public const uint PrivateInformationCreatorUID = 0x00020100;

		/// <summary>(0002,0102) VR=OB VM=1 Private Information</summary>
		public const uint PrivateInformation = 0x00020102;

		/// <summary>(0004,1130) VR=CS VM=1 File-set ID</summary>
		public const uint FilesetID = 0x00041130;

		/// <summary>(0004,1141) VR=CS VM=1-8 File-set Descriptor File ID</summary>
		public const uint FilesetDescriptorFileID = 0x00041141;

		/// <summary>(0004,1142) VR=CS VM=1 Specific Character Set of File-set Descriptor File</summary>
		public const uint SpecificCharacterSetOfFilesetDescriptorFile = 0x00041142;

		/// <summary>(0004,1200) VR=UL VM=1 Offset of the First Directory Record of the Root Directory Entity</summary>
		public const uint OffsetOfTheFirstDirectoryRecordOfTheRootDirectoryEntity = 0x00041200;

		/// <summary>(0004,1202) VR=UL VM=1 Offset of the Last Directory Record of the Root Directory Entity</summary>
		public const uint OffsetOfTheLastDirectoryRecordOfTheRootDirectoryEntity = 0x00041202;

		/// <summary>(0004,1212) VR=US VM=1 File-set Consistency Flag</summary>
		public const uint FilesetConsistencyFlag = 0x00041212;

		/// <summary>(0004,1220) VR=SQ VM=1 Directory Record Sequence</summary>
		public const uint DirectoryRecordSequence = 0x00041220;

		/// <summary>(0004,1400) VR=UL VM=1 Offset of the Next Directory Record</summary>
		public const uint OffsetOfTheNextDirectoryRecord = 0x00041400;

		/// <summary>(0004,1410) VR=US VM=1 Record In-use Flag</summary>
		public const uint RecordInuseFlag = 0x00041410;

		/// <summary>(0004,1420) VR=UL VM=1 Offset of Referenced Lower-Level Directory Entity</summary>
		public const uint OffsetOfReferencedLowerLevelDirectoryEntity = 0x00041420;

		/// <summary>(0004,1430) VR=CS VM=1 Directory Record Type</summary>
		public const uint DirectoryRecordType = 0x00041430;

		/// <summary>(0004,1432) VR=UI VM=1 Private Record UID</summary>
		public const uint PrivateRecordUID = 0x00041432;

		/// <summary>(0004,1500) VR=CS VM=1-8 Referenced File ID</summary>
		public const uint ReferencedFileID = 0x00041500;

		/// <summary>(0004,1510) VR=UI VM=1 Referenced SOP Class UID in File</summary>
		public const uint ReferencedSOPClassUIDInFile = 0x00041510;

		/// <summary>(0004,1511) VR=UI VM=1 Referenced SOP Instance UID in File</summary>
		public const uint ReferencedSOPInstanceUIDInFile = 0x00041511;

		/// <summary>(0004,1512) VR=UI VM=1 Referenced Transfer Syntax UID in File</summary>
		public const uint ReferencedTransferSyntaxUIDInFile = 0x00041512;

		/// <summary>(0004,151a) VR=UI VM=1-n Referenced Related General SOP Class UID in File</summary>
		public const uint ReferencedRelatedGeneralSOPClassUIDInFile = 0x0004151a;

		/// <summary>(0008,0005) VR=CS VM=1-n Specific Character Set</summary>
		public const uint SpecificCharacterSet = 0x00080005;

		/// <summary>(0008,0008) VR=CS VM=2-n Image Type</summary>
		public const uint ImageType = 0x00080008;

		/// <summary>(0008,0012) VR=DA VM=1 Instance Creation Date</summary>
		public const uint InstanceCreationDate = 0x00080012;

		/// <summary>(0008,0013) VR=TM VM=1 Instance Creation Time</summary>
		public const uint InstanceCreationTime = 0x00080013;

		/// <summary>(0008,0014) VR=UI VM=1 Instance Creator UID</summary>
		public const uint InstanceCreatorUID = 0x00080014;

		/// <summary>(0008,0016) VR=UI VM=1 SOP Class UID</summary>
		public const uint SOPClassUID = 0x00080016;

		/// <summary>(0008,0018) VR=UI VM=1 SOP Instance UID</summary>
		public const uint SOPInstanceUID = 0x00080018;

		/// <summary>(0008,001a) VR=UI VM=1-n Related General SOP Class UID</summary>
		public const uint RelatedGeneralSOPClassUID = 0x0008001a;

		/// <summary>(0008,001b) VR=UI VM=1 Original Specialized SOP Class UID</summary>
		public const uint OriginalSpecializedSOPClassUID = 0x0008001b;

		/// <summary>(0008,0020) VR=DA VM=1 Study Date</summary>
		public const uint StudyDate = 0x00080020;

		/// <summary>(0008,0021) VR=DA VM=1 Series Date</summary>
		public const uint SeriesDate = 0x00080021;

		/// <summary>(0008,0022) VR=DA VM=1 Acquisition Date</summary>
		public const uint AcquisitionDate = 0x00080022;

		/// <summary>(0008,0023) VR=DA VM=1 Content Date</summary>
		public const uint ContentDate = 0x00080023;

		/// <summary>(0008,002a) VR=DT VM=1 Acquisition DateTime</summary>
		public const uint AcquisitionDateTime = 0x0008002a;

		/// <summary>(0008,0030) VR=TM VM=1 Study Time</summary>
		public const uint StudyTime = 0x00080030;

		/// <summary>(0008,0031) VR=TM VM=1 Series Time</summary>
		public const uint SeriesTime = 0x00080031;

		/// <summary>(0008,0032) VR=TM VM=1 Acquisition Time</summary>
		public const uint AcquisitionTime = 0x00080032;

		/// <summary>(0008,0033) VR=TM VM=1 Content Time</summary>
		public const uint ContentTime = 0x00080033;

		/// <summary>(0008,0050) VR=SH VM=1 Accession Number</summary>
		public const uint AccessionNumber = 0x00080050;

		/// <summary>(0008,0052) VR=CS VM=1 Query/Retrieve Level</summary>
		public const uint QueryRetrieveLevel = 0x00080052;

		/// <summary>(0008,0054) VR=AE VM=1-n Retrieve AE Title</summary>
		public const uint RetrieveAETitle = 0x00080054;

		/// <summary>(0008,0056) VR=CS VM=1 Instance Availability</summary>
		public const uint InstanceAvailability = 0x00080056;

		/// <summary>(0008,0058) VR=UI VM=1-n Failed SOP Instance UID List</summary>
		public const uint FailedSOPInstanceUIDList = 0x00080058;

		/// <summary>(0008,0060) VR=CS VM=1 Modality</summary>
		public const uint Modality = 0x00080060;

		/// <summary>(0008,0061) VR=CS VM=1-n Modalities in Study</summary>
		public const uint ModalitiesInStudy = 0x00080061;

		/// <summary>(0008,0062) VR=UI VM=1-n SOP Classes in Study</summary>
		public const uint SOPClassesInStudy = 0x00080062;

		/// <summary>(0008,0064) VR=CS VM=1 Conversion Type</summary>
		public const uint ConversionType = 0x00080064;

		/// <summary>(0008,0068) VR=CS VM=1 Presentation Intent Type</summary>
		public const uint PresentationIntentType = 0x00080068;

		/// <summary>(0008,0070) VR=LO VM=1 Manufacturer</summary>
		public const uint Manufacturer = 0x00080070;

		/// <summary>(0008,0080) VR=LO VM=1 Institution Name</summary>
		public const uint InstitutionName = 0x00080080;

		/// <summary>(0008,0081) VR=ST VM=1 Institution Address</summary>
		public const uint InstitutionAddress = 0x00080081;

		/// <summary>(0008,0082) VR=SQ VM=1 Institution Code Sequence</summary>
		public const uint InstitutionCodeSequence = 0x00080082;

		/// <summary>(0008,0090) VR=PN VM=1 Referring Physician's Name</summary>
		public const uint ReferringPhysiciansName = 0x00080090;

		/// <summary>(0008,0092) VR=ST VM=1 Referring Physician's Address</summary>
		public const uint ReferringPhysiciansAddress = 0x00080092;

		/// <summary>(0008,0094) VR=SH VM=1-n Referring Physician's Telephone Numbers</summary>
		public const uint ReferringPhysiciansTelephoneNumbers = 0x00080094;

		/// <summary>(0008,0096) VR=SQ VM=1 Referring Physician Identification Sequence</summary>
		public const uint ReferringPhysicianIdentificationSequence = 0x00080096;

		/// <summary>(0008,0100) VR=SH VM=1 Code Value</summary>
		public const uint CodeValue = 0x00080100;

		/// <summary>(0008,0102) VR=SH VM=1 Coding Scheme Designator</summary>
		public const uint CodingSchemeDesignator = 0x00080102;

		/// <summary>(0008,0103) VR=SH VM=1 Coding Scheme Version</summary>
		public const uint CodingSchemeVersion = 0x00080103;

		/// <summary>(0008,0104) VR=LO VM=1 Code Meaning</summary>
		public const uint CodeMeaning = 0x00080104;

		/// <summary>(0008,0105) VR=CS VM=1 Mapping Resource</summary>
		public const uint MappingResource = 0x00080105;

		/// <summary>(0008,0106) VR=DT VM=1 Context Group Version</summary>
		public const uint ContextGroupVersion = 0x00080106;

		/// <summary>(0008,0107) VR=DT VM=1 Context Group Local Version</summary>
		public const uint ContextGroupLocalVersion = 0x00080107;

		/// <summary>(0008,010b) VR=CS VM=1 Context Group Extension Flag</summary>
		public const uint ContextGroupExtensionFlag = 0x0008010b;

		/// <summary>(0008,010c) VR=UI VM=1 Coding Scheme UID</summary>
		public const uint CodingSchemeUID = 0x0008010c;

		/// <summary>(0008,010d) VR=UI VM=1 Context Group Extension Creator UID</summary>
		public const uint ContextGroupExtensionCreatorUID = 0x0008010d;

		/// <summary>(0008,010f) VR=CS VM=1 Context Identifier</summary>
		public const uint ContextIdentifier = 0x0008010f;

		/// <summary>(0008,0110) VR=SQ VM=1 Coding Scheme Identification Sequence</summary>
		public const uint CodingSchemeIdentificationSequence = 0x00080110;

		/// <summary>(0008,0112) VR=LO VM=1 Coding Scheme Registry</summary>
		public const uint CodingSchemeRegistry = 0x00080112;

		/// <summary>(0008,0114) VR=ST VM=1 Coding Scheme External ID</summary>
		public const uint CodingSchemeExternalID = 0x00080114;

		/// <summary>(0008,0115) VR=ST VM=1 Coding Scheme Name</summary>
		public const uint CodingSchemeName = 0x00080115;

		/// <summary>(0008,0116) VR=ST VM=1 Coding Scheme Responsible Organization</summary>
		public const uint CodingSchemeResponsibleOrganization = 0x00080116;

		/// <summary>(0008,0201) VR=SH VM=1 Timezone Offset From UTC</summary>
		public const uint TimezoneOffsetFromUTC = 0x00080201;

		/// <summary>(0008,1010) VR=SH VM=1 Station Name</summary>
		public const uint StationName = 0x00081010;

		/// <summary>(0008,1030) VR=LO VM=1 Study Description</summary>
		public const uint StudyDescription = 0x00081030;

		/// <summary>(0008,1032) VR=SQ VM=1 Procedure Code Sequence</summary>
		public const uint ProcedureCodeSequence = 0x00081032;

		/// <summary>(0008,103e) VR=LO VM=1 Series Description</summary>
		public const uint SeriesDescription = 0x0008103e;

		/// <summary>(0008,1040) VR=LO VM=1 Institutional Department Name</summary>
		public const uint InstitutionalDepartmentName = 0x00081040;

		/// <summary>(0008,1048) VR=PN VM=1-n Physician(s) of Record</summary>
		public const uint PhysiciansOfRecord = 0x00081048;

		/// <summary>(0008,1049) VR=SQ VM=1 Physician(s) of Record Identification Sequence</summary>
		public const uint PhysiciansOfRecordIdentificationSequence = 0x00081049;

		/// <summary>(0008,1050) VR=PN VM=1-n Performing Physician's Name</summary>
		public const uint PerformingPhysiciansName = 0x00081050;

		/// <summary>(0008,1052) VR=SQ VM=1 Performing Physician Identification Sequence</summary>
		public const uint PerformingPhysicianIdentificationSequence = 0x00081052;

		/// <summary>(0008,1060) VR=PN VM=1-n Name of Physician(s) Reading Study</summary>
		public const uint NameOfPhysiciansReadingStudy = 0x00081060;

		/// <summary>(0008,1062) VR=SQ VM=1 Physician(s) Reading Study Identification Sequence</summary>
		public const uint PhysiciansReadingStudyIdentificationSequence = 0x00081062;

		/// <summary>(0008,1070) VR=PN VM=1-n Operators' Name</summary>
		public const uint OperatorsName = 0x00081070;

		/// <summary>(0008,1072) VR=SQ VM=1 Operator Identification Sequence</summary>
		public const uint OperatorIdentificationSequence = 0x00081072;

		/// <summary>(0008,1080) VR=LO VM=1-n Admitting Diagnoses Description</summary>
		public const uint AdmittingDiagnosesDescription = 0x00081080;

		/// <summary>(0008,1084) VR=SQ VM=1 Admitting Diagnoses Code Sequence</summary>
		public const uint AdmittingDiagnosesCodeSequence = 0x00081084;

		/// <summary>(0008,1090) VR=LO VM=1 Manufacturer's Model Name</summary>
		public const uint ManufacturersModelName = 0x00081090;

		/// <summary>(0008,1110) VR=SQ VM=1 Referenced Study Sequence</summary>
		public const uint ReferencedStudySequence = 0x00081110;

		/// <summary>(0008,1111) VR=SQ VM=1 Referenced Performed Procedure Step Sequence</summary>
		public const uint ReferencedPerformedProcedureStepSequence = 0x00081111;

		/// <summary>(0008,1115) VR=SQ VM=1 Referenced Series Sequence</summary>
		public const uint ReferencedSeriesSequence = 0x00081115;

		/// <summary>(0008,1120) VR=SQ VM=1 Referenced Patient Sequence</summary>
		public const uint ReferencedPatientSequence = 0x00081120;

		/// <summary>(0008,1125) VR=SQ VM=1 Referenced Visit Sequence</summary>
		public const uint ReferencedVisitSequence = 0x00081125;

		/// <summary>(0008,113a) VR=SQ VM=1 Referenced Waveform Sequence</summary>
		public const uint ReferencedWaveformSequence = 0x0008113a;

		/// <summary>(0008,1140) VR=SQ VM=1 Referenced Image Sequence</summary>
		public const uint ReferencedImageSequence = 0x00081140;

		/// <summary>(0008,114a) VR=SQ VM=1 Referenced Instance Sequence</summary>
		public const uint ReferencedInstanceSequence = 0x0008114a;

		/// <summary>(0008,114b) VR=SQ VM=1 Referenced Real World Value Mapping Instance Sequence</summary>
		public const uint ReferencedRealWorldValueMappingInstanceSequence = 0x0008114b;

		/// <summary>(0008,1150) VR=UI VM=1 Referenced SOP Class UID</summary>
		public const uint ReferencedSOPClassUID = 0x00081150;

		/// <summary>(0008,1155) VR=UI VM=1 Referenced SOP Instance UID</summary>
		public const uint ReferencedSOPInstanceUID = 0x00081155;

		/// <summary>(0008,115a) VR=UI VM=1-n SOP Classes Supported</summary>
		public const uint SOPClassesSupported = 0x0008115a;

		/// <summary>(0008,1160) VR=IS VM=1-n Referenced Frame Number</summary>
		public const uint ReferencedFrameNumber = 0x00081160;

		/// <summary>(0008,1195) VR=UI VM=1 Transaction UID</summary>
		public const uint TransactionUID = 0x00081195;

		/// <summary>(0008,1197) VR=US VM=1 Failure Reason</summary>
		public const uint FailureReason = 0x00081197;

		/// <summary>(0008,1198) VR=SQ VM=1 Failed SOP Sequence</summary>
		public const uint FailedSOPSequence = 0x00081198;

		/// <summary>(0008,1199) VR=SQ VM=1 Referenced SOP Sequence</summary>
		public const uint ReferencedSOPSequence = 0x00081199;

		/// <summary>(0008,1200) VR=SQ VM=1 Studies Containing Other Referenced Instances Sequence</summary>
		public const uint StudiesContainingOtherReferencedInstancesSequence = 0x00081200;

		/// <summary>(0008,1250) VR=SQ VM=1 Related Series Sequence</summary>
		public const uint RelatedSeriesSequence = 0x00081250;

		/// <summary>(0008,2111) VR=ST VM=1 Derivation Description</summary>
		public const uint DerivationDescription = 0x00082111;

		/// <summary>(0008,2112) VR=SQ VM=1 Source Image Sequence</summary>
		public const uint SourceImageSequence = 0x00082112;

		/// <summary>(0008,2120) VR=SH VM=1 Stage Name</summary>
		public const uint StageName = 0x00082120;

		/// <summary>(0008,2122) VR=IS VM=1 Stage Number</summary>
		public const uint StageNumber = 0x00082122;

		/// <summary>(0008,2124) VR=IS VM=1 Number of Stages</summary>
		public const uint NumberOfStages = 0x00082124;

		/// <summary>(0008,2127) VR=SH VM=1 View Name</summary>
		public const uint ViewName = 0x00082127;

		/// <summary>(0008,2128) VR=IS VM=1 View Number</summary>
		public const uint ViewNumber = 0x00082128;

		/// <summary>(0008,2129) VR=IS VM=1 Number of Event Timers</summary>
		public const uint NumberOfEventTimers = 0x00082129;

		/// <summary>(0008,212a) VR=IS VM=1 Number of Views in Stage</summary>
		public const uint NumberOfViewsInStage = 0x0008212a;

		/// <summary>(0008,2130) VR=DS VM=1-n Event Elapsed Time(s)</summary>
		public const uint EventElapsedTimes = 0x00082130;

		/// <summary>(0008,2132) VR=LO VM=1-n Event Timer Name(s)</summary>
		public const uint EventTimerNames = 0x00082132;

		/// <summary>(0008,2142) VR=IS VM=1 Start Trim</summary>
		public const uint StartTrim = 0x00082142;

		/// <summary>(0008,2143) VR=IS VM=1 Stop Trim</summary>
		public const uint StopTrim = 0x00082143;

		/// <summary>(0008,2144) VR=IS VM=1 Recommended Display Frame Rate</summary>
		public const uint RecommendedDisplayFrameRate = 0x00082144;

		/// <summary>(0008,2218) VR=SQ VM=1 Anatomic Region Sequence</summary>
		public const uint AnatomicRegionSequence = 0x00082218;

		/// <summary>(0008,2220) VR=SQ VM=1 Anatomic Region Modifier Sequence</summary>
		public const uint AnatomicRegionModifierSequence = 0x00082220;

		/// <summary>(0008,2228) VR=SQ VM=1 Primary Anatomic Structure Sequence</summary>
		public const uint PrimaryAnatomicStructureSequence = 0x00082228;

		/// <summary>(0008,2229) VR=SQ VM=1 Anatomic Structure, Space or Region Sequence</summary>
		public const uint AnatomicStructureSpaceOrRegionSequence = 0x00082229;

		/// <summary>(0008,2230) VR=SQ VM=1 Primary Anatomic Structure Modifier Sequence</summary>
		public const uint PrimaryAnatomicStructureModifierSequence = 0x00082230;

		/// <summary>(0008,3001) VR=SQ VM=1 Alternate Representation Sequence</summary>
		public const uint AlternateRepresentationSequence = 0x00083001;

		/// <summary>(0008,3010) VR=UI VM=1 Irradiation Event UID</summary>
		public const uint IrradiationEventUID = 0x00083010;

		/// <summary>(0008,9007) VR=CS VM=4 Frame Type</summary>
		public const uint FrameType = 0x00089007;

		/// <summary>(0008,9092) VR=SQ VM=1 Referenced Image Evidence Sequence</summary>
		public const uint ReferencedImageEvidenceSequence = 0x00089092;

		/// <summary>(0008,9121) VR=SQ VM=1 Referenced Raw Data Sequence</summary>
		public const uint ReferencedRawDataSequence = 0x00089121;

		/// <summary>(0008,9123) VR=UI VM=1 Creator-Version UID</summary>
		public const uint CreatorVersionUID = 0x00089123;

		/// <summary>(0008,9124) VR=SQ VM=1 Derivation Image Sequence</summary>
		public const uint DerivationImageSequence = 0x00089124;

		/// <summary>(0008,9154) VR=SQ VM=1 Source Image Evidence Sequence</summary>
		public const uint SourceImageEvidenceSequence = 0x00089154;

		/// <summary>(0008,9205) VR=CS VM=1 Pixel Presentation</summary>
		public const uint PixelPresentation = 0x00089205;

		/// <summary>(0008,9206) VR=CS VM=1 Volumetric Properties</summary>
		public const uint VolumetricProperties = 0x00089206;

		/// <summary>(0008,9207) VR=CS VM=1 Volume Based Calculation Technique</summary>
		public const uint VolumeBasedCalculationTechnique = 0x00089207;

		/// <summary>(0008,9208) VR=CS VM=1 Complex Image Component</summary>
		public const uint ComplexImageComponent = 0x00089208;

		/// <summary>(0008,9209) VR=CS VM=1 Acquisition Contrast</summary>
		public const uint AcquisitionContrast = 0x00089209;

		/// <summary>(0008,9215) VR=SQ VM=1 Derivation Code Sequence</summary>
		public const uint DerivationCodeSequence = 0x00089215;

		/// <summary>(0008,9237) VR=SQ VM=1 Referenced Grayscale Presentation State Sequence</summary>
		public const uint ReferencedGrayscalePresentationStateSequence = 0x00089237;

		/// <summary>(0008,9410) VR=SQ VM=1 Referenced Other Plane Sequence</summary>
		public const uint ReferencedOtherPlaneSequence = 0x00089410;

		/// <summary>(0008,9458) VR=SQ VM=1 Frame Display Sequence</summary>
		public const uint FrameDisplaySequence = 0x00089458;

		/// <summary>(0008,9459) VR=FL VM=1 Recommended Display Frame Rate in Float</summary>
		public const uint RecommendedDisplayFrameRateInFloat = 0x00089459;

		/// <summary>(0008,9460) VR=CS VM=1 Skip Frame Range Flag</summary>
		public const uint SkipFrameRangeFlag = 0x00089460;

		/// <summary>(0010,0010) VR=PN VM=1 Patient's Name</summary>
		public const uint PatientsName = 0x00100010;

		/// <summary>(0010,0020) VR=LO VM=1 Patient ID</summary>
		public const uint PatientID = 0x00100020;

		/// <summary>(0010,0021) VR=LO VM=1 Issuer of Patient ID</summary>
		public const uint IssuerOfPatientID = 0x00100021;

		/// <summary>(0010,0022) VR=CS VM=1 Type of Patient ID</summary>
		public const uint TypeOfPatientID = 0x00100022;

		/// <summary>(0010,0030) VR=DA VM=1 Patient's Birth Date</summary>
		public const uint PatientsBirthDate = 0x00100030;

		/// <summary>(0010,0032) VR=TM VM=1 Patient's Birth Time</summary>
		public const uint PatientsBirthTime = 0x00100032;

		/// <summary>(0010,0040) VR=CS VM=1 Patient's Sex</summary>
		public const uint PatientsSex = 0x00100040;

		/// <summary>(0010,0050) VR=SQ VM=1 Patient's Insurance Plan Code Sequence</summary>
		public const uint PatientsInsurancePlanCodeSequence = 0x00100050;

		/// <summary>(0010,0101) VR=SQ VM=1 Patient's Primary Language Code Sequence</summary>
		public const uint PatientsPrimaryLanguageCodeSequence = 0x00100101;

		/// <summary>(0010,0102) VR=SQ VM=1 Patient's Primary Language Code Modifier Sequence</summary>
		public const uint PatientsPrimaryLanguageCodeModifierSequence = 0x00100102;

		/// <summary>(0010,1000) VR=LO VM=1-n Other Patient IDs</summary>
		public const uint OtherPatientIDs = 0x00101000;

		/// <summary>(0010,1001) VR=PN VM=1-n Other Patient Names</summary>
		public const uint OtherPatientNames = 0x00101001;

		/// <summary>(0010,1002) VR=SQ VM=1 Other Patient IDs Sequence</summary>
		public const uint OtherPatientIDsSequence = 0x00101002;

		/// <summary>(0010,1005) VR=PN VM=1 Patient's Birth Name</summary>
		public const uint PatientsBirthName = 0x00101005;

		/// <summary>(0010,1010) VR=AS VM=1 Patient's Age</summary>
		public const uint PatientsAge = 0x00101010;

		/// <summary>(0010,1020) VR=DS VM=1 Patient's Size</summary>
		public const uint PatientsSize = 0x00101020;

		/// <summary>(0010,1030) VR=DS VM=1 Patient's Weight</summary>
		public const uint PatientsWeight = 0x00101030;

		/// <summary>(0010,1040) VR=LO VM=1 Patient's Address</summary>
		public const uint PatientsAddress = 0x00101040;

		/// <summary>(0010,1060) VR=PN VM=1 Patient's Mother's Birth Name</summary>
		public const uint PatientsMothersBirthName = 0x00101060;

		/// <summary>(0010,1080) VR=LO VM=1 Military Rank</summary>
		public const uint MilitaryRank = 0x00101080;

		/// <summary>(0010,1081) VR=LO VM=1 Branch of Service</summary>
		public const uint BranchOfService = 0x00101081;

		/// <summary>(0010,1090) VR=LO VM=1 Medical Record Locator</summary>
		public const uint MedicalRecordLocator = 0x00101090;

		/// <summary>(0010,2000) VR=LO VM=1-n Medical Alerts</summary>
		public const uint MedicalAlerts = 0x00102000;

		/// <summary>(0010,2110) VR=LO VM=1-n Allergies</summary>
		public const uint Allergies = 0x00102110;

		/// <summary>(0010,2150) VR=LO VM=1 Country of Residence</summary>
		public const uint CountryOfResidence = 0x00102150;

		/// <summary>(0010,2152) VR=LO VM=1 Region of Residence</summary>
		public const uint RegionOfResidence = 0x00102152;

		/// <summary>(0010,2154) VR=SH VM=1-n Patient's Telephone Numbers</summary>
		public const uint PatientsTelephoneNumbers = 0x00102154;

		/// <summary>(0010,2160) VR=SH VM=1 Ethnic Group</summary>
		public const uint EthnicGroup = 0x00102160;

		/// <summary>(0010,2180) VR=SH VM=1 Occupation</summary>
		public const uint Occupation = 0x00102180;

		/// <summary>(0010,21a0) VR=CS VM=1 Smoking Status</summary>
		public const uint SmokingStatus = 0x001021a0;

		/// <summary>(0010,21b0) VR=LT VM=1 Additional Patient History</summary>
		public const uint AdditionalPatientHistory = 0x001021b0;

		/// <summary>(0010,21c0) VR=US VM=1 Pregnancy Status</summary>
		public const uint PregnancyStatus = 0x001021c0;

		/// <summary>(0010,21d0) VR=DA VM=1 Last Menstrual Date</summary>
		public const uint LastMenstrualDate = 0x001021d0;

		/// <summary>(0010,21f0) VR=LO VM=1 Patient's Religious Preference</summary>
		public const uint PatientsReligiousPreference = 0x001021f0;

		/// <summary>(0010,2201) VR=LO VM=1 Patient Species Description</summary>
		public const uint PatientSpeciesDescription = 0x00102201;

		/// <summary>(0010,2202) VR=SQ VM=1 Patient Species Code Sequence</summary>
		public const uint PatientSpeciesCodeSequence = 0x00102202;

		/// <summary>(0010,2203) VR=CS VM=1 Patient's Sex Neutered</summary>
		public const uint PatientsSexNeutered = 0x00102203;

		/// <summary>(0010,2292) VR=LO VM=1 Patient Breed Description</summary>
		public const uint PatientBreedDescription = 0x00102292;

		/// <summary>(0010,2293) VR=SQ VM=1 Patient Breed Code Sequence</summary>
		public const uint PatientBreedCodeSequence = 0x00102293;

		/// <summary>(0010,2294) VR=SQ VM=1 Breed Registration Sequence</summary>
		public const uint BreedRegistrationSequence = 0x00102294;

		/// <summary>(0010,2295) VR=LO VM=1 Breed Registration Number</summary>
		public const uint BreedRegistrationNumber = 0x00102295;

		/// <summary>(0010,2296) VR=SQ VM=1 Breed Registry Code Sequence</summary>
		public const uint BreedRegistryCodeSequence = 0x00102296;

		/// <summary>(0010,2297) VR=PN VM=1 Responsible Person</summary>
		public const uint ResponsiblePerson = 0x00102297;

		/// <summary>(0010,2298) VR=CS VM=1 Responsible Person Role</summary>
		public const uint ResponsiblePersonRole = 0x00102298;

		/// <summary>(0010,2299) VR=LO VM=1 Responsible Organization</summary>
		public const uint ResponsibleOrganization = 0x00102299;

		/// <summary>(0010,4000) VR=LT VM=1 Patient Comments</summary>
		public const uint PatientComments = 0x00104000;

		/// <summary>(0010,9431) VR=FL VM=1 Examined Body Thickness</summary>
		public const uint ExaminedBodyThickness = 0x00109431;

		/// <summary>(0012,0010) VR=LO VM=1 Clinical Trial Sponsor Name</summary>
		public const uint ClinicalTrialSponsorName = 0x00120010;

		/// <summary>(0012,0020) VR=LO VM=1 Clinical Trial Protocol ID</summary>
		public const uint ClinicalTrialProtocolID = 0x00120020;

		/// <summary>(0012,0021) VR=LO VM=1 Clinical Trial Protocol Name</summary>
		public const uint ClinicalTrialProtocolName = 0x00120021;

		/// <summary>(0012,0030) VR=LO VM=1 Clinical Trial Site ID</summary>
		public const uint ClinicalTrialSiteID = 0x00120030;

		/// <summary>(0012,0031) VR=LO VM=1 Clinical Trial Site Name</summary>
		public const uint ClinicalTrialSiteName = 0x00120031;

		/// <summary>(0012,0040) VR=LO VM=1 Clinical Trial Subject ID</summary>
		public const uint ClinicalTrialSubjectID = 0x00120040;

		/// <summary>(0012,0042) VR=LO VM=1 Clinical Trial Subject Reading ID</summary>
		public const uint ClinicalTrialSubjectReadingID = 0x00120042;

		/// <summary>(0012,0050) VR=LO VM=1 Clinical Trial Time Point ID</summary>
		public const uint ClinicalTrialTimePointID = 0x00120050;

		/// <summary>(0012,0051) VR=ST VM=1 Clinical Trial Time Point Description</summary>
		public const uint ClinicalTrialTimePointDescription = 0x00120051;

		/// <summary>(0012,0060) VR=LO VM=1 Clinical Trial Coordinating Center Name</summary>
		public const uint ClinicalTrialCoordinatingCenterName = 0x00120060;

		/// <summary>(0012,0062) VR=CS VM=1 Patient Identity Removed</summary>
		public const uint PatientIdentityRemoved = 0x00120062;

		/// <summary>(0012,0063) VR=LO VM=1-n De-identification Method</summary>
		public const uint DeidentificationMethod = 0x00120063;

		/// <summary>(0012,0064) VR=SQ VM=1 De-identification Method Code Sequence</summary>
		public const uint DeidentificationMethodCodeSequence = 0x00120064;

		/// <summary>(0012,0071) VR=LO VM=1 Clinical Trial Series ID</summary>
		public const uint ClinicalTrialSeriesID = 0x00120071;

		/// <summary>(0012,0072) VR=LO VM=1 Clinical Trial Series Description</summary>
		public const uint ClinicalTrialSeriesDescription = 0x00120072;

		/// <summary>(0018,0010) VR=LO VM=1 Contrast/Bolus Agent</summary>
		public const uint ContrastBolusAgent = 0x00180010;

		/// <summary>(0018,0012) VR=SQ VM=1 Contrast/Bolus Agent Sequence</summary>
		public const uint ContrastBolusAgentSequence = 0x00180012;

		/// <summary>(0018,0014) VR=SQ VM=1 Contrast/Bolus Administration Route Sequence</summary>
		public const uint ContrastBolusAdministrationRouteSequence = 0x00180014;

		/// <summary>(0018,0015) VR=CS VM=1 Body Part Examined</summary>
		public const uint BodyPartExamined = 0x00180015;

		/// <summary>(0018,0020) VR=CS VM=1-n Scanning Sequence</summary>
		public const uint ScanningSequence = 0x00180020;

		/// <summary>(0018,0021) VR=CS VM=1-n Sequence Variant</summary>
		public const uint SequenceVariant = 0x00180021;

		/// <summary>(0018,0022) VR=CS VM=1-n Scan Options</summary>
		public const uint ScanOptions = 0x00180022;

		/// <summary>(0018,0023) VR=CS VM=1 MR Acquisition Type</summary>
		public const uint MRAcquisitionType = 0x00180023;

		/// <summary>(0018,0024) VR=SH VM=1 Sequence Name</summary>
		public const uint SequenceName = 0x00180024;

		/// <summary>(0018,0025) VR=CS VM=1 Angio Flag</summary>
		public const uint AngioFlag = 0x00180025;

		/// <summary>(0018,0026) VR=SQ VM=1 Intervention Drug Information Sequence</summary>
		public const uint InterventionDrugInformationSequence = 0x00180026;

		/// <summary>(0018,0027) VR=TM VM=1 Intervention Drug Stop Time</summary>
		public const uint InterventionDrugStopTime = 0x00180027;

		/// <summary>(0018,0028) VR=DS VM=1 Intervention Drug Dose</summary>
		public const uint InterventionDrugDose = 0x00180028;

		/// <summary>(0018,0029) VR=SQ VM=1 Intervention Drug Sequence</summary>
		public const uint InterventionDrugSequence = 0x00180029;

		/// <summary>(0018,002a) VR=SQ VM=1 Additional Drug Sequence</summary>
		public const uint AdditionalDrugSequence = 0x0018002a;

		/// <summary>(0018,0031) VR=LO VM=1 Radiopharmaceutical</summary>
		public const uint Radiopharmaceutical = 0x00180031;

		/// <summary>(0018,0034) VR=LO VM=1 Intervention Drug Name</summary>
		public const uint InterventionDrugName = 0x00180034;

		/// <summary>(0018,0035) VR=TM VM=1 Intervention Drug Start Time</summary>
		public const uint InterventionDrugStartTime = 0x00180035;

		/// <summary>(0018,0036) VR=SQ VM=1 Intervention Sequence</summary>
		public const uint InterventionSequence = 0x00180036;

		/// <summary>(0018,0038) VR=CS VM=1 Intervention Status</summary>
		public const uint InterventionStatus = 0x00180038;

		/// <summary>(0018,003a) VR=ST VM=1 Intervention Description</summary>
		public const uint InterventionDescription = 0x0018003a;

		/// <summary>(0018,0040) VR=IS VM=1 Cine Rate</summary>
		public const uint CineRate = 0x00180040;

		/// <summary>(0018,0050) VR=DS VM=1 Slice Thickness</summary>
		public const uint SliceThickness = 0x00180050;

		/// <summary>(0018,0060) VR=DS VM=1 KVP</summary>
		public const uint KVP = 0x00180060;

		/// <summary>(0018,0070) VR=IS VM=1 Counts Accumulated</summary>
		public const uint CountsAccumulated = 0x00180070;

		/// <summary>(0018,0071) VR=CS VM=1 Acquisition Termination Condition</summary>
		public const uint AcquisitionTerminationCondition = 0x00180071;

		/// <summary>(0018,0072) VR=DS VM=1 Effective Duration</summary>
		public const uint EffectiveDuration = 0x00180072;

		/// <summary>(0018,0073) VR=CS VM=1 Acquisition Start Condition</summary>
		public const uint AcquisitionStartCondition = 0x00180073;

		/// <summary>(0018,0074) VR=IS VM=1 Acquisition Start Condition Data</summary>
		public const uint AcquisitionStartConditionData = 0x00180074;

		/// <summary>(0018,0075) VR=IS VM=1 Acquisition Termination Condition Data</summary>
		public const uint AcquisitionTerminationConditionData = 0x00180075;

		/// <summary>(0018,0080) VR=DS VM=1 Repetition Time</summary>
		public const uint RepetitionTime = 0x00180080;

		/// <summary>(0018,0081) VR=DS VM=1 Echo Time</summary>
		public const uint EchoTime = 0x00180081;

		/// <summary>(0018,0082) VR=DS VM=1 Inversion Time</summary>
		public const uint InversionTime = 0x00180082;

		/// <summary>(0018,0083) VR=DS VM=1 Number of Averages</summary>
		public const uint NumberOfAverages = 0x00180083;

		/// <summary>(0018,0084) VR=DS VM=1 Imaging Frequency</summary>
		public const uint ImagingFrequency = 0x00180084;

		/// <summary>(0018,0085) VR=SH VM=1 Imaged Nucleus</summary>
		public const uint ImagedNucleus = 0x00180085;

		/// <summary>(0018,0086) VR=IS VM=1-n Echo Number(s)</summary>
		public const uint EchoNumbers = 0x00180086;

		/// <summary>(0018,0087) VR=DS VM=1 Magnetic Field Strength</summary>
		public const uint MagneticFieldStrength = 0x00180087;

		/// <summary>(0018,0088) VR=DS VM=1 Spacing Between Slices</summary>
		public const uint SpacingBetweenSlices = 0x00180088;

		/// <summary>(0018,0089) VR=IS VM=1 Number of Phase Encoding Steps</summary>
		public const uint NumberOfPhaseEncodingSteps = 0x00180089;

		/// <summary>(0018,0090) VR=DS VM=1 Data Collection Diameter</summary>
		public const uint DataCollectionDiameter = 0x00180090;

		/// <summary>(0018,0091) VR=IS VM=1 Echo Train Length</summary>
		public const uint EchoTrainLength = 0x00180091;

		/// <summary>(0018,0093) VR=DS VM=1 Percent Sampling</summary>
		public const uint PercentSampling = 0x00180093;

		/// <summary>(0018,0094) VR=DS VM=1 Percent Phase Field of View</summary>
		public const uint PercentPhaseFieldOfView = 0x00180094;

		/// <summary>(0018,0095) VR=DS VM=1 Pixel Bandwidth</summary>
		public const uint PixelBandwidth = 0x00180095;

		/// <summary>(0018,1000) VR=LO VM=1 Device Serial Number</summary>
		public const uint DeviceSerialNumber = 0x00181000;

		/// <summary>(0018,1002) VR=UI VM=1 Device UID</summary>
		public const uint DeviceUID = 0x00181002;

		/// <summary>(0018,1003) VR=LO VM=1 Device ID</summary>
		public const uint DeviceID = 0x00181003;

		/// <summary>(0018,1004) VR=LO VM=1 Plate ID</summary>
		public const uint PlateID = 0x00181004;

		/// <summary>(0018,1005) VR=LO VM=1 Generator ID</summary>
		public const uint GeneratorID = 0x00181005;

		/// <summary>(0018,1006) VR=LO VM=1 Grid ID</summary>
		public const uint GridID = 0x00181006;

		/// <summary>(0018,1007) VR=LO VM=1 Cassette ID</summary>
		public const uint CassetteID = 0x00181007;

		/// <summary>(0018,1008) VR=LO VM=1 Gantry ID</summary>
		public const uint GantryID = 0x00181008;

		/// <summary>(0018,1010) VR=LO VM=1 Secondary Capture Device ID</summary>
		public const uint SecondaryCaptureDeviceID = 0x00181010;

		/// <summary>(0018,1012) VR=DA VM=1 Date of Secondary Capture</summary>
		public const uint DateOfSecondaryCapture = 0x00181012;

		/// <summary>(0018,1014) VR=TM VM=1 Time of Secondary Capture</summary>
		public const uint TimeOfSecondaryCapture = 0x00181014;

		/// <summary>(0018,1016) VR=LO VM=1 Secondary Capture Device Manufacturers</summary>
		public const uint SecondaryCaptureDeviceManufacturers = 0x00181016;

		/// <summary>(0018,1018) VR=LO VM=1 Secondary Capture Device Manufacturer's Model Name</summary>
		public const uint SecondaryCaptureDeviceManufacturersModelName = 0x00181018;

		/// <summary>(0018,1019) VR=LO VM=1-n Secondary Capture Device Software Version(s)</summary>
		public const uint SecondaryCaptureDeviceSoftwareVersions = 0x00181019;

		/// <summary>(0018,1020) VR=LO VM=1-n Software Version(s)</summary>
		public const uint SoftwareVersions = 0x00181020;

		/// <summary>(0018,1022) VR=SH VM=1 Video Image Format Acquired</summary>
		public const uint VideoImageFormatAcquired = 0x00181022;

		/// <summary>(0018,1023) VR=LO VM=1 Digital Image Format Acquired</summary>
		public const uint DigitalImageFormatAcquired = 0x00181023;

		/// <summary>(0018,1030) VR=LO VM=1 Protocol Name</summary>
		public const uint ProtocolName = 0x00181030;

		/// <summary>(0018,1040) VR=LO VM=1 Contrast/Bolus Route</summary>
		public const uint ContrastBolusRoute = 0x00181040;

		/// <summary>(0018,1041) VR=DS VM=1 Contrast/Bolus Volume</summary>
		public const uint ContrastBolusVolume = 0x00181041;

		/// <summary>(0018,1042) VR=TM VM=1 Contrast/Bolus Start Time</summary>
		public const uint ContrastBolusStartTime = 0x00181042;

		/// <summary>(0018,1043) VR=TM VM=1 Contrast/Bolus Stop Time</summary>
		public const uint ContrastBolusStopTime = 0x00181043;

		/// <summary>(0018,1044) VR=DS VM=1 Contrast/Bolus Total Dose</summary>
		public const uint ContrastBolusTotalDose = 0x00181044;

		/// <summary>(0018,1045) VR=IS VM=1 Syringe Counts</summary>
		public const uint SyringeCounts = 0x00181045;

		/// <summary>(0018,1046) VR=DS VM=1-n Contrast Flow Rate</summary>
		public const uint ContrastFlowRate = 0x00181046;

		/// <summary>(0018,1047) VR=DS VM=1-n Contrast Flow Duration</summary>
		public const uint ContrastFlowDuration = 0x00181047;

		/// <summary>(0018,1048) VR=CS VM=1 Contrast/Bolus Ingredient</summary>
		public const uint ContrastBolusIngredient = 0x00181048;

		/// <summary>(0018,1049) VR=DS VM=1 Contrast/Bolus Ingredient Concentration</summary>
		public const uint ContrastBolusIngredientConcentration = 0x00181049;

		/// <summary>(0018,1050) VR=DS VM=1 Spatial Resolution</summary>
		public const uint SpatialResolution = 0x00181050;

		/// <summary>(0018,1060) VR=DS VM=1 Trigger Time</summary>
		public const uint TriggerTime = 0x00181060;

		/// <summary>(0018,1061) VR=LO VM=1 Trigger Source or Type</summary>
		public const uint TriggerSourceOrType = 0x00181061;

		/// <summary>(0018,1062) VR=IS VM=1 Nominal Interval</summary>
		public const uint NominalInterval = 0x00181062;

		/// <summary>(0018,1063) VR=DS VM=1 Frame Time</summary>
		public const uint FrameTime = 0x00181063;

		/// <summary>(0018,1064) VR=LO VM=1 Cardiac Framing Type</summary>
		public const uint CardiacFramingType = 0x00181064;

		/// <summary>(0018,1065) VR=DS VM=1-n Frame Time Vector</summary>
		public const uint FrameTimeVector = 0x00181065;

		/// <summary>(0018,1066) VR=DS VM=1 Frame Delay</summary>
		public const uint FrameDelay = 0x00181066;

		/// <summary>(0018,1067) VR=DS VM=1 Image Trigger Delay</summary>
		public const uint ImageTriggerDelay = 0x00181067;

		/// <summary>(0018,1068) VR=DS VM=1 Multiplex Group Time Offset</summary>
		public const uint MultiplexGroupTimeOffset = 0x00181068;

		/// <summary>(0018,1069) VR=DS VM=1 Trigger Time Offset</summary>
		public const uint TriggerTimeOffset = 0x00181069;

		/// <summary>(0018,106a) VR=CS VM=1 Synchronization Trigger</summary>
		public const uint SynchronizationTrigger = 0x0018106a;

		/// <summary>(0018,106c) VR=US VM=2 Synchronization Channel</summary>
		public const uint SynchronizationChannel = 0x0018106c;

		/// <summary>(0018,106e) VR=UL VM=1 Trigger Sample Position</summary>
		public const uint TriggerSamplePosition = 0x0018106e;

		/// <summary>(0018,1070) VR=LO VM=1 Radiopharmaceutical Route</summary>
		public const uint RadiopharmaceuticalRoute = 0x00181070;

		/// <summary>(0018,1071) VR=DS VM=1 Radiopharmaceutical Volume</summary>
		public const uint RadiopharmaceuticalVolume = 0x00181071;

		/// <summary>(0018,1072) VR=TM VM=1 Radiopharmaceutical Start Time</summary>
		public const uint RadiopharmaceuticalStartTime = 0x00181072;

		/// <summary>(0018,1073) VR=TM VM=1 Radiopharmaceutical Stop Time</summary>
		public const uint RadiopharmaceuticalStopTime = 0x00181073;

		/// <summary>(0018,1074) VR=DS VM=1 Radionuclide Total Dose</summary>
		public const uint RadionuclideTotalDose = 0x00181074;

		/// <summary>(0018,1075) VR=DS VM=1 Radionuclide Half Life</summary>
		public const uint RadionuclideHalfLife = 0x00181075;

		/// <summary>(0018,1076) VR=DS VM=1 Radionuclide Positron Fraction</summary>
		public const uint RadionuclidePositronFraction = 0x00181076;

		/// <summary>(0018,1077) VR=DS VM=1 Radiopharmaceutical Specific Activity</summary>
		public const uint RadiopharmaceuticalSpecificActivity = 0x00181077;

		/// <summary>(0018,1078) VR=DT VM=1 Radiopharmaceutical Start DateTime</summary>
		public const uint RadiopharmaceuticalStartDateTime = 0x00181078;

		/// <summary>(0018,1079) VR=DT VM=1 Radiopharmaceutical Stop DateTime</summary>
		public const uint RadiopharmaceuticalStopDateTime = 0x00181079;

		/// <summary>(0018,1080) VR=CS VM=1 Beat Rejection Flag</summary>
		public const uint BeatRejectionFlag = 0x00181080;

		/// <summary>(0018,1081) VR=IS VM=1 Low R-R Value</summary>
		public const uint LowRRValue = 0x00181081;

		/// <summary>(0018,1082) VR=IS VM=1 High R-R Value</summary>
		public const uint HighRRValue = 0x00181082;

		/// <summary>(0018,1083) VR=IS VM=1 Intervals Acquired</summary>
		public const uint IntervalsAcquired = 0x00181083;

		/// <summary>(0018,1084) VR=IS VM=1 Intervals Rejected</summary>
		public const uint IntervalsRejected = 0x00181084;

		/// <summary>(0018,1085) VR=LO VM=1 PVC Rejection</summary>
		public const uint PVCRejection = 0x00181085;

		/// <summary>(0018,1086) VR=IS VM=1 Skip Beats</summary>
		public const uint SkipBeats = 0x00181086;

		/// <summary>(0018,1088) VR=IS VM=1 Heart Rate</summary>
		public const uint HeartRate = 0x00181088;

		/// <summary>(0018,1090) VR=IS VM=1 Cardiac Number of Images</summary>
		public const uint CardiacNumberOfImages = 0x00181090;

		/// <summary>(0018,1094) VR=IS VM=1 Trigger Window</summary>
		public const uint TriggerWindow = 0x00181094;

		/// <summary>(0018,1100) VR=DS VM=1 Reconstruction Diameter</summary>
		public const uint ReconstructionDiameter = 0x00181100;

		/// <summary>(0018,1110) VR=DS VM=1 Distance Source to Detector</summary>
		public const uint DistanceSourceToDetector = 0x00181110;

		/// <summary>(0018,1111) VR=DS VM=1 Distance Source to Patient</summary>
		public const uint DistanceSourceToPatient = 0x00181111;

		/// <summary>(0018,1114) VR=DS VM=1 Estimated Radiographic Magnification Factor</summary>
		public const uint EstimatedRadiographicMagnificationFactor = 0x00181114;

		/// <summary>(0018,1120) VR=DS VM=1 Gantry/Detector Tilt</summary>
		public const uint GantryDetectorTilt = 0x00181120;

		/// <summary>(0018,1121) VR=DS VM=1 Gantry/Detector Slew</summary>
		public const uint GantryDetectorSlew = 0x00181121;

		/// <summary>(0018,1130) VR=DS VM=1 Table Height</summary>
		public const uint TableHeight = 0x00181130;

		/// <summary>(0018,1131) VR=DS VM=1 Table Traverse</summary>
		public const uint TableTraverse = 0x00181131;

		/// <summary>(0018,1134) VR=CS VM=1 Table Motion</summary>
		public const uint TableMotion = 0x00181134;

		/// <summary>(0018,1135) VR=DS VM=1-n Table Vertical Increment</summary>
		public const uint TableVerticalIncrement = 0x00181135;

		/// <summary>(0018,1136) VR=DS VM=1-n Table Lateral Increment</summary>
		public const uint TableLateralIncrement = 0x00181136;

		/// <summary>(0018,1137) VR=DS VM=1-n Table Longitudinal Increment</summary>
		public const uint TableLongitudinalIncrement = 0x00181137;

		/// <summary>(0018,1138) VR=DS VM=1 Table Angle</summary>
		public const uint TableAngle = 0x00181138;

		/// <summary>(0018,113a) VR=CS VM=1 Table Type</summary>
		public const uint TableType = 0x0018113a;

		/// <summary>(0018,1140) VR=CS VM=1 Rotation Direction</summary>
		public const uint RotationDirection = 0x00181140;

		/// <summary>(0018,1142) VR=DS VM=1-n Radial Position</summary>
		public const uint RadialPosition = 0x00181142;

		/// <summary>(0018,1143) VR=DS VM=1 Scan Arc</summary>
		public const uint ScanArc = 0x00181143;

		/// <summary>(0018,1144) VR=DS VM=1 Angular Step</summary>
		public const uint AngularStep = 0x00181144;

		/// <summary>(0018,1145) VR=DS VM=1 Center of Rotation Offset</summary>
		public const uint CenterOfRotationOffset = 0x00181145;

		/// <summary>(0018,1147) VR=CS VM=1 Field of View Shape</summary>
		public const uint FieldOfViewShape = 0x00181147;

		/// <summary>(0018,1149) VR=IS VM=1-2 Field of View Dimension(s)</summary>
		public const uint FieldOfViewDimensions = 0x00181149;

		/// <summary>(0018,1150) VR=IS VM=1 Exposure Time</summary>
		public const uint ExposureTime = 0x00181150;

		/// <summary>(0018,1151) VR=IS VM=1 X-Ray Tube Current</summary>
		public const uint XRayTubeCurrent = 0x00181151;

		/// <summary>(0018,1152) VR=IS VM=1 Exposure</summary>
		public const uint Exposure = 0x00181152;

		/// <summary>(0018,1153) VR=IS VM=1 Exposure in uAs</summary>
		public const uint ExposureInMicroAs = 0x00181153;

		/// <summary>(0018,1154) VR=DS VM=1 Average Pulse Width</summary>
		public const uint AveragePulseWidth = 0x00181154;

		/// <summary>(0018,1155) VR=CS VM=1 Radiation Setting</summary>
		public const uint RadiationSetting = 0x00181155;

		/// <summary>(0018,1156) VR=CS VM=1 Rectification Type</summary>
		public const uint RectificationType = 0x00181156;

		/// <summary>(0018,115a) VR=CS VM=1 Radiation Mode</summary>
		public const uint RadiationMode = 0x0018115a;

		/// <summary>(0018,115e) VR=DS VM=1 Image and Fluoroscopy Area Dose Product</summary>
		public const uint ImageAndFluoroscopyAreaDoseProduct = 0x0018115e;

		/// <summary>(0018,1160) VR=SH VM=1 Filter Type</summary>
		public const uint FilterType = 0x00181160;

		/// <summary>(0018,1161) VR=LO VM=1-n Type of Filters</summary>
		public const uint TypeOfFilters = 0x00181161;

		/// <summary>(0018,1162) VR=DS VM=1 Intensifier Size</summary>
		public const uint IntensifierSize = 0x00181162;

		/// <summary>(0018,1164) VR=DS VM=2 Imager Pixel Spacing</summary>
		public const uint ImagerPixelSpacing = 0x00181164;

		/// <summary>(0018,1166) VR=CS VM=1-n Grid</summary>
		public const uint Grid = 0x00181166;

		/// <summary>(0018,1170) VR=IS VM=1 Generator Power</summary>
		public const uint GeneratorPower = 0x00181170;

		/// <summary>(0018,1180) VR=SH VM=1 Collimator/grid Name</summary>
		public const uint CollimatorgridName = 0x00181180;

		/// <summary>(0018,1181) VR=CS VM=1 Collimator Type</summary>
		public const uint CollimatorType = 0x00181181;

		/// <summary>(0018,1182) VR=IS VM=1-2 Focal Distance</summary>
		public const uint FocalDistance = 0x00181182;

		/// <summary>(0018,1183) VR=DS VM=1-2 X Focus Center</summary>
		public const uint XFocusCenter = 0x00181183;

		/// <summary>(0018,1184) VR=DS VM=1-2 Y Focus Center</summary>
		public const uint YFocusCenter = 0x00181184;

		/// <summary>(0018,1190) VR=DS VM=1-n Focal Spot(s)</summary>
		public const uint FocalSpots = 0x00181190;

		/// <summary>(0018,1191) VR=CS VM=1 Anode Target Material</summary>
		public const uint AnodeTargetMaterial = 0x00181191;

		/// <summary>(0018,11a0) VR=DS VM=1 Body Part Thickness</summary>
		public const uint BodyPartThickness = 0x001811a0;

		/// <summary>(0018,11a2) VR=DS VM=1 Compression Force</summary>
		public const uint CompressionForce = 0x001811a2;

		/// <summary>(0018,1200) VR=DA VM=1-n Date of Last Calibration</summary>
		public const uint DateOfLastCalibration = 0x00181200;

		/// <summary>(0018,1201) VR=TM VM=1-n Time of Last Calibration</summary>
		public const uint TimeOfLastCalibration = 0x00181201;

		/// <summary>(0018,1210) VR=SH VM=1-n Convolution Kernel</summary>
		public const uint ConvolutionKernel = 0x00181210;

		/// <summary>(0018,1242) VR=IS VM=1 Actual Frame Duration</summary>
		public const uint ActualFrameDuration = 0x00181242;

		/// <summary>(0018,1243) VR=IS VM=1 Count Rate</summary>
		public const uint CountRate = 0x00181243;

		/// <summary>(0018,1244) VR=US VM=1 Preferred Playback Sequencing</summary>
		public const uint PreferredPlaybackSequencing = 0x00181244;

		/// <summary>(0018,1250) VR=SH VM=1 Receive Coil Name</summary>
		public const uint ReceiveCoilName = 0x00181250;

		/// <summary>(0018,1251) VR=SH VM=1 Transmit Coil Name</summary>
		public const uint TransmitCoilName = 0x00181251;

		/// <summary>(0018,1260) VR=SH VM=1 Plate Type</summary>
		public const uint PlateType = 0x00181260;

		/// <summary>(0018,1261) VR=LO VM=1 Phosphor Type</summary>
		public const uint PhosphorType = 0x00181261;

		/// <summary>(0018,1300) VR=DS VM=1 Scan Velocity</summary>
		public const uint ScanVelocity = 0x00181300;

		/// <summary>(0018,1301) VR=CS VM=1-n Whole Body Technique</summary>
		public const uint WholeBodyTechnique = 0x00181301;

		/// <summary>(0018,1302) VR=IS VM=1 Scan Length</summary>
		public const uint ScanLength = 0x00181302;

		/// <summary>(0018,1310) VR=US VM=4 Acquisition Matrix</summary>
		public const uint AcquisitionMatrix = 0x00181310;

		/// <summary>(0018,1312) VR=CS VM=1 In-plane Phase Encoding Direction</summary>
		public const uint InplanePhaseEncodingDirection = 0x00181312;

		/// <summary>(0018,1314) VR=DS VM=1 Flip Angle</summary>
		public const uint FlipAngle = 0x00181314;

		/// <summary>(0018,1315) VR=CS VM=1 Variable Flip Angle Flag</summary>
		public const uint VariableFlipAngleFlag = 0x00181315;

		/// <summary>(0018,1316) VR=DS VM=1 SAR</summary>
		public const uint SAR = 0x00181316;

		/// <summary>(0018,1318) VR=DS VM=1 dB/dt</summary>
		public const uint DBdt = 0x00181318;

		/// <summary>(0018,1400) VR=LO VM=1 Acquisition Device Processing Description</summary>
		public const uint AcquisitionDeviceProcessingDescription = 0x00181400;

		/// <summary>(0018,1401) VR=LO VM=1 Acquisition Device Processing Code</summary>
		public const uint AcquisitionDeviceProcessingCode = 0x00181401;

		/// <summary>(0018,1402) VR=CS VM=1 Cassette Orientation</summary>
		public const uint CassetteOrientation = 0x00181402;

		/// <summary>(0018,1403) VR=CS VM=1 Cassette Size</summary>
		public const uint CassetteSize = 0x00181403;

		/// <summary>(0018,1404) VR=US VM=1 Exposures on Plate</summary>
		public const uint ExposuresOnPlate = 0x00181404;

		/// <summary>(0018,1405) VR=IS VM=1 Relative X-Ray Exposure</summary>
		public const uint RelativeXRayExposure = 0x00181405;

		/// <summary>(0018,1450) VR=DS VM=1 Column Angulation</summary>
		public const uint ColumnAngulation = 0x00181450;

		/// <summary>(0018,1460) VR=DS VM=1 Tomo Layer Height</summary>
		public const uint TomoLayerHeight = 0x00181460;

		/// <summary>(0018,1470) VR=DS VM=1 Tomo Angle</summary>
		public const uint TomoAngle = 0x00181470;

		/// <summary>(0018,1480) VR=DS VM=1 Tomo Time</summary>
		public const uint TomoTime = 0x00181480;

		/// <summary>(0018,1490) VR=CS VM=1 Tomo Type</summary>
		public const uint TomoType = 0x00181490;

		/// <summary>(0018,1491) VR=CS VM=1 Tomo Class</summary>
		public const uint TomoClass = 0x00181491;

		/// <summary>(0018,1495) VR=IS VM=1 Number of Tomosynthesis Source Images</summary>
		public const uint NumberOfTomosynthesisSourceImages = 0x00181495;

		/// <summary>(0018,1500) VR=CS VM=1 Positioner Motion</summary>
		public const uint PositionerMotion = 0x00181500;

		/// <summary>(0018,1508) VR=CS VM=1 Positioner Type</summary>
		public const uint PositionerType = 0x00181508;

		/// <summary>(0018,1510) VR=DS VM=1 Positioner Primary Angle</summary>
		public const uint PositionerPrimaryAngle = 0x00181510;

		/// <summary>(0018,1511) VR=DS VM=1 Positioner Secondary Angle</summary>
		public const uint PositionerSecondaryAngle = 0x00181511;

		/// <summary>(0018,1520) VR=DS VM=1-n Positioner Primary Angle Increment</summary>
		public const uint PositionerPrimaryAngleIncrement = 0x00181520;

		/// <summary>(0018,1521) VR=DS VM=1-n Positioner Secondary Angle Increment</summary>
		public const uint PositionerSecondaryAngleIncrement = 0x00181521;

		/// <summary>(0018,1530) VR=DS VM=1 Detector Primary Angle</summary>
		public const uint DetectorPrimaryAngle = 0x00181530;

		/// <summary>(0018,1531) VR=DS VM=1 Detector Secondary Angle</summary>
		public const uint DetectorSecondaryAngle = 0x00181531;

		/// <summary>(0018,1600) VR=CS VM=1-3 Shutter Shape</summary>
		public const uint ShutterShape = 0x00181600;

		/// <summary>(0018,1602) VR=IS VM=1 Shutter Left Vertical Edge</summary>
		public const uint ShutterLeftVerticalEdge = 0x00181602;

		/// <summary>(0018,1604) VR=IS VM=1 Shutter Right Vertical Edge</summary>
		public const uint ShutterRightVerticalEdge = 0x00181604;

		/// <summary>(0018,1606) VR=IS VM=1 Shutter Upper Horizontal Edge</summary>
		public const uint ShutterUpperHorizontalEdge = 0x00181606;

		/// <summary>(0018,1608) VR=IS VM=1 Shutter Lower Horizontal Edge</summary>
		public const uint ShutterLowerHorizontalEdge = 0x00181608;

		/// <summary>(0018,1610) VR=IS VM=2 Center of Circular Shutter</summary>
		public const uint CenterOfCircularShutter = 0x00181610;

		/// <summary>(0018,1612) VR=IS VM=1 Radius of Circular Shutter</summary>
		public const uint RadiusOfCircularShutter = 0x00181612;

		/// <summary>(0018,1620) VR=IS VM=2-2n Vertices of the Polygonal Shutter</summary>
		public const uint VerticesOfThePolygonalShutter = 0x00181620;

		/// <summary>(0018,1622) VR=US VM=1 Shutter Presentation Value</summary>
		public const uint ShutterPresentationValue = 0x00181622;

		/// <summary>(0018,1623) VR=US VM=1 Shutter Overlay Group</summary>
		public const uint ShutterOverlayGroup = 0x00181623;

		/// <summary>(0018,1624) VR=US VM=3 Shutter Presentation Color CIELab Value</summary>
		public const uint ShutterPresentationColorCIELabValue = 0x00181624;

		/// <summary>(0018,1700) VR=CS VM=1-3 Collimator Shape</summary>
		public const uint CollimatorShape = 0x00181700;

		/// <summary>(0018,1702) VR=IS VM=1 Collimator Left Vertical Edge</summary>
		public const uint CollimatorLeftVerticalEdge = 0x00181702;

		/// <summary>(0018,1704) VR=IS VM=1 Collimator Right Vertical Edge</summary>
		public const uint CollimatorRightVerticalEdge = 0x00181704;

		/// <summary>(0018,1706) VR=IS VM=1 Collimator Upper Horizontal Edge</summary>
		public const uint CollimatorUpperHorizontalEdge = 0x00181706;

		/// <summary>(0018,1708) VR=IS VM=1 Collimator Lower Horizontal Edge</summary>
		public const uint CollimatorLowerHorizontalEdge = 0x00181708;

		/// <summary>(0018,1710) VR=IS VM=2 Center of Circular Collimator</summary>
		public const uint CenterOfCircularCollimator = 0x00181710;

		/// <summary>(0018,1712) VR=IS VM=1 Radius of Circular Collimator</summary>
		public const uint RadiusOfCircularCollimator = 0x00181712;

		/// <summary>(0018,1720) VR=IS VM=2-2n Vertices of the Polygonal Collimator</summary>
		public const uint VerticesOfThePolygonalCollimator = 0x00181720;

		/// <summary>(0018,1800) VR=CS VM=1 Acquisition Time Synchronized</summary>
		public const uint AcquisitionTimeSynchronized = 0x00181800;

		/// <summary>(0018,1801) VR=SH VM=1 Time Source</summary>
		public const uint TimeSource = 0x00181801;

		/// <summary>(0018,1802) VR=CS VM=1 Time Distribution Protocol</summary>
		public const uint TimeDistributionProtocol = 0x00181802;

		/// <summary>(0018,1803) VR=LO VM=1 NTP Source Address</summary>
		public const uint NTPSourceAddress = 0x00181803;

		/// <summary>(0018,2001) VR=IS VM=1-n Page Number Vector</summary>
		public const uint PageNumberVector = 0x00182001;

		/// <summary>(0018,2002) VR=SH VM=1-n Frame Label Vector</summary>
		public const uint FrameLabelVector = 0x00182002;

		/// <summary>(0018,2003) VR=DS VM=1-n Frame Primary Angle Vector</summary>
		public const uint FramePrimaryAngleVector = 0x00182003;

		/// <summary>(0018,2004) VR=DS VM=1-n Frame Secondary Angle Vector</summary>
		public const uint FrameSecondaryAngleVector = 0x00182004;

		/// <summary>(0018,2005) VR=DS VM=1-n Slice Location Vector</summary>
		public const uint SliceLocationVector = 0x00182005;

		/// <summary>(0018,2006) VR=SH VM=1-n Display Window Label Vector</summary>
		public const uint DisplayWindowLabelVector = 0x00182006;

		/// <summary>(0018,2010) VR=DS VM=2 Nominal Scanned Pixel Spacing</summary>
		public const uint NominalScannedPixelSpacing = 0x00182010;

		/// <summary>(0018,2020) VR=CS VM=1 Digitizing Device Transport Direction</summary>
		public const uint DigitizingDeviceTransportDirection = 0x00182020;

		/// <summary>(0018,2030) VR=DS VM=1 Rotation of Scanned Film</summary>
		public const uint RotationOfScannedFilm = 0x00182030;

		/// <summary>(0018,3100) VR=CS VM=1 IVUS Acquisition</summary>
		public const uint IVUSAcquisition = 0x00183100;

		/// <summary>(0018,3101) VR=DS VM=1 IVUS Pullback Rate</summary>
		public const uint IVUSPullbackRate = 0x00183101;

		/// <summary>(0018,3102) VR=DS VM=1 IVUS Gated Rate</summary>
		public const uint IVUSGatedRate = 0x00183102;

		/// <summary>(0018,3103) VR=IS VM=1 IVUS Pullback Start Frame Number</summary>
		public const uint IVUSPullbackStartFrameNumber = 0x00183103;

		/// <summary>(0018,3104) VR=IS VM=1 IVUS Pullback Stop Frame Number</summary>
		public const uint IVUSPullbackStopFrameNumber = 0x00183104;

		/// <summary>(0018,3105) VR=IS VM=1-n Lesion Number</summary>
		public const uint LesionNumber = 0x00183105;

		/// <summary>(0018,5000) VR=SH VM=1-n Output Power</summary>
		public const uint OutputPower = 0x00185000;

		/// <summary>(0018,5010) VR=LO VM=3 Transducer Data</summary>
		public const uint TransducerData = 0x00185010;

		/// <summary>(0018,5012) VR=DS VM=1 Focus Depth</summary>
		public const uint FocusDepth = 0x00185012;

		/// <summary>(0018,5020) VR=LO VM=1 Processing Function</summary>
		public const uint ProcessingFunction = 0x00185020;

		/// <summary>(0018,5022) VR=DS VM=1 Mechanical Index</summary>
		public const uint MechanicalIndex = 0x00185022;

		/// <summary>(0018,5024) VR=DS VM=1 Bone Thermal Index</summary>
		public const uint BoneThermalIndex = 0x00185024;

		/// <summary>(0018,5026) VR=DS VM=1 Cranial Thermal Index</summary>
		public const uint CranialThermalIndex = 0x00185026;

		/// <summary>(0018,5027) VR=DS VM=1 Soft Tissue Thermal Index</summary>
		public const uint SoftTissueThermalIndex = 0x00185027;

		/// <summary>(0018,5028) VR=DS VM=1 Soft Tissue-focus Thermal Index</summary>
		public const uint SoftTissuefocusThermalIndex = 0x00185028;

		/// <summary>(0018,5029) VR=DS VM=1 Soft Tissue-surface Thermal Index</summary>
		public const uint SoftTissuesurfaceThermalIndex = 0x00185029;

		/// <summary>(0018,5050) VR=IS VM=1 Depth of Scan Field</summary>
		public const uint DepthOfScanField = 0x00185050;

		/// <summary>(0018,5100) VR=CS VM=1 Patient Position</summary>
		public const uint PatientPosition = 0x00185100;

		/// <summary>(0018,5101) VR=CS VM=1 View Position</summary>
		public const uint ViewPosition = 0x00185101;

		/// <summary>(0018,5104) VR=SQ VM=1 Projection Eponymous Name Code Sequence</summary>
		public const uint ProjectionEponymousNameCodeSequence = 0x00185104;

		/// <summary>(0018,6000) VR=DS VM=1 Sensitivity</summary>
		public const uint Sensitivity = 0x00186000;

		/// <summary>(0018,6011) VR=SQ VM=1 Sequence of Ultrasound Regions</summary>
		public const uint SequenceOfUltrasoundRegions = 0x00186011;

		/// <summary>(0018,6012) VR=US VM=1 Region Spatial Format</summary>
		public const uint RegionSpatialFormat = 0x00186012;

		/// <summary>(0018,6014) VR=US VM=1 Region Data Type</summary>
		public const uint RegionDataType = 0x00186014;

		/// <summary>(0018,6016) VR=UL VM=1 Region Flags</summary>
		public const uint RegionFlags = 0x00186016;

		/// <summary>(0018,6018) VR=UL VM=1 Region Location Min X0</summary>
		public const uint RegionLocationMinX0 = 0x00186018;

		/// <summary>(0018,601a) VR=UL VM=1 Region Location Min Y0</summary>
		public const uint RegionLocationMinY0 = 0x0018601a;

		/// <summary>(0018,601c) VR=UL VM=1 Region Location Max X1</summary>
		public const uint RegionLocationMaxX1 = 0x0018601c;

		/// <summary>(0018,601e) VR=UL VM=1 Region Location Max Y1</summary>
		public const uint RegionLocationMaxY1 = 0x0018601e;

		/// <summary>(0018,6020) VR=SL VM=1 Reference Pixel X0</summary>
		public const uint ReferencePixelX0 = 0x00186020;

		/// <summary>(0018,6022) VR=SL VM=1 Reference Pixel Y0</summary>
		public const uint ReferencePixelY0 = 0x00186022;

		/// <summary>(0018,6024) VR=US VM=1 Physical Units X Direction</summary>
		public const uint PhysicalUnitsXDirection = 0x00186024;

		/// <summary>(0018,6026) VR=US VM=1 Physical Units Y Direction</summary>
		public const uint PhysicalUnitsYDirection = 0x00186026;

		/// <summary>(0018,6028) VR=FD VM=1 Reference Pixel Physical Value X</summary>
		public const uint ReferencePixelPhysicalValueX = 0x00186028;

		/// <summary>(0018,602a) VR=FD VM=1 Reference Pixel Physical Value Y</summary>
		public const uint ReferencePixelPhysicalValueY = 0x0018602a;

		/// <summary>(0018,602c) VR=FD VM=1 Physical Delta X</summary>
		public const uint PhysicalDeltaX = 0x0018602c;

		/// <summary>(0018,602e) VR=FD VM=1 Physical Delta Y</summary>
		public const uint PhysicalDeltaY = 0x0018602e;

		/// <summary>(0018,6030) VR=UL VM=1 Transducer Frequency</summary>
		public const uint TransducerFrequency = 0x00186030;

		/// <summary>(0018,6031) VR=CS VM=1 Transducer Type</summary>
		public const uint TransducerType = 0x00186031;

		/// <summary>(0018,6032) VR=UL VM=1 Pulse Repetition Frequency</summary>
		public const uint PulseRepetitionFrequency = 0x00186032;

		/// <summary>(0018,6034) VR=FD VM=1 Doppler Correction Angle</summary>
		public const uint DopplerCorrectionAngle = 0x00186034;

		/// <summary>(0018,6036) VR=FD VM=1 Steering Angle</summary>
		public const uint SteeringAngle = 0x00186036;

		/// <summary>(0018,6039) VR=SL VM=1 Doppler Sample Volume X Position</summary>
		public const uint DopplerSampleVolumeXPosition = 0x00186039;

		/// <summary>(0018,603b) VR=SL VM=1 Doppler Sample Volume Y Position</summary>
		public const uint DopplerSampleVolumeYPosition = 0x0018603b;

		/// <summary>(0018,603d) VR=SL VM=1 TM-Line Position X0</summary>
		public const uint TMLinePositionX0 = 0x0018603d;

		/// <summary>(0018,603f) VR=SL VM=1 TM-Line Position Y0</summary>
		public const uint TMLinePositionY0 = 0x0018603f;

		/// <summary>(0018,6041) VR=SL VM=1 TM-Line Position X1</summary>
		public const uint TMLinePositionX1 = 0x00186041;

		/// <summary>(0018,6043) VR=SL VM=1 TM-Line Position Y1</summary>
		public const uint TMLinePositionY1 = 0x00186043;

		/// <summary>(0018,6044) VR=US VM=1 Pixel Component Organization</summary>
		public const uint PixelComponentOrganization = 0x00186044;

		/// <summary>(0018,6046) VR=UL VM=1 Pixel Component Mask</summary>
		public const uint PixelComponentMask = 0x00186046;

		/// <summary>(0018,6048) VR=UL VM=1 Pixel Component Range Start</summary>
		public const uint PixelComponentRangeStart = 0x00186048;

		/// <summary>(0018,604a) VR=UL VM=1 Pixel Component Range Stop</summary>
		public const uint PixelComponentRangeStop = 0x0018604a;

		/// <summary>(0018,604c) VR=US VM=1 Pixel Component Physical Units</summary>
		public const uint PixelComponentPhysicalUnits = 0x0018604c;

		/// <summary>(0018,604e) VR=US VM=1 Pixel Component Data Type</summary>
		public const uint PixelComponentDataType = 0x0018604e;

		/// <summary>(0018,6050) VR=UL VM=1 Number of Table Break Points</summary>
		public const uint NumberOfTableBreakPoints = 0x00186050;

		/// <summary>(0018,6052) VR=UL VM=1-n Table of X Break Points</summary>
		public const uint TableOfXBreakPoints = 0x00186052;

		/// <summary>(0018,6054) VR=FD VM=1-n Table of Y Break Points</summary>
		public const uint TableOfYBreakPoints = 0x00186054;

		/// <summary>(0018,6056) VR=UL VM=1 Number of Table Entries</summary>
		public const uint NumberOfTableEntries = 0x00186056;

		/// <summary>(0018,6058) VR=UL VM=1-n Table of Pixel Values</summary>
		public const uint TableOfPixelValues = 0x00186058;

		/// <summary>(0018,605a) VR=FL VM=1-n Table of Parameter Values</summary>
		public const uint TableOfParameterValues = 0x0018605a;

		/// <summary>(0018,6060) VR=FL VM=1-n R Wave Time Vector</summary>
		public const uint RWaveTimeVector = 0x00186060;

		/// <summary>(0018,7000) VR=CS VM=1 Detector Conditions Nominal Flag</summary>
		public const uint DetectorConditionsNominalFlag = 0x00187000;

		/// <summary>(0018,7001) VR=DS VM=1 Detector Temperature</summary>
		public const uint DetectorTemperature = 0x00187001;

		/// <summary>(0018,7004) VR=CS VM=1 Detector Type</summary>
		public const uint DetectorType = 0x00187004;

		/// <summary>(0018,7005) VR=CS VM=1 Detector Configuration</summary>
		public const uint DetectorConfiguration = 0x00187005;

		/// <summary>(0018,7006) VR=LT VM=1 Detector Description</summary>
		public const uint DetectorDescription = 0x00187006;

		/// <summary>(0018,7008) VR=LT VM=1 Detector Mode</summary>
		public const uint DetectorMode = 0x00187008;

		/// <summary>(0018,700a) VR=SH VM=1 Detector ID</summary>
		public const uint DetectorID = 0x0018700a;

		/// <summary>(0018,700c) VR=DA VM=1 Date of Last Detector Calibration</summary>
		public const uint DateOfLastDetectorCalibration = 0x0018700c;

		/// <summary>(0018,700e) VR=TM VM=1 Time of Last Detector Calibration</summary>
		public const uint TimeOfLastDetectorCalibration = 0x0018700e;

		/// <summary>(0018,7010) VR=IS VM=1 Exposures on Detector Since Last Calibration</summary>
		public const uint ExposuresOnDetectorSinceLastCalibration = 0x00187010;

		/// <summary>(0018,7011) VR=IS VM=1 Exposures on Detector Since Manufactured</summary>
		public const uint ExposuresOnDetectorSinceManufactured = 0x00187011;

		/// <summary>(0018,7012) VR=DS VM=1 Detector Time Since Last Exposure</summary>
		public const uint DetectorTimeSinceLastExposure = 0x00187012;

		/// <summary>(0018,7014) VR=DS VM=1 Detector Active Time</summary>
		public const uint DetectorActiveTime = 0x00187014;

		/// <summary>(0018,7016) VR=DS VM=1 Detector Activation Offset From Exposure</summary>
		public const uint DetectorActivationOffsetFromExposure = 0x00187016;

		/// <summary>(0018,701a) VR=DS VM=2 Detector Binning</summary>
		public const uint DetectorBinning = 0x0018701a;

		/// <summary>(0018,7020) VR=DS VM=2 Detector Element Physical Size</summary>
		public const uint DetectorElementPhysicalSize = 0x00187020;

		/// <summary>(0018,7022) VR=DS VM=2 Detector Element Spacing</summary>
		public const uint DetectorElementSpacing = 0x00187022;

		/// <summary>(0018,7024) VR=CS VM=1 Detector Active Shape</summary>
		public const uint DetectorActiveShape = 0x00187024;

		/// <summary>(0018,7026) VR=DS VM=1-2 Detector Active Dimension(s)</summary>
		public const uint DetectorActiveDimensions = 0x00187026;

		/// <summary>(0018,7028) VR=DS VM=2 Detector Active Origin</summary>
		public const uint DetectorActiveOrigin = 0x00187028;

		/// <summary>(0018,702a) VR=LO VM=1 Detector Manufacturer Name</summary>
		public const uint DetectorManufacturerName = 0x0018702a;

		/// <summary>(0018,702b) VR=LO VM=1 Detector Manufacturer's Model Name</summary>
		public const uint DetectorManufacturersModelName = 0x0018702b;

		/// <summary>(0018,7030) VR=DS VM=2 Field of View Origin</summary>
		public const uint FieldOfViewOrigin = 0x00187030;

		/// <summary>(0018,7032) VR=DS VM=1 Field of View Rotation</summary>
		public const uint FieldOfViewRotation = 0x00187032;

		/// <summary>(0018,7034) VR=CS VM=1 Field of View Horizontal Flip</summary>
		public const uint FieldOfViewHorizontalFlip = 0x00187034;

		/// <summary>(0018,7040) VR=LT VM=1 Grid Absorbing Material</summary>
		public const uint GridAbsorbingMaterial = 0x00187040;

		/// <summary>(0018,7041) VR=LT VM=1 Grid Spacing Material</summary>
		public const uint GridSpacingMaterial = 0x00187041;

		/// <summary>(0018,7042) VR=DS VM=1 Grid Thickness</summary>
		public const uint GridThickness = 0x00187042;

		/// <summary>(0018,7044) VR=DS VM=1 Grid Pitch</summary>
		public const uint GridPitch = 0x00187044;

		/// <summary>(0018,7046) VR=IS VM=2 Grid Aspect Ratio</summary>
		public const uint GridAspectRatio = 0x00187046;

		/// <summary>(0018,7048) VR=DS VM=1 Grid Period</summary>
		public const uint GridPeriod = 0x00187048;

		/// <summary>(0018,704c) VR=DS VM=1 Grid Focal Distance</summary>
		public const uint GridFocalDistance = 0x0018704c;

		/// <summary>(0018,7050) VR=CS VM=1-n Filter Material</summary>
		public const uint FilterMaterial = 0x00187050;

		/// <summary>(0018,7052) VR=DS VM=1-n Filter Thickness Minimum</summary>
		public const uint FilterThicknessMinimum = 0x00187052;

		/// <summary>(0018,7054) VR=DS VM=1-n Filter Thickness Maximum</summary>
		public const uint FilterThicknessMaximum = 0x00187054;

		/// <summary>(0018,7060) VR=CS VM=1 Exposure Control Mode</summary>
		public const uint ExposureControlMode = 0x00187060;

		/// <summary>(0018,7062) VR=LT VM=1 Exposure Control Mode Description</summary>
		public const uint ExposureControlModeDescription = 0x00187062;

		/// <summary>(0018,7064) VR=CS VM=1 Exposure Status</summary>
		public const uint ExposureStatus = 0x00187064;

		/// <summary>(0018,7065) VR=DS VM=1 Phototimer Setting</summary>
		public const uint PhototimerSetting = 0x00187065;

		/// <summary>(0018,8150) VR=DS VM=1 Exposure Time in uS</summary>
		public const uint ExposureTimeInMicroS = 0x00188150;

		/// <summary>(0018,8151) VR=DS VM=1 X-Ray Tube Current in uA</summary>
		public const uint XRayTubeCurrentInMicroA = 0x00188151;

		/// <summary>(0018,9004) VR=CS VM=1 Content Qualification</summary>
		public const uint ContentQualification = 0x00189004;

		/// <summary>(0018,9005) VR=SH VM=1 Pulse Sequence Name</summary>
		public const uint PulseSequenceName = 0x00189005;

		/// <summary>(0018,9006) VR=SQ VM=1 MR Imaging Modifier Sequence</summary>
		public const uint MRImagingModifierSequence = 0x00189006;

		/// <summary>(0018,9008) VR=CS VM=1 Echo Pulse Sequence</summary>
		public const uint EchoPulseSequence = 0x00189008;

		/// <summary>(0018,9009) VR=CS VM=1 Inversion Recovery</summary>
		public const uint InversionRecovery = 0x00189009;

		/// <summary>(0018,9010) VR=CS VM=1 Flow Compensation</summary>
		public const uint FlowCompensation = 0x00189010;

		/// <summary>(0018,9011) VR=CS VM=1 Multiple Spin Echo</summary>
		public const uint MultipleSpinEcho = 0x00189011;

		/// <summary>(0018,9012) VR=CS VM=1 Multi-planar Excitation</summary>
		public const uint MultiplanarExcitation = 0x00189012;

		/// <summary>(0018,9014) VR=CS VM=1 Phase Contrast</summary>
		public const uint PhaseContrast = 0x00189014;

		/// <summary>(0018,9015) VR=CS VM=1 Time of Flight Contrast</summary>
		public const uint TimeOfFlightContrast = 0x00189015;

		/// <summary>(0018,9016) VR=CS VM=1 Spoiling</summary>
		public const uint Spoiling = 0x00189016;

		/// <summary>(0018,9017) VR=CS VM=1 Steady State Pulse Sequence</summary>
		public const uint SteadyStatePulseSequence = 0x00189017;

		/// <summary>(0018,9018) VR=CS VM=1 Echo Planar Pulse Sequence</summary>
		public const uint EchoPlanarPulseSequence = 0x00189018;

		/// <summary>(0018,9019) VR=FD VM=1 Tag Angle First Axis</summary>
		public const uint TagAngleFirstAxis = 0x00189019;

		/// <summary>(0018,9020) VR=CS VM=1 Magnetization Transfer</summary>
		public const uint MagnetizationTransfer = 0x00189020;

		/// <summary>(0018,9021) VR=CS VM=1 T2 Preparation</summary>
		public const uint T2Preparation = 0x00189021;

		/// <summary>(0018,9022) VR=CS VM=1 Blood Signal Nulling</summary>
		public const uint BloodSignalNulling = 0x00189022;

		/// <summary>(0018,9024) VR=CS VM=1 Saturation Recovery</summary>
		public const uint SaturationRecovery = 0x00189024;

		/// <summary>(0018,9025) VR=CS VM=1 Spectrally Selected Suppression</summary>
		public const uint SpectrallySelectedSuppression = 0x00189025;

		/// <summary>(0018,9026) VR=CS VM=1 Spectrally Selected Excitation</summary>
		public const uint SpectrallySelectedExcitation = 0x00189026;

		/// <summary>(0018,9027) VR=CS VM=1 Spatial Pre-saturation</summary>
		public const uint SpatialPresaturation = 0x00189027;

		/// <summary>(0018,9028) VR=CS VM=1 Tagging</summary>
		public const uint Tagging = 0x00189028;

		/// <summary>(0018,9029) VR=CS VM=1 Oversampling Phase</summary>
		public const uint OversamplingPhase = 0x00189029;

		/// <summary>(0018,9030) VR=FD VM=1 Tag Spacing First Dimension</summary>
		public const uint TagSpacingFirstDimension = 0x00189030;

		/// <summary>(0018,9032) VR=CS VM=1 Geometry of k-Space Traversal</summary>
		public const uint GeometryOfKSpaceTraversal = 0x00189032;

		/// <summary>(0018,9033) VR=CS VM=1 Segmented k-Space Traversal</summary>
		public const uint SegmentedKSpaceTraversal = 0x00189033;

		/// <summary>(0018,9034) VR=CS VM=1 Rectilinear Phase Encode Reordering</summary>
		public const uint RectilinearPhaseEncodeReordering = 0x00189034;

		/// <summary>(0018,9035) VR=FD VM=1 Tag Thickness</summary>
		public const uint TagThickness = 0x00189035;

		/// <summary>(0018,9036) VR=CS VM=1 Partial Fourier Direction</summary>
		public const uint PartialFourierDirection = 0x00189036;

		/// <summary>(0018,9037) VR=CS VM=1 Cardiac Synchronization Technique</summary>
		public const uint CardiacSynchronizationTechnique = 0x00189037;

		/// <summary>(0018,9041) VR=LO VM=1 Receive Coil Manufacturer Name</summary>
		public const uint ReceiveCoilManufacturerName = 0x00189041;

		/// <summary>(0018,9042) VR=SQ VM=1 MR Receive Coil Sequence</summary>
		public const uint MRReceiveCoilSequence = 0x00189042;

		/// <summary>(0018,9043) VR=CS VM=1 Receive Coil Type</summary>
		public const uint ReceiveCoilType = 0x00189043;

		/// <summary>(0018,9044) VR=CS VM=1 Quadrature Receive Coil</summary>
		public const uint QuadratureReceiveCoil = 0x00189044;

		/// <summary>(0018,9045) VR=SQ VM=1 Multi-Coil Definition Sequence</summary>
		public const uint MultiCoilDefinitionSequence = 0x00189045;

		/// <summary>(0018,9046) VR=LO VM=1 Multi-Coil Configuration</summary>
		public const uint MultiCoilConfiguration = 0x00189046;

		/// <summary>(0018,9047) VR=SH VM=1 Multi-Coil Element Name</summary>
		public const uint MultiCoilElementName = 0x00189047;

		/// <summary>(0018,9048) VR=CS VM=1 Multi-Coil Element Used</summary>
		public const uint MultiCoilElementUsed = 0x00189048;

		/// <summary>(0018,9049) VR=SQ VM=1 MR Transmit Coil Sequence</summary>
		public const uint MRTransmitCoilSequence = 0x00189049;

		/// <summary>(0018,9050) VR=LO VM=1 Transmit Coil Manufacturer Name</summary>
		public const uint TransmitCoilManufacturerName = 0x00189050;

		/// <summary>(0018,9051) VR=CS VM=1 Transmit Coil Type</summary>
		public const uint TransmitCoilType = 0x00189051;

		/// <summary>(0018,9052) VR=FD VM=1-2 Spectral Width</summary>
		public const uint SpectralWidth = 0x00189052;

		/// <summary>(0018,9053) VR=FD VM=1-2 Chemical Shift Reference</summary>
		public const uint ChemicalShiftReference = 0x00189053;

		/// <summary>(0018,9054) VR=CS VM=1 Volume Localization Technique</summary>
		public const uint VolumeLocalizationTechnique = 0x00189054;

		/// <summary>(0018,9058) VR=US VM=1 MR Acquisition Frequency Encoding Steps</summary>
		public const uint MRAcquisitionFrequencyEncodingSteps = 0x00189058;

		/// <summary>(0018,9059) VR=CS VM=1 De-coupling</summary>
		public const uint Decoupling = 0x00189059;

		/// <summary>(0018,9060) VR=CS VM=1-2 De-coupled Nucleus</summary>
		public const uint DecoupledNucleus = 0x00189060;

		/// <summary>(0018,9061) VR=FD VM=1-2 De-coupling Frequency</summary>
		public const uint DecouplingFrequency = 0x00189061;

		/// <summary>(0018,9062) VR=CS VM=1 De-coupling Method</summary>
		public const uint DecouplingMethod = 0x00189062;

		/// <summary>(0018,9063) VR=FD VM=1-2 De-coupling Chemical Shift Reference</summary>
		public const uint DecouplingChemicalShiftReference = 0x00189063;

		/// <summary>(0018,9064) VR=CS VM=1 k-space Filtering</summary>
		public const uint KspaceFiltering = 0x00189064;

		/// <summary>(0018,9065) VR=CS VM=1-2 Time Domain Filtering</summary>
		public const uint TimeDomainFiltering = 0x00189065;

		/// <summary>(0018,9066) VR=US VM=1-2 Number of Zero fills</summary>
		public const uint NumberOfZeroFills = 0x00189066;

		/// <summary>(0018,9067) VR=CS VM=1 Baseline Correction</summary>
		public const uint BaselineCorrection = 0x00189067;

		/// <summary>(0018,9069) VR=FD VM=1 Parallel Reduction Factor In-plane</summary>
		public const uint ParallelReductionFactorInplane = 0x00189069;

		/// <summary>(0018,9070) VR=FD VM=1 Cardiac R-R Interval Specified</summary>
		public const uint CardiacRRIntervalSpecified = 0x00189070;

		/// <summary>(0018,9073) VR=FD VM=1 Acquisition Duration</summary>
		public const uint AcquisitionDuration = 0x00189073;

		/// <summary>(0018,9074) VR=DT VM=1 Frame Acquisition DateTime</summary>
		public const uint FrameAcquisitionDateTime = 0x00189074;

		/// <summary>(0018,9075) VR=CS VM=1 Diffusion Directionality</summary>
		public const uint DiffusionDirectionality = 0x00189075;

		/// <summary>(0018,9076) VR=SQ VM=1 Diffusion Gradient Direction Sequence</summary>
		public const uint DiffusionGradientDirectionSequence = 0x00189076;

		/// <summary>(0018,9077) VR=CS VM=1 Parallel Acquisition</summary>
		public const uint ParallelAcquisition = 0x00189077;

		/// <summary>(0018,9078) VR=CS VM=1 Parallel Acquisition Technique</summary>
		public const uint ParallelAcquisitionTechnique = 0x00189078;

		/// <summary>(0018,9079) VR=FD VM=1-n Inversion Times</summary>
		public const uint InversionTimes = 0x00189079;

		/// <summary>(0018,9080) VR=ST VM=1 Metabolite Map Description</summary>
		public const uint MetaboliteMapDescription = 0x00189080;

		/// <summary>(0018,9081) VR=CS VM=1 Partial Fourier</summary>
		public const uint PartialFourier = 0x00189081;

		/// <summary>(0018,9082) VR=FD VM=1 Effective Echo Time</summary>
		public const uint EffectiveEchoTime = 0x00189082;

		/// <summary>(0018,9083) VR=SQ VM=1 Metabolite Map Code Sequence</summary>
		public const uint MetaboliteMapCodeSequence = 0x00189083;

		/// <summary>(0018,9084) VR=SQ VM=1 Chemical Shift Sequence</summary>
		public const uint ChemicalShiftSequence = 0x00189084;

		/// <summary>(0018,9085) VR=CS VM=1 Cardiac Signal Source</summary>
		public const uint CardiacSignalSource = 0x00189085;

		/// <summary>(0018,9087) VR=FD VM=1 Diffusion b-value</summary>
		public const uint DiffusionBvalue = 0x00189087;

		/// <summary>(0018,9089) VR=FD VM=3 Diffusion Gradient Orientation</summary>
		public const uint DiffusionGradientOrientation = 0x00189089;

		/// <summary>(0018,9090) VR=FD VM=3 Velocity Encoding Direction</summary>
		public const uint VelocityEncodingDirection = 0x00189090;

		/// <summary>(0018,9091) VR=FD VM=1 Velocity Encoding Minimum Value</summary>
		public const uint VelocityEncodingMinimumValue = 0x00189091;

		/// <summary>(0018,9093) VR=US VM=1 Number of k-Space Trajectories</summary>
		public const uint NumberOfKSpaceTrajectories = 0x00189093;

		/// <summary>(0018,9094) VR=CS VM=1 Coverage of k-Space</summary>
		public const uint CoverageOfKSpace = 0x00189094;

		/// <summary>(0018,9095) VR=UL VM=1 Spectroscopy Acquisition Phase Rows</summary>
		public const uint SpectroscopyAcquisitionPhaseRows = 0x00189095;

		/// <summary>(0018,9098) VR=FD VM=1-2 Transmitter Frequency</summary>
		public const uint TransmitterFrequency = 0x00189098;

		/// <summary>(0018,9100) VR=CS VM=1-2 Resonant Nucleus</summary>
		public const uint ResonantNucleus = 0x00189100;

		/// <summary>(0018,9101) VR=CS VM=1 Frequency Correction</summary>
		public const uint FrequencyCorrection = 0x00189101;

		/// <summary>(0018,9103) VR=SQ VM=1 MR Spectroscopy FOV/Geometry Sequence</summary>
		public const uint MRSpectroscopyFOVGeometrySequence = 0x00189103;

		/// <summary>(0018,9104) VR=FD VM=1 Slab Thickness</summary>
		public const uint SlabThickness = 0x00189104;

		/// <summary>(0018,9105) VR=FD VM=3 Slab Orientation</summary>
		public const uint SlabOrientation = 0x00189105;

		/// <summary>(0018,9106) VR=FD VM=3 Mid Slab Position</summary>
		public const uint MidSlabPosition = 0x00189106;

		/// <summary>(0018,9107) VR=SQ VM=1 MR Spatial Saturation Sequence</summary>
		public const uint MRSpatialSaturationSequence = 0x00189107;

		/// <summary>(0018,9112) VR=SQ VM=1 MR Timing and Related Parameters Sequence</summary>
		public const uint MRTimingAndRelatedParametersSequence = 0x00189112;

		/// <summary>(0018,9114) VR=SQ VM=1 MR Echo Sequence</summary>
		public const uint MREchoSequence = 0x00189114;

		/// <summary>(0018,9115) VR=SQ VM=1 MR Modifier Sequence</summary>
		public const uint MRModifierSequence = 0x00189115;

		/// <summary>(0018,9117) VR=SQ VM=1 MR Diffusion Sequence</summary>
		public const uint MRDiffusionSequence = 0x00189117;

		/// <summary>(0018,9118) VR=SQ VM=1 Cardiac Synchronization Sequence</summary>
		public const uint CardiacSynchronizationSequence = 0x00189118;

		/// <summary>(0018,9119) VR=SQ VM=1 MR Averages Sequence</summary>
		public const uint MRAveragesSequence = 0x00189119;

		/// <summary>(0018,9125) VR=SQ VM=1 MR FOV/Geometry Sequence</summary>
		public const uint MRFOVGeometrySequence = 0x00189125;

		/// <summary>(0018,9126) VR=SQ VM=1 Volume Localization Sequence</summary>
		public const uint VolumeLocalizationSequence = 0x00189126;

		/// <summary>(0018,9127) VR=UL VM=1 Spectroscopy Acquisition Data Columns</summary>
		public const uint SpectroscopyAcquisitionDataColumns = 0x00189127;

		/// <summary>(0018,9147) VR=CS VM=1 Diffusion Anisotropy Type</summary>
		public const uint DiffusionAnisotropyType = 0x00189147;

		/// <summary>(0018,9151) VR=DT VM=1 Frame Reference DateTime</summary>
		public const uint FrameReferenceDateTime = 0x00189151;

		/// <summary>(0018,9152) VR=SQ VM=1 MR Metabolite Map Sequence</summary>
		public const uint MRMetaboliteMapSequence = 0x00189152;

		/// <summary>(0018,9155) VR=FD VM=1 Parallel Reduction Factor out-of-plane</summary>
		public const uint ParallelReductionFactorOutofplane = 0x00189155;

		/// <summary>(0018,9159) VR=UL VM=1 Spectroscopy Acquisition Out-of-plane Phase Steps</summary>
		public const uint SpectroscopyAcquisitionOutofplanePhaseSteps = 0x00189159;

		/// <summary>(0018,9168) VR=FD VM=1 Parallel Reduction Factor Second In-plane</summary>
		public const uint ParallelReductionFactorSecondInplane = 0x00189168;

		/// <summary>(0018,9169) VR=CS VM=1 Cardiac Beat Rejection Technique</summary>
		public const uint CardiacBeatRejectionTechnique = 0x00189169;

		/// <summary>(0018,9170) VR=CS VM=1 Respiratory Motion Compensation Technique</summary>
		public const uint RespiratoryMotionCompensationTechnique = 0x00189170;

		/// <summary>(0018,9171) VR=CS VM=1 Respiratory Signal Source</summary>
		public const uint RespiratorySignalSource = 0x00189171;

		/// <summary>(0018,9172) VR=CS VM=1 Bulk Motion Compensation Technique</summary>
		public const uint BulkMotionCompensationTechnique = 0x00189172;

		/// <summary>(0018,9173) VR=CS VM=1 Bulk Motion Signal Source</summary>
		public const uint BulkMotionSignalSource = 0x00189173;

		/// <summary>(0018,9174) VR=CS VM=1 Applicable Safety Standard Agency</summary>
		public const uint ApplicableSafetyStandardAgency = 0x00189174;

		/// <summary>(0018,9175) VR=LO VM=1 Applicable Safety Standard Description</summary>
		public const uint ApplicableSafetyStandardDescription = 0x00189175;

		/// <summary>(0018,9176) VR=SQ VM=1 Operating Mode Sequence</summary>
		public const uint OperatingModeSequence = 0x00189176;

		/// <summary>(0018,9177) VR=CS VM=1 Operating Mode Type</summary>
		public const uint OperatingModeType = 0x00189177;

		/// <summary>(0018,9178) VR=CS VM=1 Operating Mode</summary>
		public const uint OperatingMode = 0x00189178;

		/// <summary>(0018,9179) VR=CS VM=1 Specific Absorption Rate Definition</summary>
		public const uint SpecificAbsorptionRateDefinition = 0x00189179;

		/// <summary>(0018,9180) VR=CS VM=1 Gradient Output Type</summary>
		public const uint GradientOutputType = 0x00189180;

		/// <summary>(0018,9181) VR=FD VM=1 Specific Absorption Rate Value</summary>
		public const uint SpecificAbsorptionRateValue = 0x00189181;

		/// <summary>(0018,9182) VR=FD VM=1 Gradient Output</summary>
		public const uint GradientOutput = 0x00189182;

		/// <summary>(0018,9183) VR=CS VM=1 Flow Compensation Direction</summary>
		public const uint FlowCompensationDirection = 0x00189183;

		/// <summary>(0018,9184) VR=FD VM=1 Tagging Delay</summary>
		public const uint TaggingDelay = 0x00189184;

		/// <summary>(0018,9185) VR=ST VM=1 Respiratory Motion Compensation Technique Description</summary>
		public const uint RespiratoryMotionCompensationTechniqueDescription = 0x00189185;

		/// <summary>(0018,9186) VR=SH VM=1 Respiratory Signal Source ID</summary>
		public const uint RespiratorySignalSourceID = 0x00189186;

		/// <summary>(0018,9197) VR=SQ VM=1 MR Velocity Encoding Sequence</summary>
		public const uint MRVelocityEncodingSequence = 0x00189197;

		/// <summary>(0018,9198) VR=CS VM=1 First Order Phase Correction</summary>
		public const uint FirstOrderPhaseCorrection = 0x00189198;

		/// <summary>(0018,9199) VR=CS VM=1 Water Referenced Phase Correction</summary>
		public const uint WaterReferencedPhaseCorrection = 0x00189199;

		/// <summary>(0018,9200) VR=CS VM=1 MR Spectroscopy Acquisition Type</summary>
		public const uint MRSpectroscopyAcquisitionType = 0x00189200;

		/// <summary>(0018,9214) VR=CS VM=1 Respiratory Cycle Position</summary>
		public const uint RespiratoryCyclePosition = 0x00189214;

		/// <summary>(0018,9217) VR=FD VM=1 Velocity Encoding Maximum Value</summary>
		public const uint VelocityEncodingMaximumValue = 0x00189217;

		/// <summary>(0018,9218) VR=FD VM=1 Tag Spacing Second Dimension</summary>
		public const uint TagSpacingSecondDimension = 0x00189218;

		/// <summary>(0018,9219) VR=SS VM=1 Tag Angle Second Axis</summary>
		public const uint TagAngleSecondAxis = 0x00189219;

		/// <summary>(0018,9220) VR=FD VM=1 Frame Acquisition Duration</summary>
		public const uint FrameAcquisitionDuration = 0x00189220;

		/// <summary>(0018,9226) VR=SQ VM=1 MR Image Frame Type Sequence</summary>
		public const uint MRImageFrameTypeSequence = 0x00189226;

		/// <summary>(0018,9227) VR=SQ VM=1 MR Spectroscopy Frame Type Sequence</summary>
		public const uint MRSpectroscopyFrameTypeSequence = 0x00189227;

		/// <summary>(0018,9231) VR=US VM=1 MR Acquisition Phase Encoding Steps in-plane</summary>
		public const uint MRAcquisitionPhaseEncodingStepsInplane = 0x00189231;

		/// <summary>(0018,9232) VR=US VM=1 MR Acquisition Phase Encoding Steps out-of-plane</summary>
		public const uint MRAcquisitionPhaseEncodingStepsOutofplane = 0x00189232;

		/// <summary>(0018,9234) VR=UL VM=1 Spectroscopy Acquisition Phase Columns</summary>
		public const uint SpectroscopyAcquisitionPhaseColumns = 0x00189234;

		/// <summary>(0018,9236) VR=CS VM=1 Cardiac Cycle Position</summary>
		public const uint CardiacCyclePosition = 0x00189236;

		/// <summary>(0018,9239) VR=SQ VM=1 Specific Absorption Rate Sequence</summary>
		public const uint SpecificAbsorptionRateSequence = 0x00189239;

		/// <summary>(0018,9240) VR=US VM=1 RF Echo Train Length</summary>
		public const uint RFEchoTrainLength = 0x00189240;

		/// <summary>(0018,9241) VR=US VM=1 Gradient Echo Train Length</summary>
		public const uint GradientEchoTrainLength = 0x00189241;

		/// <summary>(0018,9295) VR=FD VM=1 Chemical Shifts Minimum Integration Limit in ppm</summary>
		public const uint ChemicalShiftsMinimumIntegrationLimitInPpm = 0x00189295;

		/// <summary>(0018,9296) VR=FD VM=1 Chemical Shifts Maximum Integration Limit in ppm</summary>
		public const uint ChemicalShiftsMaximumIntegrationLimitInPpm = 0x00189296;

		/// <summary>(0018,9301) VR=SQ VM=1 CT Acquisition Type Sequence</summary>
		public const uint CTAcquisitionTypeSequence = 0x00189301;

		/// <summary>(0018,9302) VR=CS VM=1 Acquisition Type</summary>
		public const uint AcquisitionType = 0x00189302;

		/// <summary>(0018,9303) VR=FD VM=1 Tube Angle</summary>
		public const uint TubeAngle = 0x00189303;

		/// <summary>(0018,9304) VR=SQ VM=1 CT Acquisition Details Sequence</summary>
		public const uint CTAcquisitionDetailsSequence = 0x00189304;

		/// <summary>(0018,9305) VR=FD VM=1 Revolution Time</summary>
		public const uint RevolutionTime = 0x00189305;

		/// <summary>(0018,9306) VR=FD VM=1 Single Collimation Width</summary>
		public const uint SingleCollimationWidth = 0x00189306;

		/// <summary>(0018,9307) VR=FD VM=1 Total Collimation Width</summary>
		public const uint TotalCollimationWidth = 0x00189307;

		/// <summary>(0018,9308) VR=SQ VM=1 CT Table Dynamics Sequence</summary>
		public const uint CTTableDynamicsSequence = 0x00189308;

		/// <summary>(0018,9309) VR=FD VM=1 Table Speed</summary>
		public const uint TableSpeed = 0x00189309;

		/// <summary>(0018,9310) VR=FD VM=1 Table Feed per Rotation</summary>
		public const uint TableFeedPerRotation = 0x00189310;

		/// <summary>(0018,9311) VR=FD VM=1 Spiral Pitch Factor</summary>
		public const uint SpiralPitchFactor = 0x00189311;

		/// <summary>(0018,9312) VR=SQ VM=1 CT Geometry Sequence</summary>
		public const uint CTGeometrySequence = 0x00189312;

		/// <summary>(0018,9313) VR=FD VM=3 Data Collection Center (Patient)</summary>
		public const uint DataCollectionCenterPatient = 0x00189313;

		/// <summary>(0018,9314) VR=SQ VM=1 CT Reconstruction Sequence</summary>
		public const uint CTReconstructionSequence = 0x00189314;

		/// <summary>(0018,9315) VR=CS VM=1 Reconstruction Algorithm</summary>
		public const uint ReconstructionAlgorithm = 0x00189315;

		/// <summary>(0018,9316) VR=CS VM=1 Convolution Kernel Group</summary>
		public const uint ConvolutionKernelGroup = 0x00189316;

		/// <summary>(0018,9317) VR=FD VM=2 Reconstruction Field of View</summary>
		public const uint ReconstructionFieldOfView = 0x00189317;

		/// <summary>(0018,9318) VR=FD VM=3 Reconstruction Target Center (Patient)</summary>
		public const uint ReconstructionTargetCenterPatient = 0x00189318;

		/// <summary>(0018,9319) VR=FD VM=1 Reconstruction Angle</summary>
		public const uint ReconstructionAngle = 0x00189319;

		/// <summary>(0018,9320) VR=SH VM=1 Image Filter</summary>
		public const uint ImageFilter = 0x00189320;

		/// <summary>(0018,9321) VR=SQ VM=1 CT Exposure Sequence</summary>
		public const uint CTExposureSequence = 0x00189321;

		/// <summary>(0018,9322) VR=FD VM=2 Reconstruction Pixel Spacing</summary>
		public const uint ReconstructionPixelSpacing = 0x00189322;

		/// <summary>(0018,9323) VR=CS VM=1 Exposure Modulation Type</summary>
		public const uint ExposureModulationType = 0x00189323;

		/// <summary>(0018,9324) VR=FD VM=1 Estimated Dose Saving</summary>
		public const uint EstimatedDoseSaving = 0x00189324;

		/// <summary>(0018,9325) VR=SQ VM=1 CT X-Ray Details Sequence</summary>
		public const uint CTXRayDetailsSequence = 0x00189325;

		/// <summary>(0018,9326) VR=SQ VM=1 CT Position Sequence</summary>
		public const uint CTPositionSequence = 0x00189326;

		/// <summary>(0018,9327) VR=FD VM=1 Table Position</summary>
		public const uint TablePosition = 0x00189327;

		/// <summary>(0018,9328) VR=FD VM=1 Exposure Time in ms</summary>
		public const uint ExposureTimeInMs = 0x00189328;

		/// <summary>(0018,9329) VR=SQ VM=1 CT Image Frame Type Sequence</summary>
		public const uint CTImageFrameTypeSequence = 0x00189329;

		/// <summary>(0018,9330) VR=FD VM=1 X-Ray Tube Current in mA</summary>
		public const uint XRayTubeCurrentInMA = 0x00189330;

		/// <summary>(0018,9332) VR=FD VM=1 Exposure in mAs</summary>
		public const uint ExposureInMAs = 0x00189332;

		/// <summary>(0018,9333) VR=CS VM=1 Constant Volume Flag</summary>
		public const uint ConstantVolumeFlag = 0x00189333;

		/// <summary>(0018,9334) VR=CS VM=1 Fluoroscopy Flag</summary>
		public const uint FluoroscopyFlag = 0x00189334;

		/// <summary>(0018,9335) VR=FD VM=1 Distance Source to Data Collection Center</summary>
		public const uint DistanceSourceToDataCollectionCenter = 0x00189335;

		/// <summary>(0018,9337) VR=US VM=1 Contrast/Bolus Agent Number</summary>
		public const uint ContrastBolusAgentNumber = 0x00189337;

		/// <summary>(0018,9338) VR=SQ VM=1 Contrast/Bolus Ingredient Code Sequence</summary>
		public const uint ContrastBolusIngredientCodeSequence = 0x00189338;

		/// <summary>(0018,9340) VR=SQ VM=1 Contrast Administration Profile Sequence</summary>
		public const uint ContrastAdministrationProfileSequence = 0x00189340;

		/// <summary>(0018,9341) VR=SQ VM=1 Contrast/Bolus Usage Sequence</summary>
		public const uint ContrastBolusUsageSequence = 0x00189341;

		/// <summary>(0018,9342) VR=CS VM=1 Contrast/Bolus Agent Administered</summary>
		public const uint ContrastBolusAgentAdministered = 0x00189342;

		/// <summary>(0018,9343) VR=CS VM=1 Contrast/Bolus Agent Detected</summary>
		public const uint ContrastBolusAgentDetected = 0x00189343;

		/// <summary>(0018,9344) VR=CS VM=1 Contrast/Bolus Agent Phase</summary>
		public const uint ContrastBolusAgentPhase = 0x00189344;

		/// <summary>(0018,9345) VR=FD VM=1 CTDIvol</summary>
		public const uint CTDIvol = 0x00189345;

		/// <summary>(0018,9346) VR=SQ VM=1 CTDI Phantom Type Code Sequence</summary>
		public const uint CTDIPhantomTypeCodeSequence = 0x00189346;

		/// <summary>(0018,9351) VR=FL VM=1 Calcium Scoring Mass Factor Patient</summary>
		public const uint CalciumScoringMassFactorPatient = 0x00189351;

		/// <summary>(0018,9352) VR=FL VM=3 Calcium Scoring Mass Factor Device</summary>
		public const uint CalciumScoringMassFactorDevice = 0x00189352;

		/// <summary>(0018,9360) VR=SQ VM=1 CT Additional X-Ray Source Sequence</summary>
		public const uint CTAdditionalXRaySourceSequence = 0x00189360;

		/// <summary>(0018,9401) VR=SQ VM=1 Projection Pixel Calibration Sequence</summary>
		public const uint ProjectionPixelCalibrationSequence = 0x00189401;

		/// <summary>(0018,9402) VR=FL VM=1 Distance Source to Isocenter</summary>
		public const uint DistanceSourceToIsocenter = 0x00189402;

		/// <summary>(0018,9403) VR=FL VM=1 Distance Object to Table Top</summary>
		public const uint DistanceObjectToTableTop = 0x00189403;

		/// <summary>(0018,9404) VR=FL VM=2 Object Pixel Spacing in Center of Beam</summary>
		public const uint ObjectPixelSpacingInCenterOfBeam = 0x00189404;

		/// <summary>(0018,9405) VR=SQ VM=1 Positioner Position Sequence</summary>
		public const uint PositionerPositionSequence = 0x00189405;

		/// <summary>(0018,9406) VR=SQ VM=1 Table Position Sequence</summary>
		public const uint TablePositionSequence = 0x00189406;

		/// <summary>(0018,9407) VR=SQ VM=1 Collimator Shape Sequence</summary>
		public const uint CollimatorShapeSequence = 0x00189407;

		/// <summary>(0018,9412) VR=SQ VM=1 XA/XRF Frame Characteristics Sequence</summary>
		public const uint XAXRFFrameCharacteristicsSequence = 0x00189412;

		/// <summary>(0018,9417) VR=SQ VM=1 Frame Acquisition Sequence</summary>
		public const uint FrameAcquisitionSequence = 0x00189417;

		/// <summary>(0018,9420) VR=CS VM=1 X-Ray Receptor Type</summary>
		public const uint XRayReceptorType = 0x00189420;

		/// <summary>(0018,9423) VR=LO VM=1 Acquisition Protocol Name</summary>
		public const uint AcquisitionProtocolName = 0x00189423;

		/// <summary>(0018,9424) VR=LT VM=1 Acquisition Protocol Description</summary>
		public const uint AcquisitionProtocolDescription = 0x00189424;

		/// <summary>(0018,9425) VR=CS VM=1 Contrast/Bolus Ingredient Opaque</summary>
		public const uint ContrastBolusIngredientOpaque = 0x00189425;

		/// <summary>(0018,9426) VR=FL VM=1 Distance Receptor Plane to Detector Housing</summary>
		public const uint DistanceReceptorPlaneToDetectorHousing = 0x00189426;

		/// <summary>(0018,9427) VR=CS VM=1 Intensifier Active Shape</summary>
		public const uint IntensifierActiveShape = 0x00189427;

		/// <summary>(0018,9428) VR=FL VM=1-2 Intensifier Active Dimension(s)</summary>
		public const uint IntensifierActiveDimensions = 0x00189428;

		/// <summary>(0018,9429) VR=FL VM=2 Physical Detector Size</summary>
		public const uint PhysicalDetectorSize = 0x00189429;

		/// <summary>(0018,9430) VR=US VM=2 Position of Isocenter Projection</summary>
		public const uint PositionOfIsocenterProjection = 0x00189430;

		/// <summary>(0018,9432) VR=SQ VM=1 Field of View Sequence</summary>
		public const uint FieldOfViewSequence = 0x00189432;

		/// <summary>(0018,9433) VR=LO VM=1 Field of View Description</summary>
		public const uint FieldOfViewDescription = 0x00189433;

		/// <summary>(0018,9434) VR=SQ VM=1 Exposure Control Sensing Regions Sequence</summary>
		public const uint ExposureControlSensingRegionsSequence = 0x00189434;

		/// <summary>(0018,9435) VR=CS VM=1 Exposure Control Sensing Region Shape</summary>
		public const uint ExposureControlSensingRegionShape = 0x00189435;

		/// <summary>(0018,9436) VR=SS VM=1 Exposure Control Sensing Region Left Vertical Edge</summary>
		public const uint ExposureControlSensingRegionLeftVerticalEdge = 0x00189436;

		/// <summary>(0018,9437) VR=SS VM=1 Exposure Control Sensing Region Right Vertical Edge</summary>
		public const uint ExposureControlSensingRegionRightVerticalEdge = 0x00189437;

		/// <summary>(0018,9438) VR=SS VM=1 Exposure Control Sensing Region Upper Horizontal Edge</summary>
		public const uint ExposureControlSensingRegionUpperHorizontalEdge = 0x00189438;

		/// <summary>(0018,9439) VR=SS VM=1 Exposure Control Sensing Region Lower Horizontal Edge</summary>
		public const uint ExposureControlSensingRegionLowerHorizontalEdge = 0x00189439;

		/// <summary>(0018,9440) VR=SS VM=2 Center of Circular Exposure Control Sensing Region</summary>
		public const uint CenterOfCircularExposureControlSensingRegion = 0x00189440;

		/// <summary>(0018,9441) VR=US VM=1 Radius of Circular Exposure Control Sensing Region</summary>
		public const uint RadiusOfCircularExposureControlSensingRegion = 0x00189441;

		/// <summary>(0018,9442) VR=SS VM=2-n Vertices of the Polygonal Exposure Control Sensing Region</summary>
		public const uint VerticesOfThePolygonalExposureControlSensingRegion = 0x00189442;

		/// <summary>(0018,9447) VR=FL VM=1 Column Angulation (Patient)</summary>
		public const uint ColumnAngulationPatient = 0x00189447;

		/// <summary>(0018,9449) VR=FL VM=1 Beam Angle</summary>
		public const uint BeamAngle = 0x00189449;

		/// <summary>(0018,9451) VR=SQ VM=1 Frame Detector Parameters Sequence</summary>
		public const uint FrameDetectorParametersSequence = 0x00189451;

		/// <summary>(0018,9452) VR=FL VM=1 Calculated Anatomy Thickness</summary>
		public const uint CalculatedAnatomyThickness = 0x00189452;

		/// <summary>(0018,9455) VR=SQ VM=1 Calibration Sequence</summary>
		public const uint CalibrationSequence = 0x00189455;

		/// <summary>(0018,9456) VR=SQ VM=1 Object Thickness Sequence</summary>
		public const uint ObjectThicknessSequence = 0x00189456;

		/// <summary>(0018,9457) VR=CS VM=1 Plane Identification</summary>
		public const uint PlaneIdentification = 0x00189457;

		/// <summary>(0018,9461) VR=FL VM=1-2 Field of View Dimension(s) in Float</summary>
		public const uint FieldOfViewDimensionsInFloat = 0x00189461;

		/// <summary>(0018,9462) VR=SQ VM=1 Isocenter Reference System Sequence</summary>
		public const uint IsocenterReferenceSystemSequence = 0x00189462;

		/// <summary>(0018,9463) VR=FL VM=1 Positioner Isocenter Primary Angle</summary>
		public const uint PositionerIsocenterPrimaryAngle = 0x00189463;

		/// <summary>(0018,9464) VR=FL VM=1 Positioner Isocenter Secondary Angle</summary>
		public const uint PositionerIsocenterSecondaryAngle = 0x00189464;

		/// <summary>(0018,9465) VR=FL VM=1 Positioner Isocenter Detector Rotation Angle</summary>
		public const uint PositionerIsocenterDetectorRotationAngle = 0x00189465;

		/// <summary>(0018,9466) VR=FL VM=1 Table X Position to Isocenter</summary>
		public const uint TableXPositionToIsocenter = 0x00189466;

		/// <summary>(0018,9467) VR=FL VM=1 Table Y Position to Isocenter</summary>
		public const uint TableYPositionToIsocenter = 0x00189467;

		/// <summary>(0018,9468) VR=FL VM=1 Table Z Position to Isocenter</summary>
		public const uint TableZPositionToIsocenter = 0x00189468;

		/// <summary>(0018,9469) VR=FL VM=1 Table Horizontal Rotation Angle</summary>
		public const uint TableHorizontalRotationAngle = 0x00189469;

		/// <summary>(0018,9470) VR=FL VM=1 Table Head Tilt Angle</summary>
		public const uint TableHeadTiltAngle = 0x00189470;

		/// <summary>(0018,9471) VR=FL VM=1 Table Cradle Tilt Angle</summary>
		public const uint TableCradleTiltAngle = 0x00189471;

		/// <summary>(0018,9472) VR=SQ VM=1 Frame Display Shutter Sequence</summary>
		public const uint FrameDisplayShutterSequence = 0x00189472;

		/// <summary>(0018,9473) VR=FL VM=1 Acquired Image Area Dose Product</summary>
		public const uint AcquiredImageAreaDoseProduct = 0x00189473;

		/// <summary>(0018,9474) VR=CS VM=1 C-arm Positioner Tabletop Relationship</summary>
		public const uint CarmPositionerTabletopRelationship = 0x00189474;

		/// <summary>(0018,9476) VR=SQ VM=1 X-Ray Geometry Sequence</summary>
		public const uint XRayGeometrySequence = 0x00189476;

		/// <summary>(0018,9477) VR=SQ VM=1 Irradiation Event Identification Sequence</summary>
		public const uint IrradiationEventIdentificationSequence = 0x00189477;

		/// <summary>(0018,9504) VR=SQ VM=1 X-Ray 3D Frame Type Sequence</summary>
		public const uint XRay3DFrameTypeSequence = 0x00189504;

		/// <summary>(0018,9506) VR=SQ VM=1 Contributing Sources Sequence</summary>
		public const uint ContributingSourcesSequence = 0x00189506;

		/// <summary>(0018,9507) VR=SQ VM=1 X-Ray 3D Acquisition Sequence</summary>
		public const uint XRay3DAcquisitionSequence = 0x00189507;

		/// <summary>(0018,9508) VR=FL VM=1 Primary Positioner Scan Arc</summary>
		public const uint PrimaryPositionerScanArc = 0x00189508;

		/// <summary>(0018,9509) VR=FL VM=1 Secondary Positioner Scan Arc</summary>
		public const uint SecondaryPositionerScanArc = 0x00189509;

		/// <summary>(0018,9510) VR=FL VM=1 Primary Positioner Scan Start Angle</summary>
		public const uint PrimaryPositionerScanStartAngle = 0x00189510;

		/// <summary>(0018,9511) VR=FL VM=1 Secondary Positioner Scan Start Angle</summary>
		public const uint SecondaryPositionerScanStartAngle = 0x00189511;

		/// <summary>(0018,9514) VR=FL VM=1 Primary Positioner Increment</summary>
		public const uint PrimaryPositionerIncrement = 0x00189514;

		/// <summary>(0018,9515) VR=FL VM=1 Secondary Positioner Increment</summary>
		public const uint SecondaryPositionerIncrement = 0x00189515;

		/// <summary>(0018,9516) VR=DT VM=1 Start Acquisition DateTime</summary>
		public const uint StartAcquisitionDateTime = 0x00189516;

		/// <summary>(0018,9517) VR=DT VM=1 End Acquisition DateTime</summary>
		public const uint EndAcquisitionDateTime = 0x00189517;

		/// <summary>(0018,9524) VR=LO VM=1 Application Name</summary>
		public const uint ApplicationName = 0x00189524;

		/// <summary>(0018,9525) VR=LO VM=1 Application Version</summary>
		public const uint ApplicationVersion = 0x00189525;

		/// <summary>(0018,9526) VR=LO VM=1 Application Manufacturer</summary>
		public const uint ApplicationManufacturer = 0x00189526;

		/// <summary>(0018,9527) VR=CS VM=1 Algorithm Type</summary>
		public const uint AlgorithmType = 0x00189527;

		/// <summary>(0018,9528) VR=LO VM=1 Algorithm Description</summary>
		public const uint AlgorithmDescription = 0x00189528;

		/// <summary>(0018,9530) VR=SQ VM=1 X-Ray 3D Reconstruction Sequence</summary>
		public const uint XRay3DReconstructionSequence = 0x00189530;

		/// <summary>(0018,9531) VR=LO VM=1 Reconstruction Description</summary>
		public const uint ReconstructionDescription = 0x00189531;

		/// <summary>(0018,9538) VR=SQ VM=1 Per Projection Acquisition Sequence</summary>
		public const uint PerProjectionAcquisitionSequence = 0x00189538;

		/// <summary>(0018,9601) VR=SQ VM=1 Diffusion b-matrix Sequence</summary>
		public const uint DiffusionBmatrixSequence = 0x00189601;

		/// <summary>(0018,9602) VR=FD VM=1 Diffusion b-value XX</summary>
		public const uint DiffusionBvalueXX = 0x00189602;

		/// <summary>(0018,9603) VR=FD VM=1 Diffusion b-value XY</summary>
		public const uint DiffusionBvalueXY = 0x00189603;

		/// <summary>(0018,9604) VR=FD VM=1 Diffusion b-value XZ</summary>
		public const uint DiffusionBvalueXZ = 0x00189604;

		/// <summary>(0018,9605) VR=FD VM=1 Diffusion b-value YY</summary>
		public const uint DiffusionBvalueYY = 0x00189605;

		/// <summary>(0018,9606) VR=FD VM=1 Diffusion b-value YZ</summary>
		public const uint DiffusionBvalueYZ = 0x00189606;

		/// <summary>(0018,9607) VR=FD VM=1 Diffusion b-value ZZ</summary>
		public const uint DiffusionBvalueZZ = 0x00189607;

		/// <summary>(0018,a001) VR=SQ VM=1 Contributing Equipment Sequence</summary>
		public const uint ContributingEquipmentSequence = 0x0018a001;

		/// <summary>(0018,a002) VR=DT VM=1 Contribution Date Time</summary>
		public const uint ContributionDateTime = 0x0018a002;

		/// <summary>(0018,a003) VR=ST VM=1 Contribution Description</summary>
		public const uint ContributionDescription = 0x0018a003;

		/// <summary>(0020,000d) VR=UI VM=1 Study Instance UID</summary>
		public const uint StudyInstanceUID = 0x0020000d;

		/// <summary>(0020,000e) VR=UI VM=1 Series Instance UID</summary>
		public const uint SeriesInstanceUID = 0x0020000e;

		/// <summary>(0020,0010) VR=SH VM=1 Study ID</summary>
		public const uint StudyID = 0x00200010;

		/// <summary>(0020,0011) VR=IS VM=1 Series Number</summary>
		public const uint SeriesNumber = 0x00200011;

		/// <summary>(0020,0012) VR=IS VM=1 Acquisition Number</summary>
		public const uint AcquisitionNumber = 0x00200012;

		/// <summary>(0020,0013) VR=IS VM=1 Instance Number</summary>
		public const uint InstanceNumber = 0x00200013;

		/// <summary>(0020,0019) VR=IS VM=1 Item Number</summary>
		public const uint ItemNumber = 0x00200019;

		/// <summary>(0020,0020) VR=CS VM=2 Patient Orientation</summary>
		public const uint PatientOrientation = 0x00200020;

		/// <summary>(0020,0032) VR=DS VM=3 Image Position (Patient)</summary>
		public const uint ImagePositionPatient = 0x00200032;

		/// <summary>(0020,0037) VR=DS VM=6 Image Orientation (Patient)</summary>
		public const uint ImageOrientationPatient = 0x00200037;

		/// <summary>(0020,0052) VR=UI VM=1 Frame of Reference UID</summary>
		public const uint FrameOfReferenceUID = 0x00200052;

		/// <summary>(0020,0060) VR=CS VM=1 Laterality</summary>
		public const uint Laterality = 0x00200060;

		/// <summary>(0020,0062) VR=CS VM=1 Image Laterality</summary>
		public const uint ImageLaterality = 0x00200062;

		/// <summary>(0020,0100) VR=IS VM=1 Temporal Position Identifier</summary>
		public const uint TemporalPositionIdentifier = 0x00200100;

		/// <summary>(0020,0105) VR=IS VM=1 Number of Temporal Positions</summary>
		public const uint NumberOfTemporalPositions = 0x00200105;

		/// <summary>(0020,0110) VR=DS VM=1 Temporal Resolution</summary>
		public const uint TemporalResolution = 0x00200110;

		/// <summary>(0020,0200) VR=UI VM=1 Synchronization Frame of Reference UID</summary>
		public const uint SynchronizationFrameOfReferenceUID = 0x00200200;

		/// <summary>(0020,1002) VR=IS VM=1 Images in Acquisition</summary>
		public const uint ImagesInAcquisition = 0x00201002;

		/// <summary>(0020,1040) VR=LO VM=1 Position Reference Indicator</summary>
		public const uint PositionReferenceIndicator = 0x00201040;

		/// <summary>(0020,1041) VR=DS VM=1 Slice Location</summary>
		public const uint SliceLocation = 0x00201041;

		/// <summary>(0020,1200) VR=IS VM=1 Number of Patient Related Studies</summary>
		public const uint NumberOfPatientRelatedStudies = 0x00201200;

		/// <summary>(0020,1202) VR=IS VM=1 Number of Patient Related Series</summary>
		public const uint NumberOfPatientRelatedSeries = 0x00201202;

		/// <summary>(0020,1204) VR=IS VM=1 Number of Patient Related Instances</summary>
		public const uint NumberOfPatientRelatedInstances = 0x00201204;

		/// <summary>(0020,1206) VR=IS VM=1 Number of Study Related Series</summary>
		public const uint NumberOfStudyRelatedSeries = 0x00201206;

		/// <summary>(0020,1208) VR=IS VM=1 Number of Study Related Instances</summary>
		public const uint NumberOfStudyRelatedInstances = 0x00201208;

		/// <summary>(0020,1209) VR=IS VM=1 Number of Series Related Instances</summary>
		public const uint NumberOfSeriesRelatedInstances = 0x00201209;

		/// <summary>(0020,4000) VR=LT VM=1 Image Comments</summary>
		public const uint ImageComments = 0x00204000;

		/// <summary>(0020,9056) VR=SH VM=1 Stack ID</summary>
		public const uint StackID = 0x00209056;

		/// <summary>(0020,9057) VR=UL VM=1 In-Stack Position Number</summary>
		public const uint InStackPositionNumber = 0x00209057;

		/// <summary>(0020,9071) VR=SQ VM=1 Frame Anatomy Sequence</summary>
		public const uint FrameAnatomySequence = 0x00209071;

		/// <summary>(0020,9072) VR=CS VM=1 Frame Laterality</summary>
		public const uint FrameLaterality = 0x00209072;

		/// <summary>(0020,9111) VR=SQ VM=1 Frame Content Sequence</summary>
		public const uint FrameContentSequence = 0x00209111;

		/// <summary>(0020,9113) VR=SQ VM=1 Plane Position Sequence</summary>
		public const uint PlanePositionSequence = 0x00209113;

		/// <summary>(0020,9116) VR=SQ VM=1 Plane Orientation Sequence</summary>
		public const uint PlaneOrientationSequence = 0x00209116;

		/// <summary>(0020,9128) VR=UL VM=1 Temporal Position Index</summary>
		public const uint TemporalPositionIndex = 0x00209128;

		/// <summary>(0020,9153) VR=FD VM=1 Nominal Cardiac Trigger Delay Time</summary>
		public const uint NominalCardiacTriggerDelayTime = 0x00209153;

		/// <summary>(0020,9156) VR=US VM=1 Frame Acquisition Number</summary>
		public const uint FrameAcquisitionNumber = 0x00209156;

		/// <summary>(0020,9157) VR=UL VM=1-n Dimension Index Values</summary>
		public const uint DimensionIndexValues = 0x00209157;

		/// <summary>(0020,9158) VR=LT VM=1 Frame Comments</summary>
		public const uint FrameComments = 0x00209158;

		/// <summary>(0020,9161) VR=UI VM=1 Concatenation UID</summary>
		public const uint ConcatenationUID = 0x00209161;

		/// <summary>(0020,9162) VR=US VM=1 In-concatenation Number</summary>
		public const uint InconcatenationNumber = 0x00209162;

		/// <summary>(0020,9163) VR=US VM=1 In-concatenation Total Number</summary>
		public const uint InconcatenationTotalNumber = 0x00209163;

		/// <summary>(0020,9164) VR=UI VM=1 Dimension Organization UID</summary>
		public const uint DimensionOrganizationUID = 0x00209164;

		/// <summary>(0020,9165) VR=AT VM=1 Dimension Index Pointer</summary>
		public const uint DimensionIndexPointer = 0x00209165;

		/// <summary>(0020,9167) VR=AT VM=1 Functional Group Pointer</summary>
		public const uint FunctionalGroupPointer = 0x00209167;

		/// <summary>(0020,9213) VR=LO VM=1 Dimension Index Private Creator</summary>
		public const uint DimensionIndexPrivateCreator = 0x00209213;

		/// <summary>(0020,9221) VR=SQ VM=1 Dimension Organization Sequence</summary>
		public const uint DimensionOrganizationSequence = 0x00209221;

		/// <summary>(0020,9222) VR=SQ VM=1 Dimension Index Sequence</summary>
		public const uint DimensionIndexSequence = 0x00209222;

		/// <summary>(0020,9228) VR=UL VM=1 Concatenation Frame Offset Number</summary>
		public const uint ConcatenationFrameOffsetNumber = 0x00209228;

		/// <summary>(0020,9238) VR=LO VM=1 Functional Group Private Creator</summary>
		public const uint FunctionalGroupPrivateCreator = 0x00209238;

		/// <summary>(0020,9241) VR=FL VM=1 Nominal Percentage of Cardiac Phase</summary>
		public const uint NominalPercentageOfCardiacPhase = 0x00209241;

		/// <summary>(0020,9245) VR=FL VM=1 Nominal Percentage of Respiratory Phase</summary>
		public const uint NominalPercentageOfRespiratoryPhase = 0x00209245;

		/// <summary>(0020,9246) VR=FL VM=1 Starting Respiratory Amplitude</summary>
		public const uint StartingRespiratoryAmplitude = 0x00209246;

		/// <summary>(0020,9247) VR=CS VM=1 Starting Respiratory Phase</summary>
		public const uint StartingRespiratoryPhase = 0x00209247;

		/// <summary>(0020,9248) VR=FL VM=1 Ending Respiratory Amplitude</summary>
		public const uint EndingRespiratoryAmplitude = 0x00209248;

		/// <summary>(0020,9249) VR=CS VM=1 Ending Respiratory Phase</summary>
		public const uint EndingRespiratoryPhase = 0x00209249;

		/// <summary>(0020,9250) VR=CS VM=1 Respiratory Trigger Type</summary>
		public const uint RespiratoryTriggerType = 0x00209250;

		/// <summary>(0020,9251) VR=FD VM=1 R - R Interval Time Nominal</summary>
		public const uint RRIntervalTimeNominal = 0x00209251;

		/// <summary>(0020,9252) VR=FD VM=1 Actual Cardiac Trigger Delay Time</summary>
		public const uint ActualCardiacTriggerDelayTime = 0x00209252;

		/// <summary>(0020,9253) VR=SQ VM=1 Respiratory Synchronization Sequence</summary>
		public const uint RespiratorySynchronizationSequence = 0x00209253;

		/// <summary>(0020,9254) VR=FD VM=1 Respiratory Interval Time</summary>
		public const uint RespiratoryIntervalTime = 0x00209254;

		/// <summary>(0020,9255) VR=FD VM=1 Nominal Respiratory Trigger Delay Time</summary>
		public const uint NominalRespiratoryTriggerDelayTime = 0x00209255;

		/// <summary>(0020,9256) VR=FD VM=1 Respiratory Trigger Delay Threshold</summary>
		public const uint RespiratoryTriggerDelayThreshold = 0x00209256;

		/// <summary>(0020,9257) VR=FD VM=1 Actual Respiratory Trigger Delay Time</summary>
		public const uint ActualRespiratoryTriggerDelayTime = 0x00209257;

		/// <summary>(0020,9421) VR=LO VM=1 Dimension Description Label</summary>
		public const uint DimensionDescriptionLabel = 0x00209421;

		/// <summary>(0020,9450) VR=SQ VM=1 Patient Orientation in Frame Sequence</summary>
		public const uint PatientOrientationInFrameSequence = 0x00209450;

		/// <summary>(0020,9453) VR=LO VM=1 Frame Label</summary>
		public const uint FrameLabel = 0x00209453;

		/// <summary>(0020,9518) VR=US VM=1-n Acquisition Index</summary>
		public const uint AcquisitionIndex = 0x00209518;

		/// <summary>(0020,9529) VR=SQ VM=1 Contributing SOP Instances Reference Sequence</summary>
		public const uint ContributingSOPInstancesReferenceSequence = 0x00209529;

		/// <summary>(0020,9536) VR=US VM=1 Reconstruction Index</summary>
		public const uint ReconstructionIndex = 0x00209536;

		/// <summary>(0022,0001) VR=US VM=1 Light Path Filter Pass-Through Wavelength</summary>
		public const uint LightPathFilterPassThroughWavelength = 0x00220001;

		/// <summary>(0022,0002) VR=US VM=2 Light Path Filter Pass Band</summary>
		public const uint LightPathFilterPassBand = 0x00220002;

		/// <summary>(0022,0003) VR=US VM=1 Image Path Filter Pass-Through Wavelength</summary>
		public const uint ImagePathFilterPassThroughWavelength = 0x00220003;

		/// <summary>(0022,0004) VR=US VM=2 Image Path Filter Pass Band</summary>
		public const uint ImagePathFilterPassBand = 0x00220004;

		/// <summary>(0022,0005) VR=CS VM=1 Patient Eye Movement Commanded</summary>
		public const uint PatientEyeMovementCommanded = 0x00220005;

		/// <summary>(0022,0006) VR=SQ VM=1 Patient Eye Movement Command Code Sequence</summary>
		public const uint PatientEyeMovementCommandCodeSequence = 0x00220006;

		/// <summary>(0022,0007) VR=FL VM=1 Spherical Lens Power</summary>
		public const uint SphericalLensPower = 0x00220007;

		/// <summary>(0022,0008) VR=FL VM=1 Cylinder Lens Power</summary>
		public const uint CylinderLensPower = 0x00220008;

		/// <summary>(0022,0009) VR=FL VM=1 Cylinder Axis</summary>
		public const uint CylinderAxis = 0x00220009;

		/// <summary>(0022,000a) VR=FL VM=1 Emmetropic Magnification</summary>
		public const uint EmmetropicMagnification = 0x0022000a;

		/// <summary>(0022,000b) VR=FL VM=1 Intra Ocular Pressure</summary>
		public const uint IntraOcularPressure = 0x0022000b;

		/// <summary>(0022,000c) VR=FL VM=1 Horizontal Field of View</summary>
		public const uint HorizontalFieldOfView = 0x0022000c;

		/// <summary>(0022,000d) VR=CS VM=1 Pupil Dilated</summary>
		public const uint PupilDilated = 0x0022000d;

		/// <summary>(0022,000e) VR=FL VM=1 Degree of Dilation</summary>
		public const uint DegreeOfDilation = 0x0022000e;

		/// <summary>(0022,0010) VR=FL VM=1 Stereo Baseline Angle</summary>
		public const uint StereoBaselineAngle = 0x00220010;

		/// <summary>(0022,0011) VR=FL VM=1 Stereo Baseline Displacement</summary>
		public const uint StereoBaselineDisplacement = 0x00220011;

		/// <summary>(0022,0012) VR=FL VM=1 Stereo Horizontal Pixel Offset</summary>
		public const uint StereoHorizontalPixelOffset = 0x00220012;

		/// <summary>(0022,0013) VR=FL VM=1 Stereo Vertical Pixel Offset</summary>
		public const uint StereoVerticalPixelOffset = 0x00220013;

		/// <summary>(0022,0014) VR=FL VM=1 Stereo Rotation</summary>
		public const uint StereoRotation = 0x00220014;

		/// <summary>(0022,0015) VR=SQ VM=1 Acquisition Device Type Code Sequence</summary>
		public const uint AcquisitionDeviceTypeCodeSequence = 0x00220015;

		/// <summary>(0022,0016) VR=SQ VM=1 Illumination Type Code Sequence</summary>
		public const uint IlluminationTypeCodeSequence = 0x00220016;

		/// <summary>(0022,0017) VR=SQ VM=1 Light Path Filter Type Stack Code Sequence</summary>
		public const uint LightPathFilterTypeStackCodeSequence = 0x00220017;

		/// <summary>(0022,0018) VR=SQ VM=1 Image Path Filter Type Stack Code Sequence</summary>
		public const uint ImagePathFilterTypeStackCodeSequence = 0x00220018;

		/// <summary>(0022,0019) VR=SQ VM=1 Lenses Code Sequence</summary>
		public const uint LensesCodeSequence = 0x00220019;

		/// <summary>(0022,001a) VR=SQ VM=1 Channel Description Code Sequence</summary>
		public const uint ChannelDescriptionCodeSequence = 0x0022001a;

		/// <summary>(0022,001b) VR=SQ VM=1 Refractive State Sequence</summary>
		public const uint RefractiveStateSequence = 0x0022001b;

		/// <summary>(0022,001c) VR=SQ VM=1 Mydriatic Agent Code Sequence</summary>
		public const uint MydriaticAgentCodeSequence = 0x0022001c;

		/// <summary>(0022,001d) VR=SQ VM=1 Relative Image Position Code Sequence</summary>
		public const uint RelativeImagePositionCodeSequence = 0x0022001d;

		/// <summary>(0022,0020) VR=SQ VM=1 Stereo Pairs Sequence</summary>
		public const uint StereoPairsSequence = 0x00220020;

		/// <summary>(0022,0021) VR=SQ VM=1 Left Image Sequence</summary>
		public const uint LeftImageSequence = 0x00220021;

		/// <summary>(0022,0022) VR=SQ VM=1 Right Image Sequence</summary>
		public const uint RightImageSequence = 0x00220022;

		/// <summary>(0022,0030) VR=FL VM=1 Axial Length of the Eye</summary>
		public const uint AxialLengthOfTheEye = 0x00220030;

		/// <summary>(0022,0031) VR=SQ VM=1 Ophthalmic Frame Location Sequence</summary>
		public const uint OphthalmicFrameLocationSequence = 0x00220031;

		/// <summary>(0022,0032) VR=FL VM=2-2n Reference Coordinates</summary>
		public const uint ReferenceCoordinates = 0x00220032;

		/// <summary>(0022,0035) VR=FL VM=1 Depth Spatial Resolution</summary>
		public const uint DepthSpatialResolution = 0x00220035;

		/// <summary>(0022,0036) VR=FL VM=1 Maximum Depth Distortion</summary>
		public const uint MaximumDepthDistortion = 0x00220036;

		/// <summary>(0022,0037) VR=FL VM=1 Along-scan Spatial Resolution</summary>
		public const uint AlongscanSpatialResolution = 0x00220037;

		/// <summary>(0022,0038) VR=FL VM=1 Maximum Along-scan Distortion</summary>
		public const uint MaximumAlongscanDistortion = 0x00220038;

		/// <summary>(0022,0039) VR=CS VM=1 Ophthalmic Image Orientation</summary>
		public const uint OphthalmicImageOrientation = 0x00220039;

		/// <summary>(0022,0041) VR=FL VM=1 Depth of Transverse Image</summary>
		public const uint DepthOfTransverseImage = 0x00220041;

		/// <summary>(0022,0042) VR=SQ VM=1 Mydriatic Agent Concentration Units Sequence</summary>
		public const uint MydriaticAgentConcentrationUnitsSequence = 0x00220042;

		/// <summary>(0022,0048) VR=FL VM=1 Across-scan Spatial Resolution</summary>
		public const uint AcrossscanSpatialResolution = 0x00220048;

		/// <summary>(0022,0049) VR=FL VM=1 Maximum Across-scan Distortion</summary>
		public const uint MaximumAcrossscanDistortion = 0x00220049;

		/// <summary>(0022,004e) VR=DS VM=1 Mydriatic Agent Concentration</summary>
		public const uint MydriaticAgentConcentration = 0x0022004e;

		/// <summary>(0022,0055) VR=FL VM=1 Illumination Wave Length</summary>
		public const uint IlluminationWaveLength = 0x00220055;

		/// <summary>(0022,0056) VR=FL VM=1 Illumination Power</summary>
		public const uint IlluminationPower = 0x00220056;

		/// <summary>(0022,0057) VR=FL VM=1 Illumination Bandwidth</summary>
		public const uint IlluminationBandwidth = 0x00220057;

		/// <summary>(0022,0058) VR=SQ VM=1 Mydriatic Agent Sequence</summary>
		public const uint MydriaticAgentSequence = 0x00220058;

		/// <summary>(0028,0002) VR=US VM=1 Samples per Pixel</summary>
		public const uint SamplesPerPixel = 0x00280002;

		/// <summary>(0028,0003) VR=US VM=1 Samples per Pixel Used</summary>
		public const uint SamplesPerPixelUsed = 0x00280003;

		/// <summary>(0028,0004) VR=CS VM=1 Photometric Interpretation</summary>
		public const uint PhotometricInterpretation = 0x00280004;

		/// <summary>(0028,0006) VR=US VM=1 Planar Configuration</summary>
		public const uint PlanarConfiguration = 0x00280006;

		/// <summary>(0028,0008) VR=IS VM=1 Number of Frames</summary>
		public const uint NumberOfFrames = 0x00280008;

		/// <summary>(0028,0009) VR=AT VM=1-n Frame Increment Pointer</summary>
		public const uint FrameIncrementPointer = 0x00280009;

		/// <summary>(0028,000a) VR=AT VM=1-n Frame Dimension Pointer</summary>
		public const uint FrameDimensionPointer = 0x0028000a;

		/// <summary>(0028,0010) VR=US VM=1 Rows</summary>
		public const uint Rows = 0x00280010;

		/// <summary>(0028,0011) VR=US VM=1 Columns</summary>
		public const uint Columns = 0x00280011;

		/// <summary>(0028,0014) VR=US VM=1 Ultrasound Color Data Present</summary>
		public const uint UltrasoundColorDataPresent = 0x00280014;

		/// <summary>(0028,0030) VR=DS VM=2 Pixel Spacing</summary>
		public const uint PixelSpacing = 0x00280030;

		/// <summary>(0028,0031) VR=DS VM=2 Zoom Factor</summary>
		public const uint ZoomFactor = 0x00280031;

		/// <summary>(0028,0032) VR=DS VM=2 Zoom Center</summary>
		public const uint ZoomCenter = 0x00280032;

		/// <summary>(0028,0034) VR=IS VM=2 Pixel Aspect Ratio</summary>
		public const uint PixelAspectRatio = 0x00280034;

		/// <summary>(0028,0051) VR=CS VM=1-n Corrected Image</summary>
		public const uint CorrectedImage = 0x00280051;

		/// <summary>(0028,0100) VR=US VM=1 Bits Allocated</summary>
		public const uint BitsAllocated = 0x00280100;

		/// <summary>(0028,0101) VR=US VM=1 Bits Stored</summary>
		public const uint BitsStored = 0x00280101;

		/// <summary>(0028,0102) VR=US VM=1 High Bit</summary>
		public const uint HighBit = 0x00280102;

		/// <summary>(0028,0103) VR=US VM=1 Pixel Representation</summary>
		public const uint PixelRepresentation = 0x00280103;

		/// <summary>(0028,0106) VR=US/SS VM=1 Smallest Image Pixel Value</summary>
		public const uint SmallestImagePixelValue = 0x00280106;

		/// <summary>(0028,0107) VR=US/SS VM=1 Largest Image Pixel Value</summary>
		public const uint LargestImagePixelValue = 0x00280107;

		/// <summary>(0028,0108) VR=US/SS VM=1 Smallest Pixel Value in Series</summary>
		public const uint SmallestPixelValueInSeries = 0x00280108;

		/// <summary>(0028,0109) VR=US/SS VM=1 Largest Pixel Value in Series</summary>
		public const uint LargestPixelValueInSeries = 0x00280109;

		/// <summary>(0028,0120) VR=US/SS VM=1 Pixel Padding Value</summary>
		public const uint PixelPaddingValue = 0x00280120;

		/// <summary>(0028,0121) VR=US/SS VM=1 Pixel Padding Range Limit</summary>
		public const uint PixelPaddingRangeLimit = 0x00280121;

		/// <summary>(0028,0300) VR=CS VM=1 Quality Control Image</summary>
		public const uint QualityControlImage = 0x00280300;

		/// <summary>(0028,0301) VR=CS VM=1 Burned In Annotation</summary>
		public const uint BurnedInAnnotation = 0x00280301;

		/// <summary>(0028,0a02) VR=CS VM=1 Pixel Spacing Calibration Type</summary>
		public const uint PixelSpacingCalibrationType = 0x00280a02;

		/// <summary>(0028,0a04) VR=LO VM=1 Pixel Spacing Calibration Description</summary>
		public const uint PixelSpacingCalibrationDescription = 0x00280a04;

		/// <summary>(0028,1040) VR=CS VM=1 Pixel Intensity Relationship</summary>
		public const uint PixelIntensityRelationship = 0x00281040;

		/// <summary>(0028,1041) VR=SS VM=1 Pixel Intensity Relationship Sign</summary>
		public const uint PixelIntensityRelationshipSign = 0x00281041;

		/// <summary>(0028,1050) VR=DS VM=1-n Window Center</summary>
		public const uint WindowCenter = 0x00281050;

		/// <summary>(0028,1051) VR=DS VM=1-n Window Width</summary>
		public const uint WindowWidth = 0x00281051;

		/// <summary>(0028,1052) VR=DS VM=1 Rescale Intercept</summary>
		public const uint RescaleIntercept = 0x00281052;

		/// <summary>(0028,1053) VR=DS VM=1 Rescale Slope</summary>
		public const uint RescaleSlope = 0x00281053;

		/// <summary>(0028,1054) VR=LO VM=1 Rescale Type</summary>
		public const uint RescaleType = 0x00281054;

		/// <summary>(0028,1055) VR=LO VM=1-n Window Center & Width Explanation</summary>
		public const uint WindowCenterWidthExplanation = 0x00281055;

		/// <summary>(0028,1056) VR=CS VM=1 VOI LUT Function</summary>
		public const uint VOILUTFunction = 0x00281056;

		/// <summary>(0028,1090) VR=CS VM=1 Recommended Viewing Mode</summary>
		public const uint RecommendedViewingMode = 0x00281090;

		/// <summary>(0028,1101) VR=US/SS VM=3 Red Palette Color Lookup Table Descriptor</summary>
		public const uint RedPaletteColorLookupTableDescriptor = 0x00281101;

		/// <summary>(0028,1102) VR=US/SS VM=3 Green Palette Color Lookup Table Descriptor</summary>
		public const uint GreenPaletteColorLookupTableDescriptor = 0x00281102;

		/// <summary>(0028,1103) VR=US/SS VM=3 Blue Palette Color Lookup Table Descriptor</summary>
		public const uint BluePaletteColorLookupTableDescriptor = 0x00281103;

		/// <summary>(0028,1199) VR=UI VM=1 Palette Color Lookup Table UID</summary>
		public const uint PaletteColorLookupTableUID = 0x00281199;

		/// <summary>(0028,1201) VR=OW VM=1 Red Palette Color Lookup Table Data</summary>
		public const uint RedPaletteColorLookupTableData = 0x00281201;

		/// <summary>(0028,1202) VR=OW VM=1 Green Palette Color Lookup Table Data</summary>
		public const uint GreenPaletteColorLookupTableData = 0x00281202;

		/// <summary>(0028,1203) VR=OW VM=1 Blue Palette Color Lookup Table Data</summary>
		public const uint BluePaletteColorLookupTableData = 0x00281203;

		/// <summary>(0028,1221) VR=OW VM=1 Segmented Red Palette Color Lookup Table Data</summary>
		public const uint SegmentedRedPaletteColorLookupTableData = 0x00281221;

		/// <summary>(0028,1222) VR=OW VM=1 Segmented Green Palette Color Lookup Table Data</summary>
		public const uint SegmentedGreenPaletteColorLookupTableData = 0x00281222;

		/// <summary>(0028,1223) VR=OW VM=1 Segmented Blue Palette Color Lookup Table Data</summary>
		public const uint SegmentedBluePaletteColorLookupTableData = 0x00281223;

		/// <summary>(0028,1300) VR=CS VM=1 Implant Present</summary>
		public const uint ImplantPresent = 0x00281300;

		/// <summary>(0028,1350) VR=CS VM=1 Partial View</summary>
		public const uint PartialView = 0x00281350;

		/// <summary>(0028,1351) VR=ST VM=1 Partial View Description</summary>
		public const uint PartialViewDescription = 0x00281351;

		/// <summary>(0028,1352) VR=SQ VM=1 Partial View Code Sequence</summary>
		public const uint PartialViewCodeSequence = 0x00281352;

		/// <summary>(0028,135a) VR=CS VM=1 Spatial Locations Preserved</summary>
		public const uint SpatialLocationsPreserved = 0x0028135a;

		/// <summary>(0028,2000) VR=OB VM=1 ICC Profile</summary>
		public const uint ICCProfile = 0x00282000;

		/// <summary>(0028,2110) VR=CS VM=1 Lossy Image Compression</summary>
		public const uint LossyImageCompression = 0x00282110;

		/// <summary>(0028,2112) VR=DS VM=1-n Lossy Image Compression Ratio</summary>
		public const uint LossyImageCompressionRatio = 0x00282112;

		/// <summary>(0028,2114) VR=CS VM=1-n Lossy Image Compression Method</summary>
		public const uint LossyImageCompressionMethod = 0x00282114;

		/// <summary>(0028,3000) VR=SQ VM=1 Modality LUT Sequence</summary>
		public const uint ModalityLUTSequence = 0x00283000;

		/// <summary>(0028,3002) VR=US/SS VM=3 LUT Descriptor</summary>
		public const uint LUTDescriptor = 0x00283002;

		/// <summary>(0028,3003) VR=LO VM=1 LUT Explanation</summary>
		public const uint LUTExplanation = 0x00283003;

		/// <summary>(0028,3004) VR=LO VM=1 Modality LUT Type</summary>
		public const uint ModalityLUTType = 0x00283004;

		/// <summary>(0028,3006) VR=US/SS/OW VM=1-n LUT Data</summary>
		public const uint LUTData = 0x00283006;

		/// <summary>(0028,3010) VR=SQ VM=1 VOI LUT Sequence</summary>
		public const uint VOILUTSequence = 0x00283010;

		/// <summary>(0028,3110) VR=SQ VM=1 Softcopy VOI LUT Sequence</summary>
		public const uint SoftcopyVOILUTSequence = 0x00283110;

		/// <summary>(0028,6010) VR=US VM=1 Representative Frame Number</summary>
		public const uint RepresentativeFrameNumber = 0x00286010;

		/// <summary>(0028,6020) VR=US VM=1-n Frame Numbers of Interest (FOI)</summary>
		public const uint FrameNumbersOfInterestFOI = 0x00286020;

		/// <summary>(0028,6022) VR=LO VM=1-n Frame(s) of Interest Description</summary>
		public const uint FramesOfInterestDescription = 0x00286022;

		/// <summary>(0028,6023) VR=CS VM=1-n Frame of Interest Type</summary>
		public const uint FrameOfInterestType = 0x00286023;

		/// <summary>(0028,6040) VR=US VM=1-n R Wave Pointer</summary>
		public const uint RWavePointer = 0x00286040;

		/// <summary>(0028,6100) VR=SQ VM=1 Mask Subtraction Sequence</summary>
		public const uint MaskSubtractionSequence = 0x00286100;

		/// <summary>(0028,6101) VR=CS VM=1 Mask Operation</summary>
		public const uint MaskOperation = 0x00286101;

		/// <summary>(0028,6102) VR=US VM=2-2n Applicable Frame Range</summary>
		public const uint ApplicableFrameRange = 0x00286102;

		/// <summary>(0028,6110) VR=US VM=1-n Mask Frame Numbers</summary>
		public const uint MaskFrameNumbers = 0x00286110;

		/// <summary>(0028,6112) VR=US VM=1 Contrast Frame Averaging</summary>
		public const uint ContrastFrameAveraging = 0x00286112;

		/// <summary>(0028,6114) VR=FL VM=2 Mask Sub-pixel Shift</summary>
		public const uint MaskSubpixelShift = 0x00286114;

		/// <summary>(0028,6120) VR=SS VM=1 TID Offset</summary>
		public const uint TIDOffset = 0x00286120;

		/// <summary>(0028,6190) VR=ST VM=1 Mask Operation Explanation</summary>
		public const uint MaskOperationExplanation = 0x00286190;

		/// <summary>(0028,7fe0) VR=UT VM=1 Pixel Data Provider URL</summary>
		public const uint PixelDataProviderURL = 0x00287fe0;

		/// <summary>(0028,9001) VR=UL VM=1 Data Point Rows</summary>
		public const uint DataPointRows = 0x00289001;

		/// <summary>(0028,9002) VR=UL VM=1 Data Point Columns</summary>
		public const uint DataPointColumns = 0x00289002;

		/// <summary>(0028,9003) VR=CS VM=1 Signal Domain Columns</summary>
		public const uint SignalDomainColumns = 0x00289003;

		/// <summary>(0028,9108) VR=CS VM=1 Data Representation</summary>
		public const uint DataRepresentation = 0x00289108;

		/// <summary>(0028,9110) VR=SQ VM=1 Pixel Measures Sequence</summary>
		public const uint PixelMeasuresSequence = 0x00289110;

		/// <summary>(0028,9132) VR=SQ VM=1 Frame VOI LUT Sequence</summary>
		public const uint FrameVOILUTSequence = 0x00289132;

		/// <summary>(0028,9145) VR=SQ VM=1 Pixel Value Transformation Sequence</summary>
		public const uint PixelValueTransformationSequence = 0x00289145;

		/// <summary>(0028,9235) VR=CS VM=1 Signal Domain Rows</summary>
		public const uint SignalDomainRows = 0x00289235;

		/// <summary>(0028,9411) VR=FL VM=1 Display Filter Percentage</summary>
		public const uint DisplayFilterPercentage = 0x00289411;

		/// <summary>(0028,9415) VR=SQ VM=1 Frame Pixel Shift Sequence</summary>
		public const uint FramePixelShiftSequence = 0x00289415;

		/// <summary>(0028,9416) VR=US VM=1 Subtraction Item ID</summary>
		public const uint SubtractionItemID = 0x00289416;

		/// <summary>(0028,9422) VR=SQ VM=1 Pixel Intensity Relationship LUT Sequence</summary>
		public const uint PixelIntensityRelationshipLUTSequence = 0x00289422;

		/// <summary>(0028,9443) VR=SQ VM=1 Frame Pixel Data Properties Sequence</summary>
		public const uint FramePixelDataPropertiesSequence = 0x00289443;

		/// <summary>(0028,9444) VR=CS VM=1 Geometrical Properties</summary>
		public const uint GeometricalProperties = 0x00289444;

		/// <summary>(0028,9445) VR=FL VM=1 Geometric Maximum Distortion</summary>
		public const uint GeometricMaximumDistortion = 0x00289445;

		/// <summary>(0028,9446) VR=CS VM=1-n Image Processing Applied</summary>
		public const uint ImageProcessingApplied = 0x00289446;

		/// <summary>(0028,9454) VR=CS VM=1 Mask Selection Mode</summary>
		public const uint MaskSelectionMode = 0x00289454;

		/// <summary>(0028,9474) VR=CS VM=1 LUT Function</summary>
		public const uint LUTFunction = 0x00289474;

		/// <summary>(0028,9520) VR=DS VM=16 Image to Equipment Mapping Matrix</summary>
		public const uint ImageToEquipmentMappingMatrix = 0x00289520;

		/// <summary>(0028,9537) VR=CS VM=1 Equipment Coordinate System Identification</summary>
		public const uint EquipmentCoordinateSystemIdentification = 0x00289537;

		/// <summary>(0032,1031) VR=SQ VM=1 Requesting Physician Identification Sequence</summary>
		public const uint RequestingPhysicianIdentificationSequence = 0x00321031;

		/// <summary>(0032,1032) VR=PN VM=1 Requesting Physician</summary>
		public const uint RequestingPhysician = 0x00321032;

		/// <summary>(0032,1033) VR=LO VM=1 Requesting Service</summary>
		public const uint RequestingService = 0x00321033;

		/// <summary>(0032,1060) VR=LO VM=1 Requested Procedure Description</summary>
		public const uint RequestedProcedureDescription = 0x00321060;

		/// <summary>(0032,1064) VR=SQ VM=1 Requested Procedure Code Sequence</summary>
		public const uint RequestedProcedureCodeSequence = 0x00321064;

		/// <summary>(0032,1070) VR=LO VM=1 Requested Contrast Agent</summary>
		public const uint RequestedContrastAgent = 0x00321070;

		/// <summary>(0038,0004) VR=SQ VM=1 Referenced Patient Alias Sequence</summary>
		public const uint ReferencedPatientAliasSequence = 0x00380004;

		/// <summary>(0038,0008) VR=CS VM=1 Visit Status ID</summary>
		public const uint VisitStatusID = 0x00380008;

		/// <summary>(0038,0010) VR=LO VM=1 Admission ID</summary>
		public const uint AdmissionID = 0x00380010;

		/// <summary>(0038,0011) VR=LO VM=1 Issuer of Admission ID</summary>
		public const uint IssuerOfAdmissionID = 0x00380011;

		/// <summary>(0038,0016) VR=LO VM=1 Route of Admissions</summary>
		public const uint RouteOfAdmissions = 0x00380016;

		/// <summary>(0038,0020) VR=DA VM=1 Admitting Date</summary>
		public const uint AdmittingDate = 0x00380020;

		/// <summary>(0038,0021) VR=TM VM=1 Admitting Time</summary>
		public const uint AdmittingTime = 0x00380021;

		/// <summary>(0038,0050) VR=LO VM=1 Special Needs</summary>
		public const uint SpecialNeeds = 0x00380050;

		/// <summary>(0038,0060) VR=LO VM=1 Service Episode ID</summary>
		public const uint ServiceEpisodeID = 0x00380060;

		/// <summary>(0038,0061) VR=LO VM=1 Issuer of Service Episode ID</summary>
		public const uint IssuerOfServiceEpisodeID = 0x00380061;

		/// <summary>(0038,0062) VR=LO VM=1 Service Episode Description</summary>
		public const uint ServiceEpisodeDescription = 0x00380062;

		/// <summary>(0038,0100) VR=SQ VM=1 Pertinent Documents Sequence</summary>
		public const uint PertinentDocumentsSequence = 0x00380100;

		/// <summary>(0038,0300) VR=LO VM=1 Current Patient Location</summary>
		public const uint CurrentPatientLocation = 0x00380300;

		/// <summary>(0038,0400) VR=LO VM=1 Patient's Institution Residence</summary>
		public const uint PatientsInstitutionResidence = 0x00380400;

		/// <summary>(0038,0500) VR=LO VM=1 Patient State</summary>
		public const uint PatientState = 0x00380500;

		/// <summary>(0038,0502) VR=SQ VM=1 Patient Clinical Trial Participation Sequence</summary>
		public const uint PatientClinicalTrialParticipationSequence = 0x00380502;

		/// <summary>(0038,4000) VR=LT VM=1 Visit Comments</summary>
		public const uint VisitComments = 0x00384000;

		/// <summary>(003a,0004) VR=CS VM=1 Waveform Originality</summary>
		public const uint WaveformOriginality = 0x003a0004;

		/// <summary>(003a,0005) VR=US VM=1 Number of Waveform Channels</summary>
		public const uint NumberOfWaveformChannels = 0x003a0005;

		/// <summary>(003a,0010) VR=UL VM=1 Number of Waveform Samples</summary>
		public const uint NumberOfWaveformSamples = 0x003a0010;

		/// <summary>(003a,001a) VR=DS VM=1 Sampling Frequency</summary>
		public const uint SamplingFrequency = 0x003a001a;

		/// <summary>(003a,0020) VR=SH VM=1 Multiplex Group Label</summary>
		public const uint MultiplexGroupLabel = 0x003a0020;

		/// <summary>(003a,0200) VR=SQ VM=1 Channel Definition Sequence</summary>
		public const uint ChannelDefinitionSequence = 0x003a0200;

		/// <summary>(003a,0202) VR=IS VM=1 Waveform Channel Number</summary>
		public const uint WaveformChannelNumber = 0x003a0202;

		/// <summary>(003a,0203) VR=SH VM=1 Channel Label</summary>
		public const uint ChannelLabel = 0x003a0203;

		/// <summary>(003a,0205) VR=CS VM=1-n Channel Status</summary>
		public const uint ChannelStatus = 0x003a0205;

		/// <summary>(003a,0208) VR=SQ VM=1 Channel Source Sequence</summary>
		public const uint ChannelSourceSequence = 0x003a0208;

		/// <summary>(003a,0209) VR=SQ VM=1 Channel Source Modifiers Sequence</summary>
		public const uint ChannelSourceModifiersSequence = 0x003a0209;

		/// <summary>(003a,020a) VR=SQ VM=1 Source Waveform Sequence</summary>
		public const uint SourceWaveformSequence = 0x003a020a;

		/// <summary>(003a,020c) VR=LO VM=1 Channel Derivation Description</summary>
		public const uint ChannelDerivationDescription = 0x003a020c;

		/// <summary>(003a,0210) VR=DS VM=1 Channel Sensitivity</summary>
		public const uint ChannelSensitivity = 0x003a0210;

		/// <summary>(003a,0211) VR=SQ VM=1 Channel Sensitivity Units Sequence</summary>
		public const uint ChannelSensitivityUnitsSequence = 0x003a0211;

		/// <summary>(003a,0212) VR=DS VM=1 Channel Sensitivity Correction Factor</summary>
		public const uint ChannelSensitivityCorrectionFactor = 0x003a0212;

		/// <summary>(003a,0213) VR=DS VM=1 Channel Baseline</summary>
		public const uint ChannelBaseline = 0x003a0213;

		/// <summary>(003a,0214) VR=DS VM=1 Channel Time Skew</summary>
		public const uint ChannelTimeSkew = 0x003a0214;

		/// <summary>(003a,0215) VR=DS VM=1 Channel Sample Skew</summary>
		public const uint ChannelSampleSkew = 0x003a0215;

		/// <summary>(003a,0218) VR=DS VM=1 Channel Offset</summary>
		public const uint ChannelOffset = 0x003a0218;

		/// <summary>(003a,021a) VR=US VM=1 Waveform Bits Stored</summary>
		public const uint WaveformBitsStored = 0x003a021a;

		/// <summary>(003a,0220) VR=DS VM=1 Filter Low Frequency</summary>
		public const uint FilterLowFrequency = 0x003a0220;

		/// <summary>(003a,0221) VR=DS VM=1 Filter High Frequency</summary>
		public const uint FilterHighFrequency = 0x003a0221;

		/// <summary>(003a,0222) VR=DS VM=1 Notch Filter Frequency</summary>
		public const uint NotchFilterFrequency = 0x003a0222;

		/// <summary>(003a,0223) VR=DS VM=1 Notch Filter Bandwidth</summary>
		public const uint NotchFilterBandwidth = 0x003a0223;

		/// <summary>(003a,0230) VR=FL VM=1 Waveform Data Display Scale</summary>
		public const uint WaveformDataDisplayScale = 0x003a0230;

		/// <summary>(003a,0231) VR=US VM=3 Waveform Display Background CIELab Value</summary>
		public const uint WaveformDisplayBackgroundCIELabValue = 0x003a0231;

		/// <summary>(003a,0240) VR=SQ VM=1 Waveform Presentation Group Sequence</summary>
		public const uint WaveformPresentationGroupSequence = 0x003a0240;

		/// <summary>(003a,0241) VR=US VM=1 Presentation Group Number</summary>
		public const uint PresentationGroupNumber = 0x003a0241;

		/// <summary>(003a,0242) VR=SQ VM=1 Channel Display Sequence</summary>
		public const uint ChannelDisplaySequence = 0x003a0242;

		/// <summary>(003a,0244) VR=US VM=3 Channel Recommended Display CIELab Value</summary>
		public const uint ChannelRecommendedDisplayCIELabValue = 0x003a0244;

		/// <summary>(003a,0245) VR=FL VM=1 Channel Position</summary>
		public const uint ChannelPosition = 0x003a0245;

		/// <summary>(003a,0246) VR=CS VM=1 Display Shading Flag</summary>
		public const uint DisplayShadingFlag = 0x003a0246;

		/// <summary>(003a,0247) VR=FL VM=1 Fractional Channel Display Scale</summary>
		public const uint FractionalChannelDisplayScale = 0x003a0247;

		/// <summary>(003a,0248) VR=FL VM=1 Absolute Channel Display Scale</summary>
		public const uint AbsoluteChannelDisplayScale = 0x003a0248;

		/// <summary>(003a,0300) VR=SQ VM=1 Multiplexed Audio Channels Description Code Sequence</summary>
		public const uint MultiplexedAudioChannelsDescriptionCodeSequence = 0x003a0300;

		/// <summary>(003a,0301) VR=IS VM=1 Channel Identification Code</summary>
		public const uint ChannelIdentificationCode = 0x003a0301;

		/// <summary>(003a,0302) VR=CS VM=1 Channel Mode</summary>
		public const uint ChannelMode = 0x003a0302;

		/// <summary>(0040,0001) VR=AE VM=1-n Scheduled Station AE Title</summary>
		public const uint ScheduledStationAETitle = 0x00400001;

		/// <summary>(0040,0002) VR=DA VM=1 Scheduled Procedure Step Start Date</summary>
		public const uint ScheduledProcedureStepStartDate = 0x00400002;

		/// <summary>(0040,0003) VR=TM VM=1 Scheduled Procedure Step Start Time</summary>
		public const uint ScheduledProcedureStepStartTime = 0x00400003;

		/// <summary>(0040,0004) VR=DA VM=1 Scheduled Procedure Step End Date</summary>
		public const uint ScheduledProcedureStepEndDate = 0x00400004;

		/// <summary>(0040,0005) VR=TM VM=1 Scheduled Procedure Step End Time</summary>
		public const uint ScheduledProcedureStepEndTime = 0x00400005;

		/// <summary>(0040,0006) VR=PN VM=1 Scheduled Performing Physician's Name</summary>
		public const uint ScheduledPerformingPhysiciansName = 0x00400006;

		/// <summary>(0040,0007) VR=LO VM=1 Scheduled Procedure Step Description</summary>
		public const uint ScheduledProcedureStepDescription = 0x00400007;

		/// <summary>(0040,0008) VR=SQ VM=1 Scheduled Protocol Code Sequence</summary>
		public const uint ScheduledProtocolCodeSequence = 0x00400008;

		/// <summary>(0040,0009) VR=SH VM=1 Scheduled Procedure Step ID</summary>
		public const uint ScheduledProcedureStepID = 0x00400009;

		/// <summary>(0040,000a) VR=SQ VM=1 Stage Code Sequence</summary>
		public const uint StageCodeSequence = 0x0040000a;

		/// <summary>(0040,000b) VR=SQ VM=1 Scheduled Performing Physician Identification Sequence</summary>
		public const uint ScheduledPerformingPhysicianIdentificationSequence = 0x0040000b;

		/// <summary>(0040,0010) VR=SH VM=1-n Scheduled Station Name</summary>
		public const uint ScheduledStationName = 0x00400010;

		/// <summary>(0040,0011) VR=SH VM=1 Scheduled Procedure Step Location</summary>
		public const uint ScheduledProcedureStepLocation = 0x00400011;

		/// <summary>(0040,0012) VR=LO VM=1 Pre-Medication</summary>
		public const uint PreMedication = 0x00400012;

		/// <summary>(0040,0020) VR=CS VM=1 Scheduled Procedure Step Status</summary>
		public const uint ScheduledProcedureStepStatus = 0x00400020;

		/// <summary>(0040,0100) VR=SQ VM=1 Scheduled Procedure Step Sequence</summary>
		public const uint ScheduledProcedureStepSequence = 0x00400100;

		/// <summary>(0040,0220) VR=SQ VM=1 Referenced Non-Image Composite SOP Instance Sequence</summary>
		public const uint ReferencedNonImageCompositeSOPInstanceSequence = 0x00400220;

		/// <summary>(0040,0241) VR=AE VM=1 Performed Station AE Title</summary>
		public const uint PerformedStationAETitle = 0x00400241;

		/// <summary>(0040,0242) VR=SH VM=1 Performed Station Name</summary>
		public const uint PerformedStationName = 0x00400242;

		/// <summary>(0040,0243) VR=SH VM=1 Performed Location</summary>
		public const uint PerformedLocation = 0x00400243;

		/// <summary>(0040,0244) VR=DA VM=1 Performed Procedure Step Start Date</summary>
		public const uint PerformedProcedureStepStartDate = 0x00400244;

		/// <summary>(0040,0245) VR=TM VM=1 Performed Procedure Step Start Time</summary>
		public const uint PerformedProcedureStepStartTime = 0x00400245;

		/// <summary>(0040,0250) VR=DA VM=1 Performed Procedure Step End Date</summary>
		public const uint PerformedProcedureStepEndDate = 0x00400250;

		/// <summary>(0040,0251) VR=TM VM=1 Performed Procedure Step End Time</summary>
		public const uint PerformedProcedureStepEndTime = 0x00400251;

		/// <summary>(0040,0252) VR=CS VM=1 Performed Procedure Step Status</summary>
		public const uint PerformedProcedureStepStatus = 0x00400252;

		/// <summary>(0040,0253) VR=SH VM=1 Performed Procedure Step ID</summary>
		public const uint PerformedProcedureStepID = 0x00400253;

		/// <summary>(0040,0254) VR=LO VM=1 Performed Procedure Step Description</summary>
		public const uint PerformedProcedureStepDescription = 0x00400254;

		/// <summary>(0040,0255) VR=LO VM=1 Performed Procedure Type Description</summary>
		public const uint PerformedProcedureTypeDescription = 0x00400255;

		/// <summary>(0040,0260) VR=SQ VM=1 Performed Protocol Code Sequence</summary>
		public const uint PerformedProtocolCodeSequence = 0x00400260;

		/// <summary>(0040,0270) VR=SQ VM=1 Scheduled Step Attributes Sequence</summary>
		public const uint ScheduledStepAttributesSequence = 0x00400270;

		/// <summary>(0040,0275) VR=SQ VM=1 Request Attributes Sequence</summary>
		public const uint RequestAttributesSequence = 0x00400275;

		/// <summary>(0040,0280) VR=ST VM=1 Comments on the Performed Procedure Step</summary>
		public const uint CommentsOnThePerformedProcedureStep = 0x00400280;

		/// <summary>(0040,0281) VR=SQ VM=1 Performed Procedure Step Discontinuation Reason Code Sequence</summary>
		public const uint PerformedProcedureStepDiscontinuationReasonCodeSequence = 0x00400281;

		/// <summary>(0040,0293) VR=SQ VM=1 Quantity Sequence</summary>
		public const uint QuantitySequence = 0x00400293;

		/// <summary>(0040,0294) VR=DS VM=1 Quantity</summary>
		public const uint Quantity = 0x00400294;

		/// <summary>(0040,0295) VR=SQ VM=1 Measuring Units Sequence</summary>
		public const uint MeasuringUnitsSequence = 0x00400295;

		/// <summary>(0040,0296) VR=SQ VM=1 Billing Item Sequence</summary>
		public const uint BillingItemSequence = 0x00400296;

		/// <summary>(0040,0300) VR=US VM=1 Total Time of Fluoroscopy</summary>
		public const uint TotalTimeOfFluoroscopy = 0x00400300;

		/// <summary>(0040,0301) VR=US VM=1 Total Number of Exposures</summary>
		public const uint TotalNumberOfExposures = 0x00400301;

		/// <summary>(0040,0302) VR=US VM=1 Entrance Dose</summary>
		public const uint EntranceDose = 0x00400302;

		/// <summary>(0040,0303) VR=US VM=1-2 Exposed Area</summary>
		public const uint ExposedArea = 0x00400303;

		/// <summary>(0040,0306) VR=DS VM=1 Distance Source to Entrance</summary>
		public const uint DistanceSourceToEntrance = 0x00400306;

		/// <summary>(0040,030e) VR=SQ VM=1 Exposure Dose Sequence</summary>
		public const uint ExposureDoseSequence = 0x0040030e;

		/// <summary>(0040,0310) VR=ST VM=1 Comments on Radiation Dose</summary>
		public const uint CommentsOnRadiationDose = 0x00400310;

		/// <summary>(0040,0312) VR=DS VM=1 X-Ray Output</summary>
		public const uint XRayOutput = 0x00400312;

		/// <summary>(0040,0314) VR=DS VM=1 Half Value Layer</summary>
		public const uint HalfValueLayer = 0x00400314;

		/// <summary>(0040,0316) VR=DS VM=1 Organ Dose</summary>
		public const uint OrganDose = 0x00400316;

		/// <summary>(0040,0318) VR=CS VM=1 Organ Exposed</summary>
		public const uint OrganExposed = 0x00400318;

		/// <summary>(0040,0320) VR=SQ VM=1 Billing Procedure Step Sequence</summary>
		public const uint BillingProcedureStepSequence = 0x00400320;

		/// <summary>(0040,0321) VR=SQ VM=1 Film Consumption Sequence</summary>
		public const uint FilmConsumptionSequence = 0x00400321;

		/// <summary>(0040,0324) VR=SQ VM=1 Billing Supplies and Devices Sequence</summary>
		public const uint BillingSuppliesAndDevicesSequence = 0x00400324;

		/// <summary>(0040,0340) VR=SQ VM=1 Performed Series Sequence</summary>
		public const uint PerformedSeriesSequence = 0x00400340;

		/// <summary>(0040,0400) VR=LT VM=1 Comments on the Scheduled Procedure Step</summary>
		public const uint CommentsOnTheScheduledProcedureStep = 0x00400400;

		/// <summary>(0040,0440) VR=SQ VM=1 Protocol Context Sequence</summary>
		public const uint ProtocolContextSequence = 0x00400440;

		/// <summary>(0040,0441) VR=SQ VM=1 Content Item Modifier Sequence</summary>
		public const uint ContentItemModifierSequence = 0x00400441;

		/// <summary>(0040,050a) VR=LO VM=1 Specimen Accession Number</summary>
		public const uint SpecimenAccessionNumber = 0x0040050a;

		/// <summary>(0040,0550) VR=SQ VM=1 Specimen Sequence</summary>
		public const uint SpecimenSequence = 0x00400550;

		/// <summary>(0040,0551) VR=LO VM=1 Specimen Identifier</summary>
		public const uint SpecimenIdentifier = 0x00400551;

		/// <summary>(0040,0555) VR=SQ VM=1 Acquisition Context Sequence</summary>
		public const uint AcquisitionContextSequence = 0x00400555;

		/// <summary>(0040,0556) VR=ST VM=1 Acquisition Context Description</summary>
		public const uint AcquisitionContextDescription = 0x00400556;

		/// <summary>(0040,059a) VR=SQ VM=1 Specimen Type Code Sequence</summary>
		public const uint SpecimenTypeCodeSequence = 0x0040059a;

		/// <summary>(0040,06fa) VR=LO VM=1 Slide Identifier</summary>
		public const uint SlideIdentifier = 0x004006fa;

		/// <summary>(0040,071a) VR=SQ VM=1 Image Center Point Coordinates Sequence</summary>
		public const uint ImageCenterPointCoordinatesSequence = 0x0040071a;

		/// <summary>(0040,072a) VR=DS VM=1 X offset in Slide Coordinate System</summary>
		public const uint XOffsetInSlideCoordinateSystem = 0x0040072a;

		/// <summary>(0040,073a) VR=DS VM=1 Y offset in Slide Coordinate System</summary>
		public const uint YOffsetInSlideCoordinateSystem = 0x0040073a;

		/// <summary>(0040,074a) VR=DS VM=1 Z offset in Slide Coordinate System</summary>
		public const uint ZOffsetInSlideCoordinateSystem = 0x0040074a;

		/// <summary>(0040,08d8) VR=SQ VM=1 Pixel Spacing Sequence</summary>
		public const uint PixelSpacingSequence = 0x004008d8;

		/// <summary>(0040,08da) VR=SQ VM=1 Coordinate System Axis Code Sequence</summary>
		public const uint CoordinateSystemAxisCodeSequence = 0x004008da;

		/// <summary>(0040,08ea) VR=SQ VM=1 Measurement Units Code Sequence</summary>
		public const uint MeasurementUnitsCodeSequence = 0x004008ea;

		/// <summary>(0040,1001) VR=SH VM=1 Requested Procedure ID</summary>
		public const uint RequestedProcedureID = 0x00401001;

		/// <summary>(0040,1002) VR=LO VM=1 Reason for the Requested Procedure</summary>
		public const uint ReasonForTheRequestedProcedure = 0x00401002;

		/// <summary>(0040,1003) VR=SH VM=1 Requested Procedure Priority</summary>
		public const uint RequestedProcedurePriority = 0x00401003;

		/// <summary>(0040,1004) VR=LO VM=1 Patient Transport Arrangements</summary>
		public const uint PatientTransportArrangements = 0x00401004;

		/// <summary>(0040,1005) VR=LO VM=1 Requested Procedure Location</summary>
		public const uint RequestedProcedureLocation = 0x00401005;

		/// <summary>(0040,1008) VR=LO VM=1 Confidentiality Code</summary>
		public const uint ConfidentialityCode = 0x00401008;

		/// <summary>(0040,1009) VR=SH VM=1 Reporting Priority</summary>
		public const uint ReportingPriority = 0x00401009;

		/// <summary>(0040,100a) VR=SQ VM=1 Reason for Requested Procedure Code Sequence</summary>
		public const uint ReasonForRequestedProcedureCodeSequence = 0x0040100a;

		/// <summary>(0040,1010) VR=PN VM=1-n Names of Intended Recipients of Results</summary>
		public const uint NamesOfIntendedRecipientsOfResults = 0x00401010;

		/// <summary>(0040,1011) VR=SQ VM=1 Intended Recipients of Results Identification Sequence</summary>
		public const uint IntendedRecipientsOfResultsIdentificationSequence = 0x00401011;

		/// <summary>(0040,1101) VR=SQ VM=1 Person Identification Code Sequence</summary>
		public const uint PersonIdentificationCodeSequence = 0x00401101;

		/// <summary>(0040,1102) VR=ST VM=1 Person's Address</summary>
		public const uint PersonsAddress = 0x00401102;

		/// <summary>(0040,1103) VR=LO VM=1-n Person's Telephone Numbers</summary>
		public const uint PersonsTelephoneNumbers = 0x00401103;

		/// <summary>(0040,1400) VR=LT VM=1 Requested Procedure Comments</summary>
		public const uint RequestedProcedureComments = 0x00401400;

		/// <summary>(0040,2004) VR=DA VM=1 Issue Date of Imaging Service Request</summary>
		public const uint IssueDateOfImagingServiceRequest = 0x00402004;

		/// <summary>(0040,2005) VR=TM VM=1 Issue Time of Imaging Service Request</summary>
		public const uint IssueTimeOfImagingServiceRequest = 0x00402005;

		/// <summary>(0040,2008) VR=PN VM=1 Order Entered By</summary>
		public const uint OrderEnteredBy = 0x00402008;

		/// <summary>(0040,2009) VR=SH VM=1 Order Enterer's Location</summary>
		public const uint OrderEnterersLocation = 0x00402009;

		/// <summary>(0040,2010) VR=SH VM=1 Order Callback Phone Number</summary>
		public const uint OrderCallbackPhoneNumber = 0x00402010;

		/// <summary>(0040,2016) VR=LO VM=1 Placer Order Number / Imaging Service Request</summary>
		public const uint PlacerOrderNumberImagingServiceRequest = 0x00402016;

		/// <summary>(0040,2017) VR=LO VM=1 Filler Order Number / Imaging Service Request</summary>
		public const uint FillerOrderNumberImagingServiceRequest = 0x00402017;

		/// <summary>(0040,2400) VR=LT VM=1 Imaging Service Request Comments</summary>
		public const uint ImagingServiceRequestComments = 0x00402400;

		/// <summary>(0040,3001) VR=LO VM=1 Confidentiality Constraint on Patient Data Description</summary>
		public const uint ConfidentialityConstraintOnPatientDataDescription = 0x00403001;

		/// <summary>(0040,4001) VR=CS VM=1 General Purpose Scheduled Procedure Step Status</summary>
		public const uint GeneralPurposeScheduledProcedureStepStatus = 0x00404001;

		/// <summary>(0040,4002) VR=CS VM=1 General Purpose Performed Procedure Step Status</summary>
		public const uint GeneralPurposePerformedProcedureStepStatus = 0x00404002;

		/// <summary>(0040,4003) VR=CS VM=1 General Purpose Scheduled Procedure Step Priority</summary>
		public const uint GeneralPurposeScheduledProcedureStepPriority = 0x00404003;

		/// <summary>(0040,4004) VR=SQ VM=1 Scheduled Processing Applications Code Sequence</summary>
		public const uint ScheduledProcessingApplicationsCodeSequence = 0x00404004;

		/// <summary>(0040,4005) VR=DT VM=1 Scheduled Procedure Step Start Date and Time</summary>
		public const uint ScheduledProcedureStepStartDateAndTime = 0x00404005;

		/// <summary>(0040,4006) VR=CS VM=1 Multiple Copies Flag</summary>
		public const uint MultipleCopiesFlag = 0x00404006;

		/// <summary>(0040,4007) VR=SQ VM=1 Performed Processing Applications Code Sequence</summary>
		public const uint PerformedProcessingApplicationsCodeSequence = 0x00404007;

		/// <summary>(0040,4009) VR=SQ VM=1 Human Performer Code Sequence</summary>
		public const uint HumanPerformerCodeSequence = 0x00404009;

		/// <summary>(0040,4010) VR=DT VM=1 Scheduled Procedure Step Modification Date and Time</summary>
		public const uint ScheduledProcedureStepModificationDateAndTime = 0x00404010;

		/// <summary>(0040,4011) VR=DT VM=1 Expected Completion Date and Time</summary>
		public const uint ExpectedCompletionDateAndTime = 0x00404011;

		/// <summary>(0040,4015) VR=SQ VM=1 Resulting General Purpose Performed Procedure Steps Sequence</summary>
		public const uint ResultingGeneralPurposePerformedProcedureStepsSequence = 0x00404015;

		/// <summary>(0040,4016) VR=SQ VM=1 Referenced General Purpose Scheduled Procedure Step Sequence</summary>
		public const uint ReferencedGeneralPurposeScheduledProcedureStepSequence = 0x00404016;

		/// <summary>(0040,4018) VR=SQ VM=1 Scheduled Workitem Code Sequence</summary>
		public const uint ScheduledWorkitemCodeSequence = 0x00404018;

		/// <summary>(0040,4019) VR=SQ VM=1 Performed Workitem Code Sequence</summary>
		public const uint PerformedWorkitemCodeSequence = 0x00404019;

		/// <summary>(0040,4020) VR=CS VM=1 Input Availability Flag</summary>
		public const uint InputAvailabilityFlag = 0x00404020;

		/// <summary>(0040,4021) VR=SQ VM=1 Input Information Sequence</summary>
		public const uint InputInformationSequence = 0x00404021;

		/// <summary>(0040,4022) VR=SQ VM=1 Relevant Information Sequence</summary>
		public const uint RelevantInformationSequence = 0x00404022;

		/// <summary>(0040,4023) VR=UI VM=1 Referenced General Purpose Scheduled Procedure Step Transaction UID</summary>
		public const uint ReferencedGeneralPurposeScheduledProcedureStepTransactionUID = 0x00404023;

		/// <summary>(0040,4025) VR=SQ VM=1 Scheduled Station Name Code Sequence</summary>
		public const uint ScheduledStationNameCodeSequence = 0x00404025;

		/// <summary>(0040,4026) VR=SQ VM=1 Scheduled Station Class Code Sequence</summary>
		public const uint ScheduledStationClassCodeSequence = 0x00404026;

		/// <summary>(0040,4027) VR=SQ VM=1 Scheduled Station Geographic Location Code Sequence</summary>
		public const uint ScheduledStationGeographicLocationCodeSequence = 0x00404027;

		/// <summary>(0040,4028) VR=SQ VM=1 Performed Station Name Code Sequence</summary>
		public const uint PerformedStationNameCodeSequence = 0x00404028;

		/// <summary>(0040,4029) VR=SQ VM=1 Performed Station Class Code Sequence</summary>
		public const uint PerformedStationClassCodeSequence = 0x00404029;

		/// <summary>(0040,4030) VR=SQ VM=1 Performed Station Geographic Location Code Sequence</summary>
		public const uint PerformedStationGeographicLocationCodeSequence = 0x00404030;

		/// <summary>(0040,4031) VR=SQ VM=1 Requested Subsequent Workitem Code Sequence</summary>
		public const uint RequestedSubsequentWorkitemCodeSequence = 0x00404031;

		/// <summary>(0040,4032) VR=SQ VM=1 Non-DICOM Output Code Sequence</summary>
		public const uint NonDICOMOutputCodeSequence = 0x00404032;

		/// <summary>(0040,4033) VR=SQ VM=1 Output Information Sequence</summary>
		public const uint OutputInformationSequence = 0x00404033;

		/// <summary>(0040,4034) VR=SQ VM=1 Scheduled Human Performers Sequence</summary>
		public const uint ScheduledHumanPerformersSequence = 0x00404034;

		/// <summary>(0040,4035) VR=SQ VM=1 Actual Human Performers Sequence</summary>
		public const uint ActualHumanPerformersSequence = 0x00404035;

		/// <summary>(0040,4036) VR=LO VM=1 Human Performer's Organization</summary>
		public const uint HumanPerformersOrganization = 0x00404036;

		/// <summary>(0040,4037) VR=PN VM=1 Human Performer's Name</summary>
		public const uint HumanPerformersName = 0x00404037;

		/// <summary>(0040,8302) VR=DS VM=1 Entrance Dose in mGy</summary>
		public const uint EntranceDoseInMGy = 0x00408302;

		/// <summary>(0040,9094) VR=SQ VM=1 Referenced Image Real World Value Mapping Sequence</summary>
		public const uint ReferencedImageRealWorldValueMappingSequence = 0x00409094;

		/// <summary>(0040,9096) VR=SQ VM=1 Real World Value Mapping Sequence</summary>
		public const uint RealWorldValueMappingSequence = 0x00409096;

		/// <summary>(0040,9098) VR=SQ VM=1 Pixel Value Mapping Code Sequence</summary>
		public const uint PixelValueMappingCodeSequence = 0x00409098;

		/// <summary>(0040,9210) VR=SH VM=1 LUT Label</summary>
		public const uint LUTLabel = 0x00409210;

		/// <summary>(0040,9211) VR=US/SS VM=1 Real World Value Last Value Mapped</summary>
		public const uint RealWorldValueLastValueMapped = 0x00409211;

		/// <summary>(0040,9212) VR=FD VM=1-n Real World Value LUT Data</summary>
		public const uint RealWorldValueLUTData = 0x00409212;

		/// <summary>(0040,9216) VR=US/SS VM=1 Real World Value First Value Mapped</summary>
		public const uint RealWorldValueFirstValueMapped = 0x00409216;

		/// <summary>(0040,9224) VR=FD VM=1 Real World Value Intercept</summary>
		public const uint RealWorldValueIntercept = 0x00409224;

		/// <summary>(0040,9225) VR=FD VM=1 Real World Value Slope</summary>
		public const uint RealWorldValueSlope = 0x00409225;

		/// <summary>(0040,a010) VR=CS VM=1 Relationship Type</summary>
		public const uint RelationshipType = 0x0040a010;

		/// <summary>(0040,a027) VR=LO VM=1 Verifying Organization</summary>
		public const uint VerifyingOrganization = 0x0040a027;

		/// <summary>(0040,a030) VR=DT VM=1 Verification Date Time</summary>
		public const uint VerificationDateTime = 0x0040a030;

		/// <summary>(0040,a032) VR=DT VM=1 Observation Date Time</summary>
		public const uint ObservationDateTime = 0x0040a032;

		/// <summary>(0040,a040) VR=CS VM=1 Value Type</summary>
		public const uint ValueType = 0x0040a040;

		/// <summary>(0040,a043) VR=SQ VM=1 Concept Name Code Sequence</summary>
		public const uint ConceptNameCodeSequence = 0x0040a043;

		/// <summary>(0040,a050) VR=CS VM=1 Continuity Of Content</summary>
		public const uint ContinuityOfContent = 0x0040a050;

		/// <summary>(0040,a073) VR=SQ VM=1 Verifying Observer Sequence</summary>
		public const uint VerifyingObserverSequence = 0x0040a073;

		/// <summary>(0040,a075) VR=PN VM=1 Verifying Observer Name</summary>
		public const uint VerifyingObserverName = 0x0040a075;

		/// <summary>(0040,a078) VR=SQ VM=1 Author Observer Sequence</summary>
		public const uint AuthorObserverSequence = 0x0040a078;

		/// <summary>(0040,a07a) VR=SQ VM=1 Participant Sequence</summary>
		public const uint ParticipantSequence = 0x0040a07a;

		/// <summary>(0040,a07c) VR=SQ VM=1 Custodial Organization Sequence</summary>
		public const uint CustodialOrganizationSequence = 0x0040a07c;

		/// <summary>(0040,a080) VR=CS VM=1 Participation Type</summary>
		public const uint ParticipationType = 0x0040a080;

		/// <summary>(0040,a082) VR=DT VM=1 Participation DateTime</summary>
		public const uint ParticipationDateTime = 0x0040a082;

		/// <summary>(0040,a084) VR=CS VM=1 Observer Type</summary>
		public const uint ObserverType = 0x0040a084;

		/// <summary>(0040,a088) VR=SQ VM=1 Verifying Observer Identification Code Sequence</summary>
		public const uint VerifyingObserverIdentificationCodeSequence = 0x0040a088;

		/// <summary>(0040,a0b0) VR=US VM=2-2n Referenced Waveform Channels</summary>
		public const uint ReferencedWaveformChannels = 0x0040a0b0;

		/// <summary>(0040,a120) VR=DT VM=1 DateTime</summary>
		public const uint DateTime = 0x0040a120;

		/// <summary>(0040,a121) VR=DA VM=1 Date</summary>
		public const uint Date = 0x0040a121;

		/// <summary>(0040,a122) VR=TM VM=1 Time</summary>
		public const uint Time = 0x0040a122;

		/// <summary>(0040,a123) VR=PN VM=1 Person Name</summary>
		public const uint PersonName = 0x0040a123;

		/// <summary>(0040,a124) VR=UI VM=1 UID</summary>
		public const uint UID = 0x0040a124;

		/// <summary>(0040,a130) VR=CS VM=1 Temporal Range Type</summary>
		public const uint TemporalRangeType = 0x0040a130;

		/// <summary>(0040,a132) VR=UL VM=1-n Referenced Sample Positions</summary>
		public const uint ReferencedSamplePositions = 0x0040a132;

		/// <summary>(0040,a136) VR=US VM=1-n Referenced Frame Numbers</summary>
		public const uint ReferencedFrameNumbers = 0x0040a136;

		/// <summary>(0040,a138) VR=DS VM=1-n Referenced Time Offsets</summary>
		public const uint ReferencedTimeOffsets = 0x0040a138;

		/// <summary>(0040,a13a) VR=DT VM=1-n Referenced DateTime</summary>
		public const uint ReferencedDateTime = 0x0040a13a;

		/// <summary>(0040,a160) VR=UT VM=1 Text Value</summary>
		public const uint TextValue = 0x0040a160;

		/// <summary>(0040,a168) VR=SQ VM=1 Concept Code Sequence</summary>
		public const uint ConceptCodeSequence = 0x0040a168;

		/// <summary>(0040,a170) VR=SQ VM=1 Purpose of Reference Code Sequence</summary>
		public const uint PurposeOfReferenceCodeSequence = 0x0040a170;

		/// <summary>(0040,a180) VR=US VM=1 Annotation Group Number</summary>
		public const uint AnnotationGroupNumber = 0x0040a180;

		/// <summary>(0040,a195) VR=SQ VM=1 Modifier Code Sequence</summary>
		public const uint ModifierCodeSequence = 0x0040a195;

		/// <summary>(0040,a300) VR=SQ VM=1 Measured Value Sequence</summary>
		public const uint MeasuredValueSequence = 0x0040a300;

		/// <summary>(0040,a301) VR=SQ VM=1 Numeric Value Qualifier Code Sequence</summary>
		public const uint NumericValueQualifierCodeSequence = 0x0040a301;

		/// <summary>(0040,a30a) VR=DS VM=1-n Numeric Value</summary>
		public const uint NumericValue = 0x0040a30a;

		/// <summary>(0040,a360) VR=SQ VM=1 Predecessor Documents Sequence</summary>
		public const uint PredecessorDocumentsSequence = 0x0040a360;

		/// <summary>(0040,a370) VR=SQ VM=1 Referenced Request Sequence</summary>
		public const uint ReferencedRequestSequence = 0x0040a370;

		/// <summary>(0040,a372) VR=SQ VM=1 Performed Procedure Code Sequence</summary>
		public const uint PerformedProcedureCodeSequence = 0x0040a372;

		/// <summary>(0040,a375) VR=SQ VM=1 Current Requested Procedure Evidence Sequence</summary>
		public const uint CurrentRequestedProcedureEvidenceSequence = 0x0040a375;

		/// <summary>(0040,a385) VR=SQ VM=1 Pertinent Other Evidence Sequence</summary>
		public const uint PertinentOtherEvidenceSequence = 0x0040a385;

		/// <summary>(0040,a390) VR=SQ VM=1 HL7 Structured Document Reference Sequence</summary>
		public const uint HL7StructuredDocumentReferenceSequence = 0x0040a390;

		/// <summary>(0040,a491) VR=CS VM=1 Completion Flag</summary>
		public const uint CompletionFlag = 0x0040a491;

		/// <summary>(0040,a492) VR=LO VM=1 Completion Flag Description</summary>
		public const uint CompletionFlagDescription = 0x0040a492;

		/// <summary>(0040,a493) VR=CS VM=1 Verification Flag</summary>
		public const uint VerificationFlag = 0x0040a493;

		/// <summary>(0040,a494) VR=CS VM=1 Archive Requested</summary>
		public const uint ArchiveRequested = 0x0040a494;

		/// <summary>(0040,a504) VR=SQ VM=1 Content Template Sequence</summary>
		public const uint ContentTemplateSequence = 0x0040a504;

		/// <summary>(0040,a525) VR=SQ VM=1 Identical Documents Sequence</summary>
		public const uint IdenticalDocumentsSequence = 0x0040a525;

		/// <summary>(0040,a730) VR=SQ VM=1 Content Sequence</summary>
		public const uint ContentSequence = 0x0040a730;

		/// <summary>(0040,b020) VR=SQ VM=1 Annotation Sequence</summary>
		public const uint AnnotationSequence = 0x0040b020;

		/// <summary>(0040,db00) VR=CS VM=1 Template Identifier</summary>
		public const uint TemplateIdentifier = 0x0040db00;

		/// <summary>(0040,db73) VR=UL VM=1-n Referenced Content Item Identifier</summary>
		public const uint ReferencedContentItemIdentifier = 0x0040db73;

		/// <summary>(0040,e001) VR=ST VM=1 HL7 Instance Identifier</summary>
		public const uint HL7InstanceIdentifier = 0x0040e001;

		/// <summary>(0040,e004) VR=DT VM=1 HL7 Document Effective Time</summary>
		public const uint HL7DocumentEffectiveTime = 0x0040e004;

		/// <summary>(0040,e006) VR=SQ VM=1 HL7 Document Type Code Sequence</summary>
		public const uint HL7DocumentTypeCodeSequence = 0x0040e006;

		/// <summary>(0040,e010) VR=UT VM=1 Retrieve URI</summary>
		public const uint RetrieveURI = 0x0040e010;

		/// <summary>(0042,0010) VR=ST VM=1 Document Title</summary>
		public const uint DocumentTitle = 0x00420010;

		/// <summary>(0042,0011) VR=OB VM=1 Encapsulated Document</summary>
		public const uint EncapsulatedDocument = 0x00420011;

		/// <summary>(0042,0012) VR=LO VM=1 MIME Type of Encapsulated Document</summary>
		public const uint MIMETypeOfEncapsulatedDocument = 0x00420012;

		/// <summary>(0042,0013) VR=SQ VM=1 Source Instance Sequence</summary>
		public const uint SourceInstanceSequence = 0x00420013;

		/// <summary>(0042,0014) VR=LO VM=1-n List of MIME Types</summary>
		public const uint ListOfMIMETypes = 0x00420014;

		/// <summary>(0044,0001) VR=ST VM=1 Product Package Identifier</summary>
		public const uint ProductPackageIdentifier = 0x00440001;

		/// <summary>(0044,0002) VR=CS VM=1 Substance Administration Approval</summary>
		public const uint SubstanceAdministrationApproval = 0x00440002;

		/// <summary>(0044,0003) VR=LT VM=1 Approval Status Further Description</summary>
		public const uint ApprovalStatusFurtherDescription = 0x00440003;

		/// <summary>(0044,0004) VR=DT VM=1 Approval Status DateTime</summary>
		public const uint ApprovalStatusDateTime = 0x00440004;

		/// <summary>(0044,0007) VR=SQ VM=1 Product Type Code Sequence</summary>
		public const uint ProductTypeCodeSequence = 0x00440007;

		/// <summary>(0044,0008) VR=LO VM=1-n Product Name</summary>
		public const uint ProductName = 0x00440008;

		/// <summary>(0044,0009) VR=LT VM=1 Product Description</summary>
		public const uint ProductDescription = 0x00440009;

		/// <summary>(0044,000a) VR=LO VM=1 Product Lot Identifier</summary>
		public const uint ProductLotIdentifier = 0x0044000a;

		/// <summary>(0044,000b) VR=DT VM=1 Product Expiration DateTime</summary>
		public const uint ProductExpirationDateTime = 0x0044000b;

		/// <summary>(0044,0010) VR=DT VM=1 Substance Administration DateTime</summary>
		public const uint SubstanceAdministrationDateTime = 0x00440010;

		/// <summary>(0044,0011) VR=LO VM=1 Substance Administration Notes</summary>
		public const uint SubstanceAdministrationNotes = 0x00440011;

		/// <summary>(0044,0012) VR=LO VM=1 Substance Administration Device ID</summary>
		public const uint SubstanceAdministrationDeviceID = 0x00440012;

		/// <summary>(0044,0013) VR=SQ VM=1 Product Parameter Sequence</summary>
		public const uint ProductParameterSequence = 0x00440013;

		/// <summary>(0044,0019) VR=SQ VM=1 Substance Administration Parameter Sequence</summary>
		public const uint SubstanceAdministrationParameterSequence = 0x00440019;

		/// <summary>(0050,0004) VR=CS VM=1 Calibration Image</summary>
		public const uint CalibrationImage = 0x00500004;

		/// <summary>(0050,0010) VR=SQ VM=1 Device Sequence</summary>
		public const uint DeviceSequence = 0x00500010;

		/// <summary>(0050,0014) VR=DS VM=1 Device Length</summary>
		public const uint DeviceLength = 0x00500014;

		/// <summary>(0050,0016) VR=DS VM=1 Device Diameter</summary>
		public const uint DeviceDiameter = 0x00500016;

		/// <summary>(0050,0017) VR=CS VM=1 Device Diameter Units</summary>
		public const uint DeviceDiameterUnits = 0x00500017;

		/// <summary>(0050,0018) VR=DS VM=1 Device Volume</summary>
		public const uint DeviceVolume = 0x00500018;

		/// <summary>(0050,0019) VR=DS VM=1 Intermarker Distance</summary>
		public const uint IntermarkerDistance = 0x00500019;

		/// <summary>(0050,0020) VR=LO VM=1 Device Description</summary>
		public const uint DeviceDescription = 0x00500020;

		/// <summary>(0054,0010) VR=US VM=1-n Energy Window Vector</summary>
		public const uint EnergyWindowVector = 0x00540010;

		/// <summary>(0054,0011) VR=US VM=1 Number of Energy Windows</summary>
		public const uint NumberOfEnergyWindows = 0x00540011;

		/// <summary>(0054,0012) VR=SQ VM=1 Energy Window Information Sequence</summary>
		public const uint EnergyWindowInformationSequence = 0x00540012;

		/// <summary>(0054,0013) VR=SQ VM=1 Energy Window Range Sequence</summary>
		public const uint EnergyWindowRangeSequence = 0x00540013;

		/// <summary>(0054,0014) VR=DS VM=1 Energy Window Lower Limit</summary>
		public const uint EnergyWindowLowerLimit = 0x00540014;

		/// <summary>(0054,0015) VR=DS VM=1 Energy Window Upper Limit</summary>
		public const uint EnergyWindowUpperLimit = 0x00540015;

		/// <summary>(0054,0016) VR=SQ VM=1 Radiopharmaceutical Information Sequence</summary>
		public const uint RadiopharmaceuticalInformationSequence = 0x00540016;

		/// <summary>(0054,0017) VR=IS VM=1 Residual Syringe Counts</summary>
		public const uint ResidualSyringeCounts = 0x00540017;

		/// <summary>(0054,0018) VR=SH VM=1 Energy Window Name</summary>
		public const uint EnergyWindowName = 0x00540018;

		/// <summary>(0054,0020) VR=US VM=1-n Detector Vector</summary>
		public const uint DetectorVector = 0x00540020;

		/// <summary>(0054,0021) VR=US VM=1 Number of Detectors</summary>
		public const uint NumberOfDetectors = 0x00540021;

		/// <summary>(0054,0022) VR=SQ VM=1 Detector Information Sequence</summary>
		public const uint DetectorInformationSequence = 0x00540022;

		/// <summary>(0054,0030) VR=US VM=1-n Phase Vector</summary>
		public const uint PhaseVector = 0x00540030;

		/// <summary>(0054,0031) VR=US VM=1 Number of Phases</summary>
		public const uint NumberOfPhases = 0x00540031;

		/// <summary>(0054,0032) VR=SQ VM=1 Phase Information Sequence</summary>
		public const uint PhaseInformationSequence = 0x00540032;

		/// <summary>(0054,0033) VR=US VM=1 Number of Frames in Phase</summary>
		public const uint NumberOfFramesInPhase = 0x00540033;

		/// <summary>(0054,0036) VR=IS VM=1 Phase Delay</summary>
		public const uint PhaseDelay = 0x00540036;

		/// <summary>(0054,0038) VR=IS VM=1 Pause Between Frames</summary>
		public const uint PauseBetweenFrames = 0x00540038;

		/// <summary>(0054,0039) VR=CS VM=1 Phase Description</summary>
		public const uint PhaseDescription = 0x00540039;

		/// <summary>(0054,0050) VR=US VM=1-n Rotation Vector</summary>
		public const uint RotationVector = 0x00540050;

		/// <summary>(0054,0051) VR=US VM=1 Number of Rotations</summary>
		public const uint NumberOfRotations = 0x00540051;

		/// <summary>(0054,0052) VR=SQ VM=1 Rotation Information Sequence</summary>
		public const uint RotationInformationSequence = 0x00540052;

		/// <summary>(0054,0053) VR=US VM=1 Number of Frames in Rotation</summary>
		public const uint NumberOfFramesInRotation = 0x00540053;

		/// <summary>(0054,0060) VR=US VM=1-n R-R Interval Vector</summary>
		public const uint RRIntervalVector = 0x00540060;

		/// <summary>(0054,0061) VR=US VM=1 Number of R-R Intervals</summary>
		public const uint NumberOfRRIntervals = 0x00540061;

		/// <summary>(0054,0062) VR=SQ VM=1 Gated Information Sequence</summary>
		public const uint GatedInformationSequence = 0x00540062;

		/// <summary>(0054,0063) VR=SQ VM=1 Data Information Sequence</summary>
		public const uint DataInformationSequence = 0x00540063;

		/// <summary>(0054,0070) VR=US VM=1-n Time Slot Vector</summary>
		public const uint TimeSlotVector = 0x00540070;

		/// <summary>(0054,0071) VR=US VM=1 Number of Time Slots</summary>
		public const uint NumberOfTimeSlots = 0x00540071;

		/// <summary>(0054,0072) VR=SQ VM=1 Time Slot Information Sequence</summary>
		public const uint TimeSlotInformationSequence = 0x00540072;

		/// <summary>(0054,0073) VR=DS VM=1 Time Slot Time</summary>
		public const uint TimeSlotTime = 0x00540073;

		/// <summary>(0054,0080) VR=US VM=1-n Slice Vector</summary>
		public const uint SliceVector = 0x00540080;

		/// <summary>(0054,0081) VR=US VM=1 Number of Slices</summary>
		public const uint NumberOfSlices = 0x00540081;

		/// <summary>(0054,0090) VR=US VM=1-n Angular View Vector</summary>
		public const uint AngularViewVector = 0x00540090;

		/// <summary>(0054,0100) VR=US VM=1-n Time Slice Vector</summary>
		public const uint TimeSliceVector = 0x00540100;

		/// <summary>(0054,0101) VR=US VM=1 Number of Time Slices</summary>
		public const uint NumberOfTimeSlices = 0x00540101;

		/// <summary>(0054,0200) VR=DS VM=1 Start Angle</summary>
		public const uint StartAngle = 0x00540200;

		/// <summary>(0054,0202) VR=CS VM=1 Type of Detector Motion</summary>
		public const uint TypeOfDetectorMotion = 0x00540202;

		/// <summary>(0054,0210) VR=IS VM=1-n Trigger Vector</summary>
		public const uint TriggerVector = 0x00540210;

		/// <summary>(0054,0211) VR=US VM=1 Number of Triggers in Phase</summary>
		public const uint NumberOfTriggersInPhase = 0x00540211;

		/// <summary>(0054,0220) VR=SQ VM=1 View Code Sequence</summary>
		public const uint ViewCodeSequence = 0x00540220;

		/// <summary>(0054,0222) VR=SQ VM=1 View Modifier Code Sequence</summary>
		public const uint ViewModifierCodeSequence = 0x00540222;

		/// <summary>(0054,0300) VR=SQ VM=1 Radionuclide Code Sequence</summary>
		public const uint RadionuclideCodeSequence = 0x00540300;

		/// <summary>(0054,0302) VR=SQ VM=1 Administration Route Code Sequence</summary>
		public const uint AdministrationRouteCodeSequence = 0x00540302;

		/// <summary>(0054,0304) VR=SQ VM=1 Radiopharmaceutical Code Sequence</summary>
		public const uint RadiopharmaceuticalCodeSequence = 0x00540304;

		/// <summary>(0054,0306) VR=SQ VM=1 Calibration Data Sequence</summary>
		public const uint CalibrationDataSequence = 0x00540306;

		/// <summary>(0054,0308) VR=US VM=1 Energy Window Number</summary>
		public const uint EnergyWindowNumber = 0x00540308;

		/// <summary>(0054,0400) VR=SH VM=1 Image ID</summary>
		public const uint ImageID = 0x00540400;

		/// <summary>(0054,0410) VR=SQ VM=1 Patient Orientation Code Sequence</summary>
		public const uint PatientOrientationCodeSequence = 0x00540410;

		/// <summary>(0054,0412) VR=SQ VM=1 Patient Orientation Modifier Code Sequence</summary>
		public const uint PatientOrientationModifierCodeSequence = 0x00540412;

		/// <summary>(0054,0414) VR=SQ VM=1 Patient Gantry Relationship Code Sequence</summary>
		public const uint PatientGantryRelationshipCodeSequence = 0x00540414;

		/// <summary>(0054,0500) VR=CS VM=1 Slice Progression Direction</summary>
		public const uint SliceProgressionDirection = 0x00540500;

		/// <summary>(0054,1000) VR=CS VM=2 Series Type</summary>
		public const uint SeriesType = 0x00541000;

		/// <summary>(0054,1001) VR=CS VM=1 Units</summary>
		public const uint Units = 0x00541001;

		/// <summary>(0054,1002) VR=CS VM=1 Counts Source</summary>
		public const uint CountsSource = 0x00541002;

		/// <summary>(0054,1004) VR=CS VM=1 Reprojection Method</summary>
		public const uint ReprojectionMethod = 0x00541004;

		/// <summary>(0054,1100) VR=CS VM=1 Randoms Correction Method</summary>
		public const uint RandomsCorrectionMethod = 0x00541100;

		/// <summary>(0054,1101) VR=LO VM=1 Attenuation Correction Method</summary>
		public const uint AttenuationCorrectionMethod = 0x00541101;

		/// <summary>(0054,1102) VR=CS VM=1 Decay Correction</summary>
		public const uint DecayCorrection = 0x00541102;

		/// <summary>(0054,1103) VR=LO VM=1 Reconstruction Method</summary>
		public const uint ReconstructionMethod = 0x00541103;

		/// <summary>(0054,1104) VR=LO VM=1 Detector Lines of Response Used</summary>
		public const uint DetectorLinesOfResponseUsed = 0x00541104;

		/// <summary>(0054,1105) VR=LO VM=1 Scatter Correction Method</summary>
		public const uint ScatterCorrectionMethod = 0x00541105;

		/// <summary>(0054,1200) VR=DS VM=1 Axial Acceptance</summary>
		public const uint AxialAcceptance = 0x00541200;

		/// <summary>(0054,1201) VR=IS VM=2 Axial Mash</summary>
		public const uint AxialMash = 0x00541201;

		/// <summary>(0054,1202) VR=IS VM=1 Transverse Mash</summary>
		public const uint TransverseMash = 0x00541202;

		/// <summary>(0054,1203) VR=DS VM=2 Detector Element Size</summary>
		public const uint DetectorElementSize = 0x00541203;

		/// <summary>(0054,1210) VR=DS VM=1 Coincidence Window Width</summary>
		public const uint CoincidenceWindowWidth = 0x00541210;

		/// <summary>(0054,1220) VR=CS VM=1-n Secondary Counts Type</summary>
		public const uint SecondaryCountsType = 0x00541220;

		/// <summary>(0054,1300) VR=DS VM=1 Frame Reference Time</summary>
		public const uint FrameReferenceTime = 0x00541300;

		/// <summary>(0054,1310) VR=IS VM=1 Primary (Prompts) Counts Accumulated</summary>
		public const uint PrimaryPromptsCountsAccumulated = 0x00541310;

		/// <summary>(0054,1311) VR=IS VM=1-n Secondary Counts Accumulated</summary>
		public const uint SecondaryCountsAccumulated = 0x00541311;

		/// <summary>(0054,1320) VR=DS VM=1 Slice Sensitivity Factor</summary>
		public const uint SliceSensitivityFactor = 0x00541320;

		/// <summary>(0054,1321) VR=DS VM=1 Decay Factor</summary>
		public const uint DecayFactor = 0x00541321;

		/// <summary>(0054,1322) VR=DS VM=1 Dose Calibration Factor</summary>
		public const uint DoseCalibrationFactor = 0x00541322;

		/// <summary>(0054,1323) VR=DS VM=1 Scatter Fraction Factor</summary>
		public const uint ScatterFractionFactor = 0x00541323;

		/// <summary>(0054,1324) VR=DS VM=1 Dead Time Factor</summary>
		public const uint DeadTimeFactor = 0x00541324;

		/// <summary>(0054,1330) VR=US VM=1 Image Index</summary>
		public const uint ImageIndex = 0x00541330;

		/// <summary>(0060,3000) VR=SQ VM=1 Histogram Sequence</summary>
		public const uint HistogramSequence = 0x00603000;

		/// <summary>(0060,3002) VR=US VM=1 Histogram Number of Bins</summary>
		public const uint HistogramNumberOfBins = 0x00603002;

		/// <summary>(0060,3004) VR=US/SS VM=1 Histogram First Bin Value</summary>
		public const uint HistogramFirstBinValue = 0x00603004;

		/// <summary>(0060,3006) VR=US/SS VM=1 Histogram Last Bin Value</summary>
		public const uint HistogramLastBinValue = 0x00603006;

		/// <summary>(0060,3008) VR=US VM=1 Histogram Bin Width</summary>
		public const uint HistogramBinWidth = 0x00603008;

		/// <summary>(0060,3010) VR=LO VM=1 Histogram Explanation</summary>
		public const uint HistogramExplanation = 0x00603010;

		/// <summary>(0060,3020) VR=UL VM=1-n Histogram Data</summary>
		public const uint HistogramData = 0x00603020;

		/// <summary>(0062,0001) VR=CS VM=1 Segmentation Type</summary>
		public const uint SegmentationType = 0x00620001;

		/// <summary>(0062,0002) VR=SQ VM=1 Segment Sequence</summary>
		public const uint SegmentSequence = 0x00620002;

		/// <summary>(0062,0003) VR=SQ VM=1 Segmented Property Category Code Sequence</summary>
		public const uint SegmentedPropertyCategoryCodeSequence = 0x00620003;

		/// <summary>(0062,0004) VR=US VM=1 Segment Number</summary>
		public const uint SegmentNumber = 0x00620004;

		/// <summary>(0062,0005) VR=LO VM=1 Segment Label</summary>
		public const uint SegmentLabel = 0x00620005;

		/// <summary>(0062,0006) VR=ST VM=1 Segment Description</summary>
		public const uint SegmentDescription = 0x00620006;

		/// <summary>(0062,0008) VR=CS VM=1 Segment Algorithm Type</summary>
		public const uint SegmentAlgorithmType = 0x00620008;

		/// <summary>(0062,0009) VR=LO VM=1 Segment Algorithm Name</summary>
		public const uint SegmentAlgorithmName = 0x00620009;

		/// <summary>(0062,000a) VR=SQ VM=1 Segment Identification Sequence</summary>
		public const uint SegmentIdentificationSequence = 0x0062000a;

		/// <summary>(0062,000b) VR=US VM=1-n Referenced Segment Number</summary>
		public const uint ReferencedSegmentNumber = 0x0062000b;

		/// <summary>(0062,000c) VR=US VM=1 Recommended Display Grayscale Value</summary>
		public const uint RecommendedDisplayGrayscaleValue = 0x0062000c;

		/// <summary>(0062,000d) VR=US VM=3 Recommended Display CIELab Value</summary>
		public const uint RecommendedDisplayCIELabValue = 0x0062000d;

		/// <summary>(0062,000e) VR=US VM=1 Maximum Fractional Value</summary>
		public const uint MaximumFractionalValue = 0x0062000e;

		/// <summary>(0062,000f) VR=SQ VM=1 Segmented Property Type Code Sequence</summary>
		public const uint SegmentedPropertyTypeCodeSequence = 0x0062000f;

		/// <summary>(0062,0010) VR=CS VM=1 Segmentation Fractional Type</summary>
		public const uint SegmentationFractionalType = 0x00620010;

		/// <summary>(0064,0002) VR=SQ VM=1 Deformable Registration Sequence</summary>
		public const uint DeformableRegistrationSequence = 0x00640002;

		/// <summary>(0064,0003) VR=UI VM=1 Source Frame of Reference UID</summary>
		public const uint SourceFrameOfReferenceUID = 0x00640003;

		/// <summary>(0064,0005) VR=SQ VM=1 Deformable Registration Grid Sequence</summary>
		public const uint DeformableRegistrationGridSequence = 0x00640005;

		/// <summary>(0064,0007) VR=UL VM=3 Grid Dimensions</summary>
		public const uint GridDimensions = 0x00640007;

		/// <summary>(0064,0008) VR=FD VM=3 Grid Resolution</summary>
		public const uint GridResolution = 0x00640008;

		/// <summary>(0064,0009) VR=OF VM=1 Vector Grid Data</summary>
		public const uint VectorGridData = 0x00640009;

		/// <summary>(0064,000f) VR=SQ VM=1 Pre Deformation Matrix Registration Sequence</summary>
		public const uint PreDeformationMatrixRegistrationSequence = 0x0064000f;

		/// <summary>(0064,0010) VR=SQ VM=1 Post Deformation Matrix Registration Sequence</summary>
		public const uint PostDeformationMatrixRegistrationSequence = 0x00640010;

		/// <summary>(0070,0001) VR=SQ VM=1 Graphic Annotation Sequence</summary>
		public const uint GraphicAnnotationSequence = 0x00700001;

		/// <summary>(0070,0002) VR=CS VM=1 Graphic Layer</summary>
		public const uint GraphicLayer = 0x00700002;

		/// <summary>(0070,0003) VR=CS VM=1 Bounding Box Annotation Units</summary>
		public const uint BoundingBoxAnnotationUnits = 0x00700003;

		/// <summary>(0070,0004) VR=CS VM=1 Anchor Point Annotation Units</summary>
		public const uint AnchorPointAnnotationUnits = 0x00700004;

		/// <summary>(0070,0005) VR=CS VM=1 Graphic Annotation Units</summary>
		public const uint GraphicAnnotationUnits = 0x00700005;

		/// <summary>(0070,0006) VR=ST VM=1 Unformatted Text Value</summary>
		public const uint UnformattedTextValue = 0x00700006;

		/// <summary>(0070,0008) VR=SQ VM=1 Text Object Sequence</summary>
		public const uint TextObjectSequence = 0x00700008;

		/// <summary>(0070,0009) VR=SQ VM=1 Graphic Object Sequence</summary>
		public const uint GraphicObjectSequence = 0x00700009;

		/// <summary>(0070,0010) VR=FL VM=2 Bounding Box Top Left Hand Corner</summary>
		public const uint BoundingBoxTopLeftHandCorner = 0x00700010;

		/// <summary>(0070,0011) VR=FL VM=2 Bounding Box Bottom Right Hand Corner</summary>
		public const uint BoundingBoxBottomRightHandCorner = 0x00700011;

		/// <summary>(0070,0012) VR=CS VM=1 Bounding Box Text Horizontal Justification</summary>
		public const uint BoundingBoxTextHorizontalJustification = 0x00700012;

		/// <summary>(0070,0014) VR=FL VM=2 Anchor Point</summary>
		public const uint AnchorPoint = 0x00700014;

		/// <summary>(0070,0015) VR=CS VM=1 Anchor Point Visibility</summary>
		public const uint AnchorPointVisibility = 0x00700015;

		/// <summary>(0070,0020) VR=US VM=1 Graphic Dimensions</summary>
		public const uint GraphicDimensions = 0x00700020;

		/// <summary>(0070,0021) VR=US VM=1 Number of Graphic Points</summary>
		public const uint NumberOfGraphicPoints = 0x00700021;

		/// <summary>(0070,0022) VR=FL VM=2-n Graphic Data</summary>
		public const uint GraphicData = 0x00700022;

		/// <summary>(0070,0023) VR=CS VM=1 Graphic Type</summary>
		public const uint GraphicType = 0x00700023;

		/// <summary>(0070,0024) VR=CS VM=1 Graphic Filled</summary>
		public const uint GraphicFilled = 0x00700024;

		/// <summary>(0070,0041) VR=CS VM=1 Image Horizontal Flip</summary>
		public const uint ImageHorizontalFlip = 0x00700041;

		/// <summary>(0070,0042) VR=US VM=1 Image Rotation</summary>
		public const uint ImageRotation = 0x00700042;

		/// <summary>(0070,0052) VR=SL VM=2 Displayed Area Top Left Hand Corner</summary>
		public const uint DisplayedAreaTopLeftHandCorner = 0x00700052;

		/// <summary>(0070,0053) VR=SL VM=2 Displayed Area Bottom Right Hand Corner</summary>
		public const uint DisplayedAreaBottomRightHandCorner = 0x00700053;

		/// <summary>(0070,005a) VR=SQ VM=1 Displayed Area Selection Sequence</summary>
		public const uint DisplayedAreaSelectionSequence = 0x0070005a;

		/// <summary>(0070,0060) VR=SQ VM=1 Graphic Layer Sequence</summary>
		public const uint GraphicLayerSequence = 0x00700060;

		/// <summary>(0070,0062) VR=IS VM=1 Graphic Layer Order</summary>
		public const uint GraphicLayerOrder = 0x00700062;

		/// <summary>(0070,0066) VR=US VM=1 Graphic Layer Recommended Display Grayscale Value</summary>
		public const uint GraphicLayerRecommendedDisplayGrayscaleValue = 0x00700066;

		/// <summary>(0070,0068) VR=LO VM=1 Graphic Layer Description</summary>
		public const uint GraphicLayerDescription = 0x00700068;

		/// <summary>(0070,0080) VR=CS VM=1 Content Label</summary>
		public const uint ContentLabel = 0x00700080;

		/// <summary>(0070,0081) VR=LO VM=1 Content Description</summary>
		public const uint ContentDescription = 0x00700081;

		/// <summary>(0070,0082) VR=DA VM=1 Presentation Creation Date</summary>
		public const uint PresentationCreationDate = 0x00700082;

		/// <summary>(0070,0083) VR=TM VM=1 Presentation Creation Time</summary>
		public const uint PresentationCreationTime = 0x00700083;

		/// <summary>(0070,0084) VR=PN VM=1 Content Creator's Name</summary>
		public const uint ContentCreatorsName = 0x00700084;

		/// <summary>(0070,0086) VR=SQ VM=1 Content Creator's Identification Code Sequence</summary>
		public const uint ContentCreatorsIdentificationCodeSequence = 0x00700086;

		/// <summary>(0070,0100) VR=CS VM=1 Presentation Size Mode</summary>
		public const uint PresentationSizeMode = 0x00700100;

		/// <summary>(0070,0101) VR=DS VM=2 Presentation Pixel Spacing</summary>
		public const uint PresentationPixelSpacing = 0x00700101;

		/// <summary>(0070,0102) VR=IS VM=2 Presentation Pixel Aspect Ratio</summary>
		public const uint PresentationPixelAspectRatio = 0x00700102;

		/// <summary>(0070,0103) VR=FL VM=1 Presentation Pixel Magnification Ratio</summary>
		public const uint PresentationPixelMagnificationRatio = 0x00700103;

		/// <summary>(0070,0306) VR=CS VM=1 Shape Type</summary>
		public const uint ShapeType = 0x00700306;

		/// <summary>(0070,0308) VR=SQ VM=1 Registration Sequence</summary>
		public const uint RegistrationSequence = 0x00700308;

		/// <summary>(0070,0309) VR=SQ VM=1 Matrix Registration Sequence</summary>
		public const uint MatrixRegistrationSequence = 0x00700309;

		/// <summary>(0070,030a) VR=SQ VM=1 Matrix Sequence</summary>
		public const uint MatrixSequence = 0x0070030a;

		/// <summary>(0070,030c) VR=CS VM=1 Frame of Reference Transformation Matrix Type</summary>
		public const uint FrameOfReferenceTransformationMatrixType = 0x0070030c;

		/// <summary>(0070,030d) VR=SQ VM=1 Registration Type Code Sequence</summary>
		public const uint RegistrationTypeCodeSequence = 0x0070030d;

		/// <summary>(0070,030f) VR=ST VM=1 Fiducial Description</summary>
		public const uint FiducialDescription = 0x0070030f;

		/// <summary>(0070,0310) VR=SH VM=1 Fiducial Identifier</summary>
		public const uint FiducialIdentifier = 0x00700310;

		/// <summary>(0070,0311) VR=SQ VM=1 Fiducial Identifier Code Sequence</summary>
		public const uint FiducialIdentifierCodeSequence = 0x00700311;

		/// <summary>(0070,0312) VR=FD VM=1 Contour Uncertainty Radius</summary>
		public const uint ContourUncertaintyRadius = 0x00700312;

		/// <summary>(0070,0314) VR=SQ VM=1 Used Fiducials Sequence</summary>
		public const uint UsedFiducialsSequence = 0x00700314;

		/// <summary>(0070,0318) VR=SQ VM=1 Graphic Coordinates Data Sequence</summary>
		public const uint GraphicCoordinatesDataSequence = 0x00700318;

		/// <summary>(0070,031a) VR=UI VM=1 Fiducial UID</summary>
		public const uint FiducialUID = 0x0070031a;

		/// <summary>(0070,031c) VR=SQ VM=1 Fiducial Set Sequence</summary>
		public const uint FiducialSetSequence = 0x0070031c;

		/// <summary>(0070,031e) VR=SQ VM=1 Fiducial Sequence</summary>
		public const uint FiducialSequence = 0x0070031e;

		/// <summary>(0070,0401) VR=US VM=3 Graphic Layer Recommended Display CIELab Value</summary>
		public const uint GraphicLayerRecommendedDisplayCIELabValue = 0x00700401;

		/// <summary>(0070,0402) VR=SQ VM=1 Blending Sequence</summary>
		public const uint BlendingSequence = 0x00700402;

		/// <summary>(0070,0403) VR=FL VM=1 Relative Opacity</summary>
		public const uint RelativeOpacity = 0x00700403;

		/// <summary>(0070,0404) VR=SQ VM=1 Referenced Spatial Registration Sequence</summary>
		public const uint ReferencedSpatialRegistrationSequence = 0x00700404;

		/// <summary>(0070,0405) VR=CS VM=1 Blending Position</summary>
		public const uint BlendingPosition = 0x00700405;

		/// <summary>(0072,0002) VR=SH VM=1 Hanging Protocol Name</summary>
		public const uint HangingProtocolName = 0x00720002;

		/// <summary>(0072,0004) VR=LO VM=1 Hanging Protocol Description</summary>
		public const uint HangingProtocolDescription = 0x00720004;

		/// <summary>(0072,0006) VR=CS VM=1 Hanging Protocol Level</summary>
		public const uint HangingProtocolLevel = 0x00720006;

		/// <summary>(0072,0008) VR=LO VM=1 Hanging Protocol Creator</summary>
		public const uint HangingProtocolCreator = 0x00720008;

		/// <summary>(0072,000a) VR=DT VM=1 Hanging Protocol Creation DateTime</summary>
		public const uint HangingProtocolCreationDateTime = 0x0072000a;

		/// <summary>(0072,000c) VR=SQ VM=1 Hanging Protocol Definition Sequence</summary>
		public const uint HangingProtocolDefinitionSequence = 0x0072000c;

		/// <summary>(0072,000e) VR=SQ VM=1 Hanging Protocol User Identification Code Sequence</summary>
		public const uint HangingProtocolUserIdentificationCodeSequence = 0x0072000e;

		/// <summary>(0072,0010) VR=LO VM=1 Hanging Protocol User Group Name</summary>
		public const uint HangingProtocolUserGroupName = 0x00720010;

		/// <summary>(0072,0012) VR=SQ VM=1 Source Hanging Protocol Sequence</summary>
		public const uint SourceHangingProtocolSequence = 0x00720012;

		/// <summary>(0072,0014) VR=US VM=1 Number of Priors Referenced</summary>
		public const uint NumberOfPriorsReferenced = 0x00720014;

		/// <summary>(0072,0020) VR=SQ VM=1 Image Sets Sequence</summary>
		public const uint ImageSetsSequence = 0x00720020;

		/// <summary>(0072,0022) VR=SQ VM=1 Image Set Selector Sequence</summary>
		public const uint ImageSetSelectorSequence = 0x00720022;

		/// <summary>(0072,0024) VR=CS VM=1 Image Set Selector Usage Flag</summary>
		public const uint ImageSetSelectorUsageFlag = 0x00720024;

		/// <summary>(0072,0026) VR=AT VM=1 Selector Attribute</summary>
		public const uint SelectorAttribute = 0x00720026;

		/// <summary>(0072,0028) VR=US VM=1 Selector Value Number</summary>
		public const uint SelectorValueNumber = 0x00720028;

		/// <summary>(0072,0030) VR=SQ VM=1 Time Based Image Sets Sequence</summary>
		public const uint TimeBasedImageSetsSequence = 0x00720030;

		/// <summary>(0072,0032) VR=US VM=1 Image Set Number</summary>
		public const uint ImageSetNumber = 0x00720032;

		/// <summary>(0072,0034) VR=CS VM=1 Image Set Selector Category</summary>
		public const uint ImageSetSelectorCategory = 0x00720034;

		/// <summary>(0072,0038) VR=US VM=2 Relative Time</summary>
		public const uint RelativeTime = 0x00720038;

		/// <summary>(0072,003a) VR=CS VM=1 Relative Time Units</summary>
		public const uint RelativeTimeUnits = 0x0072003a;

		/// <summary>(0072,003c) VR=SS VM=2 Abstract Prior Value</summary>
		public const uint AbstractPriorValue = 0x0072003c;

		/// <summary>(0072,003e) VR=SQ VM=1 Abstract Prior Code Sequence</summary>
		public const uint AbstractPriorCodeSequence = 0x0072003e;

		/// <summary>(0072,0040) VR=LO VM=1 Image Set Label</summary>
		public const uint ImageSetLabel = 0x00720040;

		/// <summary>(0072,0050) VR=CS VM=1 Selector Attribute VR</summary>
		public const uint SelectorAttributeVR = 0x00720050;

		/// <summary>(0072,0052) VR=AT VM=1 Selector Sequence Pointer</summary>
		public const uint SelectorSequencePointer = 0x00720052;

		/// <summary>(0072,0054) VR=LO VM=1 Selector Sequence Pointer Private Creator</summary>
		public const uint SelectorSequencePointerPrivateCreator = 0x00720054;

		/// <summary>(0072,0056) VR=LO VM=1 Selector Attribute Private Creator</summary>
		public const uint SelectorAttributePrivateCreator = 0x00720056;

		/// <summary>(0072,0060) VR=AT VM=1-n Selector AT Value</summary>
		public const uint SelectorATValue = 0x00720060;

		/// <summary>(0072,0062) VR=CS VM=1-n Selector CS Value</summary>
		public const uint SelectorCSValue = 0x00720062;

		/// <summary>(0072,0064) VR=IS VM=1-n Selector IS Value</summary>
		public const uint SelectorISValue = 0x00720064;

		/// <summary>(0072,0066) VR=LO VM=1-n Selector LO Value</summary>
		public const uint SelectorLOValue = 0x00720066;

		/// <summary>(0072,0068) VR=LT VM=1 Selector LT Value</summary>
		public const uint SelectorLTValue = 0x00720068;

		/// <summary>(0072,006a) VR=PN VM=1-n Selector PN Value</summary>
		public const uint SelectorPNValue = 0x0072006a;

		/// <summary>(0072,006c) VR=SH VM=1-n Selector SH Value</summary>
		public const uint SelectorSHValue = 0x0072006c;

		/// <summary>(0072,006e) VR=ST VM=1 Selector ST Value</summary>
		public const uint SelectorSTValue = 0x0072006e;

		/// <summary>(0072,0070) VR=UT VM=1 Selector UT Value</summary>
		public const uint SelectorUTValue = 0x00720070;

		/// <summary>(0072,0072) VR=DS VM=1-n Selector DS Value</summary>
		public const uint SelectorDSValue = 0x00720072;

		/// <summary>(0072,0074) VR=FD VM=1-n Selector FD Value</summary>
		public const uint SelectorFDValue = 0x00720074;

		/// <summary>(0072,0076) VR=FL VM=1-n Selector FL Value</summary>
		public const uint SelectorFLValue = 0x00720076;

		/// <summary>(0072,0078) VR=UL VM=1-n Selector UL Value</summary>
		public const uint SelectorULValue = 0x00720078;

		/// <summary>(0072,007a) VR=US VM=1-n Selector US Value</summary>
		public const uint SelectorUSValue = 0x0072007a;

		/// <summary>(0072,007c) VR=SL VM=1-n Selector SL Value</summary>
		public const uint SelectorSLValue = 0x0072007c;

		/// <summary>(0072,007e) VR=SS VM=1-n Selector SS Value</summary>
		public const uint SelectorSSValue = 0x0072007e;

		/// <summary>(0072,0080) VR=SQ VM=1 Selector Code Sequence Value</summary>
		public const uint SelectorCodeSequenceValue = 0x00720080;

		/// <summary>(0072,0100) VR=US VM=1 Number of Screens</summary>
		public const uint NumberOfScreens = 0x00720100;

		/// <summary>(0072,0102) VR=SQ VM=1 Nominal Screen Definition Sequence</summary>
		public const uint NominalScreenDefinitionSequence = 0x00720102;

		/// <summary>(0072,0104) VR=US VM=1 Number of Vertical Pixels</summary>
		public const uint NumberOfVerticalPixels = 0x00720104;

		/// <summary>(0072,0106) VR=US VM=1 Number of Horizontal Pixels</summary>
		public const uint NumberOfHorizontalPixels = 0x00720106;

		/// <summary>(0072,0108) VR=FD VM=4 Display Environment Spatial Position</summary>
		public const uint DisplayEnvironmentSpatialPosition = 0x00720108;

		/// <summary>(0072,010a) VR=US VM=1 Screen Minimum Grayscale Bit Depth</summary>
		public const uint ScreenMinimumGrayscaleBitDepth = 0x0072010a;

		/// <summary>(0072,010c) VR=US VM=1 Screen Minimum Color Bit Depth</summary>
		public const uint ScreenMinimumColorBitDepth = 0x0072010c;

		/// <summary>(0072,010e) VR=US VM=1 Application Maximum Repaint Time</summary>
		public const uint ApplicationMaximumRepaintTime = 0x0072010e;

		/// <summary>(0072,0200) VR=SQ VM=1 Display Sets Sequence</summary>
		public const uint DisplaySetsSequence = 0x00720200;

		/// <summary>(0072,0202) VR=US VM=1 Display Set Number</summary>
		public const uint DisplaySetNumber = 0x00720202;

		/// <summary>(0072,0203) VR=LO VM=1 Display Set Label</summary>
		public const uint DisplaySetLabel = 0x00720203;

		/// <summary>(0072,0204) VR=US VM=1 Display Set Presentation Group</summary>
		public const uint DisplaySetPresentationGroup = 0x00720204;

		/// <summary>(0072,0206) VR=LO VM=1 Display Set Presentation Group Description</summary>
		public const uint DisplaySetPresentationGroupDescription = 0x00720206;

		/// <summary>(0072,0208) VR=CS VM=1 Partial Data Display Handling</summary>
		public const uint PartialDataDisplayHandling = 0x00720208;

		/// <summary>(0072,0210) VR=SQ VM=1 Synchronized Scrolling Sequence</summary>
		public const uint SynchronizedScrollingSequence = 0x00720210;

		/// <summary>(0072,0212) VR=US VM=2-n Display Set Scrolling Group</summary>
		public const uint DisplaySetScrollingGroup = 0x00720212;

		/// <summary>(0072,0214) VR=SQ VM=1 Navigation Indicator Sequence</summary>
		public const uint NavigationIndicatorSequence = 0x00720214;

		/// <summary>(0072,0216) VR=US VM=1 Navigation Display Set</summary>
		public const uint NavigationDisplaySet = 0x00720216;

		/// <summary>(0072,0218) VR=US VM=1-n Reference Display Sets</summary>
		public const uint ReferenceDisplaySets = 0x00720218;

		/// <summary>(0072,0300) VR=SQ VM=1 Image Boxes Sequence</summary>
		public const uint ImageBoxesSequence = 0x00720300;

		/// <summary>(0072,0302) VR=US VM=1 Image Box Number</summary>
		public const uint ImageBoxNumber = 0x00720302;

		/// <summary>(0072,0304) VR=CS VM=1 Image Box Layout Type</summary>
		public const uint ImageBoxLayoutType = 0x00720304;

		/// <summary>(0072,0306) VR=US VM=1 Image Box Tile Horizontal Dimension</summary>
		public const uint ImageBoxTileHorizontalDimension = 0x00720306;

		/// <summary>(0072,0308) VR=US VM=1 Image Box Tile Vertical Dimension</summary>
		public const uint ImageBoxTileVerticalDimension = 0x00720308;

		/// <summary>(0072,0310) VR=CS VM=1 Image Box Scroll Direction</summary>
		public const uint ImageBoxScrollDirection = 0x00720310;

		/// <summary>(0072,0312) VR=CS VM=1 Image Box Small Scroll Type</summary>
		public const uint ImageBoxSmallScrollType = 0x00720312;

		/// <summary>(0072,0314) VR=US VM=1 Image Box Small Scroll Amount</summary>
		public const uint ImageBoxSmallScrollAmount = 0x00720314;

		/// <summary>(0072,0316) VR=CS VM=1 Image Box Large Scroll Type</summary>
		public const uint ImageBoxLargeScrollType = 0x00720316;

		/// <summary>(0072,0318) VR=US VM=1 Image Box Large Scroll Amount</summary>
		public const uint ImageBoxLargeScrollAmount = 0x00720318;

		/// <summary>(0072,0320) VR=US VM=1 Image Box Overlap Priority</summary>
		public const uint ImageBoxOverlapPriority = 0x00720320;

		/// <summary>(0072,0330) VR=FD VM=1 Cine Relative to Real-Time</summary>
		public const uint CineRelativeToRealTime = 0x00720330;

		/// <summary>(0072,0400) VR=SQ VM=1 Filter Operations Sequence</summary>
		public const uint FilterOperationsSequence = 0x00720400;

		/// <summary>(0072,0402) VR=CS VM=1 Filter-by Category</summary>
		public const uint FilterbyCategory = 0x00720402;

		/// <summary>(0072,0404) VR=CS VM=1 Filter-by Attribute Presence</summary>
		public const uint FilterbyAttributePresence = 0x00720404;

		/// <summary>(0072,0406) VR=CS VM=1 Filter-by Operator</summary>
		public const uint FilterbyOperator = 0x00720406;

		/// <summary>(0072,0500) VR=CS VM=1 Blending Operation Type</summary>
		public const uint BlendingOperationType = 0x00720500;

		/// <summary>(0072,0510) VR=CS VM=1 Reformatting Operation Type</summary>
		public const uint ReformattingOperationType = 0x00720510;

		/// <summary>(0072,0512) VR=FD VM=1 Reformatting Thickness</summary>
		public const uint ReformattingThickness = 0x00720512;

		/// <summary>(0072,0514) VR=FD VM=1 Reformatting Interval</summary>
		public const uint ReformattingInterval = 0x00720514;

		/// <summary>(0072,0516) VR=CS VM=1 Reformatting Operation Initial View Direction</summary>
		public const uint ReformattingOperationInitialViewDirection = 0x00720516;

		/// <summary>(0072,0520) VR=CS VM=1-n 3D Rendering Type</summary>
		public const uint RenderingType3D = 0x00720520;

		/// <summary>(0072,0600) VR=SQ VM=1 Sorting Operations Sequence</summary>
		public const uint SortingOperationsSequence = 0x00720600;

		/// <summary>(0072,0602) VR=CS VM=1 Sort-by Category</summary>
		public const uint SortbyCategory = 0x00720602;

		/// <summary>(0072,0604) VR=CS VM=1 Sorting Direction</summary>
		public const uint SortingDirection = 0x00720604;

		/// <summary>(0072,0700) VR=CS VM=2 Display Set Patient Orientation</summary>
		public const uint DisplaySetPatientOrientation = 0x00720700;

		/// <summary>(0072,0702) VR=CS VM=1 VOI Type</summary>
		public const uint VOIType = 0x00720702;

		/// <summary>(0072,0704) VR=CS VM=1 Pseudo-color Type</summary>
		public const uint PseudocolorType = 0x00720704;

		/// <summary>(0072,0706) VR=CS VM=1 Show Grayscale Inverted</summary>
		public const uint ShowGrayscaleInverted = 0x00720706;

		/// <summary>(0072,0710) VR=CS VM=1 Show Image True Size Flag</summary>
		public const uint ShowImageTrueSizeFlag = 0x00720710;

		/// <summary>(0072,0712) VR=CS VM=1 Show Graphic Annotation Flag</summary>
		public const uint ShowGraphicAnnotationFlag = 0x00720712;

		/// <summary>(0072,0714) VR=CS VM=1 Show Patient Demographics Flag</summary>
		public const uint ShowPatientDemographicsFlag = 0x00720714;

		/// <summary>(0072,0716) VR=CS VM=1 Show Acquisition Techniques Flag</summary>
		public const uint ShowAcquisitionTechniquesFlag = 0x00720716;

		/// <summary>(0072,0717) VR=CS VM=1 Display Set Horizontal Justification</summary>
		public const uint DisplaySetHorizontalJustification = 0x00720717;

		/// <summary>(0072,0718) VR=CS VM=1 Display Set Vertical Justification</summary>
		public const uint DisplaySetVerticalJustification = 0x00720718;

		/// <summary>(0074,1000) VR=CS VM=1 Unified Procedure Step State</summary>
		public const uint UnifiedProcedureStepState = 0x00741000;

		/// <summary>(0074,1002) VR=SQ VM=1 UPS Progress Information Sequence</summary>
		public const uint UPSProgressInformationSequence = 0x00741002;

		/// <summary>(0074,1004) VR=DS VM=1 Unified Procedure Step Progress</summary>
		public const uint UnifiedProcedureStepProgress = 0x00741004;

		/// <summary>(0074,1006) VR=ST VM=1 Unified Procedure Step Progress Description</summary>
		public const uint UnifiedProcedureStepProgressDescription = 0x00741006;

		/// <summary>(0074,1008) VR=SQ VM=1 Unified Procedure Step Communications URI Sequence</summary>
		public const uint UnifiedProcedureStepCommunicationsURISequence = 0x00741008;

		/// <summary>(0074,100a) VR=ST VM=1 Contact URI</summary>
		public const uint ContactURI = 0x0074100a;

		/// <summary>(0074,100c) VR=LO VM=1 Contact Display Name</summary>
		public const uint ContactDisplayName = 0x0074100c;

		/// <summary>(0074,100e) VR=SQ VM=1 Unified Procedure Step Discontinuation Reason Code Sequence</summary>
		public const uint UnifiedProcedureStepDiscontinuationReasonCodeSequence = 0x0074100e;

		/// <summary>(0074,1020) VR=SQ VM=1 Beam Task Sequence</summary>
		public const uint BeamTaskSequence = 0x00741020;

		/// <summary>(0074,1022) VR=CS VM=1 Beam Task Type</summary>
		public const uint BeamTaskType = 0x00741022;

		/// <summary>(0074,1024) VR=IS VM=1 Beam Order Index</summary>
		public const uint BeamOrderIndex = 0x00741024;

		/// <summary>(0074,1030) VR=SQ VM=1 Delivery Verification Image Sequence</summary>
		public const uint DeliveryVerificationImageSequence = 0x00741030;

		/// <summary>(0074,1032) VR=CS VM=1 Verification Image Timing</summary>
		public const uint VerificationImageTiming = 0x00741032;

		/// <summary>(0074,1034) VR=CS VM=1 Double Exposure Flag</summary>
		public const uint DoubleExposureFlag = 0x00741034;

		/// <summary>(0074,1036) VR=CS VM=1 Double Exposure Ordering</summary>
		public const uint DoubleExposureOrdering = 0x00741036;

		/// <summary>(0074,1038) VR=DS VM=1 Double Exposure Meterset</summary>
		public const uint DoubleExposureMeterset = 0x00741038;

		/// <summary>(0074,103a) VR=DS VM=4 Double Exposure Field Delta</summary>
		public const uint DoubleExposureFieldDelta = 0x0074103a;

		/// <summary>(0074,1040) VR=SQ VM=1 Related Reference RT Image Sequence</summary>
		public const uint RelatedReferenceRTImageSequence = 0x00741040;

		/// <summary>(0074,1042) VR=SQ VM=1 General Machine Verification Sequence</summary>
		public const uint GeneralMachineVerificationSequence = 0x00741042;

		/// <summary>(0074,1044) VR=SQ VM=1 Conventional Machine Verification Sequence</summary>
		public const uint ConventionalMachineVerificationSequence = 0x00741044;

		/// <summary>(0074,1046) VR=SQ VM=1 Ion Machine Verification Sequence</summary>
		public const uint IonMachineVerificationSequence = 0x00741046;

		/// <summary>(0074,1048) VR=SQ VM=1 Failed Attributes Sequence</summary>
		public const uint FailedAttributesSequence = 0x00741048;

		/// <summary>(0074,104a) VR=SQ VM=1 Overridden Attributes Sequence</summary>
		public const uint OverriddenAttributesSequence = 0x0074104a;

		/// <summary>(0074,104c) VR=SQ VM=1 Conventional Control Point Verification Sequence</summary>
		public const uint ConventionalControlPointVerificationSequence = 0x0074104c;

		/// <summary>(0074,104e) VR=SQ VM=1 Ion Control Point Verification Sequence</summary>
		public const uint IonControlPointVerificationSequence = 0x0074104e;

		/// <summary>(0074,1050) VR=SQ VM=1 Attribute Occurrence Sequence</summary>
		public const uint AttributeOccurrenceSequence = 0x00741050;

		/// <summary>(0074,1052) VR=AT VM=1 Attribute Occurrence Pointer</summary>
		public const uint AttributeOccurrencePointer = 0x00741052;

		/// <summary>(0074,1054) VR=UL VM=1 Attribute Item Selector</summary>
		public const uint AttributeItemSelector = 0x00741054;

		/// <summary>(0074,1056) VR=LO VM=1 Attribute Occurrence Private Creator</summary>
		public const uint AttributeOccurrencePrivateCreator = 0x00741056;

		/// <summary>(0074,1200) VR=CS VM=1 Scheduled Procedure Step Priority</summary>
		public const uint ScheduledProcedureStepPriority = 0x00741200;

		/// <summary>(0074,1202) VR=LO VM=1 Worklist Label</summary>
		public const uint WorklistLabel = 0x00741202;

		/// <summary>(0074,1204) VR=LO VM=1 Procedure Step Label</summary>
		public const uint ProcedureStepLabel = 0x00741204;

		/// <summary>(0074,1210) VR=SQ VM=1 Scheduled Processing Parameters Sequence</summary>
		public const uint ScheduledProcessingParametersSequence = 0x00741210;

		/// <summary>(0074,1212) VR=SQ VM=1 Performed Processing Parameters Sequence</summary>
		public const uint PerformedProcessingParametersSequence = 0x00741212;

		/// <summary>(0074,1216) VR=SQ VM=1 UPS Performed Procedure Sequence</summary>
		public const uint UPSPerformedProcedureSequence = 0x00741216;

		/// <summary>(0074,1220) VR=SQ VM=1 Related Procedure Step Sequence</summary>
		public const uint RelatedProcedureStepSequence = 0x00741220;

		/// <summary>(0074,1222) VR=LO VM=1 Procedure Step Relationship Type</summary>
		public const uint ProcedureStepRelationshipType = 0x00741222;

		/// <summary>(0074,1230) VR=LO VM=1 Deletion Lock</summary>
		public const uint DeletionLock = 0x00741230;

		/// <summary>(0074,1234) VR=AE VM=1 Receiving AE</summary>
		public const uint ReceivingAE = 0x00741234;

		/// <summary>(0074,1236) VR=AE VM=1 Requesting AE</summary>
		public const uint RequestingAE = 0x00741236;

		/// <summary>(0074,1238) VR=LT VM=1 Reason for Cancellation</summary>
		public const uint ReasonForCancellation = 0x00741238;

		/// <summary>(0074,1242) VR=CS VM=1 SCP Status</summary>
		public const uint SCPStatus = 0x00741242;

		/// <summary>(0074,1244) VR=CS VM=1 Subscription List Status</summary>
		public const uint SubscriptionListStatus = 0x00741244;

		/// <summary>(0074,1246) VR=CS VM=1 UPS List Status</summary>
		public const uint UPSListStatus = 0x00741246;

		/// <summary>(0088,0130) VR=SH VM=1 Storage Media File-set ID</summary>
		public const uint StorageMediaFilesetID = 0x00880130;

		/// <summary>(0088,0140) VR=UI VM=1 Storage Media File-set UID</summary>
		public const uint StorageMediaFilesetUID = 0x00880140;

		/// <summary>(0088,0200) VR=SQ VM=1 Icon Image Sequence</summary>
		public const uint IconImageSequence = 0x00880200;

		/// <summary>(0100,0410) VR=CS VM=1 SOP Instance Status</summary>
		public const uint SOPInstanceStatus = 0x01000410;

		/// <summary>(0100,0420) VR=DT VM=1 SOP Authorization Date and Time</summary>
		public const uint SOPAuthorizationDateAndTime = 0x01000420;

		/// <summary>(0100,0424) VR=LT VM=1 SOP Authorization Comment</summary>
		public const uint SOPAuthorizationComment = 0x01000424;

		/// <summary>(0100,0426) VR=LO VM=1 Authorization Equipment Certification Number</summary>
		public const uint AuthorizationEquipmentCertificationNumber = 0x01000426;

		/// <summary>(0400,0005) VR=US VM=1 MAC ID Number</summary>
		public const uint MACIDNumber = 0x04000005;

		/// <summary>(0400,0010) VR=UI VM=1 MAC Calculation Transfer Syntax UID</summary>
		public const uint MACCalculationTransferSyntaxUID = 0x04000010;

		/// <summary>(0400,0015) VR=CS VM=1 MAC Algorithm</summary>
		public const uint MACAlgorithm = 0x04000015;

		/// <summary>(0400,0020) VR=AT VM=1-n Data Elements Signed</summary>
		public const uint DataElementsSigned = 0x04000020;

		/// <summary>(0400,0100) VR=UI VM=1 Digital Signature UID</summary>
		public const uint DigitalSignatureUID = 0x04000100;

		/// <summary>(0400,0105) VR=DT VM=1 Digital Signature DateTime</summary>
		public const uint DigitalSignatureDateTime = 0x04000105;

		/// <summary>(0400,0110) VR=CS VM=1 Certificate Type</summary>
		public const uint CertificateType = 0x04000110;

		/// <summary>(0400,0115) VR=OB VM=1 Certificate of Signer</summary>
		public const uint CertificateOfSigner = 0x04000115;

		/// <summary>(0400,0120) VR=OB VM=1 Signature</summary>
		public const uint Signature = 0x04000120;

		/// <summary>(0400,0305) VR=CS VM=1 Certified Timestamp Type</summary>
		public const uint CertifiedTimestampType = 0x04000305;

		/// <summary>(0400,0310) VR=OB VM=1 Certified Timestamp</summary>
		public const uint CertifiedTimestamp = 0x04000310;

		/// <summary>(0400,0401) VR=SQ VM=1 Digital Signature Purpose Code Sequence</summary>
		public const uint DigitalSignaturePurposeCodeSequence = 0x04000401;

		/// <summary>(0400,0402) VR=SQ VM=1 Referenced Digital Signature Sequence</summary>
		public const uint ReferencedDigitalSignatureSequence = 0x04000402;

		/// <summary>(0400,0403) VR=SQ VM=1 Referenced SOP Instance MAC Sequence</summary>
		public const uint ReferencedSOPInstanceMACSequence = 0x04000403;

		/// <summary>(0400,0404) VR=OB VM=1 MAC</summary>
		public const uint MAC = 0x04000404;

		/// <summary>(0400,0500) VR=SQ VM=1 Encrypted Attributes Sequence</summary>
		public const uint EncryptedAttributesSequence = 0x04000500;

		/// <summary>(0400,0510) VR=UI VM=1 Encrypted Content Transfer Syntax UID</summary>
		public const uint EncryptedContentTransferSyntaxUID = 0x04000510;

		/// <summary>(0400,0520) VR=OB VM=1 Encrypted Content</summary>
		public const uint EncryptedContent = 0x04000520;

		/// <summary>(0400,0550) VR=SQ VM=1 Modified Attributes Sequence</summary>
		public const uint ModifiedAttributesSequence = 0x04000550;

		/// <summary>(0400,0561) VR=SQ VM=1 Original Attributes Sequence</summary>
		public const uint OriginalAttributesSequence = 0x04000561;

		/// <summary>(0400,0562) VR=DT VM=1 Attribute Modification DateTime</summary>
		public const uint AttributeModificationDateTime = 0x04000562;

		/// <summary>(0400,0563) VR=LO VM=1 Modifying System</summary>
		public const uint ModifyingSystem = 0x04000563;

		/// <summary>(0400,0564) VR=LO VM=1 Source of Previous Values</summary>
		public const uint SourceOfPreviousValues = 0x04000564;

		/// <summary>(0400,0565) VR=CS VM=1 Reason for the Attribute Modification</summary>
		public const uint ReasonForTheAttributeModification = 0x04000565;

		/// <summary>(2000,0010) VR=IS VM=1 Number of Copies</summary>
		public const uint NumberOfCopies = 0x20000010;

		/// <summary>(2000,001e) VR=SQ VM=1 Printer Configuration Sequence</summary>
		public const uint PrinterConfigurationSequence = 0x2000001e;

		/// <summary>(2000,0020) VR=CS VM=1 Print Priority</summary>
		public const uint PrintPriority = 0x20000020;

		/// <summary>(2000,0030) VR=CS VM=1 Medium Type</summary>
		public const uint MediumType = 0x20000030;

		/// <summary>(2000,0040) VR=CS VM=1 Film Destination</summary>
		public const uint FilmDestination = 0x20000040;

		/// <summary>(2000,0050) VR=LO VM=1 Film Session Label</summary>
		public const uint FilmSessionLabel = 0x20000050;

		/// <summary>(2000,0060) VR=IS VM=1 Memory Allocation</summary>
		public const uint MemoryAllocation = 0x20000060;

		/// <summary>(2000,0061) VR=IS VM=1 Maximum Memory Allocation</summary>
		public const uint MaximumMemoryAllocation = 0x20000061;

		/// <summary>(2000,00a0) VR=US VM=1 Memory Bit Depth</summary>
		public const uint MemoryBitDepth = 0x200000a0;

		/// <summary>(2000,00a1) VR=US VM=1 Printing Bit Depth</summary>
		public const uint PrintingBitDepth = 0x200000a1;

		/// <summary>(2000,00a2) VR=SQ VM=1 Media Installed Sequence</summary>
		public const uint MediaInstalledSequence = 0x200000a2;

		/// <summary>(2000,00a4) VR=SQ VM=1 Other Media Available Sequence</summary>
		public const uint OtherMediaAvailableSequence = 0x200000a4;

		/// <summary>(2000,00a8) VR=SQ VM=1 Supported Image Display Formats Sequence</summary>
		public const uint SupportedImageDisplayFormatsSequence = 0x200000a8;

		/// <summary>(2000,0500) VR=SQ VM=1 Referenced Film Box Sequence</summary>
		public const uint ReferencedFilmBoxSequence = 0x20000500;

		/// <summary>(2010,0010) VR=ST VM=1 Image Display Format</summary>
		public const uint ImageDisplayFormat = 0x20100010;

		/// <summary>(2010,0030) VR=CS VM=1 Annotation Display Format ID</summary>
		public const uint AnnotationDisplayFormatID = 0x20100030;

		/// <summary>(2010,0040) VR=CS VM=1 Film Orientation</summary>
		public const uint FilmOrientation = 0x20100040;

		/// <summary>(2010,0050) VR=CS VM=1 Film Size ID</summary>
		public const uint FilmSizeID = 0x20100050;

		/// <summary>(2010,0052) VR=CS VM=1 Printer Resolution ID</summary>
		public const uint PrinterResolutionID = 0x20100052;

		/// <summary>(2010,0054) VR=CS VM=1 Default Printer Resolution ID</summary>
		public const uint DefaultPrinterResolutionID = 0x20100054;

		/// <summary>(2010,0060) VR=CS VM=1 Magnification Type</summary>
		public const uint MagnificationType = 0x20100060;

		/// <summary>(2010,0080) VR=CS VM=1 Smoothing Type</summary>
		public const uint SmoothingType = 0x20100080;

		/// <summary>(2010,00a6) VR=CS VM=1 Default Magnification Type</summary>
		public const uint DefaultMagnificationType = 0x201000a6;

		/// <summary>(2010,00a7) VR=CS VM=1-n Other Magnification Types Available</summary>
		public const uint OtherMagnificationTypesAvailable = 0x201000a7;

		/// <summary>(2010,00a8) VR=CS VM=1 Default Smoothing Type</summary>
		public const uint DefaultSmoothingType = 0x201000a8;

		/// <summary>(2010,00a9) VR=CS VM=1-n Other Smoothing Types Available</summary>
		public const uint OtherSmoothingTypesAvailable = 0x201000a9;

		/// <summary>(2010,0100) VR=CS VM=1 Border Density</summary>
		public const uint BorderDensity = 0x20100100;

		/// <summary>(2010,0110) VR=CS VM=1 Empty Image Density</summary>
		public const uint EmptyImageDensity = 0x20100110;

		/// <summary>(2010,0120) VR=US VM=1 Min Density</summary>
		public const uint MinDensity = 0x20100120;

		/// <summary>(2010,0130) VR=US VM=1 Max Density</summary>
		public const uint MaxDensity = 0x20100130;

		/// <summary>(2010,0140) VR=CS VM=1 Trim</summary>
		public const uint Trim = 0x20100140;

		/// <summary>(2010,0150) VR=ST VM=1 Configuration Information</summary>
		public const uint ConfigurationInformation = 0x20100150;

		/// <summary>(2010,0152) VR=LT VM=1 Configuration Information Description</summary>
		public const uint ConfigurationInformationDescription = 0x20100152;

		/// <summary>(2010,0154) VR=IS VM=1 Maximum Collated Films</summary>
		public const uint MaximumCollatedFilms = 0x20100154;

		/// <summary>(2010,015e) VR=US VM=1 Illumination</summary>
		public const uint Illumination = 0x2010015e;

		/// <summary>(2010,0160) VR=US VM=1 Reflected Ambient Light</summary>
		public const uint ReflectedAmbientLight = 0x20100160;

		/// <summary>(2010,0376) VR=DS VM=2 Printer Pixel Spacing</summary>
		public const uint PrinterPixelSpacing = 0x20100376;

		/// <summary>(2010,0500) VR=SQ VM=1 Referenced Film Session Sequence</summary>
		public const uint ReferencedFilmSessionSequence = 0x20100500;

		/// <summary>(2010,0510) VR=SQ VM=1 Referenced Image Box Sequence</summary>
		public const uint ReferencedImageBoxSequence = 0x20100510;

		/// <summary>(2010,0520) VR=SQ VM=1 Referenced Basic Annotation Box Sequence</summary>
		public const uint ReferencedBasicAnnotationBoxSequence = 0x20100520;

		/// <summary>(2020,0010) VR=US VM=1 Image Box Position</summary>
		public const uint ImageBoxPosition = 0x20200010;

		/// <summary>(2020,0020) VR=CS VM=1 Polarity</summary>
		public const uint Polarity = 0x20200020;

		/// <summary>(2020,0030) VR=DS VM=1 Requested Image Size</summary>
		public const uint RequestedImageSize = 0x20200030;

		/// <summary>(2020,0040) VR=CS VM=1 Requested Decimate/Crop Behavior</summary>
		public const uint RequestedDecimateCropBehavior = 0x20200040;

		/// <summary>(2020,0050) VR=CS VM=1 Requested Resolution ID</summary>
		public const uint RequestedResolutionID = 0x20200050;

		/// <summary>(2020,00a0) VR=CS VM=1 Requested Image Size Flag</summary>
		public const uint RequestedImageSizeFlag = 0x202000a0;

		/// <summary>(2020,00a2) VR=CS VM=1 Decimate/Crop Result</summary>
		public const uint DecimateCropResult = 0x202000a2;

		/// <summary>(2020,0110) VR=SQ VM=1 Basic Grayscale Image Sequence</summary>
		public const uint BasicGrayscaleImageSequence = 0x20200110;

		/// <summary>(2020,0111) VR=SQ VM=1 Basic Color Image Sequence</summary>
		public const uint BasicColorImageSequence = 0x20200111;

		/// <summary>(2030,0010) VR=US VM=1 Annotation Position</summary>
		public const uint AnnotationPosition = 0x20300010;

		/// <summary>(2030,0020) VR=LO VM=1 Text String</summary>
		public const uint TextString = 0x20300020;

		/// <summary>(2050,0010) VR=SQ VM=1 Presentation LUT Sequence</summary>
		public const uint PresentationLUTSequence = 0x20500010;

		/// <summary>(2050,0020) VR=CS VM=1 Presentation LUT Shape</summary>
		public const uint PresentationLUTShape = 0x20500020;

		/// <summary>(2050,0500) VR=SQ VM=1 Referenced Presentation LUT Sequence</summary>
		public const uint ReferencedPresentationLUTSequence = 0x20500500;

		/// <summary>(2100,0020) VR=CS VM=1 Execution Status</summary>
		public const uint ExecutionStatus = 0x21000020;

		/// <summary>(2100,0030) VR=CS VM=1 Execution Status Info</summary>
		public const uint ExecutionStatusInfo = 0x21000030;

		/// <summary>(2100,0040) VR=DA VM=1 Creation Date</summary>
		public const uint CreationDate = 0x21000040;

		/// <summary>(2100,0050) VR=TM VM=1 Creation Time</summary>
		public const uint CreationTime = 0x21000050;

		/// <summary>(2100,0070) VR=AE VM=1 Originator</summary>
		public const uint Originator = 0x21000070;

		/// <summary>(2100,0160) VR=SH VM=1 Owner ID</summary>
		public const uint OwnerID = 0x21000160;

		/// <summary>(2100,0170) VR=IS VM=1 Number of Films</summary>
		public const uint NumberOfFilms = 0x21000170;

		/// <summary>(2110,0010) VR=CS VM=1 Printer Status</summary>
		public const uint PrinterStatus = 0x21100010;

		/// <summary>(2110,0020) VR=CS VM=1 Printer Status Info</summary>
		public const uint PrinterStatusInfo = 0x21100020;

		/// <summary>(2110,0030) VR=LO VM=1 Printer Name</summary>
		public const uint PrinterName = 0x21100030;

		/// <summary>(2200,0001) VR=CS VM=1 Label Using Information Extracted From Instances</summary>
		public const uint LabelUsingInformationExtractedFromInstances = 0x22000001;

		/// <summary>(2200,0002) VR=UT VM=1 Label Text</summary>
		public const uint LabelText = 0x22000002;

		/// <summary>(2200,0003) VR=CS VM=1 Label Style Selection</summary>
		public const uint LabelStyleSelection = 0x22000003;

		/// <summary>(2200,0004) VR=LT VM=1 Media Disposition</summary>
		public const uint MediaDisposition = 0x22000004;

		/// <summary>(2200,0005) VR=LT VM=1 Barcode Value</summary>
		public const uint BarcodeValue = 0x22000005;

		/// <summary>(2200,0006) VR=CS VM=1 Barcode Symbology</summary>
		public const uint BarcodeSymbology = 0x22000006;

		/// <summary>(2200,0007) VR=CS VM=1 Allow Media Splitting</summary>
		public const uint AllowMediaSplitting = 0x22000007;

		/// <summary>(2200,0008) VR=CS VM=1 Include Non-DICOM Objects</summary>
		public const uint IncludeNonDICOMObjects = 0x22000008;

		/// <summary>(2200,0009) VR=CS VM=1 Include Display Application</summary>
		public const uint IncludeDisplayApplication = 0x22000009;

		/// <summary>(2200,000a) VR=CS VM=1 Preserve Composite Instances After Media Creation</summary>
		public const uint PreserveCompositeInstancesAfterMediaCreation = 0x2200000a;

		/// <summary>(2200,000b) VR=US VM=1 Total Number of Pieces of Media Created</summary>
		public const uint TotalNumberOfPiecesOfMediaCreated = 0x2200000b;

		/// <summary>(2200,000c) VR=LO VM=1 Requested Media Application Profile</summary>
		public const uint RequestedMediaApplicationProfile = 0x2200000c;

		/// <summary>(2200,000d) VR=SQ VM=1 Referenced Storage Media Sequence</summary>
		public const uint ReferencedStorageMediaSequence = 0x2200000d;

		/// <summary>(2200,000e) VR=AT VM=1-n Failure Attributes</summary>
		public const uint FailureAttributes = 0x2200000e;

		/// <summary>(2200,000f) VR=CS VM=1 Allow Lossy Compression</summary>
		public const uint AllowLossyCompression = 0x2200000f;

		/// <summary>(2200,0020) VR=CS VM=1 Request Priority</summary>
		public const uint RequestPriority = 0x22000020;

		/// <summary>(3002,0002) VR=SH VM=1 RT Image Label</summary>
		public const uint RTImageLabel = 0x30020002;

		/// <summary>(3002,0003) VR=LO VM=1 RT Image Name</summary>
		public const uint RTImageName = 0x30020003;

		/// <summary>(3002,0004) VR=ST VM=1 RT Image Description</summary>
		public const uint RTImageDescription = 0x30020004;

		/// <summary>(3002,000a) VR=CS VM=1 Reported Values Origin</summary>
		public const uint ReportedValuesOrigin = 0x3002000a;

		/// <summary>(3002,000c) VR=CS VM=1 RT Image Plane</summary>
		public const uint RTImagePlane = 0x3002000c;

		/// <summary>(3002,000d) VR=DS VM=3 X-Ray Image Receptor Translation</summary>
		public const uint XRayImageReceptorTranslation = 0x3002000d;

		/// <summary>(3002,000e) VR=DS VM=1 X-Ray Image Receptor Angle</summary>
		public const uint XRayImageReceptorAngle = 0x3002000e;

		/// <summary>(3002,0010) VR=DS VM=6 RT Image Orientation</summary>
		public const uint RTImageOrientation = 0x30020010;

		/// <summary>(3002,0011) VR=DS VM=2 Image Plane Pixel Spacing</summary>
		public const uint ImagePlanePixelSpacing = 0x30020011;

		/// <summary>(3002,0012) VR=DS VM=2 RT Image Position</summary>
		public const uint RTImagePosition = 0x30020012;

		/// <summary>(3002,0020) VR=SH VM=1 Radiation Machine Name</summary>
		public const uint RadiationMachineName = 0x30020020;

		/// <summary>(3002,0022) VR=DS VM=1 Radiation Machine SAD</summary>
		public const uint RadiationMachineSAD = 0x30020022;

		/// <summary>(3002,0024) VR=DS VM=1 Radiation Machine SSD</summary>
		public const uint RadiationMachineSSD = 0x30020024;

		/// <summary>(3002,0026) VR=DS VM=1 RT Image SID</summary>
		public const uint RTImageSID = 0x30020026;

		/// <summary>(3002,0028) VR=DS VM=1 Source to Reference Object Distance</summary>
		public const uint SourceToReferenceObjectDistance = 0x30020028;

		/// <summary>(3002,0029) VR=IS VM=1 Fraction Number</summary>
		public const uint FractionNumber = 0x30020029;

		/// <summary>(3002,0030) VR=SQ VM=1 Exposure Sequence</summary>
		public const uint ExposureSequence = 0x30020030;

		/// <summary>(3002,0032) VR=DS VM=1 Meterset Exposure</summary>
		public const uint MetersetExposure = 0x30020032;

		/// <summary>(3002,0034) VR=DS VM=4 Diaphragm Position</summary>
		public const uint DiaphragmPosition = 0x30020034;

		/// <summary>(3002,0040) VR=SQ VM=1 Fluence Map Sequence</summary>
		public const uint FluenceMapSequence = 0x30020040;

		/// <summary>(3002,0041) VR=CS VM=1 Fluence Data Source</summary>
		public const uint FluenceDataSource = 0x30020041;

		/// <summary>(3002,0042) VR=DS VM=1 Fluence Data Scale</summary>
		public const uint FluenceDataScale = 0x30020042;

		/// <summary>(3004,0001) VR=CS VM=1 DVH Type</summary>
		public const uint DVHType = 0x30040001;

		/// <summary>(3004,0002) VR=CS VM=1 Dose Units</summary>
		public const uint DoseUnits = 0x30040002;

		/// <summary>(3004,0004) VR=CS VM=1 Dose Type</summary>
		public const uint DoseType = 0x30040004;

		/// <summary>(3004,0006) VR=LO VM=1 Dose Comment</summary>
		public const uint DoseComment = 0x30040006;

		/// <summary>(3004,0008) VR=DS VM=3 Normalization Point</summary>
		public const uint NormalizationPoint = 0x30040008;

		/// <summary>(3004,000a) VR=CS VM=1 Dose Summation Type</summary>
		public const uint DoseSummationType = 0x3004000a;

		/// <summary>(3004,000c) VR=DS VM=2-n Grid Frame Offset Vector</summary>
		public const uint GridFrameOffsetVector = 0x3004000c;

		/// <summary>(3004,000e) VR=DS VM=1 Dose Grid Scaling</summary>
		public const uint DoseGridScaling = 0x3004000e;

		/// <summary>(3004,0010) VR=SQ VM=1 RT Dose ROI Sequence</summary>
		public const uint RTDoseROISequence = 0x30040010;

		/// <summary>(3004,0012) VR=DS VM=1 Dose Value</summary>
		public const uint DoseValue = 0x30040012;

		/// <summary>(3004,0014) VR=CS VM=1-3 Tissue Heterogeneity Correction</summary>
		public const uint TissueHeterogeneityCorrection = 0x30040014;

		/// <summary>(3004,0040) VR=DS VM=3 DVH Normalization Point</summary>
		public const uint DVHNormalizationPoint = 0x30040040;

		/// <summary>(3004,0042) VR=DS VM=1 DVH Normalization Dose Value</summary>
		public const uint DVHNormalizationDoseValue = 0x30040042;

		/// <summary>(3004,0050) VR=SQ VM=1 DVH Sequence</summary>
		public const uint DVHSequence = 0x30040050;

		/// <summary>(3004,0052) VR=DS VM=1 DVH Dose Scaling</summary>
		public const uint DVHDoseScaling = 0x30040052;

		/// <summary>(3004,0054) VR=CS VM=1 DVH Volume Units</summary>
		public const uint DVHVolumeUnits = 0x30040054;

		/// <summary>(3004,0056) VR=IS VM=1 DVH Number of Bins</summary>
		public const uint DVHNumberOfBins = 0x30040056;

		/// <summary>(3004,0058) VR=DS VM=2-2n DVH Data</summary>
		public const uint DVHData = 0x30040058;

		/// <summary>(3004,0060) VR=SQ VM=1 DVH Referenced ROI Sequence</summary>
		public const uint DVHReferencedROISequence = 0x30040060;

		/// <summary>(3004,0062) VR=CS VM=1 DVH ROI Contribution Type</summary>
		public const uint DVHROIContributionType = 0x30040062;

		/// <summary>(3004,0070) VR=DS VM=1 DVH Minimum Dose</summary>
		public const uint DVHMinimumDose = 0x30040070;

		/// <summary>(3004,0072) VR=DS VM=1 DVH Maximum Dose</summary>
		public const uint DVHMaximumDose = 0x30040072;

		/// <summary>(3004,0074) VR=DS VM=1 DVH Mean Dose</summary>
		public const uint DVHMeanDose = 0x30040074;

		/// <summary>(3006,0002) VR=SH VM=1 Structure Set Label</summary>
		public const uint StructureSetLabel = 0x30060002;

		/// <summary>(3006,0004) VR=LO VM=1 Structure Set Name</summary>
		public const uint StructureSetName = 0x30060004;

		/// <summary>(3006,0006) VR=ST VM=1 Structure Set Description</summary>
		public const uint StructureSetDescription = 0x30060006;

		/// <summary>(3006,0008) VR=DA VM=1 Structure Set Date</summary>
		public const uint StructureSetDate = 0x30060008;

		/// <summary>(3006,0009) VR=TM VM=1 Structure Set Time</summary>
		public const uint StructureSetTime = 0x30060009;

		/// <summary>(3006,0010) VR=SQ VM=1 Referenced Frame of Reference Sequence</summary>
		public const uint ReferencedFrameOfReferenceSequence = 0x30060010;

		/// <summary>(3006,0012) VR=SQ VM=1 RT Referenced Study Sequence</summary>
		public const uint RTReferencedStudySequence = 0x30060012;

		/// <summary>(3006,0014) VR=SQ VM=1 RT Referenced Series Sequence</summary>
		public const uint RTReferencedSeriesSequence = 0x30060014;

		/// <summary>(3006,0016) VR=SQ VM=1 Contour Image Sequence</summary>
		public const uint ContourImageSequence = 0x30060016;

		/// <summary>(3006,0020) VR=SQ VM=1 Structure Set ROI Sequence</summary>
		public const uint StructureSetROISequence = 0x30060020;

		/// <summary>(3006,0022) VR=IS VM=1 ROI Number</summary>
		public const uint ROINumber = 0x30060022;

		/// <summary>(3006,0024) VR=UI VM=1 Referenced Frame of Reference UID</summary>
		public const uint ReferencedFrameOfReferenceUID = 0x30060024;

		/// <summary>(3006,0026) VR=LO VM=1 ROI Name</summary>
		public const uint ROIName = 0x30060026;

		/// <summary>(3006,0028) VR=ST VM=1 ROI Description</summary>
		public const uint ROIDescription = 0x30060028;

		/// <summary>(3006,002a) VR=IS VM=3 ROI Display Color</summary>
		public const uint ROIDisplayColor = 0x3006002a;

		/// <summary>(3006,002c) VR=DS VM=1 ROI Volume</summary>
		public const uint ROIVolume = 0x3006002c;

		/// <summary>(3006,0030) VR=SQ VM=1 RT Related ROI Sequence</summary>
		public const uint RTRelatedROISequence = 0x30060030;

		/// <summary>(3006,0033) VR=CS VM=1 RT ROI Relationship</summary>
		public const uint RTROIRelationship = 0x30060033;

		/// <summary>(3006,0036) VR=CS VM=1 ROI Generation Algorithm</summary>
		public const uint ROIGenerationAlgorithm = 0x30060036;

		/// <summary>(3006,0038) VR=LO VM=1 ROI Generation Description</summary>
		public const uint ROIGenerationDescription = 0x30060038;

		/// <summary>(3006,0039) VR=SQ VM=1 ROI Contour Sequence</summary>
		public const uint ROIContourSequence = 0x30060039;

		/// <summary>(3006,0040) VR=SQ VM=1 Contour Sequence</summary>
		public const uint ContourSequence = 0x30060040;

		/// <summary>(3006,0042) VR=CS VM=1 Contour Geometric Type</summary>
		public const uint ContourGeometricType = 0x30060042;

		/// <summary>(3006,0044) VR=DS VM=1 Contour Slab Thickness</summary>
		public const uint ContourSlabThickness = 0x30060044;

		/// <summary>(3006,0045) VR=DS VM=3 Contour Offset Vector</summary>
		public const uint ContourOffsetVector = 0x30060045;

		/// <summary>(3006,0046) VR=IS VM=1 Number of Contour Points</summary>
		public const uint NumberOfContourPoints = 0x30060046;

		/// <summary>(3006,0048) VR=IS VM=1 Contour Number</summary>
		public const uint ContourNumber = 0x30060048;

		/// <summary>(3006,0049) VR=IS VM=1-n Attached Contours</summary>
		public const uint AttachedContours = 0x30060049;

		/// <summary>(3006,0050) VR=DS VM=3-3n Contour Data</summary>
		public const uint ContourData = 0x30060050;

		/// <summary>(3006,0080) VR=SQ VM=1 RT ROI Observations Sequence</summary>
		public const uint RTROIObservationsSequence = 0x30060080;

		/// <summary>(3006,0082) VR=IS VM=1 Observation Number</summary>
		public const uint ObservationNumber = 0x30060082;

		/// <summary>(3006,0084) VR=IS VM=1 Referenced ROI Number</summary>
		public const uint ReferencedROINumber = 0x30060084;

		/// <summary>(3006,0085) VR=SH VM=1 ROI Observation Label</summary>
		public const uint ROIObservationLabel = 0x30060085;

		/// <summary>(3006,0086) VR=SQ VM=1 RT ROI Identification Code Sequence</summary>
		public const uint RTROIIdentificationCodeSequence = 0x30060086;

		/// <summary>(3006,0088) VR=ST VM=1 ROI Observation Description</summary>
		public const uint ROIObservationDescription = 0x30060088;

		/// <summary>(3006,00a0) VR=SQ VM=1 Related RT ROI Observations Sequence</summary>
		public const uint RelatedRTROIObservationsSequence = 0x300600a0;

		/// <summary>(3006,00a4) VR=CS VM=1 RT ROI Interpreted Type</summary>
		public const uint RTROIInterpretedType = 0x300600a4;

		/// <summary>(3006,00a6) VR=PN VM=1 ROI Interpreter</summary>
		public const uint ROIInterpreter = 0x300600a6;

		/// <summary>(3006,00b0) VR=SQ VM=1 ROI Physical Properties Sequence</summary>
		public const uint ROIPhysicalPropertiesSequence = 0x300600b0;

		/// <summary>(3006,00b2) VR=CS VM=1 ROI Physical Property</summary>
		public const uint ROIPhysicalProperty = 0x300600b2;

		/// <summary>(3006,00b4) VR=DS VM=1 ROI Physical Property Value</summary>
		public const uint ROIPhysicalPropertyValue = 0x300600b4;

		/// <summary>(3006,00b6) VR=SQ VM=1 ROI Elemental Composition Sequence</summary>
		public const uint ROIElementalCompositionSequence = 0x300600b6;

		/// <summary>(3006,00b7) VR=US VM=1 ROI Elemental Composition Atomic Number</summary>
		public const uint ROIElementalCompositionAtomicNumber = 0x300600b7;

		/// <summary>(3006,00b8) VR=FL VM=1 ROI Elemental Composition Atomic Mass Fraction</summary>
		public const uint ROIElementalCompositionAtomicMassFraction = 0x300600b8;

		/// <summary>(3006,00c0) VR=SQ VM=1 Frame of Reference Relationship Sequence</summary>
		public const uint FrameOfReferenceRelationshipSequence = 0x300600c0;

		/// <summary>(3006,00c2) VR=UI VM=1 Related Frame of Reference UID</summary>
		public const uint RelatedFrameOfReferenceUID = 0x300600c2;

		/// <summary>(3006,00c4) VR=CS VM=1 Frame of Reference Transformation Type</summary>
		public const uint FrameOfReferenceTransformationType = 0x300600c4;

		/// <summary>(3006,00c6) VR=DS VM=16 Frame of Reference Transformation Matrix</summary>
		public const uint FrameOfReferenceTransformationMatrix = 0x300600c6;

		/// <summary>(3006,00c8) VR=LO VM=1 Frame of Reference Transformation Comment</summary>
		public const uint FrameOfReferenceTransformationComment = 0x300600c8;

		/// <summary>(3008,0010) VR=SQ VM=1 Measured Dose Reference Sequence</summary>
		public const uint MeasuredDoseReferenceSequence = 0x30080010;

		/// <summary>(3008,0012) VR=ST VM=1 Measured Dose Description</summary>
		public const uint MeasuredDoseDescription = 0x30080012;

		/// <summary>(3008,0014) VR=CS VM=1 Measured Dose Type</summary>
		public const uint MeasuredDoseType = 0x30080014;

		/// <summary>(3008,0016) VR=DS VM=1 Measured Dose Value</summary>
		public const uint MeasuredDoseValue = 0x30080016;

		/// <summary>(3008,0020) VR=SQ VM=1 Treatment Session Beam Sequence</summary>
		public const uint TreatmentSessionBeamSequence = 0x30080020;

		/// <summary>(3008,0021) VR=SQ VM=1 Treatment Session Ion Beam Sequence</summary>
		public const uint TreatmentSessionIonBeamSequence = 0x30080021;

		/// <summary>(3008,0022) VR=IS VM=1 Current Fraction Number</summary>
		public const uint CurrentFractionNumber = 0x30080022;

		/// <summary>(3008,0024) VR=DA VM=1 Treatment Control Point Date</summary>
		public const uint TreatmentControlPointDate = 0x30080024;

		/// <summary>(3008,0025) VR=TM VM=1 Treatment Control Point Time</summary>
		public const uint TreatmentControlPointTime = 0x30080025;

		/// <summary>(3008,002a) VR=CS VM=1 Treatment Termination Status</summary>
		public const uint TreatmentTerminationStatus = 0x3008002a;

		/// <summary>(3008,002b) VR=SH VM=1 Treatment Termination Code</summary>
		public const uint TreatmentTerminationCode = 0x3008002b;

		/// <summary>(3008,002c) VR=CS VM=1 Treatment Verification Status</summary>
		public const uint TreatmentVerificationStatus = 0x3008002c;

		/// <summary>(3008,0030) VR=SQ VM=1 Referenced Treatment Record Sequence</summary>
		public const uint ReferencedTreatmentRecordSequence = 0x30080030;

		/// <summary>(3008,0032) VR=DS VM=1 Specified Primary Meterset</summary>
		public const uint SpecifiedPrimaryMeterset = 0x30080032;

		/// <summary>(3008,0033) VR=DS VM=1 Specified Secondary Meterset</summary>
		public const uint SpecifiedSecondaryMeterset = 0x30080033;

		/// <summary>(3008,0036) VR=DS VM=1 Delivered Primary Meterset</summary>
		public const uint DeliveredPrimaryMeterset = 0x30080036;

		/// <summary>(3008,0037) VR=DS VM=1 Delivered Secondary Meterset</summary>
		public const uint DeliveredSecondaryMeterset = 0x30080037;

		/// <summary>(3008,003a) VR=DS VM=1 Specified Treatment Time</summary>
		public const uint SpecifiedTreatmentTime = 0x3008003a;

		/// <summary>(3008,003b) VR=DS VM=1 Delivered Treatment Time</summary>
		public const uint DeliveredTreatmentTime = 0x3008003b;

		/// <summary>(3008,0040) VR=SQ VM=1 Control Point Delivery Sequence</summary>
		public const uint ControlPointDeliverySequence = 0x30080040;

		/// <summary>(3008,0041) VR=SQ VM=1 Ion Control Point Delivery Sequence</summary>
		public const uint IonControlPointDeliverySequence = 0x30080041;

		/// <summary>(3008,0042) VR=DS VM=1 Specified Meterset</summary>
		public const uint SpecifiedMeterset = 0x30080042;

		/// <summary>(3008,0044) VR=DS VM=1 Delivered Meterset</summary>
		public const uint DeliveredMeterset = 0x30080044;

		/// <summary>(3008,0045) VR=FL VM=1 Meterset Rate Set</summary>
		public const uint MetersetRateSet = 0x30080045;

		/// <summary>(3008,0046) VR=FL VM=1 Meterset Rate Delivered</summary>
		public const uint MetersetRateDelivered = 0x30080046;

		/// <summary>(3008,0047) VR=FL VM=1-n Scan Spot Metersets Delivered</summary>
		public const uint ScanSpotMetersetsDelivered = 0x30080047;

		/// <summary>(3008,0048) VR=DS VM=1 Dose Rate Delivered</summary>
		public const uint DoseRateDelivered = 0x30080048;

		/// <summary>(3008,0050) VR=SQ VM=1 Treatment Summary Calculated Dose Reference Sequence</summary>
		public const uint TreatmentSummaryCalculatedDoseReferenceSequence = 0x30080050;

		/// <summary>(3008,0052) VR=DS VM=1 Cumulative Dose to Dose Reference</summary>
		public const uint CumulativeDoseToDoseReference = 0x30080052;

		/// <summary>(3008,0054) VR=DA VM=1 First Treatment Date</summary>
		public const uint FirstTreatmentDate = 0x30080054;

		/// <summary>(3008,0056) VR=DA VM=1 Most Recent Treatment Date</summary>
		public const uint MostRecentTreatmentDate = 0x30080056;

		/// <summary>(3008,005a) VR=IS VM=1 Number of Fractions Delivered</summary>
		public const uint NumberOfFractionsDelivered = 0x3008005a;

		/// <summary>(3008,0060) VR=SQ VM=1 Override Sequence</summary>
		public const uint OverrideSequence = 0x30080060;

		/// <summary>(3008,0061) VR=AT VM=1 Parameter Sequence Pointer</summary>
		public const uint ParameterSequencePointer = 0x30080061;

		/// <summary>(3008,0062) VR=AT VM=1 Override Parameter Pointer</summary>
		public const uint OverrideParameterPointer = 0x30080062;

		/// <summary>(3008,0063) VR=IS VM=1 Parameter Item Index</summary>
		public const uint ParameterItemIndex = 0x30080063;

		/// <summary>(3008,0064) VR=IS VM=1 Measured Dose Reference Number</summary>
		public const uint MeasuredDoseReferenceNumber = 0x30080064;

		/// <summary>(3008,0065) VR=AT VM=1 Parameter Pointer</summary>
		public const uint ParameterPointer = 0x30080065;

		/// <summary>(3008,0066) VR=ST VM=1 Override Reason</summary>
		public const uint OverrideReason = 0x30080066;

		/// <summary>(3008,0068) VR=SQ VM=1 Corrected Parameter Sequence</summary>
		public const uint CorrectedParameterSequence = 0x30080068;

		/// <summary>(3008,006a) VR=FL VM=1 Correction Value</summary>
		public const uint CorrectionValue = 0x3008006a;

		/// <summary>(3008,0070) VR=SQ VM=1 Calculated Dose Reference Sequence</summary>
		public const uint CalculatedDoseReferenceSequence = 0x30080070;

		/// <summary>(3008,0072) VR=IS VM=1 Calculated Dose Reference Number</summary>
		public const uint CalculatedDoseReferenceNumber = 0x30080072;

		/// <summary>(3008,0074) VR=ST VM=1 Calculated Dose Reference Description</summary>
		public const uint CalculatedDoseReferenceDescription = 0x30080074;

		/// <summary>(3008,0076) VR=DS VM=1 Calculated Dose Reference Dose Value</summary>
		public const uint CalculatedDoseReferenceDoseValue = 0x30080076;

		/// <summary>(3008,0078) VR=DS VM=1 Start Meterset</summary>
		public const uint StartMeterset = 0x30080078;

		/// <summary>(3008,007a) VR=DS VM=1 End Meterset</summary>
		public const uint EndMeterset = 0x3008007a;

		/// <summary>(3008,0080) VR=SQ VM=1 Referenced Measured Dose Reference Sequence</summary>
		public const uint ReferencedMeasuredDoseReferenceSequence = 0x30080080;

		/// <summary>(3008,0082) VR=IS VM=1 Referenced Measured Dose Reference Number</summary>
		public const uint ReferencedMeasuredDoseReferenceNumber = 0x30080082;

		/// <summary>(3008,0090) VR=SQ VM=1 Referenced Calculated Dose Reference Sequence</summary>
		public const uint ReferencedCalculatedDoseReferenceSequence = 0x30080090;

		/// <summary>(3008,0092) VR=IS VM=1 Referenced Calculated Dose Reference Number</summary>
		public const uint ReferencedCalculatedDoseReferenceNumber = 0x30080092;

		/// <summary>(3008,00a0) VR=SQ VM=1 Beam Limiting Device Leaf Pairs Sequence</summary>
		public const uint BeamLimitingDeviceLeafPairsSequence = 0x300800a0;

		/// <summary>(3008,00b0) VR=SQ VM=1 Recorded Wedge Sequence</summary>
		public const uint RecordedWedgeSequence = 0x300800b0;

		/// <summary>(3008,00c0) VR=SQ VM=1 Recorded Compensator Sequence</summary>
		public const uint RecordedCompensatorSequence = 0x300800c0;

		/// <summary>(3008,00d0) VR=SQ VM=1 Recorded Block Sequence</summary>
		public const uint RecordedBlockSequence = 0x300800d0;

		/// <summary>(3008,00e0) VR=SQ VM=1 Treatment Summary Measured Dose Reference Sequence</summary>
		public const uint TreatmentSummaryMeasuredDoseReferenceSequence = 0x300800e0;

		/// <summary>(3008,00f0) VR=SQ VM=1 Recorded Snout Sequence</summary>
		public const uint RecordedSnoutSequence = 0x300800f0;

		/// <summary>(3008,00f2) VR=SQ VM=1 Recorded Range Shifter Sequence</summary>
		public const uint RecordedRangeShifterSequence = 0x300800f2;

		/// <summary>(3008,00f4) VR=SQ VM=1 Recorded Lateral Spreading Device Sequence</summary>
		public const uint RecordedLateralSpreadingDeviceSequence = 0x300800f4;

		/// <summary>(3008,00f6) VR=SQ VM=1 Recorded Range Modulator Sequence</summary>
		public const uint RecordedRangeModulatorSequence = 0x300800f6;

		/// <summary>(3008,0100) VR=SQ VM=1 Recorded Source Sequence</summary>
		public const uint RecordedSourceSequence = 0x30080100;

		/// <summary>(3008,0105) VR=LO VM=1 Source Serial Number</summary>
		public const uint SourceSerialNumber = 0x30080105;

		/// <summary>(3008,0110) VR=SQ VM=1 Treatment Session Application Setup Sequence</summary>
		public const uint TreatmentSessionApplicationSetupSequence = 0x30080110;

		/// <summary>(3008,0116) VR=CS VM=1 Application Setup Check</summary>
		public const uint ApplicationSetupCheck = 0x30080116;

		/// <summary>(3008,0120) VR=SQ VM=1 Recorded Brachy Accessory Device Sequence</summary>
		public const uint RecordedBrachyAccessoryDeviceSequence = 0x30080120;

		/// <summary>(3008,0122) VR=IS VM=1 Referenced Brachy Accessory Device Number</summary>
		public const uint ReferencedBrachyAccessoryDeviceNumber = 0x30080122;

		/// <summary>(3008,0130) VR=SQ VM=1 Recorded Channel Sequence</summary>
		public const uint RecordedChannelSequence = 0x30080130;

		/// <summary>(3008,0132) VR=DS VM=1 Specified Channel Total Time</summary>
		public const uint SpecifiedChannelTotalTime = 0x30080132;

		/// <summary>(3008,0134) VR=DS VM=1 Delivered Channel Total Time</summary>
		public const uint DeliveredChannelTotalTime = 0x30080134;

		/// <summary>(3008,0136) VR=IS VM=1 Specified Number of Pulses</summary>
		public const uint SpecifiedNumberOfPulses = 0x30080136;

		/// <summary>(3008,0138) VR=IS VM=1 Delivered Number of Pulses</summary>
		public const uint DeliveredNumberOfPulses = 0x30080138;

		/// <summary>(3008,013a) VR=DS VM=1 Specified Pulse Repetition Interval</summary>
		public const uint SpecifiedPulseRepetitionInterval = 0x3008013a;

		/// <summary>(3008,013c) VR=DS VM=1 Delivered Pulse Repetition Interval</summary>
		public const uint DeliveredPulseRepetitionInterval = 0x3008013c;

		/// <summary>(3008,0140) VR=SQ VM=1 Recorded Source Applicator Sequence</summary>
		public const uint RecordedSourceApplicatorSequence = 0x30080140;

		/// <summary>(3008,0142) VR=IS VM=1 Referenced Source Applicator Number</summary>
		public const uint ReferencedSourceApplicatorNumber = 0x30080142;

		/// <summary>(3008,0150) VR=SQ VM=1 Recorded Channel Shield Sequence</summary>
		public const uint RecordedChannelShieldSequence = 0x30080150;

		/// <summary>(3008,0152) VR=IS VM=1 Referenced Channel Shield Number</summary>
		public const uint ReferencedChannelShieldNumber = 0x30080152;

		/// <summary>(3008,0160) VR=SQ VM=1 Brachy Control Point Delivered Sequence</summary>
		public const uint BrachyControlPointDeliveredSequence = 0x30080160;

		/// <summary>(3008,0162) VR=DA VM=1 Safe Position Exit Date</summary>
		public const uint SafePositionExitDate = 0x30080162;

		/// <summary>(3008,0164) VR=TM VM=1 Safe Position Exit Time</summary>
		public const uint SafePositionExitTime = 0x30080164;

		/// <summary>(3008,0166) VR=DA VM=1 Safe Position Return Date</summary>
		public const uint SafePositionReturnDate = 0x30080166;

		/// <summary>(3008,0168) VR=TM VM=1 Safe Position Return Time</summary>
		public const uint SafePositionReturnTime = 0x30080168;

		/// <summary>(3008,0200) VR=CS VM=1 Current Treatment Status</summary>
		public const uint CurrentTreatmentStatus = 0x30080200;

		/// <summary>(3008,0202) VR=ST VM=1 Treatment Status Comment</summary>
		public const uint TreatmentStatusComment = 0x30080202;

		/// <summary>(3008,0220) VR=SQ VM=1 Fraction Group Summary Sequence</summary>
		public const uint FractionGroupSummarySequence = 0x30080220;

		/// <summary>(3008,0223) VR=IS VM=1 Referenced Fraction Number</summary>
		public const uint ReferencedFractionNumber = 0x30080223;

		/// <summary>(3008,0224) VR=CS VM=1 Fraction Group Type</summary>
		public const uint FractionGroupType = 0x30080224;

		/// <summary>(3008,0230) VR=CS VM=1 Beam Stopper Position</summary>
		public const uint BeamStopperPosition = 0x30080230;

		/// <summary>(3008,0240) VR=SQ VM=1 Fraction Status Summary Sequence</summary>
		public const uint FractionStatusSummarySequence = 0x30080240;

		/// <summary>(3008,0250) VR=DA VM=1 Treatment Date</summary>
		public const uint TreatmentDate = 0x30080250;

		/// <summary>(3008,0251) VR=TM VM=1 Treatment Time</summary>
		public const uint TreatmentTime = 0x30080251;

		/// <summary>(300a,0002) VR=SH VM=1 RT Plan Label</summary>
		public const uint RTPlanLabel = 0x300a0002;

		/// <summary>(300a,0003) VR=LO VM=1 RT Plan Name</summary>
		public const uint RTPlanName = 0x300a0003;

		/// <summary>(300a,0004) VR=ST VM=1 RT Plan Description</summary>
		public const uint RTPlanDescription = 0x300a0004;

		/// <summary>(300a,0006) VR=DA VM=1 RT Plan Date</summary>
		public const uint RTPlanDate = 0x300a0006;

		/// <summary>(300a,0007) VR=TM VM=1 RT Plan Time</summary>
		public const uint RTPlanTime = 0x300a0007;

		/// <summary>(300a,0009) VR=LO VM=1-n Treatment Protocols</summary>
		public const uint TreatmentProtocols = 0x300a0009;

		/// <summary>(300a,000a) VR=CS VM=1 Plan Intent</summary>
		public const uint PlanIntent = 0x300a000a;

		/// <summary>(300a,000b) VR=LO VM=1-n Treatment Sites</summary>
		public const uint TreatmentSites = 0x300a000b;

		/// <summary>(300a,000c) VR=CS VM=1 RT Plan Geometry</summary>
		public const uint RTPlanGeometry = 0x300a000c;

		/// <summary>(300a,000e) VR=ST VM=1 Prescription Description</summary>
		public const uint PrescriptionDescription = 0x300a000e;

		/// <summary>(300a,0010) VR=SQ VM=1 Dose Reference Sequence</summary>
		public const uint DoseReferenceSequence = 0x300a0010;

		/// <summary>(300a,0012) VR=IS VM=1 Dose Reference Number</summary>
		public const uint DoseReferenceNumber = 0x300a0012;

		/// <summary>(300a,0013) VR=UI VM=1 Dose Reference UID</summary>
		public const uint DoseReferenceUID = 0x300a0013;

		/// <summary>(300a,0014) VR=CS VM=1 Dose Reference Structure Type</summary>
		public const uint DoseReferenceStructureType = 0x300a0014;

		/// <summary>(300a,0015) VR=CS VM=1 Nominal Beam Energy Unit</summary>
		public const uint NominalBeamEnergyUnit = 0x300a0015;

		/// <summary>(300a,0016) VR=LO VM=1 Dose Reference Description</summary>
		public const uint DoseReferenceDescription = 0x300a0016;

		/// <summary>(300a,0018) VR=DS VM=3 Dose Reference Point Coordinates</summary>
		public const uint DoseReferencePointCoordinates = 0x300a0018;

		/// <summary>(300a,001a) VR=DS VM=1 Nominal Prior Dose</summary>
		public const uint NominalPriorDose = 0x300a001a;

		/// <summary>(300a,0020) VR=CS VM=1 Dose Reference Type</summary>
		public const uint DoseReferenceType = 0x300a0020;

		/// <summary>(300a,0021) VR=DS VM=1 Constraint Weight</summary>
		public const uint ConstraintWeight = 0x300a0021;

		/// <summary>(300a,0022) VR=DS VM=1 Delivery Warning Dose</summary>
		public const uint DeliveryWarningDose = 0x300a0022;

		/// <summary>(300a,0023) VR=DS VM=1 Delivery Maximum Dose</summary>
		public const uint DeliveryMaximumDose = 0x300a0023;

		/// <summary>(300a,0025) VR=DS VM=1 Target Minimum Dose</summary>
		public const uint TargetMinimumDose = 0x300a0025;

		/// <summary>(300a,0026) VR=DS VM=1 Target Prescription Dose</summary>
		public const uint TargetPrescriptionDose = 0x300a0026;

		/// <summary>(300a,0027) VR=DS VM=1 Target Maximum Dose</summary>
		public const uint TargetMaximumDose = 0x300a0027;

		/// <summary>(300a,0028) VR=DS VM=1 Target Underdose Volume Fraction</summary>
		public const uint TargetUnderdoseVolumeFraction = 0x300a0028;

		/// <summary>(300a,002a) VR=DS VM=1 Organ at Risk Full-volume Dose</summary>
		public const uint OrganAtRiskFullvolumeDose = 0x300a002a;

		/// <summary>(300a,002b) VR=DS VM=1 Organ at Risk Limit Dose</summary>
		public const uint OrganAtRiskLimitDose = 0x300a002b;

		/// <summary>(300a,002c) VR=DS VM=1 Organ at Risk Maximum Dose</summary>
		public const uint OrganAtRiskMaximumDose = 0x300a002c;

		/// <summary>(300a,002d) VR=DS VM=1 Organ at Risk Overdose Volume Fraction</summary>
		public const uint OrganAtRiskOverdoseVolumeFraction = 0x300a002d;

		/// <summary>(300a,0040) VR=SQ VM=1 Tolerance Table Sequence</summary>
		public const uint ToleranceTableSequence = 0x300a0040;

		/// <summary>(300a,0042) VR=IS VM=1 Tolerance Table Number</summary>
		public const uint ToleranceTableNumber = 0x300a0042;

		/// <summary>(300a,0043) VR=SH VM=1 Tolerance Table Label</summary>
		public const uint ToleranceTableLabel = 0x300a0043;

		/// <summary>(300a,0044) VR=DS VM=1 Gantry Angle Tolerance</summary>
		public const uint GantryAngleTolerance = 0x300a0044;

		/// <summary>(300a,0046) VR=DS VM=1 Beam Limiting Device Angle Tolerance</summary>
		public const uint BeamLimitingDeviceAngleTolerance = 0x300a0046;

		/// <summary>(300a,0048) VR=SQ VM=1 Beam Limiting Device Tolerance Sequence</summary>
		public const uint BeamLimitingDeviceToleranceSequence = 0x300a0048;

		/// <summary>(300a,004a) VR=DS VM=1 Beam Limiting Device Position Tolerance</summary>
		public const uint BeamLimitingDevicePositionTolerance = 0x300a004a;

		/// <summary>(300a,004b) VR=FL VM=1 Snout Position Tolerance</summary>
		public const uint SnoutPositionTolerance = 0x300a004b;

		/// <summary>(300a,004c) VR=DS VM=1 Patient Support Angle Tolerance</summary>
		public const uint PatientSupportAngleTolerance = 0x300a004c;

		/// <summary>(300a,004e) VR=DS VM=1 Table Top Eccentric Angle Tolerance</summary>
		public const uint TableTopEccentricAngleTolerance = 0x300a004e;

		/// <summary>(300a,004f) VR=FL VM=1 Table Top Pitch Angle Tolerance</summary>
		public const uint TableTopPitchAngleTolerance = 0x300a004f;

		/// <summary>(300a,0050) VR=FL VM=1 Table Top Roll Angle Tolerance</summary>
		public const uint TableTopRollAngleTolerance = 0x300a0050;

		/// <summary>(300a,0051) VR=DS VM=1 Table Top Vertical Position Tolerance</summary>
		public const uint TableTopVerticalPositionTolerance = 0x300a0051;

		/// <summary>(300a,0052) VR=DS VM=1 Table Top Longitudinal Position Tolerance</summary>
		public const uint TableTopLongitudinalPositionTolerance = 0x300a0052;

		/// <summary>(300a,0053) VR=DS VM=1 Table Top Lateral Position Tolerance</summary>
		public const uint TableTopLateralPositionTolerance = 0x300a0053;

		/// <summary>(300a,0055) VR=CS VM=1 RT Plan Relationship</summary>
		public const uint RTPlanRelationship = 0x300a0055;

		/// <summary>(300a,0070) VR=SQ VM=1 Fraction Group Sequence</summary>
		public const uint FractionGroupSequence = 0x300a0070;

		/// <summary>(300a,0071) VR=IS VM=1 Fraction Group Number</summary>
		public const uint FractionGroupNumber = 0x300a0071;

		/// <summary>(300a,0072) VR=LO VM=1 Fraction Group Description</summary>
		public const uint FractionGroupDescription = 0x300a0072;

		/// <summary>(300a,0078) VR=IS VM=1 Number of Fractions Planned</summary>
		public const uint NumberOfFractionsPlanned = 0x300a0078;

		/// <summary>(300a,0079) VR=IS VM=1 Number of Fraction Pattern Digits Per Day</summary>
		public const uint NumberOfFractionPatternDigitsPerDay = 0x300a0079;

		/// <summary>(300a,007a) VR=IS VM=1 Repeat Fraction Cycle Length</summary>
		public const uint RepeatFractionCycleLength = 0x300a007a;

		/// <summary>(300a,007b) VR=LT VM=1 Fraction Pattern</summary>
		public const uint FractionPattern = 0x300a007b;

		/// <summary>(300a,0080) VR=IS VM=1 Number of Beams</summary>
		public const uint NumberOfBeams = 0x300a0080;

		/// <summary>(300a,0082) VR=DS VM=3 Beam Dose Specification Point</summary>
		public const uint BeamDoseSpecificationPoint = 0x300a0082;

		/// <summary>(300a,0084) VR=DS VM=1 Beam Dose</summary>
		public const uint BeamDose = 0x300a0084;

		/// <summary>(300a,0086) VR=DS VM=1 Beam Meterset</summary>
		public const uint BeamMeterset = 0x300a0086;

		/// <summary>(300a,0088) VR=FL VM=1 Beam Dose Point Depth</summary>
		public const uint BeamDosePointDepth = 0x300a0088;

		/// <summary>(300a,0089) VR=FL VM=1 Beam Dose Point Equivalent Depth</summary>
		public const uint BeamDosePointEquivalentDepth = 0x300a0089;

		/// <summary>(300a,008a) VR=FL VM=1 Beam Dose Point SSD</summary>
		public const uint BeamDosePointSSD = 0x300a008a;

		/// <summary>(300a,00a0) VR=IS VM=1 Number of Brachy Application Setups</summary>
		public const uint NumberOfBrachyApplicationSetups = 0x300a00a0;

		/// <summary>(300a,00a2) VR=DS VM=3 Brachy Application Setup Dose Specification Point</summary>
		public const uint BrachyApplicationSetupDoseSpecificationPoint = 0x300a00a2;

		/// <summary>(300a,00a4) VR=DS VM=1 Brachy Application Setup Dose</summary>
		public const uint BrachyApplicationSetupDose = 0x300a00a4;

		/// <summary>(300a,00b0) VR=SQ VM=1 Beam Sequence</summary>
		public const uint BeamSequence = 0x300a00b0;

		/// <summary>(300a,00b2) VR=SH VM=1 Treatment Machine Name</summary>
		public const uint TreatmentMachineName = 0x300a00b2;

		/// <summary>(300a,00b3) VR=CS VM=1 Primary Dosimeter Unit</summary>
		public const uint PrimaryDosimeterUnit = 0x300a00b3;

		/// <summary>(300a,00b4) VR=DS VM=1 Source-Axis Distance</summary>
		public const uint SourceAxisDistance = 0x300a00b4;

		/// <summary>(300a,00b6) VR=SQ VM=1 Beam Limiting Device Sequence</summary>
		public const uint BeamLimitingDeviceSequence = 0x300a00b6;

		/// <summary>(300a,00b8) VR=CS VM=1 RT Beam Limiting Device Type</summary>
		public const uint RTBeamLimitingDeviceType = 0x300a00b8;

		/// <summary>(300a,00ba) VR=DS VM=1 Source to Beam Limiting Device Distance</summary>
		public const uint SourceToBeamLimitingDeviceDistance = 0x300a00ba;

		/// <summary>(300a,00bb) VR=FL VM=1 Isocenter to Beam Limiting Device Distance</summary>
		public const uint IsocenterToBeamLimitingDeviceDistance = 0x300a00bb;

		/// <summary>(300a,00bc) VR=IS VM=1 Number of Leaf/Jaw Pairs</summary>
		public const uint NumberOfLeafJawPairs = 0x300a00bc;

		/// <summary>(300a,00be) VR=DS VM=3-n Leaf Position Boundaries</summary>
		public const uint LeafPositionBoundaries = 0x300a00be;

		/// <summary>(300a,00c0) VR=IS VM=1 Beam Number</summary>
		public const uint BeamNumber = 0x300a00c0;

		/// <summary>(300a,00c2) VR=LO VM=1 Beam Name</summary>
		public const uint BeamName = 0x300a00c2;

		/// <summary>(300a,00c3) VR=ST VM=1 Beam Description</summary>
		public const uint BeamDescription = 0x300a00c3;

		/// <summary>(300a,00c4) VR=CS VM=1 Beam Type</summary>
		public const uint BeamType = 0x300a00c4;

		/// <summary>(300a,00c6) VR=CS VM=1 Radiation Type</summary>
		public const uint RadiationType = 0x300a00c6;

		/// <summary>(300a,00c7) VR=CS VM=1 High-Dose Technique Type</summary>
		public const uint HighDoseTechniqueType = 0x300a00c7;

		/// <summary>(300a,00c8) VR=IS VM=1 Reference Image Number</summary>
		public const uint ReferenceImageNumber = 0x300a00c8;

		/// <summary>(300a,00ca) VR=SQ VM=1 Planned Verification Image Sequence</summary>
		public const uint PlannedVerificationImageSequence = 0x300a00ca;

		/// <summary>(300a,00cc) VR=LO VM=1-n Imaging Device-Specific Acquisition Parameters</summary>
		public const uint ImagingDeviceSpecificAcquisitionParameters = 0x300a00cc;

		/// <summary>(300a,00ce) VR=CS VM=1 Treatment Delivery Type</summary>
		public const uint TreatmentDeliveryType = 0x300a00ce;

		/// <summary>(300a,00d0) VR=IS VM=1 Number of Wedges</summary>
		public const uint NumberOfWedges = 0x300a00d0;

		/// <summary>(300a,00d1) VR=SQ VM=1 Wedge Sequence</summary>
		public const uint WedgeSequence = 0x300a00d1;

		/// <summary>(300a,00d2) VR=IS VM=1 Wedge Number</summary>
		public const uint WedgeNumber = 0x300a00d2;

		/// <summary>(300a,00d3) VR=CS VM=1 Wedge Type</summary>
		public const uint WedgeType = 0x300a00d3;

		/// <summary>(300a,00d4) VR=SH VM=1 Wedge ID</summary>
		public const uint WedgeID = 0x300a00d4;

		/// <summary>(300a,00d5) VR=IS VM=1 Wedge Angle</summary>
		public const uint WedgeAngle = 0x300a00d5;

		/// <summary>(300a,00d6) VR=DS VM=1 Wedge Factor</summary>
		public const uint WedgeFactor = 0x300a00d6;

		/// <summary>(300a,00d7) VR=FL VM=1 Total Wedge Tray Water-Equivalent Thickness</summary>
		public const uint TotalWedgeTrayWaterEquivalentThickness = 0x300a00d7;

		/// <summary>(300a,00d8) VR=DS VM=1 Wedge Orientation</summary>
		public const uint WedgeOrientation = 0x300a00d8;

		/// <summary>(300a,00d9) VR=FL VM=1 Isocenter to Wedge Tray Distance</summary>
		public const uint IsocenterToWedgeTrayDistance = 0x300a00d9;

		/// <summary>(300a,00da) VR=DS VM=1 Source to Wedge Tray Distance</summary>
		public const uint SourceToWedgeTrayDistance = 0x300a00da;

		/// <summary>(300a,00db) VR=FL VM=1 Wedge Thin Edge Position</summary>
		public const uint WedgeThinEdgePosition = 0x300a00db;

		/// <summary>(300a,00dc) VR=SH VM=1 Bolus ID</summary>
		public const uint BolusID = 0x300a00dc;

		/// <summary>(300a,00dd) VR=ST VM=1 Bolus Description</summary>
		public const uint BolusDescription = 0x300a00dd;

		/// <summary>(300a,00e0) VR=IS VM=1 Number of Compensators</summary>
		public const uint NumberOfCompensators = 0x300a00e0;

		/// <summary>(300a,00e1) VR=SH VM=1 Material ID</summary>
		public const uint MaterialID = 0x300a00e1;

		/// <summary>(300a,00e2) VR=DS VM=1 Total Compensator Tray Factor</summary>
		public const uint TotalCompensatorTrayFactor = 0x300a00e2;

		/// <summary>(300a,00e3) VR=SQ VM=1 Compensator Sequence</summary>
		public const uint CompensatorSequence = 0x300a00e3;

		/// <summary>(300a,00e4) VR=IS VM=1 Compensator Number</summary>
		public const uint CompensatorNumber = 0x300a00e4;

		/// <summary>(300a,00e5) VR=SH VM=1 Compensator ID</summary>
		public const uint CompensatorID = 0x300a00e5;

		/// <summary>(300a,00e6) VR=DS VM=1 Source to Compensator Tray Distance</summary>
		public const uint SourceToCompensatorTrayDistance = 0x300a00e6;

		/// <summary>(300a,00e7) VR=IS VM=1 Compensator Rows</summary>
		public const uint CompensatorRows = 0x300a00e7;

		/// <summary>(300a,00e8) VR=IS VM=1 Compensator Columns</summary>
		public const uint CompensatorColumns = 0x300a00e8;

		/// <summary>(300a,00e9) VR=DS VM=2 Compensator Pixel Spacing</summary>
		public const uint CompensatorPixelSpacing = 0x300a00e9;

		/// <summary>(300a,00ea) VR=DS VM=2 Compensator Position</summary>
		public const uint CompensatorPosition = 0x300a00ea;

		/// <summary>(300a,00eb) VR=DS VM=1-n Compensator Transmission Data</summary>
		public const uint CompensatorTransmissionData = 0x300a00eb;

		/// <summary>(300a,00ec) VR=DS VM=1-n Compensator Thickness Data</summary>
		public const uint CompensatorThicknessData = 0x300a00ec;

		/// <summary>(300a,00ed) VR=IS VM=1 Number of Boli</summary>
		public const uint NumberOfBoli = 0x300a00ed;

		/// <summary>(300a,00ee) VR=CS VM=1 Compensator Type</summary>
		public const uint CompensatorType = 0x300a00ee;

		/// <summary>(300a,00f0) VR=IS VM=1 Number of Blocks</summary>
		public const uint NumberOfBlocks = 0x300a00f0;

		/// <summary>(300a,00f2) VR=DS VM=1 Total Block Tray Factor</summary>
		public const uint TotalBlockTrayFactor = 0x300a00f2;

		/// <summary>(300a,00f3) VR=FL VM=1 Total Block Tray Water-Equivalent Thickness</summary>
		public const uint TotalBlockTrayWaterEquivalentThickness = 0x300a00f3;

		/// <summary>(300a,00f4) VR=SQ VM=1 Block Sequence</summary>
		public const uint BlockSequence = 0x300a00f4;

		/// <summary>(300a,00f5) VR=SH VM=1 Block Tray ID</summary>
		public const uint BlockTrayID = 0x300a00f5;

		/// <summary>(300a,00f6) VR=DS VM=1 Source to Block Tray Distance</summary>
		public const uint SourceToBlockTrayDistance = 0x300a00f6;

		/// <summary>(300a,00f7) VR=FL VM=1 Isocenter to Block Tray Distance</summary>
		public const uint IsocenterToBlockTrayDistance = 0x300a00f7;

		/// <summary>(300a,00f8) VR=CS VM=1 Block Type</summary>
		public const uint BlockType = 0x300a00f8;

		/// <summary>(300a,00f9) VR=LO VM=1 Accessory Code</summary>
		public const uint AccessoryCode = 0x300a00f9;

		/// <summary>(300a,00fa) VR=CS VM=1 Block Divergence</summary>
		public const uint BlockDivergence = 0x300a00fa;

		/// <summary>(300a,00fb) VR=CS VM=1 Block Mounting Position</summary>
		public const uint BlockMountingPosition = 0x300a00fb;

		/// <summary>(300a,00fc) VR=IS VM=1 Block Number</summary>
		public const uint BlockNumber = 0x300a00fc;

		/// <summary>(300a,00fe) VR=LO VM=1 Block Name</summary>
		public const uint BlockName = 0x300a00fe;

		/// <summary>(300a,0100) VR=DS VM=1 Block Thickness</summary>
		public const uint BlockThickness = 0x300a0100;

		/// <summary>(300a,0102) VR=DS VM=1 Block Transmission</summary>
		public const uint BlockTransmission = 0x300a0102;

		/// <summary>(300a,0104) VR=IS VM=1 Block Number of Points</summary>
		public const uint BlockNumberOfPoints = 0x300a0104;

		/// <summary>(300a,0106) VR=DS VM=2-2n Block Data</summary>
		public const uint BlockData = 0x300a0106;

		/// <summary>(300a,0107) VR=SQ VM=1 Applicator Sequence</summary>
		public const uint ApplicatorSequence = 0x300a0107;

		/// <summary>(300a,0108) VR=SH VM=1 Applicator ID</summary>
		public const uint ApplicatorID = 0x300a0108;

		/// <summary>(300a,0109) VR=CS VM=1 Applicator Type</summary>
		public const uint ApplicatorType = 0x300a0109;

		/// <summary>(300a,010a) VR=LO VM=1 Applicator Description</summary>
		public const uint ApplicatorDescription = 0x300a010a;

		/// <summary>(300a,010c) VR=DS VM=1 Cumulative Dose Reference Coefficient</summary>
		public const uint CumulativeDoseReferenceCoefficient = 0x300a010c;

		/// <summary>(300a,010e) VR=DS VM=1 Final Cumulative Meterset Weight</summary>
		public const uint FinalCumulativeMetersetWeight = 0x300a010e;

		/// <summary>(300a,0110) VR=IS VM=1 Number of Control Points</summary>
		public const uint NumberOfControlPoints = 0x300a0110;

		/// <summary>(300a,0111) VR=SQ VM=1 Control Point Sequence</summary>
		public const uint ControlPointSequence = 0x300a0111;

		/// <summary>(300a,0112) VR=IS VM=1 Control Point Index</summary>
		public const uint ControlPointIndex = 0x300a0112;

		/// <summary>(300a,0114) VR=DS VM=1 Nominal Beam Energy</summary>
		public const uint NominalBeamEnergy = 0x300a0114;

		/// <summary>(300a,0115) VR=DS VM=1 Dose Rate Set</summary>
		public const uint DoseRateSet = 0x300a0115;

		/// <summary>(300a,0116) VR=SQ VM=1 Wedge Position Sequence</summary>
		public const uint WedgePositionSequence = 0x300a0116;

		/// <summary>(300a,0118) VR=CS VM=1 Wedge Position</summary>
		public const uint WedgePosition = 0x300a0118;

		/// <summary>(300a,011a) VR=SQ VM=1 Beam Limiting Device Position Sequence</summary>
		public const uint BeamLimitingDevicePositionSequence = 0x300a011a;

		/// <summary>(300a,011c) VR=DS VM=2-2n Leaf/Jaw Positions</summary>
		public const uint LeafJawPositions = 0x300a011c;

		/// <summary>(300a,011e) VR=DS VM=1 Gantry Angle</summary>
		public const uint GantryAngle = 0x300a011e;

		/// <summary>(300a,011f) VR=CS VM=1 Gantry Rotation Direction</summary>
		public const uint GantryRotationDirection = 0x300a011f;

		/// <summary>(300a,0120) VR=DS VM=1 Beam Limiting Device Angle</summary>
		public const uint BeamLimitingDeviceAngle = 0x300a0120;

		/// <summary>(300a,0121) VR=CS VM=1 Beam Limiting Device Rotation Direction</summary>
		public const uint BeamLimitingDeviceRotationDirection = 0x300a0121;

		/// <summary>(300a,0122) VR=DS VM=1 Patient Support Angle</summary>
		public const uint PatientSupportAngle = 0x300a0122;

		/// <summary>(300a,0123) VR=CS VM=1 Patient Support Rotation Direction</summary>
		public const uint PatientSupportRotationDirection = 0x300a0123;

		/// <summary>(300a,0124) VR=DS VM=1 Table Top Eccentric Axis Distance</summary>
		public const uint TableTopEccentricAxisDistance = 0x300a0124;

		/// <summary>(300a,0125) VR=DS VM=1 Table Top Eccentric Angle</summary>
		public const uint TableTopEccentricAngle = 0x300a0125;

		/// <summary>(300a,0126) VR=CS VM=1 Table Top Eccentric Rotation Direction</summary>
		public const uint TableTopEccentricRotationDirection = 0x300a0126;

		/// <summary>(300a,0128) VR=DS VM=1 Table Top Vertical Position</summary>
		public const uint TableTopVerticalPosition = 0x300a0128;

		/// <summary>(300a,0129) VR=DS VM=1 Table Top Longitudinal Position</summary>
		public const uint TableTopLongitudinalPosition = 0x300a0129;

		/// <summary>(300a,012a) VR=DS VM=1 Table Top Lateral Position</summary>
		public const uint TableTopLateralPosition = 0x300a012a;

		/// <summary>(300a,012c) VR=DS VM=3 Isocenter Position</summary>
		public const uint IsocenterPosition = 0x300a012c;

		/// <summary>(300a,012e) VR=DS VM=3 Surface Entry Point</summary>
		public const uint SurfaceEntryPoint = 0x300a012e;

		/// <summary>(300a,0130) VR=DS VM=1 Source to Surface Distance</summary>
		public const uint SourceToSurfaceDistance = 0x300a0130;

		/// <summary>(300a,0134) VR=DS VM=1 Cumulative Meterset Weight</summary>
		public const uint CumulativeMetersetWeight = 0x300a0134;

		/// <summary>(300a,0140) VR=FL VM=1 Table Top Pitch Angle</summary>
		public const uint TableTopPitchAngle = 0x300a0140;

		/// <summary>(300a,0142) VR=CS VM=1 Table Top Pitch Rotation Direction</summary>
		public const uint TableTopPitchRotationDirection = 0x300a0142;

		/// <summary>(300a,0144) VR=FL VM=1 Table Top Roll Angle</summary>
		public const uint TableTopRollAngle = 0x300a0144;

		/// <summary>(300a,0146) VR=CS VM=1 Table Top Roll Rotation Direction</summary>
		public const uint TableTopRollRotationDirection = 0x300a0146;

		/// <summary>(300a,0148) VR=FL VM=1 Head Fixation Angle</summary>
		public const uint HeadFixationAngle = 0x300a0148;

		/// <summary>(300a,014a) VR=FL VM=1 Gantry Pitch Angle</summary>
		public const uint GantryPitchAngle = 0x300a014a;

		/// <summary>(300a,014c) VR=CS VM=1 Gantry Pitch Rotation Direction</summary>
		public const uint GantryPitchRotationDirection = 0x300a014c;

		/// <summary>(300a,014e) VR=FL VM=1 Gantry Pitch Angle Tolerance</summary>
		public const uint GantryPitchAngleTolerance = 0x300a014e;

		/// <summary>(300a,0180) VR=SQ VM=1 Patient Setup Sequence</summary>
		public const uint PatientSetupSequence = 0x300a0180;

		/// <summary>(300a,0182) VR=IS VM=1 Patient Setup Number</summary>
		public const uint PatientSetupNumber = 0x300a0182;

		/// <summary>(300a,0183) VR=LO VM=1 Patient Setup Label</summary>
		public const uint PatientSetupLabel = 0x300a0183;

		/// <summary>(300a,0184) VR=LO VM=1 Patient Additional Position</summary>
		public const uint PatientAdditionalPosition = 0x300a0184;

		/// <summary>(300a,0190) VR=SQ VM=1 Fixation Device Sequence</summary>
		public const uint FixationDeviceSequence = 0x300a0190;

		/// <summary>(300a,0192) VR=CS VM=1 Fixation Device Type</summary>
		public const uint FixationDeviceType = 0x300a0192;

		/// <summary>(300a,0194) VR=SH VM=1 Fixation Device Label</summary>
		public const uint FixationDeviceLabel = 0x300a0194;

		/// <summary>(300a,0196) VR=ST VM=1 Fixation Device Description</summary>
		public const uint FixationDeviceDescription = 0x300a0196;

		/// <summary>(300a,0198) VR=SH VM=1 Fixation Device Position</summary>
		public const uint FixationDevicePosition = 0x300a0198;

		/// <summary>(300a,0199) VR=FL VM=1 Fixation Device Pitch Angle</summary>
		public const uint FixationDevicePitchAngle = 0x300a0199;

		/// <summary>(300a,019a) VR=FL VM=1 Fixation Device Roll Angle</summary>
		public const uint FixationDeviceRollAngle = 0x300a019a;

		/// <summary>(300a,01a0) VR=SQ VM=1 Shielding Device Sequence</summary>
		public const uint ShieldingDeviceSequence = 0x300a01a0;

		/// <summary>(300a,01a2) VR=CS VM=1 Shielding Device Type</summary>
		public const uint ShieldingDeviceType = 0x300a01a2;

		/// <summary>(300a,01a4) VR=SH VM=1 Shielding Device Label</summary>
		public const uint ShieldingDeviceLabel = 0x300a01a4;

		/// <summary>(300a,01a6) VR=ST VM=1 Shielding Device Description</summary>
		public const uint ShieldingDeviceDescription = 0x300a01a6;

		/// <summary>(300a,01a8) VR=SH VM=1 Shielding Device Position</summary>
		public const uint ShieldingDevicePosition = 0x300a01a8;

		/// <summary>(300a,01b0) VR=CS VM=1 Setup Technique</summary>
		public const uint SetupTechnique = 0x300a01b0;

		/// <summary>(300a,01b2) VR=ST VM=1 Setup Technique Description</summary>
		public const uint SetupTechniqueDescription = 0x300a01b2;

		/// <summary>(300a,01b4) VR=SQ VM=1 Setup Device Sequence</summary>
		public const uint SetupDeviceSequence = 0x300a01b4;

		/// <summary>(300a,01b6) VR=CS VM=1 Setup Device Type</summary>
		public const uint SetupDeviceType = 0x300a01b6;

		/// <summary>(300a,01b8) VR=SH VM=1 Setup Device Label</summary>
		public const uint SetupDeviceLabel = 0x300a01b8;

		/// <summary>(300a,01ba) VR=ST VM=1 Setup Device Description</summary>
		public const uint SetupDeviceDescription = 0x300a01ba;

		/// <summary>(300a,01bc) VR=DS VM=1 Setup Device Parameter</summary>
		public const uint SetupDeviceParameter = 0x300a01bc;

		/// <summary>(300a,01d0) VR=ST VM=1 Setup Reference Description</summary>
		public const uint SetupReferenceDescription = 0x300a01d0;

		/// <summary>(300a,01d2) VR=DS VM=1 Table Top Vertical Setup Displacement</summary>
		public const uint TableTopVerticalSetupDisplacement = 0x300a01d2;

		/// <summary>(300a,01d4) VR=DS VM=1 Table Top Longitudinal Setup Displacement</summary>
		public const uint TableTopLongitudinalSetupDisplacement = 0x300a01d4;

		/// <summary>(300a,01d6) VR=DS VM=1 Table Top Lateral Setup Displacement</summary>
		public const uint TableTopLateralSetupDisplacement = 0x300a01d6;

		/// <summary>(300a,0200) VR=CS VM=1 Brachy Treatment Technique</summary>
		public const uint BrachyTreatmentTechnique = 0x300a0200;

		/// <summary>(300a,0202) VR=CS VM=1 Brachy Treatment Type</summary>
		public const uint BrachyTreatmentType = 0x300a0202;

		/// <summary>(300a,0206) VR=SQ VM=1 Treatment Machine Sequence</summary>
		public const uint TreatmentMachineSequence = 0x300a0206;

		/// <summary>(300a,0210) VR=SQ VM=1 Source Sequence</summary>
		public const uint SourceSequence = 0x300a0210;

		/// <summary>(300a,0212) VR=IS VM=1 Source Number</summary>
		public const uint SourceNumber = 0x300a0212;

		/// <summary>(300a,0214) VR=CS VM=1 Source Type</summary>
		public const uint SourceType = 0x300a0214;

		/// <summary>(300a,0216) VR=LO VM=1 Source Manufacturer</summary>
		public const uint SourceManufacturer = 0x300a0216;

		/// <summary>(300a,0218) VR=DS VM=1 Active Source Diameter</summary>
		public const uint ActiveSourceDiameter = 0x300a0218;

		/// <summary>(300a,021a) VR=DS VM=1 Active Source Length</summary>
		public const uint ActiveSourceLength = 0x300a021a;

		/// <summary>(300a,0222) VR=DS VM=1 Source Encapsulation Nominal Thickness</summary>
		public const uint SourceEncapsulationNominalThickness = 0x300a0222;

		/// <summary>(300a,0224) VR=DS VM=1 Source Encapsulation Nominal Transmission</summary>
		public const uint SourceEncapsulationNominalTransmission = 0x300a0224;

		/// <summary>(300a,0226) VR=LO VM=1 Source Isotope Name</summary>
		public const uint SourceIsotopeName = 0x300a0226;

		/// <summary>(300a,0228) VR=DS VM=1 Source Isotope Half Life</summary>
		public const uint SourceIsotopeHalfLife = 0x300a0228;

		/// <summary>(300a,0229) VR=CS VM=1 Source Strength Units</summary>
		public const uint SourceStrengthUnits = 0x300a0229;

		/// <summary>(300a,022a) VR=DS VM=1 Reference Air Kerma Rate</summary>
		public const uint ReferenceAirKermaRate = 0x300a022a;

		/// <summary>(300a,022b) VR=DS VM=1 Source Strength</summary>
		public const uint SourceStrength = 0x300a022b;

		/// <summary>(300a,022c) VR=DA VM=1 Source Strength Reference Date</summary>
		public const uint SourceStrengthReferenceDate = 0x300a022c;

		/// <summary>(300a,022e) VR=TM VM=1 Source Strength Reference Time</summary>
		public const uint SourceStrengthReferenceTime = 0x300a022e;

		/// <summary>(300a,0230) VR=SQ VM=1 Application Setup Sequence</summary>
		public const uint ApplicationSetupSequence = 0x300a0230;

		/// <summary>(300a,0232) VR=CS VM=1 Application Setup Type</summary>
		public const uint ApplicationSetupType = 0x300a0232;

		/// <summary>(300a,0234) VR=IS VM=1 Application Setup Number</summary>
		public const uint ApplicationSetupNumber = 0x300a0234;

		/// <summary>(300a,0236) VR=LO VM=1 Application Setup Name</summary>
		public const uint ApplicationSetupName = 0x300a0236;

		/// <summary>(300a,0238) VR=LO VM=1 Application Setup Manufacturer</summary>
		public const uint ApplicationSetupManufacturer = 0x300a0238;

		/// <summary>(300a,0240) VR=IS VM=1 Template Number</summary>
		public const uint TemplateNumber = 0x300a0240;

		/// <summary>(300a,0242) VR=SH VM=1 Template Type</summary>
		public const uint TemplateType = 0x300a0242;

		/// <summary>(300a,0244) VR=LO VM=1 Template Name</summary>
		public const uint TemplateName = 0x300a0244;

		/// <summary>(300a,0250) VR=DS VM=1 Total Reference Air Kerma</summary>
		public const uint TotalReferenceAirKerma = 0x300a0250;

		/// <summary>(300a,0260) VR=SQ VM=1 Brachy Accessory Device Sequence</summary>
		public const uint BrachyAccessoryDeviceSequence = 0x300a0260;

		/// <summary>(300a,0262) VR=IS VM=1 Brachy Accessory Device Number</summary>
		public const uint BrachyAccessoryDeviceNumber = 0x300a0262;

		/// <summary>(300a,0263) VR=SH VM=1 Brachy Accessory Device ID</summary>
		public const uint BrachyAccessoryDeviceID = 0x300a0263;

		/// <summary>(300a,0264) VR=CS VM=1 Brachy Accessory Device Type</summary>
		public const uint BrachyAccessoryDeviceType = 0x300a0264;

		/// <summary>(300a,0266) VR=LO VM=1 Brachy Accessory Device Name</summary>
		public const uint BrachyAccessoryDeviceName = 0x300a0266;

		/// <summary>(300a,026a) VR=DS VM=1 Brachy Accessory Device Nominal Thickness</summary>
		public const uint BrachyAccessoryDeviceNominalThickness = 0x300a026a;

		/// <summary>(300a,026c) VR=DS VM=1 Brachy Accessory Device Nominal Transmission</summary>
		public const uint BrachyAccessoryDeviceNominalTransmission = 0x300a026c;

		/// <summary>(300a,0280) VR=SQ VM=1 Channel Sequence</summary>
		public const uint ChannelSequence = 0x300a0280;

		/// <summary>(300a,0282) VR=IS VM=1 Channel Number</summary>
		public const uint ChannelNumber = 0x300a0282;

		/// <summary>(300a,0284) VR=DS VM=1 Channel Length</summary>
		public const uint ChannelLength = 0x300a0284;

		/// <summary>(300a,0286) VR=DS VM=1 Channel Total Time</summary>
		public const uint ChannelTotalTime = 0x300a0286;

		/// <summary>(300a,0288) VR=CS VM=1 Source Movement Type</summary>
		public const uint SourceMovementType = 0x300a0288;

		/// <summary>(300a,028a) VR=IS VM=1 Number of Pulses</summary>
		public const uint NumberOfPulses = 0x300a028a;

		/// <summary>(300a,028c) VR=DS VM=1 Pulse Repetition Interval</summary>
		public const uint PulseRepetitionInterval = 0x300a028c;

		/// <summary>(300a,0290) VR=IS VM=1 Source Applicator Number</summary>
		public const uint SourceApplicatorNumber = 0x300a0290;

		/// <summary>(300a,0291) VR=SH VM=1 Source Applicator ID</summary>
		public const uint SourceApplicatorID = 0x300a0291;

		/// <summary>(300a,0292) VR=CS VM=1 Source Applicator Type</summary>
		public const uint SourceApplicatorType = 0x300a0292;

		/// <summary>(300a,0294) VR=LO VM=1 Source Applicator Name</summary>
		public const uint SourceApplicatorName = 0x300a0294;

		/// <summary>(300a,0296) VR=DS VM=1 Source Applicator Length</summary>
		public const uint SourceApplicatorLength = 0x300a0296;

		/// <summary>(300a,0298) VR=LO VM=1 Source Applicator Manufacturer</summary>
		public const uint SourceApplicatorManufacturer = 0x300a0298;

		/// <summary>(300a,029c) VR=DS VM=1 Source Applicator Wall Nominal Thickness</summary>
		public const uint SourceApplicatorWallNominalThickness = 0x300a029c;

		/// <summary>(300a,029e) VR=DS VM=1 Source Applicator Wall Nominal Transmission</summary>
		public const uint SourceApplicatorWallNominalTransmission = 0x300a029e;

		/// <summary>(300a,02a0) VR=DS VM=1 Source Applicator Step Size</summary>
		public const uint SourceApplicatorStepSize = 0x300a02a0;

		/// <summary>(300a,02a2) VR=IS VM=1 Transfer Tube Number</summary>
		public const uint TransferTubeNumber = 0x300a02a2;

		/// <summary>(300a,02a4) VR=DS VM=1 Transfer Tube Length</summary>
		public const uint TransferTubeLength = 0x300a02a4;

		/// <summary>(300a,02b0) VR=SQ VM=1 Channel Shield Sequence</summary>
		public const uint ChannelShieldSequence = 0x300a02b0;

		/// <summary>(300a,02b2) VR=IS VM=1 Channel Shield Number</summary>
		public const uint ChannelShieldNumber = 0x300a02b2;

		/// <summary>(300a,02b3) VR=SH VM=1 Channel Shield ID</summary>
		public const uint ChannelShieldID = 0x300a02b3;

		/// <summary>(300a,02b4) VR=LO VM=1 Channel Shield Name</summary>
		public const uint ChannelShieldName = 0x300a02b4;

		/// <summary>(300a,02b8) VR=DS VM=1 Channel Shield Nominal Thickness</summary>
		public const uint ChannelShieldNominalThickness = 0x300a02b8;

		/// <summary>(300a,02ba) VR=DS VM=1 Channel Shield Nominal Transmission</summary>
		public const uint ChannelShieldNominalTransmission = 0x300a02ba;

		/// <summary>(300a,02c8) VR=DS VM=1 Final Cumulative Time Weight</summary>
		public const uint FinalCumulativeTimeWeight = 0x300a02c8;

		/// <summary>(300a,02d0) VR=SQ VM=1 Brachy Control Point Sequence</summary>
		public const uint BrachyControlPointSequence = 0x300a02d0;

		/// <summary>(300a,02d2) VR=DS VM=1 Control Point Relative Position</summary>
		public const uint ControlPointRelativePosition = 0x300a02d2;

		/// <summary>(300a,02d4) VR=DS VM=3 Control Point 3D Position</summary>
		public const uint ControlPoint3DPosition = 0x300a02d4;

		/// <summary>(300a,02d6) VR=DS VM=1 Cumulative Time Weight</summary>
		public const uint CumulativeTimeWeight = 0x300a02d6;

		/// <summary>(300a,02e0) VR=CS VM=1 Compensator Divergence</summary>
		public const uint CompensatorDivergence = 0x300a02e0;

		/// <summary>(300a,02e1) VR=CS VM=1 Compensator Mounting Position</summary>
		public const uint CompensatorMountingPosition = 0x300a02e1;

		/// <summary>(300a,02e2) VR=DS VM=1-n Source to Compensator Distance</summary>
		public const uint SourceToCompensatorDistance = 0x300a02e2;

		/// <summary>(300a,02e3) VR=FL VM=1 Total Compensator Tray Water-Equivalent Thickness</summary>
		public const uint TotalCompensatorTrayWaterEquivalentThickness = 0x300a02e3;

		/// <summary>(300a,02e4) VR=FL VM=1 Isocenter to Compensator Tray Distance</summary>
		public const uint IsocenterToCompensatorTrayDistance = 0x300a02e4;

		/// <summary>(300a,02e5) VR=FL VM=1 Compensator Column Offset</summary>
		public const uint CompensatorColumnOffset = 0x300a02e5;

		/// <summary>(300a,02e6) VR=FL VM=1-n Isocenter to Compensator Distances</summary>
		public const uint IsocenterToCompensatorDistances = 0x300a02e6;

		/// <summary>(300a,02e7) VR=FL VM=1 Compensator Relative Stopping Power Ratio</summary>
		public const uint CompensatorRelativeStoppingPowerRatio = 0x300a02e7;

		/// <summary>(300a,02e8) VR=FL VM=1 Compensator Milling Tool Diameter</summary>
		public const uint CompensatorMillingToolDiameter = 0x300a02e8;

		/// <summary>(300a,02ea) VR=SQ VM=1 Ion Range Compensator Sequence</summary>
		public const uint IonRangeCompensatorSequence = 0x300a02ea;

		/// <summary>(300a,02eb) VR=LT VM=1 Compensator Description</summary>
		public const uint CompensatorDescription = 0x300a02eb;

		/// <summary>(300a,0302) VR=IS VM=1 Radiation Mass Number</summary>
		public const uint RadiationMassNumber = 0x300a0302;

		/// <summary>(300a,0304) VR=IS VM=1 Radiation Atomic Number</summary>
		public const uint RadiationAtomicNumber = 0x300a0304;

		/// <summary>(300a,0306) VR=SS VM=1 Radiation Charge State</summary>
		public const uint RadiationChargeState = 0x300a0306;

		/// <summary>(300a,0308) VR=CS VM=1 Scan Mode</summary>
		public const uint ScanMode = 0x300a0308;

		/// <summary>(300a,030a) VR=FL VM=2 Virtual Source-Axis Distances</summary>
		public const uint VirtualSourceAxisDistances = 0x300a030a;

		/// <summary>(300a,030c) VR=SQ VM=1 Snout Sequence</summary>
		public const uint SnoutSequence = 0x300a030c;

		/// <summary>(300a,030d) VR=FL VM=1 Snout Position</summary>
		public const uint SnoutPosition = 0x300a030d;

		/// <summary>(300a,030f) VR=SH VM=1 Snout ID</summary>
		public const uint SnoutID = 0x300a030f;

		/// <summary>(300a,0312) VR=IS VM=1 Number of Range Shifters</summary>
		public const uint NumberOfRangeShifters = 0x300a0312;

		/// <summary>(300a,0314) VR=SQ VM=1 Range Shifter Sequence</summary>
		public const uint RangeShifterSequence = 0x300a0314;

		/// <summary>(300a,0316) VR=IS VM=1 Range Shifter Number</summary>
		public const uint RangeShifterNumber = 0x300a0316;

		/// <summary>(300a,0318) VR=SH VM=1 Range Shifter ID</summary>
		public const uint RangeShifterID = 0x300a0318;

		/// <summary>(300a,0320) VR=CS VM=1 Range Shifter Type</summary>
		public const uint RangeShifterType = 0x300a0320;

		/// <summary>(300a,0322) VR=LO VM=1 Range Shifter Description</summary>
		public const uint RangeShifterDescription = 0x300a0322;

		/// <summary>(300a,0330) VR=IS VM=1 Number of Lateral Spreading Devices</summary>
		public const uint NumberOfLateralSpreadingDevices = 0x300a0330;

		/// <summary>(300a,0332) VR=SQ VM=1 Lateral Spreading Device Sequence</summary>
		public const uint LateralSpreadingDeviceSequence = 0x300a0332;

		/// <summary>(300a,0334) VR=IS VM=1 Lateral Spreading Device Number</summary>
		public const uint LateralSpreadingDeviceNumber = 0x300a0334;

		/// <summary>(300a,0336) VR=SH VM=1 Lateral Spreading Device ID</summary>
		public const uint LateralSpreadingDeviceID = 0x300a0336;

		/// <summary>(300a,0338) VR=CS VM=1 Lateral Spreading Device Type</summary>
		public const uint LateralSpreadingDeviceType = 0x300a0338;

		/// <summary>(300a,033a) VR=LO VM=1 Lateral Spreading Device Description</summary>
		public const uint LateralSpreadingDeviceDescription = 0x300a033a;

		/// <summary>(300a,033c) VR=FL VM=1 Lateral Spreading Device Water Equivalent Thickness</summary>
		public const uint LateralSpreadingDeviceWaterEquivalentThickness = 0x300a033c;

		/// <summary>(300a,0340) VR=IS VM=1 Number of Range Modulators</summary>
		public const uint NumberOfRangeModulators = 0x300a0340;

		/// <summary>(300a,0342) VR=SQ VM=1 Range Modulator Sequence</summary>
		public const uint RangeModulatorSequence = 0x300a0342;

		/// <summary>(300a,0344) VR=IS VM=1 Range Modulator Number</summary>
		public const uint RangeModulatorNumber = 0x300a0344;

		/// <summary>(300a,0346) VR=SH VM=1 Range Modulator ID</summary>
		public const uint RangeModulatorID = 0x300a0346;

		/// <summary>(300a,0348) VR=CS VM=1 Range Modulator Type</summary>
		public const uint RangeModulatorType = 0x300a0348;

		/// <summary>(300a,034a) VR=LO VM=1 Range Modulator Description</summary>
		public const uint RangeModulatorDescription = 0x300a034a;

		/// <summary>(300a,034c) VR=SH VM=1 Beam Current Modulation ID</summary>
		public const uint BeamCurrentModulationID = 0x300a034c;

		/// <summary>(300a,0350) VR=CS VM=1 Patient Support Type</summary>
		public const uint PatientSupportType = 0x300a0350;

		/// <summary>(300a,0352) VR=SH VM=1 Patient Support ID</summary>
		public const uint PatientSupportID = 0x300a0352;

		/// <summary>(300a,0354) VR=LO VM=1 Patient Support Accessory Code</summary>
		public const uint PatientSupportAccessoryCode = 0x300a0354;

		/// <summary>(300a,0356) VR=FL VM=1 Fixation Light Azimuthal Angle</summary>
		public const uint FixationLightAzimuthalAngle = 0x300a0356;

		/// <summary>(300a,0358) VR=FL VM=1 Fixation Light Polar Angle</summary>
		public const uint FixationLightPolarAngle = 0x300a0358;

		/// <summary>(300a,035a) VR=FL VM=1 Meterset Rate</summary>
		public const uint MetersetRate = 0x300a035a;

		/// <summary>(300a,0360) VR=SQ VM=1 Range Shifter Settings Sequence</summary>
		public const uint RangeShifterSettingsSequence = 0x300a0360;

		/// <summary>(300a,0362) VR=LO VM=1 Range Shifter Setting</summary>
		public const uint RangeShifterSetting = 0x300a0362;

		/// <summary>(300a,0364) VR=FL VM=1 Isocenter to Range Shifter Distance</summary>
		public const uint IsocenterToRangeShifterDistance = 0x300a0364;

		/// <summary>(300a,0366) VR=FL VM=1 Range Shifter Water Equivalent Thickness</summary>
		public const uint RangeShifterWaterEquivalentThickness = 0x300a0366;

		/// <summary>(300a,0370) VR=SQ VM=1 Lateral Spreading Device Settings Sequence</summary>
		public const uint LateralSpreadingDeviceSettingsSequence = 0x300a0370;

		/// <summary>(300a,0372) VR=LO VM=1 Lateral Spreading Device Setting</summary>
		public const uint LateralSpreadingDeviceSetting = 0x300a0372;

		/// <summary>(300a,0374) VR=FL VM=1 Isocenter to Lateral Spreading Device Distance</summary>
		public const uint IsocenterToLateralSpreadingDeviceDistance = 0x300a0374;

		/// <summary>(300a,0380) VR=SQ VM=1 Range Modulator Settings Sequence</summary>
		public const uint RangeModulatorSettingsSequence = 0x300a0380;

		/// <summary>(300a,0382) VR=FL VM=1 Range Modulator Gating Start Value</summary>
		public const uint RangeModulatorGatingStartValue = 0x300a0382;

		/// <summary>(300a,0384) VR=FL VM=1 Range Modulator Gating Stop Value</summary>
		public const uint RangeModulatorGatingStopValue = 0x300a0384;

		/// <summary>(300a,0386) VR=FL VM=1 Range Modulator Gating Start Water Equivalent Thickness</summary>
		public const uint RangeModulatorGatingStartWaterEquivalentThickness = 0x300a0386;

		/// <summary>(300a,0388) VR=FL VM=1 Range Modulator Gating Stop Water Equivalent Thickness</summary>
		public const uint RangeModulatorGatingStopWaterEquivalentThickness = 0x300a0388;

		/// <summary>(300a,038a) VR=FL VM=1 Isocenter to Range Modulator Distance</summary>
		public const uint IsocenterToRangeModulatorDistance = 0x300a038a;

		/// <summary>(300a,0390) VR=SH VM=1 Scan Spot Tune ID</summary>
		public const uint ScanSpotTuneID = 0x300a0390;

		/// <summary>(300a,0392) VR=IS VM=1 Number of Scan Spot Positions</summary>
		public const uint NumberOfScanSpotPositions = 0x300a0392;

		/// <summary>(300a,0394) VR=FL VM=1-n Scan Spot Position Map</summary>
		public const uint ScanSpotPositionMap = 0x300a0394;

		/// <summary>(300a,0396) VR=FL VM=1-n Scan Spot Meterset Weights</summary>
		public const uint ScanSpotMetersetWeights = 0x300a0396;

		/// <summary>(300a,0398) VR=FL VM=2 Scanning Spot Size</summary>
		public const uint ScanningSpotSize = 0x300a0398;

		/// <summary>(300a,039a) VR=IS VM=1 Number of Paintings</summary>
		public const uint NumberOfPaintings = 0x300a039a;

		/// <summary>(300a,03a0) VR=SQ VM=1 Ion Tolerance Table Sequence</summary>
		public const uint IonToleranceTableSequence = 0x300a03a0;

		/// <summary>(300a,03a2) VR=SQ VM=1 Ion Beam Sequence</summary>
		public const uint IonBeamSequence = 0x300a03a2;

		/// <summary>(300a,03a4) VR=SQ VM=1 Ion Beam Limiting Device Sequence</summary>
		public const uint IonBeamLimitingDeviceSequence = 0x300a03a4;

		/// <summary>(300a,03a6) VR=SQ VM=1 Ion Block Sequence</summary>
		public const uint IonBlockSequence = 0x300a03a6;

		/// <summary>(300a,03a8) VR=SQ VM=1 Ion Control Point Sequence</summary>
		public const uint IonControlPointSequence = 0x300a03a8;

		/// <summary>(300a,03aa) VR=SQ VM=1 Ion Wedge Sequence</summary>
		public const uint IonWedgeSequence = 0x300a03aa;

		/// <summary>(300a,03ac) VR=SQ VM=1 Ion Wedge Position Sequence</summary>
		public const uint IonWedgePositionSequence = 0x300a03ac;

		/// <summary>(300a,0401) VR=SQ VM=1 Referenced Setup Image Sequence</summary>
		public const uint ReferencedSetupImageSequence = 0x300a0401;

		/// <summary>(300a,0402) VR=ST VM=1 Setup Image Comment</summary>
		public const uint SetupImageComment = 0x300a0402;

		/// <summary>(300a,0410) VR=SQ VM=1 Motion Synchronization Sequence</summary>
		public const uint MotionSynchronizationSequence = 0x300a0410;

		/// <summary>(300a,0412) VR=FL VM=3 Control Point Orientation</summary>
		public const uint ControlPointOrientation = 0x300a0412;

		/// <summary>(300a,0420) VR=SQ VM=1 General Accessory Sequence</summary>
		public const uint GeneralAccessorySequence = 0x300a0420;

		/// <summary>(300a,0421) VR=CS VM=1 General Accessory ID</summary>
		public const uint GeneralAccessoryID = 0x300a0421;

		/// <summary>(300a,0422) VR=ST VM=1 General Accessory Description</summary>
		public const uint GeneralAccessoryDescription = 0x300a0422;

		/// <summary>(300a,0423) VR=SH VM=1 General Accessory Type</summary>
		public const uint GeneralAccessoryType = 0x300a0423;

		/// <summary>(300a,0424) VR=IS VM=1 General Accessory Number</summary>
		public const uint GeneralAccessoryNumber = 0x300a0424;

		/// <summary>(300c,0002) VR=SQ VM=1 Referenced RT Plan Sequence</summary>
		public const uint ReferencedRTPlanSequence = 0x300c0002;

		/// <summary>(300c,0004) VR=SQ VM=1 Referenced Beam Sequence</summary>
		public const uint ReferencedBeamSequence = 0x300c0004;

		/// <summary>(300c,0006) VR=IS VM=1 Referenced Beam Number</summary>
		public const uint ReferencedBeamNumber = 0x300c0006;

		/// <summary>(300c,0007) VR=IS VM=1 Referenced Reference Image Number</summary>
		public const uint ReferencedReferenceImageNumber = 0x300c0007;

		/// <summary>(300c,0008) VR=DS VM=1 Start Cumulative Meterset Weight</summary>
		public const uint StartCumulativeMetersetWeight = 0x300c0008;

		/// <summary>(300c,0009) VR=DS VM=1 End Cumulative Meterset Weight</summary>
		public const uint EndCumulativeMetersetWeight = 0x300c0009;

		/// <summary>(300c,000a) VR=SQ VM=1 Referenced Brachy Application Setup Sequence</summary>
		public const uint ReferencedBrachyApplicationSetupSequence = 0x300c000a;

		/// <summary>(300c,000c) VR=IS VM=1 Referenced Brachy Application Setup Number</summary>
		public const uint ReferencedBrachyApplicationSetupNumber = 0x300c000c;

		/// <summary>(300c,000e) VR=IS VM=1 Referenced Source Number</summary>
		public const uint ReferencedSourceNumber = 0x300c000e;

		/// <summary>(300c,0020) VR=SQ VM=1 Referenced Fraction Group Sequence</summary>
		public const uint ReferencedFractionGroupSequence = 0x300c0020;

		/// <summary>(300c,0022) VR=IS VM=1 Referenced Fraction Group Number</summary>
		public const uint ReferencedFractionGroupNumber = 0x300c0022;

		/// <summary>(300c,0040) VR=SQ VM=1 Referenced Verification Image Sequence</summary>
		public const uint ReferencedVerificationImageSequence = 0x300c0040;

		/// <summary>(300c,0042) VR=SQ VM=1 Referenced Reference Image Sequence</summary>
		public const uint ReferencedReferenceImageSequence = 0x300c0042;

		/// <summary>(300c,0050) VR=SQ VM=1 Referenced Dose Reference Sequence</summary>
		public const uint ReferencedDoseReferenceSequence = 0x300c0050;

		/// <summary>(300c,0051) VR=IS VM=1 Referenced Dose Reference Number</summary>
		public const uint ReferencedDoseReferenceNumber = 0x300c0051;

		/// <summary>(300c,0055) VR=SQ VM=1 Brachy Referenced Dose Reference Sequence</summary>
		public const uint BrachyReferencedDoseReferenceSequence = 0x300c0055;

		/// <summary>(300c,0060) VR=SQ VM=1 Referenced Structure Set Sequence</summary>
		public const uint ReferencedStructureSetSequence = 0x300c0060;

		/// <summary>(300c,006a) VR=IS VM=1 Referenced Patient Setup Number</summary>
		public const uint ReferencedPatientSetupNumber = 0x300c006a;

		/// <summary>(300c,0080) VR=SQ VM=1 Referenced Dose Sequence</summary>
		public const uint ReferencedDoseSequence = 0x300c0080;

		/// <summary>(300c,00a0) VR=IS VM=1 Referenced Tolerance Table Number</summary>
		public const uint ReferencedToleranceTableNumber = 0x300c00a0;

		/// <summary>(300c,00b0) VR=SQ VM=1 Referenced Bolus Sequence</summary>
		public const uint ReferencedBolusSequence = 0x300c00b0;

		/// <summary>(300c,00c0) VR=IS VM=1 Referenced Wedge Number</summary>
		public const uint ReferencedWedgeNumber = 0x300c00c0;

		/// <summary>(300c,00d0) VR=IS VM=1 Referenced Compensator Number</summary>
		public const uint ReferencedCompensatorNumber = 0x300c00d0;

		/// <summary>(300c,00e0) VR=IS VM=1 Referenced Block Number</summary>
		public const uint ReferencedBlockNumber = 0x300c00e0;

		/// <summary>(300c,00f0) VR=IS VM=1 Referenced Control Point Index</summary>
		public const uint ReferencedControlPointIndex = 0x300c00f0;

		/// <summary>(300c,00f2) VR=SQ VM=1 Referenced Control Point Sequence</summary>
		public const uint ReferencedControlPointSequence = 0x300c00f2;

		/// <summary>(300c,00f4) VR=IS VM=1 Referenced Start Control Point Index</summary>
		public const uint ReferencedStartControlPointIndex = 0x300c00f4;

		/// <summary>(300c,00f6) VR=IS VM=1 Referenced Stop Control Point Index</summary>
		public const uint ReferencedStopControlPointIndex = 0x300c00f6;

		/// <summary>(300c,0100) VR=IS VM=1 Referenced Range Shifter Number</summary>
		public const uint ReferencedRangeShifterNumber = 0x300c0100;

		/// <summary>(300c,0102) VR=IS VM=1 Referenced Lateral Spreading Device Number</summary>
		public const uint ReferencedLateralSpreadingDeviceNumber = 0x300c0102;

		/// <summary>(300c,0104) VR=IS VM=1 Referenced Range Modulator Number</summary>
		public const uint ReferencedRangeModulatorNumber = 0x300c0104;

		/// <summary>(300e,0002) VR=CS VM=1 Approval Status</summary>
		public const uint ApprovalStatus = 0x300e0002;

		/// <summary>(300e,0004) VR=DA VM=1 Review Date</summary>
		public const uint ReviewDate = 0x300e0004;

		/// <summary>(300e,0005) VR=TM VM=1 Review Time</summary>
		public const uint ReviewTime = 0x300e0005;

		/// <summary>(300e,0008) VR=PN VM=1 Reviewer Name</summary>
		public const uint ReviewerName = 0x300e0008;

		/// <summary>(4ffe,0001) VR=SQ VM=1 MAC Parameters Sequence</summary>
		public const uint MACParametersSequence = 0x4ffe0001;

		/// <summary>(5200,9229) VR=SQ VM=1 Shared Functional Groups Sequence</summary>
		public const uint SharedFunctionalGroupsSequence = 0x52009229;

		/// <summary>(5200,9230) VR=SQ VM=1 Per-frame Functional Groups Sequence</summary>
		public const uint PerframeFunctionalGroupsSequence = 0x52009230;

		/// <summary>(5400,0100) VR=SQ VM=1 Waveform Sequence</summary>
		public const uint WaveformSequence = 0x54000100;

		/// <summary>(5400,0110) VR=OB/OW VM=1 Channel Minimum Value</summary>
		public const uint ChannelMinimumValue = 0x54000110;

		/// <summary>(5400,0112) VR=OB/OW VM=1 Channel Maximum Value</summary>
		public const uint ChannelMaximumValue = 0x54000112;

		/// <summary>(5400,1004) VR=US VM=1 Waveform Bits Allocated</summary>
		public const uint WaveformBitsAllocated = 0x54001004;

		/// <summary>(5400,1006) VR=CS VM=1 Waveform Sample Interpretation</summary>
		public const uint WaveformSampleInterpretation = 0x54001006;

		/// <summary>(5400,100a) VR=OB/OW VM=1 Waveform Padding Value</summary>
		public const uint WaveformPaddingValue = 0x5400100a;

		/// <summary>(5400,1010) VR=OB/OW VM=1 Waveform Data</summary>
		public const uint WaveformData = 0x54001010;

		/// <summary>(5600,0010) VR=OF VM=1 First Order Phase Correction Angle</summary>
		public const uint FirstOrderPhaseCorrectionAngle = 0x56000010;

		/// <summary>(5600,0020) VR=OF VM=1 Spectroscopy Data</summary>
		public const uint SpectroscopyData = 0x56000020;

		/// <summary>(7fe0,0010) VR=OB/OW VM=1 Pixel Data</summary>
		public const uint PixelData = 0x7fe00010;

		/// <summary>(fffa,fffa) VR=SQ VM=1 Digital Signatures Sequence</summary>
		public const uint DigitalSignaturesSequence = 0xfffafffa;

		/// <summary>(fffc,fffc) VR=OB VM=1 Data Set Trailing Padding</summary>
		public const uint DataSetTrailingPadding = 0xfffcfffc;

		/// <summary>(fffe,e000) VR=NONE VM=1 Item</summary>
		public const uint Item = 0xfffee000;

		/// <summary>(fffe,e00d) VR=NONE VM=1 Item Delimitation Item</summary>
		public const uint ItemDelimitationItem = 0xfffee00d;

		/// <summary>(fffe,e0dd) VR=NONE VM=1 Sequence Delimitation Item</summary>
		public const uint SequenceDelimitationItem = 0xfffee0dd;
		#endregion
	}

	/// <summary>DICOM Tag mask for matching groups of tags</summary>
	public class DicomTagMask {
		#region Private Members
		private uint _c;
		private uint _m;
		#endregion

		#region Public Constructor
		private DicomTagMask() {
		}

		public DicomTagMask(string mask) {
			mask = mask.Trim('(', ')');
			mask = mask.Replace(",", "").ToLower();

			StringBuilder sb = new StringBuilder(mask);
			sb.Replace('x', '0');
			_c = uint.Parse(sb.ToString(), System.Globalization.NumberStyles.HexNumber);

			sb = new StringBuilder(mask);
			sb.Replace('0', 'F').Replace('1', 'F').Replace('2', 'F')
				.Replace('3', 'F').Replace('4', 'F').Replace('5', 'F')
				.Replace('6', 'F').Replace('7', 'F').Replace('8', 'F')
				.Replace('9', 'F').Replace('a', 'F').Replace('b', 'F')
				.Replace('c', 'F').Replace('d', 'F').Replace('e', 'F')
				.Replace('f', 'F').Replace('x', '0');
			_m = uint.Parse(sb.ToString(), System.Globalization.NumberStyles.HexNumber);
		}
		#endregion

		#region Public Properties
		public uint Card {
			get { return _c; }
		}

		public uint Mask {
			get { return _m; }
		}

		public bool IsFullMask {
			get { return _m == 0xFFFFFFFF; }
		}

		public DicomTag Tag {
			get { return new DicomTag(_c); }
		}
		#endregion

		#region Public Members
		public static DicomTagMask Parse(string mask) {
			try {
				return new DicomTagMask(mask);
			}
			catch {
				return null;
			}
		}

		public bool IsMatch(DicomTag tag) {
			return (tag.Card & Mask) == Card;
		}

		public override string ToString() {
			string tag = String.Empty;
			string card = _c.ToString("X8");
			uint mask = 0xF0000000;
			for (int i = 0; i < 8; i++) {
				if (i == 4)
					tag += ',';
				if ((_m & mask) != 0)
					tag += card[i];
				else
					tag += 'X';
				mask >>= 4;
			}
			return tag;
		}
		#endregion
	}
}
