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
using System.Windows.Media;
using System.IO;

namespace Dicom.Imaging {
	public static class ColorTable {
		public readonly static Color[] Monochrome1 = InitGrayscaleLUT(true);
		public readonly static Color[] Monochrome2 = InitGrayscaleLUT(false);

		private static Color[] InitGrayscaleLUT(bool reverse) {
			Color[] LUT = new Color[256];
			int i;
			byte b;
			if (reverse) {
				for (i = 0, b = 255; i < 256; i++, b--) {
					LUT[i] = Color.FromArgb(0xff, b, b, b);
				}
			} else {
				for (i = 0, b = 0; i < 256; i++, b++) {
					LUT[i] = Color.FromArgb(0xff, b, b, b);
				}
			}
			return LUT;
		}

		public static Color[] Reverse(Color[] lut) {
			Color[] clone = new Color[lut.Length];
			Array.Copy(lut, clone, clone.Length);
			Array.Reverse(clone);
			return clone;
		}

		public static Color[] LoadLUT(string file) {
			try {
				byte[] data = File.ReadAllBytes(file);
				if (data.Length != (256 * 3))
					return null;

				Color[] LUT = new Color[256];
				for (int i = 0; i < 256; i++) {
					LUT[i] = Color.FromArgb(0xff, data[i], data[i + 256], data[i + 512]);
				}
				return LUT;
			} catch {
				return null;
			}
		}

		public static void SaveLUT(string file, Color[] lut) {
			if (lut.Length != 256) return;
			FileStream fs = new FileStream(file, FileMode.Create);
			for (int i = 0; i < 256; i++) fs.WriteByte(lut[i].R);
			for (int i = 0; i < 256; i++) fs.WriteByte(lut[i].G);
			for (int i = 0; i < 256; i++) fs.WriteByte(lut[i].B);
			fs.Close();
		}

#if !SILVERLIGHT
		public static void Apply(System.Drawing.Image image, Color[] lut) {
			System.Drawing.Imaging.ColorPalette palette = image.Palette;
			for (int i = 0; i < palette.Entries.Length; i++)
				palette.Entries[i] = System.Drawing.Color.FromArgb(lut[i].A, lut[i].R, lut[i].G, lut[i].B);
			image.Palette = palette;
		}
#endif
	}
}

