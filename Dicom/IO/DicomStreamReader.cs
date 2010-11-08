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
using System.Text;

using Dicom.Data;
using Dicom.Utility;

namespace Dicom.IO {
	/// <summary>DICOM read status</summary>
	public enum DicomReadStatus {
		/// <summary>Read operation completed successfully</summary>
		Success,

		/// <summary>Unknown error occurred during read operation</summary>
		UnknownError,

		/// <summary>More data is needed to complete dataset</summary>
		NeedMoreData,

		/// <summary>Found the tag we were looking for (internal)</summary>
		SuccessEndRead
	}

	/// <summary>
	/// Reads a DICOM dataset from a stream
	/// </summary>
	public class DicomStreamReader {
		#region Private Members
		private const uint UndefinedLength = 0xFFFFFFFF;

		private Stream _stream = null;
		private BinaryReader _reader = null;
		private DicomTransferSyntax _syntax = null;
		private Encoding _encoding = DcmEncoding.Default;
		private Endian _endian;
		private bool _isFile;

		private uint _largeElementSize = 4096;

		private DcmDataset _dataset;

		private uint _privateCreatorCard = 0xffffffff;
		private string _privateCreatorId = String.Empty;

		private DicomTag _tag = null;
		private DicomVR _vr = null;
		private uint _len = UndefinedLength;
		private long _pos = 0;
		private long _offset = 0;

		private long _bytes = 0;
		private long _read = 0;
		private uint _need = 0;
		private long _remain = 0;

