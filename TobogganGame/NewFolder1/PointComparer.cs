using System;
using System.Collections.Generic;
using System.Drawing;

namespace TobogganGame
{
    /// <summary>
    /// Helper class to compare points for the HashSet
    /// </summary>
    /// 
    [Serializable]
    public class PointComparer : IEqualityComparer<Point>
    {
        public bool Equals(Point a, Point b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        public int GetHashCode(Point point)
        {
            //xor
            return point.X.GetHashCode() ^ point.Y.GetHashCode(); 
        }
    }
}