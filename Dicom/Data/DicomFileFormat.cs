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

using Dicom.Codec;
using Dicom.IO;
using Dicom.Utility;

namespace Dicom.Data {
	/// <summary>
	/// User class for loading and saving DICOM files
	/// </summary>
	public class DicomFileFormat {
		#region Private Members
		private DcmFileMetaInfo _metainfo;
		private DcmDataset _dataset;
		#endregion

		/// <summary>
		/// Initializes new DICOM file format
		/// </summary>
		public DicomFileFormat() {
		}

		/// <summary>
		/// Initializes new DICOM file format from dataset
		/// </summary>
		/// <param name="dataset">Dataset</param>
		public DicomFileFormat(DcmDataset dataset) {
			_metainfo = new DcmFileMetaInfo();
			_metainfo.FileMetaInformationVersion = DcmFileMetaInfo.Version;
			_metainfo.MediaStorageSOPClassUID = dataset.GetUID(DicomTags.SOPClassUID);
			_metainfo.MediaStorageSOPInstanceUID = dataset.GetUID(DicomTags.SOPInstanceUID);
			_metainfo.TransferSyntax = dataset.InternalTransferSyntax;
			_metainfo.ImplementationClassUID = Implementation.ClassUID;
			_metainfo.ImplementationVersionName = Implementation.Version;
			_metainfo.SourceApplicationEntityTitle = "";
			_dataset = dataset;
		}

		/// <summary>
		/// File Meta Information
		/// </summary>
		public DcmFileMetaInfo FileMetaInfo {
			get {
				if (_metainfo == null)
					_metainfo = new DcmFileMetaInfo();
				return _metainfo;
			}
		}

		/// <summary>
		/// DICOM Dataset
		/// </summary>
		public DcmDataset Dataset {
			get { return _dataset; }
		}

		/// <summary>
		/// Changes transfer syntax of dataset and updates file meta information
		/// </summary>
		/// <param name="ts">New transfer syntax</param>
		/// <param name="parameters">Encode/Decode params</param>
		public void ChangeTransferSytnax(DicomTransferSyntax ts, DcmCodecParameters parameters) {
			Dataset.ChangeTransferSyntax(ts, parameters);
			FileMetaInfo.TransferSyntax = ts;
		}

		/// <summary>
		/// Gets the file meta information from a DICOM file
		/// </summary>
		/// <param name="file">Filename</param>
		/// <returns>File meta information</returns>
		public static DcmFileMetaInfo LoadFileMetaInfo(String file) {
			using (FileStream fs = File.OpenRead(file)) {
				fs.Seek(128, SeekOrigin.Begin);
				CheckFileHeader(fs);
				DicomStreamReader dsr = new DicomStreamReader(fs);
				DcmFileMetaInfo metainfo = new DcmFileMetaInfo();
				dsr.Dataset = metainfo;
				dsr.Read(DcmFileMetaInfo.StopTag, DicomReadOptions.Default | DicomReadOptions.FileMetaInfoOnly);
				fs.Close();
				return metainfo;
			}
		}

		/// <summary>
		/// Loads a dicom file
		/// </summary>
		/// <param name="file">Filename</param>
		/// <param name="options">DICOM read options</param>
		public DicomReadStatus Load(String file, DicomReadOptions options) {
            using (FileStream fs = File.OpenRead(file))
            {
                return Load(fs, null, options);
            }
        }

		/// <summary>
		/// Loads a dicom file, stopping at a certain tag
		/// </summary>
		/// <param name="file">Filename</param>
		/// <param name="stopTag">Tag to stop parsing at</param>
		/// <param name="options">DICOM read options</param>
		public DicomReadStatus Load(String file, DicomTag stopTag, DicomReadOptions options) {
			using (FileStream fs = File.OpenRead(file))
			{
			    return Load(fs, stopTag, options);
			}
		}

        /// <summary>
        /// Loads a dicom file
        /// </summary>
        /// <param name="fs">File stream to read</param>
        /// <param name="options">DICOM read options</param>
        public DicomReadStatus Load(FileStream fs, DicomReadOptions options)
        {
            return Load(fs, null, options);
        }

