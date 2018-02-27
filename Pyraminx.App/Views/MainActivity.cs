using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using OpenCV.Android;
using OpenCV.Core;
using Pyraminx.Scanner;
//using Pyraminx.Solver;
using Utils = Pyraminx.Common.Utils;

namespace Pyraminx.App.Views
{
    [Activity(Label = "pmx", MainLauncher = true)]
    public class MainActivity : BaseActivity, PortraitCameraBridgeViewBase.ICvCameraViewListener2
    {
        protected override string Prefix => "MainActivity>";
        protected override int Layout => Resource.Layout.Main;
        protected override string Header => "Pyraminx Solver";

        protected PortraitCameraBridgeViewBase CameraView;
        protected LoaderCallback Callback;
        protected bool PauseAndProcess = false;
        //protected Mat ProcessedFrame = null;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            CameraView = FindViewById<PortraitCameraBridgeViewBase>(Resource.Id.CameraView);
            CameraView.Visibility = ViewStates.Visible;
            CameraView.SetCvCameraViewListener(this);
            CameraView.EnableFpsMeter();

            Callback = new LoaderCallback(this, CameraView);

            var flash = FindViewById<Button>(Resource.Id.CameraFlashBtn);
            flash.Click += async (sender, args) =>
            {
                //var p = Core.Pyraminx.CreateSolved();
                //Utils.Log(p.ToString());
                //p.Turn("w+");
                //Utils.Log(p.ToString());
                //Utils.Log(p.Serialize());

                //var folder = Environment.ExternalStorageDirectory.Path;
                //var path = Path.Combine(folder, "solutions.db");
                //var solver = new BidirectionalSearchSolver { DatabasePath = path };

                //var solution = await solver.FindSolution(p.Serialize());
                //Utils.Log(solution);
            };

            var start = FindViewById<Button>(Resource.Id.CameraProcessBtn);
            start.Click += (sender, args) =>
            {
                lock (this)
                {
                    if (!PauseAndProcess)
                    {
                        PauseAndProcess = true;
                    }
                    else
                    {
                        PauseAndProcess = false;
                        //ProcessedFrame?.Release();
                        //ProcessedFrame = null;
                    }
                }
            };

            var ctrl = FindViewById<Button>(Resource.Id.RobotControlBtn);
            ctrl.Click += (sender, args) =>
            {
                StartActivity(typeof(ControlActivity));
            };
        }

        protected override void OnPause()
        {
            CameraView?.DisableView();
            base.OnPause();
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (!OpenCVLoader.InitDebug())
            {
                Utils.Log("Internal OpenCV library not found. Using OpenCV Manager for initialization");
                OpenCVLoader.InitAsync(OpenCVLoader.OpencvVersion300, this, Callback);
            }
            else
            {
                Utils.Log("OpenCV library found inside package. Using it!");
                Callback.OnManagerConnected(LoaderCallbackInterface.Success);
            }
        }

        protected override void OnDestroy()
        {
            CameraView?.DisableView();
            base.OnDestroy();
        }

        public void OnCameraViewStarted(int width, int height)
        {
            Utils.Log("OnCameraViewStarted");
        }

        public void OnCameraViewStopped()
        {
            Utils.Log("OnCameraViewStopped");
        }

        public Mat OnCameraFrame(CameraBridgeViewBase.ICvCameraViewFrame input)
        {
            var rgba = input.Rgba();

            if(PauseAndProcess)
            {
                using (var scanner = new StaticFaceScanner(rgba.Size()))
                {
                    var result = scanner.Process(rgba);
                    var str = string.Join(", ", result.Select(x => x.Matches.FirstOrDefault()?.Label));
                    RunOnUiThread(() => Utils.Log("Result: " + str));
                }
            }

            //lock(this)
            //{
            //    if(PauseAndProcess)
            //    {
            //        if(ProcessedFrame == null)
            //        {
            //            ProcessedFrame = rgba.Clone();
            //            var result = Scanner.Process(ProcessedFrame);
            //            var str = string.Join(", ", result.Select(x => x.Matches.First().Label));
            //            Utils.Toast("Result: " + str);
            //        }

            //        return ProcessedFrame;
            //    }
            //}

            return rgba;
        }
    }

    public class LoaderCallback : BaseLoaderCallback
    {
        protected readonly PortraitCameraBridgeViewBase View;

        public LoaderCallback(Context context, PortraitCameraBridgeViewBase view) : base(context)
        {
            View = view;
        }

        public override void OnManagerConnected(int status)
        {
            switch (status)
            {
                case LoaderCallbackInterface.Success:
                    Utils.Log("OpenCV loaded successfully");
                    View.EnableView();
                    break;
                default:
                    base.OnManagerConnected(status);
                    break;
            }
        }
    }
}