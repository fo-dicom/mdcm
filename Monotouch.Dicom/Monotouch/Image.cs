using System;
using System.Drawing.Imaging;

namespace System.Drawing
{
	public class Image
	{
		#region CONSTRUCTORS
		
		protected Image ()
		{
		}
		
		#endregion
		
		#region PROPERTIES
		
		public ColorPalette Palette { get; set; }
		
		#endregion
		
		#region METHODS
		
		public void RotateFlip(RotateFlipType rotateFlipType)
		{
		}
		
		#endregion
	}
}

