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

#include "DcmJpegLsCodec.h"

#include "CharLS/stdafx.h"
#include "CharLS/interface.h"
#include "CharLS/util.h"
#include "CharLS/defaulttraits.h"
#include "CharLS/losslesstraits.h"
#include "CharLS/colortransform.h"
#include "CharLS/streams.h"
#include "CharLS/processline.h"

using namespace System;
using namespace System::IO;
using namespace System::Runtime::InteropServices;

using namespace Dicom::Data;
using namespace Dicom::Codec;

namespace Dicom {
namespace Codec {
namespace JpegLs {

public ref class DicomJpegLsCodecException : public DicomCodecException {
public:
	DicomJpegLsCodecException(JLS_ERROR error) : DicomCodecException(GetErrorMessage(error)) {
	}

private:
	static String^ GetErrorMessage(JLS_ERROR error) {
		switch (error) {
		case InvalidJlsParameters:
			return "Invalid JPEG-LS parameters";
		case ParameterValueNotSupported:
			return "Parameter value not supported";
		case UncompressedBufferTooSmall:
			return "Uncompressed buffer too small";
		case CompressedBufferTooSmall:
			return "Compressed buffer too small";
		case InvalidCompressedData:
			return "Invalid compressed data";
		case TooMuchCompressedData:
			return "Too much compressed data";
		case ImageTypeNotSupported:
			return "Image type not supported";
		case UnsupportedBitDepthForTransform:
			return "Unsupported bit depth for transform";
		case UnsupportedColorTransform:
			return "Unsupported color transform";
		default:
			return "Unknown error";
		}
	}
};

void DcmJpegLsCodec::Encode(DcmDataset^ dataset, DcmPixelData^ oldPixelData, DcmPixelData^ newPixelData, DcmCodecParameters^ parameters) {
	if ((oldPixelData->PhotometricInterpretation == "YBR_FULL_422")    ||
		(oldPixelData->PhotometricInterpretation == "YBR_PARTIAL_422") ||
		(oldPixelData->PhotometricInterpretation == "YBR_PARTIAL_420"))
		throw gcnew DicomCodecException(String::Format("Photometric Interpretation '{0}' not supported by JPEG-LS encoder",
														oldPixelData->PhotometricInterpretation));

	DcmJpegLsParameters^ jparams = (DcmJpegLsParameters^)parameters;
	if (jparams == nullptr)
		jparams = (DcmJpegLsParameters^)GetDefaultParameters();

	JlsParamaters params = {0};
	params.width = oldPixelData->ImageWidth;
	params.height = oldPixelData->ImageHeight;
	params.bitspersample = oldPixelData->BitsStored;
	params.bytesperline = oldPixelData->BytesAllocated * oldPixelData->ImageWidth * oldPixelData->SamplesPerPixel;
	params.components = oldPixelData->SamplesPerPixel;

	params.ilv = ILV_NONE;
	params.colorTransform = COLORXFORM_NONE;

	if (oldPixelData->SamplesPerPixel == 3) {
		params.ilv = (interleavemode)jparams->InterleaveMode;
		if (oldPixelData->PhotometricInterpretation == "RGB")
			params.colorTransform = (int)jparams->ColorTransform;
	}

	if (GetTransferSyntax() == DicomTransferSyntax::JPEGLSNearLossless) {
		params.allowedlossyerror = jparams->AllowedError;

		newPixelData->IsLossy = true;
	}

	for (int frame = 0; frame < oldPixelData->NumberOfFrames; frame++) {
		array<unsigned char>^ frameArray = oldPixelData->GetFrameDataU8(frame);
		pin_ptr<unsigned char> framePin = &frameArray[0];
		unsigned char* frameData = framePin;
		const int frameDataSize = frameArray->Length;

		// assume compressed frame will be smaller than original
		unsigned char* jpegData = new unsigned char[frameDataSize];
		size_t jpegDataSize = 0;

		JLS_ERROR err = JpegLsEncode(jpegData, frameDataSize, &jpegDataSize, frameData, frameDataSize, &params);
		if (err != OK) throw gcnew DicomJpegLsCodecException(err);

		oldPixelData->Unload();

		array<unsigned char>^ compData = gcnew array<unsigned char>(jpegDataSize);
		Marshal::Copy((IntPtr)jpegData, compData, 0, jpegDataSize);

		delete[] jpegData;

		newPixelData->AddFrame(compData);
	}

	if (newPixelData->TransferSyntax == DicomTransferSyntax::JPEGLSNearLossless && newPixelData->NumberOfFrames > 0) {
		newPixelData->IsLossy = true;
		newPixelData->LossyCompressionMethod = "ISO_14495_1";

		const double oldSize = oldPixelData->GetFrameSize(0);
		const double newSize = newPixelData->GetFrameSize(0);
		String^ ratio = String::Format("{0:0.000}", oldSize / newSize);
		newPixelData->LossyCompressionRatio = ratio;
	}
}

void DcmJpegLsCodec::Decode(DcmDataset^ dataset, DcmPixelData^ oldPixelData, DcmPixelData^ newPixelData, DcmCodecParameters^ parameters) {
	array<unsigned char>^ destArray = gcnew array<unsigned char>(oldPixelData->UncompressedFrameSize);
	pin_ptr<unsigned char> destPin = &destArray[0];
	unsigned char* destData = destPin;
	const int destDataSize = destArray->Length;

	for (int frame = 0; frame < oldPixelData->NumberOfFrames; frame++) {
		array<unsigned char>^ jpegArray = oldPixelData->GetFrameDataU8(frame);
		pin_ptr<unsigned char> jpegPin = &jpegArray[0];
		unsigned char* jpegData = jpegPin;
		const int jpegDataSize = jpegArray->Length;

		JLS_ERROR err = JpegLsDecode(destData, destDataSize, jpegData, jpegDataSize);
		if (err != OK) throw gcnew DicomJpegLsCodecException(err);

		oldPixelData->Unload();

		newPixelData->AddFrame(destArray);
	}
}

void DcmJpegLsCodec::Register() {
	DicomCodec::RegisterCodec(DicomTransferSyntax::JPEGLSNearLossless, DcmJpegLsNearLosslessCodec::typeid);
	DicomCodec::RegisterCodec(DicomTransferSyntax::JPEGLSLossless, DcmJpegLsLosslessCodec::typeid);
}

} // Jpeg2000
} // Codec
} // Dicom
