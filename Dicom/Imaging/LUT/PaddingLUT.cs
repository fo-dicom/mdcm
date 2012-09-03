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
	public class PaddingLUT : ILUT {
		#region Private Members
		private int _paddingValue;
		private int _minValue;
		private int _maxValue;
		#endregion
		
		#region Public Constructors
		public PaddingLUT(int minValue, int maxValue, int paddingValue) {
			_paddingValue = paddingValue;
			_minValue = minValue;
			_maxValue = maxValue;
		}
		#endregion

		#region Public Properties
		public double PixelPaddingValue {
			get { return _paddingValue; }
		}

		public int MinimumOutputValue {
			get { return _minValue; }
		}

		public int MaximumOutputValue {
			get { return _maxValue; }
		}

		public bool IsValid {
			get { return true; }
		}

		public int this[int value] {
			get {
				if (value == _paddingValue)
					return _minValue;
				return value;
			}
		}
		#endregion

		#region Public Methods
		public void Recalculate() {
		}
		#endregion
	}
}
