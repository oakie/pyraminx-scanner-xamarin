using System;
using Android.OS;
using Android.Views;
using Pyraminx.Common;

namespace Pyraminx.App.Views
{
    public class SolverFragment : BaseFragment
    {
        protected override int Layout => Resource.Layout.Solver;
        public new static string Title => "Solver";

        public new static BaseFragment Create()
        {
            Logger.Debug("SolverFragment.Create");
            return new SolverFragment();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            Logger.Debug("SolverFragment.OnCreateView");
            var root = base.OnCreateView(inflater, container, savedInstanceState);
            return root;
        }

        public override void OnResumeFragment()
        {
            Logger.Debug("SolverFragment.OnResume");
            base.OnResumeFragment();
        }

        public override void OnPauseFragment()
        {
            Logger.Debug("SolverFragment.OnPause");
            base.OnPauseFragment();
        }
    }
}
