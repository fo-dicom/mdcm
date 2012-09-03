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

namespace Dicom.Imaging {
	public class SpatialTransform {
		#region Private Members
		private double _scale;
		private int _rotate;
		private bool _flipx;
		private bool _flipy;
		private Point _pan;
		#endregion

		#region Public Constructors
		public SpatialTransform() {
			_pan = new Point(0, 0);
			Reset();
		}
		#endregion

		#region Public Properties
		public double Scale {
			get { return _scale; }
			set { _scale = value; }
		}

		public int Rotation {
			get { return _rotate; }
			set { _rotate = value; }
		}

		public bool FlipX {
			get { return _flipx; }
			set { _flipx = value; }
		}

		public bool FlipY {
			get { return _flipy; }
			set { _flipy = value; }
		}

		public Point Pan {
			get { return _pan; }
			set { _pan = value; }
		}

		public bool IsTransformed {
			get {
				return _scale != 1.0f ||
					_rotate != 0.0f ||
					_pan == Point.Empty;
			}
		}
		#endregion

		#region Public Members
		public void Rotate(int angle) {
			_rotate += angle;
		}

		public void Reset() {
			_scale = 1.0;
			_rotate = 0;
			_flipx = false;
			_flipy = false;
			_pan.X = 0;
			_pan.Y = 0;
		}
		#endregion
	}
}
