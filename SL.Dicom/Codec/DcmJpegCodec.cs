// Copyright (c) 2011 Anders Gustafsson, Cureos AB.
// All rights reserved. This software and the accompanying materials
// are made available under the terms of the Eclipse Public License v1.0
// which accompanies this distribution, and is available at
// http://www.eclipse.org/legal/epl-v10.html

using System;
using System.IO;
using Dicom.Data;
using FluxJpeg.Core.Decoder;

namespace Dicom.Codec
{
    public abstract class DcmJpegCodec : IDcmCodec
    {
        private readonly DicomTransferSyntax _transferSyntax;
        private readonly DcmJpegParameters _defaultParameters;

        #region CONSTRUCTORS

        protected DcmJpegCodec(DicomTransferSyntax transferSyntax, DcmJpegParameters defaultParameters = null)
        {
            _transferSyntax = transferSyntax;
            _defaultParameters = defaultParameters ?? new DcmJpegParameters();
        }

        #endregion

        #region Implementation of IDcmCodec

        public string GetName()
        {
            return _transferSyntax.UID.Description;
        }

        public DicomTransferSyntax GetTransferSyntax()
        {
            return _transferSyntax;
        }

        public DcmCodecParameters GetDefaultParameters()
        {
            return _defaultParameters;
        }

        public void Encode(DcmDataset dataset, DcmPixelData oldPixelData, DcmPixelData newPixelData, DcmCodecParameters parameters)
        {
            throw new NotSupportedException("JPEG encoding currently not supported");
        }

        public void Decode(DcmDataset dataset, DcmPixelData oldPixelData, DcmPixelData newPixelData, DcmCodecParameters parameters)
        {
            if (oldPixelData.NumberOfFrames == 0) return;

            // Determine JPEG image precision and assert that the implemented codec supports this precision
            int precision;
            try
            {
                precision = JpegHelper.ScanHeaderForBitDepth(oldPixelData);
            }
            catch (DicomCodecException)
            {
                precision = oldPixelData.BitsStored;
            }
            AssertImagePrecision(precision);

            // Ensure consistency in the new pixel data header
            if (precision > 8)
                newPixelData.BitsAllocated = 16;
            else if (newPixelData.BitsStored <= 8)
                newPixelData.BitsAllocated = 8;

            // Set up new pixel data specifics
            newPixelData.PhotometricInterpretation = newPixelData.PhotometricInterpretation.Equals("YBR_FULL_422") ||
                                                     newPixelData.PhotometricInterpretation.Equals("YBR_PARTIAL_422")
                                                         ? "YBR_FULL"
                                                         : oldPixelData.PhotometricInterpretation;
            if (newPixelData.PhotometricInterpretation.Equals("YBR_FULL")) newPixelData.PlanarConfiguration = 1;

            try
            {
                for (int j = 0; j < oldPixelData.NumberOfFrames; ++j)
                {
                    var frameData = new byte[newPixelData.UncompressedFrameSize];
                    var jpegStream = new MemoryStream(oldPixelData.GetFrameDataU8(j));

                    // Decode JPEG from stream
                    var decoder = new JpegDecoder(jpegStream);
                    var jpegDecoded = decoder.Decode();
                    var img = jpegDecoded.Image;

                    // Init Buffer
                    int w = img.Width;
                    int h = img.Height;
                    var pixelsFromJpeg = img.Raster;

                    // Copy FluxJpeg buffer into frame data array
/*
                    int comps = pixelsFromJpeg.GetLength(0);
                    int preIncr = newPixelData.BytesAllocated - comps;

                    if (preIncr < 0)
                        throw new InvalidOperationException(
                            String.Format("Number of JPEG components: {0} exceeds number of bytes allocated: {1}",
                                          comps, newPixelData.BytesAllocated));
*/
                    int i = 0;
                    for (int y = 0; y < h; ++y)
                    {
                        for (int x = 0; x < w; ++x)
                        {
                            var pixel = pixelsFromJpeg[0][x, y];
                            frameData[i++] = (byte)((pixel >> 8) & 0xff);
                            frameData[i++] = (byte)(pixel & 0xff);
//                            for (int k = 0; k < preIncr; ++k) frameData[i++] = 0xff;
//                            for (int k = 0; k < comps; ++k) frameData[i++] = pixelsFromJpeg[k][x, y];
                        }
                    }

                    oldPixelData.Unload();

                    if (newPixelData.IsPlanar)
                        DcmCodecHelper.ChangePlanarConfiguration(frameData,
                                                                 frameData.Length / newPixelData.BytesAllocated,
                                                                 newPixelData.BitsAllocated,
                                                                 newPixelData.SamplesPerPixel, 0);
                    newPixelData.AddFrame(frameData);
                }
            }
            catch (Exception e)
            {
                Debug.Log.Error("Failed to decode JPEG image: {0}, reason: {1}", e.StackTrace, e.Message);
            }
        }

        #endregion

        #region PROTECTED ABSTRACT METHODS

        protected abstract void AssertImagePrecision(int bits);

        #endregion
        
        #region STATIC METHODS

        public static void Register()
        {
            DicomCodec.RegisterCodec(DicomTransferSyntax.JPEGProcess1, typeof(DcmJpegProcess1Codec));
            DicomCodec.RegisterCodec(DicomTransferSyntax.JPEGProcess2_4, typeof(DcmJpegProcess4Codec));
        }

        #endregion
    }

    [DicomCodec]
    public class DcmJpegProcess1Codec : DcmJpegCodec
    {
        #region CONSTRUCTORS

        public DcmJpegProcess1Codec()
            : base(DicomTransferSyntax.JPEGProcess1)
        {
        }

        #endregion

        #region Implementation of DcmJpegCodec

        protected override void AssertImagePrecision(int bits)
        {
            if (bits != 8) throw new DicomCodecException(String.Format("Unable to create JPEG Process 1 codec for bits stored == {0}", bits));
        }

        #endregion
    }

    [DicomCodec]
    public class DcmJpegProcess4Codec : DcmJpegCodec
    {
        #region CONSTRUCTORS

        public DcmJpegProcess4Codec()
            : base(DicomTransferSyntax.JPEGProcess2_4)
        {
        }

        #endregion

        #region Implementation of DcmJpegCodec

        protected override void AssertImagePrecision(int bits)
        {
            if (bits != 8 && bits != 12)
                throw new DicomCodecException(
                    String.Format("Unable to create JPEG Process 2 & 4 codec for bits stored == {0}", bits));
        }

        #endregion
    }
}