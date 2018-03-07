using System.Collections.Generic;
using Android.Content;
using Android.Views;
using OpenCV.Android;
using OpenCV.Core;
using Pyraminx.App.Views;
using Pyraminx.Scanner;
using Utils = Pyraminx.Common.Utils;

namespace Pyraminx.App.Misc
{
    public delegate void OnScanResult(IEnumerable<Facelet> facelets);

    public class CameraViewAdapter : BaseLoaderCallback, PortraitCameraBridgeViewBase.ICvCameraViewListener2
    {
        protected Context Context;
        protected PortraitCameraView View;
        protected IFaceScanner Scanner;

        public event OnScanResult OnScanResult;
        public bool EnableProcessing { get; set; }

        public CameraViewAdapter(Context context, PortraitCameraView view) : base(context)
        {
            Context = context;
            View = view;
            View.ScaleFactor = 0.5;
            View.SetCvCameraViewListener(this);
            View.EnableFpsMeter();
        }

        public void Start()
        {
            Utils.Log("CameraViewAdapter.Start");
            if (!OpenCVLoader.InitDebug())
            {
                Utils.Log("Internal OpenCV library not found. Using OpenCV Manager for initialization");
                OpenCVLoader.InitAsync(OpenCVLoader.OpencvVersion300, Context, this);
            }
            else
            {
                Utils.Log("OpenCV library found inside package. Using it!");
                OnManagerConnected(LoaderCallbackInterface.Success);
            }
        }

        public void Stop()
        {
            Utils.Log("CameraViewAdapter.Stop");
            View.Visibility = ViewStates.Gone;
            View.DisableView();
        }

        public override void OnManagerConnected(int status)
        {
            switch (status)
            {
                case LoaderCallbackInterface.Success:
                    Utils.Log("OpenCV loaded successfully");
                    View.Visibility = ViewStates.Visible;
                    View.EnableView();
                    break;
                default:
                    base.OnManagerConnected(status);
                    break;
            }
        }

        public void OnCameraViewStarted(int width, int height)
        {
            Utils.Log("CameraViewAdapter.OnCameraViewStarted");
            Scanner = new StaticFaceScanner(new Size(width, height));
        }

        public void OnCameraViewStopped()
        {
            Utils.Log("CameraViewAdapter.OnCameraViewStopped");
            Scanner?.Dispose();
        }

        public Mat OnCameraFrame(CameraBridgeViewBase.ICvCameraViewFrame input)
        {
            var rgba = input.Rgba();

            if (EnableProcessing)
            {
                var result = Scanner.Process(rgba);
                OnScanResult?.Invoke(result);
            }

            return rgba;
        }
    }
}