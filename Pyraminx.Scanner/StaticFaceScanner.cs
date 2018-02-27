using System;
using System.Collections.Generic;
using System.Linq;
using Android.Runtime;
using OpenCV.ImgProc;
using FaceColor = Pyraminx.Core.FaceColor;

namespace Pyraminx.Scanner
{
    using OpenCV.Core;

    public class StaticFaceScanner : IFaceScanner
    {
        protected Mat WorkMat { get; set; }
        protected List<Facelet> Facelets = Facelet.GenerateFacelets();

        public StaticFaceScanner(Size resolution)
        {
            WorkMat = new Mat(resolution, CvType.Cv8uc3);
        }

        public IEnumerable<Facelet> Process(Mat rgba)
        {
            Imgproc.CvtColor(rgba, WorkMat, Imgproc.ColorRgba2rgb);
            Imgproc.CvtColor(WorkMat, WorkMat, Imgproc.ColorRgb2hsv);

            var src = new MatOfPoint2f(Facelet.Anchors[0], Facelet.Anchors[1], Facelet.Anchors[2]);
            var dst = new MatOfPoint2f(Facelet.WarpedAnchors[0], Facelet.WarpedAnchors[1], Facelet.WarpedAnchors[2]);
            var warp = Imgproc.GetAffineTransform(src, dst);

            var centers = new MatOfPoint2f(Facelets.SelectMany(x => x.Corners).ToArray());
            var warped = new MatOfPoint2f();
            Core.Transform(centers, warped, warp);
            var points = warped.ToList();
            for (int i = 0; i < Facelets.Count; ++i)
                Facelets[i].WarpedCorners = new List<Point> { points[3 * i], points[3 * i + 1], points[3 * i + 2] };

            foreach (var facelet in Facelets)
            {
                var mask = new Mat(rgba.Size(), CvType.Cv8uc1);
                Imgproc.Circle(mask, facelet.WarpedCenter, 20, new Scalar(255));
                facelet.Color = Core.Mean(rgba, mask);
            }

            foreach (var facelet in Facelets)
            {
                facelet.Matches = ColorRange.Ranges.Values
                    .Select(x => new ColorMatch { Label = x.Label, Score = x.Score(facelet.Color) })
                    .OrderByDescending(x => x.Score).ToList();
                for (int i = 1; i < facelet.Matches.Count; ++i)
                    facelet.Matches[i].Degradation = facelet.Matches[i - 1].Score - facelet.Matches[i].Score;
            }

            foreach (var facelet in Facelets)
            {
                var color = ColorRange.Ranges[facelet.Matches[0].Label.Value].Rgba;
                Imgproc.Circle(rgba, facelet.WarpedCenter, 20, color, -1);
                Imgproc.Circle(rgba, facelet.WarpedCenter, 20, new Scalar(255, 255, 255, 255), 2);
            }

            Imgproc.Line(rgba, Facelet.WarpedAnchors[0], Facelet.WarpedAnchors[1], new Scalar(255, 255, 0, 255));
            Imgproc.Line(rgba, Facelet.WarpedAnchors[1], Facelet.WarpedAnchors[2], new Scalar(255, 255, 0, 255));
            Imgproc.Line(rgba, Facelet.WarpedAnchors[2], Facelet.WarpedAnchors[0], new Scalar(255, 255, 0, 255));

            return Facelets;
        }

        public void Dispose()
        {
            WorkMat?.Release();
        }
    }
}
