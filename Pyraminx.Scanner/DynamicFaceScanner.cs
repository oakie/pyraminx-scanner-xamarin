using System;
using System.Collections.Generic;
using Android.Runtime;
using OpenCV.ImgProc;
using Pyraminx.Core;

namespace Pyraminx.Scanner
{
    using OpenCV.Core;

    public class DynamicFaceScanner : IFaceScanner
    {
        protected Mat WorkMat { get; set; }
        protected Dictionary<FaceColor, Mat> Masks = new Dictionary<FaceColor, Mat>();

        public DynamicFaceScanner(Size resolution)
        {
            WorkMat = new Mat(resolution, CvType.Cv8uc3);
            Masks[FaceColor.Blue] = new Mat(resolution, CvType.Cv8uc1);
            Masks[FaceColor.Yellow] = new Mat(resolution, CvType.Cv8uc1);
            Masks[FaceColor.Orange] = new Mat(resolution, CvType.Cv8uc1);
            Masks[FaceColor.Green] = new Mat(resolution, CvType.Cv8uc1);
        }

        public IEnumerable<Facelet> Process(Mat rgba)
        {
            //{
            //    Imgproc.CvtColor(rgba, WorkMat, Imgproc.ColorRgba2rgb);
            //    Imgproc.CvtColor(WorkMat, WorkMat, Imgproc.ColorRgb2hsv);

            //    var contours = new Dictionary<FaceColor, JavaList<MatOfPoint>>();
            //    var centers = new List<Point>();
            //    foreach (var color in ColorRange.Colors)
            //    {
            //        GenerateColorMask(WorkMat, color);
            //        contours[color] = ComputeContours(Masks[color]);
            //        Utils.Log($"Found {contours[color].Count} {color} contours.");
            //        FilterContours(contours[color]);
            //        Utils.Log($"Found {contours[color].Count} {color} (filtered) contours.");
            //        centers.AddRange(ComputeFaceCenters(contours[color]));
            //    }

            //    Utils.Log($"Found {contours.Values.Sum(x => x.Count)} total contours.");

            //    foreach (var color in ColorRange.Colors)
            //        Imgproc.DrawContours(rgba, contours[color], -1, ColorRange.Ranges[color].Rgba, -1);
            //    foreach (var p in centers)
            //        Imgproc.Circle(rgba, p, 10, new Scalar(255, 255, 255, 255), -1);

            return new List<Facelet>();
        }

        protected void GenerateColorMask(Mat input, FaceColor color)
        {
            var mask = Masks[color];
            var range = ColorRange.Ranges[color.Value];
            Core.InRange(input, range.Lower, range.Upper, mask);
            Imgproc.Blur(mask.Clone(), mask, new Size(5, 5));
        }

        protected JavaList<MatOfPoint> ComputeContours(Mat mask)
        {
            var contours = new JavaList<MatOfPoint>();
            Imgproc.FindContours(mask.Clone(), contours, new Mat(), Imgproc.RetrList, Imgproc.ChainApproxSimple);
            return contours;
        }

        protected void FilterContours(IList<MatOfPoint> contours)
        {
            var width = WorkMat.Size().Width;
            for (int i = contours.Count - 1; i >= 0; --i)
            {
                var f = new MatOfPoint2f(contours[i].ToArray());
                var perimeter = Imgproc.ArcLength(f, true);
                if (perimeter < 0.05 * width)
                {
                    contours.RemoveAt(i);
                    continue;
                }

                var approx = new MatOfPoint2f();
                Imgproc.ApproxPolyDP(f, approx, 0.05 * width, true);
                if (approx.Rows() != 3)
                {
                    contours.RemoveAt(i);
                }
            }
        }

        protected IList<Point> ComputeFaceCenters(JavaList<MatOfPoint> contours)
        {
            var centers = new List<Point>();
            float[] f = new float[100];
            foreach (var point in contours)
            {
                Point center = new Point();
                Imgproc.MinEnclosingCircle(new MatOfPoint2f(point.ToArray()), center, f);
                centers.Add(center);
            }

            return centers;
        }

        public void Dispose()
        {
            WorkMat?.Release();

            foreach (var mask in Masks.Values)
            {
                mask.Release();
            }

            //if (Masks.ContainsKey(FaceColor.Blue))
            //    Masks[FaceColor.Blue]?.Release();
            //if (Masks.ContainsKey(FaceColor.Yellow))
            //    Masks[FaceColor.Yellow]?.Release();
            //if (Masks.ContainsKey(FaceColor.Orange))
            //    Masks[FaceColor.Orange]?.Release();
            //if (Masks.ContainsKey(FaceColor.Green))
            //    Masks[FaceColor.Green]?.Release();
            Masks.Clear();
        }
    }
}