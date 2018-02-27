
using Android.App;
using Android.Widget;

namespace Pyraminx.Common
{
    public static class Utils
    {
        public const string TAG = "pmx";

        public static void Log(string msg)
        {
            Android.Util.Log.Debug(TAG, msg);
        }

        public static void Toast(string msg)
        {
            Android.Widget.Toast.MakeText(Application.Context, msg, ToastLength.Short).Show();
            Log("[Toast]" + msg);
        }
    }
}
