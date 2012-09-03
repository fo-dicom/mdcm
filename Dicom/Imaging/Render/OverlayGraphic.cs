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

using Dicom.Imaging.Algorithms;

using Dicom.Utility;

namespace Dicom.Imaging.Render {
	public class OverlayGraphic {
		#region Private Members
		private SingleBitPixelData _originalData;
		private GrayscalePixelDataU8 _scaledData;
		private int _offsetX;
		private int _offsetY;
		private int _color;
		private double _scale;
		#endregion

		#region Public Constructors
		public OverlayGraphic(SingleBitPixelData pixelData, int offsetx, int offsety, int color) {
			_originalData = pixelData;
			_scaledData = _originalData;
			_offsetX = offsetx;
			_offsetY = offsety;
			_color = color;
			_scale = 1.0;
		}
		#endregion

		#region Public Methods
		public void Scale(double scale) {
			if (Math.Abs(scale - _scale) <= Double.Epsilon)
				return;

			_scale = scale;
			_scaledData = null;
		}

		public void Render(int[] pixels, int width, int height) {
			byte[] data = null;

			if (_scaledData == null) {
				if (_scale == 1.0)
					_scaledData = _originalData;
				else {
					int w = (int)(_originalData.Width * _scale);
					int h = (int)(_originalData.Height * _scale);
					data = BilinearInterpolation.RescaleGrayscale(_originalData.Data, _originalData.Width, _originalData.Height, w, h);
					_scaledData = new GrayscalePixelDataU8(w, h, data);
				}
			}

			data = _scaledData.Data;

			int ox = (int)(_offsetX * _scale);
			int oy = (int)(_offsetY * _scale);

			MultiThread.For(0, _scaledData.Height, y => {
				for (int i = _scaledData.Width * y, e = i + _scaledData.Width; i < e; i++) {
					if (data[i] > 0) {
						int p = (oy * width) + ox + i;
						pixels[p] = _color;
					}
				}
			});
		}
		#endregion
	}
}
