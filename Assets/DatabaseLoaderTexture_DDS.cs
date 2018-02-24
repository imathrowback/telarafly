using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Reflection;

namespace DDSLoader
{
    public class TextureData
    {
        public byte[] data;
        public int width;
        public int height;
        public TextureFormat format;
        public bool mip;
        public Texture2D getTexture()
        {
            if (data == null)
                throw new Exception("Attempt to use texture data after it was purged");
            Texture2D texture2 = new Texture2D(width, height, format, mip);
            texture2.LoadRawTextureData(data);
            texture2.Apply(true, false);
            return texture2;
        }

        internal Texture getTextureAndPurge()
        {
            Texture tex = getTexture();
            data = null;
            return tex;
        }
    }
    public class DatabaseLoaderTexture_DDS 
    {
        static System.Object lockObj = new System.Object();
        private const uint DDSD_MIPMAPCOUNT_BIT = 0x00020000;
        private const uint DDPF_ALPHAPIXELS = 0x00000001;
        private const uint DDPF_ALPHA = 0x00000002;
        private const uint DDPF_FOURCC = 0x00000004;
        private const uint DDPF_RGB = 0x00000040;
        private const uint DDPF_YUV = 0x00000200;
        private const uint DDPF_LUMINANCE = 0x00020000;
        private const uint DDPF_NORMAL = 0x80000000;

        public static string error;

        public static void LoadDDS(byte[] data, Action<TextureData> textureDataCallback)
        {
            lock (lockObj)
            {
                LoadDDS_1(data, textureDataCallback);
            }
        }
        public static void LoadDDS(byte[] data, out TextureData dataX)
        {
            lock (lockObj)
            {
                TextureData tdata = null;
                Action<TextureData> callback = (x) =>
                {
                    tdata = x;
                };
                LoadDDS_1(data, callback);
                dataX = tdata;
            }
        }
        public static Texture2D LoadDDS(byte[] data)
        {
            lock (lockObj)
            {
                TextureData dataX = null;
                Action<TextureData> callback = (x) =>
               {
                   dataX = x;
               };
                LoadDDS_1(data, callback);
                return dataX.getTexture();
            }
        }
        /*
        public static Texture2D LoadDDS(string path, bool keepReadable = false, bool asNormal = false, int mipmapBias = -1, bool apply = true)
        {
            lock (lockObj)
            {
                if (!File.Exists(path))
                {
                    throw new Exception("File [" + path + "] does not exist");
                }
                return LoadDDS(File.Open(path, FileMode.Open, FileAccess.Read), keepReadable, asNormal, mipmapBias, apply);
            }
        }
        */

