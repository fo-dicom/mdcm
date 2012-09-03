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
using System.IO;
using System.Text;

namespace Dicom.Data {
	public class DcmFileMetaInfo : DcmDataset {
		#region Public Constructors
		public DcmFileMetaInfo() : base(DicomTransferSyntax.ExplicitVRLittleEndian) {
		}
		#endregion

		#region Public Properties
		public readonly static byte[] Version = new byte[] { 0x00, 0x01 };
		public readonly static DicomTag StopTag = new DicomTag(0x0002, 0xFFFF);

		public byte[] FileMetaInformationVersion {
			get {
				if (Contains(DicomTags.FileMetaInformationVersion))
					return GetOB(DicomTags.FileMetaInformationVersion).GetValues();
				return null;
			}
			set {
				if (AddElement(DicomTags.FileMetaInformationVersion, DicomVR.OB))
					GetOB(DicomTags.FileMetaInformationVersion).SetValues(value);
			}
		}

		public DicomUID MediaStorageSOPClassUID {
			get {
				return GetUID(DicomTags.MediaStorageSOPClassUID);
			}
			set {
				AddElementWithValue(DicomTags.MediaStorageSOPClassUID, value);
			}
		}

		public DicomUID MediaStorageSOPInstanceUID {
			get {
				return GetUID(DicomTags.MediaStorageSOPInstanceUID);
			}
			set {
				AddElementWithValue(DicomTags.MediaStorageSOPInstanceUID, value);
			}
		}

		public DicomTransferSyntax TransferSyntax {
			get {
				if (Contains(DicomTags.TransferSyntaxUID))
					return GetUI(DicomTags.TransferSyntaxUID).GetTS();
				return null;
			}
			set {
				AddElementWithValue(DicomTags.TransferSyntaxUID, value.UID);
			}
		}

		public DicomUID ImplementationClassUID {
			get {
				return GetUID(DicomTags.ImplementationClassUID);
			}
			set {
				AddElementWithValue(DicomTags.ImplementationClassUID, value);
			}
		}

		public string ImplementationVersionName {
			get {
				return GetString(DicomTags.ImplementationVersionName, null);
			}
			set {
				AddElementWithValue(DicomTags.ImplementationVersionName, value);
			}
		}

		public string SourceApplicationEntityTitle {
			get {
				return GetString(DicomTags.SourceApplicationEntityTitle, null);
			}
			set {
				AddElementWithValue(DicomTags.SourceApplicationEntityTitle, value);
			}
		}

		public DicomUID PrivateInformationCreatorUID {
			get {
				return GetUID(DicomTags.PrivateInformationCreatorUID);
			}
			set {
				AddElementWithValue(DicomTags.PrivateInformationCreatorUID, value);
			}
		}

		public byte[] PrivateInformation {
			get {
				if (Contains(DicomTags.PrivateInformation))
					return GetOB(DicomTags.PrivateInformation).GetValues();
				return null;
			}
			set {
				if (AddElement(DicomTags.PrivateInformation, DicomVR.OB))
					GetOB(DicomTags.PrivateInformation).SetValues(value);
			}
		}
		#endregion
	}
}
