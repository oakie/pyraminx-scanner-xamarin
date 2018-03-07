using System;
using OpenCV.Core;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Linq;
using OpenCV.ImgProc;
using Pyraminx.Common;
using Pyraminx.Core;

namespace Pyraminx.Scanner
{
    public class ColorRange
    {
        public FaceColor Label { get; set; }
        public Scalar Lower { get; set; }
        public Scalar Upper { get; set; }
        public Scalar Rgba { get; set; }

        public static readonly ColorRange Blue = new ColorRange
        {
            Label = FaceColor.Blue,
            Lower = new Scalar(90, 80, 60, 255),
            Upper = new Scalar(150, 255, 255, 255),
            Rgba = new Scalar(0, 0, 255, 255)
        };
        public static readonly ColorRange Yellow = new ColorRange
        {
            Label = FaceColor.Yellow,
            Lower = new Scalar(30, 80, 60, 255),
            Upper = new Scalar(50, 255, 255, 255),
            Rgba = new Scalar(255, 255, 0, 255)
        };
        public static readonly ColorRange Orange = new ColorRange
        {
            Label = FaceColor.Orange,
            Lower = new Scalar(0, 80, 100, 255),
            Upper = new Scalar(30, 255, 255, 255),
            Rgba = new Scalar(255, 127, 0, 255)
        };
        public static readonly ColorRange Green = new ColorRange
        {
            Label = FaceColor.Green,
            Lower = new Scalar(50, 80, 60, 255),
            Upper = new Scalar(90, 255, 255, 255),
            Rgba = new Scalar(0, 255, 0, 255)
        };

        /// <summary>
        /// Probability of the color in rgba belonging to this range
        /// </summary>
        /// <param name="rgba"></param>
        /// <returns></returns>
        public double Score(Scalar rgba)
        {
            var mu = (Upper.Val[0] + Lower.Val[0]) / 2;
            var dev = (Upper.Val[0] - Lower.Val[0]) / 2;
            var x = (Rgb2Hsv(Rgba2Rgb(rgba)).Val[0] - mu) / dev;

            var x2 = -0.5 * x * x;
            var p = Math.Exp(x2) / Math.Sqrt(2 * Math.PI);
            var score = 100 * p / dev;

            return score;
        }

        public static Dictionary<int, ColorRange> Ranges = new Dictionary<int, ColorRange> {
            { FaceColor.Blue.Value, Blue },
            { FaceColor.Yellow.Value, Yellow },
            { FaceColor.Orange.Value, Orange },
            { FaceColor.Green.Value, Green }
        };

        public static Scalar Rgba2Rgb(Scalar rgba)
        {
            return new Scalar(rgba.Val.Take(3).ToArray());
        }

        public static Scalar Rgb2Hsv(Scalar rgb)
        {
            var mat = new Mat(1, 1, CvType.Cv8uc3, rgb);
            var hsv = new Mat(1, 1, CvType.Cv8uc3);
            Imgproc.CvtColor(mat, hsv, Imgproc.ColorRgb2hsv);
            return new Scalar(hsv.Get(0, 0));
        }
    }

    public class ColorMatch
    {
        public FaceColor Label { get; set; }
        public double Score { get; set; }
        public double Degradation { get; set; }
    }
}
