
using System;
using System.Collections.Generic;
using OpenCV.Core;

namespace Pyraminx.Scanner
{
    public interface IFaceScanner : IDisposable
    {
        IEnumerable<Facelet> Process(Mat rgba);
    }
}