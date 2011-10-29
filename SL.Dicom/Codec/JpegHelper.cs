// Copyright (c) 2011 Anders Gustafsson, Cureos AB.
// All rights reserved. This software and the accompanying materials
// are made available under the terms of the Eclipse Public License v1.0
// which accompanies this distribution, and is available at
// http://www.eclipse.org/legal/epl-v10.html

using System.IO;
using Dicom.Data;
using Dicom.IO;

namespace Dicom.Codec
{
    public static class JpegHelper
    {
        /// <summary>
        /// Scan header of JPEG image to determine image bit depth
        /// </summary>
        /// <param name="pixelData"></param>
        /// <returns></returns>
        /// <remarks>C# translation of corresponding managed C++ method in JpegHelper.h in the Dicom.Codec(64) project. 
        /// Original C++ implementation (probably) emanates from OFFIS DCMTK, djcodecd.cxx</remarks>
        public static int ScanHeaderForBitDepth(DcmPixelData pixelData)
        {
            ByteBuffer buffer = pixelData.PixelDataSequence.Fragments[0];
            MemoryStream ms = buffer.Stream;
            BinaryReader br = EndianBinaryReader.Create(ms, Endian.Big);

            long length = ms.Length;
            while (ms.Position < length)
            {
                ushort marker = br.ReadUInt16();
                switch (marker)
                {
                    case 0xffc0: // SOF_0: JPEG baseline
                    case 0xffc1: // SOF_1: JPEG extended sequential DCT
                    case 0xffc2: // SOF_2: JPEG progressive DCT
                    case 0xffc3: // SOF_3: JPEG lossless sequential
                    case 0xffc5: // SOF_5: differential (hierarchical) extended sequential, Huffman
                    case 0xffc6: // SOF_6: differential (hierarchical) progressive, Huffman
                    case 0xffc7: // SOF_7: differential (hierarchical) lossless, Huffman
                        ms.Seek(2, SeekOrigin.Current);
                        return br.ReadByte();
                    case 0xffc8: // Reserved for JPEG extentions
                        ms.Seek(br.ReadUInt16() - 2, SeekOrigin.Current);
                        break;
                    case 0xffc9: // SOF_9: extended sequential, arithmetic
                    case 0xffca: // SOF_10: progressive, arithmetic
                    case 0xffcb: // SOF_11: lossless, arithmetic
                    case 0xffcd: // SOF_13: differential (hierarchical) extended sequential, arithmetic
                    case 0xffce: // SOF_14: differential (hierarchical) progressive, arithmetic
                    case 0xffcf: // SOF_15: differential (hierarchical) lossless, arithmetic
                        ms.Seek(2, SeekOrigin.Current);
                        return br.ReadByte();
                    case 0xffc4: // DHT
                    case 0xffcc: // DAC
                        ms.Seek(br.ReadUInt16() - 2, SeekOrigin.Current);
                        break;
                    case 0xffd0: // RST m
                    case 0xffd1:
                    case 0xffd2:
                    case 0xffd3:
                    case 0xffd4:
                    case 0xffd5:
                    case 0xffd6:
                    case 0xffd7:
                    case 0xffd8: // SOI
                    case 0xffd9: // EOI
                        break;
                    case 0xffda: // SOS
                    case 0xffdb: // DQT
                    case 0xffdc: // DNL
                    case 0xffdd: // DRI
                    case 0xffde: // DHP
                    case 0xffdf: // EXP
                    case 0xffe0: // APPn
                    case 0xffe1:
                    case 0xffe2:
                    case 0xffe3:
                    case 0xffe4:
                    case 0xffe5:
                    case 0xffe6:
                    case 0xffe7:
                    case 0xffe8:
                    case 0xffe9:
                    case 0xffea:
                    case 0xffeb:
                    case 0xffec:
                    case 0xffed:
                    case 0xffee:
                    case 0xffef:
                    case 0xfff0: // JPGn
                    case 0xfff1:
                    case 0xfff2:
                    case 0xfff3:
                    case 0xfff4:
                    case 0xfff5:
                    case 0xfff6:
                    case 0xfff7:
                    case 0xfff8:
                    case 0xfff9:
                    case 0xfffa:
                    case 0xfffb:
                    case 0xfffc:
                    case 0xfffd:
                    case 0xfffe: // COM
                        ms.Seek(br.ReadUInt16() - 2, SeekOrigin.Current);
                        break;
                    case 0xff01: // TEM
                        break;
                    default:
                        int b1 = br.ReadByte();
                        int b2 = br.ReadByte();
                        if (b1 == 0xff && b2 > 2 && b2 <= 0xbf) // RES reserved markers
                            break;
                        throw new DicomCodecException("Unable to determine bit depth: JPEG syntax error!");
                }
            }
            throw new DicomCodecException("Unable to determine bit depth: no JPEG SOF marker found!");
        }
    }
}