        static DataReader reader = new DataReader(null);
        static byte[] textureDataIn = new byte[0];
        static byte[] textureDataOut = new byte[0];
        // DDS Texture loader inspired by
        // http://answers.unity3d.com/questions/555984/can-you-load-dds-textures-during-runtime.html#answer-707772
        // http://msdn.microsoft.com/en-us/library/bb943992.aspx
        // http://msdn.microsoft.com/en-us/library/windows/desktop/bb205578(v=vs.85).aspx
        // mipmapBias limits the number of mipmap when > 0
        private static void LoadDDS_1(byte[] data, Action<TextureData> textureDataCallback)
        {
            long dataBias = 128;

            reader.setData(data);
            //using (BinaryReader reader = new BinaryReader(stream))
            //{
           
                byte[] dwMagic = reader.ReadBytes(4);

                if (!fourCCEquals(dwMagic, "DDS "))
                {
                    throw new Exception("Invalid DDS file");
                }

                int dwSize = (int)reader.ReadUInt32();

                //this header byte should be 124 for DDS image files
                if (dwSize != 124)
                {

                    throw new Exception("Invalid header size");
                }

                int dwFlags = (int)reader.ReadUInt32();
                int dwHeight = (int)reader.ReadUInt32();
                int dwWidth = (int)reader.ReadUInt32();

                int dwPitchOrLinearSize = (int)reader.ReadUInt32();
                int dwDepth = (int)reader.ReadUInt32();
                int dwMipMapCount = (int)reader.ReadUInt32();

                if ((dwFlags & DDSD_MIPMAPCOUNT_BIT) == 0)
                {
                    dwMipMapCount = 1;
                }

                dwMipMapCount = 0;

                // dwReserved1
                for (int i = 0; i < 11; i++)
                {
                    reader.ReadUInt32();
                }

                // DDS_PIXELFORMAT
                uint dds_pxlf_dwSize = reader.ReadUInt32();
                uint dds_pxlf_dwFlags = reader.ReadUInt32();
                byte[] dds_pxlf_dwFourCC = reader.ReadBytes(4);
                string fourCC = Encoding.ASCII.GetString(dds_pxlf_dwFourCC);

                if (fourCCEquals(dds_pxlf_dwFourCC, "DXT3"))
                {
                    long dataLen = data.Length - dataBias;
                    if (textureDataIn.Length < dataLen)
                        textureDataIn = new byte[dataLen];
                    reader.read(dataBias, textureDataIn, dataLen);

                     processDXT3(dwWidth, dwHeight, dwMipMapCount, textureDataCallback);
                    return;
                }
                else
                {
                    uint dds_pxlf_dwRGBBitCount = reader.ReadUInt32();
                    uint pixelSize = dds_pxlf_dwRGBBitCount / 8;
                    uint dds_pxlf_dwRBitMask = reader.ReadUInt32();
                    uint dds_pxlf_dwGBitMask = reader.ReadUInt32();
                    uint dds_pxlf_dwBBitMask = reader.ReadUInt32();
                    uint dds_pxlf_dwABitMask = reader.ReadUInt32();

                    int dwCaps = (int)reader.ReadUInt32();
                    int dwCaps2 = (int)reader.ReadUInt32();
                    int dwCaps3 = (int)reader.ReadUInt32();
                    int dwCaps4 = (int)reader.ReadUInt32();
                    int dwReserved2 = (int)reader.ReadUInt32();

                    TextureFormat textureFormat = TextureFormat.ARGB32;
                    bool isCompressed = false;

                    bool alpha = (dds_pxlf_dwFlags & DDPF_ALPHA) != 0;
                    bool fourcc = (dds_pxlf_dwFlags & DDPF_FOURCC) != 0;
                    bool rgb = (dds_pxlf_dwFlags & DDPF_RGB) != 0;
                    bool alphapixel = (dds_pxlf_dwFlags & DDPF_ALPHAPIXELS) != 0;
                    bool luminance = (dds_pxlf_dwFlags & DDPF_LUMINANCE) != 0;
                    bool rgb888 = dds_pxlf_dwRBitMask == 0x000000ff && dds_pxlf_dwGBitMask == 0x0000ff00 && dds_pxlf_dwBBitMask == 0x00ff0000;
                    bool bgr888 = dds_pxlf_dwRBitMask == 0x00ff0000 && dds_pxlf_dwGBitMask == 0x0000ff00 && dds_pxlf_dwBBitMask == 0x000000ff;
                    bool rgb565 = dds_pxlf_dwRBitMask == 0x0000F800 && dds_pxlf_dwGBitMask == 0x000007E0 && dds_pxlf_dwBBitMask == 0x0000001F;
                    bool argb4444 = dds_pxlf_dwABitMask == 0x0000f000 && dds_pxlf_dwRBitMask == 0x00000f00 && dds_pxlf_dwGBitMask == 0x000000f0 && dds_pxlf_dwBBitMask == 0x0000000f;
                    bool rbga4444 = dds_pxlf_dwABitMask == 0x0000000f && dds_pxlf_dwRBitMask == 0x0000f000 && dds_pxlf_dwGBitMask == 0x000000f0 && dds_pxlf_dwBBitMask == 0x00000f00;
                    bool _DXT3 = false;
                    if (fourcc)
                    {
                        // Texture dos not contain RGB data, check FourCC for format
                        isCompressed = true;

                        if (fourCCEquals(dds_pxlf_dwFourCC, "DXT1"))
                        {
                            textureFormat = TextureFormat.DXT1;
                        }
                        else if (fourCCEquals(dds_pxlf_dwFourCC, "DXT5"))
                        {
                            textureFormat = TextureFormat.DXT5;
                        }
                        else if (fourCCEquals(dds_pxlf_dwFourCC, "DXT3"))
                        {
                            _DXT3 = true;
                        }
                        else
                        {
                            throw new Exception("Unknown fourcc value");
                        }
                    }
                    else if (rgb && (rgb888 || bgr888))
                    {
                        // RGB or RGBA format
                        textureFormat = alphapixel
                            ? TextureFormat.RGBA32
                            : TextureFormat.RGB24;
                    }
                    else if (rgb && rgb565)
                    {
                        // Nvidia texconv B5G6R5_UNORM
                        textureFormat = TextureFormat.RGB565;
                    }
                    else if (rgb && alphapixel && argb4444)
                    {
                        // Nvidia texconv B4G4R4A4_UNORM
                        textureFormat = TextureFormat.ARGB4444;
                    }
                    else if (rgb && alphapixel && rbga4444)
                    {
                        textureFormat = TextureFormat.RGBA4444;
                    }
                    else if (!rgb && alpha != luminance)
                    {
                        // A8 format or Luminance 8
                        textureFormat = TextureFormat.Alpha8;
                    }
                    else
                    {
                        throw new Exception("Only DXT1, DXT5, A8, RGB24, BGR24, RGBA32, BGBR32, RGB565, ARGB4444 and RGBA4444 are supported");
                    }

                    long dataLen = data.Length - dataBias;
                    if (textureDataIn.Length < dataLen)
                        textureDataIn = new byte[dataLen];
                    reader.read(dataBias, textureDataIn, dataLen);

                    if (_DXT3)
                    {
                        Debug.LogError("should never have gotten here");
                         processDXT3(dwWidth, dwHeight, dwMipMapCount, textureDataCallback);
                    }
                    else
                    {
                        byte[] dxtBytes = textureDataIn;
                        // Swap red and blue.
                        if (!isCompressed && bgr888)
                        {
                            for (uint i = 0; i < dxtBytes.Length; i += pixelSize)
                            {
                                byte b = dxtBytes[i + 0];
                                byte r = dxtBytes[i + 2];

                                dxtBytes[i + 0] = r;
                                dxtBytes[i + 2] = b;
                            }
                        }

                         handleNormal(dwWidth, dwHeight, textureFormat, dwMipMapCount, isCompressed, textureDataCallback);
                    }
                }
            
        }
         
