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
using System.Data;
using System.Data.Odbc;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Text;
using System.Text.RegularExpressions;

using Dicom;
using Dicom.Data;

namespace Dicom.Data {
	public interface IDicomTransformRule {
		void Transform(DcmDataset dataset);
	}

	[Serializable]
	public class DicomTransform : IDicomTransformRule {
		#region Private Members
		private List<IDicomTransformRule> _transformRules;
		private DicomMatch _conditions;
		#endregion

		#region Public Constructor
		public DicomTransform() {
			_transformRules = new List<IDicomTransformRule>();
		}

		public DicomTransform(params IDicomTransformRule[] rules) {
			_transformRules = new List<IDicomTransformRule>(rules);
		}

		public DicomTransform(DicomMatch conditions, params IDicomTransformRule[] rules) {
			_conditions = conditions;
			_transformRules = new List<IDicomTransformRule>(rules);
		}
		#endregion

		#region Public Properties
		public DicomMatch Conditions {
			get {
				if (_conditions == null)
					_conditions = new DicomMatch();
				return _conditions;
			}
			set { _conditions = value; }
		}
		#endregion

		#region Public Methods
		public void Add(IDicomTransformRule rule) {
			_transformRules.Add(rule);
		}

		public void Transform(DcmDataset dataset) {
			if (_conditions != null)
				if (!_conditions.Match(dataset))
					return;

			foreach (IDicomTransformRule rule in _transformRules)
				rule.Transform(dataset);
		}
		#endregion
	}

	/// <summary>
	/// Remove an element from a DICOM dataset.
	/// </summary>
	[Serializable]
	public class RemoveElementDicomTransformRule : IDicomTransformRule {
		#region Private Members
		private DicomTagMask _mask;
		#endregion

		#region Public Constructor
		public RemoveElementDicomTransformRule(DicomTagMask mask) {
			_mask = mask;
		}
		#endregion

		#region Public Methods
		public void Transform(DcmDataset dataset) {
			List<DicomTag> remove = new List<DicomTag>(dataset.GetMaskedTags(_mask));
			foreach (DicomTag tag in remove) {
				dataset.Remove(tag);
			}
		}

		public override string ToString() {
			string name = String.Empty;
			if (_mask.IsFullMask)
				name = String.Format("{0} {1}", _mask.Tag, _mask.Tag.Entry.Name);
			else
				name = String.Format("[{0}] User Mask", _mask);
			return String.Format("'{0}' remove", name);
		}
		#endregion
	}

	/// <summary>
	/// Sets the value of a DICOM element.
	/// </summary>
	[Serializable]
	public class SetValueDicomTransformRule : IDicomTransformRule {
		#region Private Members
		private DicomTag _tag;
		private string _value;
		#endregion

		#region Public Constructor
		public SetValueDicomTransformRule(DicomTag tag, string value) {
			_tag = tag;
			_value = value;
		}
		#endregion

		#region Public Methods
		public void Transform(DcmDataset dataset) {
			dataset.AddElementWithValueString(_tag, _value);
		}

		public override string ToString() {
			string name = String.Format("{0} {1}", _tag, _tag.Entry.Name);
			return String.Format("'{0}' set '{1}'", name, _value);
		}
		#endregion
	}

	/// <summary>
	/// Maps the value of a DICOM element to a value.
	/// </summary>
	[Serializable]
	public class MapValueDicomTransformRule : IDicomTransformRule {
		#region Private Members
		private DicomTag _tag;
		private string _match;
		private string _value;
		#endregion

		#region Public Constructor
		public MapValueDicomTransformRule(DicomTag tag, string match, string value) {
			_tag = tag;
			_match = match;
			_value = value;
		}
		#endregion

		#region Public Methods
		public void Transform(DcmDataset dataset) {
			if (dataset.Contains(_tag) && dataset.GetValueString(_tag) == _match)
				dataset.AddElementWithValueString(_tag, _value);
		}

