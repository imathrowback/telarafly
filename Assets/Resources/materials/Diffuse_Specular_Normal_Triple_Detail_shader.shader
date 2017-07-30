Shader "Custom/Diffuse_Specular_Normal_Triple_Detail_shader" {
	Properties {
		gp("gp", int) = 0
		doAlphaTest("doAlphaTest", int) = 0
		stippleAlphaVolume("stippleAlphaVolume", int)= 0
		alphaTestRef("alphaTestRef", float) = 0

		diffuseTexture("DiffuseTextureID", int) = -1
		diffuseTexture2("DiffuseTexture2ID", int) = -1
		diffuseTexture3("DiffuseTexture3ID", int) = -1
		
		normalTexture("normalTextureID", int) = -1
		normalTexture2("normalTexture2ID", int) = -1
		normalTexture3("normalTexture3ID", int) = -1

		decalTexture("decalTextureID", int) = -1
		decalTexture("decalTexture2ID", int) = -1
		decalTexture("decalTexture3ID", int) = -1

		_decalTexture("Decal", 2D) = "white" {}
		_decalTexture2("Decal2", 2D) = "white" {}
		_decalTexture3("Decal3", 2D) = "white" {}
		
		_diffuseTexture ("Diffuse", 2D) = "white" {}
		_diffuseTexture2("Diffuse2", 2D) = "white" {}
		_diffuseTexture3("Diffuse3", 2D) = "white" {}
		
		_normalTexture("Normal", 2D) = "white" {}
		_normalTexture2("Normal2", 2D) = "white" {}
		_normalTexture3("Normal3", 2D) = "white" {}

	}
	SubShader {
		Tags { "RenderType" = "Transparent" "Queue" = "AlphaTest" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf StandardSpecular fullforwardshadows alphatest:alphaTestRef addshadow 
		#pragma target 3.0

		sampler2D _diffuseTexture,_diffuseTexture2, _diffuseTexture3, _normalTexture, _normalTexture2, _normalTexture3, _decalTexture, _decalTexture2, _decalTexture3;

		struct Input {
			float2 uv_diffuseTexture, uv_diffuseTexture2, uv_diffuseTexture3;
			float2 uv_decalTexture, uv_decalTexture2, uv_decalTexture3;
			float2 uv_normalTexture, uv_normalTexture2, uv_normalTexture3;
		};

		void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
			
			fixed4 diffuse1 = tex2D(_diffuseTexture, IN.uv_diffuseTexture);
			fixed4 diffuse2 = tex2D(_diffuseTexture2, IN.uv_diffuseTexture2);
			fixed4 diffuse3 = tex2D(_diffuseTexture3, IN.uv_diffuseTexture3);
			fixed4 c = lerp(diffuse1, diffuse2, 0.5);
			c = lerp(c, diffuse3, 0.5);
			/*
			fixed3 c = diffuse.rgb + gloss.rgb/4;
			if (decalTexture >= 0)
			{
				fixed4 decal = tex2D(_decalTexture, IN.uv_decalTexture);
				c += decal.rgb/2;
			}
			*/
			fixed4 normal1 = tex2D(_normalTexture, IN.uv_normalTexture);
			fixed4 normal2 = tex2D(_normalTexture2, IN.uv_normalTexture2);
			fixed4 normal3 = tex2D(_normalTexture3, IN.uv_normalTexture3);
			o.Normal = lerp(lerp(normal1, normal2, 0.5), normal3, 0.5);
			o.Albedo = c;
			o.Alpha = 1.0f;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
