using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Dicom.HL7 {
	public class HL7v2 {
		public const string ClearValue = "\"\"";
		public const string DateFormat = "yyyyMMdd";
		public const string TimestampFormat = "yyyyMMddHHmmss";

		#region Tag
		public class Tag {
			public string Segment;
			public int SegmentNumber;
			public int Field;
			public int Component;
			public int SubComponent;

			public Tag() {
				Segment = "???";
				SegmentNumber = 1;
			}

			public Tag(string tag) {
				try {
					Segment = tag.Substring(0, 3).ToUpper();

					if (tag[3] == '[') {
						string sn = tag.Substring(4, tag.IndexOf(']') - 4);
						SegmentNumber = Int32.Parse(sn);
					} else {
						SegmentNumber = 1;
					}

					string[] parts = tag.Split('.');
					if (parts.Length > 4)
						throw new Exception();

					if (parts.Length >= 2)
						Field = Int32.Parse(parts[1]);
					if (parts.Length >= 3)
						Component = Int32.Parse(parts[2]);
					if (parts.Length == 4)
						SubComponent = Int32.Parse(parts[3]);
				} catch {
					throw new ArgumentException("Invalid HL7 tag: " + tag, "tag");
				}
			}

			public override string ToString() {
				string tag = Segment;
				if (SegmentNumber > 1)
					tag += "[" + SegmentNumber.ToString() + "]";
				if (Field > 0)
					tag += "." + Field.ToString();
				if (Component > 0)
					tag += "." + Component.ToString();
				if (SubComponent > 0)
					tag += "." + SubComponent.ToString();
				return tag;
			}
		}
		#endregion

		#region Private Members
		private List<List<string>> _segments;
		private char _field_delim;
		private char _component_delim;
		private char _subcomponent_delim;
		#endregion

		#region Public Constructors
		public HL7v2() {
			_segments = new List<List<string>>();

			_field_delim = '|';
			_component_delim = '^';
			_subcomponent_delim = '&';
		}
		#endregion

		#region Public Properties
		public string this[string tag] {
			get { return Get(tag, null); }
			set { Set(tag, value); }
		}

		public string this[Tag tag] {
			get { return Get(tag, null); }
			set { Set(tag, value); }
		}
		#endregion

		#region Private Methods
		private List<string> GetSegment(Tag tag) {
			int number = Math.Max(1, tag.SegmentNumber);
			foreach (List<string> segment in _segments) {
				if (segment[0] == tag.Segment) {
					if (--number == 0)
						return segment;
				}
			}
			return null;
		}

		private string GetField(Tag tag) {
			List<string> segment = GetSegment(tag);
			if (segment != null) {
				if (tag.Field < segment.Count)
					return segment[tag.Field];
			}
			return null;
		}
		private void SetField(Tag tag, string value) {
			List<string> segment = GetSegment(tag);
			if (segment == null) {
				segment = new List<string>();
				segment.Add(tag.Segment);
				if (IsControlSegment(tag.Segment)) {
					segment.Add("|");
					segment.Add("^~\\&");
				}
				_segments.Add(segment);
			}

			while (segment.Count <= tag.Field)
				segment.Add(String.Empty);
			segment[tag.Field] = value;

			if (IsControlSegment(tag.Segment)) {
				if (tag.Field == 1)
					_field_delim = value[0];
				else if (tag.Field == 2) {
					_component_delim = value[0];
					_subcomponent_delim = value[3];
				}
			}
		}

		private string GetComponent(Tag tag) {
			string field = GetField(tag);
			if (field == null)
				return null;

			string[] components = field.Split(_component_delim);
			if (tag.Component <= components.Length)
				return components[tag.Component - 1];

			return null;
		}
		private void SetComponent(Tag tag, string value) {
			string field = GetField(tag);
			if (field == null)
				field = String.Empty;

			List<string> components = new List<string>(field.Split(_component_delim));
			while (components.Count < tag.Component)
				components.Add(String.Empty);
			components[tag.Component - 1] = value;

			SetField(tag, String.Join(new string(new char[] { _component_delim }), components.ToArray()));
		}

		private string GetSubComponent(Tag tag) {
			string component = GetComponent(tag);
			if (component == null)
				return null;

			string[] subs = component.Split(_subcomponent_delim);
			if (tag.SubComponent <= subs.Length)
				return subs[tag.SubComponent - 1];

			return null;
		}
		private void SetSubComponent(Tag tag, string value) {
			string component = GetComponent(tag);
			if (component == null)
				component = String.Empty;

			List<string> subs = new List<string>(component.Split(_subcomponent_delim));
			while (subs.Count < tag.SubComponent)
				subs.Add(String.Empty);
			subs[tag.SubComponent - 1] = value;

			SetComponent(tag, String.Join(new string(new char[] { _subcomponent_delim }), subs.ToArray()));
		}
		#endregion

		#region Public Methods
		public int Count(string tag) {
			return Count(new Tag(tag));
		}
		public int Count(Tag tag) {
			if (tag.SubComponent > 0) {
				throw new ArgumentException("Invalid HL7 tag for Count() operation: " + tag.ToString(), "tag");
			} else if (tag.Component > 0) {
				// number of subcomponents
				string component = GetComponent(tag);
				if (!String.IsNullOrEmpty(component))
					return component.Split(_subcomponent_delim).Length;
			} else if (tag.Field > 0) {
				// number of componenets
				string field = GetField(tag);
				if (!String.IsNullOrEmpty(field))
					return field.Split(_component_delim).Length;
			} else {
				// number of fields
				List<string> segment = GetSegment(tag);
				if (segment != null)
					return segment.Count - 1;
			}
			return 0;
		}

		public IEnumerable<Tag> GetSegments() {
			Dictionary<string, int> sCount = new Dictionary<string, int>();
			foreach (List<string> segment in _segments) {
				Tag tag = new Tag();
				tag.Segment = segment[0];

				sCount.TryGetValue(tag.Segment, out tag.SegmentNumber);
				sCount[tag.Segment] = ++tag.SegmentNumber;

				yield return tag;
			}
		}
		public IEnumerable<Tag> GetSegments(string id) {
			int n = 0;
			foreach (List<string> segment in _segments) {
				if (segment[0] != id)
					continue;

				Tag tag = new Tag();
				tag.Segment = id;
				tag.SegmentNumber = ++n;

				yield return tag;
			}
		}

		public int CountSegments(string id) {
			int n = 0;
			foreach (List<string> segment in _segments) {
				if (segment[0] == id)
					n++;
			}
			return n;
		}

		public string Get(string tag, string defaultValue) {
			return Get(new Tag(tag), defaultValue);
		}
		public string Get(Tag tag, string defaultValue) {
			string value = null;
			if (tag.SubComponent > 0)
				value = GetSubComponent(tag);
			else if (tag.Component > 0)
				value = GetComponent(tag);
			else if (tag.Field > 0)
				value = GetField(tag);
			else
				throw new ArgumentException("Invalid HL7 tag for Get() operation: " + tag.ToString(), "tag");
			return (value != null) ? value : defaultValue;
		}

		public void Set(string tag, string value) {
			Set(new Tag(tag), value);
		}
		public void Set(Tag tag, string value) {
			if (tag.SubComponent > 0)
				SetSubComponent(tag, value);
			else if (tag.Component > 0)
				SetComponent(tag, value);
			else if (tag.Field > 0)
				SetField(tag, value);
			else
				throw new ArgumentException("Invalid HL7 tag for Set() operation: " + tag.ToString(), "tag");
		}

		public override string ToString() {
			return ToString("\r");
		}
		public string ToString(string segmentSeperator) {
			string field_delimiter = "|";

			StringBuilder hl7 = new StringBuilder();
			foreach (List<string> segment in _segments) {
				string id = segment[0];
				if (IsControlSegment(id)) {
					field_delimiter = segment[1];

					hl7.Append(id);
					hl7.Append(field_delimiter);
					hl7.Append(String.Join(field_delimiter, segment.ToArray(), 2, segment.Count - 2));
				} else {
					hl7.Append(String.Join(field_delimiter, segment.ToArray()));
				}
				hl7.Append(segmentSeperator);
			}
			return hl7.ToString();
		}

		public static HL7v2 Parse(string hl7) {
			HL7v2 msg = new HL7v2();
			msg._field_delim = '|';
			msg._component_delim = '^';
			msg._subcomponent_delim = '&';

			string line = null;
			StringReader reader = new StringReader(hl7);
			while ((line = reader.ReadLine()) != null) {
				line = line.Trim();
				if (line == String.Empty)
					continue;

				string id = line.Substring(0, 3);
				if (IsControlSegment(id)) {
					msg._field_delim = line[3];
					msg._component_delim = line[4];
					msg._subcomponent_delim = line[7];

					List<string> segment = new List<string>();
					segment.Add(id);
					segment.Add(new string(new char[] { msg._field_delim }));
					segment.AddRange(line.Substring(4).Split(msg._field_delim));

					msg._segments.Add(segment);
				} else {
					msg._segments.Add(new List<string>(line.Split(msg._field_delim)));
				}
			}

			return msg;
		}

		public static bool IsControlSegment(string segmentId) {
			return segmentId == "MSH" || segmentId == "BHS" || segmentId == "FHS";
		}
		#endregion
	}
}
