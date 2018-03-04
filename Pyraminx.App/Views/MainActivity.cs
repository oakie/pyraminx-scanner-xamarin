using Android.App;
using Android.OS;
using Android.Support.V4.View;

namespace Pyraminx.App.Views
{
    [Activity(Label = "PMX", MainLauncher = true, ConfigurationChanges = Config, ScreenOrientation = Orientation, Theme = BaseTheme)]
    public class MainActivity : BaseActivity
    {
        protected override int Layout => Resource.Layout.Main;
        protected override string Header => "Pyraminx";

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            var pager = FindViewById<ViewPager>(Resource.Id.TabPager);
            var adapter = new TabPagerAdapter(SupportFragmentManager);
            pager.Adapter = adapter;
            pager.PageSelected += (sender, args) => adapter.OnPageSelected(args.Position);
        }
    }
}