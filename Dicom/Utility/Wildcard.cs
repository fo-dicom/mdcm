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
//
// Note:  This file may contain code using a license that has not been 
//        verified to be compatible with the licensing of this software.  
//
// References:
//     * Based on version by robagar in messages:
//       http://www.codeproject.com/string/wildcmp.asp

using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Utility {
	public static class Wildcard {
		/// <summary>
		/// Array of valid wildcards
		/// </summary>
		private static char[] Wildcards = new char[] { '*', '?' };

		/// <summary>
		/// Returns true if the string matches the pattern which may contain * and ? wildcards.
		/// Matching is done without regard to case.
		/// </summary>
		/// <param name="pattern"></param>
		/// <param name="s"></param>
		/// <returns></returns>
		public static bool Match(string pattern, string s) {
			return Match(pattern, s, false);
		}

		/// <summary>
		/// Returns true if the string matches the pattern which may contain * and ? wildcards.
		/// </summary>
		/// <param name="pattern"></param>
		/// <param name="s"></param>
		/// <param name="caseSensitive"></param>
		/// <returns></returns>
		public static bool Match(string pattern, string s, bool caseSensitive) {
			// if not concerned about case, convert both string and pattern
			// to lower case for comparison
			if (!caseSensitive) {
				pattern = pattern.ToLower();
				s = s.ToLower();
			}

			// if pattern doesn't actually contain any wildcards, use simple equality
			if (pattern.IndexOfAny(Wildcards) == -1)
				return (s == pattern);

			// otherwise do pattern matching
			int i = 0;
			int j = 0;
			while (i < s.Length && j < pattern.Length && pattern[j] != '*') {
				if ((pattern[j] != s[i]) && (pattern[j] != '?')) {
					return false;
				}
				i++;
				j++;
			}

			// if we have reached the end of the pattern without finding a * wildcard,
			// the match must fail if the string is longer or shorter than the pattern
			if (j == pattern.Length)
				return s.Length == pattern.Length;

			int cp = 0;
			int mp = 0;
			while (i < s.Length) {
				if (j < pattern.Length && pattern[j] == '*') {
					if ((j++) >= pattern.Length) {
						return true;
					}
					mp = j;
					cp = i + 1;
				} else if (j < pattern.Length && (pattern[j] == s[i] || pattern[j] == '?')) {
					j++;
					i++;
				} else {
					j = mp;
					i = cp++;
				}
			}

			while (j < pattern.Length && pattern[j] == '*') {
				j++;
			}

			return j >= pattern.Length;
		}
	}
}
