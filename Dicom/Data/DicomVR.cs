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

namespace Dicom.Data {
	public enum DicomVrRestriction {
		NotApplicable,
		Fixed,
		Maximum,
		Any
	}

	public class DicomVR {
		#region Private Members
		private string _strval;
		private int _hash;
		private string _desc;
		private bool _is16BitLength;
		private bool _isString;
		private bool _isEncodedString;
		private byte _padding;
		private int _maxLength;
		private int _unitSize;
		private DicomVrRestriction _restriction;

		private DicomVR() {
		}

		internal DicomVR(string value, string desc, bool isString, bool isEncodedString, bool is16BitLength, 
			byte padding, int maxLength, int unitSize, DicomVrRestriction restriction) {
			_strval = value;
			_hash = (((int)value[0] << 8) | (int)value[1]);
			_desc = desc;
			_isString = isString;
			_isEncodedString = isEncodedString;
			_is16BitLength = is16BitLength;
			_padding = padding;
			//_padding = PadZero;
			_maxLength = maxLength;
			_unitSize = unitSize;
			_restriction = restriction;
		}
		#endregion

		#region Public Properties
		public string VR {
			get { return _strval; }
		}

		public string Description {
			get { return _desc; }
		}

		public bool IsString {
			get { return _isString; }
		}

		/// <summary>
		/// Specific Character Set applies to this VR
		/// </summary>
		public bool IsEncodedString {
			get { return _isEncodedString; }
		}

		public bool Is16BitLengthField {
			get { return _is16BitLength; }
		}

		public byte Padding {
			get { return _padding; }
		}

		public int MaxmimumLength {
			get { return _maxLength; }
		}

		public int UnitSize {
			get { return _unitSize; }
		}

		public DicomVrRestriction Restriction {
			get { return _restriction; }
		}
		#endregion

		public override string ToString() {
			return String.Format("{0} - {1}", VR, Description);
		}

		private const byte PadSpace = 0x20;
		private const byte PadZero = 0x00;

		/// <summary>No VR</summary>
		public static DicomVR NONE = new DicomVR("NONE", "No VR", false, false, false, PadZero, 0, 0, DicomVrRestriction.NotApplicable);

		/// <summary>Application Entity</summary>
		public static DicomVR AE = new DicomVR("AE", "Application Entity", true, false, true, PadSpace, 16, 1, DicomVrRestriction.Maximum);

		/// <summary>Age String</summary>
		public static DicomVR AS = new DicomVR("AS", "Age String", true, false, true, PadSpace, 4, 1, DicomVrRestriction.Fixed);

		/// <summary>Attribute Tag</summary>
		public static DicomVR AT = new DicomVR("AT", "Attribute Tag", false, false, true, PadZero, 4, 4, DicomVrRestriction.Fixed);

		/// <summary>Code String</summary>
		public static DicomVR CS = new DicomVR("CS", "Code String", true, false, true, PadSpace, 16, 1, DicomVrRestriction.Maximum);

		/// <summary>Date</summary>
		public static DicomVR DA = new DicomVR("DA", "Date", true, false, true, PadSpace, 8, 1, DicomVrRestriction.Fixed);

		/// <summary>Decimal String</summary>
		public static DicomVR DS = new DicomVR("DS", "Decimal String", true, false, true, PadSpace, 16, 1, DicomVrRestriction.Maximum);

		/// <summary>Date Time</summary>
		public static DicomVR DT = new DicomVR("DT", "Date Time", true, false, true, PadSpace, 26, 1, DicomVrRestriction.Maximum);

		/// <summary>Floating Point Double</summary>
		public static DicomVR FD = new DicomVR("FD", "Floating Point Double", false, false, true, PadZero, 8, 8, DicomVrRestriction.Fixed);

		/// <summary>Floating Point Single</summary>
		public static DicomVR FL = new DicomVR("FL", "Floating Point Single", false, false, true, PadZero, 4, 4, DicomVrRestriction.Fixed);

		/// <summary>Integer String</summary>
		public static DicomVR IS = new DicomVR("IS", "Integer String", true, false, true, PadSpace, 12, 1, DicomVrRestriction.Maximum);

		/// <summary>Long String</summary>
		public static DicomVR LO = new DicomVR("LO", "Long String", true, true, true, PadSpace, 64, 1, DicomVrRestriction.Maximum);

		/// <summary>Long Text</summary>
		public static DicomVR LT = new DicomVR("LT", "Long Text", true, true, true, PadSpace, 10240, 1, DicomVrRestriction.Maximum);

		/// <summary>Other Byte</summary>
		public static DicomVR OB = new DicomVR("OB", "Other Byte", false, false, false, PadZero, 0, 1, DicomVrRestriction.Any);

		/// <summary>Other Float</summary>
		public static DicomVR OF = new DicomVR("OF", "Other Float", false, false, false, PadZero, 0, 4, DicomVrRestriction.Any);

		/// <summary>Other Word</summary>
		public static DicomVR OW = new DicomVR("OW", "Other Word", false, false, false, PadZero, 0, 2, DicomVrRestriction.Any);

		/// <summary>Person Name</summary>
		public static DicomVR PN = new DicomVR("PN", "Person Name", true, true, true, PadSpace, 64, 1, DicomVrRestriction.Maximum);

