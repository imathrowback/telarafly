using Assets.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Export
{
    class OBJExport
    {
        public void export(GameObject root, string output)
        {
            DB db = DBInst.inst;
            DoExport(root, true, output);


           
        }

        static void DoExport(GameObject obj, bool makeSubmeshes, string fileName)
        {

            string meshName = obj.name;

            ObjExporterScript.Start();
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                sw.Write("#" + meshName + ".obj"
                                    + "\n#" + System.DateTime.Now.ToLongDateString()
                                    + "\n#" + System.DateTime.Now.ToLongTimeString()
                                    + "\n#-------"
                                    + "\n\n");

                Transform t = obj.transform;

                Vector3 originalPosition = t.position;
                t.position = Vector3.zero;

                if (!makeSubmeshes)
                    sw.Write("g " + t.name + "\n");
                processTransform(t, makeSubmeshes, sw);


                t.position = originalPosition;

                ObjExporterScript.End();
                Debug.Log("Exported Mesh: " + fileName);
            }
        }

        static void processTransform(Transform t, bool makeSubmeshes, StreamWriter sw)
        {
            sw.Write("#" + t.name
                            + "\n#-------"
                            + "\n");

            if (makeSubmeshes)
                sw.Write("g " + t.name + "\n");

            MeshFilter mf = t.GetComponent<MeshFilter>();
            if (mf)
            {
                sw.Write(ObjExporterScript.MeshToString(mf, t));
            }

            for (int i = 0; i < t.childCount; i++)
            {
                processTransform(t.GetChild(i), makeSubmeshes, sw);
            }
        }

        

    }
}
