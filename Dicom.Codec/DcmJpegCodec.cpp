// mDCM: A C# DICOM library
//
// Copyright (c) 2008  Colby Dillion
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

#include "DcmJpegCodec.h"

using namespace System;
using namespace System::IO;

using namespace Dicom::Codec;
using namespace Dicom::Data;
using namespace Dicom::IO;

#include "JpegCodec.h"
#include "JpegHelper.h"
#include "DcmJpegParameters.h"

namespace Dicom {
namespace Codec {
namespace Jpeg {
	void DcmJpegCodec::Encode(DcmDataset^ dataset, DcmPixelData^ oldPixelData, DcmPixelData^ newPixelData, DcmCodecParameters^ parameters)
	{
		if (oldPixelData->NumberOfFrames == 0)
			return;

		// IJG eats the extra padding bits. Is there a better way to test for this?
		if (oldPixelData->BitsAllocated == 16 && oldPixelData->BitsStored <= 8) {
			// check for embedded overlays?
			newPixelData->BitsAllocated = 8;
		}

		if (parameters == nullptr || parameters->GetType() != DcmJpegParameters::typeid)
			parameters = GetDefaultParameters();

		DcmJpegParameters^ jparams = (DcmJpegParameters^)parameters;

		IJpegCodec^ codec = GetCodec(oldPixelData->BitsStored, jparams);

		for (int frame = 0; frame < oldPixelData->NumberOfFrames; frame++) {
			codec->Encode(oldPixelData, newPixelData, jparams, frame);
		}

		if (codec->Mode != JpegMode::Lossless) {
			newPixelData->IsLossy = true;
			newPixelData->LossyCompressionMethod = "ISO_10918_1";

			double oldSize = oldPixelData->GetFrameSize(0);
			double newSize = newPixelData->GetFrameSize(0);
			String^ ratio = String::Format("{0:0.000}", oldSize / newSize);
			newPixelData->LossyCompressionRatio = ratio;
		}
	}

	void DcmJpegCodec::Decode(DcmDataset^ dataset, DcmPixelData^ oldPixelData, DcmPixelData^ newPixelData, DcmCodecParameters^ parameters)
	{
		if (oldPixelData->NumberOfFrames == 0)
			return;

		// IJG eats the extra padding bits. Is there a better way to test for this?
		if (newPixelData->BitsAllocated == 16 && newPixelData->BitsStored <= 8) {
			// check for embedded overlays here or below?
			newPixelData->BitsAllocated = 8;
		}

		if (parameters == nullptr || parameters->GetType() != DcmJpegParameters::typeid)
			parameters = GetDefaultParameters();

		DcmJpegParameters^ jparams = (DcmJpegParameters^)parameters;

		int precision = 0;
		try {
			precision = JpegHelper::ScanHeaderForBitDepth(oldPixelData);
		}
		catch (...) {
			// the old scanner choked on several valid images...
			// assume the correct encoder was used and let libijg handle the rest
			precision = oldPixelData->BitsStored;
		}

		if (newPixelData->BitsStored <= 8 && precision > 8)
			newPixelData->BitsAllocated = 16; // embedded overlay?

		IJpegCodec^ codec = GetCodec(precision, jparams);

		for (int frame = 0; frame < oldPixelData->NumberOfFrames; frame++) {
			codec->Decode(oldPixelData, newPixelData, jparams, frame);
		}
	}

	void DcmJpegCodec::Register() {
		DicomCodec::RegisterCodec(DicomTransferSyntax::JPEGProcess1, DcmJpegProcess1Codec::typeid);
		DicomCodec::RegisterCodec(DicomTransferSyntax::JPEGProcess2_4, DcmJpegProcess4Codec::typeid);
		DicomCodec::RegisterCodec(DicomTransferSyntax::JPEGProcess14, DcmJpegLossless14Codec::typeid);
		DicomCodec::RegisterCodec(DicomTransferSyntax::JPEGProcess14SV1, DcmJpegLossless14SV1Codec::typeid);
	}
} // Jpeg
} // Codec
} // Dicom