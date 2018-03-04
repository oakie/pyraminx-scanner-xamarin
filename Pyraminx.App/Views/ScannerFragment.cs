using System.Linq;
using Android.OS;
using Android.Views;
using Pyraminx.App.Misc;
using Pyraminx.Common;

namespace Pyraminx.App.Views
{
    public class ScannerFragment : BaseFragment
    {
        protected override int Layout => Resource.Layout.Scanner;
        public new static string Title => "Scanner";

        protected CameraViewAdapter CameraAdapter;

        public new static BaseFragment Create()
        {
            Utils.Log("ScannerFragment.Create");
            return new ScannerFragment();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle bundle)
        {
            Utils.Log("ScannerFragment.OnCreateView");
            var root = base.OnCreateView(inflater, container, bundle);

            var camera = root.FindViewById<PortraitCameraBridgeViewBase>(Resource.Id.CameraView);
            CameraAdapter = new CameraViewAdapter(Context, camera);
            CameraAdapter.OnScanResult += (facelets) =>
            {
                var str = string.Join(", ", facelets.Select(x => x.Matches.FirstOrDefault()?.Label));
                Activity.RunOnUiThread(() => Utils.Log("Result: " + str));

                if (ServiceBound && Service.Solution.NeedsFaceScan)
                    Service.Solution.SubmitFaceScan(facelets);
            };
            CameraAdapter.EnableProcessing = true;
            CameraAdapter.Start();

            return root;
        }

        public override void OnResumeFragment()
        {
            Utils.Log("ScannerFragment.OnResume");
            base.OnResumeFragment();
            CameraAdapter?.Start();
        }

        public override void OnPauseFragment()
        {
            Utils.Log("ScannerFragment.OnPause");
            CameraAdapter?.Stop();
            base.OnPauseFragment();
        }
    }
}