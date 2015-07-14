Shader "Custom/VertexLit Colored" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	}

	SubShader {
		Tags { 
			"RenderType" = "Transparent"
		}
		CGPROGRAM

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		#pragma surface surf Standard fullforwardshadows
		struct Input {
			float4 color : color;
			float2 uv_mainTex;
		};
		sampler2D _MainTex;
		fixed4 _Color;
		
		void surf(Input IN, inout SurfaceOutputStandard o) {
			//o.Albedo = tex2D(_MainTex, IN.uv_mainTex).rgb * IN.color.rgb * _Color.rgb;
			o.Albedo = IN.color.rgb;
			//o.Alpha = tex2D(_MainTex, IN.uv_mainTex).a * IN.color.a * _Color.a;
			o.Alpha = IN.color.a;
			//o.Specular = 0.2;
			//o.Gloss = 1.0;
		}
		ENDCG
	}
	Fallback "Alpha/VertexLit", 1
}