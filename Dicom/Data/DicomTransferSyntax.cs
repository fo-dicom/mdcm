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

using Dicom.IO;

namespace Dicom.Data {
	public class DicomTransferSyntax {
		public readonly DicomUID UID;
		public readonly bool IsBigEndian;
		public readonly bool IsExplicitVR;
		public readonly bool IsEncapsulated;
		public readonly bool IsLossy;
		public readonly bool IsDeflate;
		public readonly Endian Endian;

		public DicomTransferSyntax(DicomUID uid, bool be, bool evr, bool encaps, bool lssy, bool dflt) {
			UID = uid;
			IsBigEndian = be;
			IsExplicitVR = evr;
			IsEncapsulated = encaps;
			IsLossy = lssy;
			IsDeflate = dflt;
			Endian = IsBigEndian ? Endian.Big : Endian.Little;
		}

		public override string ToString() {
			return UID.Description;
		}

		public override bool Equals(object obj) {
			if (obj is DicomTransferSyntax)
				return ((DicomTransferSyntax)obj).UID.Equals(UID);
			if (obj is DicomUID)
				return ((DicomUID)obj).Equals(UID);
			if (obj is String)
				return UID.Equals((String)obj);
			return false;
		}

		public override int GetHashCode() {
			return base.GetHashCode();
		}

		#region Dicom Transfer Syntax
		/// <summary>Implicit VR Little Endian</summary>
		public static DicomTransferSyntax ImplicitVRLittleEndian = new DicomTransferSyntax(DicomUID.ImplicitVRLittleEndian, false, false, false, false, false);

		/// <summary>Explicit VR Little Endian</summary>
		public static DicomTransferSyntax ExplicitVRLittleEndian = new DicomTransferSyntax(DicomUID.ExplicitVRLittleEndian, false, true, false, false, false);

		/// <summary>Explicit VR Big Endian</summary>
		public static DicomTransferSyntax ExplicitVRBigEndian = new DicomTransferSyntax(DicomUID.ExplicitVRBigEndian, true, true, false, false, false);

		/// <summary>Deflated Explicit VR Little Endian</summary>
		public static DicomTransferSyntax DeflatedExplicitVRLittleEndian = new DicomTransferSyntax(DicomUID.DeflatedExplicitVRLittleEndian, false, true, false, false, true);

		/// <summary>JPEG Baseline (Process 1)</summary>
		public static DicomTransferSyntax JPEGProcess1 = new DicomTransferSyntax(DicomUID.JPEGBaselineProcess1, false, true, true, true, false);

		/// <summary>JPEG Extended (Process 2 &amp; 4)</summary>
		public static DicomTransferSyntax JPEGProcess2_4 = new DicomTransferSyntax(DicomUID.JPEGExtendedProcess2_4, false, true, true, true, false);

		/// <summary>JPEG Extended (Process 3 &amp; 5) (Retired)</summary>
		public static DicomTransferSyntax JPEGProcess3_5Retired = new DicomTransferSyntax(DicomUID.JPEGExtendedProcess3_5RETIRED, false, true, true, true, false);

		/// <summary>JPEG Spectral Selection, Non-Hierarchical (Process 6 &amp; 8) (Retired)</summary>
		public static DicomTransferSyntax JPEGProcess6_8Retired = new DicomTransferSyntax(DicomUID.JPEGSpectralSelectionNonHierarchicalProcess6_8RETIRED, false, true, true, true, false);

		/// <summary>JPEG Spectral Selection, Non-Hierarchical (Process 7 &amp; 9) (Retired)</summary>
		public static DicomTransferSyntax JPEGProcess7_9Retired = new DicomTransferSyntax(DicomUID.JPEGSpectralSelectionNonHierarchicalProcess7_9RETIRED, false, true, true, true, false);

		/// <summary>JPEG Full Progression, Non-Hierarchical (Process 10 &amp; 12) (Retired)</summary>
		public static DicomTransferSyntax JPEGProcess10_12Retired = new DicomTransferSyntax(DicomUID.JPEGFullProgressionNonHierarchicalProcess10_12RETIRED, false, true, true, true, false);

