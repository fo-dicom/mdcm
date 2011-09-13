using System;

namespace System.Drawing
{
	public struct Color
	{
		#region PRIVATE MEMBERS
		
		private byte _a;
		private byte _r;
		private byte _g;
		private byte _b;
		
		#endregion
		
		#region CONSTRUCTORS
		
		private Color(int a, int r, int g, int b)
		{
			_a = Convert.ToByte(a);
			_r = Convert.ToByte(r);
			_g = Convert.ToByte(g);
			_b = Convert.ToByte(b);
		}
		
		#endregion
		
		#region PROPERTIES
		
		public byte A { get { return _a; } }
		
		public byte R { get { return _r; } }
		
		public byte G { get { return _g; } }
		
		public byte B { get { return _b; } }
		
		#endregion
		
		#region METHODS
		
        public int ToArgb()
        {
            return (_a << 0x18) + (_r << 0x10) + (_g << 0x8) + _b;
        }
		
		#endregion
		
		#region STATIC METHODS
		
		public static Color FromArgb(int r, int g, int b)
		{
			return new Color(0xff, r, g, b);
		}
		
		#endregion
	}
}

