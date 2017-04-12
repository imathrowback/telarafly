using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.NIF
{
   
    
    public class NiSkinningMeshModifier : NiMeshModifier
    {
        public List<int> boneLinkIDs = new List<int>();
        public List<NITransform> m_pkSkinToBoneTransforms = new List<NITransform>();
        public int flags;
        public int m_uiBoneCount;
        public int rootBoneLinkID;
        public NITransform m_kRootBoneParentToSkinTransform;

        public override void parse(NIFFile file, NIFObject baseo, BinaryReader ds)
        {
            base.parse(file, baseo, ds);

            this.flags = ds.readUnsignedShort();
            this.rootBoneLinkID = loadLinkID(ds);

            m_kRootBoneParentToSkinTransform = NITransform.parse(ds); 
           
            this.m_uiBoneCount = ds.readInt();
            for (int i = 0; i < m_uiBoneCount; i++)
                boneLinkIDs.Add(loadLinkID(ds));
            for (int i = 0; i < m_uiBoneCount; i++)
                m_pkSkinToBoneTransforms.Add(NITransform.parse(ds));
            if ((flags & 2) == 0)
            {
                for (int i = 0; i < m_uiBoneCount; i++)
                {
                    Vector3 center = new Vector3(ds.readFloat(), ds.readFloat(), ds.readFloat());
                    float rad = ds.readFloat();
                }
            }
        }
    }
}
