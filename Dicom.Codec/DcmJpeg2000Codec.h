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

#ifndef __DCMJPEG2000CODEC_H__
#define __DCMJPEG2000CODEC_H__

#pragma once

using namespace System;

using namespace Dicom::Data;
using namespace Dicom::Codec;

namespace Dicom {
namespace Codec {
namespace Jpeg2000 {

	public ref class DcmJpeg2000Parameters : public DcmCodecParameters {
	private:
		bool _irreversible;
		int _rate;
		array<int>^ _rates;
		bool _isVerbose;
		bool _enableMct;
		bool _updatePmi;
		bool _signedAsUnsigned;

	public:
		DcmJpeg2000Parameters() {
			_irreversible = true;
			_rate = 20;
			_isVerbose = false;
			_enableMct = true;
			_updatePmi = true;
			_signedAsUnsigned = true;

			_rates = gcnew array<int>(9);
			_rates[0] = 1280;
			_rates[1] = 640;
			_rates[2] = 320;
			_rates[3] = 160;
			_rates[4] = 80;
			_rates[5] = 40;
			_rates[6] = 20;
			_rates[7] = 10;
			_rates[8] = 5;
		}

		property bool Irreversible {
			bool get() { return _irreversible; }
			void set(bool value) { _irreversible = value; }
		}

		property int Rate {
			int get() { return _rate; }
			void set(int value) { _rate = value; }
		}

		property array<int>^ RateLevels {
			array<int>^ get() { return _rates; }
			void set(array<int>^ value) { _rates = value; }
		}

		property bool IsVerbose {
			bool get() { return _isVerbose; }
			void set(bool value) { _isVerbose = value; }
		}

		property bool AllowMCT {
			bool get() { return _enableMct; }
			void set(bool value) { _enableMct = value; }
		}

		property bool UpdatePhotometricInterpretation {
			bool get() { return _updatePmi; }
			void set(bool value) { _updatePmi = value; }
		}

		property bool EncodeSignedPixelValuesAsUnsigned {
			bool get() { return _signedAsUnsigned; }
			void set(bool value) { _signedAsUnsigned = value; }
		}
	};


	public ref class DcmJpeg2000Codec abstract : public IDcmCodec
	{
	public:
		virtual String^ GetName() {
			return GetTransferSyntax()->UID->Description;
		}

		virtual DicomTransferSyntax^ GetTransferSyntax() = 0;

		virtual DcmCodecParameters^ GetDefaultParameters() {
			return gcnew DcmJpeg2000Parameters();
		}

		virtual void Encode(DcmDataset^ dataset, DcmPixelData^ oldPixelData, DcmPixelData^ newPixelData, DcmCodecParameters^ parameters);
		virtual void Decode(DcmDataset^ dataset, DcmPixelData^ oldPixelData, DcmPixelData^ newPixelData, DcmCodecParameters^ parameters);

		static void Register();
	};

	[DicomCodec]
	public ref class DcmJpeg2000LossyCodec : public DcmJpeg2000Codec
	{
	public:
		virtual DicomTransferSyntax^ GetTransferSyntax() override {
			return DicomTransferSyntax::JPEG2000Lossy;
		}
	};

	[DicomCodec]
	public ref class DcmJpeg2000LosslessCodec : public DcmJpeg2000Codec
	{
	public:
		virtual DicomTransferSyntax^ GetTransferSyntax() override {
			return DicomTransferSyntax::JPEG2000Lossless;
		}
	};

} // Jpeg2000
} // Codec
} // Dicom

#endif