		/// <summary>JPEG Full Progression, Non-Hierarchical (Process 11 &amp; 13) (Retired)</summary>
		public static DicomTransferSyntax JPEGProcess11_13Retired = new DicomTransferSyntax(DicomUID.JPEGFullProgressionNonHierarchicalProcess11_13RETIRED, false, true, true, true, false);

		/// <summary>JPEG Lossless, Non-Hierarchical (Process 14)</summary>
		public static DicomTransferSyntax JPEGProcess14 = new DicomTransferSyntax(DicomUID.JPEGLosslessNonHierarchicalProcess14, false, true, true, false, false);

		/// <summary>JPEG Lossless, Non-Hierarchical (Process 15) (Retired)</summary>
		public static DicomTransferSyntax JPEGProcess15Retired = new DicomTransferSyntax(DicomUID.JPEGLosslessNonHierarchicalProcess15RETIRED, false, true, true, false, false);

		/// <summary>JPEG Extended, Hierarchical (Process 16 &amp; 18) (Retired)</summary>
		public static DicomTransferSyntax JPEGProcess16_18Retired = new DicomTransferSyntax(DicomUID.JPEGExtendedHierarchicalProcess16_18RETIRED, false, true, true, true, false);

		/// <summary>JPEG Extended, Hierarchical (Process 17 &amp; 19) (Retired)</summary>
		public static DicomTransferSyntax JPEGProcess17_19Retired = new DicomTransferSyntax(DicomUID.JPEGExtendedHierarchicalProcess17_19RETIRED, false, true, true, true, false);

		/// <summary>JPEG Spectral Selection, Hierarchical (Process 20 &amp; 22) (Retired)</summary>
		public static DicomTransferSyntax JPEGProcess20_22Retired = new DicomTransferSyntax(DicomUID.JPEGSpectralSelectionHierarchicalProcess20_22RETIRED, false, true, true, true, false);

		/// <summary>JPEG Spectral Selection, Hierarchical (Process 21 &amp; 23) (Retired)</summary>
		public static DicomTransferSyntax JPEGProcess21_23Retired = new DicomTransferSyntax(DicomUID.JPEGSpectralSelectionHierarchicalProcess21_23RETIRED, false, true, true, true, false);

		/// <summary>JPEG Full Progression, Hierarchical (Process 24 &amp; 26) (Retired)</summary>
		public static DicomTransferSyntax JPEGProcess24_26Retired = new DicomTransferSyntax(DicomUID.JPEGFullProgressionHierarchicalProcess24_26RETIRED, false, true, true, true, false);

		/// <summary>JPEG Full Progression, Hierarchical (Process 25 &amp; 27) (Retired)</summary>
		public static DicomTransferSyntax JPEGProcess25_27Retired = new DicomTransferSyntax(DicomUID.JPEGFullProgressionHierarchicalProcess25_27RETIRED, false, true, true, true, false);

		/// <summary>JPEG Lossless, Hierarchical (Process 28) (Retired)</summary>
		public static DicomTransferSyntax JPEGProcess28Retired = new DicomTransferSyntax(DicomUID.JPEGLosslessHierarchicalProcess28RETIRED, false, true, true, false, false);

		/// <summary>JPEG Lossless, Hierarchical (Process 29) (Retired)</summary>
		public static DicomTransferSyntax JPEGProcess29Retired = new DicomTransferSyntax(DicomUID.JPEGLosslessHierarchicalProcess29RETIRED, false, true, true, false, false);

		/// <summary>JPEG Lossless, Non-Hierarchical, First-Order Prediction (Process 14 [Selection Value 1])</summary>
		public static DicomTransferSyntax JPEGProcess14SV1 = new DicomTransferSyntax(DicomUID.JPEGLosslessProcess14SV1, false, true, true, false, false);

		/// <summary>JPEG-LS Lossless Image Compression</summary>
		public static DicomTransferSyntax JPEGLSLossless = new DicomTransferSyntax(DicomUID.JPEGLSLosslessImageCompression, false, true, true, false, false);

		/// <summary>JPEG-LS Lossy (Near-Lossless) Image Compression</summary>
		public static DicomTransferSyntax JPEGLSNearLossless = new DicomTransferSyntax(DicomUID.JPEGLSLossyNearLosslessImageCompression, false, true, true, true, false);

