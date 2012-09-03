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
using System.Globalization;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Utility {
	public static class Format {
		public static string Percent(int numerator, int denominator) {
			if (denominator == 0)
				return "--%";
			return String.Format("{0}%", (int)(((double)numerator/(double)denominator) * 100));
		}

		public static string ByteCount(uint bytes) {
			if (bytes > 1073741824)
				return String.Format("{0:0.00} GB", bytes / 1073741824.0);
			if (bytes > 1048576)
				return String.Format("{0:0.00} MB", bytes / 1048576.0);
			if (bytes > 1024)
				return String.Format("{0:0.00} KB", bytes / 1024.0);
			return String.Format("{0:0} B", (double)bytes);
		}

		public static string ByteCount(long bytes) {
			if (bytes > 1099511627776L)
				return String.Format("{0:0.00} TB", (double)bytes / 1099511627776.0);
			if (bytes > 1073741824L)
				return String.Format("{0:0.00} GB", (double)bytes / 1073741824.0);
			if (bytes > 1048576L)
				return String.Format("{0:0.00} MB", (double)bytes / 1048576.0);
			if (bytes > 1024L)
				return String.Format("{0:0.00} KB", (double)bytes / 1024.0);
			return String.Format("{0} B", bytes);
		}

		public static string ByteCount(double bytes) {
			if (bytes > 1099511627776.0)
				return String.Format("{0:0.00} TB", bytes / 1099511627776.0);
			if (bytes > 1073741824.0)
				return String.Format("{0:0.00} GB", bytes / 1073741824.0);
			if (bytes > 1048576.0)
				return String.Format("{0:0.00} MB", bytes / 1048576.0);
			if (bytes > 1024.0)
				return String.Format("{0:0.00} KB", bytes / 1024.0);
			return String.Format("{0:0} B", bytes);
		}

		public static string AddSpaces(string str) {
			if (str.Length <= 2)
				return str;
			StringBuilder sb = new StringBuilder();
			sb.Append(str[0]);
			int count = str.Length - 1;
			for (int i = 1; i < count; i++) {
				bool prevUpper = Char.IsUpper(str[i - 1]);
				bool nextLower = Char.IsLower(str[i + 1]);
				bool currUpper = Char.IsUpper(str[i]);

				if (prevUpper && currUpper && nextLower)
					sb.Append(' ');
				else if (!prevUpper && currUpper)
					sb.Append(' ');
				sb.Append(str[i]);
			}
			sb.Append(str[str.Length - 1]);
			return sb.ToString();
		}

		public static string CamelCase(string str) {
			bool nextUpper = true;
			string camel = String.Empty;
			foreach (char c in str) {
				if (Char.IsLetterOrDigit(c)) {
					if (nextUpper) {
						camel += Char.ToUpper(c);
						nextUpper = false;
					} else
						camel += c;
				} else if (c == ' ')
					nextUpper = true;
			}
			return camel;
		}
	}
}
