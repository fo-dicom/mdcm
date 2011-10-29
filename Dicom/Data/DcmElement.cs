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
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Dicom.IO;

namespace Dicom.Data {
	public abstract class DcmElement : DcmItem {
		#region Protected Members
		protected ByteBuffer _bb;
		#endregion

		#region Constructors
		protected DcmElement(DicomTag tag, DicomVR vr) : base(tag, vr) {
			_bb = new ByteBuffer();
		}

		protected DcmElement(DicomTag tag, DicomVR vr, long pos, Endian endian)
			: base(tag, vr, pos, endian) {
			_bb = new ByteBuffer(Endian);
		}

		protected DcmElement(DicomTag tag, DicomVR vr, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, vr, pos, endian) {
			if (buffer == null && Endian != Endian.LocalMachine)
				_bb = new ByteBuffer(Endian);
			else
				_bb = buffer;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the length of the internal byte buffer
		/// </summary>
		/// <value>The length.</value>
		public int Length {
			get {
				if (_bb == null)
					return 0;
				return _bb.Length;
			}
		}

		/// <summary>
		/// Gets or sets the byte buffer.
		/// </summary>
		public ByteBuffer ByteBuffer {
			get {
				if (_bb == null)
					_bb = new ByteBuffer();
				return _bb;
			}
			set { _bb = value; }
		}
		#endregion

		#region Abstract Methods
		public abstract int GetVM();

		public abstract string GetValueString();
		public abstract void SetValueString(string val);

		public abstract Type GetValueType();
		public abstract object GetValueObject();
		public abstract object[] GetValueObjectArray();
		public abstract void SetValueObject(object val);
		public abstract void SetValueObjectArray(object[] vals);
		#endregion

		#region DcmItem Methods
		internal override uint CalculateWriteLength(DicomTransferSyntax syntax, DicomWriteOptions options) {
			uint length = 4; // element tag
			if (syntax.IsExplicitVR) {
				length += 2; // vr
				if (VR.Is16BitLengthField)
					length += 2;
				else
					length += 6;
			} else {
				length += 4; // length tag				
			}
			length += (uint)Length;
			return length;
		}

		protected override void ChangeEndianInternal() {
			if (ByteBuffer.Endian != Endian) {
				ByteBuffer.Endian = Endian;
				ByteBuffer.Swap(VR.UnitSize);
			}
		}

		internal override void Preload() {
			if (_bb != null)
				_bb.Preload();
		}

		internal override void Unload() {
			if (_bb != null)
				_bb.Unload();
		}

		public override DcmItem Clone() {
			return DcmElement.Create(Tag, VR, StreamPosition, Endian, ByteBuffer.Clone());
		}

		public override void Dump(StringBuilder sb, string prefix, DicomDumpOptions options) {
			int ValueWidth = 40 - prefix.Length;
			int SbLength = sb.Length;

			sb.Append(prefix);
			sb.AppendFormat("{0} {1} ", Tag.ToString(), VR.VR);
			if (Length == 0) {
				String value = "(no value available)";
				sb.Append(value.PadRight(ValueWidth, ' '));
			} else {
				if (VR.IsString) {
					String value = null;
					if (VR == DicomVR.UI) {
						DcmUniqueIdentifier ui = this as DcmUniqueIdentifier;
						DicomUID uid = ui.GetUID();
						if (uid != null) {
							if (uid.Type == DicomUidType.Unknown)
								value = "[" + uid.UID + "]";
							else
								value = "=" + uid.Description;
							if (Flags.IsSet(options, DicomDumpOptions.ShortenLongValues)) {
								if (value.Length > ValueWidth) {
									value = value.Substring(0, ValueWidth - 3);
								}
							}
						} else {
							value = "[" + GetValueString() + "]";
							if (Flags.IsSet(options, DicomDumpOptions.ShortenLongValues)) {
								if (value.Length > ValueWidth) {
									value = value.Substring(0, ValueWidth - 4) + "...]";
								}
							}
						}
					} else {
						value = "[" + GetValueString() + "]";
						if (Flags.IsSet(options, DicomDumpOptions.ShortenLongValues)) {
							if (value.Length > ValueWidth) {
								value = value.Substring(0, ValueWidth - 4) + "...]";
							}
						}
					}
					sb.Append(value.PadRight(ValueWidth, ' '));
				} else {
					String value = GetValueString();
					if (Flags.IsSet(options, DicomDumpOptions.ShortenLongValues)) {
						if (value.Length > ValueWidth) {
							value = value.Substring(0, ValueWidth - 3) + "...";
						}
					}
					sb.Append(value.PadRight(ValueWidth, ' '));
				}
			}
			sb.AppendFormat(" # {0,4} {2}", Length, GetVM(), Tag.Entry.Name);

			if (Flags.IsSet(options, DicomDumpOptions.Restrict80CharactersPerLine)) {
				if (sb.Length > (SbLength + 79)) {
					sb.Length = SbLength + 79;
					//sb.Append(">");
				}
			}
		}
		#endregion

		#region Static Create Methods
		public static DcmElement Create(DicomTag tag) {
			DicomVR vr = tag.Entry.DefaultVR;
			return Create(tag, vr);
		}

		public static DcmElement Create(DicomTag tag, DicomVR vr) {
			return Create(tag, vr, 0, Endian.LocalMachine);
		}

		public static DcmElement Create(DicomTag tag, DicomVR vr, long pos, Endian endian) {
			return Create(tag, vr, pos, endian, null);
		}

		public static DcmElement Create(DicomTag tag, DicomVR vr, long pos, Endian endian, ByteBuffer buffer) {
			if (vr == DicomVR.SQ)
				throw new DicomDataException("Sequence Elements should be created explicitly");

			switch (vr.VR) {
			case "AE":
				return new DcmApplicationEntity(tag, pos, endian, buffer);
			case "AS":
				return new DcmAgeString(tag, pos, endian, buffer);
			case "AT":
				return new DcmAttributeTag(tag, pos, endian, buffer);
			case "CS":
				return new DcmCodeString(tag, pos, endian, buffer);
			case "DA":
				return new DcmDate(tag, pos, endian, buffer);
			case "DS":
				return new DcmDecimalString(tag, pos, endian, buffer);
			case "DT":
				return new DcmDateTime(tag, pos, endian, buffer);
			case "FD":
				return new DcmFloatingPointDouble(tag, pos, endian, buffer);
			case "FL":
				return new DcmFloatingPointSingle(tag, pos, endian, buffer);
			case "IS":
				return new DcmIntegerString(tag, pos, endian, buffer);
			case "LO":
				return new DcmLongString(tag, pos, endian, buffer);
			case "LT":
				return new DcmLongText(tag, pos, endian, buffer);
			case "OB":
				return new DcmOtherByte(tag, pos, endian, buffer);
			case "OF":
				return new DcmOtherFloat(tag, pos, endian, buffer);
			case "OW":
				return new DcmOtherWord(tag, pos, endian, buffer);
			case "PN":
				return new DcmPersonName(tag, pos, endian, buffer);
			case "SH":
				return new DcmShortString(tag, pos, endian, buffer);
			case "SL":
				return new DcmSignedLong(tag, pos, endian, buffer);
			case "SS":
				return new DcmSignedShort(tag, pos, endian, buffer);
			case "ST":
				return new DcmShortText(tag, pos, endian, buffer);
			case "TM":
				return new DcmTime(tag, pos, endian, buffer);
			case "UI":
				return new DcmUniqueIdentifier(tag, pos, endian, buffer);
			case "UL":
				return new DcmUnsignedLong(tag, pos, endian, buffer);
			case "UN":
				return new DcmUnknown(tag, pos, endian, buffer);
			case "US":
				return new DcmUnsignedShort(tag, pos, endian, buffer);
			case "UT":
				return new DcmUnlimitedText(tag, pos, endian, buffer);
			default:
				break;
			}
			throw new DicomDataException("Unhandled VR: " + vr.VR);
		}
		#endregion
	}

