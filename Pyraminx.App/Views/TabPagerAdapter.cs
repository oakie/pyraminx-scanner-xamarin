using System;
using System.Collections.Generic;
using Android.Support.V4.App;
using Android.Views;
using Pyraminx.Common;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using Object = Java.Lang.Object;

namespace Pyraminx.App.Views
{
    public class TabPagerAdapter : FragmentStatePagerAdapter
    {
        protected delegate BaseFragment FactoryMethod();

        protected BaseFragment Current, Previous;

        protected static FactoryMethod[] Factories =
        {
            ScannerFragment.Create,
            SolverFragment.Create,
            ControlFragment.Create
        };
        protected static string[] Titles =
        {
            ScannerFragment.Title,
            SolverFragment.Title,
            ControlFragment.Title
        };
        protected BaseFragment[] Fragments = new BaseFragment[Factories.Length];

        public override int Count => Factories.Length;

        public TabPagerAdapter(FragmentManager fm) : base(fm) { }

        public override Fragment GetItem(int position)
        {
            Utils.Log("TabPagerAdapter.GetItem " + position);
            Fragments[position] = Factories[position]();

            if (Current == null)
                Current = Fragments[position];

            return Fragments[position];
        }

        //public override int GetItemPosition(Object @object)
        //{
        //    return PositionNone;
        //}

        public override void DestroyItem(ViewGroup container, int position, Object @object)
        {
            Utils.Log("TabPagerAdapter.DestroyItem: " + position);
            //var fragment = (Fragment) @object;
            //var mgr = fragment.FragmentManager;
            //var tx = mgr.BeginTransaction();
            //tx.Remove(fragment);
            //tx.Commit();

            //var fragment = Fragments[position];
            //container.RemoveView(fragment.View);
        }

        public override Java.Lang.ICharSequence GetPageTitleFormatted(int position)
        {
            return new Java.Lang.String(Titles[position]);
        }

        public void OnPageSelected(int position)
        {
            Utils.Log("TabPagerAdapter.OnPageSelected " + position);

            Previous = Current;
            Current = Fragments[position];

            Previous?.OnPauseFragment();
            Current?.OnResumeFragment();
        }
    }
}
