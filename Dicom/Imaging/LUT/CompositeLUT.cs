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

namespace Dicom.Imaging.LUT {
	public class CompositeLUT : ILUT {
		#region Private Members
		private List<ILUT> _luts = new List<ILUT>();
		#endregion

		#region Public Properties
		public ILUT FinalLUT {
			get {
				if (_luts.Count > 0)
					return _luts[_luts.Count - 1];
				return null;
			}
		}
		#endregion

		#region Public Constructor
		public CompositeLUT() {
		}
		#endregion

		#region Public Members
		public void Add(ILUT lut) {
			_luts.Add(lut);
		}
		#endregion

		#region ILUT Members
		public int MinimumOutputValue {
			get {
				ILUT lut = FinalLUT;
				if (lut != null)
					return lut.MinimumOutputValue;
				return 0;
			}
		}

		public int MaximumOutputValue {
			get {
				ILUT lut = FinalLUT;
				if (lut != null)
					return lut.MaximumOutputValue;
				return 255;
			}
		}

		public bool IsValid {
			get {
				foreach (ILUT lut in _luts) {
					if (!lut.IsValid)
						return false;
				}
				return true;
			}
		}

		public int this[int value] {
			get {
				foreach (ILUT lut in _luts)
					value = lut[value];
				return value;
			}
		}

		public void Recalculate() {
			foreach (ILUT lut in _luts)
				lut.Recalculate();
		}
		#endregion
	}
}
