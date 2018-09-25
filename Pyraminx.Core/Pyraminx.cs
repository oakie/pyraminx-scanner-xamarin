using System;
using System.Collections.Generic;
using System.Linq;
using Android.Speech;
using Java.Lang;
using Exception = System.Exception;
using StringBuilder = System.Text.StringBuilder;

namespace Pyraminx.Core
{
    public class PyraminxTransform
    {
        public Axis TipW { get; set; }
        public Axis TipX { get; set; }

        public int Hash => ((int) TipW << 4) + ((int) TipX << 0);
    }

    public class Pyraminx
    {
        public static readonly Dictionary<Axis, List<Axis>> AxialOffset = new Dictionary<Axis, List<Axis>> {
            { Axis.W, new List<Axis> {Axis.X, Axis.Z, Axis.Y } },
            { Axis.X, new List<Axis> {Axis.W, Axis.Y, Axis.Z } },
            { Axis.Y, new List<Axis> {Axis.W, Axis.Z, Axis.X } },
            { Axis.Z, new List<Axis> {Axis.W, Axis.X, Axis.Y } }
        };

        public readonly Polyhedron[,,,] Polys = new Polyhedron[3, 3, 3, 3];

        public Polyhedron GetTip(Axis axis)
        {
            if(axis == Axis.W)
                return Polys[2, 0, 0, 0];
            if (axis == Axis.X)
                return Polys[0, 2, 0, 0];
            if (axis == Axis.Y)
                return Polys[0, 0, 2, 0];
            return Polys[0, 0, 0, 2];
        }

        public Polyhedron GetAxial(Axis axis)
        {
            if (axis == Axis.W)
                return Polys[1, 0, 0, 0];
            if (axis == Axis.X)
                return Polys[0, 1, 0, 0];
            if (axis == Axis.Y)
                return Polys[0, 0, 1, 0];
            return Polys[0, 0, 0, 1];
        }

        public PyraminxTransform GetTransform()
        {
            var w = Polys[2, 0, 0, 0].MissingColors;
            var x = Polys[0, 2, 0, 0].MissingColors;

            if(!w.Any() || !x.Any())
                throw new Exception("Invalid pyraminx state! Cannot calculate transform.");

            return new PyraminxTransform
            {
                TipW = Polyhedron.ColorAxisMap[w.First()],
                TipX = Polyhedron.ColorAxisMap[x.First()],
            };
        }

        public Pyraminx()
        {
            for (int w = 0; w < 3; ++w)
                for (int x = 0; x < 3; ++x)
                    for (int y = 0; y < 3; ++y)
                        for (int z = 0; z < 3; ++z)
                            Polys[w, x, y, z] = new Polyhedron();
        }

        public Pyraminx(Pyraminx other)
        {
            for (int w = 0; w < 3; ++w)
                for (int x = 0; x < 3; ++x)
                    for (int y = 0; y < 3; ++y)
                        for (int z = 0; z < 3; ++z)
                            Polys[w, x, y, z] = new Polyhedron(other[w, x, y, z]);
        }

        public Polyhedron this[int w, int x, int y, int z]
        {
            get { return Polys[w, x, y, z]; }
            set { Polys[w, x, y, z] = value; }
        }

