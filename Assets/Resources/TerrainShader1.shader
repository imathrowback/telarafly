Shader "Custom/TerrainShader1" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_terrain0("Terrain0 (RGB)", 2D) = "white" {}
		_terrain1("Terrain1 (RGB)", 2D) = "white" {}
		_terrain2("Terrain2 (RGB)", 2D) = "white" {}
		_terrain3("Terrain3 (RGB)", 2D) = "white" {}
		_terrain4("Terrain4 (RGB)", 2D) = "white" {}
		_terrain5("Terrain5 (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _terrain0;
		sampler2D _terrain1;
		sampler2D _terrain2;
		sampler2D _terrain3;
		sampler2D _terrain4;
		sampler2D _terrain5;

		struct Input {
			float2 uv_MainTex;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 col0 = tex2D(_terrain0, IN.uv_MainTex) * _Color;
			fixed4 col1 = tex2D(_terrain1, IN.uv_MainTex*20) * _Color;
			fixed4 col2 = tex2D(_terrain2, IN.uv_MainTex*20) * _Color;
			fixed4 col3 = tex2D(_terrain3, IN.uv_MainTex * 20) * _Color;
			fixed4 col4 = tex2D(_terrain4, IN.uv_MainTex * 20) * _Color;
			fixed4 col5 = tex2D(_terrain5, IN.uv_MainTex * 20) * _Color;

			fixed4 c10 = fixed4(-2, 3, -0.5, 0);
			fixed4 c11 = fixed4(1, 0.300000012, -0.00105730689, 9.55352688);
			fixed4 c12 = fixed4(0.75, 0.600000024, 0.25, 1);
			fixed4 c13 = fixed4(2, -1, 1, 0.00392156886);
			fixed4 c14 = fixed4(0.0500000007, 0, 0, 0);
			fixed4 c15 = fixed4(0, 1, 0.800000012, 0.100000001);

			half4 t = half4(0,0,0,0);

			t = lerp(col1, col2, col0.r);
			t = lerp(t, col3, col0.g );
			t = lerp(t, col4, col0.b);
			t = lerp(t, col5, col0.a);

			o.Albedo = t;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = col1.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
