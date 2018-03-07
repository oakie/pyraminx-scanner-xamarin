using System;
using System.IO;
using Android.App;
using Android.Content;
using Android.OS;
using Pyraminx.App.Misc;
using Pyraminx.Core;
using Pyraminx.Robot;
using Pyraminx.Solver;
using Environment = Android.OS.Environment;

namespace Pyraminx.App.Service
{
    [Service]
    public class PyraminxService : Android.App.Service
    {
        #region Binder
        protected IBinder Binder { get; set; }

        public override IBinder OnBind(Intent intent)
        {
            Binder = new PyraminxBinder(this);
            return Binder;
        }
        #endregion

        public ILogger Logger = new Logger();
        public RobotConnection Robot { get; protected set; }
        public SolutionProcedure Solution { get; protected set; }

        public override void OnCreate()
        {
            Logger.Debug("PyraminxService.OnCreate");
            base.OnCreate();

            try
            {
                Robot = new RobotConnection(Logger);
                Robot.Connect();

                var db = Path.Combine(Environment.ExternalStorageDirectory.AbsolutePath, "solutions.db");
                var solver = new PyraminxSolver {Logger = Logger, DatabasePath = db};
                Solution = new SolutionProcedure(Logger) { Robot = Robot, Solver = solver };

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            Logger.Debug("PyraminxService.OnStartCommand");
            return StartCommandResult.Sticky;
        }

        public override void OnTaskRemoved(Intent rootIntent)
        {
            Logger.Debug("PyraminxService.OnTaskRemoved");
            base.OnTaskRemoved(rootIntent);
            StopSelf();
        }

        public override void OnDestroy()
        {
            Logger.Debug("PyraminxService.OnDestroy");
            Robot?.Disconnect();
            base.OnDestroy();
        }
    }
}
