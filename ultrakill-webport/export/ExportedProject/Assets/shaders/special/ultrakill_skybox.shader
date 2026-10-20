Shader "ULTRAKILL/Skybox" {
	Properties {
		_RotateSky ("Sky Rotation", Range(0, 1)) = 0
		[KeywordEnum(Panorama, Cubemap)] _TexMode ("Texture Type", Float) = 0
		_MainTex ("Texture", 2D) = "white" {}
		_Cubemap ("Cubemap", Cube) = "hiwte" {}
		[IntRange] _SkyTile ("Pano Sky Latitude Tile", Range(1, 4)) = 4
		_SkyHeightOffset ("Pano Sky Height Offset", Float) = 0.5
		_SkyStretch ("Pano Sky Stretch", Float) = 1
		_SkyTopSharpness ("Pano Sky Top Sharpness", Float) = 4
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;
			float4 _MainTex_ST;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Vertex_Stage_Output
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.uv = (input.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			Texture2D<float4> _MainTex;
			SamplerState sampler_MainTex;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(sampler_MainTex, input.uv.xy);
			}

			ENDHLSL
		}
	}
}