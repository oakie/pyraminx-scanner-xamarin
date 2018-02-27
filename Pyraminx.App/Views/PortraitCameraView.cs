using System.Linq;
using System.Threading;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Java.Lang;
using OpenCV.Android;
using OpenCV.Core;
using OpenCV.ImgProc;
using Pyraminx.App.Misc;
using Camera = Android.Hardware.Camera;
using Exception = System.Exception;
using Math = System.Math;
using Size = OpenCV.Core.Size;
using Thread = Java.Lang.Thread;
using Utils = Pyraminx.Common.Utils;

namespace Pyraminx.App.Views
{
    /**
     * This class is an implementation of the Bridge View between OpenCV and Java Camera.
     * This class relays on the functionality available in base class and only implements
     * required functions:
     * connectCamera - opens Java camera and sets the PreviewCallback to be delivered.
     * disconnectCamera - closes the camera and stops preview.
     * When frame is delivered via callback from Camera - it processed via OpenCV to be
     * converted to RGBA32 and then passed to the external callback for modifications if required.
     */
    public class PortraitCameraView : PortraitCameraBridgeViewBase, Camera.IPreviewCallback
    {
        private const int MAGIC_TEXTURE_ID = 10;

        protected byte[] ByteBuffer;
        protected Mat TmpPreviewFrame;
        protected int BufferIndex = 0;
        protected InputFrame[] FrameBuffer;
        protected InputFrame FrontBuffer => FrameBuffer[BufferIndex];
        protected InputFrame BackBuffer => FrameBuffer[1 - BufferIndex];

        protected Thread WorkerThread;
        protected bool StopWorkerThread;
        protected object SyncObject = new object();
        protected bool CameraFrameReady = false;

        protected Camera SelectedCamera;
        protected SurfaceTexture SurfaceTexture;

        public PortraitCameraView(Context context, int cameraId) : base(context, cameraId) { }

        public PortraitCameraView(Context context, IAttributeSet attrs) : base(context, attrs) { }

        protected bool InitializeCamera(int width, int height)
        {
            Utils.Log("Initialize java camera");
            lock (SyncObject)
            {
                SelectedCamera = CameraSelector.GetCamera(CameraIndex);
                if (SelectedCamera == null)
                    return false;

                /* Now set camera parameters */
                try
                {
                    var p = SelectedCamera.GetParameters();
                    Utils.Log("getSupportedPreviewSizes()");
                    var sizes = p.SupportedPreviewSizes;
                    if (sizes == null)
                        return false;

                    /* Select the size that fits surface considering maximum size allowed */
                    Size frameSize = CalculateCameraFrameSize(sizes.Select(x => new Size(x.Width, x.Height)), width, height);

                    p.PreviewFormat = ImageFormatType.Nv21;
                    Utils.Log("Set preview size to " + (int)frameSize.Width + "x" + (int)frameSize.Height);
                    p.SetPreviewSize((int)frameSize.Width, (int)frameSize.Height);

                    if (Build.VERSION.SdkInt >= BuildVersionCodes.IceCreamSandwich)
                        p.SetRecordingHint(true);

                    var focus = p.SupportedFocusModes;
                    if (focus != null && focus.Contains(Camera.Parameters.FocusModeContinuousVideo))
                    {
                        p.FocusMode = Camera.Parameters.FocusModeContinuousVideo;
                    }

                    p.FlashMode = Camera.Parameters.FlashModeTorch;

                    SelectedCamera.SetParameters(p);
                    p = SelectedCamera.GetParameters();

                    FrameWidth = p.PreviewSize.Width;
                    FrameHeight = p.PreviewSize.Height;

                    if ((LayoutParameters.Width == ViewGroup.LayoutParams.MatchParent) && (LayoutParameters.Height == ViewGroup.LayoutParams.MatchParent))
                        mScale = Math.Min(((float)height) / FrameWidth, ((float)width) / FrameHeight);
                    else
                        mScale = 0;

                    FpsMeter?.SetResolution(FrameHeight, FrameWidth);

                    int size = FrameWidth * FrameHeight;
                    size = size * ImageFormat.GetBitsPerPixel(p.PreviewFormat) / 8;
                    ByteBuffer = new byte[size];
                    SelectedCamera.AddCallbackBuffer(ByteBuffer);
                    SelectedCamera.SetPreviewCallbackWithBuffer(this);

                    AllocateCache();

                    TmpPreviewFrame = new Mat((int)(FrameHeight * 1.5), FrameWidth, CvType.Cv8uc1);
                    FrameBuffer = new InputFrame[2];
                    FrameBuffer[0] = new InputFrame(FrameHeight, FrameWidth);
                    FrameBuffer[1] = new InputFrame(FrameHeight, FrameWidth);

                    if (Build.VERSION.SdkInt >= BuildVersionCodes.Honeycomb)
                    {
                        SurfaceTexture = new SurfaceTexture(MAGIC_TEXTURE_ID);
                        SelectedCamera.SetPreviewTexture(SurfaceTexture);
                    }
                    else
                        SelectedCamera.SetPreviewDisplay(null);

                    /* Finally we are ready to start the preview */
                    Utils.Log("startPreview");
                    SelectedCamera.StartPreview();
                    return true;
                }
                catch (Exception e)
                {
                    Utils.Log(e.ToString());
                }
            }

            return false;
        }

