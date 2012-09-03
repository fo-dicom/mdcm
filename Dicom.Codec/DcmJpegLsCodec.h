// mDCM: A C# DICOM library
//
// Copyright (c) 2010  Colby Dillion
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

#ifndef __DCMJPEGLSCODEC_H__
#define __DCMJPEGLSCODEC_H__

#pragma once

using namespace System;

using namespace Dicom::Data;
using namespace Dicom::Codec;

namespace Dicom {
namespace Codec {
namespace JpegLs {
	public enum class DcmJpegLsInterleaveMode {
		None = 0,
		Line = 1,
		Sample = 2
	};

	public enum class DcmJpegLsColorTransform {
		None = 0,
		HP1 = 1,
		HP2 = 2,
		HP3 = 3
	};

	public ref class DcmJpegLsParameters : public DcmCodecParameters {
	private:
		int _allowedError;
		DcmJpegLsInterleaveMode _ilMode;
		DcmJpegLsColorTransform _colorTransform;

	public:
		DcmJpegLsParameters() {
			_allowedError = 3;
			_ilMode = DcmJpegLsInterleaveMode::Line;
			_colorTransform = DcmJpegLsColorTransform::HP1;
		}

		property int AllowedError {
			int get() { return _allowedError; }
			void set(int value) { _allowedError = value; }
		}

		property DcmJpegLsInterleaveMode InterleaveMode {
			DcmJpegLsInterleaveMode get() { return _ilMode; }
			void set(DcmJpegLsInterleaveMode value) { _ilMode = value; }
		}

		property DcmJpegLsColorTransform ColorTransform {
			DcmJpegLsColorTransform get() { return _colorTransform; }
			void set(DcmJpegLsColorTransform value) { _colorTransform = value; }
		}
	};


	public ref class DcmJpegLsCodec abstract : public IDcmCodec
	{
	public:
		virtual String^ GetName() {
			return GetTransferSyntax()->UID->Description;
		}

		virtual DicomTransferSyntax^ GetTransferSyntax() = 0;

		virtual DcmCodecParameters^ GetDefaultParameters() {
			return gcnew DcmJpegLsParameters();
		}

		virtual void Encode(DcmDataset^ dataset, DcmPixelData^ oldPixelData, DcmPixelData^ newPixelData, DcmCodecParameters^ parameters);
		virtual void Decode(DcmDataset^ dataset, DcmPixelData^ oldPixelData, DcmPixelData^ newPixelData, DcmCodecParameters^ parameters);

		static void Register();
	};

	[DicomCodec]
	public ref class DcmJpegLsNearLosslessCodec : public DcmJpegLsCodec
	{
	public:
		virtual DicomTransferSyntax^ GetTransferSyntax() override {
			return DicomTransferSyntax::JPEGLSNearLossless;
		}
	};

	[DicomCodec]
	public ref class DcmJpegLsLosslessCodec : public DcmJpegLsCodec
	{
	public:
		virtual DicomTransferSyntax^ GetTransferSyntax() override {
			return DicomTransferSyntax::JPEGLSLossless;
		}
	};

} // JpegLs
} // Codec
} // Dicom

#endif