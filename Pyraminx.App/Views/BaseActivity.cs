using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Pyraminx.App.Misc;
using Pyraminx.App.Service;
using Pyraminx.Common;
using Pyraminx.Core;

namespace Pyraminx.App.Views
{
    public delegate void OnServiceConnectionChanged(bool connected);

    public abstract class BaseActivity : AppCompatActivity, IServiceConnection
    {
        protected abstract int Layout { get; }
        protected abstract string Header { get; }

        protected const ConfigChanges Config = ConfigChanges.ScreenSize | ConfigChanges.Orientation;
        protected const ScreenOrientation Orientation = ScreenOrientation.Portrait;
        protected const string BaseTheme = "@style/AppTheme";

        protected ILogger Logger = new Logger();

        public event OnServiceConnectionChanged OnServiceConnectionChanged;
        public PyraminxService Service { get; protected set; }
        public bool ServiceBound { get; protected set; }

        protected IMenuItem ConnectMenuItem, DisconnectMenuItem;

        protected override void OnCreate(Bundle state)
        {
            base.OnCreate(state);
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
            Logger.Debug("BaseActivity.OnStart " + ServiceBound);

            base.OnStart();
            if (!ServiceBound)
            {
                Intent intent = new Intent(ApplicationContext, typeof(PyraminxService));
                ApplicationContext.StartService(intent);
                ApplicationContext.BindService(intent, this, Bind.AutoCreate);
            }
            else
            {
                OnServiceConnectionChanged?.Invoke(true);
            }
        }

        protected override void OnStop()
        {
            Logger.Debug("BaseActivity.OnStop " + ServiceBound);

            if (ServiceBound)
            {
                Service.Robot.OnConnectedChanged -= OnRobotConnectionChanged;
                Service.Solution.OnSolutionProgress -= OnSolutionProgress;

                ApplicationContext.UnbindService(this);
                OnServiceDisconnected(null);
            }
            else
            {
                OnServiceConnectionChanged?.Invoke(false);
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
            Logger.Debug("BaseActivity.OnRobotConnectionChanged " + connected);
            RunOnUiThread(() =>
            {
                Utils.Toast("Robot connected: " + connected);
                ConnectMenuItem?.SetVisible(!connected);
                DisconnectMenuItem?.SetVisible(connected);
            });
        }

        protected virtual void OnSolutionProgress(SolutionState state) { }

        public virtual void OnServiceConnected(ComponentName name, IBinder service)
        {
            Logger.Debug("BaseActivity.OnServiceConnected");

            Service = (service as PyraminxBinder)?.Service;
            ServiceBound = Service != null;
            OnServiceConnectionChanged?.Invoke(ServiceBound);

            if (Service == null)
                return;

            Service.Robot.OnConnectedChanged += OnRobotConnectionChanged;
            OnRobotConnectionChanged(Service.Robot.Connected);

            Service.Solution.OnSolutionProgress += OnSolutionProgress;
            OnSolutionProgress(Service.Solution.CurrentState);
        }

        public virtual void OnServiceDisconnected(ComponentName name)
        {
            Logger.Debug("BaseActivity.OnServiceDisconnected");

            Service = null;
            ServiceBound = false;
            OnServiceConnectionChanged?.Invoke(ServiceBound);
        }
    }
}