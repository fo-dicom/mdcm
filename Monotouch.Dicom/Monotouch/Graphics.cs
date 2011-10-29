using System;

namespace System.Drawing
{
	public sealed class Graphics : IDisposable
	{
		#region CONSTRUCTORS
		
		private Graphics()
		{
		}
		
		#endregion

		#region IDisposable implementation
		
		public void Dispose()
		{
		}
		
		#endregion
		
		#region METHODS
		
		public void DrawImage(Image image, int x, int y)
		{
		}
		
		#endregion
		
		#region STATIC METHODS
		
		public static Graphics FromImage(Image image)
		{
			return new Graphics();
		}
		
		#endregion
	}
}