		public override string ToString() {
			string name = String.Format("{0} {1}", _tag, _tag.Entry.Name);
			return String.Format("'{0}' map '{1}' -> '{2}'", name, _match, _value);
		}
		#endregion
	}

	/// <summary>
	/// Copies the value of a DICOM element to another DICOM element.
	/// </summary>
	[Serializable]
	public class CopyValueDicomTransformRule : IDicomTransformRule {
		#region Private Members
		private DicomTag _src;
		private DicomTag _dst;
		#endregion

		#region Public Constructor
		public CopyValueDicomTransformRule(DicomTag src, DicomTag dst) {
			_src = src;
			_dst = dst;
		}
		#endregion

		#region Public Methods
		public void Transform(DcmDataset dataset) {
			if (dataset.Contains(_src))
				dataset.AddElementWithValueString(_dst, dataset.GetValueString(_src));
		}

		public override string ToString() {
			string sname = String.Format("{0} {1}", _src, _src.Entry.Name);
			string dname = String.Format("{0} {1}", _dst, _dst.Entry.Name);
			return String.Format("'{0}' copy to '{1}'", sname, dname);
		}
		#endregion
	}

	/// <summary>
	/// Performs a regular expression replace operation on a DICOM element value.
	/// </summary>
	[Serializable]
	public class RegexDicomTransformRule : IDicomTransformRule {
		#region Private Members
		private DicomTag _tag;
		private string _pattern;
		private string _replacement;
		#endregion

		#region Public Constructor
		public RegexDicomTransformRule(DicomTag tag, string pattern, string replacement) {
			_tag = tag;
			_pattern = pattern;
			_replacement = replacement;
		}
		#endregion

		#region Public Methods
		public void Transform(DcmDataset dataset) {
			if (dataset.Contains(_tag)) {
				string value = dataset.GetValueString(_tag);
				value = Regex.Replace(value, _pattern, _replacement);
				dataset.AddElementWithValueString(_tag, value);
			}
		}

		public override string ToString() {
			string name = String.Format("{0} {1}", _tag, _tag.Entry.Name);
			return String.Format("'{0}' regex '{1}' -> '{2}'", name, _pattern, _replacement);
		}
		#endregion
	}

	/// <summary>
	/// Prefix the value of a DICOM element.
	/// </summary>
	[Serializable]
	public class PrefixDicomTransformRule : IDicomTransformRule {
		#region Private Members
		private DicomTag _tag;
		private string _prefix;
		#endregion

		#region Public Constructor
		public PrefixDicomTransformRule(DicomTag tag, string prefix) {
			_tag = tag;
			_prefix = prefix;
		}
		#endregion

		#region Public Methods
		public void Transform(DcmDataset dataset) {
			if (dataset.Contains(_tag)) {
				string value = dataset.GetValueString(_tag);
				dataset.AddElementWithValueString(_tag, _prefix + value);
			}
		}

		public override string ToString() {
			string name = String.Format("{0} {1}", _tag, _tag.Entry.Name);
			return String.Format("'{0}' prefix '{1}'", name, _prefix);
		}
		#endregion
	}

	/// <summary>
	/// Append the value of a DICOM element.
	/// </summary>
	[Serializable]
	public class AppendDicomTransformRule : IDicomTransformRule {
		#region Private Members
		private DicomTag _tag;
		private string _append;
		#endregion

		#region Public Constructor
		public AppendDicomTransformRule(DicomTag tag, string append) {
			_tag = tag;
			_append = append;
		}
		#endregion

		#region Public Methods
		public void Transform(DcmDataset dataset) {
			if (dataset.Contains(_tag)) {
				string value = dataset.GetValueString(_tag);
				dataset.AddElementWithValueString(_tag, value + _append);
			}
		}

		public override string ToString() {
			string name = String.Format("{0} {1}", _tag, _tag.Entry.Name);
			return String.Format("'{0}' append '{1}'", name, _append);
		}
		#endregion
	}

