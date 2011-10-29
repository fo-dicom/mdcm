using System;
using System.Windows.Media;

using Dicom.Imaging.LUT;

namespace Dicom.Imaging.Process {
	public class GenericGrayscalePipeline : IPipeline {
		#region Private Members
		private CompositeLUT _lut;
		private RescaleLUT _rescaleLut;
		private VOILinearLUT _voiLut;
		private OutputLUT _outputLut;
		private bool _invert;
		#endregion

		#region Public Constructor
		public GenericGrayscalePipeline(double slope, double intercept, int bitsStored, bool signed) {
			int minValue = signed ? -(1 << (bitsStored - 1)) : 0;
			int maxValue = signed ? (1 << (bitsStored - 1)) : (1 << (bitsStored + 1) - 1);
			_rescaleLut = new RescaleLUT(minValue, maxValue, slope, intercept);
			_voiLut = new VOILinearLUT(new WindowLevel(maxValue - minValue, (minValue + maxValue) / 2));
			_outputLut = new OutputLUT(ColorTable.Monochrome2);
		}
		#endregion

		#region Public Properties
		public WindowLevel WindowLevel {
			get { return _voiLut.WindowLevel; }
			set { _voiLut.WindowLevel = value; }
		}

		public Color[] ColorMap {
			get { return _outputLut.ColorMap; }
			set { _outputLut.ColorMap = value; }
		}

		public bool Invert {
			get { return _invert; }
			set {
				_invert = value;
				_lut = null;
			}
		}

		public ILUT LUT {
			get {
				if (_lut == null) {
					CompositeLUT composite = new CompositeLUT();
					if (_rescaleLut != null)
						composite.Add(_rescaleLut);
					composite.Add(_voiLut);
					composite.Add(_outputLut);
					if (_invert)
						composite.Add(new InvertLUT(_outputLut.MinimumOutputValue, _outputLut.MaximumOutputValue));
					_lut = composite;
				}
				return _lut;
			}
		}
		#endregion
	}
}
