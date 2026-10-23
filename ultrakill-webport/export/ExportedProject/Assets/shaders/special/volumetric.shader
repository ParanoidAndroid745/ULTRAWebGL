// port: shader funcional (shadertest)
Shader "Unlit/Volumetric" {
	Properties {
		_MainTex("Base (RGB)", 2D) = "white" {}
		_Center("Center", Float) = 0
		_Radius("Radius", Float) = 1
	}
	SubShader {
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
		Pass {
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Cull Front
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			sampler2D _MainTex;
			float4 _MainTex_ST;
			struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
			struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
			v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = TRANSFORM_TEX(v.uv, _MainTex); return o; }
			float4 frag(v2f i) : COLOR { return tex2D(_MainTex, i.uv); }
			ENDCG
		}
	}
}
