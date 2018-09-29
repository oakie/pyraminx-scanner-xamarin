using Android.OS;
using Android.Views;
using Android.Widget;
using Pyraminx.App.Misc;
using Pyraminx.Core;
using System.Collections.Generic;
using System.Linq;
using Axis = Pyraminx.Core.Axis;

namespace Pyraminx.App.Views
{
    public class SolverFragment : BaseFragment
    {
        protected override int Layout => Resource.Layout.Solver;
        public new static string Title => "Solver";

        protected FaceTile TileW, TileX, TileY, TileZ;
        protected Button ClearStateBtn, ResetStateBtn, SolveStateBtn;
        protected TextView SolutionText;

        private static List<int[]> OrderW = new List<int[]>
        {
            new []{0, 0, 0, 2},
            new []{0, 0, 1, 1},
            new []{0, 0, 0, 1},
            new []{0, 1, 0, 1},
            new []{0, 0, 2, 0},
            new []{0, 0, 1, 0},
            new []{0, 1, 1, 0},
            new []{0, 1, 0, 0},
            new []{0, 2, 0, 0}
        };
        private static List<int[]> OrderX = new List<int[]>
        {
            new []{2, 0, 0, 0},
            new []{1, 0, 1, 0},
            new []{1, 0, 0, 0},
            new []{1, 0, 0, 1},
            new []{0, 0, 2, 0},
            new []{0, 0, 1, 0},
            new []{0, 0, 1, 1},
            new []{0, 0, 0, 1},
            new []{0, 0, 0, 2}
        };
        private static List<int[]> OrderY = new List<int[]>
        {
            new []{2, 0, 0, 0},
            new []{1, 0, 0, 1},
            new []{1, 0, 0, 0},
            new []{1, 1, 0, 0},
            new []{0, 0, 0, 2},
            new []{0, 0, 0, 1},
            new []{0, 1, 0, 1},
            new []{0, 1, 0, 0},
            new []{0, 2, 0, 0}
        };
        private static List<int[]> OrderZ = new List<int[]>
        {
            new []{2, 0, 0, 0},
            new []{1, 1, 0, 0},
            new []{1, 0, 0, 0},
            new []{1, 0, 1, 0},
            new []{0, 2, 0, 0},
            new []{0, 1, 0, 0},
            new []{0, 1, 1, 0},
            new []{0, 0, 1, 0},
            new []{0, 0, 2, 0}
        };

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
            TileW.FaceClick += index => SelectFaceTile(Axis.W, OrderW);
            TileX = root.FindViewById<FaceTile>(Resource.Id.FaceTileX);
            TileX.FaceClick += index => SelectFaceTile(Axis.X, OrderX);
            TileY = root.FindViewById<FaceTile>(Resource.Id.FaceTileY);
            TileY.FaceClick += index => SelectFaceTile(Axis.Y, OrderY);
            TileZ = root.FindViewById<FaceTile>(Resource.Id.FaceTileZ);
            TileZ.FaceClick += index => SelectFaceTile(Axis.Z, OrderZ);

            ClearStateBtn = root.FindViewById<Button>(Resource.Id.ClearStateBtn);
            ClearStateBtn.Click += (sender, args) =>
            {
                if (!ServiceBound)
                    return;
                Service.Solution.Model.Clear();
                Service.Solution.NotifyModelChanged();
            };

            ResetStateBtn = root.FindViewById<Button>(Resource.Id.ResetStateBtn);
            ResetStateBtn.Click += (sender, args) =>
            {
                if (!ServiceBound)
                    return;
                Service.Solution.Model.Reset();
                Service.Solution.NotifyModelChanged();
            };

            SolveStateBtn = root.FindViewById<Button>(Resource.Id.SolveStateBtn);
            SolveStateBtn.Click += async (sender, args) =>
            {
                if (!ServiceBound)
                    return;
                await Service.Solution.FindSolution();
            };

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
                if (solution == null)
                    SolutionText.Text = "[No solution found]";
                else
                    SolutionText.Text = solution;
            });
        }

        protected void RefreshFaceTiles()
        {
            if (!ServiceBound || Service?.Solution.Model == null)
            {
                var undefined = new List<FaceColor>(Enumerable.Repeat(FaceColor.Undefined, 9));
                TileW.SetFace(undefined);
                TileX.SetFace(undefined);
                TileY.SetFace(undefined);
                TileZ.SetFace(undefined);
                return;
            }

            var p = Service.Solution.Model;

            var w = OrderW.Select(h => p[h].W);
            TileW.SetFace(w);
            var x = OrderX.Select(h => p[h].X);
            TileX.SetFace(x);
            var y = OrderY.Select(h => p[h].Y);
            TileY.SetFace(y);
            var z = OrderZ.Select(h => p[h].Z);
            TileZ.SetFace(z);
        }

        protected void SelectFaceTile(Axis face, List<int[]> order)
        {
            Logger.Debug("SelectFaceTile");
            if (!ServiceBound)
                return;

            var dialog = new FaceDialogBuilder
            {
                Face = face,
                Model = Service.Solution.Model,
                Order = order
            };

            dialog.DialogClose += Service.Solution.NotifyModelChanged;

            dialog.Show(Context);
        }
    }
}
