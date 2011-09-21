// Copyright (c) 2011 Anders Gustafsson, Cureos AB.
// All rights reserved. This software and the accompanying materials
// are made available under the terms of the Eclipse Public License v1.0
// which accompanies this distribution, and is available at
// http://www.eclipse.org/legal/epl-v10.html

using System;
using System.IO;
using Dicom.Data;
using FluxJpeg.Core;
using FluxJpeg.Core.Decoder;
using FluxJpeg.Core.Encoder;

namespace Dicom.Codec
{
    [DicomCodec]
    public class DcmJpegCodec : IDcmCodec
    {
        #region Implementation of IDcmCodec

        public string GetName()
        {
            return GetTransferSyntax().UID.Description;
        }

        public DicomTransferSyntax GetTransferSyntax()
        {
            return DicomTransferSyntax.JPEGProcess2_4;
        }

        public DcmCodecParameters GetDefaultParameters()
        {
            return new DcmJpegParameters();
        }

        public void Encode(DcmDataset dataset, DcmPixelData oldPixelData, DcmPixelData newPixelData, DcmCodecParameters parameters)
        {
            var jpegParams = parameters as DcmJpegParameters ?? GetDefaultParameters() as DcmJpegParameters;

            for (int i = 0; i < oldPixelData.NumberOfFrames; i++)
            {
                // Init buffer in FluxJpeg format
                int w = oldPixelData.ImageWidth;
                int h = oldPixelData.ImageHeight;
                byte[][,] pixelsForJpeg = new byte[3][,]; // RGB colors
                pixelsForJpeg[0] = new byte[w, h];
                pixelsForJpeg[1] = new byte[w, h];
                pixelsForJpeg[2] = new byte[w, h];

                // Copy data into buffer for FluxJpeg
                byte[] p = oldPixelData.GetFrameDataU8(i);
                int j = 0;
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        byte color = p[j++];
                        pixelsForJpeg[0][x, y] = color; // R
                        pixelsForJpeg[1][x, y] = color; // G
                        pixelsForJpeg[2][x, y] = color; // B
                    }
                }

                // Encode Image as JPEG using the FluxJpeg library
                // and write to destination stream
                ColorModel cm = new ColorModel { colorspace = ColorSpace.RGB };
                Image jpegImage = new Image(cm, pixelsForJpeg);
                using (MemoryStream destinationStream = new MemoryStream())
                {
                    JpegEncoder encoder = new JpegEncoder(jpegImage, jpegParams.Quality, destinationStream);
                    encoder.Encode();
                    newPixelData.AddFrame(destinationStream.GetBuffer());
                }
            }
        }

        public void Decode(DcmDataset dataset, DcmPixelData oldPixelData, DcmPixelData newPixelData, DcmCodecParameters parameters)
        {
            var frameData = new byte[newPixelData.UncompressedFrameSize];

            try
            {
                for (int j = 0; j < oldPixelData.NumberOfFrames; ++j)
                {
                    var jpegStream = new MemoryStream(oldPixelData.GetFrameDataU8(j));

                    // Decode JPEG from stream
                    var decoder = new JpegDecoder(jpegStream);
                    var jpegDecoded = decoder.Decode();
                    var img = jpegDecoded.Image;
                    img.ChangeColorSpace(ColorSpace.RGB);

                    // Init Buffer
                    int w = img.Width;
                    int h = img.Height;
                    byte[][,] pixelsFromJpeg = img.Raster;

                    // Copy FluxJpeg buffer into WriteableBitmap
                    int i = 0;
                    for (int y = 0; y < h; ++y)
                    {
                        for (int x = 0; x < w; ++x)
                        {
                            frameData[i++] = 0xff;  // A
                            frameData[i++] = pixelsFromJpeg[0][x, y];   // R
                            frameData[i++] = pixelsFromJpeg[1][x, y];   // G
                            frameData[i++] = pixelsFromJpeg[2][x, y];   // B
                        }
                    }

                    newPixelData.AddFrame(frameData);
                }
            }
            catch (Exception e)
            {
                Debug.Log.Error("Failed to decode JPEG image, reason: {0}", e.Message);
            }
        }

        #endregion

        public static void Register()
        {
            DicomCodec.RegisterCodec(DicomTransferSyntax.JPEGProcess2_4, typeof(DcmJpegCodec));
        }
    }
}