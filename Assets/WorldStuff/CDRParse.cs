using Assets.Database;
using Assets.DatParser;
using Assets.RiftAssets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Assets.WorldStuff
{
    class Map
    {
        public List<Zone> zones;
        public List<Scene> scenes;
    }
    class Zone
    {
        public long _113Key;
        public string name;
        public List<Vector3> points = new List<Vector3>();
        public PolygonCollider2D collider;
        public List<string> sky = new List<string>();
    }
    class Scene
    {
        public long _114Key;
        public List<Vector3> points = new List<Vector3>();
        internal string name;
        internal PolygonCollider2D collider;
    }
    class CDRParse
    {
        static private string getLocalized(CObject obj, string defaultText)
        {
            if (obj == null)
                return defaultText;
            if (obj.type != 7703)
                throw new Exception("Object[" + obj.type + "]: Not a localizable entry");
            int textID = obj.getIntMember(0);
            return DBInst.lang_inst.getOrDefault(textID, defaultText);
        }

        private static string getZoneName(long key)
        {
            try
            {
                CObject entry = DBInst.inst.getObject(113, key);
                return getLocalized(entry.getMember(0), "");
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                return "";
            }
        }

        private static string getSceneName(long key)
        {
            try
            {
                CObject entry = DBInst.inst.getObject(114, key);
                return getLocalized(entry.getMember(0), "");
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                return "";
            }
        }

        private static List<string> getSky(long key)
        {
            List<string> ilist = new List<string>();
            CObject entry = DBInst.inst.getObject(114, key);
            CObject skyInfo = DBInst.inst.getObject(111, entry.getIntMember(31));
            CObject list = skyInfo.getMember(20);
            for (int i = 0; i < list.members.Count; i++)
            {
                int _7305 = list.getIntMember(i);
                ilist.Add(DBInst.inst.getObject(7305, _7305).getStringMember(2));
            }
            return ilist;
        }

        public static Map getMap(string worldNameNoCDR)
        {
            Map map = new Map();
            try
            {
                map.zones = getZones(worldNameNoCDR);
                map.scenes = getScenes(worldNameNoCDR);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
            return map;
        }

        private static List<Scene> getScenes(string worldNameNoCDR)
        {
            List<Scene> scenes = new List<Scene>();
            try
            {
                string worldName = worldNameNoCDR + "_map.cdr";
                AssetDatabase adb = AssetDatabaseInst.DB;
                if (!adb.getManifest().containsHash(Util.hashFileName(worldName)))
                    throw new Exception("Unable to find world name:" + worldName);
                //Debug.Log("loading world zones:" + worldName);
                byte[] data = adb.extractUsingFilename(worldName);
                CObject obj = Parser.processStreamObject(data);

                CObject scenesObj = obj.getMember(9);
                //Debug.Log("found scenes object with " + scenesObj.members.Count + " members");
                for (int i = 0; i < scenesObj.members.Count; i++)
                {
                    CObject sceneObj = scenesObj.get(i);
                    long key = sceneObj.getIntMember(0);
                    //Debug.Log("found scene with key:" + key);
                    List<Vector3> points = getPoints(sceneObj.getMember(3));
                    Scene scene = new WorldStuff.Scene();
                    scene._114Key = key;
                    scene.name = getSceneName(key);
                    scene.points = points;
                    scenes.Add(scene);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex);
            }
            return scenes;

        }

        private static List<Zone> getZones(string worldNameNoCDR)
        {
            List<Zone> zones = new List<Zone>();
            try
            {
                string worldName = worldNameNoCDR + "_map.cdr";
                AssetDatabase adb = AssetDatabaseInst.DB;
                if (!adb.getManifest().containsHash(Util.hashFileName(worldName)))
                    throw new Exception("Unable to find world name:" + worldName);
                Debug.Log("loading world zones:" + worldName);
                byte[] data = adb.extractUsingFilename(worldName);
                CObject obj = Parser.processStreamObject(data);

                CObject zonesObj = obj.getMember(8);
                Debug.Log("found zones object with " + zonesObj.members.Count + " members");
                for (int i = 0; i < zonesObj.members.Count; i++)
                {
                    CObject zoneObj = zonesObj.get(i);
                    long key = zoneObj.getIntMember(0);
                    //Debug.Log("found zone with key:" + key);
                    List<Vector3> points = getPoints(zoneObj.getMember(3));
                    Zone zone = new WorldStuff.Zone();
                    zone._113Key = key;
                    zone.name = getZoneName(key);
                    zone.points = points;
                    //zone.sky = getSky(key);
                    zones.Add(zone);


                }
            }catch (Exception ex)
            {
                Debug.LogWarning(ex);
            }
            return zones;

        }

        

        private static List<Vector3> getPoints(CObject cObject)
        {
            List<Vector3> p = new List<Vector3>();
            for (int i = 0; i < cObject.members.Count; i++)
            {
                CObject v = cObject.get(i);
                p.Add(v.getVector3Member(0));

            }
            return p;
        }

        public static void getMinMax(string worldName_, ref int x, ref int y)
        {
            string worldName = worldName_;
            if (!worldName_.Contains("cdr"))
                worldName = worldName + "_map.cdr";
            AssetDatabase adb = AssetDatabaseInst.DB;
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

            watch.Start();
            if (!adb.getManifest().containsHash(Util.hashFileName(worldName)))
                throw new Exception("Unable to find world name:" + worldName);
            byte[] data = adb.extractUsingFilename(worldName);
            //Debug.Log("extract in " + watch.ElapsedMilliseconds + " ms");

            watch.Reset(); watch.Start();
            CObject obj = Parser.processStreamObject(data);
            //Debug.Log("processStreamObject in " + watch.ElapsedMilliseconds + " ms");

            watch.Reset(); watch.Start();
            //Debug.Log("got world name:" + worldName);
            y = obj.getIntMember(3) * 256;
            if (obj.hasMember(2))
                x = obj.getIntMember(2) * 256;
            else
                x = y;
            //Debug.Log("get min max in " + watch.ElapsedMilliseconds + " ms");
            watch.Stop();
        }

       

        public static void doWorldTile(AssetDatabase adb, DB db, string worldName, int x, int y, Action<ObjectPosition> addFunc, bool terrainOnly = false)
        {
            string s = worldName + "_" + x + "_" + y + ".cdr";
            //Debug.Log("doWorldTile :" + s);

            try
            {
                // also add the terrain nif!
                String type = "_split";
                String terrainNif = String.Format("{0}_terrain_{1}_{2}{3}.nif", worldName, x, y, type);
                if (adb.filenameExists(terrainNif))
                {
                   // Debug.Log("add tile nif:" + terrainNif);

                    Vector3 pos = new Vector3(x, 0.0f, y);
                    addFunc.Invoke(new ObjectPosition(terrainNif, pos, Quaternion.identity, pos, 1.0f));
                    if (terrainOnly)
                        return;
                }
                else
                {
                    //Debug.Log("can't add tile nif:" + terrainNif + ", it doesn't exist");
                }

                processCDR(s, addFunc, adb, db);
            }
            catch (ThreadAbortException ex)
            {
                UnityEngine.Debug.Log("Unable to process CDR:" + s + " due to error:" + ex.Message);
                return;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log("Unable to process CDR:" + s + " due to error:" + ex.Message + ":\n" + ex);
            }
        }

        static void processCDR(String str, Action<ObjectPosition> addFunc, AssetDatabase adb, DB db)
        {
            if (!adb.filenameExists(str))
                return;
            AssetEntry ae = adb.getEntryForFileName(str);
            byte[] data = adb.extract(ae);
            if (data[0] != 0x6B)
            {
                UnityEngine.Debug.Log("Unknown code " + data[0] + ", expected:" + 0x6b);
                return;
            }
            processCDR(new MemoryStream(data), str, addFunc, db);

        }

        public static List<WorldSpawn> getSpawns(AssetDatabase adb, DB db, string world)
        {
            List<WorldSpawn> worlds = new List<WorldSpawn>();
            IEnumerable<entry> keys = db.getEntriesForID(4479);

            foreach (entry e in keys)
            {
                byte[] data = e.decompressedData;
                using (MemoryStream ms = new MemoryStream(data))
                {
                    CObject obj = Parser.processStreamObject(ms);
                    string worldName = obj.getStringMember(0);
                    string imagePath = obj.getStringMember(5);
                    string internalSpawnName = obj.getStringMember(1);
                    string spawnName = getLocalized(obj.getMember(10), internalSpawnName);
                    if (world != null)
                        if (!worldName.Equals(world))
                            continue;
                    try
                    {
                        Vector3 pos = obj.getVector3Member(2);
                        float angle = angle = obj.getFloatMember(3, 0);
                        pos.y += 2;

                        if (adb.filenameExists(worldName + "_map.cdr"))
                        {
                            WorldSpawn ws = new WorldSpawn(worldName, spawnName, pos, angle);
                            ws.imagePath = imagePath;
                            worlds.Add(ws);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log("Unable to get position for spawn [" + e.id + "][" + e.key + "]" + ex);
                    }
                }
            }
            return worlds;
        }

        static void processCDR(Stream ms, string cdrName, Action<ObjectPosition> addFunc, DB db)
        {
            try
            {
                CObject obj = Parser.processStreamObject(ms);

                if (obj.type != 107)
                    throw new Exception("CDR file was not class 107");

                

                List<CObject> members = obj.members;
                if (members.Count > 0)
                {
                    CObject first = members[0];
                    if (first.type == 11)
                    {
                        foreach (CObject child in first.members)
                        {
                            if (child.type == 600)
                            {

                                if (child.members.Count > 1)
                                {
                                    string oname = child.getStringMember(1);
                                    CObject ary = child.getMember(4);
                                    
                                    // child members in ary 602 and 603 contain references into the database under id 623
                                    // they point to object 628 which contains references to the actual NIF/HKX files
                                    long nif_hkx_ref = long.MaxValue;
                                    CObject _602 = findFirstType(ary, 602);
                                    if (_602 == null)
                                    {
                                        UnityEngine.Debug.Log("no nif ref found for :" + oname);
                                    }
                                    else
                                    {
                                        bool visible = true;
                                        // this is not a visibility property? _602.getBoolMember(2, true);

                                        try
                                        {
                                            nif_hkx_ref = Convert.ToInt64(_602.get(0).convert());
                                            CObject _603 = findFirstType(ary, 603);

                                            Quaternion qut = _603.getMember(4).readQuat();
                                            float scale = _603.getFloatMember(5, 1.0f);
                                            Vector3 translation = _603.getVector3Member(3);
                                            Vector3 centroid = translation;
                                            if (_603.hasMember(9))
                                                centroid = _603.getVector3Member(9);

                                            Vector3 min = translation;
                                            Vector3 max = centroid;

                                            if (nif_hkx_ref != long.MaxValue)
                                            {
                                                CObject dbObj = getDBObj(db, 623, nif_hkx_ref);
                                                if (dbObj != null)
                                                {
                                                    CObject dbAry = dbObj.get(0);
                                                    CObject _7319 = findFirstType(dbAry, 7319);
                                                    CObject _7318 = findFirstType(dbAry, 7318);
                                                    if (_7319 != null)
                                                    {
                                                        if (_7319.members.Count == 0)
                                                        {
                                                            // Collision only NIF, ignore it
                                                            // Debug.Log("empty 7319 for nif ref 623:" + nif_hkx_ref);
                                                        }
                                                        else
                                                        {
                                                            long nifKey = Convert.ToInt64(_7319.get(0).convert());
                                                            CObject _7305Obj = getDBObj(db, 7305, nifKey);
                                                            String nif = "" + _7305Obj.members[0].convert();

                                                            string nifFile = Path.GetFileName(nif);

                                                            Assets.ObjectPosition op = new Assets.ObjectPosition(nifFile, min, qut, max, scale);
                                                            op.index = child.index;
                                                            op.cdrfile = cdrName;
                                                            op.visible = visible;
                                                            op.entityname = oname;

                                                            addFunc.Invoke(op);
                                                        }
                                                    }
                                                }

                                            }
                                            // does it have a light source?
                                            if (_602.hasMember(3))
                                            {
                                                Vector3 color = _602.getVector3Member(3);
                                                float r = color.x;
                                                float g = color.y;
                                                float b = color.z;
                                                float range = _602.getFloatMember(4, 2.0f);
                                                Assets.LightPosition lp = new Assets.LightPosition(range, r, g, b, min, qut, max, scale);
                                                lp.visible = visible;
                                                lp.index = child.index;
                                                lp.cdrfile = cdrName;
                                                lp.entityname = oname;

                                                addFunc.Invoke(lp);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.Log(ex);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log("exception trying to process CDR:" + cdrName);
                Debug.Log(ex);
            }
            finally
            {
                //Debug.Log("process cdr[" + cdrName + "]: done in " + watch.ElapsedMilliseconds + " ms");
            }
            return;
        }

        static private CObject getDBObj(DB db, long id, long key)
        {
            if (!db.hasEntry(id, key))
                return null;
            byte[] dbData = db.getData(id, key);
            CObject obj = Parser.processStreamObject(new MemoryStream(dbData));

            return obj;
        }
       

        

        private static CObject findFirstType(CObject ary, int i)
        {
            foreach (CObject child in ary.members)
                if (child.type == i)
                    return child;

            return null;
        }
    }
}
