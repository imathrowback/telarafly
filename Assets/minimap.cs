using Assets;
using Assets.RiftAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class minimap : MonoBehaviour {
    AssetDatabase adb;
    public float x = 5700.0f;
    public float y = 5376.0f;
    int imageSize = 100;

    RawImage[][] images;
    const int mapSize = 3;
    const int offset = 1;

    public Button playerPos;

	// Use this for initialization
	void Start () {
        adb = AssetDatabaseInst.DB;
        images = new RawImage[mapSize][];
        for (int x= 0; x < mapSize; x++)
        {
            images[x] = new RawImage[mapSize];
            for (int y = 0; y < mapSize; y++)
            {
                GameObject go = new GameObject();
                go.transform.parent = this.transform;
                go.name = "RawImage[" + x + "," + y + "]";
                RawImage ri = go.AddComponent<RawImage>();
                images[x][y] = ri;
            }
        }
	}
	
	// Update is called once per frame
	void Update () {
        // world_terrain_5632_5376_1050_mapimage
        int originCX = Mathf.FloorToInt(x / 256) * 256;
        int originCY = Mathf.FloorToInt(y / 256) * 256;
        int x1 = (Mathf.FloorToInt(x / 256) - offset) * 256;
        int y1 = (Mathf.FloorToInt(y / 256) - offset) * 256;

        float ratio = imageSize / 256.0f;
        float distFromX = ((x - originCX) * ratio);
        float distFromY = ((y - originCY) * ratio);
        playerPos.transform.localPosition = new Vector3(distFromX, distFromY, 0);



        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                RawImage ri = images[x][y];

                RectTransform rt = ri.gameObject.GetComponent<RectTransform>();
                rt.localPosition = new Vector3((-offset * imageSize) + (x * imageSize), (-offset * imageSize) + ( y * imageSize));
                rt.pivot = new Vector2(0, 0);

                int rx = x1 + (x*256);
                int ry = y1 + (y*256);

                string texture = string.Format("world_terrain_{0}_{1}_mapimage.dds", rx, ry);
                if (adb.filenameExists(texture))
                {
                    byte[] data = adb.extractUsingFilename(texture);
                    Texture tex = DDSLoader.DatabaseLoaderTexture_DDS.LoadDDS(data);
                    ri.texture = tex;
                }
                else
                {
                    ri.texture = null;
                }
            }
        }



    }
}
