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
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Data {
	/// <summary>
	/// DICOM Overlay
	/// </summary>
	public class DcmOverlayData {
		#region Private Members
		private ushort _group;

		private int _rows;
		private int _columns;
		private string _type;
		private int _originX;
		private int _originY;
		private int _bitsAllocated;
		private int _bitPosition;
		private byte[] _data;

		private string _description;
		private string _subtype;
		private string _label;

		private int _frames;
		private int _frameOrigin;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes overlay from DICOM dataset and overlay group.
		/// </summary>
		/// <param name="ds">Dataset</param>
		/// <param name="group">Overlay group</param>
		public DcmOverlayData(DcmDataset ds, ushort group) {
			_group = group;
			Load(ds);
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Overlay group
		/// </summary>
		public ushort Group {
			get { return _group; }
		}

		/// <summary>
		/// Number of rows in overlay
		/// </summary>
		public int Rows {
			get { return _rows; }
		}

		/// <summary>
		/// Number of columns in overlay
		/// </summary>
		public int Columns {
			get { return _columns; }
		}

		/// <summary>
		/// Overlay type
		/// </summary>
		public string Type {
			get { return _type; }
		}

		/// <summary>
		/// Position of the first column of an overlay
		/// </summary>
		public int OriginX {
			get { return _originX; }
		}

		/// <summary>
		/// Position of the first row of an overlay
		/// </summary>
		public int OriginY {
			get { return _originY; }
		}

		/// <summary>
		/// Number of bits allocated in overlay data
		/// </summary>
		public int BitsAllocated {
			get { return _bitsAllocated; }
		}

		/// <summary>
		/// Bit position of embedded overlay
		/// </summary>
		public int BitPosition {
			get { return _bitPosition; }
		}

		/// <summary>
		/// Overlay data
		/// </summary>
		public byte[] Data {
			get { return _data; }
		}

		/// <summary>
		/// Description of overlay
		/// </summary>
		public string Description {
			get { return _description; }
		}

		/// <summary>
		/// Subtype
		/// </summary>
		public string Subtype {
			get { return _subtype; }
		}

		/// <summary>
		/// Overlay label
		/// </summary>
		public string Label {
			get { return _label; }
		}

		/// <summary>
		/// Number of frames
		/// </summary>
		public int NumberOfFrames {
			get { return _frames; }
		}

		/// <summary>
		/// First frame of overlay
		/// </summary>
		public int OriginFrame {
			get { return _frameOrigin; }
		}
		#endregion

		#region Public Members
		/// <summary>
		/// Gets the overlay data.
		/// </summary>
		/// <param name="bg">Background color</param>
		/// <param name="fg">Foreground color</param>
		/// <returns>Overlay data</returns>
		public int[] GetOverlayDataS32(int bg, int fg) {
			int[] overlay = new int[Rows * Columns];
			BitArray bits = new BitArray(_data);
			if (bits.Length < overlay.Length)
				throw new DicomDataException("Invalid overlay length: " + bits.Length);
			for (int i = 0, c = overlay.Length; i < c; i++) {
				if (bits.Get(i))
					overlay[i] = fg;
				else
					overlay[i] = bg;
			}
			return overlay;
		}

		/// <summary>
		/// Gets all overlays in a DICOM dataset.
		/// </summary>
		/// <param name="ds">Dataset</param>
		/// <returns>Array of overlays</returns>
		public static DcmOverlayData[] FromDataset(DcmDataset ds) {
			List<ushort> groups = new List<ushort>();
			foreach (DcmItem elem in ds.Elements) {
				if (elem.Tag.Element == 0x0010) {
					if (elem.Tag.Group >= 0x6000 && elem.Tag.Group <= 0x60FF) {
						groups.Add(elem.Tag.Group);
					}
				}
			}
			List<DcmOverlayData> overlays = new List<DcmOverlayData>();
			foreach (ushort group in groups) {
				DcmOverlayData overlay = new DcmOverlayData(ds, group);
				overlays.Add(overlay);
			}
			return overlays.ToArray();
		}
		#endregion

		#region Private Methods
		private DicomTag OverlayTag(DicomTag tag) {
			return new DicomTag(_group, tag.Element);
		}

		private void Load(DcmDataset ds) {
			_rows = ds.GetUInt16(OverlayTag(DicomTags.OverlayRows), 0);
			_columns = ds.GetUInt16(OverlayTag(DicomTags.OverlayColumns), 0);
			_type = ds.GetString(OverlayTag(DicomTags.OverlayType), "Unknown");

			DicomTag tag = OverlayTag(DicomTags.OverlayOrigin);
			if (ds.Contains(tag)) {
				short[] xy = ds.GetSS(tag).GetValues();
				if (xy != null && xy.Length == 2) {
					_originX = xy[0];
					_originY = xy[1];
				}
			}

			_bitsAllocated = ds.GetUInt16(OverlayTag(DicomTags.OverlayBitsAllocated), 1);
			_bitPosition = ds.GetUInt16(OverlayTag(DicomTags.OverlayBitPosition), 0);

			tag = OverlayTag(DicomTags.OverlayData);
			if (ds.Contains(tag)) {
				DcmElement elem = ds.GetElement(tag);
				_data = elem.ByteBuffer.ToBytes();
			}

			_description = ds.GetString(OverlayTag(DicomTags.OverlayDescription), String.Empty);
			_subtype = ds.GetString(OverlayTag(DicomTags.OverlaySubtype), String.Empty);
			_label = ds.GetString(OverlayTag(DicomTags.OverlayLabel), String.Empty);

			_frames = ds.GetInt32(OverlayTag(DicomTags.NumberOfFramesInOverlay), 1);
			_frameOrigin = ds.GetUInt16(OverlayTag(DicomTags.ImageFrameOrigin), 1);

			//TODO: include ROI
		}
		#endregion
	}
}
