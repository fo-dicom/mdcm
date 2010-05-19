using System;
using System.Drawing;

using Dicom.Imaging.LUT;

namespace Dicom.Imaging.Process {
	public class GenericGrayscalePipeline : IPipeline {
		#region Private Members
		private PrecalculatedLUT _lut;
		private VOILinearLUT _voiLut;
		private OutputLUT _outputLut;
		private bool _invert;
		#endregion

		#region Public Constructor
		public GenericGrayscalePipeline() {
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
					CompositeLUT composite = new CompositeLUT(0, 0);

				}
				return _lut;
			}
		}
		#endregion
	}
}
