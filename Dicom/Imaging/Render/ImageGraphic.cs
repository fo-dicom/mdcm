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
using System.Threading;

using Dicom.Utility;

using Dicom.Imaging.Algorithms;
using Dicom.Imaging.LUT;
using Dicom.Imaging.Render;

namespace Dicom.Imaging.Display {
	public class ImageGraphic : IGraphic {
		#region Protected Members
		protected IPixelData _originalData;
		protected IPixelData _scaledData;

		protected PinnedIntArray _pixels;
		protected Bitmap _bitmap;

		protected double _scaleFactor;
		protected int _rotation;
		protected bool _flipX;
		protected bool _flipY;
		protected int _offsetX;
		protected int _offsetY;

		protected int _zorder;
		protected bool _applyLut;
		#endregion

		#region Public Properties
		public int Components {
			get { return OriginalData.Components; }
		}

		public IPixelData OriginalData {
			get { return _originalData; }
		}
		public int OriginalWidth {
			get { return _originalData.Width; }
		}
		public int OriginalHeight {
			get { return _originalData.Height; }
		}
		public int OriginalOffsetX {
			get { return _offsetX; }
		}

		public int OriginalOffsetY {
			get { return _offsetY; }
		}

		public double ScaleFactor {
			get { return _scaleFactor; }
		}

		public IPixelData ScaledData {
			get {
				if (_scaledData == null) {
					if (Math.Abs(_scaleFactor - 1.0) <= Double.Epsilon)
						_scaledData = _originalData;
					else
						_scaledData = OriginalData.Rescale(_scaleFactor);
				}
				return _scaledData;
			}
		}
		public int ScaledWidth {
			get { return ScaledData.Width; }
		}
		public int ScaledHeight {
			get { return ScaledData.Height; }
		}
		public int ScaledOffsetX {
			get { return (int)(_offsetX * _scaleFactor); }
		}

		public int ScaledOffsetY {
			get { return (int)(_offsetY * _scaleFactor); }
		}

		public int ZOrder {
			get { return _zorder; }
			set { _zorder = value; }
		}
		#endregion

		#region Public Constructors
		public ImageGraphic(IPixelData pixelData) {
			_originalData = pixelData;
			_zorder = 255;
			_applyLut = true;
			Scale(1.0);
		}

		protected ImageGraphic() { }
		#endregion

		#region Public Members
		public void Reset() {
			Scale(1.0);
			_rotation = 0;
			_flipX = false;
			_flipY = false;
		}

		public void Scale(double scale) {
			if (Math.Abs(scale - _scaleFactor) <= Double.Epsilon)
				return;

			_scaleFactor = scale;
			if (_bitmap != null) {
				_scaledData = null;
				_pixels.Dispose();
				_pixels = null;
				_bitmap = null;
			}
		}

		public void BestFit(int width, int height) {
			double xF = (double)width / (double)OriginalWidth;
			double yF = (double)height / (double)OriginalHeight;
			Scale(Math.Min(xF, yF));
		}

		public void Rotate(int angle) {
			if (angle > 0) {
				if (angle <= 90)
					_rotation += 90;
				else if (angle <= 180)
					_rotation += 180;
				else if (angle <= 270)
					_rotation += 270;
			} else if (angle < 0) {
				if (angle >= -90)
					_rotation -= 90;
				else if (angle >= -180)
					_rotation -= 180;
				else if (angle >= -270)
					_rotation -= 270;
			}
			if (angle != 0) {
				if (_rotation >= 360)
					_rotation -= 360;
				else if (_rotation < 0)
					_rotation += 360;
			}
		}

		public void FlipX() {
			_flipX = !_flipX;
		}

		public void FlipY() {
			_flipY = !_flipY;
		}

		public void Transform(double scale, int rotation, bool flipx, bool flipy) {
			Scale(scale);
			Rotate(rotation);
			_flipX = flipx;
			_flipY = flipy;
		}

		public Image RenderImage(ILUT lut) {
			bool render = false;
			if (_bitmap == null) {
				PixelFormat format = Components == 4 ? PixelFormat.Format32bppArgb : PixelFormat.Format32bppRgb;
				_pixels = new PinnedIntArray(ScaledData.Width * ScaledData.Height);
				_bitmap = new Bitmap(ScaledData.Width, ScaledData.Height, ScaledData.Width * 4, format, _pixels.Pointer);
				render = true;
			}
			if (_applyLut && lut != null && !lut.IsValid) {
				lut.Recalculate();
				render = true;
			}
			_bitmap.RotateFlip(RotateFlipType.RotateNoneFlipNone);
			if (render) {
				ScaledData.Render((_applyLut ? lut : null), _pixels.Data);
			}
			_bitmap.RotateFlip(GetRotateFlipType());
			return _bitmap;
		}

		protected RotateFlipType GetRotateFlipType() {
			if (_flipX && _flipY) {
				switch (_rotation) {
				case  90: return RotateFlipType.Rotate90FlipXY;
				case 180: return RotateFlipType.Rotate180FlipXY;
				case 270: return RotateFlipType.Rotate270FlipXY;
				default: return RotateFlipType.RotateNoneFlipXY;
				}
			} else if (_flipX) {
				switch (_rotation) {
				case  90: return RotateFlipType.Rotate90FlipX;
				case 180: return RotateFlipType.Rotate180FlipX;
				case 270: return RotateFlipType.Rotate270FlipX;
				default: return RotateFlipType.RotateNoneFlipX;
				}
			} else if (_flipY) {
				switch (_rotation) {
				case  90: return RotateFlipType.Rotate90FlipY;
				case 180: return RotateFlipType.Rotate180FlipY;
				case 270: return RotateFlipType.Rotate270FlipY;
				default: return RotateFlipType.RotateNoneFlipY;
				}
			} else {
				switch (_rotation) {
				case  90: return RotateFlipType.Rotate90FlipNone;
				case 180: return RotateFlipType.Rotate180FlipNone;
				case 270: return RotateFlipType.Rotate270FlipNone;
				default: return RotateFlipType.RotateNoneFlipNone;
				}
			}
		}
		#endregion
	}
}
