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
using System.IO;
using System.Security;
using System.Text;

namespace Dicom.Data {
	public class DicomTemplateAdapter {
		private static Dictionary<string, DicomTag> _map;

		static DicomTemplateAdapter() {
			_map = new Dictionary<string, DicomTag>();
			_map.Add("StudyUID", DicomTags.StudyInstanceUID);
			_map.Add("SeriesUID", DicomTags.SeriesInstanceUID);
			_map.Add("ImageUID", DicomTags.SOPInstanceUID);
			_map.Add("PatientID", DicomTags.PatientID);
			_map.Add("PatientName", DicomTags.PatientsName);
			_map.Add("PatientSex", DicomTags.PatientsSex);
			_map.Add("Modality", DicomTags.Modality);
			_map.Add("Description", DicomTags.StudyDescription);
			_map.Add("BodyPart", DicomTags.BodyPartExamined);
			_map.Add("StudyID", DicomTags.StudyID);
			_map.Add("AccessionNumber", DicomTags.AccessionNumber);
			_map.Add("StudyDate", DicomTags.StudyDate);
			_map.Add("StudyTime", DicomTags.StudyTime);
		}

		private DcmDataset _dataset;
		private bool _xmlEscape;

		public DicomTemplateAdapter(DcmDataset dataset) {
			_dataset = dataset;
			_xmlEscape = false;
		}

		public DicomTemplateAdapter(DcmDataset dataset, bool xmlEscape) {
			_dataset = dataset;
			_xmlEscape = xmlEscape;
		}

		public object this[string index] {
			get {
				if (_dataset == null)
					return String.Empty;

				DicomTag tag = null;
				if (!_map.TryGetValue(index, out tag)) {
					tag = DicomTag.Parse(index);
				}

				if (tag != null) {
					DcmElement elem = _dataset.GetElement(tag);
					if (elem.Length == 0)
						return String.Empty;
					if (elem != null) {
						object o = elem.GetValueObject();
#if !SILVERLIGHT
						if (_xmlEscape && o is string)
							return SecurityElement.Escape((string)o);
						else
#endif
                            return o;
					}
				}

				return String.Empty;
			}
		}

		public static string FixStringTemplateLines(string template) {
			StringBuilder sb = new StringBuilder();
			StringReader reader = new StringReader(template);
			for (; ; ) {
				string line = reader.ReadLine();
				if (line == null)
					break;
				sb.Append(line).AppendLine();//.AppendLine("$\\n$");
			}
			return sb.ToString();
		}
	}
}
