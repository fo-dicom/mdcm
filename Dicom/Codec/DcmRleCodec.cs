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
// Credits:
//    Includes patches from ClearCanvas project (TODO: CC License)
//
// Note:  This file may contain code using a license that has not been 
//        verified to be compatible with the licensing of this software.  
//
// References:
//     * originally based on the RLE codec on DCMTK
//       http://www.dcmtk.org

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Dicom.Data;
using Dicom.IO;
using Dicom.Utility;

namespace Dicom.Codec {
	public class DcmRleCodecParameters : DcmCodecParameters {
		#region Private Members
		bool _reverseByteOrder;
		#endregion

		#region Public Members
		public DcmRleCodecParameters() {
			_reverseByteOrder = false;
		}

		public DcmRleCodecParameters(bool reverseByteOrder) {
			_reverseByteOrder = reverseByteOrder;
		}
		#endregion

		#region Public Properties
		public bool ReverseByteOrder {
			get { return _reverseByteOrder; }
			set { _reverseByteOrder = value; }
		}
		#endregion
	}

	[DicomCodec]
	public class DcmRleCodec : IDcmCodec {
		public string GetName() {
			return "RLE Lossless";
		}

		public DicomTransferSyntax GetTransferSyntax() {
			return DicomTransferSyntax.RLELossless;
		}

		public DcmCodecParameters GetDefaultParameters() {
			return new DcmRleCodecParameters();
		}

		public static void Register() {
			DicomCodec.RegisterCodec(DicomTransferSyntax.RLELossless, typeof(DcmRleCodec));
		}

		#region Encode
		private class RLEEncoder {
			#region Private Members
			private int _count;
			private readonly uint[] _offsets;
			private readonly MemoryStream _stream;
			private readonly BinaryWriter _writer;
			private readonly byte[] _buffer;

			private int _prevByte;
			private int _repeatCount;
			private int _bufferPos;
			#endregion

			#region Public Constructors
			public RLEEncoder() {
				_count = 0;
				_offsets = new uint[15];
				_stream = new MemoryStream();
				_writer = EndianBinaryWriter.Create(_stream, Endian.Little);
				_buffer = new byte[132];
				WriteHeader();

				_prevByte = -1;
				_repeatCount = 0;
				_bufferPos = 0;
			}
			#endregion

			#region Public Members
			public int NumberOfSegments {
				get { return _count; }
			}

			public long Length {
				get { return _stream.Length; }
			}

			public byte[] GetBuffer() {
				Flush();
				WriteHeader();
				return _stream.ToArray();
			}

			public void NextSegment() {
				Flush();
				if ((Length & 1) == 1)
					_stream.WriteByte(0x00);
				_offsets[_count++] = (uint)_stream.Length;
			}

			public void Encode(byte b) {
				if (b == _prevByte) {
					_repeatCount++;

					if (_repeatCount > 2 && _bufferPos > 0) {
						// We're starting a run, flush out the buffer
						while (_bufferPos > 0) {
							int count = Math.Min(128, _bufferPos);
							_stream.WriteByte((byte)(count - 1));
							MoveBuffer(count);
						}
					}
					else if (_repeatCount > 128) {
						int count = Math.Min(_repeatCount, 128);
						_stream.WriteByte((byte)(257 - count));
						_stream.WriteByte((byte)_prevByte);
						_repeatCount -= count;
					}
				}
				else {
					switch (_repeatCount) {
						case 0:
							break;
						case 1: {
								_buffer[_bufferPos++] = (byte)_prevByte;
								break;
							}
						case 2: {
								_buffer[_bufferPos++] = (byte)_prevByte;
								_buffer[_bufferPos++] = (byte)_prevByte;
								break;
							}
						default: {
								while (_repeatCount > 0) {
									int count = Math.Min(_repeatCount, 128);
									_stream.WriteByte((byte)(257 - count));
									_stream.WriteByte((byte)_prevByte);
									_repeatCount -= count;
								}

								break;
							}
					}

					while (_bufferPos > 128) {
						int count = Math.Min(128, _bufferPos);
						_stream.WriteByte((byte)(count - 1));
						MoveBuffer(count);
					}

					_prevByte = b;
					_repeatCount = 1;
				}
			}

			public void MakeEvenLength() {
				// Make even length
				if (_stream.Length % 2 == 1)
					_stream.WriteByte(0);
			}

