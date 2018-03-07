using System;
using System.Threading.Tasks;
using Pyraminx.Core;
using Thread = System.Threading.Thread;

namespace Pyraminx.Robot
{
    public delegate void OnConnectedChanged(bool connected);

    public delegate void OnBusyChanged(bool busy);

    public class RobotConnection
    {
        public const string DefaultUrl = "192.168.0.22:5000";

        public event OnConnectedChanged OnConnectedChanged;
        public event OnBusyChanged OnBusyChanged;

        protected ILogger Logger { get; set; }
        public string Url { get; protected set; }
        public bool Connected { get; protected set; }
        public bool Busy { get; protected set; }

        protected Thread ConnectionThread { get; set; }

        public RobotConnection(ILogger logger)
        {
            Logger = logger;
        }

        public void Connect(string url = DefaultUrl)
        {
            Logger.Debug("RobotConenction.Connect " + url);
            Url = url;

            ConnectionThread?.Abort();

            ConnectionThread = new Thread(() =>
            {
                while(Thread.CurrentThread.IsAlive)
                {
                    string response = RestHelper.Get(Url + "/ping").Result;
                    Logger.Debug($"response: {response}");

                    var before = Connected;
                    Connected = response == "pong";

                    if(before != Connected)
                    {
                        SetBusy(false);
                        OnConnectedChanged?.Invoke(Connected);
                    }

                    //if(!Connected)
                    //{
                    //    break;
                    //}

                    Thread.Sleep(3000);
                }
            });
            ConnectionThread.Start();
        }

        public void Disconnect()
        {
            Logger.Debug("RobotConnection.Disconnect");
            if(ConnectionThread != null && ConnectionThread.IsAlive)
            {
                ConnectionThread.Abort();
            }

            ConnectionThread = null;
            Connected = false;
            SetBusy(false);
            OnConnectedChanged?.Invoke(Connected);
        }

        public async Task Execute(string cmd)
        {
            if(Busy)
                throw new Exception("Robot is busy");

            if(!Connected)
                throw new Exception("Robot is not connected");

            Logger.Debug("RobotConenction.Execute " + cmd);

            SetBusy(true);
            var response = await RestHelper.Get(Url + "/execute/" + cmd);
            SetBusy(false);

            if (response != "ok")
            {
                throw new Exception("Robot command execution failed: " + response);
            }
        }

        public async Task Flip(string cmd)
        {
            if (Busy)
                throw new Exception("Robot is busy");

            if (!Connected)
                throw new Exception("Robot is not connected");

            Logger.Debug("RobotConenction.Flip " + cmd);

            SetBusy(true);
            var response = await RestHelper.Get(Url + "/flip/" + cmd);
            SetBusy(false);

            if (response != "ok")
            {
                throw new Exception("Robot command execution failed: " + response);
            }
        }

        public async Task Reset()
        {
            if(Busy)
                throw new Exception("Robot is busy");

            if(!Connected)
                throw new Exception("Robot is not connected");

            SetBusy(true);
            var response = await RestHelper.Get(Url + "/reset");
            SetBusy(false);

            if(response != "ok")
            {
                throw new Exception("Robot reset failed: " + response);
            }
        }

        protected void SetBusy(bool val)
        {
            Busy = val;
            OnBusyChanged?.Invoke(Busy);
        }
    }
}