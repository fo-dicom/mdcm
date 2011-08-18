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
#if SILVERLIGHT || WPF
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
#else
using System.Drawing;
#endif
using System.Text;
using Dicom.Imaging.LUT;

namespace Dicom.Imaging.Render {
	public class CompositeGraphic : IGraphic {
		#region Private Members
		private List<IGraphic> _layers = new List<IGraphic>();
		#endregion

		#region Public Constructor
		public CompositeGraphic(IGraphic bg) {
			_layers.Add(bg);
		}
		#endregion

		#region Public Properties
		public IGraphic BackgroundLayer {
			get { return _layers[0]; }
		}

		public int OriginalWidth {
			get { return BackgroundLayer.OriginalWidth; }
		}

		public int OriginalHeight {
			get { return BackgroundLayer.OriginalHeight; }
		}

		public int OriginalOffsetX {
			get { return 0; }
		}

		public int OriginalOffsetY {
			get { return 0; }
		}

		public double ScaleFactor {
			get { return BackgroundLayer.ScaleFactor; }
		}

		public int ScaledWidth {
			get { return BackgroundLayer.ScaledWidth; }
		}

		public int ScaledHeight {
			get { return BackgroundLayer.ScaledHeight; }
		}

		public int ScaledOffsetX {
			get { return 0; }
		}

		public int ScaledOffsetY {
			get { return 0; }
		}

		public int ZOrder {
			get { return 0; }
		}
		#endregion

		#region Public Members
		public void AddLayer(IGraphic layer) {
			_layers.Add(layer);
			_layers.Sort(delegate(IGraphic a, IGraphic b) {
				if (b.ZOrder > a.ZOrder)
					return 1;
				else if (a.ZOrder > b.ZOrder)
					return -1;
				else
					return 0;
			});
		}

		public void Reset() {
			foreach (IGraphic graphic in _layers)
				graphic.Reset();
		}

		public void Scale(double scale) {
			foreach (IGraphic graphic in _layers)
				graphic.Scale(scale);
		}

		public void BestFit(int width, int height) {
			foreach (IGraphic graphic in _layers)
				graphic.BestFit(width, height);
		}

		public void Rotate(int angle) {
			foreach (IGraphic graphic in _layers)
				graphic.Rotate(angle);
		}

		public void FlipX() {
			foreach (IGraphic graphic in _layers)
				graphic.FlipX();
		}

		public void FlipY() {
			foreach (IGraphic graphic in _layers)
				graphic.FlipY();
		}

		public void Transform(double scale, int rotation, bool flipx, bool flipy) {
			foreach (IGraphic graphic in _layers)
				graphic.Transform(scale, rotation, flipx, flipy);
		}
#if SILVERLIGHT
		public BitmapSource RenderImage(ILUT lut)
		{
			WriteableBitmap img = BackgroundLayer.RenderImage(lut) as WriteableBitmap;
			if (_layers.Count > 1)
			{
				throw new NotSupportedException("Multilayered images are not supported in Silverlight!");
			}
			return img;
		}
#elif WPF
		public BitmapSource RenderImage(ILUT lut)
		{
			WriteableBitmap img = BackgroundLayer.RenderImage(lut) as WriteableBitmap;
			if (img != null && _layers.Count > 1)
			{
				for (int i = 1; i < _layers.Count; ++i)
				{
					var g = _layers[i];
					var layer = _layers[i].RenderImage(null) as WriteableBitmap;

					if (layer != null)
					{
						Array pixels = new int[g.ScaledWidth * g.ScaledHeight];
						layer.CopyPixels(pixels, 4, 0);
						img.WritePixels(new Int32Rect(g.ScaledOffsetX, g.ScaledOffsetY, g.ScaledWidth, g.ScaledHeight),
										pixels, 4, 0);
					}
				}
			}
			return img;
		}
#else
		public Image RenderImage(ILUT lut) {
			Image img = BackgroundLayer.RenderImage(lut);
			if (_layers.Count > 1) {
				using (Graphics graphics = Graphics.FromImage(img)) {
					for (int i = 1; i < _layers.Count; i++) {
						Image layer = _layers[i].RenderImage(null);
						graphics.DrawImage(layer, _layers[i].ScaledOffsetX, _layers[i].ScaledOffsetY);
					}
				}
			}
			return img;
		}
#endif
		#endregion
	}
}
