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

#include "stdio.h"
#include "string.h"

#include "DcmJpeg2000Codec.h"

using namespace System;
using namespace System::IO;
using namespace System::Runtime::InteropServices;

using namespace Dicom::Data;
using namespace Dicom::Codec;

extern "C" {
#include "OpenJPEG/openjpeg.h"
#include "OpenJPEG/j2k.h"

	void opj_error_callback(const char *msg, void *usr) {
		Dicom::Debug::Log->Error("OpenJPEG: {0}", gcnew String(msg));
	}

	void opj_warning_callback(const char *msg, void *) {
		Dicom::Debug::Log->Warn("OpenJPEG: {0}", gcnew String(msg));
	}

	void opj_info_callback(const char *msg, void *) {
		Dicom::Debug::Log->Info("OpenJPEG: {0}", gcnew String(msg));
	}
}

namespace Dicom {
namespace Codec {
namespace Jpeg2000 {

OPJ_COLOR_SPACE getOpenJpegColorSpace(String^ photometricInterpretation) {
	if (photometricInterpretation == "RGB")
		return CLRSPC_SRGB;
	else if (photometricInterpretation == "MONOCHROME1" || photometricInterpretation == "MONOCHROME2")
		return CLRSPC_GRAY;
	else if (photometricInterpretation == "PALETTE COLOR")
		return CLRSPC_GRAY;
	else if (photometricInterpretation == "YBR_FULL" || photometricInterpretation == "YBR_FULL_422" || photometricInterpretation == "YBR_PARTIAL_422")
		return CLRSPC_SYCC;
	else
		return CLRSPC_UNKNOWN;
}

void DcmJpeg2000Codec::Encode(DcmDataset^ dataset, DcmPixelData^ oldPixelData, DcmPixelData^ newPixelData, DcmCodecParameters^ parameters) {
	if ((oldPixelData->PhotometricInterpretation == "YBR_FULL_422")    ||
		(oldPixelData->PhotometricInterpretation == "YBR_PARTIAL_422") ||
		(oldPixelData->PhotometricInterpretation == "YBR_PARTIAL_420"))
		throw gcnew DicomCodecException(String::Format("Photometric Interpretation '{0}' not supported by JPEG 2000 encoder",
														oldPixelData->PhotometricInterpretation));

	DcmJpeg2000Parameters^ jparams = (DcmJpeg2000Parameters^)parameters;
	if (jparams == nullptr)
		jparams = (DcmJpeg2000Parameters^)GetDefaultParameters();

	int pixelCount = oldPixelData->ImageHeight * oldPixelData->ImageWidth;

	for (int frame = 0; frame < oldPixelData->NumberOfFrames; frame++) {
		array<unsigned char>^ frameArray = oldPixelData->GetFrameDataU8(frame);
		pin_ptr<unsigned char> framePin = &frameArray[0];
		unsigned char* frameData = framePin;
		const int frameDataSize = frameArray->Length;

		opj_image_cmptparm_t cmptparm[3];
		opj_cparameters_t eparams;  /* compression parameters */
		opj_event_mgr_t event_mgr;  /* event manager */
		opj_cinfo_t* cinfo = NULL;  /* handle to a compressor */
		opj_image_t *image = NULL;
		opj_cio_t *cio = NULL;

		memset(&event_mgr, 0, sizeof(opj_event_mgr_t));
		event_mgr.error_handler = opj_error_callback;
		if (jparams->IsVerbose) {
			event_mgr.warning_handler = opj_warning_callback;
			event_mgr.info_handler = opj_info_callback;
		}			

		cinfo = opj_create_compress(CODEC_J2K);

		opj_set_event_mgr((opj_common_ptr)cinfo, &event_mgr, NULL);

		opj_set_default_encoder_parameters(&eparams);
		eparams.cp_disto_alloc = 1;

		if (newPixelData->TransferSyntax == DicomTransferSyntax::JPEG2000Lossy && jparams->Irreversible)
			eparams.irreversible = 1;

		int r = 0;
		for (; r < jparams->RateLevels->Length; r++) {
			if (jparams->RateLevels[r] > jparams->Rate) {
				eparams.tcp_numlayers++;
				eparams.tcp_rates[r] = (float)jparams->RateLevels[r];
			} else
				break;
		}
		eparams.tcp_numlayers++;
		eparams.tcp_rates[r] = (float)jparams->Rate;

		if (newPixelData->TransferSyntax == DicomTransferSyntax::JPEG2000Lossless && jparams->Rate > 0)
			eparams.tcp_rates[eparams.tcp_numlayers++] = 0;

		if (oldPixelData->PhotometricInterpretation == "RGB" && jparams->AllowMCT)
			eparams.tcp_mct = 1;

		memset(&cmptparm[0], 0, sizeof(opj_image_cmptparm_t) * 3);
		for (int i = 0; i < oldPixelData->SamplesPerPixel; i++) {
			cmptparm[i].bpp = oldPixelData->BitsAllocated;
			cmptparm[i].prec = oldPixelData->BitsStored;
			if (!jparams->EncodeSignedPixelValuesAsUnsigned)
				cmptparm[i].sgnd = oldPixelData->PixelRepresentation;
			cmptparm[i].dx = eparams.subsampling_dx;
			cmptparm[i].dy = eparams.subsampling_dy;
			cmptparm[i].h = oldPixelData->ImageHeight;
			cmptparm[i].w = oldPixelData->ImageWidth;
		}

		try {
			OPJ_COLOR_SPACE color_space = getOpenJpegColorSpace(oldPixelData->PhotometricInterpretation);
			image = opj_image_create(oldPixelData->SamplesPerPixel, &cmptparm[0], color_space);

			image->x0 = eparams.image_offset_x0;
			image->y0 = eparams.image_offset_y0;
			image->x1 =	image->x0 + ((oldPixelData->ImageWidth - 1) * eparams.subsampling_dx) + 1;
			image->y1 =	image->y0 + ((oldPixelData->ImageHeight - 1) * eparams.subsampling_dy) + 1;

			for (int c = 0; c < image->numcomps; c++) {
				opj_image_comp_t* comp = &image->comps[c];

				int pos = oldPixelData->IsPlanar ? (c * pixelCount) : c;
				const int offset = oldPixelData->IsPlanar ? 1 : image->numcomps;

				if (oldPixelData->BytesAllocated == 1) {
					if (comp->sgnd) {
						if (oldPixelData->BitsStored < 8) {
							const unsigned char sign = 1 << oldPixelData->HighBit;
							const unsigned char mask = sign - 1;
							for (int p = 0; p < pixelCount; p++) {
								const unsigned char pixel = frameData[pos];
								if (pixel & sign)
									comp->data[p] = -(pixel & mask);
								else
									comp->data[p] = pixel;
								pos += offset;
							}
						}
						else {
							char* frameData8 = (char*)frameData;
							for (int p = 0; p < pixelCount; p++) {
								comp->data[p] = frameData8[pos];
								pos += offset;
							}
						}
					}
					else {
						for (int p = 0; p < pixelCount; p++) {
							comp->data[p] = frameData[pos];
							pos += offset;
						}
					}
				}
				else if (oldPixelData->BytesAllocated == 2) {
					if (comp->sgnd) {
						if (oldPixelData->BitsStored < 16) {
							unsigned short* frameData16 = (unsigned short*)frameData;
							const unsigned short sign = 1 << oldPixelData->HighBit;
							const unsigned short mask = sign - 1;
							for (int p = 0; p < pixelCount; p++) {
								const unsigned short pixel = frameData16[pos];
								if (pixel & sign)
									comp->data[p] = -(pixel & mask);
								else
									comp->data[p] = pixel;
								pos += offset;
							}
						}
						else {
							short* frameData16 = (short*)frameData;
							for (int p = 0; p < pixelCount; p++) {
								comp->data[p] = frameData16[pos];
								pos += offset;
							}
						}
					}
					else {
						unsigned short* frameData16 = (unsigned short*)frameData;
						for (int p = 0; p < pixelCount; p++) {
							comp->data[p] = frameData16[pos];
							pos += offset;
						}
					}
				}
				else
					throw gcnew DicomCodecException("JPEG 2000 codec only supports Bits Allocated == 8 or 16");
			}

			opj_setup_encoder(cinfo, &eparams, image);

			cio = opj_cio_open((opj_common_ptr)cinfo, NULL, 0);

			if (opj_encode(cinfo, cio, image, eparams.index)) {
				int clen = cio_tell(cio);
				array<unsigned char>^ cbuf = gcnew array<unsigned char>(clen);
				Marshal::Copy((IntPtr)cio->buffer, cbuf, 0, clen);
				newPixelData->AddFrame(cbuf);
			} else
				throw gcnew DicomCodecException("Unable to JPEG 2000 encode image");
		}
		finally {
			if (cio != nullptr)
				opj_cio_close(cio);
			if (image != nullptr)
				opj_image_destroy(image);
			if (cinfo != nullptr)
				opj_destroy_compress(cinfo);
		}
	}

	if (oldPixelData->PhotometricInterpretation == "RGB" && jparams->AllowMCT) {
		if (jparams->UpdatePhotometricInterpretation) {
			if (newPixelData->TransferSyntax == DicomTransferSyntax::JPEG2000Lossy && jparams->Irreversible)
				newPixelData->PhotometricInterpretation = "YBR_ICT";
			else
				newPixelData->PhotometricInterpretation = "YBR_RCT";
		}
	}

	if (newPixelData->TransferSyntax == DicomTransferSyntax::JPEG2000Lossy && newPixelData->NumberOfFrames > 0) {
		newPixelData->IsLossy = true;
		newPixelData->LossyCompressionMethod = "ISO_15444_1";

		const double oldSize = oldPixelData->GetFrameSize(0);
		const double newSize = newPixelData->GetFrameSize(0);
		String^ ratio = String::Format("{0:0.000}", oldSize / newSize);
		newPixelData->LossyCompressionRatio = ratio;
	}
}

void DcmJpeg2000Codec::Decode(DcmDataset^ dataset, DcmPixelData^ oldPixelData, DcmPixelData^ newPixelData, DcmCodecParameters^ parameters) {
	DcmJpeg2000Parameters^ jparams = (DcmJpeg2000Parameters^)parameters;
	if (jparams == nullptr)
		jparams = (DcmJpeg2000Parameters^)GetDefaultParameters();

	array<unsigned char>^ destArray = gcnew array<unsigned char>(oldPixelData->UncompressedFrameSize);
	pin_ptr<unsigned char> destPin = &destArray[0];
	unsigned char* destData = destPin;
	const int destDataSize = destArray->Length;

	const int pixelCount = oldPixelData->ImageHeight * oldPixelData->ImageWidth;

	if (newPixelData->PhotometricInterpretation == "YBR_RCT" || newPixelData->PhotometricInterpretation == "YBR_ICT")
		newPixelData->PhotometricInterpretation = "RGB";

	if (newPixelData->PhotometricInterpretation == "YBR_FULL_422" || newPixelData->PhotometricInterpretation == "YBR_PARTIAL_422")
		newPixelData->PhotometricInterpretation = "YBR_FULL";
	
	if (newPixelData->PhotometricInterpretation == "YBR_FULL")
		newPixelData->PlanarConfiguration = 1;

	for (int frame = 0; frame < oldPixelData->NumberOfFrames; frame++) {
		array<unsigned char>^ jpegArray = oldPixelData->GetFrameDataU8(frame);
		pin_ptr<unsigned char> jpegPin = &jpegArray[0];
		unsigned char* jpegData = jpegPin;
		const int jpegDataSize = jpegArray->Length;

		opj_dparameters_t dparams;
		opj_event_mgr_t event_mgr;
		opj_image_t *image = NULL;
		opj_dinfo_t* dinfo = NULL;
		opj_cio_t *cio = NULL;

		memset(&event_mgr, 0, sizeof(opj_event_mgr_t));
		event_mgr.error_handler = opj_error_callback;
		if (jparams->IsVerbose) {
			event_mgr.warning_handler = opj_warning_callback;
			event_mgr.info_handler = opj_info_callback;
		}

		opj_set_default_decoder_parameters(&dparams);
		dparams.cp_layer=0;
		dparams.cp_reduce=0;

		try {
			dinfo = opj_create_decompress(CODEC_J2K);

			opj_set_event_mgr((opj_common_ptr)dinfo, &event_mgr, NULL);

			opj_setup_decoder(dinfo, &dparams);

			bool opj_err = false;
			dinfo->client_data = (void*)&opj_err;

			cio = opj_cio_open((opj_common_ptr)dinfo, jpegData, (int)jpegDataSize);
			image = opj_decode(dinfo, cio);

			oldPixelData->Unload();

			if (image == nullptr)
				throw gcnew DicomCodecException("Error in JPEG 2000 code stream!");

			for (int c = 0; c < image->numcomps; c++) {
				opj_image_comp_t* comp = &image->comps[c];

				int pos = newPixelData->IsPlanar ? (c * pixelCount) : c;
				const int offset = newPixelData->IsPlanar ? 1 : image->numcomps;

				if (newPixelData->BytesAllocated == 1) {
					if (comp->sgnd) {
						const unsigned char sign = 1 << newPixelData->HighBit;
						for (int p = 0; p < pixelCount; p++) {
							const int i = comp->data[p];
							if (i < 0)
								destArray[pos] = (unsigned char)(-i | sign);
							else
								destArray[pos] = (unsigned char)(i);
							pos += offset;
						}
					}
					else {
						for (int p = 0; p < pixelCount; p++) {
							destArray[pos] = (unsigned char)comp->data[p];
							pos += offset;
						}
					}
				}
				else if (newPixelData->BytesAllocated == 2) {
					const unsigned short sign = 1 << newPixelData->HighBit;
					unsigned short* destData16 = (unsigned short*)destData;
					if (comp->sgnd) {
						for (int p = 0; p < pixelCount; p++) {
							const int i = comp->data[p];
							if (i < 0)
								destData16[pos] = (unsigned short)(-i | sign);
							else
								destData16[pos] = (unsigned short)(i);
							pos += offset;
						}
					}
					else {
						for (int p = 0; p < pixelCount; p++) {
							destData16[pos] = (unsigned short)comp->data[p];
							pos += offset;
						}
					}
				}
				else
					throw gcnew DicomCodecException("JPEG 2000 module only supports Bytes Allocated == 8 or 16!");
			}

			newPixelData->AddFrame(destArray);
		}
		finally {
			if (cio != nullptr)
				opj_cio_close(cio);
			if (dinfo != nullptr)
				opj_destroy_decompress(dinfo);
			if (image != nullptr)
				opj_image_destroy(image);
		}
	}
}

void DcmJpeg2000Codec::Register() {
	DicomCodec::RegisterCodec(DicomTransferSyntax::JPEG2000Lossy, DcmJpeg2000LossyCodec::typeid);
	DicomCodec::RegisterCodec(DicomTransferSyntax::JPEG2000Lossless, DcmJpeg2000LosslessCodec::typeid);
}

} // Jpeg2000
} // Codec
} // Dicom
