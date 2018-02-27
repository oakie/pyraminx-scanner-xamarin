using System;
using System.Collections.Generic;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Util;
using Android.Views;
using OpenCV.Android;
using OpenCV.Core;
using Pyraminx.App.Misc;
using Rect = Android.Graphics.Rect;
using Size = OpenCV.Core.Size;
using Utils = Pyraminx.Common.Utils;

namespace Pyraminx.App.Views
{
    /**
     * This is a basic class, implementing the interaction with Camera and OpenCV library.
     * The main responsibility of it - is to control when camera can be enabled, process the frame,
     * call external listener to make any adjustments to the frame and then draw the resulting
     * frame to the screen.
     * The clients shall implement ICvCameraViewListener.
     */
    public abstract class PortraitCameraBridgeViewBase : SurfaceView, ISurfaceHolderCallback
    {
        private static readonly string TAG = "CameraBridge";
        private static readonly int MAX_UNSPECIFIED = -1;
        private const int STOPPED = 0;
        private const int STARTED = 1;

        private int mState = STOPPED;
        private Bitmap mCacheBitmap;
        private ICvCameraViewListener2 mListener;
        private bool mSurfaceExist;
        private Object mSyncObject = new Object();

        protected int FrameWidth;
        protected int FrameHeight;
        protected int mMaxHeight;
        protected int mMaxWidth;
        protected float mScale = 0;
        protected int CameraIndex = CameraSelector.CameraIdAny;
        protected bool mEnabled;
        protected FpsMeter FpsMeter = null;

        protected PortraitCameraBridgeViewBase(Context context, int cameraId) : base(context)
        {
            CameraIndex = cameraId;
            Holder.AddCallback(this);
            mMaxWidth = MAX_UNSPECIFIED;
            mMaxHeight = MAX_UNSPECIFIED;
        }

        protected PortraitCameraBridgeViewBase(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            int count = attrs.AttributeCount;
            Utils.Log("Attr count: " + count);

            TypedArray styledAttrs = Context.ObtainStyledAttributes(attrs, Resource.Styleable.CameraBridgeViewBase);
            if (styledAttrs.GetBoolean(Resource.Styleable.CameraBridgeViewBase_show_fps, false))
                EnableFpsMeter();

            CameraIndex = styledAttrs.GetInt(Resource.Styleable.CameraBridgeViewBase_camera_id, -1);

            Holder.AddCallback(this);
            mMaxWidth = MAX_UNSPECIFIED;
            mMaxHeight = MAX_UNSPECIFIED;
            styledAttrs.Recycle();
        }

        public interface ICvCameraViewListener2
        {
            /**
             * This method is invoked when camera preview has started. After this method is invoked
             * the frames will start to be delivered to client via the onCameraFrame() callback.
             * @param width -  the width of the frames that will be delivered
             * @param height - the height of the frames that will be delivered
             */
            void OnCameraViewStarted(int width, int height);

            /**
             * This method is invoked when camera preview has been stopped for some reason.
             * No frames will be delivered via onCameraFrame() callback after this method is called.
             */
            void OnCameraViewStopped();

            /**
             * This method is invoked when delivery of the frame needs to be done.
             * The returned values - is a modified frame which needs to be displayed on the screen.
             * TODO: pass the parameters specifying the format of the frame (BPP, YUV or RGB and etc)
             */
            Mat OnCameraFrame(CameraBridgeViewBase.ICvCameraViewFrame inputFrame);
        }

        public void SurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
        {
            Utils.Log("call surfaceChanged event");
            lock (mSyncObject)
            {
                if (!mSurfaceExist)
                {
                    mSurfaceExist = true;
                    CheckCurrentState();
                }
                else
                {
                    /* Surface changed. We need to stop camera and restart with new parameters */
                    /* Pretend that old surface has been destroyed */
                    mSurfaceExist = false;
                    CheckCurrentState();
                    /* Now use new surface. Say we have it now */
                    mSurfaceExist = true;
                    CheckCurrentState();
                }
            }
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            /* Do nothing. Wait until surfaceChanged delivered */
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            lock (mSyncObject)
            {
                mSurfaceExist = false;
                CheckCurrentState();
            }
        }

