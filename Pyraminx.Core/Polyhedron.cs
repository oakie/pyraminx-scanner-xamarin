using System.Collections.Generic;
using System.Linq;

namespace Pyraminx.Core
{
    public enum Axis { W = 1, X = 2, Y = 4, Z = 8 };
    public enum Direction { None, Pos, Neg }

    public class Polyhedron
    {
        public FaceColor W
        {
            get => Faces[Axis.W];
            set => Faces[Axis.W] = value;
        }
        public FaceColor X
        {
            get => Faces[Axis.X];
            set => Faces[Axis.X] = value;
        }
        public FaceColor Y
        {
            get => Faces[Axis.Y];
            set => Faces[Axis.Y] = value;
        }
        public FaceColor Z
        {
            get => Faces[Axis.Z];
            set => Faces[Axis.Z] = value;
        }

        public readonly Dictionary<Axis, FaceColor> Faces = new Dictionary<Axis, FaceColor>
        {
            { Axis.W, FaceColor.Undefined },
            { Axis.X, FaceColor.Undefined },
            { Axis.Y, FaceColor.Undefined },
            { Axis.Z, FaceColor.Undefined }
        };

        public IEnumerable<FaceColor> Colors
        {
            get { return Faces.Values.Where(x => x != FaceColor.Undefined); }
        }

        public IEnumerable<FaceColor> MissingColors
        {
            get {
                var values = Colors.Select(x => x.Value).ToList();
                return FaceColor.AsEnumerable.Where(x => !values.Contains(x.Value));
            }
        }

        public static readonly Dictionary<Axis, FaceColor> AxisColorMap = new Dictionary<Axis, FaceColor> {
            { Axis.W, FaceColor.Yellow },
            { Axis.X, FaceColor.Blue },
            { Axis.Y, FaceColor.Orange },
            { Axis.Z, FaceColor.Green }
        };

        public static readonly Dictionary<FaceColor, Axis> ColorAxisMap = new Dictionary<FaceColor, Axis> {
            { FaceColor.Yellow, Axis.W },
            { FaceColor.Blue, Axis.X },
            { FaceColor.Orange, Axis.Y },
            { FaceColor.Green, Axis.Z }
        };

        public Polyhedron(FaceColor w, FaceColor x, FaceColor y, FaceColor z)
        {
            W = w;
            X = x;
            Y = y;
            Z = z;
        }

        public Polyhedron() : this(FaceColor.Undefined, FaceColor.Undefined, FaceColor.Undefined, FaceColor.Undefined) { }

        public Polyhedron(Polyhedron other) : this(other.W, other.X, other.Y, other.Z) { }

        public override string ToString()
        {
            return $"[{W}{X}{Y}{Z}]";
        }

        public void Turn(string cmd)
        {
            var direction = cmd[1] == '+' ? Direction.Pos : Direction.Neg;
            var axis = char.ToLower(cmd[0]);
            if (axis == 'w')
                TurnW(direction);
            if (axis == 'x')
                TurnX(direction);
            if (axis == 'y')
                TurnY(direction);
            if (axis == 'z')
                TurnZ(direction);
        }

        public void Turn(Axis axis, Direction direction)
        {
            if(axis == Axis.W)
                TurnW(direction);
            if (axis == Axis.X)
                TurnX(direction);
            if (axis == Axis.Y)
                TurnY(direction);
            if (axis == Axis.Z)
                TurnZ(direction);
        }

        public void TurnW(Direction direction)
        {
            var swap = X;
            if (direction == Direction.Pos)
            {
                X = Y;
                Y = Z;
                Z = swap;
            }
            else
            {
                X = Z;
                Z = Y;
                Y = swap;
            }
        }

        public void TurnX(Direction direction)
        {
            var swap = W;
            if (direction == Direction.Pos)
            {
                W = Z;
                Z = Y;
                Y = swap;
            }
            else
            {
                W = Y;
                Y = Z;
                Z = swap;
            }
        }

        public void TurnY(Direction direction)
        {
            var swap = W;
            if (direction == Direction.Pos)
            {
                W = X;
                X = Z;
                Z = swap;
            }
            else
            {
                W = Z;
                Z = X;
                X = swap;
            }
        }

        public void TurnZ(Direction direction)
        {
            var swap = W;
            if (direction == Direction.Pos)
            {
                W = Y;
                Y = X;
                X = swap;
            }
            else
            {
                W = X;
                X = Y;
                Y = swap;
            }
        }
    }
}