	#region Base Types
	public class DcmStringElement : DcmElement {
		#region Public Constructors
		public DcmStringElement(DicomTag tag, DicomVR vr) : base(tag, vr) {
		}

		public DcmStringElement(DicomTag tag, DicomVR vr, long pos, Endian endian)
			: base(tag, vr, pos, endian) {
		}

		public DcmStringElement(DicomTag tag, DicomVR vr, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, vr, pos, endian, buffer) {
		}
		#endregion

		#region Abstract Overrides
		public override int GetVM() {
			return 1;
		}

		public override string GetValueString() {
			return ByteBuffer.GetString().TrimEnd(' ', '\0');
		}
		public override void SetValueString(string val) {
			ByteBuffer.SetString(val, VR.Padding);
		}

		public override Type GetValueType() {
			return typeof(string);
		}
		public override object GetValueObject() {
			return GetValue();
		}
		public override object[] GetValueObjectArray() {
			return GetValues();
		}
		public override void SetValueObject(object val) {
			if (val.GetType() != GetValueType())
				throw new DicomDataException("Invalid type for Element VR!");
			SetValue((string)val);
		}
		public override void SetValueObjectArray(object[] vals) {
			if (vals.Length == 0)
				SetValues(new string[0]);
			if (vals[0].GetType() != GetValueType())
				throw new DicomDataException("Invalid type for Element VR!");
			SetValues((string[])vals);
		}
		#endregion

		#region Public Members
		public string GetValue() {
			return GetValue(0);
		}

		public string GetValue(int index) {
			if (index != 0)
				throw new DicomDataException("Non-zero index used for single value string element");
			return GetValueString().TrimEnd(' ', '\0');
		}

		public string[] GetValues() {
			return GetValueString().TrimEnd(' ', '\0').Split('\\');
		}

		public List<string> GetValueList() {
			return new List<string>(GetValues());
		}

		public void SetValue(string value) {
			ByteBuffer.SetString(value, VR.Padding);
		}

		public void SetValues(string[] values) {
			ByteBuffer.SetString(string.Join("\\", values), VR.Padding);
		}

		public void SetValues(IEnumerable<string> values) {
			StringBuilder sb = new StringBuilder();
			var valueEnum = values.GetEnumerator();
			if(valueEnum.MoveNext()) {
				sb.Append(valueEnum.Current);
				while(valueEnum.MoveNext()) {
					sb.Append("\\");
					sb.Append(valueEnum.Current);
				}
			}
			ByteBuffer.SetString(sb.ToString(), VR.Padding);
		}

		#endregion
	}

	public class DcmMultiStringElement : DcmElement {
		#region Public Constructors
		public DcmMultiStringElement(DicomTag tag, DicomVR vr) : base(tag, vr) {
		}

		public DcmMultiStringElement(DicomTag tag, DicomVR vr, long pos, Endian endian)
			: base(tag, vr, pos, endian) {
		}

		public DcmMultiStringElement(DicomTag tag, DicomVR vr, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, vr, pos, endian, buffer) {
		}
		#endregion

		#region Abstract Overrides
		private int _vm = -1;
		public override int GetVM() {
			if (_vm == -1)
				_vm = GetValues().Length;
			return _vm;
		}

		public override string GetValueString() {
			return ByteBuffer.GetString().TrimEnd(' ', '\0');
		}
		public override void SetValueString(string val) {
			ByteBuffer.SetString(val, VR.Padding);
		}

