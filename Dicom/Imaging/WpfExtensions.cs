using System;
using System.Windows.Media;

namespace Dicom.Imaging {
	public static class WpfExtensions {
		public static int ToArgb(this Color color) {
			return (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
		}
	}
}
