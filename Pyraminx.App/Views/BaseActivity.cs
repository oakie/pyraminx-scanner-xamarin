using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Pyraminx.App.Service;
using Pyraminx.Common;

//using Pyraminx.Android.Misc;
//using Pyraminx.Common;

namespace Pyraminx.App.Views
{
    [Activity(ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Portrait, Theme = "@style/AppTheme")]
    public abstract class BaseActivity : AppCompatActivity, IServiceConnection
    {
        protected abstract string Prefix { get; }
        protected abstract int Layout { get; }
        protected abstract string Header { get; }

        protected PyraminxService Service { get; set; }
        protected bool ServiceBound = false;

        protected IMenuItem ConnectMenuItem, DisconnectMenuItem;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Layout);

            var toolbar = FindViewById<Toolbar>(Resource.Id.Toolbar);
            if (toolbar != null)
            {
                SetSupportActionBar(toolbar);
                SupportActionBar.Title = Header;

                if (GetType() != typeof(MainActivity))
                {
                    SupportActionBar.SetDisplayHomeAsUpEnabled(true);
                    SupportActionBar.SetHomeButtonEnabled(true);
                }
            }
        }

        protected override void OnStart()
        {
            base.OnStart();
            if (!ServiceBound)
            {
                Intent intent = new Intent(this, typeof(PyraminxService));
                BindService(intent, this, Bind.AutoCreate);
            }
        }

        protected override void OnStop()
        {
            if (ServiceBound)
            {
                Service.Robot.OnConnectedChanged -= OnRobotConnectionChanged;

                UnbindService(this);
            }
            base.OnStop();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.Shared, menu);

            ConnectMenuItem = menu.FindItem(Resource.Id.ConnectAction);
            DisconnectMenuItem = menu.FindItem(Resource.Id.DisconnectAction);

            if (ServiceBound)
            {
                ConnectMenuItem.SetVisible(!Service.Robot.Connected);
                DisconnectMenuItem.SetVisible(Service.Robot.Connected);
            }

            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.ConnectAction:
                    Service?.Robot.Connect();
                    return true;
                case Resource.Id.DisconnectAction:
                    Service?.Robot.Disconnect();
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        protected virtual void OnRobotConnectionChanged(bool connected)
        {
            RunOnUiThread(() =>
            {
                Utils.Toast("Robot connected: " + connected);
                ConnectMenuItem?.SetVisible(!connected);
                DisconnectMenuItem?.SetVisible(connected);
            });
        }

        public virtual void OnServiceConnected(ComponentName name, IBinder service)
        {
            Service = (service as PyraminxBinder)?.Service;
            ServiceBound = Service != null;

            if (Service == null)
                return;

            Service.Robot.OnConnectedChanged += OnRobotConnectionChanged;
            OnRobotConnectionChanged(Service.Robot.Connected);
        }

        public virtual void OnServiceDisconnected(ComponentName name)
        {
            Service = null;
            ServiceBound = false;
        }
    }
}