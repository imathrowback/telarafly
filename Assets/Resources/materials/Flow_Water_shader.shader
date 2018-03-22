Shader "Custom/Flow_Water_shader" {
	Properties {

		doAlphaTest("doAlphaTest", int) = 0

		_diffuseTexture("Diffuse", 2D) = "white" {}
		_normalTexture("Normal", 2D) = "white" {}
	
		_environmentMap("Environment map", 2D) = "white" {}
		_flowTexture("Flow texture", 2D) = "white" {}

	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard vertex:vert alpha:fade nolightmap
		#pragma target 3.0
		sampler2D _diffuseTexture, _normalTexture;

		struct Input {
			float2 uv_diffuseTexture;
			float2 uv_normalTexture;
			float4 screenPos;
			float eyeDepth;
		};

		void vert(inout appdata_full v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);
			COMPUTE_EYEDEPTH(o.eyeDepth);
		}

		void surf (Input IN, inout SurfaceOutputStandard o) {
			fixed4 c = tex2D (_diffuseTexture, IN.uv_diffuseTexture);
			fixed4 n = tex2D(_normalTexture, IN.uv_normalTexture);
			o.Albedo = c.rgb;
			o.Alpha = 0.5;
			//o.Normal = n;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
