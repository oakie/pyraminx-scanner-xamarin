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

    public class SolutionDatabase
    {
        public string DatabasePath { get; set; }

        protected SQLiteAsyncConnection Connection { get; set; }

        public async Task<string> FindSolution(string state)
        {
            if(Connection == null)
                Connection = new SQLiteAsyncConnection(DatabasePath);

            var result = Connection.Table<Solution>().Where(x => x.State == state);

            if (await result.CountAsync() == 0)
                return null;

            var solution = await result.FirstOrDefaultAsync();

            return solution.Sequence;
        }
    }
}