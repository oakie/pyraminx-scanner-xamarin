using Pyraminx.Common;
using Pyraminx.Core;
using Pyraminx.Robot;
using Pyraminx.Scanner;
using Pyraminx.Solver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pyraminx.App.Service
{
    public enum SolutionState { Start, Init, Scan, Solve, Exec, Done }
    public enum RunMode { Continuous, Halted }

    public delegate void OnProgressUpdate(SolutionState state);
    public delegate void OnModelUpdate(Core.Pyraminx pyraminx);
    public delegate void OnSolutionUpdate(string solution);

    public class SolutionProcedure
    {
        protected string[] FlipSequence => new[] { null, "w+", "w+", "z-" };
        protected ILogger Logger { get; set; }

        public RobotConnection Robot { get; set; }
        public PyraminxSolver Solver { get; set; }

        protected List<IEnumerable<Facelet>> FaceScans { get; set; }
        public Core.Pyraminx Model { get; protected set; }
        public string Solution { get; protected set; }

        public event OnProgressUpdate OnProgressUpdate;
        public event OnModelUpdate OnModelUpdate;
        public event OnSolutionUpdate OnSolutionUpdate;

        public bool AwaitingFaceScan { get; protected set; }
        protected object ScanSyncObject = new object();
        public bool AwaitingGoAhead { get; protected set; }
        protected object HaltSyncObject = new object();

        protected Thread WorkerThread { get; set; }
        public bool InProgress => WorkerThread != null;

        public SolutionState CurrentState { get; protected set; }
        public RunMode CurrentMode { get; protected set; }

        public SolutionProcedure(ILogger logger)
        {
            Logger = logger;
            CurrentState = SolutionState.Start;
        }

        public void DeliverFaceScan(IEnumerable<Facelet> facelets)
        {
            lock (ScanSyncObject)
            {
                FaceScans.Add(facelets);
                AwaitingFaceScan = false;
                Monitor.Pulse(ScanSyncObject);
            }
        }

        public void DeliverGoAhead()
        {
            lock (HaltSyncObject)
            {
                AwaitingGoAhead = false;
                Monitor.Pulse(HaltSyncObject);
            }
        }

        protected void SetState(SolutionState state)
        {
            CurrentState = state;
            OnProgressUpdate?.Invoke(state);
        }

        public void Run(RunMode mode = RunMode.Continuous)
        {
            if (InProgress)
                throw new Exception("Solution procedure is already in progress!");

            CurrentMode = mode;
            SetState(SolutionState.Init);
            Solution = null;
            Model = new Core.Pyraminx();
            FaceScans = new List<IEnumerable<Facelet>>();

            OnModelUpdate?.Invoke(Model);
            OnSolutionUpdate?.Invoke(Solution);

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
                    foreach (var flip in FlipSequence)
                    {
                        AwaitGoAhead();
                        await ScanFace(flip);
                    }

                    ClassifyFaces();
                    AwaitGoAhead();

                    await FindSolution();
                    AwaitGoAhead();

                    if (Solution == null)
                    {
                        Finish();
                        return;
                    }

                    if (Solution.Length > 0)
                        await ExecuteSolution();
                }
                catch (Exception e)
                {
                    Logger.Debug(e.ToString());
                }

                Finish();
            });
            WorkerThread.Start();
        }

        protected void Finish()
        {
            Logger.Debug("Finish solution procedure");
            WorkerThread = null;
            SetState(SolutionState.Done);
        }

        protected async Task ScanFace(string flip)
        {
            SetState(SolutionState.Scan);
            if (flip != null)
                await Robot.Flip(flip);

            // wait for camera focus
            await Task.Delay(1000);
            AwaitCameraFrame();
        }

        protected void AwaitGoAhead()
        {
            if (CurrentMode == RunMode.Continuous)
                return;

            lock (HaltSyncObject)
            {
                AwaitingGoAhead = true;
                OnProgressUpdate?.Invoke(CurrentState);
                Logger.Debug("waiting for go-ahead");
                Monitor.Wait(HaltSyncObject);
            }
        }

        protected void AwaitCameraFrame()
        {
            lock (ScanSyncObject)
            {
                AwaitingFaceScan = true;
                Logger.Debug("waiting for frame");
                Monitor.Wait(ScanSyncObject);
                Logger.Debug(string.Join(", ", FaceScans.Last().Select(x => x.Matches[0].Label)));
            }
        }

        protected void ClassifyFaces()
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

            OnModelUpdate?.Invoke(Model);
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
            OnSolutionUpdate?.Invoke(Solution);
        }

        public async Task ExecuteSolution()
        {
            Logger.Debug("SolutionProcedure.ExecuteSolution");
            SetState(SolutionState.Exec);
            await Robot.Execute(Solution);
        }
    }
}