        public static Pyraminx CreateSolved()
        {
            var p = new Pyraminx();
            // Tips
            p[2, 0, 0, 0] = new Polyhedron(FaceColor.Undefined, FaceColor.Blue, FaceColor.Orange, FaceColor.Green);
            p[0, 2, 0, 0] = new Polyhedron(FaceColor.Yellow, FaceColor.Undefined, FaceColor.Orange, FaceColor.Green);
            p[0, 0, 2, 0] = new Polyhedron(FaceColor.Yellow, FaceColor.Blue, FaceColor.Undefined, FaceColor.Green);
            p[0, 0, 0, 2] = new Polyhedron(FaceColor.Yellow, FaceColor.Blue, FaceColor.Orange, FaceColor.Undefined);

            // Centers
            p[1, 0, 0, 0] = new Polyhedron(FaceColor.Undefined, FaceColor.Blue, FaceColor.Orange, FaceColor.Green);
            p[0, 1, 0, 0] = new Polyhedron(FaceColor.Yellow, FaceColor.Undefined, FaceColor.Orange, FaceColor.Green);
            p[0, 0, 1, 0] = new Polyhedron(FaceColor.Yellow, FaceColor.Blue, FaceColor.Undefined, FaceColor.Green);
            p[0, 0, 0, 1] = new Polyhedron(FaceColor.Yellow, FaceColor.Blue, FaceColor.Orange, FaceColor.Undefined);

            // Edges
            p[1, 1, 0, 0] = new Polyhedron(FaceColor.Undefined, FaceColor.Undefined, FaceColor.Orange, FaceColor.Green);
            p[1, 0, 1, 0] = new Polyhedron(FaceColor.Undefined, FaceColor.Blue, FaceColor.Undefined, FaceColor.Green);
            p[1, 0, 0, 1] = new Polyhedron(FaceColor.Undefined, FaceColor.Blue, FaceColor.Orange, FaceColor.Undefined);
            p[0, 1, 1, 0] = new Polyhedron(FaceColor.Yellow, FaceColor.Undefined, FaceColor.Undefined, FaceColor.Green);
            p[0, 1, 0, 1] = new Polyhedron(FaceColor.Yellow, FaceColor.Undefined, FaceColor.Orange, FaceColor.Undefined);
            p[0, 0, 1, 1] = new Polyhedron(FaceColor.Yellow, FaceColor.Blue, FaceColor.Undefined, FaceColor.Undefined);

            return p;
        }

        public void ExecuteTurns(string cmd, bool tipsonly = false)
        {
            for (int i = 0; i < cmd.Length; i += 2)
            {
                Turn(cmd.Substring(i, 2), tipsonly);
            }
        }

        public void ExecuteFlips(string cmd)
        {
            for (int i = 0; i < cmd.Length; i += 2)
            {
                Flip(cmd.Substring(i, 2));
            }
        }

        public Direction GetTipAlignment(Axis axis)
        {
            var tip = Polys[axis == Axis.W ? 2 : 0, axis == Axis.X ? 2 : 0, axis == Axis.Y ? 2 : 0, axis == Axis.Z ? 2 : 0];
            var axial = Polys[axis == Axis.W ? 1 : 0, axis == Axis.X ? 1 : 0, axis == Axis.Y ? 1 : 0, axis == Axis.Z ? 1 : 0];
            var offset = AxialOffset[axis];
            if (tip.Faces[offset[0]] == axial.Faces[offset[1]])
                return Direction.Neg;
            if (tip.Faces[offset[1]] == axial.Faces[offset[0]])
                return Direction.Pos;
            return Direction.None;
        }

        protected void Swap(ref Polyhedron a, ref Polyhedron b, ref Polyhedron c, Direction direction)
        {
            var swap = a;
            if (direction == Direction.Pos)
            {
                a = c;
                c = b;
                b = swap;
            }
            else
            {
                a = b;
                b = c;
                c = swap;
            }
        }

        #region Primitives

        public void Turn(Axis axis, Direction direction, bool tiponly = false)
        {
            if (axis == Axis.W)
                TurnW(direction, tiponly);
            if (axis == Axis.X)
                TurnX(direction, tiponly);
            if (axis == Axis.Y)
                TurnY(direction, tiponly);
            if (axis == Axis.Z)
                TurnZ(direction, tiponly);
        }

        public void Turn(string cmd, bool tiponly = false)
        {
            var direction = cmd[1] == '+' ? Direction.Pos : Direction.Neg;
            var axis = char.ToLower(cmd[0]);
            if (axis == 'w')
                TurnW(direction, tiponly);
            if (axis == 'x')
                TurnX(direction, tiponly);
            if (axis == 'y')
                TurnY(direction, tiponly);
            if (axis == 'z')
                TurnZ(direction, tiponly);
        }

        public void TurnW(Direction direction, bool tiponly = false)
        {
            Polys[2, 0, 0, 0].TurnW(direction);
            if (tiponly)
                return;

            for (int x = 0; x < 3; ++x)
                for (int y = 0; y < 3; ++y)
                    for (int z = 0; z < 3; ++z)
                        Polys[1, x, y, z].TurnW(direction);

            Swap(ref Polys[1, 1, 0, 0], ref Polys[1, 0, 0, 1], ref Polys[1, 0, 1, 0], direction);
        }