	public enum DicomTrimPosition {
		Start,
		End,
		Both
	}

	/// <summary>
	/// Trims a string from the beginning and end of a DICOM element value.
	/// </summary>
	[Serializable]
	public class TrimStringDicomTransformRule : IDicomTransformRule {
		#region Private Members
		private DicomTag _tag;
		private string _trim;
		private DicomTrimPosition _position;
		#endregion

		#region Public Constructor
		public TrimStringDicomTransformRule(DicomTag tag, DicomTrimPosition position, string trim) {
			_tag = tag;
			_trim = trim;
			_position = position;
		}
		#endregion

		#region Public Methods
		public void Transform(DcmDataset dataset) {
			if (dataset.Contains(_tag)) {
				string value = dataset.GetValueString(_tag);
				if (_position == DicomTrimPosition.Start || _position == DicomTrimPosition.Both)
					while (value.StartsWith(_trim))
						value = value.Substring(_trim.Length);
				if (_position == DicomTrimPosition.End || _position == DicomTrimPosition.Both)
					while (value.EndsWith(_trim))
						value = value.Substring(0, value.Length - _trim.Length);
				dataset.AddElementWithValueString(_tag, value);
			}
		}

		public override string ToString() {
			string name = String.Format("{0} {1}", _tag, _tag.Entry.Name);
			return String.Format("'{0}' trim '{1}' from {2}", name, _trim, _position.ToString().ToLower());
		}
		#endregion
	}

	/// <summary>
	/// Trims whitespace or a set of characters from the beginning and end of a DICOM element value.
	/// </summary>
	[Serializable]
	public class TrimCharactersDicomTransformRule : IDicomTransformRule {
		#region Private Members
		private DicomTag _tag;
		private char[] _trim;
		private DicomTrimPosition _position;
		#endregion

		#region Public Constructor
		public TrimCharactersDicomTransformRule(DicomTag tag, DicomTrimPosition position) {
			_tag = tag;
			_trim = null;
			_position = position;
		}

		public TrimCharactersDicomTransformRule(DicomTag tag, DicomTrimPosition position, char[] trim) {
			_tag = tag;
			_trim = trim;
			_position = position;
		}
		#endregion

		#region Public Methods
		public void Transform(DcmDataset dataset) {
			if (dataset.Contains(_tag)) {
				string value = dataset.GetValueString(_tag);
				if (_position == DicomTrimPosition.Both) {
					if (_trim != null)
						value = value.Trim(_trim);
					else
						value = value.Trim();
				}
				else if (_position == DicomTrimPosition.Start) {
					if (_trim != null)
						value = value.TrimStart(_trim);
					else
						value = value.TrimStart();
				}
				else {
					if (_trim != null)
						value = value.TrimEnd(_trim);
					else
						value = value.TrimEnd();
				}
				dataset.AddElementWithValueString(_tag, value);
			}
		}

		public override string ToString() {
			string name = String.Format("{0} {1}", _tag, _tag.Entry.Name);
			if (_trim != null)
				return String.Format("'{0}' trim '{1}' from {2}", name, new string(_trim), _position.ToString().ToLower());
			else
				return String.Format("'{0}' trim whitespace from {2}", name, _position.ToString().ToLower());
		}
		#endregion
	}

	/// <summary>
	/// Pads a DICOM element value.
	/// </summary>
	[Serializable]
	public class PadStringDicomTransformRule : IDicomTransformRule {
		#region Private Members
		private DicomTag _tag;
		private char _paddingChar;
		private int _totalLength;
		#endregion

		#region Public Constructor
		public PadStringDicomTransformRule(DicomTag tag, int totalLength, char paddingChar) {
			_tag = tag;
			_totalLength = totalLength;
			_paddingChar = paddingChar;
		}
		#endregion

