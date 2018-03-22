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

        Queue<Action> queuedActions = new Queue<Action>();
        Queue<TexInfo> texQueue = new Queue<TexInfo>();

        public void addQueuedTextureAction(Action a)
        {
            queuedActions.Enqueue(a);
        }
        internal Texture2D add(int width, int height, TextureFormat format, bool mip, byte[] data)
        {
            Texture2D texture2 = new Texture2D(width, height, format, mip);
            TexInfo ti = new Assets.NIF.TexInfo(texture2, data);

            add(ti);
            return texture2;
        }

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
                DateTime end = DateTime.Now.AddMilliseconds(1);
                while (texQueue.Count() > 0)
                {
                    TexInfo ti = texQueue.Dequeue();
                    Texture2D t = ti.t;
                    t.LoadRawTextureData(ti.data);
                    t.Compress(true);
                    t.Apply(true, true);
                    if (DateTime.Now > end)
                        break;
                }
                while (queuedActions.Count() > 0)
                {
                    Action a = queuedActions.Dequeue();
                    a.Invoke();
                    if (DateTime.Now > end)
                        break;
                }
            });
        }

        /** Try to lock the object and then perform the action. If the object cannot be locked, then don't run the action
         */
        private void TryWithLock(object lockObj, Action a)
        {
            if (Monitor.TryEnter(lockObj))
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
