using SCG = System.Collections.Generic;
using KdTree;
using C5;
using System.Linq;

using System;
using System.Linq;
using System.Text;
using UnityEngine;
using Assets.WorldStuff;
using Assets.Database;

namespace Assets
{
    [AttributeUsage(AttributeTargets.Method)]
    class CallFromUnityUpdate : System.Attribute
    {

    }
    class WorldLoadingThread
    {
        KdTree.KdTree<float, SCG.List<NifLoadJob>> postree = new KdTree<float, SCG.List<NifLoadJob>>(2, new KdTree.Math.FloatMath(), AddDuplicateBehavior.Error);
        KdTree.KdTree<float, SCG.List<NifLoadJob>> terraintree = new KdTree<float, SCG.List<NifLoadJob>>(2, new KdTree.Math.FloatMath(), AddDuplicateBehavior.Error);
        SCG.List<ObjectPosition> objectPositions;

        HashSet<long> processedTiles = new HashSet<long>();

        SCG.List<KeyValuePair<int, int>> cdrJobQueue = new SCG.List<KeyValuePair<int, int>>();
        int MAX_TERRAIN_THREADS = 2;
        int MAX_RUNNING_THREADS = 6;
        volatile int runningTerrainThreads = 0;
        System.Threading.Thread worldThread;
        TreeDictionary<long, NifLoadJob> terrainRunningList;
        TreeDictionary<long, NifLoadJob> objectRunningList;

        public Vector3 telaraWorldCamPos { get; set; }
        public Vector3 cameraWorldCamPos { get; set; }
        volatile bool shutdown = false;
        public void doShutdown()
        {
            shutdown = true;
            Debug.Log("SHUTDOWN world loader thread");

            DateTime end = DateTime.Now.AddMilliseconds(10000);
            do
            {
                if (DateTime.Now > end)
                {
                    worldThread.Abort();
                    break;
                }

            }
            while (worldThread.IsAlive) ;
            Debug.Log("world loader thread is now shutdown");

        }
        public WorldLoadingThread()
        {
            objectRunningList = new TreeDictionary<long, NifLoadJob>();
            terrainRunningList = new TreeDictionary<long, NifLoadJob>();
            objectPositions = new SCG.List<ObjectPosition>();
            MAX_RUNNING_THREADS = ProgramSettings.get("MAX_RUNNING_THREADS", 10);
        }

        public void startThread()
        {
            worldThread = new System.Threading.Thread(worldLoadLoop);
            worldThread.Start();
        }

        volatile int tListCountEstimate;
        volatile int oListCountEstimate;

        public int tCount()
        {
            return (tListCountEstimate + oListCountEstimate);
        }
        public int availThreads()
        {
            return MAX_RUNNING_THREADS - (tListCountEstimate + oListCountEstimate);
        }

        void processCDRQueue()
        {

            int tileX = Mathf.FloorToInt(telaraWorldCamPos.x / 256.0f);
            int tileY = Mathf.FloorToInt(telaraWorldCamPos.z / 256.0f);
            cdrJobQueue = cdrJobQueue.OrderBy(x => Vector2.Distance(new Vector2(tileX, tileY), new Vector2(x.Key, x.Value))).ToList();
            while (runningTerrainThreads < MAX_TERRAIN_THREADS && cdrJobQueue.Count() > 0)
            {
                KeyValuePair<int, int> job = cdrJobQueue[0];
                cdrJobQueue.RemoveAt(0);
                int tx = job.Key;
                int ty = job.Value;
                runningTerrainThreads++;
                //Debug.Log("Starting thread for CDR job[" + tx + "," + ty + "]");

                System.Threading.Thread m_Thread = new System.Threading.Thread(() =>
                {
                    try
                    {
                        SCG.List<ObjectPosition> objs = new SCG.List<ObjectPosition>();
                        CDRParse.doWorldTile(AssetDatabaseInst.DB, DBInst.inst, GameWorld.worldName, tx * 256, ty * 256, (p) =>
                        {
                            objs.Add(p);
                        });
                        lock (objectPositions)
                        {
                            objectPositions.AddRange(objs);
                        }
                    }
                    finally
                    {
                        runningTerrainThreads--;
                    }
                });
                m_Thread.Priority = (System.Threading.ThreadPriority)ProgramSettings.get("MAP_LOAD_THREAD_PRIORITY", (int)System.Threading.ThreadPriority.Normal);
                m_Thread.Start();
            }
        }
        void submitCDRJob(int tx, int ty)
        {
            long key = Combine(tx, ty);
            if (!processedTiles.Contains(key))
            {
                //Debug.Log("Submitting new CDR job[" + tx + "," + ty + "]");
                cdrJobQueue.Add(new KeyValuePair<int, int>(tx, ty));
                processedTiles.Add(key);
            }

        }