		#region Public Methods
		public void Transform(DcmDataset dataset) {
			if (dataset.Contains(_tag)) {
				string value = dataset.GetValueString(_tag);
				if (_totalLength < 0)
					value = value.PadLeft(-_totalLength, _paddingChar);
				else
					value = value.PadRight(_totalLength, _paddingChar);
				dataset.AddElementWithValueString(_tag, value);
			}
		}

		public override string ToString() {
			string name = String.Format("{0} {1}", _tag, _tag.Entry.Name);
			return String.Format("'{0}' pad to {1} with '{2}'", name, _totalLength, _paddingChar);
		}
		#endregion
	}

	/// <summary>
	/// Truncates a DICOM element value.
	/// </summary>
	[Serializable]
	public class TruncateDicomTransformRule : IDicomTransformRule {
		#region Private Members
		private DicomTag _tag;
		private int _length;
		#endregion

		#region Public Constructor
		public TruncateDicomTransformRule(DicomTag tag, int length) {
			_tag = tag;
			_length = length;
		}
		#endregion

		#region Public Methods
		public void Transform(DcmDataset dataset) {
			if (dataset.Contains(_tag)) {
				string value = dataset.GetValueString(_tag);
				string[] parts = value.Split('\\');
				for (int i = 0; i < parts.Length; i++) {
					if (parts[i].Length > _length)
						parts[i] = parts[i].Substring(0, _length);
				}
				value = String.Join("\\", parts);
				dataset.AddElementWithValueString(_tag, value);
			}
		}

		public override string ToString() {
			string name = String.Format("{0} {1}", _tag, _tag.Entry.Name);
			return String.Format("'{0}' truncate to {1} characters", name, _length);
		}
		#endregion
	}

	/// <summary>
	/// Splits a DICOM element value and then formats the a string from the resulting array.
	/// </summary>
	[Serializable]
	public class SplitFormatDicomTransformRule : IDicomTransformRule {
		#region Private Members
		private DicomTag _tag;
		private char[] _seperators;
		private string _format;
		#endregion

		#region Public Constructor
		public SplitFormatDicomTransformRule(DicomTag tag, char[] seperators, string format) {
			_tag = tag;
			_seperators = seperators;
			_format = format;
		}
		#endregion

		#region Public Methods
		public void Transform(DcmDataset dataset) {
			if (dataset.Contains(_tag)) {
				string value = dataset.GetValueString(_tag);
				string[] parts = value.Split(_seperators);
				value = String.Format(_format, parts);
				dataset.AddElementWithValueString(_tag, value);
			}
		}

		public override string ToString() {
			string name = String.Format("{0} {1}", _tag, _tag.Entry.Name);
			return String.Format("'{0}' split on '{1}' and format as '{2}'", name, new string(_seperators), _format);
		}
		#endregion
	}

	/// <summary>
	/// Changes the case of a DICOM element value to all upper case.
	/// </summary>
	[Serializable]
	public class ToUpperDicomTransformRule : IDicomTransformRule {
		#region Private Members
		private DicomTag _tag;
		#endregion

		#region Public Constructor
		public ToUpperDicomTransformRule(DicomTag tag) {
			_tag = tag;
		}
		#endregion

		#region Public Methods
		public void Transform(DcmDataset dataset) {
			if (dataset.Contains(_tag)) {
				string value = dataset.GetValueString(_tag);
				dataset.AddElementWithValueString(_tag, value.ToUpper());
			}
		}

		public override string ToString() {
			string name = String.Format("{0} {1}", _tag, _tag.Entry.Name);
			return String.Format("'{0}' to upper case", name);
		}
		#endregion
	}

	/// <summary>
	/// Changes the case of a DICOM element value to all lower case.
	/// </summary>
	[Serializable]
	public class ToLowerDicomTransformRule : IDicomTransformRule {
		#region Private Members
		private DicomTag _tag;
		#endregion

		#region Public Constructor
		public ToLowerDicomTransformRule(DicomTag tag) {
			_tag = tag;
		}
		#endregion

