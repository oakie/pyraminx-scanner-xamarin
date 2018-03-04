using Android.Util;
using Pyraminx.Core;

namespace Pyraminx.App.Misc
{
    public class Logger : ILogger
    {
        public const string TAG = "pmx";

        public void Debug(string msg)
        {
            Log.Debug(TAG, msg);
        }

        public void Error(string msg)
        {
            Log.Error(TAG, msg);
        }
    }
}
