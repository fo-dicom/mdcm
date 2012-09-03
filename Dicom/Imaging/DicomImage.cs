using System;
using System.Collections.Generic;
#if !SILVERLIGHT
using System.Drawing;
using System.Drawing.Imaging;
#endif
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
		private const int OverlayColor = unchecked((int)0xffff00ff);

		private IPixelData _pixelData;
		private IPipeline _pipeline;

		private DcmOverlayData[] _overlays;
		#endregion

		/// <summary>Creates DICOM image object from dataset</summary>
		/// <param name="dataset">Source dataset</param>
		public DicomImage(DcmDataset dataset) {
			Load(dataset);
		}

#if !SILVERLIGHT
		/// <summary>Creates DICOM image object from file</summary>
		/// <param name="fileName">Source file</param>
		public DicomImage(string fileName) {
			DicomFileFormat ff = new DicomFileFormat();
			ff.Load(fileName, DicomReadOptions.Default);
			Load(ff.Dataset);
		}
#endif

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
#if !SILVERLIGHT
		public Image RenderImage()
		{
			ImageGraphic graphic = new ImageGraphic(_pixelData);

			foreach (var overlay in _overlays) {
				OverlayGraphic og = new OverlayGraphic(PixelDataFactory.Create(overlay), overlay.OriginX, overlay.OriginY, OverlayColor);
				graphic.AddOverlay(og);
			}

			return graphic.RenderImage(_pipeline.LUT);
		}
#endif

		public ImageSource RenderImageSource() {
			ImageGraphic graphic = new ImageGraphic(_pixelData);

			foreach (var overlay in _overlays) {
				OverlayGraphic og = new OverlayGraphic(PixelDataFactory.Create(overlay), overlay.OriginX, overlay.OriginY, OverlayColor);
				graphic.AddOverlay(og);
			}

			return graphic.RenderImageSource(_pipeline.LUT);
		}

		private void Load(DcmDataset dataset) {
			Dataset = dataset;
			if (Dataset.InternalTransferSyntax.IsEncapsulated)
				Dataset.ChangeTransferSyntax(DicomTransferSyntax.ExplicitVRLittleEndian, null);
			DcmPixelData pixelData = new DcmPixelData(Dataset);
			_pixelData = PixelDataFactory.Create(pixelData, 0);
			_pipeline = PipelineFactory.Create(Dataset, pixelData);
			pixelData.Unload();

			_overlays = DcmOverlayData.FromDataset(Dataset);
		}
	}
}
