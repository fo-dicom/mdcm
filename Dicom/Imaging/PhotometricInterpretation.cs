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

namespace Dicom.Imaging {
	/// <summary>
	/// Photometric Interpretation
	/// </summary>
	public class PhotometricInterpretation {
		#region Private Members
		private readonly string _value;
		private readonly string _description;
		private readonly bool _isColor;
		private readonly bool _isYbr;
		private readonly bool _isPalette;
		#endregion

		#region Constructor
		private PhotometricInterpretation(string value, string description, bool isColor, bool isPalette, bool isYbr) {
			_value = value;
			_description = description;
			_isColor = isColor;
			_isPalette = isPalette;
			_isYbr = isYbr;
		}
		#endregion

		#region Public Properties
		public string Value {
			get { return _value; }
		}

		public string Description {
			get { return _description; }
		}

		public bool IsColor {
			get { return _isColor; }
		}

		public bool IsPalette {
			get { return _isPalette; }
		}

		public bool IsYBR {
			get { return _isYbr; }
		}
		#endregion

		#region Public Methods
		public override string ToString() {
			return Description;
		}
		#endregion

		#region Static Methods
		private static Dictionary<string, PhotometricInterpretation> _photometricInterpretationMap;
		static PhotometricInterpretation() {
			_photometricInterpretationMap = new Dictionary<string, PhotometricInterpretation>();
			_photometricInterpretationMap.Add(Monochrome1.Value, Monochrome1);
			_photometricInterpretationMap.Add(Monochrome2.Value, Monochrome2);
			_photometricInterpretationMap.Add(PaletteColor.Value, PaletteColor);
			_photometricInterpretationMap.Add(Rgb.Value, Rgb);
			_photometricInterpretationMap.Add(YbrFull.Value, YbrFull);
			_photometricInterpretationMap.Add(YbrFull422.Value, YbrFull422);
			_photometricInterpretationMap.Add(YbrPartial422.Value, YbrPartial422);
			_photometricInterpretationMap.Add(YbrPartial420.Value, YbrPartial420);
			_photometricInterpretationMap.Add(YbrIct.Value, YbrIct);
			_photometricInterpretationMap.Add(YbrRct.Value, YbrRct);
		}

		public PhotometricInterpretation Lookup(string photometricInterpretation) {
			PhotometricInterpretation pi;
			if (!_photometricInterpretationMap.TryGetValue(photometricInterpretation, out pi))
				throw new DicomImagingException("Unknown Photometric Interpretation [{0}]", photometricInterpretation);
			return pi;
		}
		#endregion

		/// <summary>
		/// Pixel data represent a single monochrome image plane. The minimum sample value is intended 
		/// to be displayed as white after any VOI gray scale transformations have been performed. See 
		/// PS 3.4. This value may be used only when Samples per Pixel (0028,0002) has a value of 1.
		/// </summary>
		public readonly static PhotometricInterpretation Monochrome1 = 
			new PhotometricInterpretation("MONOCHROME1", "Monochrome 1", false, false, false);

		/// <summary>
		/// Pixel data represent a single monochrome image plane. The minimum sample value is intended 
		/// to be displayed as black after any VOI gray scale transformations have been performed. See 
		/// PS 3.4. This value may be used only when Samples per Pixel (0028,0002) has a value of 1.
		/// </summary>
		public readonly static PhotometricInterpretation Monochrome2 = 
			new PhotometricInterpretation("MONOCHROME2", "Monochrome 2", false, false, false);

		/// <summary>
		/// Pixel data describe a color image with a single sample per pixel (single image plane). The 
		/// pixel value is used as an index into each of the Red, Blue, and Green Palette Color Lookup 
		/// Tables (0028,1101-1103&1201-1203). This value may be used only when Samples per Pixel (0028,0002) 
		/// has a value of 1. When the Photometric Interpretation is Palette Color; Red, Blue, and Green 
		/// Palette Color Lookup Tables shall be present.
		/// </summary>
		public readonly static PhotometricInterpretation PaletteColor = 
			new PhotometricInterpretation("PALETTE COLOR", "Palette Color", true, true, false);

		/// <summary>
		/// Pixel data represent a color image described by red, green, and blue image planes. The minimum 
		/// sample value for each color plane represents minimum intensity of the color. This value may be 
		/// used only when Samples per Pixel (0028,0002) has a value of 3.
		/// </summary>
		public readonly static PhotometricInterpretation Rgb = 
			new PhotometricInterpretation("RGB", "RGB", true, false, false);

		/// <summary>
		/// Pixel data represent a color image described by one luminance (Y) and two chrominance planes 
		/// (Cb and Cr). This photometric interpretation may be used only when Samples per Pixel (0028,0002) 
		/// has a value of 3. Black is represented by Y equal to zero. The absence of color is represented 
		/// by both Cb and Cr values equal to half full scale.
		/// 
		/// In the case where Bits Allocated (0028,0100) has a value of 8 then the following equations convert 
		/// between RGB and YCBCR Photometric Interpretation:
		/// Y  = + .2990R + .5870G + .1140B
		/// Cb = - .1687R - .3313G + .5000B + 128
		/// Cr = + .5000R - .4187G - .0813B + 128
		/// </summary>
		public readonly static PhotometricInterpretation YbrFull = 
			new PhotometricInterpretation("YBR_FULL", "YBR Full", true, false, true);

