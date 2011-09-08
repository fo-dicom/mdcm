#region Header
//
//   Project:           WriteableBitmapEx - Silverlight WriteableBitmap extensions
//   Description:       Collection of transformation extension methods for the Silverlight WriteableBitmap class.
//
//   Changed by:        $Author$
//   Changed on:        $Date$
//   Changed in:        $Revision$
//   Project:           $URL$
//   Id:                $Id$
//
//
//   Copyright © 2009-2010 Rene Schulte and WriteableBitmapEx Contributors
//
//   This Software is weak copyleft open source. Please read the License.txt for details.
//
#endregion

namespace System.Windows.Media.Imaging
{
   /// <summary>
   /// Collection of transformation extension methods for the Silverlight WriteableBitmap class.
   /// </summary>
   public static partial class WriteableBitmapExtensions
   {
      #region Enums

      /// <summary>
      /// The interpolation method.
      /// </summary>
      public enum Interpolation
      {
         /// <summary>
         /// The nearest neighbor algorithm simply selects the color of the nearest pixel.
         /// </summary>
         NearestNeighbor = 0,

         /// <summary>
         /// Linear interpolation in 2D using the average of 3 neighboring pixels.
         /// </summary>
         Bilinear,
      }

      /// <summary>
      /// The mode for flipping.
      /// </summary>
      public enum FlipMode
      {
         /// <summary>
         /// Flips the image vertical (around the center of the y-axis).
         /// </summary>
         Vertical,

         /// <summary>
         /// Flips the image horizontal (around the center of the x-axis).
         /// </summary>
         Horizontal
      }

      #endregion

      #region Methods

      #region Crop

      /// <summary>
      /// Creates a new cropped WriteableBitmap.
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="x">The x coordinate of the rectangle that defines the crop region.</param>
      /// <param name="y">The y coordinate of the rectangle that defines the crop region.</param>
      /// <param name="width">The width of the rectangle that defines the crop region.</param>
      /// <param name="height">The height of the rectangle that defines the crop region.</param>
      /// <returns>A new WriteableBitmap that is a cropped version of the input.</returns>
      public static WriteableBitmap Crop(this WriteableBitmap bmp, int x, int y, int width, int height)
      {
         var srcWidth = bmp.PixelWidth;
         var srcHeight = bmp.PixelHeight;

         // If the rectangle is completly out of the bitmap
         if (x > srcWidth || y > srcHeight)
         {
            return new WriteableBitmap(0, 0);
         }

         // Clamp to boundaries
         if (x < 0) x = 0;
         if (x + width > srcWidth) width = srcWidth - x;
         if (y < 0) y = 0;
         if (y + height > srcHeight) height = srcHeight - y;

         // Copy the pixels line by line using fast BlockCopy
         var result = new WriteableBitmap(width, height);
         for (var line = 0; line < height; line++)
         {
            var srcOff = ((y + line) * srcWidth + x) * SizeOfArgb;
            var dstOff = line * width * SizeOfArgb;
            Buffer.BlockCopy(bmp.Pixels, srcOff, result.Pixels, dstOff, width * SizeOfArgb);
         }
         return result;
      }

      /// <summary>
      /// Creates a new cropped WriteableBitmap.
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="region">The rectangle that defines the crop region.</param>
      /// <returns>A new WriteableBitmap that is a cropped version of the input.</returns>
      public static WriteableBitmap Crop(this WriteableBitmap bmp, Rect region)
      {
         return bmp.Crop((int)region.X, (int)region.Y, (int)region.Width, (int)region.Height);
      }

      #endregion

      #region Resize

      /// <summary>
      /// Creates a new resized WriteableBitmap.
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="width">The new desired width.</param>
      /// <param name="height">The new desired height.</param>
      /// <param name="interpolation">The interpolation method that should be used.</param>
      /// <returns>A new WriteableBitmap that is a resized version of the input.</returns>
      public static WriteableBitmap Resize(this WriteableBitmap bmp, int width, int height, Interpolation interpolation)
      {
         var pd = Resize(bmp.Pixels, bmp.PixelWidth, bmp.PixelHeight, width, height, interpolation);

         var result = new WriteableBitmap(width, height);
         Buffer.BlockCopy(pd, 0, result.Pixels, 0, SizeOfArgb * pd.Length);
         return result;
      }

