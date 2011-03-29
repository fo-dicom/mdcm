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
using System.IO;
#if !SILVERLIGHT
using System.IO.Compression;
#endif
using System.Text;

using Dicom.Data;
using Dicom.Utility;

namespace Dicom.IO {
	/// <summary>DICOM write status</summary>
	public enum DicomWriteStatus {
		/// <summary>Write operation completed successfully</summary>
		Success,

		/// <summary>Unknown error occured during write operation</summary>
		UnknownError
	}

	/// <summary>
	/// Writes a DICOM dataset to a stream
	/// </summary>
	public class DicomStreamWriter {
		#region Private Members
		private const uint UndefinedLength = 0xFFFFFFFF;

		private Stream _stream = null;
		private BinaryWriter _writer = null;
		private DicomTransferSyntax _syntax = null;
		private Encoding _encoding = DcmEncoding.Default;
		private Endian _endian;

		private ushort _group = 0xffff;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new DicomStreamWriter with a target stream
		/// </summary>
		/// <param name="stream">Target stream</param>
		public DicomStreamWriter(Stream stream) {
			_stream = stream;
			TransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Transfer syntax
		/// </summary>
		public DicomTransferSyntax TransferSyntax {
			get { return _syntax; }
			set {
				_syntax = value;
				_endian = _syntax.Endian;
#if !SILVERLIGHT
				if (_syntax.IsDeflate)
					_writer = EndianBinaryWriter.Create(
						new DeflateStream(_stream, CompressionMode.Compress), _encoding, _endian);
				else
#endif
					_writer = EndianBinaryWriter.Create(_stream, _encoding, _endian);
			}
		}

		/// <summary>
		/// String encoding
		/// </summary>
		public Encoding Encoding {
			get { return _encoding; }
			set {
				_encoding = value;
				TransferSyntax = _syntax;
			}
		}
		#endregion

		/// <summary>
		/// Write dataset to stream
		/// </summary>
		/// <param name="dataset">Dataset</param>
		/// <param name="options">DICOM write options</param>
		/// <returns>Status code</returns>
		public DicomWriteStatus Write(DcmDataset dataset, DicomWriteOptions options) {
			TransferSyntax = dataset.InternalTransferSyntax;
			dataset.SelectByteOrder(_syntax.Endian);

			foreach (DcmItem item in dataset.Elements) {
				if (item.Tag.Element == 0x0000)
					continue;

				if (Flags.IsSet(options, DicomWriteOptions.CalculateGroupLengths) 
					&& item.Tag.Group != _group && item.Tag.Group <= 0x7fe0)
				{
					_group = item.Tag.Group;
					_writer.Write((ushort)_group);
					_writer.Write((ushort)0x0000);
					if (_syntax.IsExplicitVR) {
						_writer.Write((byte)'U');
						_writer.Write((byte)'L');
						_writer.Write((ushort)4);
					} else {
						_writer.Write((uint)4);
					}
					_writer.Write((uint)dataset.CalculateGroupWriteLength(_group, _syntax, options));
				}

				_writer.Write((ushort)item.Tag.Group);
				_writer.Write((ushort)item.Tag.Element);

				if (_syntax.IsExplicitVR) {
					_writer.Write((byte)item.VR.VR[0]);
					_writer.Write((byte)item.VR.VR[1]);
				}

				if (item is DcmItemSequence) {
					DcmItemSequence sq = item as DcmItemSequence;

					if (_syntax.IsExplicitVR)
						_writer.Write((ushort)0x0000);

					if (Flags.IsSet(options, DicomWriteOptions.ExplicitLengthSequence) || (item.Tag.IsPrivate && !_syntax.IsExplicitVR)) {
						int hl = _syntax.IsExplicitVR ? 12 : 8;
						_writer.Write((uint)sq.CalculateWriteLength(_syntax, options & ~DicomWriteOptions.CalculateGroupLengths) - (uint)hl);
					} else {
						_writer.Write((uint)UndefinedLength);
					}

					foreach (DcmItemSequenceItem ids in sq.SequenceItems) {
						ids.Dataset.ChangeTransferSyntax(dataset.InternalTransferSyntax, null);

						_writer.Write((ushort)DicomTags.Item.Group);
						_writer.Write((ushort)DicomTags.Item.Element);

						if (Flags.IsSet(options, DicomWriteOptions.ExplicitLengthSequenceItem)) {
							_writer.Write((uint)ids.CalculateWriteLength(_syntax, options & ~DicomWriteOptions.CalculateGroupLengths) - (uint)8);
						} else {
							_writer.Write((uint)UndefinedLength);
						}

						Write(ids.Dataset, options & ~DicomWriteOptions.CalculateGroupLengths);

						if (!Flags.IsSet(options, DicomWriteOptions.ExplicitLengthSequenceItem)) {
							_writer.Write((ushort)DicomTags.ItemDelimitationItem.Group);
							_writer.Write((ushort)DicomTags.ItemDelimitationItem.Element);
							_writer.Write((uint)0x00000000);
						}
					}

					if (!Flags.IsSet(options, DicomWriteOptions.ExplicitLengthSequence) && !(item.Tag.IsPrivate && !_syntax.IsExplicitVR)) {
						_writer.Write((ushort)DicomTags.SequenceDelimitationItem.Group);
						_writer.Write((ushort)DicomTags.SequenceDelimitationItem.Element);
						_writer.Write((uint)0x00000000);
					}
				}
				
				else if (item is DcmFragmentSequence) {
					DcmFragmentSequence fs = item as DcmFragmentSequence;

					if (_syntax.IsExplicitVR)
						_writer.Write((ushort)0x0000);
					_writer.Write((uint)UndefinedLength);

					_writer.Write((ushort)DicomTags.Item.Group);
					_writer.Write((ushort)DicomTags.Item.Element);

					if (Flags.IsSet(options, DicomWriteOptions.WriteFragmentOffsetTable) && fs.HasOffsetTable) {
						_writer.Write((uint)fs.OffsetTableBuffer.Length);
						fs.OffsetTableBuffer.CopyTo(_writer.BaseStream);
					} else {
						_writer.Write((uint)0x00000000);
					}

					foreach (ByteBuffer bb in fs.Fragments) {
						_writer.Write((ushort)DicomTags.Item.Group);
						_writer.Write((ushort)DicomTags.Item.Element);
						_writer.Write((uint)bb.Length);
						bb.CopyTo(_writer.BaseStream);
					}

					_writer.Write((ushort)DicomTags.SequenceDelimitationItem.Group);
					_writer.Write((ushort)DicomTags.SequenceDelimitationItem.Element);
					_writer.Write((uint)0x00000000);
				}
				
				else {
					DcmElement de = item as DcmElement;

					if (_syntax.IsExplicitVR) {
						if (de.VR.Is16BitLengthField) {
							_writer.Write((ushort)de.Length);
						} else {
							_writer.Write((ushort)0x0000);
							_writer.Write((uint)de.Length);
						}
					} else {
						_writer.Write((uint)de.Length);
					}

					de.ByteBuffer.CopyTo(_writer.BaseStream);
				}
			}

			return DicomWriteStatus.Success;
		}
	}
}
