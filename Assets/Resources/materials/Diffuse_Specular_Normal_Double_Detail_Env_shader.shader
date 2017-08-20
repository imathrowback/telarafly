Shader "Custom/Diffuse_Specular_Normal_Double_Detail_Env_shader" {
	Properties{
		doAlphaTest("doAlphaTest", int) = 0

		_diffuseTexture("Diffuse", 2D) = "white" {}
		_diffuseTexture2("Diffuse2", 2D) = "white" {}
		_glowTexture2("Glow texture", 2D) = "white" {}

		_normalTexture("Normal", 2D) = "white" {}
		_normalTexture2("Normal2", 2D) = "white" {}
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
#pragma surface surf StandardSpecular fullforwardshadows


		sampler2D _diffuseTexture;
		sampler2D _normalTexture;

		struct Input {
			float2 uv_diffuseTexture;
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
			fixed4 normal = tex2D(_normalTexture, IN.uv_normalTexture);

			o.Normal = normal;
			o.Specular = (0.2, 0.2, 0.2);
			o.Albedo = diffuse.rgb;
			o.Emission = half3(1, 0, 0);
			o.Alpha = diffuse.a;
		}
		ENDCG
			
		//Blend OneMinusDstColor One
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf StandardSpecular fullforwardshadows

			// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0
		sampler2D _diffuseTexture2;
		sampler2D _normalTexture2;

		struct Input {
			float2 uv_diffuseTexture2;
			float2 uv_normalTexture2;
		};

		
		void surf(Input IN, inout SurfaceOutputStandardSpecular o) {
			fixed4 diffuse2 = tex2D(_diffuseTexture2, IN.uv_diffuseTexture2);
			fixed4 normal2 = tex2D(_normalTexture2, IN.uv_normalTexture2);

			o.Normal = normal2;
			o.Albedo = diffuse2.rgb;
			o.Alpha = diffuse2.a;
		}
		ENDCG
			/*
			//Blend OneMinusDstColor One
			CGPROGRAM
			// Physically based Standard lighting model, and enable shadows on all light types
#pragma surface surf StandardSpecular fullforwardshadows

			// Use shader model 3.0 target, to get nicer looking lighting
#pragma target 3.0
			sampler2D _glowTexture;

		struct Input {
			float2 uv_glowTexture;
		};


		void surf(Input IN, inout SurfaceOutputStandardSpecular o) {
			fixed4 c = tex2D(_glowTexture, IN.uv_glowTexture);

			o.Albedo = c.rgb / 10;
		}
		ENDCG
		*/
	}
	FallBack "Diffuse"
}