			public void Flush() {
				if (_repeatCount < 2) {
					while (_repeatCount > 0) {
						_buffer[_bufferPos++] = (byte)_prevByte;
						_repeatCount--;
					}
				}

				while (_bufferPos > 0) {
					int count = Math.Min(128, _bufferPos);
					_stream.WriteByte((byte)(count - 1));
					MoveBuffer(count);
				}

				if (_repeatCount >= 2) {
					while (_repeatCount > 0) {
						int count = Math.Min(_repeatCount, 128);
						_stream.WriteByte((byte)(257 - count));
						_stream.WriteByte((byte)_prevByte);
						_repeatCount -= count;
					}
				}

				_prevByte = -1;
				_repeatCount = 0;
				_bufferPos = 0;
			}
			#endregion

			#region Private Members
			private void MoveBuffer(int count) {
				_stream.Write(_buffer, 0, count);
				for (int i = count, n = 0; i < _bufferPos; i++, n++) {
					_buffer[n] = _buffer[i];
				}
				_bufferPos = _bufferPos - count;
			}

			private void WriteHeader() {
				_stream.Seek(0, SeekOrigin.Begin);
				_writer.Write((uint)_count);
				for (int i = 0; i < 15; i++) {
					_writer.Write(_offsets[i]);
				}
			}
			#endregion
		}

		public void Encode(DcmDataset dataset, DcmPixelData oldPixelData, DcmPixelData newPixelData, DcmCodecParameters parameters) {
			DcmRleCodecParameters rleParams = parameters as DcmRleCodecParameters;

			if (rleParams == null)
				rleParams = GetDefaultParameters() as DcmRleCodecParameters;

			int pixelCount = oldPixelData.ImageWidth * oldPixelData.ImageHeight;
			int numberOfSegments = oldPixelData.BytesAllocated * oldPixelData.SamplesPerPixel;

			for (int i = 0; i < oldPixelData.NumberOfFrames; i++) {
				RLEEncoder encoder = new RLEEncoder();
				byte[] frameData = oldPixelData.GetFrameDataU8(i);

				for (int s = 0; s < numberOfSegments; s++) {
					encoder.NextSegment();

					int sample = s / oldPixelData.BytesAllocated;
					int sabyte = s % oldPixelData.BytesAllocated;

					int pos;
					int offset;

					if (newPixelData.PlanarConfiguration == 0) {
						pos = sample * oldPixelData.BytesAllocated;
						offset = numberOfSegments;
					}
					else {
						pos = sample * oldPixelData.BytesAllocated * pixelCount;
						offset = oldPixelData.BytesAllocated;
					}

					if (rleParams.ReverseByteOrder)
						pos += sabyte;
					else
						pos += oldPixelData.BytesAllocated - sabyte - 1;

					for (int p = 0; p < pixelCount; p++) {
						if (pos >= frameData.Length)
							throw new DicomCodecException("");
						encoder.Encode(frameData[pos]);
						pos += offset;
					}
					encoder.Flush();
				}

				encoder.MakeEvenLength();

				newPixelData.AddFrame(encoder.GetBuffer());
			}
		}
		#endregion

		#region Decode
		private class RLEDecoder {
			#region Private Members
			private int _count;
			private int[] _offsets;
			private byte[] _data;
			#endregion

			#region Public Constructors
			public RLEDecoder(IList<ByteBuffer> data) {
				uint size = 0;
				foreach (ByteBuffer frag in data)
					size += (uint)frag.Length;
				MemoryStream stream = new MemoryStream(data[0].ToBytes());
				for (int i = 1; i < data.Count; i++) {
					stream.Seek(0, SeekOrigin.End);
					byte[] ba = data[i].ToBytes();
					stream.Write(ba, 0, ba.Length);
				}
				BinaryReader reader = EndianBinaryReader.Create(stream, Endian.Little);
				_count = (int)reader.ReadUInt32();
				_offsets = new int[15];
				for (int i = 0; i < 15; i++) {
					_offsets[i] = reader.ReadInt32();
				}
				_data = new byte[stream.Length - 64]; // take off 64 bytes for the offsets
				stream.Read(_data, 0, _data.Length);
			}
			#endregion

			#region Public Members
			public int NumberOfSegments {
				get { return _count; }
			}

