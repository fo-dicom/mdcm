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
using System.Reflection;
using System.Text;

using Dicom.Codec;
using Dicom.IO;

namespace Dicom.Data {
	public class DcmDataset {
		#region Private Members
		private SortedList<DicomTag, DcmItem> _items;

		private long _streamPosition = 0;
		private uint _streamLength = 0xffffffff;

		private DicomTransferSyntax _transferSyntax;
		private object _userState;
		#endregion

		#region Public Constructors
		public DcmDataset() : this(DicomTransferSyntax.ExplicitVRLittleEndian) {
		}

		public DcmDataset(DicomTransferSyntax transferSyntax) {
			_transferSyntax = transferSyntax;
			_items = new SortedList<DicomTag, DcmItem>(new DicomTagComparer());
		}

		public DcmDataset(long streamPosition, uint lengthInStream) : this(streamPosition, lengthInStream, DicomTransferSyntax.ExplicitVRLittleEndian) {
		}

		public DcmDataset(long streamPosition, uint lengthInStream, DicomTransferSyntax transferSyntax) {
			_streamPosition = streamPosition;
			_streamLength = lengthInStream;
			_transferSyntax = transferSyntax;
			_items = new SortedList<DicomTag, DcmItem>(new DicomTagComparer());
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Position of this dataset in the source stream.
		/// </summary>
		public long StreamPosition {
			get { return _streamPosition; }
		}

		/// <summary>
		/// Length of this dataset in the source stream.
		/// </summary>
		public uint StreamLength {
			get { return _streamLength; }
		}

		/// <summary>
		/// Transfer syntax used to encode the elements in this dataset.
		/// </summary>
		public DicomTransferSyntax InternalTransferSyntax {
			get { return _transferSyntax; }
		}

		/// <summary>
		/// List of the items contained in this dataset.
		/// </summary>
		public IList<DcmItem> Elements {
			get { return _items.Values; }
		}

		/// <summary>
		/// User state object
		/// </summary>
		public object UserState {
			get { return _userState; }
			set { _userState = value; }
		}

		/// <summary>
		/// Specific Character Set
		/// </summary>
		public Encoding SpecificCharacterSetEncoding {
			get {
				DcmCodeString enc = GetCS(DicomTags.SpecificCharacterSet);
				if (enc == null || enc.Length == 0)
					return DcmEncoding.Default;
				return DcmEncoding.GetEncodingForSpecificCharacterSet(enc.GetValue());
			}
			set {
				string charSet = DcmEncoding.GetSpecificCharacterSetForEncoding(value);
				AddElementWithValue(DicomTags.SpecificCharacterSet, charSet);

				foreach (DcmItem item in Elements) {
					if (item is DcmElement && item.VR.IsEncodedString) {
						DcmElement elem = (DcmElement)item;

						if (elem.Length == 0)
							continue;

						string elemValue = elem.ByteBuffer.GetString();
						elem.ByteBuffer.Encoding = value;
						elem.ByteBuffer.SetString(elemValue, elem.VR.Padding);
					}
				}
			}
		}
		#endregion

		#region Internal Use Methods
		public uint CalculateGroupWriteLength(ushort group, DicomTransferSyntax syntax, DicomWriteOptions options) {
			uint length = 0;
			foreach (DcmItem item in _items.Values) {
				if (item.Tag.Group < group || item.Tag.Element == 0x0000)
					continue;
				if (item.Tag.Group > group)
					return length;
				length += item.CalculateWriteLength(syntax, options);
			}
			return length;
		}

		public uint CalculateWriteLength(DicomTransferSyntax syntax, DicomWriteOptions options) {
			uint length = 0;
			ushort group = 0xffff;
			foreach (DcmItem item in _items.Values) {
				if (item.Tag.Element == 0x0000)
					continue;
				if (item.Tag.Group != group) {
					group = item.Tag.Group;
					if (Flags.IsSet(options, DicomWriteOptions.CalculateGroupLengths)) {
						if (syntax.IsExplicitVR)
							length += 4 + 2 + 2 + 4;
						else
							length += 4 + 4 + 4;
					}
				}
				length += item.CalculateWriteLength(syntax, options);
			}
			return length;
		}

		internal void SelectByteOrder(Endian endian) {
			foreach (DcmItem item in _items.Values) {
				item.Endian = endian;
			}
		}

		public void PreloadDeferredBuffers() {
			foreach (DcmItem item in _items.Values) {
				item.Preload();
			}
		}

		public void UnloadDeferredBuffers() {
			foreach (DcmItem item in _items.Values) {
				item.Unload();
			}
		}

		public void AddReferenceSequenceItem(DicomTag tag, DicomUID classUid, DicomUID instUid) {
			DcmItemSequence sq = GetSQ(tag);

			if (sq == null) {
				sq = new DcmItemSequence(tag);
				AddItem(sq);
			}

			DcmItemSequenceItem item = new DcmItemSequenceItem();
			item.Dataset.AddElementWithValue(DicomTags.ReferencedSOPClassUID, classUid);
			item.Dataset.AddElementWithValue(DicomTags.ReferencedSOPInstanceUID, instUid);
			sq.AddSequenceItem(item);
		}

		public void Merge(DcmDataset dataset) {
			foreach (DcmItem item in dataset.Elements)
				AddItem(item);
		}

		public DcmDataset Clone() {
			DcmDataset dataset = new DcmDataset(StreamPosition, StreamLength, InternalTransferSyntax);
			foreach (DcmItem item in Elements) {
				dataset.AddItem(item.Clone());
			}
			dataset.UserState = UserState;
			return dataset;
		}
		#endregion

		#region Element Access Methods
		public void Remove(DicomTag tag) {
			try { _items.Remove(tag); }
			catch { }
		}

		public void Remove(DicomTagMask mask) {
			for (int i = 0; i < _items.Values.Count; i++) {
				if (mask.IsMatch(_items.Values[i].Tag)) {
					try { _items.RemoveAt(i--); }
					catch { }
				}
			}
		}

		public bool AddElement(DicomTag tag) {
			return AddElement(tag, tag.Entry.DefaultVR);
		}

		public bool AddElement(DicomTag tag, DicomVR vr) {
			DcmElement elem = DcmElement.Create(tag, vr);

			if (vr.IsEncodedString)
				elem.ByteBuffer.Encoding = SpecificCharacterSetEncoding;

			AddItem(elem);
			return true;
		}

		public bool AddElementWithValue(DicomTag tag, string value) {
			DicomVR vr = tag.Entry.DefaultVR;
			if (!vr.IsString)
				throw new DicomDataException("Tried to create element with incorrect VR");
			if (AddElement(tag, vr)) {
				if (value != null && value != String.Empty)
					SetString(tag, value);
				return true;
			}
			return false;
		}

		public bool AddElementWithValue(DicomTag tag, ushort value) {
			DicomVR vr = tag.Entry.DefaultVR;
			if (vr != DicomVR.US)
				throw new DicomDataException("Tried to create element with incorrect VR");
			if (AddElement(tag, vr)) {
				GetUS(tag).SetValue(value);
				return true;
			}
			return false;
		}

		public bool AddElementWithValue(DicomTag tag, int value) {
			DicomVR vr = tag.Entry.DefaultVR;
			if (vr != DicomVR.IS && vr != DicomVR.SL)
				throw new DicomDataException("Tried to create element with incorrect VR");
			if (AddElement(tag, vr)) {
				if (vr == DicomVR.IS)
					GetIS(tag).SetInt32(value);
				else
					GetSL(tag).SetValue(value);
				return true;
			}
			return false;
		}
		
		public bool AddElementWithValue(DicomTag tag, double value) {
			DicomVR vr = tag.Entry.DefaultVR;
			if (vr != DicomVR.DS && vr != DicomVR.FD)
				throw new DicomDataException("Tried to create element with incorrect VR");
			if (AddElement(tag, vr)) {
				if (vr == DicomVR.DS)
					GetDS(tag).SetDouble(value);
				else
					GetFD(tag).SetValue(value);
				return true;
			}
			return false;
		}

		public bool AddElementWithValue(DicomTag tag, decimal value) {
			DicomVR vr = tag.Entry.DefaultVR;
			if (vr != DicomVR.DS)
				throw new DicomDataException("Tried to create element with incorrect VR");
			if (AddElement(tag, vr)) {
				GetDS(tag).SetDecimal(value);
				return true;
			}
			return false;
		}

		public bool AddElementWithValue(DicomTag tag, DateTime value) {
			DicomVR vr = tag.Entry.DefaultVR;
			if (vr != DicomVR.DA && vr != DicomVR.DT && vr != DicomVR.TM)
				throw new DicomDataException("Tried to create element with incorrect VR");
			if (AddElement(tag, vr)) {
				if (vr == DicomVR.DA)
					GetDA(tag).SetDateTime(value);
				else if (vr == DicomVR.DT)
					GetDT(tag).SetDateTime(value);
				else if (vr == DicomVR.TM)
					GetTM(tag).SetDateTime(value);
				return true;
			}
			return false;
		}

		public bool AddElementWithValue(DicomTag tag, DicomTag value) {
			DicomVR vr = tag.Entry.DefaultVR;
			if (vr != DicomVR.AT)
				throw new DicomDataException("Tried to create element with incorrect VR");
			if (AddElement(tag, vr)) {
				GetAT(tag).SetValue(value);
				return true;
			}
			return false;
		}

		public bool AddElementWithValue(DicomTag tag, DicomUID value) {
			return AddElementWithValue(tag, value.UID);
		}

		public bool AddElementWithObjectValue(DicomTag tag, object value) {
			DicomVR vr = tag.Entry.DefaultVR;
			if (AddElement(tag, vr)) {
				GetElement(tag).SetValueObject(value);
				return true;
			}
			return false;
		}

		public bool AddElementWithValueString(DicomTag tag, string value) {
			DicomVR vr = tag.Entry.DefaultVR;
			if (AddElement(tag, vr)) {
				GetElement(tag).SetValueString(value);
				return true;
			}
			return false;
		}

		public void AddItem(DcmItem item) {
			_items.Remove(item.Tag);
			_items.Add(item.Tag, item);
			item.Endian = InternalTransferSyntax.Endian;
		}

		public DcmItem GetItem(DicomTag tag) {
			DcmItem item = null;
			if (!_items.TryGetValue(tag, out item))
				return null;
			return item;
		}

		public bool Contains(DicomTag tag) {
			return _items.ContainsKey(tag);
		}

		public DicomVR GetVR(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem == null)
				return null;
			return elem.VR;
		}

