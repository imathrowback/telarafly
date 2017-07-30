Shader "Custom/Diffuse_Specular_Normal_DeepGlow_shader" {
	Properties{
		gp("gp", int) = 0
		doAlphaTest("doAlphaTest", int) = 0
		stippleAlphaVolume("stippleAlphaVolume", int) = 0
		alphaTestRef("alphaTestRef", float) = 1

		diffuseTexture("DiffuseTextureID", int) = -1
		glowTexture("GlowTextureID", int) = -1
		glossTexture("glossTextureID", int) = -1
		normalTexture("normalTextureID", int) = -1
		decalTexture("decalTextureID", int) = -1

		GlowDeep("GlowDeep", float) = 0
		GlowDeepMin("GlowDeepMin", float) = 0

		_glowTexture("Glow", 2D) = "white" {}
		_decalTexture("Decal", 2D) = "white" {}
		_diffuseTexture ("Diffuse", 2D) = "white" {}
		_glossTexture("Specular", 2D) = "white" {}
		_normalTexture("Normal", 2D) = "white" {}

	}
	SubShader {
		Tags { "RenderType" = "Transparent" "Queue" = "AlphaTest" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf StandardSpecular fullforwardshadows alphatest:alphaTestRef addshadow 

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _diffuseTexture, _glossTexture,_normalTexture,_decalTexture,_glowTexture;
		int decalTexture;
		int normalTexture;
		int glowTexture;
		float GlowDeep;

		struct Input {
			float2 uv_diffuseTexture, uv_decalTexture, uv_glossTexture, uv_normalTexture, uv_glowTexture ;
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
			if (glowTexture >= 0)
			{
				fixed4 glow = tex2D(_glowTexture, IN.uv_glowTexture) * GlowDeep;
				c = (c ) + glow;
			}
			fixed4 normal = tex2D(_normalTexture, IN.uv_normalTexture);
			o.Normal = normal;
			o.Albedo = c;
			o.Alpha = diffuse.a;
		}
		ENDCG
			
			/*
		Blend SrcAlpha one
		
		CGPROGRAM
		#pragma surface surf StandardSpecular   alphatest:alphaTestRef

		sampler2D _glowTexture;
		float GlowDeep, GlowDeepMin;
		struct Input {
			float2 uv_glowTexture;
		};

		void surf(Input IN, inout SurfaceOutputStandardSpecular o) {
			fixed4 glow = tex2D(_glowTexture, IN.uv_glowTexture) *GlowDeep;
			o.Albedo = glow * 2;
			o.Emission = glow * 2;
			//o.Alpha = 0;
		}
		ENDCG*/
		
	}
	FallBack "Diffuse"
}
