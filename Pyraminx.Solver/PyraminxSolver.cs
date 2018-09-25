using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Java.Lang;
using Pyraminx.Core;
using Exception = System.Exception;
using Pyraminx = Pyraminx.Core.Pyraminx;

namespace Pyraminx.Solver
{
    public class PyraminxSolver
    {
        public ILogger Logger { get; set; }
        public string DatabasePath { get; set; }

        public async Task<string> FindSolution(Core.Pyraminx pyraminx)
        {
            Logger.Debug("PyraminxSolver.FindSolution");
            try
            {
                var hash = pyraminx.GetTransform().Hash;
                var transform = TransformMap[hash];
                var state = new Core.Pyraminx(pyraminx);
                state.ExecuteFlips(transform[0]);
                Logger.Debug(state.ToString());

                Logger.Debug("Search for solution...");
                var solution = await Search(state);

                if (solution == null)
                {
                    Logger.Debug("No solution found!");
                    return null;
                }

                solution = TransformMoves(solution, transform[1]);

                var tips = SolveTips(pyraminx);
                if (!string.IsNullOrEmpty(tips))
                    solution += ":" + tips;

                Logger.Debug($"Found solution: {solution}");
                return solution;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return null;
        }

        protected string SolveTips(Core.Pyraminx pyraminx)
        {
            var s = "";
            foreach (var axis in Polyhedron.Axes)
            {
                var tip = pyraminx.GetTip(axis);
                var axial = pyraminx.GetAxial(axis);
                var offset = Core.Pyraminx.AxialOffset[axis];
                if (tip.Faces[offset[0]] == axial.Faces[offset[1]])
                    s += axis + "+";
                if (tip.Faces[offset[0]] == axial.Faces[offset[2]])
                    s += axis + "-";
            }

            return s.ToLower();
        }

        protected async Task<string> Search(Core.Pyraminx original)
        {
            var db = new SolutionDatabase { DatabasePath = DatabasePath };
            var permutations = new[] { "w+", "w-", "x+", "x-", "y+", "y-", "z+", "z-" };

            var q = new Queue<Tuple<Core.Pyraminx, string>>();
            q.Enqueue(new Tuple<Core.Pyraminx, string>(original, ""));
            while (q.Any())
            {
                var candidate = q.Dequeue();
                var state = candidate.Item1.Serialize();
                var solution = await db.FindSolution(state);

                if (solution != null)
                {
                    var moves = candidate.Item2 + solution;
                    return moves;
                }

                if (candidate.Item2.Length > 6)
                    continue;

                foreach (var prefix in permutations)
                {
                    var child = new Core.Pyraminx(candidate.Item1);
                    child.Turn(prefix);
                    var moves = prefix + candidate.Item2;
                    q.Enqueue(new Tuple<Core.Pyraminx, string>(child, moves));
                }
            }

            return null;
        }

        protected string TransformMoves(string moves, string transform)
        {
            var poly = new Polyhedron(FaceColor.Yellow, FaceColor.Blue, FaceColor.Orange, FaceColor.Green);
            for (int i = 0; i < transform.Length; i += 2)
                poly.Turn(transform.Substring(i, 2));

            var map = new Dictionary<char, char>();
            foreach (var axis in poly.Faces.Keys)
            {
                var key = Polyhedron.ColorAxisMap[poly.Faces[axis]].ToString().ToLower()[0];
                map[key] = axis.ToString().ToLower()[0];
            }

            var m = "";
            for (int i = 0; i < moves.Length; i += 2)
                m += map[moves[i]] + "" + moves[i + 1];
            return m;
        }

        protected Dictionary<int, string[]> TransformMap = new Dictionary<int, string[]>
        {
            { Hash(Axis.W, Axis.X), new [] {"", ""} },
            { Hash(Axis.W, Axis.Y), new [] {"w-", "w+"} },
            { Hash(Axis.W, Axis.Z), new [] {"w+", "w-"} },

            { Hash(Axis.X, Axis.W), new [] {"z-w+", "w-z+"} },
            { Hash(Axis.X, Axis.Y), new [] {"z+", "z-"} },
            { Hash(Axis.X, Axis.Z), new [] {"w+z+", "z-w-"} },

            { Hash(Axis.Y, Axis.W), new [] {"z-", "z+"} },
            { Hash(Axis.Y, Axis.X), new [] {"x+", "x-"} },
            { Hash(Axis.Y, Axis.Z), new [] {"w+z-", "z+w-"} },

            { Hash(Axis.Z, Axis.W), new [] {"y+", "y-"} },
            { Hash(Axis.Z, Axis.X), new [] {"x-", "x+"} },
            { Hash(Axis.Z, Axis.Y), new [] {"x+w-", "w+x-"} }
        };

        protected static int Hash(Axis w, Axis x)
        {
            return ((int)w << 4) + ((int)x << 0);
        }
    }
}
