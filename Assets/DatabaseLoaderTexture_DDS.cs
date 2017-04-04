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
    public class DatabaseLoaderTexture_DDS 
    {
        private const uint DDSD_MIPMAPCOUNT_BIT = 0x00020000;
        private const uint DDPF_ALPHAPIXELS = 0x00000001;
        private const uint DDPF_ALPHA = 0x00000002;
        private const uint DDPF_FOURCC = 0x00000004;
        private const uint DDPF_RGB = 0x00000040;
        private const uint DDPF_YUV = 0x00000200;
        private const uint DDPF_LUMINANCE = 0x00020000;
        private const uint DDPF_NORMAL = 0x80000000;

        public static string error;

        public static Texture LoadDDS(byte[] data)
        {
            return LoadDDS(new MemoryStream(data));
        }
        public static Texture2D LoadDDS(string path, bool keepReadable = false, bool asNormal = false, int mipmapBias = -1, bool apply = true)
        {
            if (!File.Exists(path))
            {
                throw new Exception("File [" + path + "] does not exist");
            }
            return LoadDDS(File.Open(path, FileMode.Open, FileAccess.Read), keepReadable, asNormal, mipmapBias, apply);
        }


        // DDS Texture loader inspired by
        // http://answers.unity3d.com/questions/555984/can-you-load-dds-textures-during-runtime.html#answer-707772
        // http://msdn.microsoft.com/en-us/library/bb943992.aspx
        // http://msdn.microsoft.com/en-us/library/windows/desktop/bb205578(v=vs.85).aspx
        // mipmapBias limits the number of mipmap when > 0
        public static Texture2D LoadDDS(Stream stream, bool keepReadable = false, bool asNormal = false, int mipmapBias = -1, bool apply = true)
        {
            //Debug.Log("path:" + path);
            
            using (BinaryReader reader = new BinaryReader(stream))
            {
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
                bool isNormalMap = (dds_pxlf_dwFlags & DDPF_NORMAL) != 0 || asNormal;

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
               // Debug.Log("format:" + textureFormat);
                long dataBias = 128;

                long dxtBytesLength = reader.BaseStream.Length - dataBias;
                reader.BaseStream.Seek(dataBias, SeekOrigin.Begin);
                byte[] dxtBytes = reader.ReadBytes((int)dxtBytesLength);

                if (_DXT3)
                {
                    //Debug.Log("try to unsquish :");
                    dxtBytes = DecompressImageA(dxtBytes, dwWidth, dwHeight, 1 << 1);
                    textureFormat = TextureFormat.RGBA32;
                }


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

                //QualitySettings.masterTextureLimit = 0;
                // Work around for an >Unity< Bug.
                // if QualitySettings.masterTextureLimit != 0 (half or quarter texture rez)
                // and dwWidth and dwHeight divided by 2 (or 4 for quarter rez) are not a multiple of 4 
                // and we are creating a DXT5 or DXT1 texture
                // Then you get an Unity error on the "new Texture"

                int quality = QualitySettings.masterTextureLimit;

                // If the bug conditions are present then switch to full quality
                if (isCompressed && quality > 0 && (dwWidth >> quality) % 4 != 0 && (dwHeight >> quality) % 4 != 0)
                    QualitySettings.masterTextureLimit = 0;

                Texture2D texture = new Texture2D(dwWidth, dwHeight, textureFormat, dwMipMapCount > 1);
                texture.LoadRawTextureData(dxtBytes);
                if (apply)
                    texture.Apply(false, !keepReadable);

                if (QualitySettings.masterTextureLimit != quality)
                    QualitySettings.masterTextureLimit = quality;

                return texture;
            }
        }

        private static bool fourCCEquals(IList<byte> bytes, string s)
        {
            return bytes[0] == s[0] && bytes[1] == s[1] && bytes[2] == s[2] && bytes[3] == s[3];
        }

#if UNITY_64
        [DllImport(@"squish_x64")]
        static extern void DecompressImage(IntPtr rgba, int width, int height, IntPtr blocks, int flag);
#elif UNITY_32
        [DllImport(@"squish_x86")]
        static extern void DecompressImage(IntPtr rgba, int width, int height, IntPtr blocks, int flag);
#endif
        public static byte[] DecompressImageA(byte[] blocks, int width, int height, int flags)
        {
            byte[] decompressedData = new byte[width * height * 4];

            GCHandle pinnedData = GCHandle.Alloc(decompressedData, GCHandleType.Pinned);
            GCHandle pinnedBlocks = GCHandle.Alloc(blocks, GCHandleType.Pinned);

            DecompressImage(pinnedData.AddrOfPinnedObject(), width, height, pinnedBlocks.AddrOfPinnedObject(), (int)flags);

            pinnedBlocks.Free();
            pinnedData.Free();

            return decompressedData;
        }
    }
}
