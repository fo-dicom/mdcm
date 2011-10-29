using System;

namespace System.Drawing.Imaging
{
	public sealed class ColorPalette
	{
		#region CONSTRUCTORS
		
		public ColorPalette()
		{
		}
		
		#endregion
		
		#region PROPERTIES
		
		public Color[] Entries { get; private set; }
		
		#endregion
	}
}

