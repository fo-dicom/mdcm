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
//     http://aurora.regenstrief.org/xhl7/

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Dicom.Utility;

namespace Dicom.HL7 {
	public static class XHL7 {
		private const string DEFAULT_DELIMITERS = "|^~\\&";
		private const string DELIMITER_ESCAPES = "FSRET";
		private const char SEGMENT_DELIMITER = '\r';

		private const int N_DEL_FIELD = 0;
		private const int N_DEL_COMPONENT = 1;
		private const int N_DEL_REPEAT = 2;
		private const int N_DEL_ESCAPE = 3;
		private const int N_DEL_SUBCOMPONENT = 4;
		private const int NUMBER_OF_DELIMITERS = 5;

		private const string NAMESPACE_URI = "";
		private const string TAG_ROOT = "hl7";
		private const string ATT_DEL_FIELD = "fieldDelimiter";
		private const string ATT_DEL_COMPONENT = "componentDelimiter";
		private const string ATT_DEL_REPEAT = "repeatDelimiter";
		private const string ATT_DEL_ESCAPE = "escapeDelimiter";
		private const string ATT_DEL_SUBCOMPONENT = "subcomponentDelimiter";
		private const string TAG_FIELD = "field";
		private const string TAG_COMPONENT = "component";
		private const string TAG_REPEAT = "repeat";
		private const string TAG_ESCAPE = "escape";
		private const string TAG_SUBCOMPONENT = "subcomponent";
		private const string CDATA = "CDATA";

		public static XDocument ToXML(string hl7) {
			XDocument document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
			XElement root = new XElement(TAG_ROOT);

			string delimiters = DEFAULT_DELIMITERS;

			string line = null;
			StringReader reader = new StringReader(hl7);
			while ((line = reader.ReadLine()) != null) {
				line = line.Trim();
				if (line == String.Empty)
					continue;

				XElement segmentEl = null;
				string segmentTag = line.Substring(0, 3);

				bool isControlSegment = false;
				if (segmentTag == "FHS" || segmentTag == "BHS" || segmentTag == "MSH") {
					isControlSegment = true;
					delimiters = line.Substring(3, 8);
					segmentEl = new XElement(segmentTag,
									new XAttribute(ATT_DEL_FIELD, delimiters[N_DEL_FIELD]),
									new XAttribute(ATT_DEL_COMPONENT, delimiters[N_DEL_COMPONENT]),
									new XAttribute(ATT_DEL_REPEAT, delimiters[N_DEL_REPEAT]),
									new XAttribute(ATT_DEL_ESCAPE, delimiters[N_DEL_ESCAPE]),
									new XAttribute(ATT_DEL_SUBCOMPONENT, delimiters[N_DEL_SUBCOMPONENT]));
				}
				else {
					segmentEl = new XElement(segmentTag);
				}

				bool firstField = true;
				foreach (string field in line.Split(delimiters[N_DEL_FIELD])) {
					if (firstField) {
						if (isControlSegment) {
							isControlSegment = false;
							continue;
						}
						firstField = false;
						continue;
					}

					XElement fieldEl = new XElement(TAG_FIELD);

					string[] components = field.Split(delimiters[N_DEL_COMPONENT]);
					for (int c = 0; c < components.Length; c++) {
						XElement componentEl = fieldEl;
						if (c > 0) {
							componentEl = new XElement(TAG_COMPONENT);
							fieldEl.Add(componentEl);
						}

						string[] subcomponents = components[c].Split(delimiters[N_DEL_SUBCOMPONENT]);
						for (int s = 0; s < subcomponents.Length; s++) {
							XElement subcomponentEl = componentEl;
							if (s > 0) {
								subcomponentEl = new XElement(TAG_SUBCOMPONENT);
								componentEl.Add(subcomponentEl);
							}
							subcomponentEl.Add(subcomponents[s]);
						}
					}

					segmentEl.Add(fieldEl);
				}

				root.Add(segmentEl);
			}

			document.Add(root);
			return document;
		}

		public static string ToHL7(XDocument document) {
			StringBuilder hl7 = new StringBuilder();
			char[] delimiters = DEFAULT_DELIMITERS.ToCharArray();

			foreach (XElement segmentEl in document.Root.Nodes()) {
				string segmentTag = segmentEl.Name.LocalName;
				hl7.Append(segmentTag);

				if (segmentTag == "FHS" || segmentTag == "BHS" || segmentTag == "MSH") {
					foreach (XAttribute attr in segmentEl.Attributes()) {
						string name = attr.Name.LocalName;
						string value = attr.Value;

						if (value == String.Empty)
							continue;

						if (name == ATT_DEL_FIELD)
							delimiters[N_DEL_FIELD] = value[0];
						else if (name == ATT_DEL_COMPONENT)
							delimiters[N_DEL_COMPONENT] = value[0];
						else if (name == ATT_DEL_REPEAT)
							delimiters[N_DEL_REPEAT] = value[0];
						else if (name == ATT_DEL_ESCAPE)
							delimiters[N_DEL_ESCAPE] = value[0];
						else if (name == ATT_DEL_SUBCOMPONENT)
							delimiters[N_DEL_SUBCOMPONENT] = value[0];
					}

					hl7.Append(delimiters);
				}

				foreach (XElement fieldEl in segmentEl.Nodes()) {
					hl7.Append(delimiters[N_DEL_FIELD]).Append(fieldEl.FirstText());
					foreach (XElement componentEl in fieldEl.Elements(TAG_COMPONENT)) {
						hl7.Append(delimiters[N_DEL_COMPONENT]).Append(componentEl.FirstText());
						foreach (XElement subcomponentEl in componentEl.Elements(TAG_SUBCOMPONENT)) {
							hl7.Append(delimiters[N_DEL_SUBCOMPONENT]).Append(subcomponentEl.FirstText());
						}
					}
				}

				hl7.Append(SEGMENT_DELIMITER);
			}

			return hl7.ToString();
		}

		public static string ExtractReport(XDocument document) {
			if (document.Root.Name.LocalName != TAG_ROOT)
				return String.Empty;

			StringBuilder sb = new StringBuilder();

			foreach (XElement obx in document.Root.Elements("OBX")) {
				XElement obs = obx.Elements(TAG_FIELD).ElementAt(4);
				sb.AppendLine(obs.FirstText());
			}

			return sb.ToString();
		}
	}
}