        static void processDXT3(int dwWidth, int dwHeight, int dwMipMapCount, Action<TextureData> textureDataCallback)
        {    
            int v = 1 << 1;
            if (textureDataOut.Length < dwWidth * dwHeight * 4)
                textureDataOut = new byte[dwWidth * dwHeight * 4];

            DecompressImageA(textureDataIn, textureDataOut, dwWidth, dwHeight, v);

            TextureData texData = new TextureData();
            texData.width = dwWidth;
            texData.height = dwHeight;
            texData.format = TextureFormat.RGBA32;
            texData.mip = dwMipMapCount > 1;
            texData.data = clone(textureDataOut);
            textureDataCallback.Invoke(texData);

       }

        private static byte[] clone(byte[] data)
        {
            byte[] d = new byte[data.Length];
            Array.Copy(data, d, data.Length);
            return d;
        }

        static void handleNormal(int dwWidth, int dwHeight, TextureFormat textureFormat, int dwMipMapCount, bool isCompressed, Action<TextureData> textureDataCallback)
        {
            TextureData texData = new TextureData();
            texData.width = dwWidth;
            texData.height = dwHeight;
            texData.format = textureFormat;
            texData.mip = dwMipMapCount > 1;
            texData.data = clone(textureDataIn);
            textureDataCallback.Invoke(texData);
        }

        private static bool fourCCEquals(IList<byte> bytes, string s)
        {
            return bytes[0] == s[0] && bytes[1] == s[1] && bytes[2] == s[2] && bytes[3] == s[3];
        }

//#if UNITY_64
        [DllImport(@"squish_x64")]
        static extern void DecompressImage(IntPtr rgba, int width, int height, IntPtr blocks, int flag);
//#elif UNITY_32
//        [DllImport(@"squish_x86")]
//        static extern void DecompressImage(IntPtr rgba, int width, int height, IntPtr blocks, int flag);
//#endif
        public static void DecompressImageA(byte[] blocksIn, byte[] dataOut, int width, int height, int flags)
        {
            int inSize = dataOut.Length;
            GCHandle pinnedData = GCHandle.Alloc(dataOut, GCHandleType.Pinned);
            GCHandle pinnedBlocks = GCHandle.Alloc(blocksIn, GCHandleType.Pinned);

            DecompressImage(pinnedData.AddrOfPinnedObject(), width, height, pinnedBlocks.AddrOfPinnedObject(), (int)flags);

            pinnedBlocks.Free();
            pinnedData.Free();
            int outSize = dataOut.Length;
            if (inSize != outSize)
                Debug.LogError("in size:" + inSize + " != " + outSize);

        }
    }


    class DataReader
    {
        long offset = 0;
        byte[] data;
        public DataReader(byte[] d)
        {
            this.data = d;
        }

        public void setData(byte[] d)
        {
            this.data = d;
            offset = 0;
        }

        public byte[] ReadBytes(int count)
        {
            byte[] d = new byte[count];
            Array.Copy(data, offset, d, 0, count);
            offset += count;
            return d;
        }

        public uint ReadUInt32()
        {
            uint value = (uint)(data[offset++] | (data[offset++]) << 8 | (data[offset++]) << 16 | data[offset++] << 24);
         
            return value;
        }

        internal void read(long offset, byte[] output, long len)
        {
            Array.Copy(data, offset, output, 0, len);
        }
    }
}
