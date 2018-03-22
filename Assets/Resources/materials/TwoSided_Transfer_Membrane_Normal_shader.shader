// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "Custom/TwoSided_Transfer_Membrane_Normal" {
	Properties{
		doAlphaTest("doAlphaTest", int) = 1
		materialDiffuse("materialDiffuse", Color) = (1,1,1,1)
		materialSpecular("materialSpecular", Color) = (0.7,0.7,0.7,1)

		lightTransferFactor("lightTransferFactor", float) = 1
		_diffuseTexture("diffuseTexture", 2D) = "" {}
		_normalTexture("normalTexture", 2D) = "" {}
		_glossTexture("glossTexture", 2D) = "" {}
		_glowTexture("glowTexture", 2D) = "" {}


	}

		SubShader{
			Tags{ "Queue" = "Geometry" }
		LOD 200
		Cull Front
		CGPROGRAM

		// Physically based Standard lighting model, and enable shadows on all light types
#pragma surface surf StandardSpecular  fullforwardshadows   

		// Use shader model 3.0 target, to get nicer looking lighting
#pragma target 3.0

		sampler2D _diffuseTexture;
	sampler2D _normalTexture;
	sampler2D _glossTexture;
	sampler2D _glowTexture;
	float lightTransferFactor;
	struct Input {
		float2 uv_diffuseTexture;
		float2 uv_normalTexture;
		float2 uv_glossTexture;
		float2 uv_glowTexture;
	};

	// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
	// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
	// #pragma instancing_options assumeuniformscaling
	UNITY_INSTANCING_BUFFER_START(Props)
		// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		// Flip normal for back faces
		void vert(inout appdata_full v) {
		v.normal *= -1;
	}
	void surf(Input IN, inout SurfaceOutputStandardSpecular o) {
		fixed4 diff = tex2D(_diffuseTexture, IN.uv_diffuseTexture);
		fixed4 spec = tex2D(_glossTexture, IN.uv_glossTexture);
		fixed4 normal = tex2D(_normalTexture, IN.uv_normalTexture);
		o.Albedo = diff.rgb + (spec.rgb);
		o.Alpha = 1.0;
		o.Normal = normal;
	}
	ENDCG


		Cull Back
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
#pragma surface surf StandardSpecular  fullforwardshadows   

		// Use shader model 3.0 target, to get nicer looking lighting
#pragma target 3.0

		sampler2D _diffuseTexture;
	sampler2D _normalTexture;
	sampler2D _glossTexture;
	sampler2D _glowTexture;
	float lightTransferFactor;

	struct Input {
		float2 uv_diffuseTexture;
		float2 uv_normalTexture;
		float2 uv_glossTexture;
		float2 uv_glowTexture;
	};


	// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
	// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
	// #pragma instancing_options assumeuniformscaling
	UNITY_INSTANCING_BUFFER_START(Props)
		// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf(Input IN, inout SurfaceOutputStandardSpecular o) {
		fixed4 diff = tex2D(_diffuseTexture, IN.uv_diffuseTexture);
		fixed4 spec = tex2D(_glossTexture, IN.uv_glossTexture);
		fixed4 normal = tex2D(_normalTexture, IN.uv_normalTexture);
		o.Albedo = diff.rgb + (spec.rgb);
		o.Alpha =  1.0;
		o.Normal = normal;
	}
	ENDCG

	
	
	}
		FallBack "Diffuse"
}
