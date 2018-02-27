using System.Collections.Generic;
using Android.App;
using Android.OS;
using Android.Widget;
using Pyraminx.Common;
using System;
using Android.Content;

namespace Pyraminx.App.Views
{
    [Activity(Label = "ControlActivity", ParentActivity = typeof(MainActivity))]
    public class ControlActivity : BaseActivity
    {
        protected override string Prefix => "ControlActivity>";
        protected override int Layout => Resource.Layout.Control;
        protected override string Header => "Robot Controls";

        protected List<Button> DisableWhileBusy = new List<Button>();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var reset = FindViewById<Button>(Resource.Id.RobotResetBtn);
            DisableWhileBusy.Add(reset);
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
                var button = FindViewById<Button>(id);
                DisableWhileBusy.Add(button);
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
        }

        protected void OnRobotBusyChanged(bool busy)
        {
            foreach (var button in DisableWhileBusy)
            {
                button.Enabled = !busy;
            }
        }

        public override void OnServiceConnected(ComponentName name, IBinder service)
        {
            base.OnServiceConnected(name, service);
            if (!ServiceBound)
                return;

            Service.Robot.OnBusyChanged += OnRobotBusyChanged;
        }

        protected override void OnStop()
        {
            if (ServiceBound)
            {
                Service.Robot.OnBusyChanged -= OnRobotBusyChanged;
            }

            base.OnStop();
        }
    }
}