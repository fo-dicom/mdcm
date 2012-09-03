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

#ifndef __DCMJPEGCODEC_H__
#define __DCMJPEGCODEC_H__

#pragma once

using namespace System;
using namespace System::IO;

using namespace Dicom::Codec;
using namespace Dicom::Data;

#include "JpegCodec.h"
#include "DcmJpegParameters.h"

namespace Dicom {
namespace Codec {
namespace Jpeg {

public ref class DcmJpegCodec abstract : public IDcmCodec {
public:
	virtual String^ GetName() {
		return GetTransferSyntax()->UID->Description;
	}

	virtual DicomTransferSyntax^ GetTransferSyntax() = 0;

	virtual DcmCodecParameters^ GetDefaultParameters() {
		return gcnew DcmJpegParameters();
	}

	virtual void Encode(DcmDataset^ dataset, DcmPixelData^ oldPixelData, DcmPixelData^ newPixelData, DcmCodecParameters^ parameters);
	virtual void Decode(DcmDataset^ dataset, DcmPixelData^ oldPixelData, DcmPixelData^ newPixelData, DcmCodecParameters^ parameters);

	virtual IJpegCodec^ GetCodec(int bits, DcmJpegParameters^ jparams) = 0;

	static void Register();
};


[DicomCodec]
public ref class DcmJpegProcess1Codec : public DcmJpegCodec {
public:
	virtual DicomTransferSyntax^ GetTransferSyntax() override {
		return DicomTransferSyntax::JPEGProcess1;
	}

	virtual IJpegCodec^ GetCodec(int bits, DcmJpegParameters^ jparams) override {
		if (bits == 8)
			return gcnew Jpeg8Codec(JpegMode::Baseline, 0, 0);
		else
			throw gcnew DicomCodecException(String::Format("Unable to create JPEG Process 1 codec for bits stored == {0}", bits));
	}
};

[DicomCodec]
public ref class DcmJpegProcess4Codec : public DcmJpegCodec {
public:
	virtual DicomTransferSyntax^ GetTransferSyntax() override {
		return DicomTransferSyntax::JPEGProcess2_4;
	}

	virtual IJpegCodec^ GetCodec(int bits, DcmJpegParameters^ jparams) override {
		if (bits == 12)
			return gcnew Jpeg12Codec(JpegMode::Sequential, 0, 0);
		else
			throw gcnew DicomCodecException(String::Format("Unable to create JPEG Process 4 codec for bits stored == {0}", bits));
	}
};

[DicomCodec]
public ref class DcmJpegLossless14Codec : public DcmJpegCodec {
public:
	virtual DicomTransferSyntax^ GetTransferSyntax() override {
		return DicomTransferSyntax::JPEGProcess14;
	}

	virtual IJpegCodec^ GetCodec(int bits, DcmJpegParameters^ jparams) override {
		if (bits <= 8)
			return gcnew Jpeg8Codec(JpegMode::Lossless, jparams->Predictor, jparams->PointTransform);
		else if (bits <= 12)
			return gcnew Jpeg12Codec(JpegMode::Lossless, jparams->Predictor, jparams->PointTransform);
		else if (bits <= 16)
			return gcnew Jpeg16Codec(JpegMode::Lossless, jparams->Predictor, jparams->PointTransform);
		else
			throw gcnew DicomCodecException(String::Format("Unable to create JPEG Process 14 codec for bits stored == {0}", bits));
	}
};

[DicomCodec]
public ref class DcmJpegLossless14SV1Codec : public DcmJpegCodec {
public:
	virtual DicomTransferSyntax^ GetTransferSyntax() override {
		return DicomTransferSyntax::JPEGProcess14SV1;
	}

	virtual IJpegCodec^ GetCodec(int bits, DcmJpegParameters^ jparams) override {
		if (bits <= 8)
			return gcnew Jpeg8Codec(JpegMode::Lossless, 1, jparams->PointTransform);
		else if (bits <= 12)
			return gcnew Jpeg12Codec(JpegMode::Lossless, 1, jparams->PointTransform);
		else if (bits <= 16)
			return gcnew Jpeg16Codec(JpegMode::Lossless, 1, jparams->PointTransform);
		else
			throw gcnew DicomCodecException(String::Format("Unable to create JPEG Process 14 [SV1] codec for bits stored == {0}", bits));
	}
};

} // Jpeg
} // Codec
} // Dicom

#endif