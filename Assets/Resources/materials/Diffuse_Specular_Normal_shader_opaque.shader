Shader "Custom/Diffuse_Specular_Normal_shader_opaque" {
	Properties {
		gp("gp", int) = 0
		doAlphaTest("doAlphaTest", int) = 0
		stippleAlphaVolume("stippleAlphaVolume", int)= 0
		alphaTestRef("alphaTestRef", float) = 0

		diffuseTexture("DiffuseTextureID", int) = -1
		glossTexture("glossTextureID", int) = -1
		normalTexture("normalTextureID", int) = -1
		decalTexture("decalTextureID", int) = -1

		_decalTexture("Decal", 2D) = "white" {}
		_diffuseTexture ("Diffuse", 2D) = "white" {}
		_glossTexture("Specular", 2D) = "white" {}
		_normalTexture("Normal", 2D) = "white" {}

	}
	SubShader {
		Tags {"Queue" = "Geometry" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf StandardSpecular fullforwardshadows 

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _diffuseTexture;
		sampler2D _glossTexture;
		sampler2D _normalTexture;
		sampler2D _decalTexture;
		int decalTexture;
		int normalTexture;

		struct Input {
			float2 uv_diffuseTexture;
			float2 uv_decalTexture;
			float2 uv_glossTexture;
			float2 uv_normalTexture;
		};

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_CBUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_CBUFFER_END

		void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
			fixed4 diffuse = tex2D (_diffuseTexture, IN.uv_diffuseTexture);
			fixed4 gloss = tex2D(_glossTexture, IN.uv_glossTexture);
			
			fixed3 c = diffuse.rgb + gloss.rgb/4;
			if (decalTexture >= 0)
			{
				fixed4 decal = tex2D(_decalTexture, IN.uv_decalTexture);
				c += decal.rgb/2;
			}
			fixed4 normal = tex2D(_normalTexture, IN.uv_normalTexture);
			o.Normal = normal;
			o.Albedo = c;
			o.Alpha = diffuse.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
