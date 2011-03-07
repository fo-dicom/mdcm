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

using Dicom;
using Dicom.Data;

using Dicom.Imaging.LUT;

namespace Dicom.Imaging.Process {
	public interface IPipeline {
		ILUT LUT {
			get;
		}
	}

	public static class PipelineFactory {
		public static IPipeline Create(DcmDataset dataset, DcmPixelData pixelData) {
			PhotometricInterpretation pi = PhotometricInterpretation.Lookup(pixelData.PhotometricInterpretation);
			if (pi == PhotometricInterpretation.Monochrome1 || pi == PhotometricInterpretation.Monochrome2) {
				GenericGrayscalePipeline pipeline = new GenericGrayscalePipeline(pixelData.RescaleSlope, pixelData.RescaleIntercept, pixelData.BitsStored, pixelData.IsSigned);
				if (pi == PhotometricInterpretation.Monochrome1)
					pipeline.ColorMap = ColorTable.Monochrome1;
				else
					pipeline.ColorMap = ColorTable.Monochrome2;
				WindowLevel[] wl = WindowLevel.FromDataset(dataset);
				if (wl.Length > 0)
					pipeline.WindowLevel = wl[0];
				return pipeline;
			} else if (pi == PhotometricInterpretation.Rgb) {
				return new RgbColorPipeline();
			} else {
				throw new DicomImagingException("Unsupported pipeline photometric interpretation: {0}", pi.Value);
			}
		}
	}
}
