using Android.App;
using Android.OS;
using Android.Support.V4.View;
using Android.Widget;
using Pyraminx.App.Misc;
using Pyraminx.App.Service;
using Pyraminx.Core;

namespace Pyraminx.App.Views
{
    [Activity(Label = "PMX", MainLauncher = true, ConfigurationChanges = Config, ScreenOrientation = Orientation, Theme = BaseTheme)]
    public class MainActivity : BaseActivity
    {
        protected override int Layout => Resource.Layout.Main;
        protected override string Header => "Pyraminx";

        protected ViewPager Pager;
        protected Button RunContinuousBtn, RunHaltedBtn, CancelBtn, ContinueBtn;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Pager = FindViewById<ViewPager>(Resource.Id.TabPager);
            var adapter = new TabPagerAdapter(Logger, SupportFragmentManager);
            Pager.Adapter = adapter;
            Pager.PageSelected += (sender, args) => adapter.OnPageSelected(args.Position);

            RunContinuousBtn = FindViewById<Button>(Resource.Id.RunContinuousBtn);
            RunContinuousBtn.Click += (sender, args) =>
            {
                Logger.Debug("RunContinuousBtn.Click");
                if(ServiceBound && !Service.Solution.InProgress)
                    Service.Solution.Run(RunMode.Continuous);
            };
            RunContinuousBtn.Enabled = ServiceBound && !Service.Solution.InProgress;

            RunHaltedBtn = FindViewById<Button>(Resource.Id.RunHaltedBtn);
            RunHaltedBtn.Click += (sender, args) =>
            {
                Logger.Debug("RunHaltedBtn.Click");
                if (ServiceBound && !Service.Solution.InProgress)
                    Service.Solution.Run(RunMode.Halted);
            };
            RunContinuousBtn.Enabled = ServiceBound && !Service.Solution.InProgress;

            ContinueBtn = FindViewById<Button>(Resource.Id.ContinueBtn);
            ContinueBtn.Click += (sender, args) =>
            {
                Logger.Debug("ContinueBtn.Click");
                if(!ServiceBound || !Service.Solution.InProgress)
                    return;
                if(Service.Solution.CurrentMode == RunMode.Halted && Service.Solution.AwaitingGoAhead)
                    Service.Solution.DeliverGoAhead();
            };
            ContinueBtn.Enabled = ServiceBound && Service.Solution.InProgress;

            CancelBtn = FindViewById<Button>(Resource.Id.CancelBtn);
            CancelBtn.Click += (sender, args) =>
            {
                Logger.Debug("CancelBtn.Click");
                if (!ServiceBound || !Service.Solution.InProgress)
                    return;
                Service.Solution.Cancel();
            };
            CancelBtn.Enabled = ServiceBound && Service.Solution.InProgress;
        }

        protected override void OnProgressUpdate(SolutionState state)
        {
            Logger.Debug("MainActivity.OnProgressUpdate " + state);

            RunOnUiThread(() =>
            {
                if(state == SolutionState.Scan)
                {
                    Pager.SetCurrentItem(0, false);
                }
                else if(state == SolutionState.Exec)
                {
                    Pager.SetCurrentItem(2, false);
                }
                else
                {
                    Pager.SetCurrentItem(1, false);
                }

                var idle = state == SolutionState.Start || state == SolutionState.Done;
                var pending = Service.Solution.AwaitingGoAhead;

                RunContinuousBtn.Enabled = idle;
                RunHaltedBtn.Enabled = idle;
                ContinueBtn.Enabled = !idle && pending;
                CancelBtn.Enabled = !idle;
            });
        }
    }
}