        public void worldLoadLoop()
        {
            Debug.Log("starting world loader thread");
            while (!shutdown)
            {
                // abort this thread if we arn't playing
                try
                {
                    worldLoad();
                }
                catch (Exception ex)
                {
                    Debug.LogError("Exception in world thread");
                    Debug.LogError(ex);
                }
            }
            Debug.Log("ending world loader thread");
        }

        private void worldLoad()
        {
            processJobAdds();
            /**
             * Load the world tiles around the camera and their objects 
            */
            int tileX = Mathf.FloorToInt(telaraWorldCamPos.x / 256.0f);
            int tileY = Mathf.FloorToInt(telaraWorldCamPos.z / 256.0f);
            int[][] v = {
              new int[]{ -1, 1 },  new int[]{ 0, 1 },   new int[]{ 1, 1 },
                new int[]{ -1, 0 },  new int[]{ 0, 0 },   new int[]{ 1, 0 },
                new int[]{ -1, -1 },  new int[]{ 0, -1 },   new int[]{ 1, -1 },
            };
            int range = ProgramSettings.get("TERRAIN_VIS", 10);
            submitCDRJob(tileX, tileY);
            for (int txx = tileX - range; txx <= tileX + range; txx++)
                for (int txy = tileY - range; txy <= tileY + range; txy++)
                    submitCDRJob(txx, txy);
            processCDRQueue();

            if (availThreads() > 0)
            {
                Vector3 camPos = cameraWorldCamPos;
                //getWorldCamPos();
                float[] camPosF = new float[] { camPos.x, camPos.z };

                lock (terraintree)
                {
                    lock (postree)
                    {
                        KdTreeNode<float, SCG.List<NifLoadJob>>[] tercandidates = this.terraintree.RadialSearch(camPosF, ProgramSettings.get("TERRAIN_VIS", 10) * 256, MAX_RUNNING_THREADS);
                        KdTreeNode<float, SCG.List<NifLoadJob>>[] candidates = this.postree.RadialSearch(camPosF, ProgramSettings.get("OBJECT_VISIBLE", 500), MAX_RUNNING_THREADS);
                        SCG.IEnumerable<NifLoadJob> terjobs = tercandidates.SelectMany(e => e.Value);
                        // always have at least two terrain job running 
                        lock (terrainRunningList)
                        {
                            tListCountEstimate = terrainRunningList.Count;

                            if (terjobs.Count() > 0 && terrainRunningList.Count <= 2)
                            {
                                terjobs = terjobs.OrderBy(n => Vector3.Distance(n.parentPos, camPos));

                                SCG.List<NifLoadJob> jobs = terjobs.ToList();
                                startJob(jobs[0], terrainRunningList, tercandidates);
                                if (candidates.Count() < availThreads())
                                {
                                    if (terjobs.Count() > 1)
                                        startJob(jobs[1], terrainRunningList, tercandidates);
                                }

                                foreach (KdTreeNode<float, SCG.List<NifLoadJob>> n in tercandidates)
                                    if (n.Value.Count == 0)
                                        terraintree.RemoveAt(n.Point);
                            }
                        }

                        if (availThreads() > 0)
                        {

                            oListCountEstimate = objectRunningList.Count;
                            SCG.IEnumerable<NifLoadJob> otherjobs = candidates.SelectMany(e => e.Value);
                            otherjobs = otherjobs.OrderBy(n => Vector3.Distance(n.parentPos, camPos));
                            foreach (NifLoadJob job in otherjobs)
                            {
                                if (availThreads() > 0)
                                {
                                    lock (objectRunningList)
                                    {
                                        startJob(job, objectRunningList, candidates);
                                    }
                                }
                                else
                                    break;
                            }

                            foreach (KdTreeNode<float, SCG.List<NifLoadJob>> n in candidates)
                                if (n.Value.Count == 0)
                                    postree.RemoveAt(n.Point);

                        }
                    }
                }
                
            }
        }

      

        private static long Combine(int x, int y)
        {
            return (long)(((ulong)x) | ((ulong)y) << 32);
        }

