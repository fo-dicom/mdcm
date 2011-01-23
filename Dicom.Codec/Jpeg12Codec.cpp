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

#include <vector>
#include <vcclr.h>

using namespace System;
using namespace System::IO;
using namespace System::Runtime::InteropServices;

using namespace Dicom::Data;

#include "JpegCodec.h"

#define IJGVERS IJG12
#define JPEGCODEC Jpeg12Codec

namespace Dicom {
namespace Codec {
namespace Jpeg {

extern "C" {
#define boolean ijg_boolean
#include "stdio.h"
#include "string.h"
#include "setjmp.h"
#include "libijg12/jpeglib12.h"
#include "libijg12/jerror12.h"
#include "libijg12/jpegint12.h"
#undef boolean

// disable any preprocessor magic the IJG library might be doing with the "const" keyword
#ifdef const
#undef const
#endif
} // extern "C"

#include "JpegCodec.i"

} // Jpeg
} // Codec
} // Dicom