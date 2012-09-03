// mDCM: A C# DICOM library
//
// Copyright (c) 2006-2008  Colby Dillion
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
using System.IO;
using System.Text;

using Dicom.Data;

namespace Dicom.Codec {
	public static class DcmCodecHelper {
		public static void ChangePlanarConfiguration(byte[] pixelData, int numValues, int bitsAllocated, 
			int samplesPerPixel, int oldPlanarConfiguration) {
			int bytesAllocated = bitsAllocated / 8;
			int numPixels = numValues / samplesPerPixel;
			if (bytesAllocated == 1) {
				byte[] buffer = new byte[pixelData.Length];
				if (oldPlanarConfiguration == 1) {
					for (int n = 0; n < numPixels; n++) {
						for (int s = 0; s < samplesPerPixel; s++) {
							buffer[n * samplesPerPixel + s] = pixelData[n + numPixels * s];
						}
					}
				}
				else {
					for (int n = 0; n < numPixels; n++) {
						for (int s = 0; s < samplesPerPixel; s++) {
							buffer[n + numPixels * s] = pixelData[n * samplesPerPixel + s];
						}
					}
				}
				Buffer.BlockCopy(buffer, 0, pixelData, 0, numValues);
			}
			else if (bytesAllocated == 2) {
				throw new DicomCodecException(String.Format("BitsAllocated={0} is not supported!", bitsAllocated));
			}
			else
				throw new DicomCodecException(String.Format("BitsAllocated={0} is not supported!", bitsAllocated));
		}

		public static void DumpFrameToDisk(DcmDataset data, int frame, string file) {
			DcmPixelData pixelData = new DcmPixelData(data);
			byte[] pixels = pixelData.GetFrameDataU8(frame);
			File.WriteAllBytes(file, pixels);
		}
	}
}