        public bool IsVisibleFrom(Vector3 v, Camera camera)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
            return GeometryUtility.TestPlanesAABB(planes, new Bounds(v, Vector3.one));
        }

        [CallFromUnityUpdate]
        void processLoadingQueue()
        {
            // Handle loading capsule queue
            lock (loadingCapsuleQueue)
            {
                while (!loadingCapsuleQueue.IsEmpty)
                    addLoading(loadingCapsuleQueue.Dequeue());

            }
        }
        IQueue<NifLoadJob> loadingCapsuleQueue = new CircularQueue<NifLoadJob>();
        void startJob(NifLoadJob job, TreeDictionary<long, NifLoadJob> runningList, KdTreeNode<float, SCG.List<NifLoadJob>>[] candidates)
        {
            //Debug.Log("Start job:" + job.filename);
            job.Start((System.Threading.ThreadPriority)ProgramSettings.get("OBJECT_LOAD_THREAD_PRIORITY", (int)System.Threading.ThreadPriority.Normal));
            lock (runningList)
            {
                runningList.Add(job.uid, job);
            }
            lock (loadingCapsuleQueue)
            {
                loadingCapsuleQueue.Enqueue(job);
            }
            foreach (KdTreeNode<float, SCG.List<NifLoadJob>> n in candidates)
                n.Value.Remove(job);

        }

        [CallFromUnityUpdate]
        internal void processThreadsUnityUpdate(Action<TreeDictionary<long, NifLoadJob>> processRunningList, Func<ObjectPosition, GameObject> process)
        {
            lock (objectRunningList)
            {
                processRunningList(objectRunningList);
                oListCountEstimate = objectRunningList.Count;
            }
            lock (terrainRunningList)
            { 
                processRunningList(terrainRunningList);
                tListCountEstimate = terrainRunningList.Count;
            }
            lock (objectPositions)
            {
                DateTime end = DateTime.Now.AddMilliseconds(100);
                while (objectPositions.Count() > 0)
                {
                    ObjectPosition p = objectPositions[0];
                    objectPositions.RemoveAt(0);
                    GameObject go = process(p);
                    // don't stay here too long
                    if (DateTime.Now > end)
                        break;
                }
            }
        }
        IQueue<NifLoadJob> jobsToAdd = new CircularQueue<NifLoadJob>();
        public void addJob(telara_obj parent, string filename)
        {
            NifLoadJob job = new NifLoadJob(filename);
            job.parent = parent;
            Vector3 pos = parent.transform.position;
            job.parentPos = pos;

            lock (jobsToAdd)
            {
                jobsToAdd.Enqueue(job);
            }

        }

        private void processJobAdds()
        {
            while (true)
            {
                NifLoadJob job;
                lock (jobsToAdd)
                {
                    if (jobsToAdd.IsEmpty)
                        break;
                    job = jobsToAdd.Dequeue();
                }
                Vector3 pos = job.parentPos;
                float[] floatf = new float[] { pos.x, pos.z };
                SCG.List<NifLoadJob> nList;
                if (job.filename.Contains("terrain") || job.filename.Contains("ocean"))
                {
                    lock (terraintree)
                    {
                        if (!this.terraintree.TryFindValueAt(floatf, out nList))
                        {
                            nList = new SCG.List<NifLoadJob>();
                            nList.Add(job);
                            this.terraintree.Add(floatf, nList);
                        }
                        else
                            nList.Add(job);
                    }
                }
                else
                {
                    lock (postree)
                    {
                        if (!this.postree.TryFindValueAt(floatf, out nList))
                        {
                            nList = new SCG.List<NifLoadJob>();
                            nList.Add(job);
                            this.postree.Add(floatf, nList);
                        }
                        else
                            nList.Add(job);
                    }
                }
            }
        }

        [CallFromUnityUpdate]
        private void addLoading(NifLoadJob job)
        {
            // Add a loading capsule to the location of the job 
            //if (job.parent.gameObject.transform.childCount == 0)
            {
                telara_obj obj = job.parent;
                GameObject loading = (GameObject)GameObject.Instantiate(Resources.Load("LoadingCapsule"));
                loading.name = "Loading";
                SphereCollider sp = obj.GetComponent<SphereCollider>();
                if (sp != null)
                    loading.transform.localScale = Vector3.one * 3;
                loading.transform.parent = obj.gameObject.transform;
                loading.transform.localPosition = Vector3.zero;
                loading.transform.localRotation = Quaternion.identity;
                //applyLOD(loading);
            }

        }
    }

}
