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
        protected Button StartBtn;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Pager = FindViewById<ViewPager>(Resource.Id.TabPager);
            var adapter = new TabPagerAdapter(Logger, SupportFragmentManager);
            Pager.Adapter = adapter;
            Pager.PageSelected += (sender, args) => adapter.OnPageSelected(args.Position);

            StartBtn = FindViewById<Button>(Resource.Id.StartProcedureBtn);
            StartBtn.Click += (sender, args) =>
            {
                Logger.Debug("StartBtn.Click");
                if(ServiceBound && !Service.Solution.InProgress)
                    Service.Solution.Run();
            };
            StartBtn.Enabled = ServiceBound && !Service.Solution.InProgress;
        }

        protected override void OnSolutionProgress(SolutionState state)
        {
            Logger.Debug("MainActivity.OnSolutionProgress " + state);

            RunOnUiThread(() =>
            {
                if (state == SolutionState.Scan)
                {
                    Pager.SetCurrentItem(0, false);
                }
                else if (state == SolutionState.Exec)
                {
                    Pager.SetCurrentItem(2, false);
                }
                else
                {
                    Pager.SetCurrentItem(1, false);
                }

                StartBtn.Enabled = state == SolutionState.Start || state == SolutionState.Done;
            });
        }
    }
}