		/// <summary>Short String</summary>
		public static DicomVR SH = new DicomVR("SH", "Short String", true, true, true, PadSpace, 16, 1, DicomVrRestriction.Maximum);

		/// <summary>Signed Long</summary>
		public static DicomVR SL = new DicomVR("SL", "Signed Long", false, false, true, PadZero, 4, 4, DicomVrRestriction.Fixed);

		/// <summary>Sequence of Items</summary>
		public static DicomVR SQ = new DicomVR("SQ", "Sequence of Items", false, false, false, PadZero, 0, 0, DicomVrRestriction.NotApplicable);

		/// <summary>Signed Short</summary>
		public static DicomVR SS = new DicomVR("SS", "Signed Short", false, false, true, PadZero, 2, 2, DicomVrRestriction.Fixed);

		/// <summary>Short Text</summary>
		public static DicomVR ST = new DicomVR("ST", "Short Text", true, true, true, PadSpace, 1024, 1, DicomVrRestriction.Maximum);

		/// <summary>Time</summary>
		public static DicomVR TM = new DicomVR("TM", "Time", true, false, true, PadSpace, 16, 1, DicomVrRestriction.Maximum);

		/// <summary>Unique Identifier</summary>
		public static DicomVR UI = new DicomVR("UI", "Unique Identifier", true, false, true, PadZero, 64, 1, DicomVrRestriction.Maximum);

		/// <summary>Unsigned Long</summary>
		public static DicomVR UL = new DicomVR("UL", "Unsigned Long", false, false, true, PadZero, 4, 4, DicomVrRestriction.Fixed);

		/// <summary>Unknown</summary>
		public static DicomVR UN = new DicomVR("UN", "Unknown", false, false, false, PadZero, 0, 1, DicomVrRestriction.Any);

		/// <summary>Unsigned Short</summary>
		public static DicomVR US = new DicomVR("US", "Unsigned Short", false, false, true, PadZero, 2, 2, DicomVrRestriction.Fixed);

		/// <summary>Unlimited Text</summary>
		public static DicomVR UT = new DicomVR("UT", "Unlimited Text", true, true, false, PadSpace, 0, 1, DicomVrRestriction.Any);

		#region Static Methods
		public static List<DicomVR> Entries = new List<DicomVR>();

		static DicomVR() {
			#region Load VRs
			Entries.Add(DicomVR.AE);
			Entries.Add(DicomVR.AS);
			Entries.Add(DicomVR.AT);
			Entries.Add(DicomVR.CS);
			Entries.Add(DicomVR.DA);
			Entries.Add(DicomVR.DS);
			Entries.Add(DicomVR.DT);
			Entries.Add(DicomVR.FD);
			Entries.Add(DicomVR.FL);
			Entries.Add(DicomVR.IS);
			Entries.Add(DicomVR.LO);
			Entries.Add(DicomVR.LT);
			Entries.Add(DicomVR.OB);
			Entries.Add(DicomVR.OF);
			Entries.Add(DicomVR.OW);
			Entries.Add(DicomVR.PN);
			Entries.Add(DicomVR.SH);
			Entries.Add(DicomVR.SL);
			Entries.Add(DicomVR.SQ);
			Entries.Add(DicomVR.SS);
			Entries.Add(DicomVR.ST);
			Entries.Add(DicomVR.TM);
			Entries.Add(DicomVR.UI);
			Entries.Add(DicomVR.UL);
			Entries.Add(DicomVR.UN);
			Entries.Add(DicomVR.US);
			Entries.Add(DicomVR.UT);
			#endregion
		}

		public static DicomVR Lookup(ushort vr) {
			if (vr == 0x0000)
				return DicomVR.NONE;
			return Lookup(new char[] { (char)(vr >> 8), (char)(vr) });
		}

		public static DicomVR Lookup(char[] vr) {
			return Lookup(new String(vr));
		}

		public static DicomVR Lookup(string vr) {
			switch (vr) {
			case "NONE": return DicomVR.NONE;
			case "AE": return DicomVR.AE;
			case "AS": return DicomVR.AS;
			case "AT": return DicomVR.AT;
			case "CS": return DicomVR.CS;
			case "DA": return DicomVR.DA;
			case "DS": return DicomVR.DS;
			case "DT": return DicomVR.DT;
			case "FD": return DicomVR.FD;
			case "FL": return DicomVR.FL;
			case "IS": return DicomVR.IS;
			case "LO": return DicomVR.LO;
			case "LT": return DicomVR.LT;
			case "OB": return DicomVR.OB;
			case "OF": return DicomVR.OF;
			case "OW": return DicomVR.OW;
			case "PN": return DicomVR.PN;
			case "SH": return DicomVR.SH;
			case "SL": return DicomVR.SL;
			case "SQ": return DicomVR.SQ;
			case "SS": return DicomVR.SS;
			case "ST": return DicomVR.ST;
			case "TM": return DicomVR.TM;
			case "UI": return DicomVR.UI;
			case "UL": return DicomVR.UL;
			case "UN": return DicomVR.UN;
			case "US": return DicomVR.US;
			case "UT": return DicomVR.UT;
			default:
				return DicomVR.UN;
			}
		}
		#endregion
	}
}
