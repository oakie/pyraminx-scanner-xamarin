using System;
using System.Collections.Generic;
using System.Linq;
using Android.OS;
using Android.Views;
using Android.Widget;
using Pyraminx.Common;
using Pyraminx.Core;

namespace Pyraminx.App.Views
{
    public class SolverFragment : BaseFragment
    {
        protected override int Layout => Resource.Layout.Solver;
        public new static string Title => "Solver";

        protected FaceTile TileW, TileX, TileY, TileZ;
        protected TextView SolutionText;

        public new static BaseFragment Create()
        {
            Logger.Debug("SolverFragment.Create");
            return new SolverFragment();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            Logger.Debug("SolverFragment.OnCreateView");
            var root = base.OnCreateView(inflater, container, savedInstanceState);

            TileW = root.FindViewById<FaceTile>(Resource.Id.FaceTileW);
            TileX = root.FindViewById<FaceTile>(Resource.Id.FaceTileX);
            TileY = root.FindViewById<FaceTile>(Resource.Id.FaceTileY);
            TileZ = root.FindViewById<FaceTile>(Resource.Id.FaceTileZ);

            SolutionText = root.FindViewById<TextView>(Resource.Id.SolutionTxt);

            return root;
        }

        public override void OnResumeFragment()
        {
            Logger.Debug("SolverFragment.OnResume");
            base.OnResumeFragment();

            if (ServiceBound)
            {
                Service.Solution.OnModelUpdate += OnModelUpdate;
                OnModelUpdate(Service.Solution.Model);

                Service.Solution.OnSolutionUpdate += OnSolutionUpdate;
                OnSolutionUpdate(Service.Solution.Solution);
            }
        }

        public override void OnPauseFragment()
        {
            Logger.Debug("SolverFragment.OnPause");

            if (ServiceBound)
            {
                Service.Solution.OnModelUpdate -= OnModelUpdate;
                Service.Solution.OnSolutionUpdate -= OnSolutionUpdate;
            }

            base.OnPauseFragment();
        }

        protected override void OnServiceConnectionChanged(bool connected)
        {
            base.OnServiceConnectionChanged(connected);
            if (connected)
            {
                Service.Solution.OnModelUpdate += OnModelUpdate;
                OnModelUpdate(Service.Solution.Model);

                Service.Solution.OnSolutionUpdate += OnSolutionUpdate;
                OnSolutionUpdate(Service.Solution.Solution);
            }
        }

        protected void OnModelUpdate(Core.Pyraminx pyraminx)
        {
            ParentActivity.RunOnUiThread(RefreshFaceTiles);
        }

        protected void OnSolutionUpdate(string solution)
        {
            ParentActivity.RunOnUiThread(() =>
            {
                if(solution == null)
                    SolutionText.Text = "[No solution found]";
                else
                    SolutionText.Text = solution;
            });
        }

        protected void RefreshFaceTiles()
        {
            if(!ServiceBound || Service?.Solution.Model == null)
            {
                var undefined = new List<FaceColor>(Enumerable.Repeat(FaceColor.Undefined, 9));
                TileW.SetFace(undefined);
                TileX.SetFace(undefined);
                TileY.SetFace(undefined);
                TileZ.SetFace(undefined);
                return;
            }

            var p = Service.Solution.Model;

            var w = new List<FaceColor>
            {
                p[0, 0, 0, 2].W,
                p[0, 0, 1, 1].W,
                p[0, 0, 0, 1].W,
                p[0, 1, 0, 1].W,
                p[0, 0, 2, 0].W,
                p[0, 0, 1, 0].W,
                p[0, 1, 1, 0].W,
                p[0, 1, 0, 0].W,
                p[0, 2, 0, 0].W
            };
            TileW.SetFace(w);

            var x = new List<FaceColor>
            {
                p[2, 0, 0, 0].X,
                p[1, 0, 1, 0].X,
                p[1, 0, 0, 0].X,
                p[1, 0, 0, 1].X,
                p[0, 0, 2, 0].X,
                p[0, 0, 1, 0].X,
                p[0, 0, 1, 1].X,
                p[0, 0, 0, 1].X,
                p[0, 0, 0, 2].X

            };
            TileX.SetFace(x);

            var y = new List<FaceColor>
            {
                p[2, 0, 0, 0].Y,
                p[1, 0, 0, 1].Y,
                p[1, 0, 0, 0].Y,
                p[1, 1, 0, 0].Y,
                p[0, 0, 0, 2].Y,
                p[0, 0, 0, 1].Y,
                p[0, 1, 0, 1].Y,
                p[0, 1, 0, 0].Y,
                p[0, 2, 0, 0].Y
            };
            TileY.SetFace(y);

            var z = new List<FaceColor>
            {
                p[2, 0, 0, 0].Z,
                p[1, 1, 0, 0].Z,
                p[1, 0, 0, 0].Z,
                p[1, 0, 1, 0].Z,
                p[0, 2, 0, 0].Z,
                p[0, 1, 0, 0].Z,
                p[0, 1, 1, 0].Z,
                p[0, 0, 1, 0].Z,
                p[0, 0, 2, 0].Z
            };
            TileZ.SetFace(z);            
        }
    }
}
