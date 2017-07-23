Shader "Custom/EC_PowerPlant_shader" {
	Properties {
		doAlphaTest("doAlphaTest", int) = 0
		stippleAlphaVolume("stippleAlphaVolume", int) = 0
		alphaTestRef("alphaTestRef", float) = 0
		Metalness("metalness", float) = 0
		diffuseTexture("DiffuseTextureID", int) = 0
		glossTexture("glossTextureID", int) = 0
		normalTexture("normalTextureID", int) = 0

		_diffuseTexture("Diffuse", 2D) = "white" {}
		_glossTexture("Specular", 2D) = "white" {}
		_normalTexture("Normal", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf StandardSpecular fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _diffuseTexture;
		sampler2D _glossTexture;
		sampler2D _normalTexture;
		float Metalness;

		struct Input {
			float2 uv_diffuseTexture;
			float2 uv_glossTexture;
			float2 uv_normalTexture;
		};

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_CBUFFER_START(Props)
			// put more per-instance properties here
			UNITY_INSTANCING_CBUFFER_END

			void surf(Input IN, inout SurfaceOutputStandardSpecular o) {
			fixed4 diffuse = tex2D(_diffuseTexture, IN.uv_diffuseTexture);
			fixed4 gloss = tex2D(_glossTexture, IN.uv_glossTexture);
			fixed4 normal = tex2D(_normalTexture, IN.uv_normalTexture);

			o.Normal = normal;
			o.Albedo = diffuse.rgb + (gloss.rgb * Metalness);
			o.Alpha = diffuse.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
