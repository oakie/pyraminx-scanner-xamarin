
using Android.App;
using Android.Content;
using Android.Views;
using Pyraminx.App.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using Axis = Pyraminx.Core.Axis;

namespace Pyraminx.App.Misc
{
    public delegate void FaceDialogCloseDelegate();

    public class FaceDialogBuilder
    {
        public Axis Face { get; set; } = Axis.Undefined;

        public Core.Pyraminx Model { get; set; }

        public List<int[]> Order { get; set; }

        public event FaceDialogCloseDelegate DialogClose;

        public void Show(Context context)
        {
            if (Face == Axis.Undefined)
                throw new Exception("FaceDialog needs a Face!");
            if (Order == null)
                throw new Exception("FaceDialog needs an Order!");
            if (Model == null)
                throw new Exception("FaceDialog needs a Model!");

            var inflater = LayoutInflater.From(context);
            var root = inflater.Inflate(Resource.Layout.FaceDialog, null);

            var tile = root.FindViewById<FaceTile>(Resource.Id.FaceTile);
            RefreshFace(tile);
            tile.FaceClick += index =>
            {
                if (index >= 0)
                {
                    var h = Order[index];
                    Model[h].Faces[Face] = Model[h].Faces[Face].Next;
                    RefreshFace(tile);
                }
            };

            var builder = new AlertDialog.Builder(context).Create();
            builder.SetView(root);
            builder.DismissEvent += (sender, args) => DialogClose?.Invoke();
            builder.Show();
        }

        protected void RefreshFace(FaceTile tile)
        {
            var f = Order.Select(h => Model[h].Faces[Face]);
            tile.SetFace(f);
            var tips = new List<int[]> { Order[0], Order[4], Order[8] };
            tile.SetLabels(tips.Select(GetLetter));
        }

        protected string GetLetter(int[] coord)
        {
            if (coord[0] > 0)
                return "W";
            if (coord[1] > 0)
                return "X";
            if (coord[2] > 0)
                return "Y";
            if (coord[3] > 0)
                return "Z";
            return "";
        }
    }
}