      /// <summary>
      /// Creates a new resized bitmap.
      /// </summary>
      /// <param name="pixels">The source pixels.</param>
      /// <param name="widthSource">The width of the source pixels.</param>
      /// <param name="heightSource">The height of the source pixels.</param>
      /// <param name="width">The new desired width.</param>
      /// <param name="height">The new desired height.</param>
      /// <param name="interpolation">The interpolation method that should be used.</param>
      /// <returns>A new bitmap that is a resized version of the input.</returns>
      public static int[] Resize(int[] pixels, int widthSource, int heightSource, int width, int height, Interpolation interpolation)
      {
         var pd = new int[width * height];
         var xs = (float)widthSource / width;
         var ys = (float)heightSource / height;

         float fracx, fracy, ifracx, ifracy, sx, sy, l0, l1, rf, gf, bf;
         int c, x0, x1, y0, y1;
         byte c1a, c1r, c1g, c1b, c2a, c2r, c2g, c2b, c3a, c3r, c3g, c3b, c4a, c4r, c4g, c4b;
         byte a, r, g, b;

         // Nearest Neighbor
         if (interpolation == Interpolation.NearestNeighbor)
         {
            var srcIdx = 0;
            for (var y = 0; y < height; y++)
            {
               for (var x = 0; x < width; x++)
               {
                  sx = x * xs;
                  sy = y * ys;
                  x0 = (int)sx;
                  y0 = (int)sy;

                  pd[srcIdx++] = pixels[y0 * widthSource + x0];
               }
            }
         }

            // Bilinear
         else if (interpolation == Interpolation.Bilinear)
         {
            var srcIdx = 0;
            for (var y = 0; y < height; y++)
            {
               for (var x = 0; x < width; x++)
               {
                  sx = x * xs;
                  sy = y * ys;
                  x0 = (int)sx;
                  y0 = (int)sy;

                  // Calculate coordinates of the 4 interpolation points
                  fracx = sx - x0;
                  fracy = sy - y0;
                  ifracx = 1f - fracx;
                  ifracy = 1f - fracy;
                  x1 = x0 + 1;
                  if (x1 >= widthSource)
                  {
                     x1 = x0;
                  }
                  y1 = y0 + 1;
                  if (y1 >= heightSource)
                  {
                     y1 = y0;
                  }


                  // Read source color
                  c = pixels[y0 * widthSource + x0];
                  c1a = (byte)(c >> 24);
                  c1r = (byte)(c >> 16);
                  c1g = (byte)(c >> 8);
                  c1b = (byte)(c);

                  c = pixels[y0 * widthSource + x1];
                  c2a = (byte)(c >> 24);
                  c2r = (byte)(c >> 16);
                  c2g = (byte)(c >> 8);
                  c2b = (byte)(c);

                  c = pixels[y1 * widthSource + x0];
                  c3a = (byte)(c >> 24);
                  c3r = (byte)(c >> 16);
                  c3g = (byte)(c >> 8);
                  c3b = (byte)(c);

                  c = pixels[y1 * widthSource + x1];
                  c4a = (byte)(c >> 24);
                  c4r = (byte)(c >> 16);
                  c4g = (byte)(c >> 8);
                  c4b = (byte)(c);


                  // Calculate colors
                  // Alpha
                  l0 = ifracx * c1a + fracx * c2a;
                  l1 = ifracx * c3a + fracx * c4a;
                  a = (byte)(ifracy * l0 + fracy * l1);

                  // Red
                  l0 = ifracx * c1r * c1a + fracx * c2r * c2a;
                  l1 = ifracx * c3r * c3a + fracx * c4r * c4a;
                  rf = ifracy * l0 + fracy * l1;

                  // Green
                  l0 = ifracx * c1g * c1a + fracx * c2g * c2a;
                  l1 = ifracx * c3g * c3a + fracx * c4g * c4a;
                  gf = ifracy * l0 + fracy * l1;

                  // Blue
                  l0 = ifracx * c1b * c1a + fracx * c2b * c2a;
                  l1 = ifracx * c3b * c3a + fracx * c4b * c4a;
                  bf = ifracy * l0 + fracy * l1;

                  // Divide by alpha
                  if (a > 0)
                  {
                     rf = rf / a;
                     gf = gf / a;
                     bf = bf / a;
                  }

                  // Cast to byte
                  r = (byte)rf;
                  g = (byte)gf;
                  b = (byte)bf;

                  // Write destination
                  pd[srcIdx++] = (a << 24) | (r << 16) | (g << 8) | b;
               }
            }
         }
         return pd;
      }

