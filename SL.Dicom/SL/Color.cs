// Copyright (c) 2011 Anders Gustafsson, Cureos AB.
// All rights reserved. This software and the accompanying materials
// are made available under the terms of the Eclipse Public License v1.0
// which accompanies this distribution, and is available at
// http://www.eclipse.org/legal/epl-v10.html

namespace System.Drawing
{
    public struct Color
    {
        #region MEMBERS

        private readonly Windows.Media.Color _color;

        #endregion

        #region CONSTRUCTORS

        private Color(byte a, byte r, byte g, byte b)
        {
            _color = Windows.Media.Color.FromArgb(a, r, g, b);
        }

        #endregion

        #region PROPERTIES

        public Windows.Media.Color Self
        {
            get { return _color; }
        }

        internal byte A
        {
            get { return _color.A; }
        }

        internal byte R
        {
            get { return _color.R; }
        }

        internal byte G
        {
            get { return _color.G; }
        }

        internal byte B
        {
            get { return _color.B; }
        }

        #endregion

        internal static Color FromArgb(byte r, byte g, byte b)
        {
            return new Color(0xff, r, g, b);
        }

        internal int ToArgb()
        {
            return A << 0x18 + R << 0x10 + G << 0x8 + B;
        }
    }
}