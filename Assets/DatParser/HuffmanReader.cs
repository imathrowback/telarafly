using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.DatParser
{
    public static class ListUtil
    {
        private static Random rng = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
    public class HuffmanReader
    {
        private int[] frequencies;
        private Node[] nodes;
        private Node root;

        public HuffmanReader(byte[] freq)
        {
            int[] frequencies = new int[256];

            using (BinaryReader reader = new BinaryReader(new MemoryStream(freq)))
            {
                for (int i = 0; i < frequencies.Length; i++)
                    frequencies[i] = reader.readInt();
            }

            this.frequencies = frequencies;
            root = buildTree2(this.frequencies);
        }
        /**
         * Initialize a new huffman reader with the given frequencies array.
         */
        public HuffmanReader(int[] frequencies)
        {
            this.frequencies = frequencies;

            root = buildTree2(this.frequencies);
        }

        public byte[] read(byte[] bb, int srcLimit, int dstLimit)
        {
            return read(new BinaryReader(new MemoryStream(bb)), srcLimit, dstLimit);
        }
        /**
         * Reads bytes from the given ByteBuffer, up to srcLimit compressed bytes or dstLimit uncompressed bytes.
         */
        public byte[] read(BinaryReader bb, int srcLimit, int dstLimit)
        {
            Node current = root;

            //MemoryStream outr = new MemoryStream(dstLimit);
            byte[] output = new byte[dstLimit];
            int outIndex = 0;

            int count = 0;
            while (count++ < srcLimit)
            {
                int b = bb.ReadByte();
                int o = 0x80;

                do
                {

                    if ((b & o) != 0)
                    {
                        current = current.b;
                    }
                    else
                    {
                        current = current.a;
                    }

                    o = (int)((uint)o >> 1);

                    if (current.a == null && current.b == null)
                    {
                        output[outIndex++] = ((byte)current.value);

                        if (outIndex >= dstLimit)
                        {
                            return output;
                        }

                        current = root;
                    }
                } while (o != 0);
            }

            return output;
        }



        /**
         * Builds an huffman tree with the given frequencies array.
         */
        private Node buildTree(int[] freqs)
        {
            Node[] heap = new Node[freqs.Length];
            for (int i = 0; i < freqs.Length; ++i)
            {
                heap[i] = new Node();
                heap[i].value = i;
                heap[i].freq = freqs[i];
            }

            Node[] heap2 = new Node[heap.Length];
            Array.Copy(heap, 0, heap2, 0, heap.Length);

            // heapSort3(heap2);
            heapSort(heap);

            List<Node> leafs0 = new List<Node>(heap);
            leafs0.Reverse();

            List<Node> leafs = leafs0;

            int v = -1;
            int first = -1;
            for (int i = 0; i < leafs.Count() - 1; ++i)
            {
                if (leafs[i].freq == v)
                {

                }
                else
                {
                    if (first != -1 && i - first > 1)
                    {
                        //("Conflicts for " + v + " from " + first + " to " + (i - 1));
                        List<Node> range = leafs.GetRange(first, i - 1);
                        range.Shuffle();
                        for (int xx = 0; xx < range.Count; xx++)
                            leafs[first + xx] = range[xx];
                    }
                    first = i;
                    v = leafs[i].freq;
                }
            }

            //System.out.println("Leaf0 : " + leafs0);
            //System.out.println("Leaf1 : " + new ArrayList<Node>(Arrays.asList(heap2)));

            List<Node> nodes = new List<Node>();

            Node[] children = new Node[2];

            while (leafs.Count() + nodes.Count() > 1)
            {
                //System.out.println("Leafs: " + leafs);
                //System.out.println("Nodes: " + nodes);

                for (int i = 0; i < children.Length; ++i)
                {
                    if (leafs.Count() == 0)
                    {
                        children[i] = nodes.Take(1).First();
                    }
                    else if (nodes.Count() == 0)
                    {
                        children[i] = leafs.Take(1).First();
                    }
                    else
                    {
                        if (nodes.First().CompareTo(leafs.First()) < 0)
                        {
                            children[i] = nodes.Take(1).First();
                        }
                        else
                        {
                            children[i] = leafs.Take(1).First();
                        }
                    }
                }

                Node n = new Node();
                n.freq = children[0].freq + children[1].freq;
                n.value = -1;
                n.a = children[0];
                n.a.parent = n;
                n.b = children[1];
                n.b.parent = n;

                n.depth = Math.Min(n.a.depth, n.b.depth) + 1;

                int p = 0;
                while (p < nodes.Count() && nodes[p].freq <= n.freq)
                {
                    p++;
                }
                nodes.Insert(p, n);
            }

            return nodes.Take(1).First();
        }

        public static void heapSort(IComparable[] heap)
        {
            for (int i = (heap.Length >> 1); i >= 1; --i)
            {
                IComparable node = heap[i - 1];

                heapify(heap, i, node, heap.Length);
            }

            for (int i = heap.Length; i > 1; --i)
            {
                IComparable node = heap[i - 1];
                heap[i - 1] = heap[0];
                heap[0] = node;

                heapify(heap, 1, node, i - 1);
            }
        }

        private static void heapify(IComparable[] heap, int insert, IComparable node, int limit)
        {
            for (int j = insert << 1; ; j <<= 1)
            {
                if (j > limit)
                    break;

                if (j < limit)
                {
                    if (heap[j - 1].CompareTo(heap[j]) > 0)
                    {
                        j++;
                    }
                }
                if (node.CompareTo(heap[j - 1]) <= 0)
                    break;

                heap[insert - 1] = heap[j - 1];
                insert = j;
            }

            heap[insert - 1] = node;
        }

        public static Node buildTree2(int[] frequencies)
        {

            // The heap is twice the size of the array as we will first store pairs inside
            int[] heap = new int[2 * frequencies.Length];
            for (int i = 0; i < frequencies.Length; ++i)
            {
                heap[i] = i;
                heap[i + (heap.Length >> 1)] = frequencies[i];
            }



            for (int i = (heap.Length >> 2); i > 0; --i)
            {
                int value = heap[i - 1];
                int weight = heap[i - 1 + (heap.Length >> 1)];

                heapify2(heap, i, weight, value, (heap.Length >> 1));
            }

            int prevFreq = 0;



            int b = 0;
            int limit = heap.Length >> 1;
            while (true)
            {
                b++;
                if ((b & 1) == 0)
                {
                    int value = -limit;
                    int weight = prevFreq + heap[0 + (heap.Length >> 1)];

                    heap[limit + (heap.Length >> 1)] = heap[0];

                    heapify2(heap, 1, weight, value, limit);
                }
                else
                {
                    limit--;

                    int value = heap[limit];
                    int weight = heap[limit + (heap.Length >> 1)];

                    // Copy the first value of the heap as a node of the inline tree
                    heap[limit] = heap[0];

                    if (limit == 1)
                    {
                        heap[1 + (heap.Length >> 1)] = value;
                        break;
                    }

                    prevFreq = heap[0 + (heap.Length >> 1)];

                    heapify2(heap, 1, weight, value, limit);
                }
            }

            return maketable(heap);
        }

        private static Node maketable(int[] values)
        {
            int nextDepth;
            int result;
            int maxDepth = 9;
            int v9;
            int v10;
            int[] v12 = new int[32];
            int[] a3 = new int[512];
            int[] codeValues = new int[512];
            int[] codeLength = new int[512];
            int depth = 0;
            int v6 = 0;
            v12[0] = 1;
            do
            {
                result = values[(v6 & 1) * (values.Length >> 1) + v12[depth]];
                if (result < 0)
                {
                    nextDepth = depth + 1;
                    do
                    {
                        if (nextDepth == maxDepth)
                            a3[v6] = result;
                        v6 *= 2;
                        ++depth;
                        v12[depth] = -result;
                        result = values[-result];
                        ++nextDepth;
                    } while (result < 0);
                }
                codeValues[result] = v6;
                codeLength[result] = depth + 1;
                if (depth + 1 <= maxDepth)
                {
                    if (depth + 1 == maxDepth)
                    {
                        a3[v6] = result;
                    }
                    else
                    {
                        v9 = maxDepth - (depth + 1);
                        v10 = 1 << v9;
                        if (1 << v9 != 0)
                        {
                            int v11 = v10 + (v6 << v9);
                            do
                            {
                                --v10;
                                --v11;
                                v11 = result;
                                a3[v11] = result;
                            } while (v10 != 0);
                        }
                    }
                }
                do
                {
                    if ((v6 & 1) == 0)
                        break;
                    v6 >>= 1;
                    --depth;
                } while (depth >= 0);
                v6 |= 1;
            } while (depth >= 0);

            //		  for (int i = 0; i < codeValues.length; ++i) {
            //			  if (codeLength[i] != 0) {
            //				  String s = "0000000000" + Integer.toBinaryString(codeValues[i]);
            //				  System.out.println(i + " => " + s.substring(s.length() - codeLength[i]));
            //			  }
            //		  }

            Node root = new Node();

            for (int i = 0; i < codeValues.Length; ++i)
            {
                if (codeLength[i] != 0)
                {
                    int mask = 1 << (codeLength[i] - 1);

                    Node current = root;
                    while (mask != 0)
                    {
                        bool b = (codeValues[i] & mask) != 0;

                        if (b)
                        {
                            if (current.b == null)
                            {
                                current.b = new Node();
                            }
                            current = current.b;
                        }
                        else
                        {
                            if (current.a == null)
                            {
                                current.a = new Node();
                            }
                            current = current.a;
                        }

                        mask = (int)((uint)mask >> 1);
                    }

                    current.value = i;

                    //				  String s = "0000000000" + Integer.toBinaryString(codeValues[i]);
                    //				  System.out.println(i + " => " + s.substring(s.length() - codeLength[i]));
                }
            }

            return root;
        }

        private static void heapify2(int[] heap, int insert, int weight, int value, int limit)
        {
            for (int j = insert << 1; ; j <<= 1)
            {
                if (j > limit)
                    break;

                if (j < limit)
                {
                    if (heap[j - 1 + (heap.Length >> 1)] > heap[j + (heap.Length >> 1)])
                    {
                        j++;
                    }
                }
                if (weight <= heap[j - 1 + (heap.Length >> 1)])
                    break;

                heap[insert - 1] = heap[j - 1];
                heap[insert - 1 + (heap.Length >> 1)] = heap[j - 1 + (heap.Length >> 1)];
                insert = j;
            }

            heap[insert - 1] = value;
            heap[insert - 1 + (heap.Length >> 1)] = weight;
        }


    }

    /**
     * An huffman tree node, be it a node or leaf
     *
     */
    public class Node : IComparable
    {

        public int freq;
        public int value;
        public int depth;

        public Node parent;
        public Node a, b;

        public bool isNode()
        {
            return a != null;
        }

        public bool isLeaf()
        {
            return a == null;
        }


        public int CompareTo(Object on)
        {
            Node o = (Node)on;
            int d = freq - o.freq;

            if (d == 0)
            {
                // Nodes always after
                if (isNode() && o.isLeaf())
                {
                    return +1;
                }
                else if (isLeaf() && o.isNode())
                {
                    return -1;
                }
            }

            return d;
        }




    }

}
