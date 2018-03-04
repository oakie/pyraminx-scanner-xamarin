using System.Collections.Generic;

using Android.OS;
using Android.Widget;
using Pyraminx.Common;
using System;
using Android.Support.V4.App;
using Android.Views;

namespace Pyraminx.App.Views
{
    public class ControlFragment : BaseFragment
    {
        protected override int Layout => Resource.Layout.Control;
        public new static string Title => "Robot";

        protected List<Button> RequiresRobotConnection = new List<Button>();

        public new static BaseFragment Create()
        {
            Utils.Log("ControlFragment.Create");
            return new ControlFragment();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle bundle)
        {
            Utils.Log("ControlFragment.OnCreateView");
            var root = base.OnCreateView(inflater, container, bundle);

            RequiresRobotConnection.Clear();

            var reset = root.FindViewById<Button>(Resource.Id.RobotResetBtn);
            RequiresRobotConnection.Add(reset);
            reset.Click += async (sender, args) =>
            {
                Utils.Toast("Resetting robot");
                try
                {
                    await Service.Robot.Reset();
                }
                catch (Exception e)
                {
                    Utils.Toast(e.ToString());
                }
            };

            var twists = new Dictionary<int, string> {
                {Resource.Id.TwistWPosBtn, "w+"},
                {Resource.Id.TwistWNegBtn, "w-"},
                {Resource.Id.TwistXPosBtn, "x+"},
                {Resource.Id.TwistXNegBtn, "x-"},
                {Resource.Id.TwistYPosBtn, "y+"},
                {Resource.Id.TwistYNegBtn, "y-"},
                {Resource.Id.TwistZPosBtn, "z+"},
                {Resource.Id.TwistZNegBtn, "z-"}
            };
            foreach (var id in twists.Keys)
            {
                var button = root.FindViewById<Button>(id);
                RequiresRobotConnection.Add(button);
                button.Click += async (sender, args) =>
                {
                    Utils.Toast("Executing: " + twists[id]);
                    try
                    {
                        await Service.Robot.Execute(twists[id]);
                    }
                    catch (Exception e)
                    {
                        Utils.Toast(e.ToString());
                    }
                };
            }

            CheckRobotAvailable();

            return root;
        }

        protected override void OnServiceConnectionChanged(bool connected)
        {
            base.OnServiceConnectionChanged(connected);
            if (connected)
            {
                Service.Robot.OnBusyChanged += OnRobotBusyChanged;
                OnRobotBusyChanged(Service.Robot.Busy);
            }
            CheckRobotAvailable();
        }

        public override void OnResumeFragment()
        {
            Utils.Log("ControlFragment.OnResume");
            base.OnResumeFragment();

            if (ServiceBound)
            {
                Service.Robot.OnBusyChanged += OnRobotBusyChanged;
                OnRobotBusyChanged(Service.Robot.Busy);
            }
            CheckRobotAvailable();
        }

        public override void OnPauseFragment()
        {
            Utils.Log("ControlFragment.OnPause");

            if (ServiceBound)
            {
                Service.Robot.OnBusyChanged -= OnRobotBusyChanged;
            }

            base.OnPauseFragment();
        }

        protected void OnRobotBusyChanged(bool busy)
        {
            ParentActivity?.RunOnUiThread(CheckRobotAvailable);
        }

        protected void CheckRobotAvailable()
        {
            var available = Service != null && Service.Robot.Connected && !Service.Robot.Busy;
            RequiresRobotConnection.ForEach(x => x.Enabled = available);
        }
    }
}