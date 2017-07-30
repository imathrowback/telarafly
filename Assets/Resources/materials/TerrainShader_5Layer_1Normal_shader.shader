Shader "Custom/TerrainShader_5Layer_1Normal" {
	Properties {
		_terrain0("Terrain0 (RGB)", 2D) = "white" {}
		_terrain1("Terrain1 (RGB)", 2D) = "white" {}
		_terrain2("Terrain2 (RGB)", 2D) = "white" {}
		_terrain3("Terrain3 (RGB)", 2D) = "white" {}
		_terrain4("Terrain4 (RGB)", 2D) = "white" {}
		_terrain5("Terrain5 (RGB)", 2D) = "white" {}
		_terrain6("Terrain6 (RGB)", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _terrain0;
		sampler2D _terrain1;
		sampler2D _terrain2;
		sampler2D _terrain3;
		sampler2D _terrain4;
		sampler2D _terrain5;
		sampler2D _terrain6;

		struct Input {
			float2 uv_terrain0;
		};


		void surf (Input IN, inout SurfaceOutputStandard o) {
			fixed4 col0 = tex2D(_terrain0, IN.uv_terrain0) ;
			fixed4 col1 = tex2D(_terrain1, IN.uv_terrain0 * 20) ;
			fixed4 col2 = tex2D(_terrain2, IN.uv_terrain0 * 20) ;
			fixed4 col3 = tex2D(_terrain3, IN.uv_terrain0 * 20) ;
			fixed4 col4 = tex2D(_terrain4, IN.uv_terrain0 * 20);
			fixed4 col5 = tex2D(_terrain5, IN.uv_terrain0 * 20) ;

			half4 t = half4(0,0,0,0);

			t = lerp(col1, col2, col0.r);
			t = lerp(t, col3, col0.g );
			t = lerp(t, col4, col0.b);
			t = lerp(t, col5, col0.a);

			o.Normal = tex2D(_terrain6, IN.uv_terrain0);
			o.Albedo = t;
			o.Alpha = col1.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
