using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyraminx.Common;
using Pyraminx.Core;
using Pyraminx.Robot;
using Pyraminx.Scanner;
using Pyraminx.Solver;

namespace Pyraminx.App.Service
{
    public enum SolutionState { Start, Init, Scan, Solve, Exec, Done }

    public delegate void OnSolutionProgress(SolutionState state);

    public class SolutionProcedure
    {
        protected string[] FlipSequence => new[] { null, "w+", "w+", "z-" };
        protected ILogger Logger { get; set; }

        public RobotConnection Robot { get; set; }
        public PyraminxSolver Solver { get; set; }

        protected List<IEnumerable<Facelet>> FaceScans { get; set; }
        protected Core.Pyraminx Model { get; set; }
        protected string Solution { get; set; }

        public event OnSolutionProgress OnSolutionProgress;

        public bool NeedsFaceScan { get; protected set; }
        protected object SyncObject = new object();
        protected Thread WorkerThread { get; set; }
        public bool InProgress => WorkerThread != null;
        public SolutionState CurrentState { get; protected set; }

        public SolutionProcedure(ILogger logger)
        {
            Logger = logger;
            CurrentState = SolutionState.Start;
        }

        public void SubmitFaceScan(IEnumerable<Facelet> facelets)
        {
            lock (SyncObject)
            {
                FaceScans.Add(facelets);
                NeedsFaceScan = false;
                Monitor.Pulse(SyncObject);
            }
        }

        protected void SetState(SolutionState state)
        {
            CurrentState = state;
            OnSolutionProgress?.Invoke(state);
        }

        public void Run()
        {
            if (InProgress)
                throw new Exception("Solution procedure is already in progress!");

            SetState(SolutionState.Init);

            Solution = null;
            Model = new Core.Pyraminx();
            FaceScans = new List<IEnumerable<Facelet>>();

            if (!Robot.Connected)
            {
                Utils.Toast("Robot is not connected");
                return;
            }

            WorkerThread = new Thread(async () =>
            {
                Logger.Debug("starting scan procedure");
                try
                {
                    await ScanFaces();
                    SaveFaces();

                    await FindSolution();

                    if (Solution == null)
                    {
                        Finish();
                        return;
                    }

                    if (Solution.Length > 0)
                        await ExecuteSolution();

                    Finish();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                Finish();
            });
            WorkerThread.Start();
        }

        protected void Finish()
        {
            WorkerThread = null;
            SetState(SolutionState.Done);
        }

        protected async Task ScanFaces()
        {
            foreach (var flip in FlipSequence)
            {
                SetState(SolutionState.Scan);
                if (flip != null)
                    await Robot.Flip(flip);

                // wait for camera focus
                await Task.Delay(1000);
                ScanFace();
            }
        }

        protected void ScanFace()
        {
            lock (SyncObject)
            {
                NeedsFaceScan = true;
                Logger.Debug("waiting for frame");
                Monitor.Wait(SyncObject);
                Logger.Debug(string.Join(", ", FaceScans.Last().Select(x => x.Matches[0].Label)));
            }
        }

        protected void SaveFaces()
        {
            StoreFacelets(FaceScans[0]);
            Logger.Debug(string.Join(", ", FaceScans[0].Select(x => x.Matches[0].Label)));
            Logger.Debug(Model.ToString());

            Model.Flip("w+");

            StoreFacelets(FaceScans[1]);
            Logger.Debug(string.Join(", ", FaceScans[1].Select(x => x.Matches[0].Label)));
            Logger.Debug(Model.ToString());

            Model.Flip("w+");

            StoreFacelets(FaceScans[2]);
            Logger.Debug(string.Join(", ", FaceScans[2].Select(x => x.Matches[0].Label)));
            Logger.Debug(Model.ToString());

            Model.Flip("z-");

            StoreFacelets(FaceScans[3]);
            Logger.Debug(string.Join(", ", FaceScans[3].Select(x => x.Matches[0].Label)));
            Logger.Debug(Model.ToString());
        }

        protected void StoreFacelets(IEnumerable<Facelet> facelets)
        {
            var colors = facelets.Select(x => x.Matches[0].Label).ToArray();
            Model[2, 0, 0, 0].Y = colors[0];
            Model[1, 0, 0, 1].Y = colors[1];
            Model[1, 0, 0, 0].Y = colors[2];
            Model[1, 1, 0, 0].Y = colors[3];
            Model[0, 0, 0, 2].Y = colors[4];
            Model[0, 0, 0, 1].Y = colors[5];
            Model[0, 1, 0, 1].Y = colors[6];
            Model[0, 1, 0, 0].Y = colors[7];
            Model[0, 2, 0, 0].Y = colors[8];
        }

        public async Task FindSolution()
        {
            Logger.Debug("SolutionProcedure.FindSolution");
            SetState(SolutionState.Solve);
            Solution = await Solver.FindSolution(Model);
        }

        public async Task ExecuteSolution()
        {
            Logger.Debug("SolutionProcedure.ExecuteSolution");
            SetState(SolutionState.Exec);
            await Robot.Execute(Solution);
        }
    }
}