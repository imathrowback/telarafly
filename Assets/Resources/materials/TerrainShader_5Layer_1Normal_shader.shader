Shader "Custom/TerrainShader_5Layer" {
    Properties {
        _terrain0("Splat Map (R=T1, G=T2, B=T3, A=T4)", 2D) = "white" {} // Splat map for blending
        _terrain1("Terrain 1 (Albedo)", 2D) = "white" {}
        _terrain2("Terrain 2 (Albedo)", 2D) = "white" {}
        _terrain3("Terrain 3 (Albedo)", 2D) = "white" {}
        _terrain4("Terrain 4 (Albedo)", 2D) = "white" {}
        _terrain5("Terrain 5 (Albedo)", 2D) = "white" {}
        
        // Single normal map option
        _terrain6("Generic Normal Map", 2D) = "bump" {} // Changed default to "bump" for normal maps
        
        _terrain7("Terrain7 (RGB) (Unused)", 2D) = "white" {}
        _terrain8("Terrain8 (RGB) (Unused)", 2D) = "white" {} 


        // Individual normal maps
        _terrain9("Terrain 1 Normal Map", 2D) = "bump" {}
        _terrain10("Terrain 2 Normal Map", 2D) = "bump" {}
        _terrain11("Terrain 3 Normal Map", 2D) = "bump" {}
        _terrain12("Terrain 4 Normal Map", 2D) = "bump" {}
        _terrain13("Terrain 5 Normal Map", 2D) = "bump" {}

        _terrain14("Terrain14 (RGB) (Unused)", 2D) = "white" {} 

        _TilingFactor("Terrain Texture Tiling", Range(1, 100)) = 24.5 // Control for texture tiling



        [Toggle(_USE_INDIVIDUAL_NORMAL_MAPS)] _UseIndividualNormalMaps ("Use Individual Normal Maps", Float) = 1
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        // Shader feature for toggling normal map usage
        #pragma shader_feature _USE_INDIVIDUAL_NORMAL_MAPS

        sampler2D _terrain0;
        sampler2D _terrain1;
        sampler2D _terrain2;
        sampler2D _terrain3;
        sampler2D _terrain4;
        sampler2D _terrain5;
        sampler2D _terrain6; // Generic normal map

        sampler2D _terrain7;
        sampler2D _terrain8;

        sampler2D _terrain9; // Normal map for terrain1
        sampler2D _terrain10; // Normal map for terrain2
        sampler2D _terrain11; // Normal map for terrain3
        sampler2D _terrain12; // Normal map for terrain4
        sampler2D _terrain13; // Normal map for terrain5
        sampler2D _terrain14; 

        float _TilingFactor;

        struct Input {
            float2 uv_terrain0; // UV for the splat map
        };

        void surf (Input IN, inout SurfaceOutputStandard o) {
            // Sample the splat map
            fixed4 splat = tex2D(_terrain0, IN.uv_terrain0);

            // Calculate tiled UVs for terrain textures
            float2 tiledUV = IN.uv_terrain0 * _TilingFactor;

            // Sample albedo textures
            fixed4 col1 = tex2D(_terrain1, tiledUV);
            fixed4 col2 = tex2D(_terrain2, tiledUV);
            fixed4 col3 = tex2D(_terrain3, tiledUV);
            fixed4 col4 = tex2D(_terrain4, tiledUV);
            fixed4 col5 = tex2D(_terrain5, tiledUV);

            // Blend albedo colors using the splat map
            fixed4 finalAlbedo = fixed4(0,0,0,0);
            finalAlbedo = lerp(col1, col2, splat.r); // Blend T1 and T2 based on Red channel
            finalAlbedo = lerp(finalAlbedo, col3, splat.g); // Blend with T3 based on Green channel
            finalAlbedo = lerp(finalAlbedo, col4, splat.b); // Blend with T4 based on Blue channel
            finalAlbedo = lerp(finalAlbedo, col5, splat.a); // Blend with T5 based on Alpha channel

            o.Albedo = finalAlbedo.rgb;
            o.Alpha = finalAlbedo.a; // Use alpha from blended albedo

            // Normal map blending logic
            #ifdef _USE_INDIVIDUAL_NORMAL_MAPS
                // Sample individual normal maps
                fixed3 normal1 = UnpackNormal(tex2D(_terrain9, tiledUV));
                fixed3 normal2 = UnpackNormal(tex2D(_terrain10, tiledUV));
                fixed3 normal3 = UnpackNormal(tex2D(_terrain11, tiledUV));
                fixed3 normal4 = UnpackNormal(tex2D(_terrain12, tiledUV));
                fixed3 normal5 = UnpackNormal(tex2D(_terrain13, tiledUV));

                // Blend normal maps. Normalize after blending.
                fixed3 finalNormal = fixed3(0,0,0);
                finalNormal = lerp(normal1, normal2, splat.r);
                finalNormal = lerp(finalNormal, normal3, splat.g);
                finalNormal = lerp(finalNormal, normal4, splat.b);
                finalNormal = lerp(finalNormal, normal5, splat.a);

                o.Normal = normalize(finalNormal);
            #else
                // Use the generic normal map
                o.Normal = UnpackNormal(tex2D(_terrain6, tiledUV));
            #endif
        }
        ENDCG
    }
    FallBack "Standard" // Changed Fallback to Standard for better compatibility
}