        /// <summary>
	    /// Loads a dicom file, stopping at a certain tag
	    /// </summary>
	    /// <param name="fs">File stream to read</param>
	    /// <param name="stopTag">Tag to stop parsing at</param>
	    /// <param name="options">DICOM read options</param>
        public DicomReadStatus Load(FileStream fs, DicomTag stopTag, DicomReadOptions options)
	    {
	        if (fs.CanSeek && fs.CanRead)
	        {
	            fs.Seek(128, SeekOrigin.Begin);
	            CheckFileHeader(fs);
	            DicomStreamReader dsr = new DicomStreamReader(fs);

	            _metainfo = new DcmFileMetaInfo();
	            dsr.Dataset = _metainfo;
	            dsr.Read(DcmFileMetaInfo.StopTag, options | DicomReadOptions.FileMetaInfoOnly);

#if !SILVERLIGHT
				if (_metainfo.TransferSyntax.IsDeflate) {
					MemoryStream ms = StreamUtility.Deflate(fs, false);
					dsr = new DicomStreamReader(ms);
				}
#endif

	            _dataset = new DcmDataset(_metainfo.TransferSyntax);
	            dsr.Dataset = _dataset;
	            DicomReadStatus status = dsr.Read(stopTag, options);

	            fs.Close();

	            return status;
	        }
	        return DicomReadStatus.UnknownError;
	    }

	    public static bool IsDicomFile(string file)
        {
			bool isDicom = false;
			using (FileStream fs = File.OpenRead(file)) {
				fs.Seek(128, SeekOrigin.Begin);
				if (fs.ReadByte() == (byte)'D' ||
					fs.ReadByte() == (byte)'I' ||
					fs.ReadByte() == (byte)'C' ||
					fs.ReadByte() == (byte)'M')
					isDicom = true;
				fs.Close();
			}
			return isDicom;
		}

		private static void CheckFileHeader(FileStream fs) {
			if (fs.ReadByte() != (byte)'D' ||
				fs.ReadByte() != (byte)'I' ||
				fs.ReadByte() != (byte)'C' ||
				fs.ReadByte() != (byte)'M')
				throw new DicomDataException("Invalid DICOM file: " + fs.Name);
		}

		/// <summary>
		/// Gets file stream starting at DICOM dataset
		/// </summary>
		/// <param name="file">Filename</param>
		/// <returns>File stream</returns>
		public static FileStream GetDatasetStream(String file) {
			FileStream fs = File.OpenRead(file);
			fs.Seek(128, SeekOrigin.Begin);
			CheckFileHeader(fs);
			DicomStreamReader dsr = new DicomStreamReader(fs);
			DcmFileMetaInfo metainfo = new DcmFileMetaInfo();
			dsr.Dataset = metainfo;
			if (dsr.Read(DcmFileMetaInfo.StopTag, DicomReadOptions.Default | DicomReadOptions.FileMetaInfoOnly) == DicomReadStatus.Success && fs.Position < fs.Length) {
				return fs;
			}
			fs.Close();
			return null;
		}

		/// <summary>
		/// Saves a DICOM file
		/// </summary>
		/// <param name="file">Filename</param>
		/// <param name="options">DICOM write options</param>
		public void Save(String file, DicomWriteOptions options) {
			// expand to full path
			file = Path.GetFullPath(file);

			string dir = Path.GetDirectoryName(file);
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			using (FileStream fs = File.Create(file)) {
				fs.Seek(128, SeekOrigin.Begin);
				fs.WriteByte((byte)'D');
				fs.WriteByte((byte)'I');
				fs.WriteByte((byte)'C');
				fs.WriteByte((byte)'M');

				DicomStreamWriter dsw = new DicomStreamWriter(fs);
				dsw.Write(_metainfo, options | DicomWriteOptions.CalculateGroupLengths);
				if (_dataset != null)
					dsw.Write(_dataset, options);

				fs.Close();
			}
		}
	}
}
