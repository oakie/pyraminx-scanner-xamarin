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
            Utils.Log("SolverFragment.Create");
            return new SolverFragment();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            Utils.Log("SolverFragment.OnCreateView");
            var root = base.OnCreateView(inflater, container, savedInstanceState);
            return root;
        }

        public override void OnResumeFragment()
        {
            Utils.Log("SolverFragment.OnResume");
            base.OnResumeFragment();
        }

        public override void OnPauseFragment()
        {
            Utils.Log("SolverFragment.OnPause");
            base.OnPauseFragment();
        }
    }
}