		public DcmElement GetElement(DicomTag tag) {
			DcmItem item = null;
			if (!_items.TryGetValue(tag, out item))
				return null;
			if (item is DcmElement)
				return item as DcmElement;
			return null;
		}

		public IEnumerable<DicomTag> GetMaskedTags(DicomTagMask mask) {
			for (int i = 0; i < _items.Values.Count; i++) {
				if (mask.IsMatch(_items.Values[i].Tag))
					yield return _items.Values[i].Tag;
			}
		}

		public DcmApplicationEntity GetAE(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmApplicationEntity)
				return elem as DcmApplicationEntity;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmAgeString GetAS(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmAgeString)
				return elem as DcmAgeString;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmAttributeTag GetAT(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmAttributeTag)
				return elem as DcmAttributeTag;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmCodeString GetCS(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmCodeString)
				return elem as DcmCodeString;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmDate GetDA(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmDate)
				return elem as DcmDate;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmDecimalString GetDS(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmDecimalString)
				return elem as DcmDecimalString;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmDateTime GetDT(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmDateTime)
				return elem as DcmDateTime;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmFloatingPointDouble GetFD(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmFloatingPointDouble)
				return elem as DcmFloatingPointDouble;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmFloatingPointSingle GetFL(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmFloatingPointSingle)
				return elem as DcmFloatingPointSingle;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmIntegerString GetIS(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmIntegerString)
				return elem as DcmIntegerString;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmLongString GetLO(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmLongString)
				return elem as DcmLongString;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmLongText GetLT(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmLongText)
				return elem as DcmLongText;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmOtherByte GetOB(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmOtherByte)
				return elem as DcmOtherByte;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmOtherFloat GetOF(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmOtherFloat)
				return elem as DcmOtherFloat;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmOtherWord GetOW(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmOtherWord)
				return elem as DcmOtherWord;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmPersonName GetPN(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmPersonName)
				return elem as DcmPersonName;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmShortString GetSH(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmShortString)
				return elem as DcmShortString;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmSignedLong GetSL(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmSignedLong)
				return elem as DcmSignedLong;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmItemSequence GetSQ(DicomTag tag) {
			DcmItem item = GetItem(tag);
			if (item is DcmItemSequence)
				return item as DcmItemSequence;
			if (item != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmSignedShort GetSS(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmSignedShort)
				return elem as DcmSignedShort;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmShortText GetST(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmShortText)
				return elem as DcmShortText;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmTime GetTM(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmTime)
				return elem as DcmTime;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmUniqueIdentifier GetUI(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmUniqueIdentifier)
				return elem as DcmUniqueIdentifier;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmUnsignedLong GetUL(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmUnsignedLong)
				return elem as DcmUnsignedLong;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmUnknown GetUN(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmUnknown)
				return elem as DcmUnknown;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmUnsignedShort GetUS(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmUnsignedShort)
				return elem as DcmUnsignedShort;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		public DcmUnlimitedText GetUT(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmUnlimitedText)
				return elem as DcmUnlimitedText;
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return null;
		}

		/// <summary>
		/// Performs a recursive search for the specified tag. This function returns value elements only.
		/// </summary>
		/// <param name="tag">DICOM Tag</param>
		/// <returns>First occurence of tag or null.</returns>
		public IEnumerable<DcmElement> Search(DicomTag tag) {
			foreach (DcmItem item in _items.Values) {
				if (item.Tag == tag && item is DcmElement)
					yield return item as DcmElement;
				else if (item is DcmItemSequence) {
					DcmItemSequence sq = item as DcmItemSequence;
					foreach (DcmItemSequenceItem sqi in sq.SequenceItems) {
						foreach (DcmElement elem in sqi.Dataset.Search(tag))
							yield return elem;
					}
				}
			}
		}

		/// <summary>
		/// Recursively enumerates all value elements.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<DcmElement> Recurse() {
			foreach (DcmItem item in _items.Values) {
				if (item is DcmElement)
					yield return item as DcmElement;
				else if (item is DcmItemSequence) {
					DcmItemSequence sq = item as DcmItemSequence;
					foreach (DcmItemSequenceItem sqi in sq.SequenceItems) {
						foreach (DcmElement elem in sqi.Dataset.Recurse())
							yield return elem;
					}
				}
			}
		}

