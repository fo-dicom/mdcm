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

namespace Dicom.Imaging.LUT {
	public class PrecalculatedLUT : ILUT {
		#region Private Members
		private ILUT _lut;

		private int _minValue;
		private int _maxValue;

		private int[] _table;
		private int _offset;
		#endregion

		#region Public Constructor
		public PrecalculatedLUT(ILUT lut) {
			_minValue = lut.MinimumOutputValue;
			_maxValue = lut.MaximumOutputValue;
			_offset = -_minValue;
			_table = new int[_maxValue - _minValue + 1];
			_lut = lut;

			for (int i = _minValue; i <= _maxValue; i++) {
				_table[i + _offset] = _lut[i];
			}
		}
		#endregion

		#region Public Properties
		public bool IsValid {
			get { return _lut.IsValid; }
		}

		public int MinimumOutputValue {
			get { return _lut.MinimumOutputValue; }
		}

		public int MaximumOutputValue {
			get { return _lut.MaximumOutputValue; }
		}

		public int this[int value] {
			get { return _table[value + _offset]; }
		}
		#endregion

		#region Public Methods
		public void Recalculate() {
			if (IsValid)
				return;

			for (int i = _minValue; i <= _maxValue; i++) {
				_table[i + _offset] = _lut[i];
			}
		}
		#endregion
	}
}
