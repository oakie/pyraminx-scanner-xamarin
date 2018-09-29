using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Pyraminx.Core;
using Pyraminx.Scanner;
using System;
using System.Collections.Generic;
using System.Linq;
using Rect = Android.Graphics.Rect;

namespace Pyraminx.App.Views
{
    public delegate void FaceletClickDelegate(int index);

    public class FaceTile : View
    {
        protected Bitmap Bitmap;
        protected Canvas Canvas;
        protected Path[] Paths = new Path[9];
        protected Region[] Regions = new Region[9];
        protected string[] Tips = new string[3];
        protected float[,] Corners = new float[3, 2];
        protected List<int> Facelets = new List<int>();
        protected Paint Line = new Paint();
        protected Paint Text = new Paint();
        protected Dictionary<int, Paint> Paints = new Dictionary<int, Paint>();
        protected Dictionary<int, Color> ColorMap = new Dictionary<int, Color>
        {
            { FaceColor.Undefined.Value, Color.LightGray },
            { FaceColor.Yellow.Value, Color.Yellow },
            { FaceColor.Blue.Value, Color.Blue },
            { FaceColor.Orange.Value, Color.Orange},
            { FaceColor.Green.Value, Color.Green }
        };

        public event FaceletClickDelegate FaceClick;

        public FaceTile(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Initialize(attrs);
        }

        public FaceTile(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            Initialize(attrs);
        }

        protected void Initialize(IAttributeSet attrs)
        {
            Facelets = new List<int>();
            for (int i = 0; i < 9; ++i)
            {
                Paths[i] = new Path();
                Regions[i] = new Region();
                Facelets.Add(FaceColor.Undefined.Value);
            }

            var tips = Context.ObtainStyledAttributes(attrs, Resource.Styleable.FaceTile);
            Tips[0] = tips.GetString(Resource.Styleable.FaceTile_tip_u);
            Tips[1] = tips.GetString(Resource.Styleable.FaceTile_tip_l);
            Tips[2] = tips.GetString(Resource.Styleable.FaceTile_tip_r);

            Line = new Paint
            {
                AntiAlias = true,
                Color = Color.White,
                StrokeJoin = Paint.Join.Round,
                StrokeWidth = 4f
            };
            Line.SetStyle(Paint.Style.Stroke);

            Text = new Paint
            {
                AntiAlias = true,
                Color = Color.White,
                TextSize = 32
            };

            foreach (var key in ColorMap.Keys)
            {
                Paints[key] = new Paint
                {
                    AntiAlias = true,
                    Color = ColorMap[key]
                };
                Paints[key].SetStyle(Paint.Style.Fill);
            }
        }

        public override bool OnTouchEvent(MotionEvent evt)
        {
            Log.Debug("PMX", "OnTouchEvent");
            if (FaceClick == null)
                return false;
            if (evt.Action != MotionEventActions.Down)
                return false;

            var x = (int)evt.GetX();
            var y = (int)evt.GetY();

            for (int i = 0; i < Regions.Length; ++i)
            {
                if(Regions[i].Contains(x, y))
                {
                    FaceClick(i);
                    return true;
                }
            }

            FaceClick(-1);
            return true;
        }

        protected override void OnMeasure(int w, int h)
        {
            base.OnMeasure(w, h);
            int width = MeasureSpec.GetSize(w);
            int height = MeasureSpec.GetSize(h);
            int size = width > height ? height : width;
            SetMeasuredDimension(size, size);
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);
            Bitmap = Bitmap.CreateBitmap(w, h, Bitmap.Config.Argb8888);
            Canvas = new Canvas(Bitmap);

            float scale = Math.Min(w, h) / 16f;
            float cx = w / 2f, cy = h / 2f;
            var triangles = Facelet.GenerateTriangles();

            var ys = triangles.SelectMany(g => g.Select(p => (float)p.Y)).ToList();
            cy += scale * (ys.Max() - ys.Min()) / 6;

            for (int i = 0; i < 9; ++i)
            {
                var t = triangles[i];
                Paths[i].Reset();
                Paths[i].MoveTo(cx + (float)t[2].X * scale, cy + (float)t[2].Y * scale);
                foreach (var p in t)
                    Paths[i].LineTo(cx + (float)p.X * scale, cy + (float)p.Y * scale);

                Regions[i].SetPath(Paths[i], GetBounds(Paths[i]));
            }

            var v = Facelet.Vertices;

            Corners[0, 0] = cx + (float)v[0].X * scale * 1.1f;
            Corners[0, 1] = cy + (float)v[0].Y * scale * 1.1f;
            Corners[1, 0] = cx + (float)v[6].X * scale * 1.1f;
            Corners[1, 1] = cy + (float)v[6].Y * scale * 1.1f;
            Corners[2, 0] = cx + (float)v[9].X * scale * 1.1f;
            Corners[2, 1] = cy + (float)v[9].Y * scale * 1.1f;
        }

        protected Region GetBounds(Path p)
        {
            var bounds = new RectF();
            p.ComputeBounds(bounds, true);
            return new Region((int)bounds.Left, (int)bounds.Top, (int)bounds.Right, (int)bounds.Bottom);
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            for (int i = 0; i < 9; ++i)
            {
                canvas.DrawPath(Paths[i], Paints[Facelets[i]]);
                canvas.DrawPath(Paths[i], Line);
            }

            for (int i = 0; i < 3; ++i)
                DrawTextCenter(canvas, Tips[i], Corners[i, 0], Corners[i, 1], Text);
        }

        protected void DrawTextCenter(Canvas canvas, string text, float x, float y, Paint paint)
        {
            var r = new Rect();
            paint.GetTextBounds(text, 0, text.Length, r);

            canvas.DrawText(text, x - 0.5f * r.Width(), y + 0.5f * r.Height(), paint);
        }

        public void SetFace(IEnumerable<FaceColor> facelets)
        {
            Facelets = facelets.Select(f => f.Value).ToList();
            Invalidate();
        }

        public void SetLabels(IEnumerable<string> tips)
        {
            Tips = tips.ToArray();
            Invalidate();
        }
    }
}
