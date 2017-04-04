Shader "WaterShader" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" }
		CGPROGRAM
#pragma surface surf Lambert alpha
		struct Input {
		float2 uv_MainTex;
	};
	sampler2D _MainTex;
	void surf(Input IN, inout SurfaceOutput   o) {
		fixed4 t = tex2D(_MainTex, IN.uv_MainTex);
		o.Albedo = t.rgb;
		 o.Alpha = 0.5;
	}
	ENDCG
	}
		Fallback "Diffuse"
}