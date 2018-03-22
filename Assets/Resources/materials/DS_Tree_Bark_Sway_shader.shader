// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "Custom/DS_Tree_Bark_Sway_shader" {
	Properties {

		GlowStrength("GlowStrength", range(0, 5)) = 1
		WindStrength("WindStrength", range(0, 1)) = 0.01
		WindFreq("WindFreq", range(0, 15)) = 2
		WindAngle("WindAngle", range(0, 15)) = 8
		WindPhase("WindPhase", range(0, 15)) = 2
		DecalNormalBlender("DecalNormalBlender", range(0, 1)) = 1.0
		doAlphaTest("doAlphaTest", int) = 1
		materialDiffuse("materialDiffuse", Color) = (1,1,1,1)
		materialSpecular("materialSpecular", Color) = (0.7,0.7,0.7,1)
		SwayPivot("SwayPivot", Vector) = (0,0,0)
		tex0ScrollRate("tex0ScrollRate", Vector) = (0.01, 0.01, 0, 0)
		tex1ScrollRate("tex1ScrollRate", Vector) = (0.02, 0.01, 0, 0)
		alphaScroll("alphaScroll", Vector) = (0.03, 0.02, 0, 0)
		
		diffuseTexture("diffuseTextureIndex", int) = 0
		decalTexture("decalTextureIndex", int) = 0

		_diffuseTexture("diffuseTexture", 2D) = "" {}
		_glossTexture("glossTexture", 2D) = "" {}
		_decalSpecularTexture("decalSpecularTexture", 2D) = "" {}
		_decalNormalTexture("decalNormalTexture", 2D) = "" {}
		_decalTexture("decalTexture", 2D) = "" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf StandardSpecular  fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _diffuseTexture;
		sampler2D _decalTexture;
		sampler2D _glossTexture;
		sampler2D _decalNormalTexture;
		sampler2D _decalSpecularTexture;
		float2 tex0ScrollRate;
		float2 tex1ScrollRate;
		int diffuseTexture;
		int decalTexture;

		struct Input {
			float2 uv_diffuseTexture;
			float2 uv_decalTexture;
			float2 uv_glossTexture;
			float2 uv_decalNormalTexture;
			float2 uv_decalSpecularTexture;
		};

		
		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandardSpecular o) {

			fixed2 diffScrollRate = fixed2(0, 0);
			fixed2 decalScrollRate = fixed2(0, 0);
			if (diffuseTexture == 0)
				diffScrollRate = tex0ScrollRate;
			else if (diffuseTexture == 1)
				diffScrollRate = tex1ScrollRate;
			if (decalTexture == 0)
				decalScrollRate = tex0ScrollRate;
			else if (decalTexture == 1)
				decalScrollRate = tex1ScrollRate;


			fixed4 diff = tex2D(_diffuseTexture, IN.uv_diffuseTexture + diffScrollRate * _Time.x);
			fixed4 decal = tex2D(_decalTexture, IN.uv_decalTexture + decalScrollRate * _Time.x);
			fixed4 spec = tex2D(_glossTexture, IN.uv_glossTexture );
			fixed4 decalNormal = tex2D(_decalNormalTexture, IN.uv_decalNormalTexture);
			fixed4 decalSpec = tex2D(_decalSpecularTexture, IN.uv_decalSpecularTexture );
			o.Albedo = diff.rgb * (decal.rgb + decalSpec.rgb) + (spec.rgb);
			o.Normal = decalNormal;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
