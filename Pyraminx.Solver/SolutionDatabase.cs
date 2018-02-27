using SQLite;
using System.Threading.Tasks;

namespace Pyraminx.Solver
{
    [Table("solutions")]
    public class Solution
    {
        [Column("id"), PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Column("state"), Unique]
        public string State { get; set; }

        [Column("solution"), Unique]
        public string Sequence { get; set; }
    }

    public static class SolutionDatabase
    {
        public static async Task<string> FindSolution(string path, string state)
        {
            
            var db = new SQLiteAsyncConnection(path);
            var result = db.Table<Solution>().Where(x => x.State == state);

            if(await result.CountAsync() == 0)
                return null;

            var solution = await result.FirstOrDefaultAsync();

            return solution.Sequence;
        }
    }
}