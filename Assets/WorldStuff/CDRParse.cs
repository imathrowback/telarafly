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
    class CDRParse
    {
        public static void getMinMax(string worldName, ref int x, ref int y)
        {
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

       

        public static void doWorldTile(AssetDatabase adb, DB db, string worldName, int x, int y, Action<ObjectPosition> addFunc)
        {
            string s = worldName + "_" + x + "_" + y + ".cdr";

            try
            {
                processCDR(s, addFunc, adb, db);
                // also add the terrain nif!
                String type = "_split";
                String terrainNif = String.Format("{0}_terrain_{1}_{2}{3}.nif", worldName, x, y, type);
                if (adb.filenameExists(terrainNif))
                {
                    Vector3 pos = new Vector3(x, 0.0f, y);
                    addFunc.Invoke(new ObjectPosition(terrainNif, pos, Quaternion.identity, pos, 1.0f));
                }

            }
            catch (ThreadAbortException ex)
            {
                UnityEngine.Debug.Log("Unable to process CDR:" + s + " due to error:" + ex.Message);
                return;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("Unable to process CDR:" + s + " due to error:" + ex.Message + ":\n" + ex);
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

        static void processCDR(Stream ms, string cdrName, Action<ObjectPosition> addFunc, DB db)
        {
            try
            {
                CObject obj = Parser.processStreamObject(ms);

                if (obj.type != 107)
                    throw new Exception("CDR file was not class 107");

                String oname = "";

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
                                List<CObject> cMembers = child.members;

                                CObject index = cMembers[0];
                                if (cMembers.Count > 1)
                                {
                                    CObject nameObj = cMembers[1];
                                    CStringConvertor sconv = (CStringConvertor)nameObj.getConvertor();
                                    oname = (string)sconv.convert(nameObj);
                                    CObject ary = null;
                                    if (cMembers.Count == 3)
                                        ary = cMembers[2];
                                    else if (cMembers.Count == 4)
                                    {
                                        String setdec = cMembers[2].get(0).get(0).convert() + "";
                                        // System.out.println(setdec);
                                        ary = cMembers[3];
                                    }
                                    else
                                    {
                                        // dunno, guess?
                                        foreach (CObject o in cMembers)
                                            if (o.members.Count == 4)
                                                ary = o;
                                    }
                                    if (null == ary)
                                        throw new Exception("Unable to handle cMembers size:" + cMembers.Count);
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

                                        try
                                        {
                                            nif_hkx_ref = Convert.ToInt64(_602.get(0).convert());
                                            CObject _603 = findFirstType(ary, 603);

                                            Vector3 min = _603.members[1].readVec3();
                                            Quaternion qut = _603.members[2].readQuat();

                                            float unkValue = 0;
                                            int _3index = 3;
                                            Vector3 max = new Vector3();
                                            float scale = 1.0f;
                                            if (_603.members.Count >= 4)
                                            {
                                                if (_603.members[3].type == 11)
                                                    max = _603.members[3].readVec3();
                                                else
                                                {
                                                    //System.out.println(_603.members.get(3).convert());
                                                    if (_603.members.Count >= 5
                                                            && _603.members[4].type == 11)
                                                    {
                                                        scale = (float)(CFloatConvertor.inst.convert(_603.members[3]));
                                                        max = _603.members[4].readVec3();
                                                    }
                                                }
                                            }
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

                                                            addFunc.Invoke(new Assets.ObjectPosition(nifFile, min, qut, max, scale));
                                                        }
                                                    }
                                                }

                                            }
                                            // does it have a light source?
                                            if (_602.hasMember(3))
                                            {
                                                CObject lary = _602.getMember(3);
                                                //Debug.Log("found a light! process ary:" + lary);
                                                float r = (float)CFloatConvertor.inst.convert(lary.get(0));
                                                float g = (float)CFloatConvertor.inst.convert(lary.get(1));
                                                float b = (float)CFloatConvertor.inst.convert(lary.get(2));
                                                float range = 0;
                                                if (_602.hasMember(4))
                                                    range = (float)CFloatConvertor.inst.convert(_602.getMember(4));
                                                addFunc.Invoke(new Assets.LightPosition(range, r, g, b, min, qut, max, scale));
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
