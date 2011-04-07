using System;
using System.Collections.Generic;
#if SILVERLIGHT
using System.Windows.Media;
using System.Windows.Media.Imaging;
#else
using System.Drawing;
using System.Drawing.Imaging;
#endif
using System.Text;
using Dicom;
using Dicom.Data;
using Dicom.Imaging.LUT;
using Dicom.Imaging.Process;
using Dicom.Imaging.Render;

namespace Dicom.Imaging {
	/// <summary>
	/// DICOM Image
	/// </summary>
	public class DicomImage {
		#region Private Members
		private IPixelData _pixelData;
		private IPipeline _pipeline;
		#endregion

		/// <summary>Creates DICOM image object from dataset</summary>
		/// <param name="dataset">Source dataset</param>
		public DicomImage(DcmDataset dataset) {
			Load(dataset);
		}

		/// <summary>Creates DICOM image object from file</summary>
		/// <param name="fileName">Source file</param>
		public DicomImage(string fileName) {
			DicomFileFormat ff = new DicomFileFormat();
			ff.Load(fileName, DicomReadOptions.Default);
			Load(ff.Dataset);
		}

		/// <summary>Source DICOM dataset</summary>
		public DcmDataset Dataset {
			get;
			private set;
		}

		/// <summary>Width of image in pixels</summary>
		public int Width {
			get { return _pixelData.Width; }
		}

		/// <summary>Height of image in pixels</summary>
		public int Height {
			get { return _pixelData.Height; }
		}

		/// <summary>Renders DICOM image to System.Drawing.Image</summary>
		/// <returns>Rendered image</returns>
#if SILVERLIGHT
        public ImageSource Render()
#else
        public Image Render()
#endif
        {
			ImageGraphic graphic = new ImageGraphic(_pixelData);
			return graphic.RenderImage(_pipeline.LUT);
		}

		private void Load(DcmDataset dataset) {
			Dataset = dataset;
			if (Dataset.InternalTransferSyntax.IsEncapsulated)
				Dataset.ChangeTransferSyntax(DicomTransferSyntax.ExplicitVRLittleEndian, null);
			DcmPixelData pixelData = new DcmPixelData(Dataset);
			_pixelData = PixelDataFactory.Create(pixelData, 0);
			_pipeline = PipelineFactory.Create(Dataset, pixelData);
			pixelData.Unload();
		}
	}
}
