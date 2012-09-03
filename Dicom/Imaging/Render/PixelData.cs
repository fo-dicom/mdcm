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
using System.Threading.Tasks;

using Dicom;
using Dicom.Data;

using Dicom.Imaging.Algorithms;
using Dicom.Imaging.LUT;

using Dicom.Utility;

namespace Dicom.Imaging.Render {
	public interface IPixelData {
		int Width { get; }
		int Height { get; }
		int Components { get; }
		IPixelData Rescale(double scale);
		void Render(ILUT lut, int[] output);
	}

	public static class PixelDataFactory {
		public static IPixelData Create(DcmPixelData pixelData, int frame) {
			PhotometricInterpretation pi = PhotometricInterpretation.Lookup(pixelData.PhotometricInterpretation);
			if (pi == PhotometricInterpretation.Monochrome1 || pi == PhotometricInterpretation.Monochrome2 || pi == PhotometricInterpretation.PaletteColor) {
				if (pixelData.BitsStored <= 8)
					return new GrayscalePixelDataU8(pixelData.ImageWidth, pixelData.ImageHeight, pixelData.GetFrameDataU8(frame));
				else if (pixelData.BitsStored <= 16) {
					if (pixelData.IsSigned)
						return new GrayscalePixelDataS16(pixelData.ImageWidth, pixelData.ImageHeight, pixelData.GetFrameDataS16(frame));
					else
						return new GrayscalePixelDataU16(pixelData.ImageWidth, pixelData.ImageHeight, pixelData.GetFrameDataU16(frame));
				} else
					throw new DicomImagingException("Unsupported pixel data value for bits stored: {0}", pixelData.BitsStored);
			} else if (pi == PhotometricInterpretation.Rgb || pi == PhotometricInterpretation.YbrFull) {
				return new ColorPixelData24(pixelData.ImageWidth, pixelData.ImageHeight, pixelData.GetFrameDataU8(frame));
			} else {
				throw new DicomImagingException("Unsupported pixel data photometric interpretation: {0}", pi.Value);
			}
		}

		public static SingleBitPixelData Create(DcmOverlayData overlayData) {
			return new SingleBitPixelData(overlayData.Columns, overlayData.Rows, overlayData.Data);
		}
	}

	public class GrayscalePixelDataU8 : IPixelData {
		#region Private Members
		int _width;
		int _height;
		byte[] _data;
		#endregion

		#region Public Constructor
		public GrayscalePixelDataU8(int width, int height, byte[] data) {
			_width = width;
			_height = height;
			_data = data;
		}
		#endregion

		#region Public Properties
		public int Width {
			get { return _width; }
		}

		public int Height {
			get { return _height; }
		}

		public int Components {
			get { return 1; }
		}

		public byte[] Data {
			get { return _data; }
		}
		#endregion

		#region Public Methods
		public IPixelData Rescale(double scale) {
			int w = (int)(Width * scale);
			int h = (int)(Height * scale);
			byte[] data = BilinearInterpolation.RescaleGrayscale(_data, Width, Height, w, h);
			return new GrayscalePixelDataU8(w, h, data);
		}

		public void Render(ILUT lut, int[] output) {
			if (lut == null) {
				MultiThread.For(0, Height, y => {
					for (int i = Width * y, e = i + Width; i < e; i++) {
						output[i] = _data[i];
					}
				});
			}
			else {
				MultiThread.For(0, Height, y => {
					for (int i = Width * y, e = i + Width; i < e; i++) {
						output[i] = lut[_data[i]];
					}
				});
			}
		}
		#endregion
	}

	public class SingleBitPixelData : GrayscalePixelDataU8 {
		#region Public Constructor
		public SingleBitPixelData(int width, int height, byte[] data) : base(width, height, ExpandBits(width, height, data)) {
		}
		#endregion

		#region Static Methods
		private const byte One = 1;
		private const byte Zero = 0;

		private static byte[] ExpandBits(int width, int height, byte[] input) {
			BitArray bits = new BitArray(input);
			byte[] output = new byte[width * height];
			for (int i = 0, l = width * height; i < l; i++) {
				output[i] = bits[i] ? One : Zero;
			}
			return output;
		}
		#endregion
	}

