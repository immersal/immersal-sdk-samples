using UnityEngine;
using System.Threading;

namespace Immersal.Samples.Util
{
    public class MainThreadDispatch : IDispatch
    {
        public MainThreadDispatch () : base ()
        {
            targetThread = Thread.CurrentThread;
            Camera.onPostRender += updateLoop;
        }
    }
}
