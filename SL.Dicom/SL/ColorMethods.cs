// Copyright (c) 2011 Anders Gustafsson, Cureos AB.
// All rights reserved. This software and the accompanying materials
// are made available under the terms of the Eclipse Public License v1.0
// which accompanies this distribution, and is available at
// http://www.eclipse.org/legal/epl-v10.html

namespace System.Windows.Media
{
    public static class ColorMethods
    {
        public static int ToArgb(this Color iColor)
        {
            return iColor.A << 0x18 + iColor.R << 0x10 + iColor.G << 0x8 + iColor.B;
        }
    }
}