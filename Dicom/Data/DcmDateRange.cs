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

namespace Dicom.Data {
	/// <summary>
	/// DICOM date range
	/// </summary>
	public class DcmDateRange {
		#region Private Members
		private DateTime _dtBegin;
		private DateTime _dtEnd;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="DcmDateRange"/> class.
		/// </summary>
		public DcmDateRange() {
			_dtBegin = DateTime.MinValue;
			_dtEnd = DateTime.MinValue;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DcmDateRange"/> class.
		/// </summary>
		/// <param name="dt">Start and end date</param>
		public DcmDateRange(DateTime dt) {
			_dtBegin = _dtEnd = dt;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DcmDateRange"/> class.
		/// </summary>
		/// <param name="begin">Start date</param>
		/// <param name="end">End date</param>
		public DcmDateRange(DateTime begin, DateTime end) {
			_dtBegin = begin;
			_dtEnd = end;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DcmDateRange"/> class.
		/// </summary>
		/// <param name="range">Date range</param>
		public DcmDateRange(DateTime[] range) {
			if (range.Length == 1) {
				DateTime = range[0];
				return;
			}
			if (range.Length == 2) {
				Begin = range[0];
				End = range[1];
				return;
			}
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets the date time.
		/// </summary>
		/// <value>The date time.</value>
		public DateTime DateTime {
			get {
				_dtEnd = _dtBegin;
				return _dtBegin;
			}
			set {
				_dtBegin = _dtEnd = value;
			}
		}

		/// <summary>
		/// Gets or sets the start date.
		/// </summary>
		/// <value>Start date</value>
		public DateTime Begin {
			get { return _dtBegin; }
			set { _dtBegin = value; }
		}

		/// <summary>
		/// Gets or sets the end date.
		/// </summary>
		/// <value>End date</value>
		public DateTime End {
			get { return _dtEnd; }
			set { _dtEnd = value; }
		}
		#endregion

		#region Public Members
		/// <summary>
		/// Gets date range as formatted string.
		/// </summary>
		/// <param name="format">DateTime format</param>
		/// <returns></returns>
		public string ToString(string format) {
			if ((Begin == DateTime.MinValue || Begin == DateTime.MaxValue) &&
				(End == DateTime.MinValue || End == DateTime.MaxValue)) {
				return "";
			}
			//if (Begin == End) {
			//    return Begin.ToString(format);
			//}
			if (Begin == DateTime.MinValue || Begin == DateTime.MaxValue) {
				return String.Format("-{0}", End.ToString(format));
			}
			if (End == DateTime.MinValue || End == DateTime.MaxValue) {
				return String.Format("{0}-", Begin.ToString(format));
			}
			return String.Format("{0}-{1}", Begin.ToString(format), End.ToString(format));
		}
		#endregion
	}
}