		/// <summary>JPEG 2000 Lossless Image Compression</summary>
		public static DicomTransferSyntax JPEG2000Lossless = new DicomTransferSyntax(DicomUID.JPEG2000ImageCompressionLosslessOnly, false, true, true, false, false);

		/// <summary>JPEG 2000 Lossy Image Compression</summary>
		public static DicomTransferSyntax JPEG2000Lossy = new DicomTransferSyntax(DicomUID.JPEG2000ImageCompression, false, true, true, true, false);

		/// <summary>MPEG2 Main Profile @ Main Level</summary>
		public static DicomTransferSyntax MPEG2 = new DicomTransferSyntax(DicomUID.MPEG2MainProfileMainLevel, false, true, true, true, false);

		/// <summary>RLE Lossless</summary>
		public static DicomTransferSyntax RLELossless = new DicomTransferSyntax(DicomUID.RLELossless, false, true, true, false, false);
		#endregion

		#region Static Methods
		public static List<DicomTransferSyntax> Entries = new List<DicomTransferSyntax>();

		static DicomTransferSyntax() {
			#region Load Transfer Syntax List
			Entries.Add(DicomTransferSyntax.ImplicitVRLittleEndian);
			Entries.Add(DicomTransferSyntax.ExplicitVRLittleEndian);
			Entries.Add(DicomTransferSyntax.ExplicitVRBigEndian);
			Entries.Add(DicomTransferSyntax.DeflatedExplicitVRLittleEndian);
			Entries.Add(DicomTransferSyntax.JPEGProcess1);
			Entries.Add(DicomTransferSyntax.JPEGProcess2_4);
			Entries.Add(DicomTransferSyntax.JPEGProcess3_5Retired);
			Entries.Add(DicomTransferSyntax.JPEGProcess6_8Retired);
			Entries.Add(DicomTransferSyntax.JPEGProcess7_9Retired);
			Entries.Add(DicomTransferSyntax.JPEGProcess10_12Retired);
			Entries.Add(DicomTransferSyntax.JPEGProcess11_13Retired);
			Entries.Add(DicomTransferSyntax.JPEGProcess14);
			Entries.Add(DicomTransferSyntax.JPEGProcess15Retired);
			Entries.Add(DicomTransferSyntax.JPEGProcess16_18Retired);
			Entries.Add(DicomTransferSyntax.JPEGProcess17_19Retired);
			Entries.Add(DicomTransferSyntax.JPEGProcess20_22Retired);
			Entries.Add(DicomTransferSyntax.JPEGProcess21_23Retired);
			Entries.Add(DicomTransferSyntax.JPEGProcess24_26Retired);
			Entries.Add(DicomTransferSyntax.JPEGProcess25_27Retired);
			Entries.Add(DicomTransferSyntax.JPEGProcess28Retired);
			Entries.Add(DicomTransferSyntax.JPEGProcess29Retired);
			Entries.Add(DicomTransferSyntax.JPEGProcess14SV1);
			Entries.Add(DicomTransferSyntax.JPEGLSLossless);
			Entries.Add(DicomTransferSyntax.JPEGLSNearLossless);
			Entries.Add(DicomTransferSyntax.JPEG2000Lossless);
			Entries.Add(DicomTransferSyntax.JPEG2000Lossy);
			Entries.Add(DicomTransferSyntax.MPEG2);
			Entries.Add(DicomTransferSyntax.RLELossless);
			#endregion
		}

		public static DicomTransferSyntax Lookup(String uid) {
			return Lookup(DicomUID.Lookup(uid));
		}

		public static DicomTransferSyntax Lookup(DicomUID uid) {
			foreach (DicomTransferSyntax ts in Entries) {
				if (ts.Equals(uid))
					return ts;
			}
			return new DicomTransferSyntax(uid, false, true, true, false, false);
		}

		public static bool IsImageCompression(DicomTransferSyntax tx) {
			return  tx != DicomTransferSyntax.ImplicitVRLittleEndian &&
					tx != DicomTransferSyntax.ExplicitVRLittleEndian &&
					tx != DicomTransferSyntax.ExplicitVRBigEndian &&
					tx != DicomTransferSyntax.DeflatedExplicitVRLittleEndian;
		}
		#endregion
	}
}