		public override Type GetValueType() {
			return typeof(string);
		}
		public override object GetValueObject() {
			return GetValue();
		}
		public override object[] GetValueObjectArray() {
			return GetValues();
		}
		public override void SetValueObject(object val) {
			if (val.GetType() != GetValueType())
				throw new DicomDataException("Invalid type for Element VR!");
			SetValue((string)val);
		}
		public override void SetValueObjectArray(object[] vals) {
			if (vals.Length == 0)
				SetValues(new string[0]);
			if (vals[0].GetType() != GetValueType())
				throw new DicomDataException("Invalid type for Element VR!");
			SetValues((string[])vals);
		}
		#endregion

		#region Public Members
		public string GetValue() {
			return GetValue(0);
		}

		public string GetValue(int index) {
			string[] vals = GetValues();
			if (index >= vals.Length)
				throw new DicomDataException("Value index out of range");
			return vals[index].TrimEnd(' ', '\0');
		}

		public string[] GetValues() {
			return GetValueString().TrimEnd(' ', '\0').Split('\\');
		}

		public List<string> GetValueList() {
			return new List<string>(GetValues());
		}

		public void SetValue(string value) {
			ByteBuffer.SetString(value, VR.Padding);
		}

		public void SetValues(string[] values) {
			ByteBuffer.SetString(string.Join("\\", values), VR.Padding);
		}

		public void SetValues(IEnumerable<string> values) {
			StringBuilder sb = new StringBuilder();
			var valueEnum = values.GetEnumerator();
			if (valueEnum.MoveNext()) {
				sb.Append(valueEnum.Current);
				while (valueEnum.MoveNext()) {
					sb.Append("\\");
					sb.Append(valueEnum.Current);
				}
			}
			ByteBuffer.SetString(sb.ToString(), VR.Padding);
		}

		#endregion
	}

	public class DcmDateElementBase : DcmMultiStringElement {
		#region Protected Members
		protected string[] _formats;
		#endregion

		#region Public Constructors
		public DcmDateElementBase(DicomTag tag, DicomVR vr)
			: base(tag, vr) {
		}

		public DcmDateElementBase(DicomTag tag, DicomVR vr, long pos, Endian endian)
			: base(tag, vr, pos, endian) {
		}

		public DcmDateElementBase(DicomTag tag, DicomVR vr, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, vr, pos, endian, buffer) {
		}
		#endregion

		#region Private Members
		private DateTime ParseDate(string date) {
			try {
				if (_formats != null)
					return DateTime.ParseExact(date, _formats, CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault);
				else
					return DateTime.Parse(date, CultureInfo.InvariantCulture);
			}
			catch {
				return DateTime.Today;
			}
		}

		private DateTime[] ParseDateRange(string date) {
			if (date == null || date.Length == 0) {
				DateTime[] r = new DateTime[2];
				r[0] = DateTime.MinValue;
				r[1] = DateTime.MinValue;
				return r;
			}

			int hypPos = date.IndexOf((Char)'-');
			if (hypPos == -1) {
				DateTime[] d = new DateTime[1];
				d[0] = ParseDate(date);
				return d;
			}
			DateTime[] range = new DateTime[2];
			try {
				range[0] = ParseDate(date.Substring(0, hypPos));
			} catch {
				range[0] = DateTime.MinValue;
			}
			try {
				range[1] = ParseDate(date.Substring(hypPos + 1));
			} catch {
				range[1] = DateTime.MinValue;
			}
			return range;
		}
		#endregion

		#region Abstract Overrides
		public override Type GetValueType() {
			return typeof(DateTime);
		}
		public override object GetValueObject() {
			return GetDateTime();
		}
		public override object[] GetValueObjectArray() {
			throw new DicomDataException("GetValueObjectArray() should not be called for DateTime types!");
		}
		public override void SetValueObject(object val) {
			if (val.GetType() == typeof(DcmDateRange))
				SetDateTimeRange((DcmDateRange)val);
			else if (val.GetType() == typeof(DateTime))
				SetDateTime((DateTime)val);
			else if (val.GetType() == typeof(string))
				SetValue((string)val);
			else
				throw new DicomDataException("Invalid type for Element VR!");
		}
		public override void SetValueObjectArray(object[] vals) {
			throw new DicomDataException("SetValueObjectArray() should not be called for DateTime types!");
		}
		#endregion

		#region Public Members
		public DateTime GetDateTime() {
			return GetDateTime(0);
		}
		public DateTime GetDateTime(int index) {
			return ParseDate(GetValue(index));
		}

		public DateTime[] GetDateTimes() {
			string[] strings = GetValues();
			DateTime[] values = new DateTime[strings.Length];
			for (int i = 0; i < strings.Length; i++) {
				values[i] = ParseDate(strings[i]);
			}
			return values;
		}

		public List<DateTime> GetDateTimeList() {
			return new List<DateTime>(GetDateTimes());
		}

		public DcmDateRange GetDateTimeRange() {
			return new DcmDateRange(ParseDateRange(GetValue(0)));
		}

		public void SetDateTime(DateTime value) {
			if (_formats != null)
				SetValue(value.ToString(_formats[0]));
			else
				SetValue(value.ToString());
		}

		public void SetDateTimes(DateTime[] values) {
			string[] strings = new string[values.Length];
			for (int i = 0; i < strings.Length; i++) {
				if (_formats != null)
					strings[i] = values[i].ToString(_formats[0]);
				else
					strings[i] = values[i].ToString();
			}
			SetValues(strings);
		}

		public void SetDateTimes(IEnumerable<DateTime> values) {
			SetDateTimes(new List<DateTime>(values).ToArray());
		}

		public void SetDateTimeRange(DcmDateRange range) {
			if (range != null)
				SetValue(range.ToString(_formats[0]));
			else
				SetValue(String.Empty);
		}
		#endregion
	}

