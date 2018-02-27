using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Pyraminx.Robot;

namespace Pyraminx.App.Service
{
    [Service]
    public class PyraminxService : Android.App.Service
    {
        #region Binder
        protected IBinder Binder { get; set; }

        public override IBinder OnBind(Intent intent)
        {
            Binder = new PyraminxBinder(this);
            return Binder;
        }
        #endregion

        public RobotConnection Robot { get; protected set; }

        public override void OnCreate()
        {
            base.OnCreate();

            Robot = new RobotConnection();
        }
    }
}