        /**
         * This method is provided for clients, so they can enable the camera connection.
         * The actual onCameraViewStarted callback will be delivered only after both this method is called and surface is available
         */
        public void EnableView()
        {
            lock (mSyncObject)
            {
                mEnabled = true;
                CheckCurrentState();
            }
        }

        /**
         * This method is provided for clients, so they can disable camera connection and stop
         * the delivery of frames even though the surface view itself is not destroyed and still stays on the scren
         */
        public void DisableView()
        {
            lock (mSyncObject)
            {
                mEnabled = false;
                CheckCurrentState();
            }
        }

        /**
         * This method enables label with fps value on the screen
         */
        public void EnableFpsMeter()
        {
            if (FpsMeter == null)
            {
                FpsMeter = new FpsMeter();
                FpsMeter.SetResolution(FrameWidth, FrameHeight);
            }
        }

        public void DisableFpsMeter()
        {
            FpsMeter = null;
        }

        /**
         *
         * @param listener
         */
        public void SetCvCameraViewListener(ICvCameraViewListener2 listener)
        {
            mListener = listener;
        }

        /**
         * This method sets the maximum size that camera frame is allowed to be. When selecting
         * size - the biggest size which less or equal the size set will be selected.
         * As an example - we set setMaxFrameSize(200,200) and we have 176x152 and 320x240 sizes. The
         * preview frame will be selected with 176x152 size.
         * This method is useful when need to restrict the size of preview frame for some reason (for example for video recording)
         * @param maxWidth - the maximum width allowed for camera frame.
         * @param maxHeight - the maximum height allowed for camera frame
         */
        public void SetMaxFrameSize(int maxWidth, int maxHeight)
        {
            mMaxWidth = maxWidth;
            mMaxHeight = maxHeight;
        }

        /**
         * Called when SyncObject lock is held
         */
        private void CheckCurrentState()
        {
            Utils.Log("call checkCurrentState");
            int targetState;

            if (mEnabled && mSurfaceExist && Visibility == ViewStates.Visible)
            {
                targetState = STARTED;
            }
            else
            {
                targetState = STOPPED;
            }

            if (targetState != mState)
            {
                /* The state change detected. Need to exit the current state and enter target state */
                ProcessExitState(mState);
                mState = targetState;
                ProcessEnterState(mState);
            }
        }

        private void ProcessEnterState(int state)
        {
            Utils.Log("call processEnterState: " + state);
            switch (state)
            {
                case STARTED:
                    OnEnterStartedState();
                    if (mListener != null)
                    {
                        mListener.OnCameraViewStarted(FrameWidth, FrameHeight);
                    }

                    break;
                case STOPPED:
                    OnEnterStoppedState();
                    if (mListener != null)
                    {
                        mListener.OnCameraViewStopped();
                    }

                    break;
            }

            ;
        }

        private void ProcessExitState(int state)
        {
            Utils.Log("call processExitState: " + state);
            switch (state)
            {
                case STARTED:
                    OnExitStartedState();
                    break;
                case STOPPED:
                    OnExitStoppedState();
                    break;
            }

            ;
        }

        private void OnEnterStoppedState()
        {
            /* nothing to do */
        }

        private void OnExitStoppedState()
        {
            /* nothing to do */
        }

        // NOTE: The order of bitmap constructor and camera connection is important for android 4.1.x
        // Bitmap must be constructed before surface
        private void OnEnterStartedState()
        {
            Utils.Log("call OnEnterStartedState");
            /* Connect camera */
            if (!ConnectCamera(Width, Height))
            {
                Utils.Log("It seems that you device does not support camera (or it is locked). Application will be closed.");
            }
        }

        private void OnExitStartedState()
        {
            DisconnectCamera();
            mCacheBitmap?.Recycle();
        }

