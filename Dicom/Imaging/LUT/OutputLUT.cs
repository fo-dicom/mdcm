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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Dicom.Imaging.LUT {
	public class OutputLUT : ILUT {
		#region Private Members
		private int[] _table;
		private Color[] _lut;
		#endregion

		#region Public Constructors
		public OutputLUT(Color[] lut) {
			ColorMap = lut;
		}
		#endregion

		#region Public Properties
		public Color[] ColorMap {
			get { return _lut; }
			set {
				if (_lut == null || _lut.Length != 256)
					throw new DicomImagingException("Expected 256 entry color map");
				_lut = value;
				_table = null;
			}
		}

		public int MinimumOutputValue {
			get { return int.MinValue; }
		}

		public int MaximumOutputValue {
			get { return int.MaxValue; }
		}

		public bool IsValid {
			get { return _table != null; }
		}

		public int this[int value] {
			get {
				//if (value < 0)
				//    return _table[0];
				//if (value > 255)
				//    return _table[255];
				return _table[value];
			}
		}
		#endregion

		#region Public Methods
		public void Recalculate() {
			if (_table == null) {
				_table = new int[256];
				for (int i = 0; i < 256; i++) {
					_table[i] = _lut[i].ToArgb();
				}
			}
		}
		#endregion
	}
}
