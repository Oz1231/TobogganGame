using System.Drawing;

namespace TobogganGame
{
    /// <summary>
    /// Information about a ray cast
    /// </summary>
    public class RayInfo
    {
        public Point Start { get; set; }
        public Point End { get; set; }
        public RayHitType HitType { get; set; }
        public double Distance { get; set; }

        /// <summary>
        /// Creates a new ray cast information
        /// </summary>
        public RayInfo(Point start, Point end, RayHitType hitType, double distance)
        {
            Start = start;
            End = end;
            HitType = hitType;
            Distance = distance;
        }
    }
}