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

using Dicom.Data;

namespace Dicom.Imaging {
	/// <summary>
	/// Window/Level
	/// </summary>
	public class WindowLevel {
		#region Private Members
		private readonly string _description;
		private readonly double _window;
		private readonly double _level;
		#endregion

		#region Public Constructors
		public WindowLevel(double window, double level) {
			_window = window;
			_level = level;
			_description = String.Empty;
		}

		public WindowLevel(string description, double window, double level) {
			_window = window;
			_level = level;
			_description = description;
		}
		#endregion

		#region Public Properties
		public string Description {
			get { return _description; }
		}

		public double Window {
			get { return _window; }
		}

		public double Level {
			get { return _level; }
		}
		#endregion

		#region Public Methods
		public override string ToString() {
			if (String.IsNullOrEmpty(Description))
				return String.Format("{0}:{1} No Description", Window, Level);
			else
				return String.Format("{0}:{1} {2}", Window, Level, Description);
		}
		#endregion

		#region Static Methods
		public static WindowLevel[] FromDataset(DcmDataset dataset) {
			List<WindowLevel> settings = new List<WindowLevel>();

			if (dataset.Contains(DicomTags.WindowCenter) && dataset.Contains(DicomTags.WindowWidth)) {
				string[] wc = dataset.GetDS(DicomTags.WindowCenter).GetValues();
				string[] ww = dataset.GetDS(DicomTags.WindowWidth).GetValues();

				if (wc.Length != ww.Length)
					throw new DicomImagingException("Window Center count does not match Window Width count");

				string[] desc = null;
				if (dataset.Contains(DicomTags.WindowCenterWidthExplanation)) {
					desc = dataset.GetLO(DicomTags.WindowCenterWidthExplanation).GetValues();
				}

				for (int i = 0; i < wc.Length; i++) {
					double window;
					double level;
					if (!Double.TryParse(ww[i], out window) || !Double.TryParse(wc[i], out level))
						throw new DicomImagingException("Unable to parse Window/Level [wc: {0}; ww: {1}]", wc[i], ww[i]);

					string description = String.Empty;
					if (desc != null && i < desc.Length)
						description = desc[i];

					settings.Add(new WindowLevel(description, window, level));
				}
			}

			return settings.ToArray();
		}
		#endregion
	}
}
