// Copyright (c) 2011 Anders Gustafsson, Cureos AB.
// All rights reserved. This software and the accompanying materials
// are made available under the terms of the Eclipse Public License v1.0
// which accompanies this distribution, and is available at
// http://www.eclipse.org/legal/epl-v10.html

using System.Windows.Media.Imaging;

namespace System.Drawing
{
    public class Bitmap : Image
    {
        private int _width;
        private int _height;
        private int _stride;
        private Imaging.PixelFormat _format;
        private IntPtr _scan0;

        public Bitmap(int width, int height, int stride, Imaging.PixelFormat format, IntPtr scan0) :
            base(new WriteableBitmap(width, height))
        {
            // TODO: Complete member initialization
            _width = width;
            _height = height;
            _stride = stride;
            _format = format;
            _scan0 = scan0;
        }
    }
}