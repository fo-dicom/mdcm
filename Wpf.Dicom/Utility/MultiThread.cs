// mDCM: A C# DICOM library
//
// Copyright (c) 2006-2009  Colby Dillion
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
using System.Threading.Tasks;

namespace Dicom.Utility {
	public static class MultiThread {

		public static void For(int start, int end, Action<int> action)
		{
			Parallel.For(start, end, action);
		}

		public static void For(int start, int end, int chunkSize, Action<int> action)
		{
			var options = new ParallelOptions { MaxDegreeOfParallelism = chunkSize };
			Parallel.For(start, end, options, action);
		}

		public static void ForEach<T>(IEnumerable<T> enumerable, Action<T> action)
		{
			Parallel.ForEach(enumerable, action);
		}
	}
}