        protected void ReleaseCamera()
        {
            lock (SyncObject)
            {
                if (SelectedCamera != null)
                {
                    SelectedCamera.StopPreview();
                    SelectedCamera.SetPreviewCallback(null);

                    SelectedCamera.Release();
                }
                SelectedCamera = null;
                if (FrameBuffer != null)
                {
                    FrameBuffer[0].Img?.Release();
                    FrameBuffer[1].Img?.Release();
                    FrameBuffer[0].Dispose();
                    FrameBuffer[1].Dispose();
                }
            }
        }

        protected override bool ConnectCamera(int width, int height)
        {

            /* 1. We need to instantiate camera
             * 2. We need to start thread which will be getting frames
             */
            /* First step - initialize camera connection */
            Utils.Log("Connecting to camera");
            if (!InitializeCamera(width, height))
                return false;

            CameraFrameReady = false;

            /* now we can start update thread */
            Utils.Log("Starting processing thread");
            StopWorkerThread = false;
            WorkerThread = new Thread(CameraWorker);
            WorkerThread.Start();

            return true;
        }

        protected override void DisconnectCamera()
        {
            /* 1. We need to stop thread which updating the frames
             * 2. Stop camera and release it
             */
            Utils.Log("Disconnecting from camera");
            try
            {
                StopWorkerThread = true;
                Utils.Log("Notify thread");
                lock (SyncObject)
                {
                    Monitor.Pulse(SyncObject);
                }
                Utils.Log("Wating for thread");
                if (WorkerThread != null)
                    WorkerThread.Join();
            }
            catch (InterruptedException e)
            {
                Utils.Log(e.ToString());
            }
            finally
            {
                WorkerThread = null;
            }

            /* Now release camera */
            ReleaseCamera();

            CameraFrameReady = false;
        }

        protected void SwapBuffers()
        {
            BufferIndex = 1 - BufferIndex;
        }

        public void OnPreviewFrame(byte[] frame, Camera arg1)
        {
            lock (SyncObject)
            {
                TmpPreviewFrame.Put(0, 0, frame);
                Imgproc.CvtColor(TmpPreviewFrame, FrontBuffer.Img, Imgproc.ColorYuv2rgbaNv21);
                
                CameraFrameReady = true;
                Monitor.Pulse(SyncObject);
            }

            SelectedCamera?.AddCallbackBuffer(ByteBuffer);
        }

        protected void CameraWorker()
        {
            do
            {
                lock (SyncObject)
                {
                    try
                    {
                        while (!CameraFrameReady && !StopWorkerThread)
                        {
                            Monitor.Wait(SyncObject);
                        }
                    }
                    catch (InterruptedException e)
                    {
                        Utils.Log(e.ToString());
                    }
                    if (CameraFrameReady)
                        SwapBuffers();
                }

                if (!StopWorkerThread && CameraFrameReady)
                {
                    CameraFrameReady = false;
                    if (!BackBuffer.Img.Empty())
                        DeliverAndDrawFrame(BackBuffer);
                }
            } while (!StopWorkerThread);
            Utils.Log("Finish processing thread");
        }

        protected class InputFrame : Java.Lang.Object, CameraBridgeViewBase.ICvCameraViewFrame
        {
            public Mat Img { get; protected set; }

            public InputFrame(int rows, int cols)
            {
                Img = new Mat(rows, cols, CvType.Cv8uc4);
            }

            public Mat Gray()
            {
                if (Img == null)
                    return null;

                var gray = Mat.Zeros(Img.Size(), CvType.Cv8uc1);
                Imgproc.CvtColor(Img, gray, Imgproc.ColorRgba2gray);
                return gray;
            }

            public Mat Rgba()
            {
                return Img;
            }
        };
    }
}