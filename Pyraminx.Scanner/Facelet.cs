using System;
using System.Collections.Generic;
using System.Linq;
using OpenCV.Core;

namespace Pyraminx.Scanner
{
    public class Facelet
    {
        public static readonly List<Point> Vertices = new List<Point>
        {
            new Point(0, -4 * Math.Sqrt(3)),

            new Point(-2, -2 * Math.Sqrt(3)),
            new Point(2, -2 * Math.Sqrt(3)),

            new Point(-4, 0),
            new Point(0, 0),
            new Point(4, 0),

            new Point(-6, 2 * Math.Sqrt(3)),
            new Point(-2, 2 * Math.Sqrt(3)),
            new Point(2, 2 * Math.Sqrt(3)),
            new Point(6, 2 * Math.Sqrt(3))
        };

        public static List<Point> Anchors = new List<Point> { Vertices[0], Vertices[6], Vertices[9] };
        public static List<Point> WarpedAnchors = new List<Point> {
            new Point(100.0 / 1080, 220.0 / 1080),
            new Point(510.0 / 1080, 820.0 / 1080),
            new Point(800.0 / 1080, 180.0 / 1080)
        };

        public List<Point> Corners { get; protected set; }
        public List<Point> WarpedCorners { get; set; }

        public Point Center => AveragePoint(Corners);
        public Point WarpedCenter => AveragePoint(WarpedCorners);

        public Scalar Color { get; set; }
        public List<ColorMatch> Matches { get; set; }

        public static List<Facelet> GenerateFacelets()
        {
            return GenerateTriangles().Select(x => new Facelet {Corners = x}).ToList();
        }

        public static List<List<Point>> GenerateTriangles()
        {
            var list = new List<List<Point>>
            {
                new List<Point> { Vertices[0], Vertices[1], Vertices[2] },

                new List<Point> { Vertices[1], Vertices[3], Vertices[4] },
                new List<Point> { Vertices[1], Vertices[2], Vertices[4] },
                new List<Point> { Vertices[2], Vertices[4], Vertices[5] },

                new List<Point> { Vertices[3], Vertices[6], Vertices[7] },
                new List<Point> { Vertices[3], Vertices[4], Vertices[7] },
                new List<Point> { Vertices[4], Vertices[7], Vertices[8] },
                new List<Point> { Vertices[4], Vertices[5], Vertices[8] },
                new List<Point> { Vertices[5], Vertices[8], Vertices[9] }
            };

            return list;
        }

        protected Point AveragePoint(List<Point> points)
        {
            return new Point(points.Sum(p => p.X) / points.Count, points.Sum(p => p.Y) / points.Count);
        }
    }
}