		/// <summary>
		/// The same as YBR_FULL except that the Cb and Cr values are sampled horizontally at half the Y rate 
		/// and as a result there are half as many Cb and Cr values as Y values.
		/// 
		/// This Photometric Interpretation is only allowed with Planar Configuration (0028,0006) equal to 0.  
		/// Two Y values shall be stored followed by one Cb and one Cr value. The Cb and Cr values shall be 
		/// sampled at the location of the first of the two Y values. For each Row of Pixels, the first Cb and 
		/// Cr samples shall be at the location of the first Y sample. The next Cb and Cr samples shall be 
		/// at the location of the third Y sample etc.
		/// </summary>
		public readonly static PhotometricInterpretation YbrFull422 = 
			new PhotometricInterpretation("YBR_FULL_422", "YBR Full 4:2:2", true, false, true);

		/// <summary>
		/// The same as YBR_FULL_422 except that:
		/// <list type="number">
		/// <item>black corresponds to Y = 16</item>
		/// <item>Y is restricted to 220 levels (i.e. the maximum value is 235)</item>
		/// <item>Cb and Cr each has a minimum value of 16</item>
		/// <item>Cb and Cr are restricted to 225 levels (i.e. the maximum value is 240)</item>
		/// <item>lack of color is represented by Cb and Cr equal to 128</item>
		/// </list>
		/// 
		/// In the case where Bits Allocated (0028,0100) has value of 8 then the following equations convert 
		/// between RGB and YBR_PARTIAL_422 Photometric Interpretation:
		/// Y  = + .2568R + .5041G + .0979B + 16
		/// Cb = - .1482R - .2910G + .4392B + 128
		/// Cr = + .4392R - .3678G - .0714B + 128
		/// </summary>
		public readonly static PhotometricInterpretation YbrPartial422 = 
			new PhotometricInterpretation("YBR_PARTIAL_422", "YBR Partial 4:2:2", true, false, true);

		/// <summary>
		/// The same as YBR_PARTIAL_422 except that the Cb and Cr values are sampled horizontally and vertically 
		/// at half the Y rate and as a result there are four times less Cb and Cr values than Y values, versus 
		/// twice less for YBR_PARTIAL_422.
		/// 
		/// This Photometric Interpretation is only allowed with Planar Configuration (0028,0006) equal to 0.  
		/// The Cb and Cr values shall be sampled at the location of the first of the two Y values. For the first 
		/// Row of Pixels (etc.), the first Cb and Cr samples shall be at the location of the first Y sample.  The 
		/// next Cb and Cr samples shall be at the location of the third Y sample etc. The next Rows of Pixels 
		/// containing Cb and Cr samples (at the same locations than for the first Row) will be the third etc.
		/// </summary>
		public readonly static PhotometricInterpretation YbrPartial420 = 
			new PhotometricInterpretation("YBR_PARTIAL_420", "YBR Partial 4:2:0", true, false, true);

		/// <summary>
		/// Pixel data represent a color image described by one luminance (Y) and two chrominance planes 
		/// (Cb and Cr). This photometric interpretation may be used only when Samples per Pixel (0028,0002) has 
		/// a value of 3. Black is represented by Y equal to zero. The absence of color is represented by both 
		/// Cb and Cr values equal to zero.
		/// 
		/// Regardless of the value of Bits Allocated (0028,0100), the following equations convert between RGB 
		/// and YCbCr Photometric Interpretation:
		/// Y  = + .29900R + .58700G + .11400B
		/// Cb = - .16875R - .33126G + .50000B
		/// Cr = + .50000R - .41869G - .08131B
		/// </summary>
		public readonly static PhotometricInterpretation YbrIct = 
			new PhotometricInterpretation("YBR_ICT", "YBR Irreversible Color Transformation (JPEG 2000)", true, false, true);

		/// <summary>
		/// Pixel data represent a color image described by one luminance (Y) and two chrominance planes 
		/// (Cb and Cr). This photometric interpretation may be used only when Samples per Pixel (0028,0002) 
		/// has a value of 3. Black is represented by Y equal to zero. The absence of color is represented 
		/// by both Cb and Cr values equal to zero.
		/// 
		/// Regardless of the value of Bits Allocated (0028,0100), the following equations convert between 
		/// RGB and YBR_RCT Photometric Interpretation:
		/// Y  = floor((R + 2G +B) / 4)
		/// Cb = B - G
		/// Cr = R - G
		/// 
		/// The following equations convert between YBR_RCT and RGB Photometric Interpretation:
		/// R = Cr + G
		/// G = Y – floor((Cb + Cr) / 4)
		/// B = Cb + G
		/// </summary>
		public readonly static PhotometricInterpretation YbrRct = 
			new PhotometricInterpretation("YBR_RCT", "YBR Reversible Color Transformation (JPEG 2000)", true, false, true);
	}
}
