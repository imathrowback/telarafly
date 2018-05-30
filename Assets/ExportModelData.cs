using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExportModelData
{
    public static bool valid = false;
    public static string outputDirectory = @"L:\rift\converted";
    public static string outputDirectoryTextures = @"textures";
    public static string expectedTextureExtension = "jpg";
    public static bool mtlWebPathing = false;
    public static HashSet<int> langIDs = new HashSet<int>();

}