      #endregion

      #region Rotate

      /// <summary>
      /// Rotates the bitmap in 90° steps clockwise and returns a new rotated WriteableBitmap.
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="angle">The angle in degress the bitmap should be rotated in 90° steps clockwise.</param>
      /// <returns>A new WriteableBitmap that is a rotated version of the input.</returns>
      public static WriteableBitmap Rotate(this WriteableBitmap bmp, int angle)
      {
         // Use refs for faster access (really important!) speeds up a lot!
         var w = bmp.PixelWidth;
         var h = bmp.PixelHeight;
         var p = bmp.Pixels;
         var i = 0;
         WriteableBitmap result = null;
         angle %= 360;

         if (angle > 0 && angle <= 90)
         {
            result = new WriteableBitmap(h, w);
            var rp = result.Pixels;
            for (var x = 0; x < w; x++)
            {
               for (var y = h - 1; y >= 0; y--)
               {
                  var srcInd = y * w + x;
                  rp[i] = p[srcInd];
                  i++;
               }
            }
         }
         else if (angle > 90 && angle <= 180)
         {
            result = new WriteableBitmap(w, h);
            var rp = result.Pixels;
            for (var y = h - 1; y >= 0; y--)
            {
               for (var x = w - 1; x >= 0; x--)
               {
                  var srcInd = y * w + x;
                  rp[i] = p[srcInd];
                  i++;
               }
            }
         }
         else if (angle > 180 && angle <= 270)
         {
            result = new WriteableBitmap(h, w);
            var rp = result.Pixels;
            for (var x = w - 1; x >= 0; x--)
            {
               for (var y = 0; y < h; y++)
               {
                  var srcInd = y * w + x;
                  rp[i] = p[srcInd];
                  i++;
               }
            }
         }
         else
         {
            result = bmp.Clone();
         }
         return result;
      }

      #endregion

      #region Flip

      /// <summary>
      /// Flips (reflects the image) eiter vertical or horizontal.
      /// </summary>
      /// <param name="bmp">The WriteableBitmap.</param>
      /// <param name="flipMode">The flip mode.</param>
      /// <returns>A new WriteableBitmap that is a flipped version of the input.</returns>
      public static WriteableBitmap Flip(this WriteableBitmap bmp, FlipMode flipMode)
      {
         // Use refs for faster access (really important!) speeds up a lot!
         var w = bmp.PixelWidth;
         var h = bmp.PixelHeight;
         var p = bmp.Pixels;
         var i = 0;
         WriteableBitmap result = null;

         if (flipMode == FlipMode.Horizontal)
         {
            result = new WriteableBitmap(w, h);
            var rp = result.Pixels;
            for (var y = h - 1; y >= 0; y--)
            {
               for (var x = 0; x < w; x++)
               {
                  var srcInd = y * w + x;
                  rp[i] = p[srcInd];
                  i++;
               }
            }
         }
         else if (flipMode == FlipMode.Vertical)
         {
            result = new WriteableBitmap(w, h);
            var rp = result.Pixels;
            for (var y = 0; y < h; y++)
            {
               for (var x = w - 1; x >= 0; x--)
               {
                  var srcInd = y * w + x;
                  rp[i] = p[srcInd];
                  i++;
               }
            }
         }

         return result;
      }

      #endregion

      #endregion
   }
}