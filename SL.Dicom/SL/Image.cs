// Copyright (c) 2011 Anders Gustafsson, Cureos AB.
// All rights reserved. This software and the accompanying materials
// are made available under the terms of the Eclipse Public License v1.0
// which accompanies this distribution, and is available at
// http://www.eclipse.org/legal/epl-v10.html

using System.Drawing.Imaging;
using System.Windows.Media.Imaging;

namespace System.Drawing
{
    public abstract class Image
    {
        #region CONSTRUCTORS

        protected Image(BitmapSource image)
        {
            Object = image;
        }

        #endregion
        
        #region PROPERTIES

        public BitmapSource Object { get; private set; }

        internal ColorPalette Palette { get; set; }

        #endregion

        #region METHODS

        internal void RotateFlip(RotateFlipType rotateFlipType)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}