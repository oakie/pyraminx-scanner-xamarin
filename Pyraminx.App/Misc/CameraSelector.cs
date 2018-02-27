using System;
using Android.Hardware;
using Android.OS;
using Pyraminx.Common;

namespace Pyraminx.App.Misc
{
    public class CameraSelector
    {
        public const int CameraIdAny = -1;
        public const int CameraIdFront = 888;
        public const int CameraIdBack = 999;

        public static Camera GetCamera(int index)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Gingerbread)
            {
                Camera.CameraInfo info = new Camera.CameraInfo();
                for (int i = 0; i < Camera.NumberOfCameras; ++i)
                {
                    Utils.Log("Trying to open camera with new open(" + i + ")");
                    try
                    {
                        Camera.GetCameraInfo(i, info);

                        if (index == CameraIdAny)
                            return Camera.Open(i);
                        if (index == CameraIdBack && info.Facing == CameraFacing.Back)
                            return Camera.Open(i);
                        if (index == CameraIdFront && info.Facing == CameraFacing.Front)
                            return Camera.Open(i);
                        if (index == i)
                            return Camera.Open(i);
                    }
                    catch (Exception e)
                    {
                        Utils.Log("Camera #" + i + " failed to open: " + e);
                    }
                }
            }
            else
            {
                Utils.Log("Trying to open camera with old open()");
                try
                {
                    return Camera.Open();
                }
                catch (Exception e)
                {
                    Utils.Log("Camera is not available (in use or does not exist): " + e);
                }
            }

            Utils.Log("No camera device could be opened.");
            return null;
        }
    }
}