		private Stack<DcmDataset> _sds = new Stack<DcmDataset>();
		private Stack<DcmItemSequence> _sqs = new Stack<DcmItemSequence>();
		private DcmFragmentSequence _fragment = null;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new DicomStreamReader with a source stream
		/// </summary>
		/// <param name="stream">Source stream</param>
		public DicomStreamReader(Stream stream) {
			_stream = stream;
			_isFile = _stream is FileStream;
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
				_reader = EndianBinaryReader.Create(_stream, _encoding, _endian);
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

		/// <summary>
		/// DICOM dataset
		/// </summary>
		public DcmDataset Dataset {
			get { return _dataset; }
			set {
				_dataset = value;
				TransferSyntax = _dataset.InternalTransferSyntax;
			}
		}

		/// <summary>
		/// Estimated size of dataset
		/// </summary>
		public long BytesEstimated {
			get { return _bytes + _need; }
		}

		/// <summary>
		/// Number of bytes read from stream
		/// </summary>
		public long BytesRead {
			get { return _read; }
		}

		/// <summary>
		/// Number of bytes remaining in stream
		/// </summary>
		public long BytesRemaining {
			get { return _remain; }
		}

		/// <summary>
		/// Number of bytes needed to complete current read operation
		/// </summary>
		public uint BytesNeeded {
			get { return _need; }
		}

		/// <summary>
		/// Offset of this stream relative to a parent stream.
		/// </summary>
		public long PositionOffset {
			get { return _offset; }
			set { _offset = value; }
		}

		/// <summary>
		/// Minimum size, in bytes, of elements that should be defer loaded when
		/// using the DicomReadOptions.DeferLoadingLargeElements read option.
		/// </summary>
		public uint LargeElementSize {
			get { return _largeElementSize; }
			set { _largeElementSize = value; }
		}
		#endregion

		private ByteBuffer CurrentBuffer(DicomReadOptions options) {
			ByteBuffer bb = null;

			if (_isFile) {
				bool delayLoad = false;
				if (_len >= _largeElementSize && _vr != DicomVR.SQ) {
					if (Flags.IsSet(options, DicomReadOptions.DeferLoadingLargeElements))
						delayLoad = true;
					else if (Flags.IsSet(options, DicomReadOptions.DeferLoadingPixelData) && _tag == DicomTags.PixelData)
						delayLoad = true;
					else if (Flags.IsSet(options, DicomReadOptions.DeferLoadingPixelData) && _fragment != null && _fragment.Tag == DicomTags.PixelData)
						delayLoad = true;
				}

				if (delayLoad) {
					FileStream fs = (FileStream)_stream;
					FileSegment segment = new FileSegment(fs.Name, fs.Position, _len);
					_stream.Seek(_len, SeekOrigin.Current);
					bb = new ByteBuffer(segment, _endian);
				}
			}

			if (bb == null) {
				bb = new ByteBuffer(_endian);
				bb.CopyFrom(_stream, (int)_len);
			}

			if (_vr.IsEncodedString)
				bb.Encoding = _encoding;

			return bb;
		}

		private DicomReadStatus NeedMoreData(long count) {
			_need = (uint)(count - _remain);
			return DicomReadStatus.NeedMoreData;
		}

		/// <summary>
		/// Read dataset from stream
		/// </summary>
		/// <param name="stopAtTag">End parsing at this tag</param>
		/// <param name="options">DICOM read options</param>
		/// <returns>Status code</returns>
		public DicomReadStatus Read(DicomTag stopAtTag, DicomReadOptions options) {
			// Counters:
			//  _remain - bytes remaining in stream
			//  _bytes - estimates bytes to end of dataset
			//  _read - number of bytes read from stream
			try {
				_need = 0;
				_remain = _stream.Length - _stream.Position;

				while (_remain > 0) {
					DicomReadStatus status = ParseTag(stopAtTag, options);
					if (status == DicomReadStatus.SuccessEndRead)
						return DicomReadStatus.Success;
					if (status != DicomReadStatus.Success)
						return status;

					status = ParseVR(options);
					if (status != DicomReadStatus.Success)
						return status;

					status = ParseLength(options);
					if (status != DicomReadStatus.Success)
						return status;
					
					if (_tag.IsPrivate) {
						if (_tag.Element != 0x0000 && _tag.Element <= 0x00ff) {
							// handle UN private creator id
							if (_vr != DicomVR.LO && Flags.IsSet(options, DicomReadOptions.ForcePrivateCreatorToLO)) {
								Dicom.Debug.Log.Warn("Converting Private Creator VR from '{0}' to 'LO'", _vr.VR);
								_vr = DicomVR.LO;
							}
						}
					}

					if (_vr == DicomVR.UN && _syntax.IsExplicitVR && Flags.IsSet(options, DicomReadOptions.UseDictionaryForExplicitUN)) {
						_vr = _tag.Entry.DefaultVR;
					}

					if (_fragment != null) {
						status = InsertFragmentItem(options);
						if (status != DicomReadStatus.Success)
							return status;
					}
					else if (_sqs.Count > 0 &&
								(_tag == DicomTags.Item ||
								 _tag == DicomTags.ItemDelimitationItem ||
								 _tag == DicomTags.SequenceDelimitationItem)) {
						status = InsertSequenceItem(options);
						if (status != DicomReadStatus.Success)
							return status;
					}
					else {
						if (_sqs.Count > 0) {
							DcmItemSequence sq = _sqs.Peek();
							if (sq.StreamLength != UndefinedLength) {
								long end = sq.StreamPosition + 8 + sq.StreamLength;
								if (_syntax.IsExplicitVR)
									end += 2 + 2;
								if ((_stream.Position - _offset) >= end) {
									if (_sds.Count == _sqs.Count)
										_sds.Pop();
									_sqs.Pop();
								}
							}
						}

						if (_len == UndefinedLength) {
							if (_vr == DicomVR.SQ) {
								DcmItemSequence sq = new DcmItemSequence(_tag, _pos, _len, _endian);
								InsertDatasetItem(sq, options);
								_sqs.Push(sq);
							}
							else {
								_fragment = new DcmFragmentSequence(_tag, _vr, _pos, _endian);
								InsertDatasetItem(_fragment, options);
							}
						}
						else {
							if (_vr == DicomVR.SQ) {
								DcmItemSequence sq = new DcmItemSequence(_tag, _pos, _len, _endian);
								InsertDatasetItem(sq, options);
								_sqs.Push(sq);
							}
							else {
								if (_len > _remain)
									return NeedMoreData(_len);

								DcmElement elem = DcmElement.Create(_tag, _vr, _pos, _endian, CurrentBuffer(options));
								_remain -= _len;
								_read += _len;

								InsertDatasetItem(elem, options);
							}
						}
					}

					_tag = null;
					_vr = null;
					_len = UndefinedLength;
				}

				return DicomReadStatus.Success;
			}
			catch (EndOfStreamException) {
				// should never happen
				return DicomReadStatus.UnknownError;
			}
		}

		private DicomReadStatus ParseTag(DicomTag stopAtTag, DicomReadOptions options) {
			if (_tag == null) {
				if (_remain >= 4) {
					_pos = _stream.Position + _offset;
					ushort g = _reader.ReadUInt16();
					if (Flags.IsSet(options, DicomReadOptions.FileMetaInfoOnly) && g != 0x0002) {
						_stream.Seek(-2, SeekOrigin.Current);
						return DicomReadStatus.SuccessEndRead;
					}
					ushort e = _reader.ReadUInt16();
					if (DicomTag.IsPrivateGroup(g) && e > 0x00ff) {
						uint card = DicomTag.GetCard(g, e);
						if ((card & 0xffffff00) != _privateCreatorCard) {
							_privateCreatorCard = card & 0xffffff00;
							DicomTag pct = DicomTag.GetPrivateCreatorTag(g, e);
							DcmDataset ds = _dataset;
							if (_sds.Count > 0 && _sds.Count == _sqs.Count) {
								ds = _sds.Peek();
								if (!ds.Contains(pct))
									ds = _dataset;
							}
							_privateCreatorId = ds.GetString(pct, String.Empty);
						}
						_tag = new DicomTag(g, e, _privateCreatorId);
					}
					else {
						_tag = new DicomTag(g, e);

						if (g == 0xfffe) {
							if (_tag == DicomTags.Item ||
								_tag == DicomTags.ItemDelimitationItem ||
								_tag == DicomTags.SequenceDelimitationItem)
								_vr = DicomVR.NONE;
						}
					}
					_remain -= 4;
					_bytes += 4;
					_read += 4;
				}
				else {
					return NeedMoreData(4);
				}
			}

			if (_tag == DicomTags.ItemDelimitationItem && Flags.IsSet(options, DicomReadOptions.SequenceItemOnly))
				return DicomReadStatus.SuccessEndRead;

			if (_tag >= stopAtTag)
				return DicomReadStatus.SuccessEndRead;

			return DicomReadStatus.Success;
		}

		private DicomReadStatus ParseVR(DicomReadOptions options) {
			if (_vr == null) {
				if (_syntax.IsExplicitVR) {
					if (_remain >= 2) {
						_vr = DicomVR.Lookup(_reader.ReadChars(2));
						_remain -= 2;
						_bytes += 2;
						_read += 2;
					}
					else {
						return NeedMoreData(2);
					}
				}
				else {
					if (_tag.Element == 0x0000)
						_vr = DicomVR.UL;
					else if (Flags.IsSet(options, DicomReadOptions.ForcePrivateCreatorToLO) &&
						_tag.IsPrivate && _tag.Element > 0x0000 && _tag.Element <= 0x00ff)
						_vr = DicomVR.UN;
					else
						_vr = _tag.Entry.DefaultVR;
				}

				if (_vr == DicomVR.UN) {
					if (_tag.Element == 0x0000)
						_vr = DicomVR.UL; // is this needed?
					else if (_tag.IsPrivate) {
						if (_tag.Element <= 0x00ff) {
							// private creator id
						} else if (_stream.CanSeek && Flags.IsSet(options, DicomReadOptions.AllowSeekingForContext)) {
							// attempt to identify private sequence
							long pos = _stream.Position;
							if (_syntax.IsExplicitVR) {
								if (_remain >= 2)
									_reader.ReadUInt16();
								else {
									_vr = null;
									_stream.Position = pos;
									return NeedMoreData(2);
								}
							}

							uint l = 0;
							if (_remain >= 4) {
								l = _reader.ReadUInt32();
								if (l == UndefinedLength)
									_vr = DicomVR.SQ;
							} else {
								_vr = null;
								_stream.Position = pos;
								return NeedMoreData(4);
							}

							//if (l != 0 && _vr == DicomVR.UN) {
							//    if (_remain >= 4) {
							//        ushort g = _reader.ReadUInt16();
							//        ushort e = _reader.ReadUInt16();
							//        DicomTag tag = new DicomTag(g, e);
							//        if (tag == DicomTags.Item || tag == DicomTags.SequenceDelimitationItem)
							//            _vr = DicomVR.SQ;
							//    } else {
							//        _vr = null;
							//        _stream.Position = pos;
							//        return NeedMoreData(4);
							//    }
							//}

							_stream.Position = pos;
						}
					}
				}
			}
			return DicomReadStatus.Success;
		}

		private DicomReadStatus ParseLength(DicomReadOptions options) {
			if (_len == UndefinedLength) {
				if (_syntax.IsExplicitVR) {
					if (_tag == DicomTags.Item ||
						_tag == DicomTags.ItemDelimitationItem ||
						_tag == DicomTags.SequenceDelimitationItem) {
						if (_remain >= 4) {
							_len = _reader.ReadUInt32();
							_remain -= 4;
							_bytes += 4;
							_read += 4;
						}
						else {
							return NeedMoreData(4);
						}
					}
					else {
						if (_vr.Is16BitLengthField) {
							if (_remain >= 2) {
								_len = (uint)_reader.ReadUInt16();
								_remain -= 2;
								_bytes += 2;
								_read += 2;
							} else {
								return NeedMoreData(2);
							}
						}
						else {
							if (_remain >= 6) {
								_reader.ReadByte();
								_reader.ReadByte();
								_len = _reader.ReadUInt32();
								_remain -= 6;
								_bytes += 6;
								_read += 6;
							}
							else {
								return NeedMoreData(6);
							}
						}
					}
				}
				else {
					if (_remain >= 4) {
						_len = _reader.ReadUInt32();
						_remain -= 4;
						_bytes += 4;
						_read += 4;
					}
					else {
						return NeedMoreData(4);
					}
				}

				if (_len != UndefinedLength) {
					if (_vr != DicomVR.SQ && !(_tag.Equals(DicomTags.Item) && _fragment == null))
						_bytes += _len;
				}
			}
			return DicomReadStatus.Success;
		}

		private DicomReadStatus InsertFragmentItem(DicomReadOptions options) {
			if (_tag == DicomTags.Item) {
				if (_len > _remain)
					return NeedMoreData(_len);

				ByteBuffer data = CurrentBuffer(options);
				_remain -= _len;
				_read += _len;

				if (!_fragment.HasOffsetTable)
					_fragment.SetOffsetTable(data);
				else
					_fragment.AddFragment(data);
			}
			else if (_tag == DicomTags.SequenceDelimitationItem) {
				_fragment = null;
			}
			else {
				// unexpected tag
				return DicomReadStatus.UnknownError;
			}
			return DicomReadStatus.Success;
		}

		private DicomReadStatus ParseSequenceItemDataset(DicomTransferSyntax syntax, long len, out DcmDataset dataset, DicomReadOptions options) {
			long pos = _stream.Position;

			dataset = new DcmDataset(pos, (uint)len, syntax);

			Stream stream = (len != UndefinedLength) ? new SegmentStream(_stream, _stream.Position, _len) : _stream;

			DicomStreamReader idsr = new DicomStreamReader(stream);
			idsr.Dataset = dataset;
			idsr.Encoding = _encoding;
			if (len != UndefinedLength)
				idsr.PositionOffset = dataset.StreamPosition;

			DicomReadStatus status = idsr.Read(null, options);

			if (status != DicomReadStatus.Success) {
				_stream.Seek(pos, SeekOrigin.Begin);
				dataset = null;
			}
			else {
				if (len == UndefinedLength) {
					// rewind delimitation item tag
					_stream.Seek(-4, SeekOrigin.Current);

					len = _stream.Position - pos;
				}

				_remain -= len;
				_bytes += len;
				_read += len;
			}

			return status;
		}

		private DicomReadStatus InsertSequenceItem(DicomReadOptions options) {
			if (_tag.Equals(DicomTags.Item)) {
				if (_len != UndefinedLength && _len > _remain)
					return NeedMoreData(_len);

				if (_sds.Count > _sqs.Count)
					_sds.Pop();

				DcmItemSequenceItem si = new DcmItemSequenceItem(_pos, _len);

				if (_len != UndefinedLength || (_stream.CanSeek && Flags.IsSet(options, DicomReadOptions.AllowSeekingForContext))) {
					if (_len == UndefinedLength)
						options |= DicomReadOptions.SequenceItemOnly;

					DcmDataset ds = null;
					DicomReadStatus status = ParseSequenceItemDataset(TransferSyntax, _len, out ds, options);

					if (status == DicomReadStatus.NeedMoreData)
						return DicomReadStatus.NeedMoreData;

					if (status != DicomReadStatus.Success) {
						Dicom.Debug.Log.Warn("Unknown error while attempting to read sequence item.  Trying again with alternate encodings.");

						DicomTransferSyntax[] syntaxes = null;
						if (TransferSyntax == DicomTransferSyntax.ExplicitVRBigEndian)
							syntaxes = new DicomTransferSyntax[] { DicomTransferSyntax.ImplicitVRLittleEndian, DicomTransferSyntax.ExplicitVRLittleEndian };
						else if (TransferSyntax.IsExplicitVR)
							syntaxes = new DicomTransferSyntax[] { DicomTransferSyntax.ImplicitVRLittleEndian, DicomTransferSyntax.ExplicitVRBigEndian };
						else
							syntaxes = new DicomTransferSyntax[] { DicomTransferSyntax.ExplicitVRLittleEndian, DicomTransferSyntax.ExplicitVRBigEndian };

						foreach (DicomTransferSyntax tx in syntaxes) {
							status = ParseSequenceItemDataset(tx, _len, out ds, options);
							if (status == DicomReadStatus.Success)
								break;
						}
					}

					if (status != DicomReadStatus.Success)
						return DicomReadStatus.UnknownError;

					si.Dataset = ds;

					if (_len == UndefinedLength) {
						if (8 > _remain) {
							// need more data?
							_sds.Push(ds);
						}
						else {
							// skip delimitation item
							_stream.Seek(8, SeekOrigin.Current);
							_remain -= 8;
							_bytes += 8;
							_read += 8;
						}
					}
				}
				else {
					DcmDataset ds = new DcmDataset(_pos + 8, _len, TransferSyntax);
					_sds.Push(ds);
				}

				_sqs.Peek().AddSequenceItem(si);
			}
			else if (_tag == DicomTags.ItemDelimitationItem) {
				if (_sds.Count == _sqs.Count)
					_sds.Pop();
			}
			else if (_tag == DicomTags.SequenceDelimitationItem) {
				if (_sds.Count == _sqs.Count)
					_sds.Pop();
				_sqs.Pop();
			}
			return DicomReadStatus.Success;
		}

		private void InsertDatasetItem(DcmItem item, DicomReadOptions options) {
			if (_sds.Count > 0 && _sds.Count == _sqs.Count) {
				DcmDataset ds = _sds.Peek();

				if (_tag.Element == 0x0000) {
					if (Flags.IsSet(options, DicomReadOptions.KeepGroupLengths))
						ds.AddItem(item);
				}
				else
					ds.AddItem(item);

				if (ds.StreamLength != UndefinedLength) {
					long end = ds.StreamPosition + ds.StreamLength;
					if ((_stream.Position - _offset) >= end)
						_sds.Pop();
				}
			}
			else {
				if (_tag.Element == 0x0000) {
					if (Flags.IsSet(options, DicomReadOptions.KeepGroupLengths))
						_dataset.AddItem(item);
				}
				else
					_dataset.AddItem(item);
			}

			if (_tag == DicomTags.SpecificCharacterSet && item is DcmCodeString) {
				DcmCodeString cs = (DcmCodeString)item;
				if (cs.Length > 0) {
					string[] values = cs.GetValues();
					for (int i = 0; i < values.Length; i++) {
						if (String.IsNullOrEmpty(values[i]))
							continue;
						_encoding = DcmEncoding.GetEncodingForSpecificCharacterSet(values[i]);
						break;
					}
				}
			}
		}
	}
}
