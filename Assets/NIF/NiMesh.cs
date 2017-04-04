using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.NIF
{
    class NiMesh : NiRenderObject
    {
        public int meshPrimType;
        public int numSubMeshes;
        public bool isInstanced;
        public Point3f boundsCenter;
        public float boundsRad;
        public int numStreamRefs;
        public List<NifMeshStream> streamRefs;
        public List<int> modLinks;

        public List<String> getStreams()
        {
            List<String> names = new List<String>();
            foreach (NifMeshStream streamRef in streamRefs)
            {
                for (int i = 0; i < streamRef.elementDescs.Count(); i++)
                {
                    Pair<String, int> pair = streamRef.elementDescs[i];
                    String elemCheckName = pair.First;
                    names.Add(elemCheckName);
                }
            }
            return names;
        }

        public StreamAndElement getStreamAndElement(NIFFile file, String elementName, int preferredIndex)
        {
            foreach (NifMeshStream streamRef in streamRefs)
            {
                for (int i = 0; i < streamRef.elementDescs.Count(); i++)
                {
                    Pair<String, int> pair = streamRef.elementDescs[i];
                    String elemCheckName = pair.getKey();
                    int elemCheckIndex = pair.getValue();
                    if (preferredIndex == -1 || elemCheckIndex == preferredIndex)
                    {
                        if (elemCheckName.StartsWith(elementName))
                        {
                            NiDataStream dataStream = (NiDataStream)file.getObjects()[streamRef.streamLinkID];
                            if (dataStream == null)
                                Debug.Log("null dataStream");
                            if (dataStream.streamElems == null)
                                Debug.Log("null dataStream.streamElems: " + dataStream);
                            if (i >= dataStream.streamElems.Count())
                                Debug.Log("WARNING: Data stream does not have enough elements.");
                            else
                            {
                                NifStreamElement elem = dataStream.streamElems[i];
                                return new StreamAndElement(streamRef, elem, dataStream);
                            }
                        }
                    }
                }
            }
            return null;
        }


        public override void parse(NIFFile file, NIFObject baseo, BinaryReader ds)
        {
            base.parse(file, baseo, ds);

            loadRenderable(file, ds);
            meshPrimType = ds.readInt();
            numSubMeshes = ds.readUnsignedShort();
            isInstanced = ds.readUnsignedByte() > 0;
            boundsCenter = new Point3f(ds.readFloat(), ds.readFloat(), ds.readFloat());
            boundsRad = ds.readFloat();
            numStreamRefs = ds.readInt();
            streamRefs = new List<NifMeshStream>();

            for (int i = 0; i < numStreamRefs; i++)
            {
                NifMeshStream meshStream = new NifMeshStream();
                meshStream.parse(file, baseo, ds);
                streamRefs.Add(meshStream);
            }

            modLinks = loadLinkIDs(ds);
            file.addMesh(this);

        }

        internal NiTexturingProperty getTexturingProperty(NIFFile nf)
        {
            List<int> propIDs = nodePropertyIDs;
            foreach (int propID in propIDs)
            {
                NIFObject obj = nf.getObject(propID);
                if (obj is NiTexturingProperty)
                {
                    NiTexturingProperty propObj = (NiTexturingProperty)obj;
                    return propObj;
                }
            }
            return null;
        }
    }
}