		#region Public Methods
		public void Transform(DcmDataset dataset) {
			if (dataset.Contains(_tag)) {
				string value = dataset.GetValueString(_tag);
				dataset.AddElementWithValueString(_tag, value.ToLower());
			}
		}

		public override string ToString() {
			string name = String.Format("{0} {1}", _tag, _tag.Entry.Name);
			return String.Format("'{0}' to lower case", name);
		}
		#endregion
	}

	/// <summary>
	/// Generates a new UID for a DICOM element.
	/// </summary>
	[Serializable]
	public class GenerateUidDicomTransformRule : IDicomTransformRule {
		#region Private Members
		private DicomTag _tag;
		#endregion

		#region Public Constructor
		public GenerateUidDicomTransformRule(DicomTag tag) {
			_tag = tag;
		}
		#endregion

		#region Public Methods
		public void Transform(DcmDataset dataset) {
			dataset.AddElementWithValue(_tag, DicomUID.Generate());
		}

		public override string ToString() {
			string name = String.Format("{0} {1}", _tag, _tag.Entry.Name);
			return String.Format("'{0}' generate UID", name);
		}
		#endregion
	}

	public enum DatabaseType {
		Odbc,
		MsSql,
		DB2
	}

	/// <summary>
	/// Updates a DICOM dataset based on a database query.
	/// </summary>
	[Serializable]
	public class DatabaseQueryTransformRule : IDicomTransformRule {
		#region Private Members
		private string _connectionString;
		private DatabaseType _dbType;
		private string _query;
		private List<DicomTag> _output;
		private List<DicomTag> _params;
		#endregion

		#region Public Constructor
		public DatabaseQueryTransformRule() {
			_dbType = DatabaseType.MsSql;
			_output = new List<DicomTag>();
			_params = new List<DicomTag>();
		}

		public DatabaseQueryTransformRule(string connectionString, DatabaseType dbType, string query, DicomTag[] outputTags, DicomTag[] paramTags) {
			_connectionString = connectionString;
			_dbType = dbType;
			_query = query;
			_output = new List<DicomTag>(outputTags);
			_params = new List<DicomTag>(paramTags);
		}
		#endregion

		#region Public Properties
		public string ConnectionString {
			get { return _connectionString; }
			set { _connectionString = value; }
		}

		public DatabaseType ConnectionType {
			get { return _dbType; }
			set { _dbType = value; }
		}

		public string Query {
			get { return _query; }
			set { _query = value; }
		}

		public List<DicomTag> Output {
			get { return _output; }
			set { _output = value; }
		}

		public List<DicomTag> Parameters {
			get { return _params; }
			set { _params = value; }
		}
		#endregion

		#region Public Methods
		public void Transform(DcmDataset dataset) {
			IDbConnection connection = null;

			try {
				if (_dbType == DatabaseType.Odbc)
					connection = new OdbcConnection(_connectionString);
				else if (_dbType == DatabaseType.MsSql)
					connection = new SqlConnection(_connectionString);

				using (IDbCommand command = connection.CreateCommand()) {
					command.Connection = connection;
					command.CommandText = _query;

					for (int i = 0; i < _params.Count; i++) {
						string str = dataset.GetValueString(_params[i]);
						SqlParameter prm = new SqlParameter(String.Format("@{0}", i), str);
						command.Parameters.Add(prm);
					}

					connection.Open();

					if (_output.Count == 0) {
						command.ExecuteNonQuery();
					} else {
						using (IDataReader reader = command.ExecuteReader()) {
							if (reader.Read()) {
								for (int i = 0; i < _output.Count; i++) {
									string str = reader.GetString(i);
									dataset.AddElementWithValueString(_output[i], str);
								}
							}
						}
					}

					connection.Close();

					connection = null;
				}
			} finally {
				if (connection != null) {
					if (connection.State == ConnectionState.Closed || connection.State == ConnectionState.Broken)
						connection.Close();
					connection.Dispose();
				}
			}
		}

		public override string ToString() {
			return base.ToString();
		}
		#endregion
	}
}
