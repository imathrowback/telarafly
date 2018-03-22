// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "Custom/EC_DreamJuice_shader" {
		Properties{

			tex0ScrollRate("tex0ScrollRate", Vector) = (0.01, 0.01, 0, 0)

			diffuseTexture("diffuseTextureIndex", int) = 0
			flowTexture("flowTextureIndex", int) = 0

			LavaColor("LavaColor", Color) = (1,1,1,1)
			_diffuseTexture("diffuseTexture", 2D) = "" {}
			_flowTexture("flowTexture", 2D) = "" {}
			_glowTexture("glowTexture", 2D) = "" {}
			_normalTexture("normalTexture", 2D) = "" {}
		}
			SubShader{
			Tags{ "Queue" = "Transparent" }
			LOD 200

			CGPROGRAM
			// Physically based Standard lighting model, and enable shadows on all light types
#pragma surface surf StandardSpecular  fullforwardshadows

			// Use shader model 3.0 target, to get nicer looking lighting
#pragma target 3.0

		sampler2D _diffuseTexture;
		sampler2D _flowTexture;
		sampler2D _glowTexture;
		sampler2D _normalTexture;

		float2 tex0ScrollRate;
		float4 LavaColor;

		int diffuseTexture;
		int flowTexture;

		struct Input {
			float2 uv_diffuseTexture;
			float2 uv_flowTexture;
			float2 uv_glowTexture;
			float2 uv_normalTexture;
		};


		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
		// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf(Input IN, inout SurfaceOutputStandardSpecular o) {
			fixed2 diffScrollRate = fixed2(0, 0);
			if (diffuseTexture == 0)
				diffScrollRate = tex0ScrollRate;

			fixed4 diff = tex2D(_diffuseTexture, IN.uv_diffuseTexture + diffScrollRate * _Time.x);
			fixed4 flow = tex2D(_flowTexture, IN.uv_flowTexture);
			//fixed4 spec = tex2D(_glossTexture, IN.uv_glossTexture);
			//fixed4 decalNormal = tex2D(_decalNormalTexture, IN.uv_decalNormalTexture);
			//fixed4 decalSpec = tex2D(_decalSpecularTexture, IN.uv_decalSpecularTexture);
			o.Albedo = diff.rgb + LavaColor * flow.rgb;
			o.Normal = tex2D(_normalTexture, IN.uv_normalTexture);
		}
		ENDCG
	}
		FallBack "Diffuse"
}
