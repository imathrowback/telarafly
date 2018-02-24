﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets
{
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>Source: https://forum.unity3d.com/threads/trying-extrude-a-2d-polygon-to-create-a-mesh.102629/ </remarks>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(PolygonCollider2D))]
    public class ExtrudeSprite : MonoBehaviour
    {
        public Color extrudeColor = Color.white;
        public float frontDistance = -0.249f;
        public float backDistance = 0.249f;

        void Start()
        {
            PolygonCollider2D pol = GetComponent<PolygonCollider2D>();
            Mesh m = CreateMesh(pol.points, frontDistance, backDistance);

            GetComponent<MeshFilter>().sharedMesh = m;
            GetComponent<MeshRenderer>().material.color = extrudeColor;

            pol.isTrigger = true;
            pol.enabled = false;
        }

        public static Mesh CreateMesh(Vector2[] poly, float frontDistance = -10, float backDistance = 10)
        {
            frontDistance = Mathf.Min(frontDistance, 0);
            backDistance = Mathf.Max(backDistance, 0);

            // convert polygon to triangles
            Triangulator triangulator = new Triangulator(poly);
            int[] tris = triangulator.Triangulate();
            Mesh m = new Mesh();
            Vector3[] vertices = new Vector3[poly.Length * 2];

            for (int i = 0; i < poly.Length; i++)
            {
                vertices[i].x = poly[i].x;
                vertices[i].y = poly[i].y;
                vertices[i].z = frontDistance; // front vertex
                vertices[i + poly.Length].x = poly[i].x;
                vertices[i + poly.Length].y = poly[i].y;
                vertices[i + poly.Length].z = backDistance;  // back vertex    
            }
            int[] triangles = new int[tris.Length * 2 + poly.Length * 6];
            int count_tris = 0;
            for (int i = 0; i < tris.Length; i += 3)
            {
                triangles[i] = tris[i];
                triangles[i + 1] = tris[i + 1];
                triangles[i + 2] = tris[i + 2];
            } // front vertices
            count_tris += tris.Length;
            for (int i = 0; i < tris.Length; i += 3)
            {
                triangles[count_tris + i] = tris[i + 2] + poly.Length;
                triangles[count_tris + i + 1] = tris[i + 1] + poly.Length;
                triangles[count_tris + i + 2] = tris[i] + poly.Length;
            } // back vertices
            count_tris += tris.Length;
            for (int i = 0; i < poly.Length; i++)
            {
                // triangles around the perimeter of the object
                int n = (i + 1) % poly.Length;
                triangles[count_tris] = i;
                triangles[count_tris + 1] = n;
                triangles[count_tris + 2] = i + poly.Length;
                triangles[count_tris + 3] = n;
                triangles[count_tris + 4] = n + poly.Length;
                triangles[count_tris + 5] = i + poly.Length;
                count_tris += 6;
            }
            m.vertices = vertices;
            m.triangles = triangles;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>Source: http://wiki.unity3d.com/index.php?title=Triangulator </remarks>
    public class Triangulator
    {
        private List<Vector2> m_points = new List<Vector2>();

        public Triangulator(Vector2[] points)
        {
            m_points = new List<Vector2>(points);
        }

        public int[] Triangulate()
        {
            List<int> indices = new List<int>();

            int n = m_points.Count;
            if (n < 3)
                return indices.ToArray();

            int[] V = new int[n];
            if (Area() > 0)
            {
                for (int v = 0; v < n; v++)
                    V[v] = v;
            }
            else
            {
                for (int v = 0; v < n; v++)
                    V[v] = (n - 1) - v;
            }

            int nv = n;
            int count = 2 * nv;
            for (int m = 0, v = nv - 1; nv > 2;)
            {
                if ((count--) <= 0)
                    return indices.ToArray();

                int u = v;
                if (nv <= u)
                    u = 0;
                v = u + 1;
                if (nv <= v)
                    v = 0;
                int w = v + 1;
                if (nv <= w)
                    w = 0;

                if (Snip(u, v, w, nv, V))
                {
                    int a, b, c, s, t;
                    a = V[u];
                    b = V[v];
                    c = V[w];
                    indices.Add(a);
                    indices.Add(b);
                    indices.Add(c);
                    m++;
                    for (s = v, t = v + 1; t < nv; s++, t++)
                        V[s] = V[t];
                    nv--;
                    count = 2 * nv;
                }
            }

            indices.Reverse();
            return indices.ToArray();
        }

        private float Area()
        {
            int n = m_points.Count;
            float A = 0.0f;
            for (int p = n - 1, q = 0; q < n; p = q++)
            {
                Vector2 pval = m_points[p];
                Vector2 qval = m_points[q];
                A += pval.x * qval.y - qval.x * pval.y;
            }
            return (A * 0.5f);
        }

        private bool Snip(int u, int v, int w, int n, int[] V)
        {
            int p;
            Vector2 A = m_points[V[u]];
            Vector2 B = m_points[V[v]];
            Vector2 C = m_points[V[w]];
            if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
                return false;
            for (p = 0; p < n; p++)
            {
                if ((p == u) || (p == v) || (p == w))
                    continue;
                Vector2 P = m_points[V[p]];
                if (InsideTriangle(A, B, C, P))
                    return false;
            }
            return true;
        }

        private bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
        {
            float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
            float cCROSSap, bCROSScp, aCROSSbp;

            ax = C.x - B.x; ay = C.y - B.y;
            bx = A.x - C.x; by = A.y - C.y;
            cx = B.x - A.x; cy = B.y - A.y;
            apx = P.x - A.x; apy = P.y - A.y;
            bpx = P.x - B.x; bpy = P.y - B.y;
            cpx = P.x - C.x; cpy = P.y - C.y;

            aCROSSbp = ax * bpy - ay * bpx;
            cCROSSap = cx * apy - cy * apx;
            bCROSScp = bx * cpy - by * cpx;

            return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
        }
    }

}
