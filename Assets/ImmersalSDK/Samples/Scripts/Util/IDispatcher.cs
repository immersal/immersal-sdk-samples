using System;
using System.Threading;
using UnityEngine;
using Queue = System.Collections.Generic.List<System.Action>;

namespace Immersal.Samples.Util
{
    public abstract class IDispatch
    {
        protected Thread targetThread;
        protected Queue pending, executing;
        protected readonly Camera.CameraCallback updateLoop;
        protected readonly object queueLock = new object();

        public virtual void Dispatch(Action action, bool repeating = false)
        {
            if (action == null) return;
            Action actionWrapper = action;
            if (repeating) actionWrapper = delegate()
            {
                action();
                lock (queueLock) pending.Add(actionWrapper);
            };
            if (Thread.CurrentThread.ManagedThreadId == targetThread.ManagedThreadId && !repeating) actionWrapper();
            else lock (queueLock) pending.Add(actionWrapper);
        }
        
        public virtual void Release()
        {
            Camera.onPostRender -= updateLoop;
            pending.Clear(); executing.Clear();
            pending = executing = null;
        }

        protected virtual void Update()
        {
            lock (queueLock) {
                executing.AddRange(pending);
                pending.Clear();
            }
            executing.ForEach(e => e());
            executing.Clear();
        }

        protected IDispatch()
        {
            updateLoop = cam => Update();
            pending = new Queue();
            executing = new Queue();
        }
    }
}
