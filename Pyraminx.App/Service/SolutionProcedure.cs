using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pyraminx.Common;
using Pyraminx.Robot;
using Pyraminx.Scanner;
using Pyraminx.Solver;
using Pyraminx = Pyraminx.Core.Pyraminx;

namespace Pyraminx.App.Service
{
    public class SolutionProcedure
    {
        protected Core.Pyraminx Model { get; set; }
        protected RobotConnection Robot { get; set; }
        protected PyraminxSolver Solver { get; set; }

        protected List<IEnumerable<Facelet>> FaceScans { get; set; }
        protected string Solution { get; set; }

        public bool NeedsFaceScan { get; protected set; }
        protected object SyncObject = new object();

        public SolutionProcedure(RobotConnection robot)
        {
            Robot = robot;
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

        public void Run()
        {
            Model = new Core.Pyraminx();
            Solver = new PyraminxSolver();
            FaceScans = new List<IEnumerable<Facelet>>();

            if(!Robot.Connected)
            {
                Utils.Toast("Robot is not connected");
                return;
            }

            var t = new Thread(async () =>
            {
                Utils.Log("starting scan procedure");
                await ExecuteScan();

                StoreFacelets(FaceScans[0]);
                Utils.Log(string.Join(", ", FaceScans[0].Select(x => x.Matches[0].Label)));
                Utils.Log(Model.ToString());

                Model.Flip("w+");

                StoreFacelets(FaceScans[1]);
                Utils.Log(string.Join(", ", FaceScans[1].Select(x => x.Matches[0].Label)));
                Utils.Log(Model.ToString());

                Model.Flip("w+");

                StoreFacelets(FaceScans[2]);
                Utils.Log(string.Join(", ", FaceScans[2].Select(x => x.Matches[0].Label)));
                Utils.Log(Model.ToString());

                Model.Flip("z-");

                StoreFacelets(FaceScans[3]);
                Utils.Log(string.Join(", ", FaceScans[3].Select(x => x.Matches[0].Label)));
                Utils.Log(Model.ToString());
            });
            t.Start();
        }

        protected async Task ExecuteScan()
        {
            try
            {
                await Task.Delay(1000);
                lock (SyncObject)
                {
                    NeedsFaceScan = true;
                    Utils.Log("waiting for frame");
                    Monitor.Wait(SyncObject);
                }

                await Robot.Flip("w+");

                await Task.Delay(1000);
                lock (SyncObject)
                {
                    NeedsFaceScan = true;
                    Utils.Log("waiting for frame");
                    Monitor.Wait(SyncObject);
                }

                await Robot.Flip("w+");

                await Task.Delay(1000);
                lock (SyncObject)
                {
                    NeedsFaceScan = true;
                    Utils.Log("waiting for frame");
                    Monitor.Wait(SyncObject);
                }

                await Robot.Flip("z-");

                await Task.Delay(1000);
                lock (SyncObject)
                {
                    NeedsFaceScan = true;
                    Utils.Log("waiting for frame");
                    Monitor.Wait(SyncObject);
                }
            }
            catch(Exception e)
            {
                Utils.Log(e.ToString());
            }
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
            var state = Model.Serialize();
            Solution = await Solver.FindSolution(state);
        }

        public async Task ExecuteSolution()
        {
            await Robot.Execute(Solution);
        }
    }
}