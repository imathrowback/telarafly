Shader "BasicTerrainShader" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }
		CGPROGRAM
#pragma surface surf Lambert 
		struct Input {
		float2 uv_MainTex;
	};
	sampler2D _MainTex;
	void surf(Input IN, inout SurfaceOutput   o) {
		fixed4 t = tex2D(_MainTex, IN.uv_MainTex);
		o.Albedo = float3(0.5, 0.5, 0.5);
		// o.Alpha = t.a;
	}
	ENDCG
	}
		Fallback "Diffuse"
}