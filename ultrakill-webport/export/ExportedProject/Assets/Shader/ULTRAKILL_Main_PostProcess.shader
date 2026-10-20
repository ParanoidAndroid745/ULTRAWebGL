Shader "ULTRAKILL/Main_PostProcess" {
	Properties {
		_MainTex ("Render Texture", 2D) = "black" {}
		_NoiseTex ("Tiling Noise", 2D) = "white" {}
		_UnderWaterScale ("Underwater Warp Scaling", Float) = 1
		_UnderWaterStrength ("Underwater Warp Strength", Float) = 1
		_UnderWaterSpeed ("Underwater Warp Speed", Float) = 1
		[MaterialToggle] _ClampFinalOpacity ("Ensure Final Opacity Never goes Below 0", Float) = 0
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
		LOD 200

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Cull Off
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