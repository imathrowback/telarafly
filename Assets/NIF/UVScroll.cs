using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.NIF
{
    class UVScroll : MonoBehaviour
    {
        public Material material;
        public float xRate = 0;
        public float yRate = 0;

        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void stop()
        {
            this.xRate = 0;
            this.yRate = 0;
        }

        void FixedUpdate()
        {
            if (material != null)
            {
                Vector2 v = material.mainTextureOffset;
                v.x += xRate / 480.0f;
                v.y += yRate / 480.0f;
                if (v.x > 1.0)
                    v.x = v.x - 1.0f;
                if (v.y > 1.0)
                    v.y = v.y - 1.0f;

                material.mainTextureOffset = v;
            }
        }

    }
}