        public void TurnX(Direction direction, bool tiponly = false)
        {
            Polys[0, 2, 0, 0].TurnX(direction);
            if (tiponly)
                return;

            for (int w = 0; w < 3; ++w)
                for (int y = 0; y < 3; ++y)
                    for (int z = 0; z < 3; ++z)
                        Polys[w, 1, y, z].TurnX(direction);

            Swap(ref Polys[1, 1, 0, 0], ref Polys[0, 1, 1, 0], ref Polys[0, 1, 0, 1], direction);
        }

        public void TurnY(Direction direction, bool tiponly = false)
        {
            Polys[0, 0, 2, 0].TurnY(direction);
            if (tiponly)
                return;

            for (int w = 0; w < 3; ++w)
                for (int x = 0; x < 3; ++x)
                    for (int z = 0; z < 3; ++z)
                        Polys[w, x, 1, z].TurnY(direction);

            Swap(ref Polys[1, 0, 1, 0], ref Polys[0, 0, 1, 1], ref Polys[0, 1, 1, 0], direction);
        }

        public void TurnZ(Direction direction, bool tiponly = false)
        {
            Polys[0, 0, 0, 2].TurnZ(direction);
            if (tiponly)
                return;

            for (int w = 0; w < 3; ++w)
                for (int x = 0; x < 3; ++x)
                    for (int y = 0; y < 3; ++y)
                        Polys[w, x, y, 1].TurnZ(direction);

            Swap(ref Polys[1, 0, 0, 1], ref Polys[0, 1, 0, 1], ref Polys[0, 0, 1, 1], direction);
        }

        public void Flip(Axis axis, Direction direction)
        {
            if (axis == Axis.W)
                FlipW(direction);
            if (axis == Axis.X)
                FlipX(direction);
            if (axis == Axis.Y)
                FlipY(direction);
            if (axis == Axis.Z)
                FlipZ(direction);
        }

        public void Flip(string cmd)
        {
            var direction = cmd[1] == '+' ? Direction.Pos : Direction.Neg;
            var axis = char.ToLower(cmd[0]);
            if (axis == 'w')
                FlipW(direction);
            if (axis == 'x')
                FlipX(direction);
            if (axis == 'y')
                FlipY(direction);
            if (axis == 'z')
                FlipZ(direction);
        }

        public void FlipW(Direction direction)
        {
            TurnW(direction);
            for (int x = 0; x < 3; ++x)
                for (int y = 0; y < 3; ++y)
                    for (int z = 0; z < 3; ++z)
                        Polys[0, x, y, z].TurnW(direction);
            Swap(ref Polys[0, 2, 0, 0], ref Polys[0, 0, 0, 2], ref Polys[0, 0, 2, 0], direction);
            Swap(ref Polys[0, 1, 0, 0], ref Polys[0, 0, 0, 1], ref Polys[0, 0, 1, 0], direction);
            Swap(ref Polys[0, 1, 1, 0], ref Polys[0, 1, 0, 1], ref Polys[0, 0, 1, 1], direction);
        }

        public void FlipX(Direction direction)
        {
            TurnX(direction);
            for (int w = 0; w < 3; ++w)
                for (int y = 0; y < 3; ++y)
                    for (int z = 0; z < 3; ++z)
                        Polys[w, 0, y, z].TurnX(direction);
            Swap(ref Polys[2, 0, 0, 0], ref Polys[0, 0, 2, 0], ref Polys[0, 0, 0, 2], direction);
            Swap(ref Polys[1, 0, 0, 0], ref Polys[0, 0, 1, 0], ref Polys[0, 0, 0, 1], direction);
            Swap(ref Polys[1, 0, 1, 0], ref Polys[0, 0, 1, 1], ref Polys[1, 0, 0, 1], direction);
        }