	public class DcmValueElement<T> : DcmElement {
		#region Protected Members
		protected string StringFormat;
		protected NumberStyles NumberStyle;
		#endregion

		#region Public Constructors
		public DcmValueElement(DicomTag tag, DicomVR vr)
			: base(tag, vr) {
			StringFormat = "{0}";
			NumberStyle = NumberStyles.Any;
		}

		public DcmValueElement(DicomTag tag, DicomVR vr, long pos, Endian endian)
			: base(tag, vr, pos, endian) {
			StringFormat = "{0}";
			NumberStyle = NumberStyles.Any;
		}

		public DcmValueElement(DicomTag tag, DicomVR vr, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, vr, pos, endian, buffer) {
			StringFormat = "{0}";
			NumberStyle = NumberStyles.Any;
		}
		#endregion

		#region Abstract Overrides
		public override int GetVM() {
			return ByteBuffer.Length / VR.UnitSize;
		}

		public override string GetValueString() {
			T[] vals = GetValues();
			StringBuilder sb = new StringBuilder();
			foreach (T val in vals) {
				sb.AppendFormat(StringFormat, val).Append('\\');
			}
			if (sb.Length > 0) {
				sb.Length = sb.Length - 1;
			}
			return sb.ToString();
		}

		private object ParseNumber(string val) {
			try {
				if (typeof(T) == typeof(byte)) {
					return byte.Parse(val, NumberStyle, CultureInfo.InvariantCulture);
				}
				if (typeof(T) == typeof(sbyte)) {
					return sbyte.Parse(val, NumberStyle, CultureInfo.InvariantCulture);
				}
				if (typeof(T) == typeof(short)) {
					return short.Parse(val, NumberStyle, CultureInfo.InvariantCulture);
				}
				if (typeof(T) == typeof(ushort)) {
					return ushort.Parse(val, NumberStyle, CultureInfo.InvariantCulture);
				}
				if (typeof(T) == typeof(int)) {
					return int.Parse(val, NumberStyle, CultureInfo.InvariantCulture);
				}
				if (typeof(T) == typeof(uint)) {
					return uint.Parse(val, NumberStyle, CultureInfo.InvariantCulture);
				}
				if (typeof(T) == typeof(long)) {
					return long.Parse(val, NumberStyle, CultureInfo.InvariantCulture);
				}
				if (typeof(T) == typeof(ulong)) {
					return ulong.Parse(val, NumberStyle, CultureInfo.InvariantCulture);
				}
				if (typeof(T) == typeof(float)) {
					return float.Parse(val, NumberStyle, CultureInfo.InvariantCulture);
				}
				if (typeof(T) == typeof(double)) {
					return double.Parse(val, NumberStyle, CultureInfo.InvariantCulture);
				}
			} catch { }
			return null;
		}

		public override void SetValueString(string val) {
			if (val == null || val == String.Empty) {
				SetValues(new T[0]);
				return;
			}
			string[] strs = val.Split('\\');
			T[] vals = new T[strs.Length];
			for (int i = 0; i < vals.Length; i++) {
				vals[i] = (T)ParseNumber(strs[i]);
			}
			SetValues(vals);
		}

		public override Type GetValueType() {
			return typeof(T);
		}
		public override object GetValueObject() {
			if (GetVM() == 0)
				return null;
			return GetValue();
		}
		public override object[] GetValueObjectArray() {
			T[] v = GetValues();
			object[] o = new object[v.Length];
			Array.Copy(v, o, v.Length);
			return o;
		}
		public override void SetValueObject(object val) {
			if (val.GetType() != GetValueType())
				throw new DicomDataException("Invalid type for Element VR!");
			SetValue((T)val);
		}
		public override void SetValueObjectArray(object[] vals) {
			if (vals.Length == 0)
				SetValues(new T[0]);
			if (vals[0].GetType() != GetValueType())
				throw new DicomDataException("Invalid type for Element VR!");
			T[] v = new T[vals.Length];
			Array.Copy(vals, v, vals.Length);
			SetValues(v);
		}
		#endregion

		#region Public Members
		public T GetValue() {
			return GetValue(0);
		}

		public T GetValue(int index) {
			if (index >= GetVM())
				throw new DicomDataException("Value index out of range");
			Endian = Endian.LocalMachine;
			T[] vals = new T[1];
			Buffer.BlockCopy(ByteBuffer.ToBytes(), index * VR.UnitSize,
				vals, 0, VR.UnitSize);
			return vals[0];
		}

		public T[] GetValues() {
			Endian = Endian.LocalMachine;
			T[] vals = new T[GetVM()];
			Buffer.BlockCopy(ByteBuffer.ToBytes(), 0, vals, 0, vals.Length * VR.UnitSize);
			return vals;
		}

		public List<T> GetValueList() {
			return new List<T>(GetValues());
		}

		public void SetValue(T value) {
			T[] vals = new T[1];
			vals[0] = value;
			SetValues(vals);
		}

		public void SetValues(T[] vals) {
			Endian = Endian.LocalMachine;
			byte[] bytes = new byte[vals.Length * VR.UnitSize];
			Buffer.BlockCopy(vals, 0, bytes, 0, bytes.Length);
			ByteBuffer.FromBytes(bytes);
		}

		public void SetValues(IEnumerable<T> vals) {
			SetValues(new List<T>(vals).ToArray());
		}

		#endregion
	}
	#endregion

	#region Value Types
	/// <summary>Application Entity (AE)</summary>
	public class DcmApplicationEntity : DcmMultiStringElement {
		#region Public Constructors
		public DcmApplicationEntity(DicomTag tag)
			: base(tag, DicomVR.AE) {
		}

		public DcmApplicationEntity(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.AE, pos, endian) {
		}