	public class GrayscalePixelDataS16 : IPixelData {
		#region Private Members
		int _width;
		int _height;
		short[] _data;
		#endregion

		#region Public Constructor
		public GrayscalePixelDataS16(int width, int height, short[] data) {
			_width = width;
			_height = height;
			_data = data;
		}
		#endregion

		#region Public Properties
		public int Width {
			get { return _width; }
		}

		public int Height {
			get { return _height; }
		}

		public int Components {
			get { return 1; }
		}

		public short[] Data {
			get { return _data; }
		}
		#endregion

		#region Public Methods
		public IPixelData Rescale(double scale) {
			int w = (int)(Width * scale);
			int h = (int)(Height * scale);
			short[] data = BilinearInterpolation.RescaleGrayscale(_data, Width, Height, w, h);
			return new GrayscalePixelDataS16(w, h, data);
		}

		public void Render(ILUT lut, int[] output) {
			if (lut == null) {
				MultiThread.For(0, Height, y => {
					for (int i = Width * y, e = i + Width; i < e; i++) {
						output[i] = _data[i];
					}
				});
			}
			else {
				MultiThread.For(0, Height, y => {
					for (int i = Width * y, e = i + Width; i < e; i++) {
						output[i] = lut[_data[i]];
					}
				});
			}
		}
		#endregion
	}

	public class GrayscalePixelDataU16 : IPixelData {
		#region Private Members
		int _width;
		int _height;
		ushort[] _data;
		#endregion

		#region Public Constructor
		public GrayscalePixelDataU16(int width, int height, ushort[] data) {
			_width = width;
			_height = height;
			_data = data;
		}
		#endregion

		#region Public Properties
		public int Width {
			get { return _width; }
		}

		public int Height {
			get { return _height; }
		}

		public int Components {
			get { return 1; }
		}

		public ushort[] Data {
			get { return _data; }
		}
		#endregion

		#region Public Methods
		public IPixelData Rescale(double scale) {
			int w = (int)(Width * scale);
			int h = (int)(Height * scale);
			ushort[] data = BilinearInterpolation.RescaleGrayscale(_data, Width, Height, w, h);
			return new GrayscalePixelDataU16(w, h, data);
		}

		public void Render(ILUT lut, int[] output) {
			if (lut == null) {
				MultiThread.For(0, Height, y => {
					for (int i = Width * y, e = i + Width; i < e; i++) {
						output[i] = _data[i];
					}
				});
			}
			else {
				MultiThread.For(0, Height, y => {
					for (int i = Width * y, e = i + Width; i < e; i++) {
						output[i] = lut[_data[i]];
					}
				});
			}
		}
		#endregion
	}

	public class ColorPixelData24 : IPixelData {
		#region Private Members
		int _width;
		int _height;
		byte[] _data;
		#endregion

		#region Public Constructor
		public ColorPixelData24(int width, int height, byte[] data) {
			_width = width;
			_height = height;
			_data = data;
		}
		#endregion

		#region Public Properties
		public int Width {
			get { return _width; }
		}

		public int Height {
			get { return _height; }
		}

		public int Components {
			get { return 3; }
		}

		public byte[] Data {
			get { return _data; }
		}
		#endregion

		#region Public Methods
		public IPixelData Rescale(double scale) {
			int w = (int)(Width * scale);
			int h = (int)(Height * scale);
			byte[] data = BilinearInterpolation.RescaleColor24(_data, Width, Height, w, h);
			return new ColorPixelData24(w, h, data);
		}

		public void Render(ILUT lut, int[] output) {
			if (lut == null) {
				MultiThread.For(0, Height, y => {
					for (int i = Width * y, e = i + Width, p = i * 3; i < e; i++) {
						output[i] = (_data[p++] << 16) | (_data[p++] << 8) | _data[p++];
					}
				});
			}
			else {
				MultiThread.For(0, Height, y => {
					for (int i = Width * y, e = i + Width, p = i * 3; i < e; i++) {
						output[i] = (lut[_data[p++]] << 16) | (lut[_data[p++]] << 8) | lut[_data[p++]];
					}
				});
			}
		}
		#endregion
	}
}