        public void FlipY(Direction direction)
        {
            TurnY(direction);
            for (int w = 0; w < 3; ++w)
                for (int x = 0; x < 3; ++x)
                    for (int z = 0; z < 3; ++z)
                        Polys[w, x, 0, z].TurnY(direction);
            Swap(ref Polys[2, 0, 0, 0], ref Polys[0, 0, 0, 2], ref Polys[0, 2, 0, 0], direction);
            Swap(ref Polys[1, 0, 0, 0], ref Polys[0, 0, 0, 1], ref Polys[0, 1, 0, 0], direction);
            Swap(ref Polys[1, 1, 0, 0], ref Polys[1, 0, 0, 1], ref Polys[0, 1, 0, 1], direction);
        }

        public void FlipZ(Direction direction)
        {
            TurnZ(direction);
            for (int w = 0; w < 3; ++w)
                for (int x = 0; x < 3; ++x)
                    for (int y = 0; y < 3; ++y)
                        Polys[w, x, y, 0].TurnZ(direction);
            Swap(ref Polys[2, 0, 0, 0], ref Polys[0, 2, 0, 0], ref Polys[0, 0, 2, 0], direction);
            Swap(ref Polys[1, 0, 0, 0], ref Polys[0, 1, 0, 0], ref Polys[0, 0, 1, 0], direction);
            Swap(ref Polys[1, 1, 0, 0], ref Polys[0, 1, 1, 0], ref Polys[1, 0, 1, 0], direction);
        }

        #endregion

        public override string ToString()
        {
            var s = new StringBuilder("  ");

            s.Append(Polys[2, 0, 0, 0].Y + "     ");
            s.Append(Polys[2, 0, 0, 0].Z + "     ");
            s.Append(Polys[2, 0, 0, 0].X + "\n");

            s.Append(" ");
            s.Append(Polys[1, 0, 0, 1].Y + "" + Polys[1, 0, 0, 0].Y + "" + Polys[1, 1, 0, 0].Y + "   ");
            s.Append(Polys[1, 1, 0, 0].Z + "" + Polys[1, 0, 0, 0].Z + "" + Polys[1, 0, 1, 0].Z + "   ");
            s.Append(Polys[1, 0, 1, 0].X + "" + Polys[1, 0, 0, 0].X + "" + Polys[1, 0, 0, 1].X + "\n");

            s.Append(Polys[0, 0, 0, 2].Y + "" + Polys[0, 0, 0, 1].Y + "" + Polys[0, 1, 0, 1].Y + "" + Polys[0, 1, 0, 0].Y + "" + Polys[0, 2, 0, 0].Y + " ");
            s.Append(Polys[0, 2, 0, 0].Z + "" + Polys[0, 1, 0, 0].Z + "" + Polys[0, 1, 1, 0].Z + "" + Polys[0, 0, 1, 0].Z + "" + Polys[0, 0, 2, 0].Z + " ");
            s.Append(Polys[0, 0, 2, 0].X + "" + Polys[0, 0, 1, 0].X + "" + Polys[0, 0, 1, 1].X + "" + Polys[0, 0, 0, 1].X + "" + Polys[0, 0, 0, 2].X + "\n");

            s.Append("      " + Polys[0, 2, 0, 0].W + "" + Polys[0, 1, 0, 0].W + "" + Polys[0, 1, 1, 0].W + "" + Polys[0, 0, 1, 0].W + "" + Polys[0, 0, 2, 0].W + "\n");
            s.Append("       " + Polys[0, 1, 0, 1].W + "" + Polys[0, 0, 0, 1].W + "" + Polys[0, 0, 1, 1].W + "\n");
            s.Append("        " + Polys[0, 0, 0, 2].W);

            return s.ToString();
        }

        public string Serialize()
        {
            var s = new StringBuilder();

            // Axials
            s.Append(Polys[1, 0, 0, 0]);
            s.Append(Polys[0, 1, 0, 0]);
            s.Append(Polys[0, 0, 1, 0]);
            s.Append(Polys[0, 0, 0, 1]);

            // Edges
            s.Append(Polys[1, 1, 0, 0]);
            s.Append(Polys[1, 0, 1, 0]);
            s.Append(Polys[1, 0, 0, 1]);
            s.Append(Polys[0, 1, 1, 0]);
            s.Append(Polys[0, 1, 0, 1]);
            s.Append(Polys[0, 0, 1, 1]);

            return s.ToString();
        }
    }
}