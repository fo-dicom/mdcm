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
#if SILVERLIGHT || WPF
using System.Windows.Media;
using System.Windows.Media.Imaging;
#else
using System.Drawing;
#endif
using Dicom.Imaging.LUT;

namespace Dicom.Imaging.Render {
	public interface IGraphic {
		int OriginalWidth { get; }
		int OriginalHeight { get; }
		int OriginalOffsetX { get; }
		int OriginalOffsetY { get; }
		double ScaleFactor { get; }
		int ScaledWidth { get; }
		int ScaledHeight { get; }
		int ScaledOffsetX { get; }
		int ScaledOffsetY { get; }

		int ZOrder { get; }

		void Reset();
		void Scale(double scale);
		void BestFit(int width, int height);
		void Rotate(int angle);
		void FlipX();
		void FlipY();
		void Transform(double scale, int rotation, bool flipx, bool flipy);
#if SILVERLIGHT || WPF
		BitmapSource RenderImage(ILUT lut);
#else
		Image RenderImage(ILUT lut);
#endif
	}
}
