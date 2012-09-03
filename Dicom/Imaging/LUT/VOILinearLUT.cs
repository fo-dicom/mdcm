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
	public class VOILinearLUT : ILUT {
		#region Private Members
		private WindowLevel _windowLevel;

		private double _windowCenterMin05;
		private double _windowWidthMin1;
		private double _windowWidthDiv2;
		private int _windowStart;
		private int _windowEnd;

		private bool _valid;
		#endregion

		#region Public Constructors
		public VOILinearLUT(WindowLevel wl) {
			WindowLevel = wl;
		}
		#endregion

		#region Public Properties
		public WindowLevel WindowLevel {
			get { return _windowLevel; }
			set {
				_windowLevel = value;
				_valid = false;
			}
		}

		public bool IsValid {
			get { return _valid; }
		}

		public int MinimumOutputValue {
			get { return 0; }
		}

		public int MaximumOutputValue {
			get { return 255; }
		}

		public int this[int value] {
			get {
				if (value <= _windowStart)
					return 0;
				if (value > _windowEnd)
					return 255;
				unchecked {
					double scale = ((value - _windowCenterMin05) / _windowWidthMin1) + 0.5;
					return (int)(scale * 255);
				}
			}
		}
		#endregion

		#region Public Methods
		public void Recalculate() {
			if (!_valid) {
				_windowCenterMin05 = _windowLevel.Level - 0.5;
				_windowWidthMin1 = _windowLevel.Window - 1;
				_windowWidthDiv2 = _windowWidthMin1 / 2;
				_windowStart = (int)(_windowCenterMin05 - _windowWidthDiv2);
				_windowEnd = (int)(_windowCenterMin05 + _windowWidthDiv2);
				_valid = true;
			}
		}
		#endregion
	}
}