		/// <summary>
		/// Recursively replaces tags with the specified value.
		/// </summary>
		/// <param name="tag">DICOM Tag</param>
		/// <param name="value">Value</param>
		public void ReplaceAll(DicomTag tag, object value) {
			foreach (var elem in Search(tag))
				elem.SetValueObject(value);
		}

		/// <summary>
		/// Replaces UID for specified tag and all instances of UID in UI elements.
		/// </summary>
		/// <param name="tag"></param>
		/// <param name="uid"></param>
		public void ReplaceUID(DicomTag tag, DicomUID uid) {
			DicomUID old = GetUID(tag);
			AddElementWithValue(tag, uid);

			if (old != null) {
				foreach (var elem in Recurse()) {
					if (elem.VR == DicomVR.UI) {
						if (old.Equals((elem as DcmUniqueIdentifier).GetUID()))
							elem.SetValueObject(uid);
					}
				}
			}
		}
		#endregion

		#region Data Access Methods
		public string GetValueString(DicomTag tag) {
			DcmElement elem = GetElement(tag);
			if (elem == null)
				return null;
			return elem.GetValueString();
		}

		public string GetString(DicomTag tag, string deflt) {
			return GetString(tag, 0, deflt);
		}

		public string GetString(DicomTag tag, int index, string deflt) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmStringElement)
				return (elem as DcmStringElement).GetValue(index);
			if (elem is DcmMultiStringElement)
				return (elem as DcmMultiStringElement).GetValue(index);
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return deflt;
		}

		public void SetString(DicomTag tag, string value) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmStringElement) {
				(elem as DcmStringElement).SetValue(value);
				return;
			}
			if (elem is DcmMultiStringElement) {
				(elem as DcmMultiStringElement).SetValue(value);
				return;
			}
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			throw new DicomDataException("Element does not exist in Dataset");
		}

		public string[] GetStringArray(DicomTag tag, string[] deflt) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmMultiStringElement)
				return (elem as DcmMultiStringElement).GetValues();
			if (elem is DcmStringElement)
				return new string[] { (elem as DcmStringElement).GetValue() };
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return deflt;
		}

		public void SetStringArray(DicomTag tag, string[] values) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmMultiStringElement) {
				(elem as DcmMultiStringElement).SetValues(values);
				return;
			}
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			throw new DicomDataException("Element does not exist in Dataset");
		}

		public DateTime GetDateTime(DicomTag tag, DateTime deflt) {
			return GetDateTime(tag, 0, deflt);
		}

		public DateTime GetDateTime(DicomTag tag, int index, DateTime deflt) {
			DcmElement elem = GetElement(tag);
			if (elem is DcmDate || elem is DcmTime || elem is DcmDateTime) {
				try {
					if (elem is DcmDate)
						return (elem as DcmDate).GetDateTime(index);
					if (elem is DcmTime)
						return (elem as DcmTime).GetDateTime(index);
					if (elem is DcmDateTime)
						return (elem as DcmDateTime).GetDateTime(index);
				}
				catch {
					return deflt;
				}
			}
			if (elem != null)
				throw new DicomDataException("Tried to access element with incorrect VR");
			return deflt;	
		}

		public DateTime GetDateTime(DicomTag dtag, DicomTag ttag, DateTime deflt) {
			DateTime dt;
			DcmDate da = GetDA(dtag);
			if (da != null)
				dt = da.GetDateTime(0);
			else
				dt = deflt;
			if (ttag != null) {
				try {
					DcmTime tm = GetTM(ttag);
					if (tm != null) {
						DateTime time = tm.GetDateTime(0);
						dt = new DateTime(dt.Year, dt.Month, dt.Day, time.Hour, time.Minute, time.Second);
					}
				}
				catch { }
			}
			return dt;
		}

		public void SetDateTime(DicomTag dtag, DicomTag ttag, DateTime value) {
			DcmDate da = new DcmDate(dtag);
			da.SetDateTime(value);
			AddItem(da);

			DcmTime tm = new DcmTime(ttag);
			tm.SetDateTime(value);
			AddItem(tm);
		}

		public DicomTag GetDcmTag(DicomTag tag) {
			DcmAttributeTag at = GetAT(tag);
			if (at != null && at.Length > 0)
				return at.GetValue();
			return null;
		}

		public DicomUID GetUID(DicomTag tag) {
			DcmUniqueIdentifier ui = GetUI(tag);
			if (ui != null && ui.Length > 0)
				return ui.GetUID();
			return null;
		}

		public int GetInt32(DicomTag tag, int deflt) {
			DcmElement elem = GetElement(tag);
			if (elem != null && elem.Length > 0) {
				if (elem.VR == DicomVR.IS)
					return (elem as DcmIntegerString).GetInt32();
				else if (elem.VR == DicomVR.SL)
					return (elem as DcmSignedLong).GetValue();
				else
					throw new DicomDataException("Tried to access element with incorrect VR");
			}
			return deflt;
		}

		public short GetInt16(DicomTag tag, short deflt) {
			DcmSignedShort ss = GetSS(tag);
			if (ss != null && ss.Length > 0)
				return ss.GetValue();
			return deflt;
		}

		public ushort GetUInt16(DicomTag tag, ushort deflt) {
			DcmUnsignedShort us = GetUS(tag);
			if (us != null && us.Length > 0)
				return us.GetValue();
			return deflt;
		}

		public float GetFloat(DicomTag tag, float deflt) {
			DcmElement elem = GetElement(tag);
			if (elem != null && elem.Length > 0) {
				if (elem.VR == DicomVR.FL)
					return (elem as DcmFloatingPointSingle).GetValue();
				else if (elem.VR == DicomVR.DS)
					return (elem as DcmDecimalString).GetFloat();
				else
					throw new DicomDataException("Tried to access element with incorrect VR");
			}
			return deflt;
		}

		public double GetDouble(DicomTag tag, double deflt) {
			DcmElement elem = GetElement(tag);
			if (elem != null && elem.Length > 0) {
				if (elem.VR == DicomVR.FD)
					return (elem as DcmFloatingPointDouble).GetValue();
				else if (elem.VR == DicomVR.DS)
					return (elem as DcmDecimalString).GetDouble();
				else
					throw new DicomDataException("Tried to access element with incorrect VR");
			}
			return deflt;
		}

		public decimal GetDecimal(DicomTag tag, decimal deflt) {
			DcmDecimalString ds = GetDS(tag);
			if (ds != null && ds.Length > 0)
				return ds.GetDecimal();
			return deflt;
		}
		#endregion

		#region Special Use
		/// <summary>
		/// Original Attributes Sequence (0400,0561)
		/// Sequence of Items containing all attributes that were
		/// removed or replaced by other values in the main dataset.
		/// One or more Items may be permitted in this sequence.
		/// </summary>
		/// <param name="originalAttributesSource">
		/// Source of Previous Values (0400,0564)
		/// The source that provided the SOP Instance prior to the
		/// removal or replacement of the  values. For example, this
		/// might be the Institution from which imported SOP Instances
		/// were received.
		/// </param>
		/// <param name="modifyingSystem">
		/// Modifying System (0400,0563)
		/// Identification of the system which removed and/or replaced
		/// the attributes.
		/// </param>
		/// <param name="reasonForModification">
		/// Reason for the Attribute Modification (0400,0565)
		/// Reason for the attribute modification. Defined terms are:
		/// COERCE = Replace values of attributes such as Patient
		///     Name, ID, Accession Number, for example, during import
		///     of media from an external institution, or reconciliation
		///     against a master patient index.
		/// CORRECT = Replace incorrect values, such as Patient
		///     Name or ID, for example, when incorrect worklist item
		///     was chosen or operator input error.
		/// </param>
		/// <param name="tagsToModify">
		/// Tags from this dataset to be removed or modified.
		/// </param>
		public void CreateOriginalAttributesSequence(string originalAttributesSource, string modifyingSystem, 
			string reasonForModification, IEnumerable<DicomTag> tagsToModify) {
			DcmItemSequenceItem item = new DcmItemSequenceItem();
			item.Dataset.AddElementWithValue(DicomTags.SourceOfPreviousValues, originalAttributesSource);
			item.Dataset.AddElementWithValue(DicomTags.AttributeModificationDateTime, DateTime.Now);
			item.Dataset.AddElementWithValue(DicomTags.ModifyingSystem, modifyingSystem);
			item.Dataset.AddElementWithValue(DicomTags.ReasonForTheAttributeModification, reasonForModification);

			DcmItemSequence sq = new DcmItemSequence(DicomTags.ModifiedAttributesSequence);
			item.Dataset.AddItem(sq);

			DcmItemSequenceItem modified = new DcmItemSequenceItem();
			sq.AddSequenceItem(modified);

			foreach (DicomTag tag in tagsToModify) {
				DcmItem modifiedItem = GetItem(tag);
				if (modifiedItem == null)
					modified.Dataset.AddItem(modifiedItem.Clone());
			}

			DcmItemSequence oasq = GetSQ(DicomTags.OriginalAttributesSequence);
			if (oasq == null) {
				oasq = new DcmItemSequence(DicomTags.OriginalAttributesSequence);
				AddItem(oasq);
			}
			oasq.AddSequenceItem(item);
		}
		#endregion

		#region Encoding
		public void ChangeTransferSyntax(DicomTransferSyntax newTransferSyntax, DcmCodecParameters parameters) {
			DicomTransferSyntax oldTransferSyntax = InternalTransferSyntax;

			if (oldTransferSyntax == newTransferSyntax)
				return;

			if (oldTransferSyntax.IsEncapsulated && newTransferSyntax.IsEncapsulated) {
				ChangeTransferSyntax(DicomTransferSyntax.ExplicitVRLittleEndian, parameters);
				oldTransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;
			}

			if (Contains(DicomTags.PixelData)) {
				DcmPixelData oldPixelData = new DcmPixelData(this);
				DcmPixelData newPixelData = new DcmPixelData(newTransferSyntax, oldPixelData);

				if (oldTransferSyntax.IsEncapsulated) {
					IDcmCodec codec = DicomCodec.GetCodec(oldTransferSyntax);
					codec.Decode(this, oldPixelData, newPixelData, parameters);
				}
				else if (newTransferSyntax.IsEncapsulated) {
					IDcmCodec codec = DicomCodec.GetCodec(newTransferSyntax);
					codec.Encode(this, oldPixelData, newPixelData, parameters);
				}
				else {
					for (int i = 0; i < oldPixelData.NumberOfFrames; i++) {
						byte[] data = oldPixelData.GetFrameDataU8(i);
						newPixelData.AddFrame(data);
					}
				}

				newPixelData.UpdateDataset(this);
			}

			SetInternalTransferSyntax(newTransferSyntax);
		}

		public string ComputePixelDataMD5() {
			if (!Contains(DicomTags.PixelData))
				throw new DicomDataException("Dataset does not contain pixel data");

			DcmPixelData pixelData = new DcmPixelData(this);
			return pixelData.ComputeMD5();
		}

		internal void SetInternalTransferSyntax(DicomTransferSyntax ts) {
			_transferSyntax = ts;
			foreach (DcmItem item in _items.Values) {
				if (item is DcmElement) {
					item.Endian = ts.Endian;
				}
				else if (item is DcmFragmentSequence) {
					item.Endian = ts.Endian;
				}
				else if (item is DcmItemSequence) {
					DcmItemSequence sq = item as DcmItemSequence;
					sq.Endian = ts.Endian;
					foreach (DcmItemSequenceItem si in sq.SequenceItems) {
						si.Dataset.SetInternalTransferSyntax(ts);
					}
				}
			}
		}
		#endregion

		#region Binding
		private object GetDefaultValue(Type vtype, DicomFieldDefault deflt) {
			try {
				if (vtype == typeof(DicomUID) || vtype == typeof(DicomTransferSyntax) || vtype.IsSubclassOf(typeof(DcmElement)))
					return null;
				if (deflt == DicomFieldDefault.Null || deflt == DicomFieldDefault.None)
					return null;
				if (deflt == DicomFieldDefault.DBNull)
					return DBNull.Value;
				if (deflt == DicomFieldDefault.Default && vtype != typeof(string))
					return Activator.CreateInstance(vtype);
				if (vtype == typeof(string)) {
					if (deflt == DicomFieldDefault.StringEmpty || deflt == DicomFieldDefault.Default)
						return String.Empty;
				} else if (vtype == typeof(DateTime)) {
					if (deflt == DicomFieldDefault.DateTimeNow)
						return DateTime.Now;
					if (deflt == DicomFieldDefault.MinValue)
						return DateTime.MinValue;
					if (deflt == DicomFieldDefault.MaxValue)
						return DateTime.MaxValue;
				} else if (vtype.IsSubclassOf(typeof(ValueType))) {
					if (deflt == DicomFieldDefault.MinValue) {
						PropertyInfo pi = vtype.GetProperty("MinValue", BindingFlags.Static);
						if (pi != null) return pi.GetValue(null, null);
					}
					if (deflt == DicomFieldDefault.MaxValue) {
						PropertyInfo pi = vtype.GetProperty("MaxValue", BindingFlags.Static);
						if (pi != null) return pi.GetValue(null, null);
					}
					return Activator.CreateInstance(vtype);
				}
				return null;
			}
			catch (Exception) {
				Debug.Log.Error("Error in default value type! - {0}", vtype.ToString());
				return null;
			}
		}

		private object LoadDicomFieldValue(DcmElement elem, Type vtype, DicomFieldDefault deflt, bool udzl) {
			if (vtype.IsSubclassOf(typeof(DcmElement))) {
				if (elem != null && vtype != elem.GetType())
					throw new DicomDataException("Invalid binding type for Element VR!");
				return elem;
			} else if (vtype.IsArray) {
				if (elem != null) {
					if (vtype.GetElementType() != elem.GetValueType())
						throw new DicomDataException("Invalid binding type for Element VR!");
					if (elem.GetValueType() == typeof(DateTime))
						return (elem as DcmDateElementBase).GetDateTimes();
					else
						return elem.GetValueObjectArray();
				} else {
					if (deflt == DicomFieldDefault.EmptyArray)
						return Array.CreateInstance(vtype, 0);
					else
						return null;
				}
			} else {
				if (elem != null) {
					if (elem.Length == 0 && udzl) {
						return GetDefaultValue(vtype, deflt);
					}
					if (vtype != elem.GetValueType()) {
						if (vtype == typeof(string)) {
							return elem.GetValueString();
						} else if (vtype == typeof(DicomUID) && elem.VR == DicomVR.UI) {
							return (elem as DcmUniqueIdentifier).GetUID();
						} else if (vtype == typeof(DicomTransferSyntax) && elem.VR == DicomVR.UI) {
							return (elem as DcmUniqueIdentifier).GetTS();
						} else if (vtype == typeof(DcmDateRange) && elem.GetType().IsSubclassOf(typeof(DcmDateElementBase))) {
							return (elem as DcmDateElementBase).GetDateTimeRange();
						} else if (vtype == typeof(Int32)) {
							return Convert.ToInt32(elem.GetValueString(), 10);
						} else if (vtype == typeof(object)) {
							return elem.GetValueObject();
						} else
							throw new DicomDataException("Invalid binding type for Element VR!");
					} else {
						return elem.GetValueObject();
					}
				} else {
					return GetDefaultValue(vtype, deflt);
				}
			}
		}

		public void LoadDicomFields(object obj) {
			FieldInfo[] fields = obj.GetType().GetFields();
			foreach (FieldInfo field in fields) {
				if (field.IsDefined(typeof(DicomFieldAttribute), true)) {
					try {
						DicomFieldAttribute dfa = (DicomFieldAttribute)field.GetCustomAttributes(typeof(DicomFieldAttribute), true)[0];
						DcmElement elem = GetElement(dfa.Tag);
						if ((elem == null || (elem.Length == 0 && dfa.UseDefaultForZeroLength)) && dfa.DefaultValue == DicomFieldDefault.None) {
							// do nothing
						}
						else {
							field.SetValue(obj, LoadDicomFieldValue(elem, field.FieldType, dfa.DefaultValue, dfa.UseDefaultForZeroLength));
						}
					}
					catch (Exception e) {
						Debug.Log.Debug("Unable to bind field: " + e.Message);
					}
				}
			}

			PropertyInfo[] properties = obj.GetType().GetProperties();
			foreach (PropertyInfo property in properties) {
				if (property.IsDefined(typeof(DicomFieldAttribute), true)) {
					try {
						DicomFieldAttribute dfa = (DicomFieldAttribute)property.GetCustomAttributes(typeof(DicomFieldAttribute), true)[0];
						DcmElement elem = GetElement(dfa.Tag);
						if ((elem == null || (elem.Length == 0 && dfa.UseDefaultForZeroLength)) && dfa.DefaultValue == DicomFieldDefault.None) {
							// do nothing
						} else {
							property.SetValue(obj, LoadDicomFieldValue(elem, property.PropertyType, dfa.DefaultValue, dfa.UseDefaultForZeroLength), null);
						}
					}
					catch (Exception e) {
						Debug.Log.Debug("Unable to bind field: " + e.Message);
					}
				}
			}
		}

		private void SaveDicomFieldValue(DicomTag tag, object value, bool createEmpty) {
			if (value != null && value != DBNull.Value) {
				Type vtype = value.GetType();
				if (vtype.IsSubclassOf(typeof(DcmElement))) {
					AddItem((DcmItem)value);
				} else {
					if (!Contains(tag)) {
						AddElement(tag, tag.Entry.DefaultVR);
					}
					DcmElement elem = GetElement(tag);
					if (vtype.IsArray) {
						if (vtype.GetElementType() != elem.GetValueType())
							throw new DicomDataException("Invalid binding type for Element VR!");
						if (elem.GetValueType() == typeof(DateTime))
							(elem as DcmDateElementBase).SetDateTimes((DateTime[])value);
						else
							elem.SetValueObjectArray((object[])value);
					} else {
						if (elem.VR == DicomVR.UI && vtype == typeof(DicomUID)) {
							DicomUID ui = (DicomUID)value;
							elem.SetValueString(ui.UID);
						} else if (elem.VR == DicomVR.UI && vtype == typeof(DicomTransferSyntax)) {
							DicomTransferSyntax ts = (DicomTransferSyntax)value;
							elem.SetValueString(ts.UID.UID);
						}
						else if (vtype == typeof(DcmDateRange) && elem.GetType().IsSubclassOf(typeof(DcmDateElementBase))) {
							DcmDateRange dr = (DcmDateRange)value;
							(elem as DcmDateElementBase).SetDateTimeRange(dr);
						} else if (vtype != elem.GetValueType()) {
							if (vtype == typeof(string)) {
								elem.SetValueString((string)value);
							} else
								throw new DicomDataException("Invalid binding type for Element VR!");
						} else {
							elem.SetValueObject(value);
						}
					}
				}
			} else {
				if (Contains(tag)) {
					GetElement(tag).ByteBuffer.Clear();
				} else if (createEmpty) {
					AddElement(tag, tag.Entry.DefaultVR);
				}
			}
		}

		public void SaveDicomFields(object obj) {
			FieldInfo[] fields = obj.GetType().GetFields();
			foreach (FieldInfo field in fields) {
				if (field.IsDefined(typeof(DicomFieldAttribute), true)) {
					DicomFieldAttribute dfa = (DicomFieldAttribute)field.GetCustomAttributes(typeof(DicomFieldAttribute), true)[0];
					object value = field.GetValue(obj);
					SaveDicomFieldValue(dfa.Tag, value, dfa.CreateEmptyElement);
				}
			}

			PropertyInfo[] properties = obj.GetType().GetProperties();
			foreach (PropertyInfo property in properties) {
				if (property.IsDefined(typeof(DicomFieldAttribute), true)) {
					DicomFieldAttribute dfa = (DicomFieldAttribute)property.GetCustomAttributes(typeof(DicomFieldAttribute), true)[0];
					object value = property.GetValue(obj, null);
					SaveDicomFieldValue(dfa.Tag, value, dfa.CreateEmptyElement);
				}
			}
		}
		#endregion

		#region Dump
		public string Dump() {
			StringBuilder sb = new StringBuilder();
			Dump(sb, "", DicomDumpOptions.Default);
			return sb.ToString();
		}

		public void Dump(StringBuilder sb, String prefix, DicomDumpOptions options) {
			foreach (DcmItem item in _items.Values) {
				item.Dump(sb, prefix, options);
				sb.AppendLine();
			}
		}
		#endregion
	}
}