        /**
         * This method shall be called by the subclasses when they have valid
         * object and want it to be delivered to external client (via callback) and
         * then displayed on the screen.
         * @param frame - the current frame to be delivered
         */
        protected void DeliverAndDrawFrame(CameraBridgeViewBase.ICvCameraViewFrame frame)
        {
            Mat modified;

            if (mListener != null)
            {
                modified = mListener.OnCameraFrame(frame);
            }
            else
            {
                modified = frame.Rgba();
            }

            bool bmpValid = true;
            if (modified != null)
            {
                try
                {
                    OpenCV.Android.Utils.MatToBitmap(modified, mCacheBitmap);
                }
                catch (Exception e)
                {
                    Utils.Log("Mat type: " + modified);
                    Utils.Log("Bitmap type: " + mCacheBitmap.Width + "*" + mCacheBitmap.Height);
                    Utils.Log("Utils.matToBitmap() throws an exception: " + e);
                    bmpValid = false;
                }
            }

            if (bmpValid && mCacheBitmap != null)
            {
                Canvas canvas = Holder.LockCanvas();
                if (canvas != null)
                {
                    //canvas.DrawColor(Color.Black, PorterDuff.Mode.Clear);

                    Matrix matrix = new Matrix();
                    matrix.PostRotate(90f);
                    Bitmap bitmap = Bitmap.CreateBitmap(mCacheBitmap, 0, 0, mCacheBitmap.Width, mCacheBitmap.Height, matrix, true);

                    if (mScale != 0)
                    {
                        canvas.DrawBitmap(bitmap, new Rect(0, 0, bitmap.Width, bitmap.Height),
                                new Rect((int)((canvas.Width - mScale * bitmap.Width) / 2),
                                        (int)((canvas.Height - mScale * bitmap.Height) / 2),
                                        (int)((canvas.Width - mScale * bitmap.Width) / 2 + mScale * bitmap.Width),
                                        (int)((canvas.Height - mScale * bitmap.Height) / 2 + mScale * bitmap.Height)), null);
                    }
                    else
                    {
                        canvas.DrawBitmap(bitmap, new Rect(0, 0, bitmap.Width, bitmap.Height),
                                new Rect((canvas.Width - bitmap.Width) / 2,
                                        (canvas.Height - bitmap.Height) / 2,
                                        (canvas.Width - bitmap.Width) / 2 + bitmap.Width,
                                        (canvas.Height - bitmap.Height) / 2 + bitmap.Height), null);
                    }

                    if (FpsMeter != null)
                    {
                        FpsMeter.Measure();
                        FpsMeter.Draw(canvas, 20, 30);
                    }
                    Holder.UnlockCanvasAndPost(canvas);
                }
            }
        }

        /**
         * This method is invoked shall perform concrete operation to initialize the camera.
         * CONTRACT: as a result of this method variables FrameWidth and FrameHeight MUST be
         * initialized with the size of the Camera frames that will be delivered to external processor.
         * @param width - the width of this SurfaceView
         * @param height - the height of this SurfaceView
         */
        protected abstract bool ConnectCamera(int width, int height);

        /**
         * Disconnects and release the particular camera object being connected to this surface view.
         * Called when syncObject lock is held
         */
        protected abstract void DisconnectCamera();

        // NOTE: On Android 4.1.x the function must be called before SurfaceTextre constructor!
        protected void AllocateCache()
        {
            mCacheBitmap = Bitmap.CreateBitmap(FrameWidth, FrameHeight, Bitmap.Config.Argb8888);
        }

        /**
         * This helper method can be called by subclasses to select camera preview size.
         * It goes over the list of the supported preview sizes and selects the maximum one which
         * fits both values set via setMaxFrameSize() and surface frame allocated for this view
         * @param sizes
         * @param surfaceWidth
         * @param surfaceHeight
         * @return optimal frame size
         */
        protected Size CalculateCameraFrameSize(IEnumerable<Size> sizes, int surfaceWidth, int surfaceHeight)
        {
            int calcWidth = 0;
            int calcHeight = 0;

            int maxAllowedWidth = (mMaxWidth != MAX_UNSPECIFIED && mMaxWidth < surfaceWidth) ? mMaxWidth : surfaceWidth;
            int maxAllowedHeight = (mMaxHeight != MAX_UNSPECIFIED && mMaxHeight < surfaceHeight) ? mMaxHeight : surfaceHeight;

            foreach (var size in sizes)
            {
                Utils.Log($"Size: {size.Width} x {size.Height}");
                if (size.Width <= maxAllowedWidth && size.Height <= maxAllowedHeight)
                {
                    if (size.Width >= calcWidth && size.Height >= calcHeight)
                    {
                        calcWidth = (int)size.Width;
                        calcHeight = (int)size.Height;
                    }
                }
            }

            return new Size(calcWidth, calcHeight);
        }
    }
}

