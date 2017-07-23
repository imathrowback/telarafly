Shader "Custom/Worldspace_Diffuse_DecalNormal" {
	Properties{
		gp("gp", int) = 0
		doAlphaTest("doAlphaTest", int) = 0

		diffuseTextureXZ("DiffuseTextureXZID", int) = -1
		diffuseTextureY("DiffuseTextureYID", int) = -1
		glow2Texture("glow2TextureID", int) = -1
		normalTextureXZ("normalTextureXZID", int) = -1
		normalTextureY("normalTextureYID", int) = -1
		decalTexture("decalTextureID", int) = -1
		decalNormalTexture("decalNormalTextureID", int) = -1

		offsetY("offsetY", float) = 0
		scale("scale", float) = 0
		scaleY("scaleY", float) = 0
		offsetXYZ("offsetXYZ", Vector) = (0,0,0)

		_decalTexture("Decal", 2D) = "white" {}
		_decalNormalTexture("Decal Normal", 2D) = "white" {}
		_diffuseTextureXZ ("DiffuseTextureXZ", 2D) = "white" {}
		_diffuseTextureY("DiffuseTextureY", 2D) = "white" {}
		_glow2Texture("Glow2Texture", 2D) = "white" {}
		_normalTextureXZ("NormalXZ", 2D) = "white" {}
		_normalTextureY("NormalY", 2D) = "white" {}

	}
	SubShader {
		Tags {"Queue" = "Geometry" "RenderType" = "Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert     

		sampler2D _diffuseTextureXZ, _normalTextureXZ, _diffuseTextureY, _normalTextureY;
	float scale, scaleY;

		struct Input {
			float3 worldNormal; INTERNAL_DATA

			float3 worldPos; 
		};

		void surf (Input IN, inout SurfaceOutput   o) {
			// tri-planar mapping! - https://gamedevelopment.tutsplus.com/articles/use-tri-planar-texture-mapping-for-better-terrain--gamedev-13821
			float3 correctWorldNormal = WorldNormalVector(IN, float3(0, 0, 1));
			float3 coords = IN.worldPos;

			float3 blending = abs(correctWorldNormal);
			blending = normalize(max(blending, 0.00001)); // Force weights to sum to 1.0
			float b = (blending.x + blending.y + blending.z);
			blending /= float3(b, b, b);

			float4 xaxis = tex2D(_diffuseTextureXZ, coords.yz / scale);
			float4 yaxis = tex2D(_diffuseTextureY, coords.xz / scaleY);
			float4 zaxis = tex2D(_diffuseTextureXZ, coords.xy / scale);
			// blend the results of the 3 planar projections.
			float4 tex = xaxis * blending.x + xaxis * blending.y + zaxis;

			float4 nxaxis = tex2D(_normalTextureXZ, coords.yz / scale);
			float4 nyaxis = tex2D(_normalTextureY, coords.xz / scaleY);
			float4 nzaxis = tex2D(_normalTextureXZ, coords.xy / scale);
			float4 ntex = nxaxis * blending.x + nxaxis * blending.y + nzaxis;
			o.Normal = ntex;
			o.Albedo = tex;

		}
		ENDCG
		

		Blend  SrcAlpha One
		CGPROGRAM
		#pragma surface surf Lambert   alpha 


		sampler2D _decalTexture, _decalNormalTexture;
		float scale;

		struct Input {
			float2 uv_decalTexture, uv_decalNormalTexture;
			float3 worldNormal, worldPos;
		};
		void surf(Input IN, inout SurfaceOutput   o) {
			o.Normal = tex2D(_decalNormalTexture, IN.uv_decalNormalTexture);
			fixed4 c = tex2D(_decalTexture, IN.uv_decalTexture);
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
		
	
	

	}
	FallBack "Diffuse"
}
