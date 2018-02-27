
using System.Threading.Tasks;

namespace Pyraminx.Solver
{
    public class BidirectionalSearchSolver
    {
        public string DatabasePath { get; set; }

        public async Task<string> FindSolution(string state)
        {
            var solution = await SolutionDatabase.FindSolution(DatabasePath, state);
            return solution;
        }
    }
}