		public DcmApplicationEntity(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.AE, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>Age String (AS)</summary>
	public class DcmAgeString : DcmMultiStringElement {
		#region Public Constructors
		public DcmAgeString(DicomTag tag)
			: base(tag, DicomVR.AS) {
		}

		public DcmAgeString(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.AS, pos, endian) {
		}

		public DcmAgeString(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.AS, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>Attribute Tag (AT)</summary>
	public class DcmAttributeTag : DcmElement {
		#region Public Constructors
		public DcmAttributeTag(DicomTag tag)
			: base(tag, DicomVR.AT) {
		}

		public DcmAttributeTag(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.AT, pos, endian) {
		}

		public DcmAttributeTag(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.AT, pos, endian, buffer) {
		}
		#endregion

		#region Abstract Overrides
		public override int GetVM() {
			return ByteBuffer.Length / 4;
		}

		public override string GetValueString() {
			DicomTag[] tags = GetValues();
			StringBuilder sb = new StringBuilder();
			foreach (DicomTag tag in tags) {
				sb.AppendFormat("{0:X4}{1:X4}\\", tag.Group, tag.Element);
			}
			if (sb.Length > 0) {
				sb.Length = sb.Length - 1;
			}
			return sb.ToString();
		}
		public override void SetValueString(string val) {
			string[] strs = val.Split('\\');
			DicomTag[] tags = new DicomTag[strs.Length];
			for (int i = 0; i < tags.Length; i++) {
				if (strs[i].Length == 8) {
					string gs = strs[i].Substring(0, 4);
					string es = strs[i].Substring(4, 4);
					ushort g = ushort.Parse(gs, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
					ushort e = ushort.Parse(es, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
					tags[i] = new DicomTag(g, e);
				}
			}
			SetValues(tags);
		}

		public override Type GetValueType() {
			return typeof(DicomTag);
		}
		public override object GetValueObject() {
			return GetValue();
		}
		public override object[] GetValueObjectArray() {
			return GetValues();
		}
		public override void SetValueObject(object val) {
			if (val.GetType() != GetValueType())
				throw new DicomDataException("Invalid type for Element VR!");
			SetValue((DicomTag)val);
		}
		public override void SetValueObjectArray(object[] vals) {
			if (vals.Length == 0)
				SetValues(new DicomTag[0]);
			if (vals[0].GetType() != GetValueType())
				throw new DicomDataException("Invalid type for Element VR!");
			SetValues((DicomTag[])vals);
		}
		#endregion

		#region Public Members
		public DicomTag GetValue() {
			return GetValue(0);
		}

		public DicomTag GetValue(int index) {
			if (index >= GetVM())
				throw new DicomDataException("Value index out of range");
			Endian = Endian.LocalMachine;
			ushort[] u16s = new ushort[2];
			Buffer.BlockCopy(ByteBuffer.ToBytes(), index * 4, u16s, 0, 4);
			return new DicomTag(u16s[0], u16s[1]);
		}

		public DicomTag[] GetValues() {
			Endian = Endian.LocalMachine;
			ushort[] u16s = new ushort[GetVM() * 2];
			Buffer.BlockCopy(ByteBuffer.ToBytes(), 0, u16s, 0, u16s.Length * 2);
			DicomTag[] tags = new DicomTag[GetVM()];
			for (int i = 0, n = 0; i < tags.Length; i++) {
				tags[i] = new DicomTag(u16s[n++], u16s[n++]);
			}
			return tags;
		}

		public List<DicomTag> GetValueList() {
			return new List<DicomTag>(GetValues());
		}

		public void SetValue(DicomTag val) {
			Endian = Endian.LocalMachine;
			ByteBuffer.Clear();
			ByteBuffer.Writer.Write(val.Group);
			ByteBuffer.Writer.Write(val.Element);
		}

		public void SetValues(DicomTag[] vals) {
			Endian = Endian.LocalMachine;
			ByteBuffer.Clear();
			for (int i = 0; i < vals.Length; i++) {
				DicomTag val = vals[i];
				ByteBuffer.Writer.Write(val.Group);
				ByteBuffer.Writer.Write(val.Element);
			}
		}

		public void SetValues(IEnumerable<DicomTag> vals) {
			Endian = Endian.LocalMachine;
			ByteBuffer.Clear();
			foreach (DicomTag val in vals) {
				ByteBuffer.Writer.Write(val.Group);
				ByteBuffer.Writer.Write(val.Element);
			}
		}

		#endregion

		#region DcmItem Methods
		protected override void ChangeEndianInternal() {
			if (ByteBuffer.Endian != Endian) {
				ByteBuffer.Endian = Endian;
				ByteBuffer.Swap(2);
			}
		}
		#endregion
	}

	/// <summary>Code String (CS)</summary>
	public class DcmCodeString : DcmMultiStringElement {
		#region Public Constructors
		public DcmCodeString(DicomTag tag)
			: base(tag, DicomVR.CS) {
		}

		public DcmCodeString(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.CS, pos, endian) {
		}

		public DcmCodeString(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.CS, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>Date (DA)</summary>
	public class DcmDate : DcmDateElementBase {
		#region Public Constructors
		public DcmDate(DicomTag tag)
			: base(tag, DicomVR.DA) {
			InitFormats();
		}

		public DcmDate(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.DA, pos, endian) {
			InitFormats();
		}

		public DcmDate(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.DA, pos, endian, buffer) {
			InitFormats();
		}

		private void InitFormats() {
			if (_formats == null) {
				_formats = new string[6];
				_formats[0] = "yyyyMMdd";
				_formats[1] = "yyyy.MM.dd";
				_formats[2] = "yyyy/MM/dd";
				_formats[3] = "yyyy";
				_formats[4] = "yyyyMM";
				_formats[5] = "yyyy.MM";
			}
		}
		#endregion
	}

	/// <summary>Decimal String (DS)</summary>
	public class DcmDecimalString : DcmMultiStringElement {
		#region Public Constructors
		public DcmDecimalString(DicomTag tag)
			: base(tag, DicomVR.DS) {
		}

		public DcmDecimalString(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.DS, pos, endian) {
		}

		public DcmDecimalString(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.DS, pos, endian, buffer) {
		}
		#endregion

		#region Public Members
		public float GetFloat() {
			return GetFloat(0);
		}

		public float GetFloat(int index) {
			string val = GetValue(index);
			return float.Parse(val, CultureInfo.InvariantCulture);
		}

		public float[] GetFloats() {
			float[] vals = new float[GetVM()];
			for (int i = 0; i < vals.Length; i++) {
				vals[i] = GetFloat(i);
			}
			return vals;
		}

		public List<float> GetFloatList() {
			List<float> vals = new List<float>(GetVM());
			for (int i = 0; i < vals.Count; i++) {
				vals[i] = GetFloat(i);
			}
			return vals;
		}

		public void SetFloat(float value) {
			SetValue(value.ToString());
		}
		public void SetFloat(float value, int round) {
			SetValue(Math.Round(value, round).ToString());
		}

		public void SetFloats(float[] values) {
			string[] strs = new string[values.Length];
			for (int i = 0; i < strs.Length; i++) {
				strs[i] = values[i].ToString();
			}
			SetValues(strs);
		}
		public void SetFloats(float[] values, int round) {
			string[] strs = new string[values.Length];
			for (int i = 0; i < strs.Length; i++) {
				strs[i] = Math.Round(values[i], round).ToString();
			}
			SetValues(strs);
		}

		public void SetFloats(IEnumerable<float> values) {
			SetFloats(new List<float>(values).ToArray());
		}
		public void SetFloats(IEnumerable<float> values, int round) {
			SetFloats(new List<float>(values).ToArray(), round);
		}

		public double GetDouble() {
			return GetDouble(0);
		}

		public double GetDouble(int index) {
			string val = GetValue(index);
			return double.Parse(val, CultureInfo.InvariantCulture);
		}

		public double[] GetDoubles() {
			double[] vals = new double[GetVM()];
			for (int i = 0; i < vals.Length; i++) {
				vals[i] = GetDouble(i);
			}
			return vals;
		}

		public List<double> GetDoubleList() {
			List<double> vals = new List<double>(GetVM());
			for(int i = 0; i < vals.Count; i++) {
				vals[i] = GetDouble(i);
			}
			return vals;
		}

		public void SetDouble(double value) {
			SetValue(value.ToString());
		}
		public void SetDouble(double value, int round) {
			SetValue(Math.Round(value, round).ToString());
		}

		public void SetDoubles(double[] values) {
			string[] strs = new string[values.Length];
			for (int i = 0; i < strs.Length; i++) {
				strs[i] = values[i].ToString();
			}
			SetValues(strs);
		}
		public void SetDoubles(double[] values, int round) {
			string[] strs = new string[values.Length];
			for (int i = 0; i < strs.Length; i++) {
				strs[i] = Math.Round(values[i], round).ToString();
			}
			SetValues(strs);
		}

		public void SetDoubles(IEnumerable<double> values) {
			SetDoubles(new List<double>(values).ToArray());
		}
		public void SetDoubles(IEnumerable<double> values, int round) {
			SetDoubles(new List<double>(values).ToArray(), round);
		}

		public decimal GetDecimal() {
			return GetDecimal(0);
		}

		public decimal GetDecimal(int index) {
			string val = GetValue(index);
			return decimal.Parse(val, CultureInfo.InvariantCulture);
		}

		public decimal[] GetDecimals() {
			decimal[] vals = new decimal[GetVM()];
			for (int i = 0; i < vals.Length; i++) {
				vals[i] = GetDecimal(i);
			}
			return vals;
		}

		public List<decimal> GetDecimalList() {
			List<decimal> vals = new List<decimal>(GetVM());
			for (int i = 0; i < vals.Count; i++) {
				vals[i] = GetDecimal(i);
			}
			return vals;
		}

		public void SetDecimal(decimal value) {
			SetValue(value.ToString());
		}

		public void SetDecimals(decimal[] values) {
			string[] strs = new string[values.Length];
			for (int i = 0; i < strs.Length; i++) {
				strs[i] = values[i].ToString();
			}
			SetValues(strs);
		}

		public void SetDecimals(IEnumerable<decimal> values) {
			SetDecimals(new List<decimal>(values).ToArray());
		}

		#endregion
	}

	/// <summary>Date Time (DT)</summary>
	public class DcmDateTime : DcmDateElementBase {
		#region Public Constructors
		public DcmDateTime(DicomTag tag)
			: base(tag, DicomVR.DT) {
		}

		public DcmDateTime(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.DT, pos, endian) {
		}

		public DcmDateTime(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.DT, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>Floating Point Double (FD)</summary>
	public class DcmFloatingPointDouble : DcmValueElement<double> {
		#region Public Constructors
		public DcmFloatingPointDouble(DicomTag tag)
			: base(tag, DicomVR.FD) {
		}

		public DcmFloatingPointDouble(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.FD, pos, endian) {
		}

		public DcmFloatingPointDouble(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.FD, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>Floating Point Single (FL)</summary>
	public class DcmFloatingPointSingle : DcmValueElement<float> {
		#region Public Constructors
		public DcmFloatingPointSingle(DicomTag tag) 
			: base(tag, DicomVR.FL) {
		}

		public DcmFloatingPointSingle(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.FL, pos, endian) {
		}

		public DcmFloatingPointSingle(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.FL, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>Integer String (IS)</summary>
	public class DcmIntegerString : DcmMultiStringElement {
		#region Public Constructors
		public DcmIntegerString(DicomTag tag)
			: base(tag, DicomVR.IS) {
		}

		public DcmIntegerString(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.IS, pos, endian) {
		}

		public DcmIntegerString(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.IS, pos, endian, buffer) {
		}
		#endregion

		#region Access Methods
		public int GetInt32() {
			return GetInt32(0);
		}

		public int GetInt32(int index) {
			string val = GetValue(index);
			return int.Parse(val, CultureInfo.InvariantCulture);
		}

		public int[] GetInt32s() {
			int[] ints = new int[GetVM()];
			for (int i = 0; i < ints.Length; i++) {
				ints[i] = GetInt32(i);
			}
			return ints;
		}

		public List<int> GetInt32List() {
			List<int> ints = new List<int>(GetVM());
			for (int i = 0; i < ints.Count; i++) {
				ints[i] = GetInt32(i);
			}
			return ints;
		}

		public void SetInt32(int value) {
			SetValue(value.ToString());
		}

		public void SetInt32s(int[] values) {
			string[] strs = new string[values.Length];
			for (int i = 0; i < strs.Length; i++) {
				strs[i] = values[i].ToString();
			}
			SetValues(strs);
		}

		public void SetInt32s(IEnumerable<int> values) {
			SetInt32s(new List<int>(values).ToArray());
		}

		#endregion
	}

	/// <summary>Long String (LO)</summary>
	public class DcmLongString : DcmStringElement {
		#region Public Constructors
		public DcmLongString(DicomTag tag)
			: base(tag, DicomVR.LO) {
		}

		public DcmLongString(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.LO, pos, endian) {
		}

		public DcmLongString(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.LO, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>Long Text (LT)</summary>
	public class DcmLongText : DcmStringElement {
		#region Public Constructors
		public DcmLongText(DicomTag tag)
			: base(tag, DicomVR.LT) {
		}

		public DcmLongText(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.LT, pos, endian) {
		}

		public DcmLongText(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.LT, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>Other Byte (OB)</summary>
	public class DcmOtherByte : DcmValueElement<byte> {
		#region Public Constructors
		public DcmOtherByte(DicomTag tag)
			: base(tag, DicomVR.OB) {
			StringFormat = "{0:X2}";
			NumberStyle = NumberStyles.HexNumber;
		}

		public DcmOtherByte(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.OB, pos, endian) {
			StringFormat = "{0:X2}";
			NumberStyle = NumberStyles.HexNumber;
		}

		public DcmOtherByte(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.OB, pos, endian, buffer) {
			StringFormat = "{0:X2}";
			NumberStyle = NumberStyles.HexNumber;
		}
		#endregion
	}

	/// <summary>Other Word (OW)</summary>
	public class DcmOtherWord : DcmValueElement<ushort> {
		#region Public Constructors
		public DcmOtherWord(DicomTag tag)
			: base(tag, DicomVR.OW) {
			StringFormat = "{0:X4}";
			NumberStyle = NumberStyles.HexNumber;
		}

		public DcmOtherWord(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.OW, pos, endian) {
			StringFormat = "{0:X4}";
			NumberStyle = NumberStyles.HexNumber;
		}

		public DcmOtherWord(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.OW, pos, endian, buffer) {
			StringFormat = "{0:X4}";
			NumberStyle = NumberStyles.HexNumber;
		}
		#endregion
	}

	/// <summary>Other Float (OF)</summary>
	public class DcmOtherFloat : DcmValueElement<float> {
		#region Public Constructors
		public DcmOtherFloat(DicomTag tag)
			: base(tag, DicomVR.OF) {
		}

		public DcmOtherFloat(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.OF, pos, endian) {
		}

		public DcmOtherFloat(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.OF, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>Person Name (PN)</summary>
	public class DcmPersonName : DcmStringElement {
		#region Public Constructors
		public DcmPersonName(DicomTag tag)
			: base(tag, DicomVR.PN) {
		}

		public DcmPersonName(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.PN, pos, endian) {
		}

		public DcmPersonName(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.PN, pos, endian, buffer) {
		}
		#endregion

        #region Public Methods

        public string GetFamilyNameComplex()
        {
            return GetNameComponent(0);
        }

        public string GetGivenNameComplex()
        {
            return GetNameComponent(1);
        }

	    public string GetMiddleName()
	    {
            return GetNameComponent(2);
	    }

	    public string GetNamePrefix()
	    {
            return GetNameComponent(3);
	    }

	    public string GetNameSuffix()
	    {
            return GetNameComponent(4);
	    }

        #endregion

        #region Private Methods

        private string GetNameComponent(int iIndex)
        {
            string[] split;
            var val = GetValueString();
            return val != null && (split = val.Split('^')).Length > iIndex ? split[iIndex] : String.Empty;            
        }

        #endregion
    }

	/// <summary>Short String (SH)</summary>
	public class DcmShortString : DcmStringElement {
		#region Public Constructors
		public DcmShortString(DicomTag tag)
			: base(tag, DicomVR.SH) {
		}

		public DcmShortString(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.SH, pos, endian) {
		}

		public DcmShortString(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.SH, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>Signed Long (SL)</summary>
	public class DcmSignedLong : DcmValueElement<int> {
		#region Public Constructors
		public DcmSignedLong(DicomTag tag)
			: base(tag, DicomVR.SL) {
		}

		public DcmSignedLong(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.SL, pos, endian) {
		}

		public DcmSignedLong(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.SL, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>Signed Short (SS)</summary>
	public class DcmSignedShort : DcmValueElement<short> {
		#region Public Constructors
		public DcmSignedShort(DicomTag tag)
			: base(tag, DicomVR.SS) {
		}

		public DcmSignedShort(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.SS, pos, endian) {
		}

		public DcmSignedShort(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.SS, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>Short Text (ST)</summary>
	public class DcmShortText : DcmStringElement {
		#region Public Constructors
		public DcmShortText(DicomTag tag)
			: base(tag, DicomVR.ST) {
		}

		public DcmShortText(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.ST, pos, endian) {
		}

		public DcmShortText(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.ST, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>Time (TM)</summary>
	public class DcmTime : DcmDateElementBase {
		#region Public Constructors
		public DcmTime(DicomTag tag)
			: base(tag, DicomVR.TM) {
			InitFormats();
		}

		public DcmTime(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.TM, pos, endian) {
			InitFormats();
		}

		public DcmTime(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.TM, pos, endian, buffer) {
			InitFormats();
		}

		private void InitFormats() {
			if (_formats == null) {
				_formats = new string[37];
				_formats[0] = "HHmmss";
				_formats[1] = "HH";
				_formats[2] = "HHmm";
				_formats[3] = "HHmmssf";
				_formats[4] = "HHmmssff";
				_formats[5] = "HHmmssfff";
				_formats[6] = "HHmmssffff";
				_formats[7] = "HHmmssfffff";
				_formats[8] = "HHmmssffffff";
				_formats[9] = "HHmmss.f";
				_formats[10] = "HHmmss.ff";
				_formats[11] = "HHmmss.fff";
				_formats[12] = "HHmmss.ffff";
				_formats[13] = "HHmmss.fffff";
				_formats[14] = "HHmmss.ffffff";
				_formats[15] = "HH.mm";
				_formats[16] = "HH.mm.ss";
				_formats[17] = "HH.mm.ss.f";
				_formats[18] = "HH.mm.ss.ff";
				_formats[19] = "HH.mm.ss.fff";
				_formats[20] = "HH.mm.ss.ffff";
				_formats[21] = "HH.mm.ss.fffff";
				_formats[22] = "HH.mm.ss.ffffff";
				_formats[23] = "HH:mm";
				_formats[24] = "HH:mm:ss";
				_formats[25] = "HH:mm:ss:f";
				_formats[26] = "HH:mm:ss:ff";
				_formats[27] = "HH:mm:ss:fff";
				_formats[28] = "HH:mm:ss:ffff";
				_formats[29] = "HH:mm:ss:fffff";
				_formats[30] = "HH:mm:ss:ffffff";
				_formats[25] = "HH:mm:ss.f";
				_formats[26] = "HH:mm:ss.ff";
				_formats[27] = "HH:mm:ss.fff";
				_formats[28] = "HH:mm:ss.ffff";
				_formats[29] = "HH:mm:ss.fffff";
				_formats[30] = "HH:mm:ss.ffffff";
			}
		}
		#endregion
	}

	/// <summary>Unique Identifier (UI)</summary>
	public class DcmUniqueIdentifier : DcmMultiStringElement {
		#region Public Constructors
		public DcmUniqueIdentifier(DicomTag tag)
			: base(tag, DicomVR.UI) {
		}

		public DcmUniqueIdentifier(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.UI, pos, endian) {
		}

		public DcmUniqueIdentifier(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.UI, pos, endian, buffer) {
		}
		#endregion

		#region Access Methods
		public DicomUID GetUID() {
			return DicomUID.Lookup(GetValue());
		}

		public DicomTransferSyntax GetTS() {
			return DicomTransferSyntax.Lookup(GetUID());
		}

		public void SetUID(DicomUID ui) {
			SetValue(ui.UID);
		}

		public void SetTS(DicomTransferSyntax ts) {
			SetUID(ts.UID);
		}

		public override void SetValueObject(object val) {
			if (val is DicomUID)
				SetUID(val as DicomUID);
			else
				base.SetValueObject(val);
		}
		#endregion
	}

	/// <summary>Unsigned Long (UL)</summary>
	public class DcmUnsignedLong : DcmValueElement<uint> {
		#region Public Constructors
		public DcmUnsignedLong(DicomTag tag)
			: base(tag, DicomVR.UL) {
		}

		public DcmUnsignedLong(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.UL, pos, endian) {
		}

		public DcmUnsignedLong(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.UL, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>Unknown (UN)</summary>
	public class DcmUnknown : DcmValueElement<byte> {
		#region Public Constructors
		public DcmUnknown(DicomTag tag)
			: base(tag, DicomVR.UN) {
			StringFormat = "{0:X2}";
			NumberStyle = NumberStyles.HexNumber;
		}

		public DcmUnknown(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.UN, pos, endian) {
			StringFormat = "{0:X2}";
			NumberStyle = NumberStyles.HexNumber;
		}

		public DcmUnknown(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.UN, pos, endian, buffer) {
			StringFormat = "{0:X2}";
			NumberStyle = NumberStyles.HexNumber;
		}
		#endregion
	}

	/// <summary>Unsigned Short (US)</summary>
	public class DcmUnsignedShort : DcmValueElement<ushort> {
		#region Public Constructors
		public DcmUnsignedShort(DicomTag tag)
			: base(tag, DicomVR.US) {
		}

		public DcmUnsignedShort(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.US, pos, endian) {
		}

		public DcmUnsignedShort(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.US, pos, endian, buffer) {
		}
		#endregion
	}

	/// <summary>Unlimited Text (UT)</summary>
	public class DcmUnlimitedText : DcmStringElement {
		#region Public Constructors
		public DcmUnlimitedText(DicomTag tag)
			: base(tag, DicomVR.UT) {
		}

		public DcmUnlimitedText(DicomTag tag, long pos, Endian endian)
			: base(tag, DicomVR.UT, pos, endian) {
		}

		public DcmUnlimitedText(DicomTag tag, long pos, Endian endian, ByteBuffer buffer)
			: base(tag, DicomVR.UT, pos, endian, buffer) {
		}
		#endregion
	}
	#endregion
}
