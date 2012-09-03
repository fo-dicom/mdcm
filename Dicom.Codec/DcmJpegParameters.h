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

#ifndef __DCMJPEGPARAMETERS_H__
#define __DCMJPEGPARAMETERS_H__

#pragma once

using namespace System;
using namespace System::IO;

using namespace Dicom::Codec;
using namespace Dicom::Data;

#include "JpegCodec.h"

namespace Dicom {
namespace Codec {
namespace Jpeg {

public enum class JpegSampleFactor {
	SF444,
	SF422,
	Unknown
};

public ref class DcmJpegParameters : public DcmCodecParameters {
private:
	int _quality;
	int _smoothing;
	bool _convertColorspace;
	JpegSampleFactor _sample;
	int _predictor;
	int _pointTransform;

public:
	DcmJpegParameters() {
		_quality = 90;
		_smoothing = 0;
		_convertColorspace = false;
		_sample = JpegSampleFactor::SF444;
		_predictor = 1;
		_pointTransform = 0;
	}

	property int Quality {
		int get() { return _quality; }
		void set(int value) { _quality = value; }
	}

	property int SmoothingFactor {
		int get() { return _smoothing; }
		void set(int value) { _smoothing = value; }
	}

	property bool ConvertColorspaceToRGB {
		bool get() { return _convertColorspace; }
		void set(bool value) { _convertColorspace = value; }
	}

	property JpegSampleFactor SampleFactor {
		JpegSampleFactor get() { return _sample; }
		void set(JpegSampleFactor value) { _sample = value; }
	}

	property int Predictor {
		int get() { return _predictor; }
		void set(int value) { _predictor = value; }
	}

	property int PointTransform {
		int get() { return _pointTransform; }
		void set(int value) { _pointTransform = value; }
	}
};

} // Jpeg
} // Codec
} // Dicom

#endif