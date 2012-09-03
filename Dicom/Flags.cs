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
using System.Text;

using Dicom.Data;
using Dicom.IO;

namespace Dicom {
	public static class Flags {
		public static bool IsSet(DicomDumpOptions options, DicomDumpOptions flag) {
			return (options & flag) == flag;
		}
		public static bool IsSet(DicomReadOptions options, DicomReadOptions flag) {
			return (options & flag) == flag;
		}
		public static bool IsSet(DicomWriteOptions options, DicomWriteOptions flag) {
			return (options & flag) == flag;
		}
		public static bool IsSet(XDicomOptions options, XDicomOptions flag) {
			return (options & flag) == flag;
		}
	}

	[Flags]
	public enum DicomDumpOptions : int {
		None = 0x00000000,
		ShortenLongValues = 0x00000001,
		Restrict80CharactersPerLine = 0x00000002,
		KeepGroupLengthElements = 0x00000004,
		Default = DicomDumpOptions.ShortenLongValues | DicomDumpOptions.Restrict80CharactersPerLine
	}

	[Flags]
	public enum DicomReadOptions : int {
		None = 0x00000000,
		KeepGroupLengths = 0x00000001,
		UseDictionaryForExplicitUN = 0x00000002,
		AllowSeekingForContext = 0x00000004,
		DeferLoadingLargeElements = 0x00000008,
		DeferLoadingPixelData = 0x00000010,
		ForcePrivateCreatorToLO = 0x00000020,
		FileMetaInfoOnly = 0x00000040,
		SequenceItemOnly = 0x00000080,
		Default = DicomReadOptions.UseDictionaryForExplicitUN | 
				  DicomReadOptions.AllowSeekingForContext | 
				  DicomReadOptions.DeferLoadingLargeElements | 
				  DicomReadOptions.DeferLoadingPixelData |
				  DicomReadOptions.ForcePrivateCreatorToLO,
		DefaultWithoutDeferredLoading = DicomReadOptions.UseDictionaryForExplicitUN |
				  DicomReadOptions.AllowSeekingForContext |
				  DicomReadOptions.ForcePrivateCreatorToLO
	}

	[Flags]
	public enum DicomWriteOptions : int {
		None = 0x00000000,
		CalculateGroupLengths = 0x00000001,
		ExplicitLengthSequence = 0x00000002,
		ExplicitLengthSequenceItem = 0x00000004,
		WriteFragmentOffsetTable = 0x00000008,
		Default = DicomWriteOptions.CalculateGroupLengths | DicomWriteOptions.WriteFragmentOffsetTable
	}

	[Flags]
	public enum XDicomOptions : int {
		None = 0x00000000,
		Comments = 0x00000001,
		IncludePixelData = 0x00000002,
		Default = XDicomOptions.Comments
	}
}
