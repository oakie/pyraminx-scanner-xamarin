using Android.OS;

namespace Pyraminx.App.Service
{
    public class PyraminxBinder : Binder
    {
        public PyraminxService Service { get; protected set; }

        public PyraminxBinder(PyraminxService service)
        {
            Service = service;
        }
    }
}