using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Assets.NIF
{
    public class TexInfo
    {
        public Texture2D t;
        public byte[] data;

        public TexInfo(Texture2D t, byte[] data)
        {
            this.t = t;
            this.data = data;
        }
    }
    public class NIFTexturePool
    {
        private static NIFTexturePool tex;

        public static NIFTexturePool inst
        {
            get
            {
                if (tex == null)
                    tex = new NIFTexturePool();
                return tex;
            }
        }

        private NIFTexturePool()
        {

        }

        Queue<TexInfo> texQueue = new Queue<TexInfo>();

        public void add(TexInfo t)
        {
            lock (texQueue)
            {
                texQueue.Enqueue(t);
            }
        }

        [CallFromUnityUpdate]
        public void process()
        {
            TryWithLock(texQueue, () =>
            {
                DateTime end = DateTime.Now.AddMilliseconds(50);
                while (texQueue.Count() > 0)
                {
                    TexInfo ti = texQueue.Dequeue();
                    Texture2D t = ti.t;
                    t.LoadRawTextureData(ti.data);
                    t.Apply(true, false);
                    if (DateTime.Now > end)
                        break;
                }
            });
        }

        /** Try to lock the object and then perform the action. If the object cannot be locked within 5ms, then don't run the action
         */
        private void TryWithLock(object lockObj, Action a)
        {
            if (Monitor.TryEnter(lockObj, 5))
            {
                try
                {
                    a.Invoke();
                }
                finally
                {
                    Monitor.Exit(lockObj);
                }
            }
        }
    }
}