			public void DecodeSegment(int segment, byte[] buffer, int start, int sampleOffset) {
				if (segment < 0 || segment >= _count)
					throw new IndexOutOfRangeException("Segment number out of range");

				int offset = GetSegmentOffset(segment);
				int length = GetSegmentLength(segment);

				Decode(buffer, start, sampleOffset, _data, offset, length);
			}

			private static void Decode(byte[] buffer, int start, int sampleOffset, byte[] rleData, int offset, int count) {
				unchecked {
					int pos = start;
					int end = offset + count;
					int bufferLength = buffer.Length;

					for (int i = offset; i < end && pos < bufferLength; ) {
						sbyte control = (sbyte)rleData[i++];

						if (control >= 0) {
							int length = control + 1;

							if ((end - i) < length)
								throw new DicomCodecException("RLE literal run exceeds input buffer length.");
							if ((pos + ((length - 1) * sampleOffset)) >= bufferLength)
							    throw new DicomCodecException("RLE literal run exceeds output buffer length.");

							if (sampleOffset == 1) {
								// ANTS says this is faster than Array.Copy!
								Buffer.BlockCopy(rleData, i, buffer, pos, length);
								pos += length;
								i += length;
							}
							else {
								while (length-- > 0) {
									buffer[pos] = rleData[i++];
									pos += sampleOffset;
								}
							}
						}
						else if (control >= -127) {
							int length = -control;

							//if (i >= end) // never happens due to check below
							//    throw new DicomCodecException("RLE repeat run exceeds input buffer length.");
							if ((pos + ((length - 1) * sampleOffset)) >= bufferLength)
							    throw new DicomCodecException("RLE repeat run exceeds output buffer length.");

							// why is there not a managed version of memset??
							byte b = rleData[i++];

							if (sampleOffset == 1) {
								while (length-- >= 0)
									buffer[pos++] = b;
							}
							else {
								while (length-- >= 0) {
									buffer[pos] = b;
									pos += sampleOffset;
								}
							}
						}

						if ((i + 2) >= end)
							break;
					}
				}
			}
			#endregion

			#region Private Members
			private int GetSegmentOffset(int segment) {
				return _offsets[segment] - 64;
			}

			private int GetSegmentLength(int segment) {
				int offset = GetSegmentOffset(segment);
				if (segment < (_count - 1)) {
					int next = GetSegmentOffset(segment + 1);
					return next - offset;
				}
				else {
					return _data.Length - offset;
				}
			}
			#endregion
		}

		public void Decode(DcmDataset dataset, DcmPixelData oldPixelData, DcmPixelData newPixelData, DcmCodecParameters parameters) {
			DcmRleCodecParameters rleParams = parameters as DcmRleCodecParameters;

			if (rleParams == null)
				rleParams = GetDefaultParameters() as DcmRleCodecParameters;

			int pixelCount = oldPixelData.ImageWidth * oldPixelData.ImageHeight;
			int numberOfSegments = oldPixelData.BytesAllocated * oldPixelData.SamplesPerPixel;

			byte[] frameData = new byte[newPixelData.UncompressedFrameSize];

			for (int i = 0; i < oldPixelData.NumberOfFrames; i++) {
				IList<ByteBuffer> rleData = oldPixelData.GetFrameFragments(i);
				RLEDecoder decoder = new RLEDecoder(rleData);

				if (decoder.NumberOfSegments != numberOfSegments)
					throw new DicomCodecException("Unexpected number of RLE segments!");

				for (int s = 0; s < numberOfSegments; s++) {
					int sample = s / newPixelData.BytesAllocated;
					int sabyte = s % newPixelData.BytesAllocated;

					int pos, offset;

					if (newPixelData.PlanarConfiguration == 0) {
						pos = sample * newPixelData.BytesAllocated;
						offset = newPixelData.SamplesPerPixel * newPixelData.BytesAllocated;
					}
					else {
						pos = sample * newPixelData.BytesAllocated * pixelCount;
						offset = newPixelData.BytesAllocated;
					}

					if (rleParams.ReverseByteOrder)
						pos += sabyte;
					else
						pos += newPixelData.BytesAllocated - sabyte - 1;

					decoder.DecodeSegment(s, frameData, pos, offset);
				}

				newPixelData.AddFrame(frameData);
			}
		}
		#endregion
	}
}
