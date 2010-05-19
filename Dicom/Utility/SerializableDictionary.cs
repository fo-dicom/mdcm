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
//     * Based on SerializableDictionary by Paul Welter
//       http://weblogs.asp.net/pwelter34/archive/2006/05/03/444961.aspx

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Dicom.Utility {
	[XmlRoot("dictionary"), Serializable()]
	public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable, ISerializable {
		public SerializableDictionary() {
		}

		public SerializableDictionary(SerializationInfo info, StreamingContext context) : base(info, context) {
		}

		#region IXmlSerializable Members
		public System.Xml.Schema.XmlSchema GetSchema() {
			return null;
		}

		public void ReadXml(System.Xml.XmlReader reader) {
			XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
			XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));
			bool wasEmpty = reader.IsEmptyElement;
			reader.Read();
			if (wasEmpty)
				return;
			while (reader.NodeType != System.Xml.XmlNodeType.EndElement) {
				reader.ReadStartElement("item");
				reader.ReadStartElement("key");
				TKey key = (TKey)keySerializer.Deserialize(reader);
				reader.ReadEndElement();
				reader.ReadStartElement("value");
				TValue value = (TValue)valueSerializer.Deserialize(reader);
				reader.ReadEndElement();
				this.Add(key, value);
				reader.ReadEndElement();
				reader.MoveToContent();
			}
			reader.ReadEndElement();
		}

		public void WriteXml(System.Xml.XmlWriter writer) {
			XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
			XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));
			foreach (TKey key in this.Keys) {
				writer.WriteStartElement("item");
				writer.WriteStartElement("key");
				keySerializer.Serialize(writer, key);
				writer.WriteEndElement();
				writer.WriteStartElement("value");
				TValue value = this[key];
				valueSerializer.Serialize(writer, value);
				writer.WriteEndElement();
				writer.WriteEndElement();
			}
		}
		#endregion

		#region ISerializable Members
		public override void GetObjectData(SerializationInfo info, StreamingContext context) {
			base.GetObjectData(info, context);
		}
		#endregion
	}
}
