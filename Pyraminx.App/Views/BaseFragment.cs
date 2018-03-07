using System;
using Android.OS;
using Android.Support.V4.App;
using Android.Views;
using Pyraminx.App.Misc;
using Pyraminx.App.Service;
using Pyraminx.Core;

namespace Pyraminx.App.Views
{
    public abstract class BaseFragment : Fragment
    {
        protected abstract int Layout { get; }
        public static string Title => "Base Fragment";
        protected static ILogger Logger = new Logger();

        public static BaseFragment Create()
        {
            throw new NotImplementedException();
        }

        protected BaseActivity ParentActivity => ((BaseActivity)Activity);
        protected PyraminxService Service => ParentActivity?.Service;
        protected bool ServiceBound => ParentActivity?.ServiceBound ?? false;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle bundle)
        {
            var root = inflater.Inflate(Layout, container, false);
            return root;
        }

        public virtual void OnResumeFragment()
        {
            ParentActivity.OnServiceConnectionChanged += OnServiceConnectionChanged;
            if (ServiceBound)
            {
                OnServiceConnectionChanged(true);
            }
        }

        public virtual void OnPauseFragment()
        {
            ParentActivity.OnServiceConnectionChanged -= OnServiceConnectionChanged;
        }

        protected virtual void OnServiceConnectionChanged(bool connected) { }
    }
}
