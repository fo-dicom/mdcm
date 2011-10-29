// Copyright (c) 2011 Anders Gustafsson, Cureos AB.
// All rights reserved. This software and the accompanying materials
// are made available under the terms of the Eclipse Public License v1.0
// which accompanies this distribution, and is available at
// http://www.eclipse.org/legal/epl-v10.html

namespace System.Drawing
{
    public struct Point
    {
        #region MEMBERS

        private int _x;
        private int _y;

        #endregion

        #region FIELDS

        public static readonly Point Empty;

        #endregion
        
        #region CONSTRUCTORS

        static Point()
        {
            Empty = new Point(0, 0);
        }

        public Point(int x, int y)
        {
            _x = x;
            _y = y;
        }

        #endregion

        #region PROPERTIES

        public int X
        {
            get { return _x; }
            set { _x = value; }
        }

        public int Y
        {
            get { return _y; }
            set { _y = value; }
        }

        #endregion

        #region METHODS

        public bool Equals(Point other)
        {
            return other._x == _x && other._y == _y;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != typeof(Point)) return false;
            return Equals((Point)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_x * 397) ^ _y;
            }
        }

        #endregion
        
        #region OPERATORS

        public static bool operator==(Point lhs, Point rhs)
        {
            return lhs._x == rhs._x && lhs._y == rhs._y;
        }

        public static bool operator !=(Point lhs, Point rhs)
        {
            return lhs._x != rhs._x || lhs._y != rhs._y;
        }

        #endregion
    }
}