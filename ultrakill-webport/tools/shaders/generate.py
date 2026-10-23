import os, re, sys
sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\export\ExportedProject\Assets"
NEW = r"C:\Users\JJ\Documents\Proyecto predeterminado\ultrakill-webport\tools\shaders\new"
os.makedirs(NEW, exist_ok=True)

VERTEX = r'''
			v2f vert(appdata_full v)
			{
				v2f o;

				// Vertex snapping + pixelizacion (con _ResY = 0 no hace nada).
				float4 snapToPixel = UnityObjectToClipPos(v.vertex);
				float4 vertex = snapToPixel;
				vertex.xyz = snapToPixel.xyz / snapToPixel.w;
				if (_ResY > 0.5)
				{
					float gridX = _ResY * 1.7777778;
					vertex.x = floor(gridX * vertex.x) / gridX;
					vertex.y = floor(_ResY * vertex.y) / _ResY;
				}
				vertex.xyz *= snapToPixel.w;
				o.pos = vertex;

				o.color = __LIGHT__;

				float distance = length(mul(UNITY_MATRIX_MV, v.vertex));

				// Affine texture mapping: con _TextureWarping = 0 es correcto.
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				if (_TextureWarping > 0.001) { o.uv *= distance + (vertex.w * (UNITY_LIGHTMODEL_AMBIENT.a * 8)) / distance / 2; }
				o.normal = distance + (vertex.w * (UNITY_LIGHTMODEL_AMBIENT.a * 8)) / distance / 2;
				o.normal.g = (unity_FogEnd - distance) / (unity_FogEnd - unity_FogStart);
				o.normal.b = 1;

				o.colorFog = unity_FogColor;
				o.colorFog.a = clamp((unity_FogEnd - distance) / (unity_FogEnd - unity_FogStart), 0, 1);

				// Recorte de poligonos lejanos, como en el resto del port.
				if (distance > unity_FogStart.z + unity_FogColor.a * 255) { o.pos.w = 0; }

				return o;
			}
'''

PREAMBLE = r'''			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			uniform float _ResY;
			uniform float _TextureWarping;
			uniform float _ColorPrecision;

			struct v2f
			{
				fixed4 pos : SV_POSITION;
				half4 color : COLOR0;
				half4 colorFog : COLOR1;
				float2 uv : TEXCOORD0;
				half3 normal : TEXCOORD1;
			};

			float4 _MainTex_ST;
			uniform half4 unity_FogStart;
			uniform half4 unity_FogEnd;
'''

TAIL = r'''
				return color;
			}
			ENDCG
'''

def fog(call="c"):
    return f'''
				half4 color = {call} * IN.colorFog.a;
				color.rgb += IN.colorFog.rgb * (1 - IN.colorFog.a);
				float resY = _ResY > 0.5 ? _ResY : 2048.0;
				float prec = _ColorPrecision > 1.5 ? (_ColorPrecision - 1.0) : (resY - 1.0);
				color.rgb = floor(color.rgb * prec) / prec;
'''

def shader(name, props, subshader_tags, pass_state, frag, light="float4(ShadeVertexLightsFull(v.vertex, v.normal, 4, true), 1.0)"):
    p = "\n".join("\t\t" + x for x in props)
    st = "\n".join("\t\t\t" + x for x in pass_state)
    return f'''Shader "{name}" {{
	Properties {{
{p}
	}}
	SubShader {{
		Tags {{ {subshader_tags} }}
		LOD 200

		Pass {{
{st}
			Lighting On
			CGPROGRAM
{PREAMBLE}
{VERTEX.replace("__LIGHT__", light)}
{frag}
{TAIL}
		}}
	}}
}}
'''

# ---------------------------------------------------------------- 1. emissive
emissive = shader(
    "psx/vertexlit-noambient-emissive",
    ['_MainTex("Base (RGB)", 2D) = "white" {}',
     '_Color("Color", Color) = (1,1,1,1)',
     '_EmissiveTex("Emissive Mask (G)", 2D) = "black" {}',
     '_EmissiveColor("Emissive Color (RGB)", Color) = (1,1,1,1)',
     '_EmissiveIntensity("Emissive Strength", Range(0,8)) = 1',
     '_UseAlbedoAsEmissive("Use Albedo As Emissive", Float) = 0',
     '_EmissiveReplaces("Emissive Replaces", Float) = 0',
     '_Outline("Outline", Range(0,1)) = 0',
     '_VertexWarpScale("Vertex Warp Scale", Range(0,10)) = 1'],
    '"RenderType" = "Opaque" "Queue" = "Geometry"',
    ['ZWrite On', 'Cull Back'],
    r'''
			sampler2D _MainTex;
			sampler2D _EmissiveTex;
			float4 _Color;
			float4 _EmissiveColor;
			float _EmissiveIntensity;
			float _UseAlbedoAsEmissive;
			float _EmissiveReplaces;

			float4 frag(v2f IN) : COLOR
			{
				float2 uv = _TextureWarping > 0.001 ? IN.uv / IN.normal.r : IN.uv;
				half4 albedo = tex2D(_MainTex, uv) * _Color;
				half4 c = albedo * IN.color;
				float mask = lerp(tex2D(_EmissiveTex, uv).g, albedo.g, saturate(_UseAlbedoAsEmissive));
				half3 emissive = _EmissiveColor.rgb * mask * _EmissiveIntensity;
				c.rgb = lerp(c.rgb + emissive, emissive, saturate(_EmissiveReplaces));
''' + fog("c"))

# ---------------------------------------------------------------- 2. railgun
railgun = shader(
    "psx/railgun",
    ['_MainTex("Base (RGB)", 2D) = "white" {}',
     '_Color("Color", Color) = (1,1,1,1)',
     '_EmissiveColor("Emissive Color (RGB)", Color) = (1,1,1,1)',
     '_EmissiveIntensity("Emissive Strength", Range(0,8)) = 1',
     '_EmissivePosition("Emissive Position", Range(0,10)) = 0',
     '_Outline("Outline", Range(0,1)) = 0',
     '_VertexWarpScale("Vertex Warp Scale", Range(0,10)) = 1'],
    '"RenderType" = "Opaque" "Queue" = "Geometry"',
    ['ZWrite On', 'Cull Back'],
    r'''
			sampler2D _MainTex;
			float4 _Color;
			float4 _EmissiveColor;
			float _EmissiveIntensity;

			float4 frag(v2f IN) : COLOR
			{
				float2 uv = _TextureWarping > 0.001 ? IN.uv / IN.normal.r : IN.uv;
				half4 albedo = tex2D(_MainTex, uv) * _Color;
				half4 c = albedo * IN.color;
				// El railgun lleva el brillo en la propia textura: el emisivo la
				// realza sin depender de la luz.
				c.rgb += albedo.rgb * _EmissiveColor.rgb * _EmissiveIntensity;
''' + fog("c"))

# ---------------------------------------------------------------- 3. water
water = shader(
    "psx/water",
    ['_MainTex("Base (RGB)", 2D) = "white" {}',
     '_Color("Color", Color) = (1,1,1,1)',
     '_VertexNoise("Vertex Noise", 2D) = "white" {}',
     '_OpacScale("Opacity", Range(0,2)) = 1',
     '_FlowDirection("Flow Direction", Vector) = (1,1,1,1)',
     '_ScrollMainTextureX("Scroll X", Float) = 0',
     '_ScrollMainTextureY("Scroll Y", Float) = 0',
     '_VertexNoiseScale("Noise Scale", Range(0,10)) = 1',
     '_VertexNoiseSpeed("Noise Speed", Range(0,10)) = 1',
     '_VertexNoiseAmplitude("Noise Amplitude", Range(0,10)) = 1',
     '_VertexWarpScale("Vertex Warp Scale", Range(0,10)) = 1'],
    '"Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True"',
    ['Blend SrcAlpha OneMinusSrcAlpha', 'ZWrite On', 'Cull Off'],
    r'''
			sampler2D _MainTex;
			float4 _Color;
			float _OpacScale;
			float4 _FlowDirection;
			float _ScrollMainTextureX;
			float _ScrollMainTextureY;

			float4 frag(v2f IN) : COLOR
			{
				float2 uv = _TextureWarping > 0.001 ? IN.uv / IN.normal.r : IN.uv;
				// El scroll va en el fragment para no usar properties numericas
				// en el vertex (en GLES no se declaran alli).
				uv += float2(_ScrollMainTextureX, _ScrollMainTextureY) * _FlowDirection.xy * _Time.y;
				half4 c = tex2D(_MainTex, uv) * _Color * IN.color;
				c.a *= _OpacScale;
''' + fog("c"))

# ---------------------------------------------------------------- 4. heartbeat
heartbeat = shader(
    "psx/heartbeat",
    ['_MainTex("Base (RGB)", 2D) = "white" {}',
     '_Color("Color", Color) = (1,1,1,1)',
     '_OpacScale("Opacity", Range(0,2)) = 1',
     '_UseVCols("Use Vertex Colors", Float) = 1',
     '_FlowDirection("Flow Direction", Vector) = (1,1,1,1)',
     '_ScrollMainTextureX("Scroll X", Float) = 0',
     '_ScrollMainTextureY("Scroll Y", Float) = 0',
     '_VertexNoiseScale("Noise Scale", Range(0,10)) = 1',
     '_VertexNoiseSpeed("Noise Speed", Range(0,10)) = 1',
     '_VertexNoiseAmplitude("Noise Amplitude", Range(0,10)) = 1',
     '_VertexWarpScale("Vertex Warp Scale", Range(0,10)) = 1'],
    '"Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True"',
    ['Blend SrcAlpha OneMinusSrcAlpha', 'ZWrite On', 'Cull Off'],
    r'''
			sampler2D _MainTex;
			float4 _Color;
			float _OpacScale;
			float _UseVCols;
			float4 _FlowDirection;
			float _ScrollMainTextureX;
			float _ScrollMainTextureY;
			float _VertexNoiseSpeed;

			float4 frag(v2f IN) : COLOR
			{
				float2 uv = _TextureWarping > 0.001 ? IN.uv / IN.normal.r : IN.uv;
				uv += float2(_ScrollMainTextureX, _ScrollMainTextureY) * _FlowDirection.xy * _Time.y;
				half4 c = tex2D(_MainTex, uv) * _Color;
				c.rgb = lerp(c.rgb, c.rgb * IN.color.rgb, saturate(_UseVCols));
				// Latido suave.
				c.a *= _OpacScale * (0.8 + 0.2 * sin(_Time.y * (_VertexNoiseSpeed + 2.0)));
''' + fog("c"), light="v.color")

# ---------------------------------------------------------------- 5. bloodstain
bloodstain = shader(
    "ULTRAKILL/bloodstain",
    ['_MainTex("Base (RGB)", 2D) = "white" {}',
     '_Color("Color", Color) = (1,1,1,1)',
     '_OpacScale("Opacity", Range(0,2)) = 1',
     '_VertexWarpScale("Vertex Warp Scale", Range(0,10)) = 1'],
    '"Queue" = "Transparent-1" "RenderType" = "Transparent" "IgnoreProjector" = "True"',
    ['Blend SrcAlpha SrcColor', 'ZWrite Off', 'Cull Back'],
    r'''
			sampler2D _MainTex;
			float4 _Color;
			float _OpacScale;

			float4 frag(v2f IN) : COLOR
			{
				float2 uv = _TextureWarping > 0.001 ? IN.uv / IN.normal.r : IN.uv;
				half4 c = tex2D(_MainTex, uv) * _Color * IN.color;
				c.a *= _OpacScale;
''' + fog("c"))

# ---------------------------------------------------------------- 6. probuilder standard
probuilder = shader(
    "ProBuilder/Standard Vertex Color",
    ['_Color("Color", Color) = (1,1,1,1)',
     '_MainTex("Albedo", 2D) = "white" {}',
     '_Glossiness("Smoothness", Range(0,1)) = 0.5',
     '_Metallic("Metallic", Range(0,1)) = 0'],
    '"RenderType" = "Opaque" "Queue" = "Geometry"',
    ['ZWrite On', 'Cull Back'],
    r'''
			sampler2D _MainTex;
			float4 _Color;

			float4 frag(v2f IN) : COLOR
			{
				float2 uv = _TextureWarping > 0.001 ? IN.uv / IN.normal.r : IN.uv;
				half4 c = tex2D(_MainTex, uv) * _Color * IN.color;
''' + fog("c"))

# ---------------------------------------------------------------- 7. hidden
hidden = '''Shader "Hidden/ProBuilder/HideVertices" {
	Properties {
	}
	SubShader {
		Tags { "RenderType" = "Opaque" }
		Pass {
			ColorMask 0
			ZWrite Off
			Cull Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			struct appdata { float4 vertex : POSITION; };
			struct v2f { float4 pos : SV_POSITION; };
			v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); return o; }
			float4 frag(v2f i) : COLOR { return float4(0,0,0,0); }
			ENDCG
		}
	}
}
'''

# ---------------------------------------------------------------- 8. invert
invert = '''Shader "ULTRAKILL/Invert" {
	Properties {
	}
	SubShader {
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
		Pass {
			Blend OneMinusDstColor Zero
			ZWrite Off
			ZTest Always
			Cull Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			struct appdata { float4 vertex : POSITION; };
			struct v2f { float4 pos : SV_POSITION; };
			v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); return o; }
			float4 frag(v2f i) : COLOR { return float4(1,1,1,1); }
			ENDCG
		}
	}
}
'''

# ---------------------------------------------------------------- 9. postprocess
postprocess = '''Shader "ULTRAKILL/Main_PostProcess" {
	Properties {
		_MainTex("Base (RGB)", 2D) = "white" {}
		_NoiseTex("Noise", 2D) = "white" {}
		_UnderWaterScale("Underwater Scale", Range(0,10)) = 0
		_UnderWaterStrength("Underwater Strength", Range(0,1)) = 0
		_UnderWaterSpeed("Underwater Speed", Range(0,10)) = 0
		_ClampFinalOpacity("Clamp Final Opacity", Range(0,1)) = 1
	}
	SubShader {
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
		Pass {
			Blend One OneMinusSrcAlpha
			ZWrite Off
			ZTest Always
			Cull Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _ClampFinalOpacity;
			struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
			struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
			v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = TRANSFORM_TEX(v.uv, _MainTex); return o; }
			float4 frag(v2f i) : COLOR
			{
				half4 c = tex2D(_MainTex, i.uv);
				c.a = saturate(c.a * _ClampFinalOpacity);
				return c;
			}
			ENDCG
		}
	}
}
'''

# ---------------------------------------------------------------- 10. pickers
def picker(name, props, sample_tex):
    body = '''
			struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
			struct v2f { float4 pos : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
			v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.color = v.color; o.uv = v.uv; return o; }
''' + ("			sampler2D _MainTex;\n			float4 _Color;\n" if sample_tex else "			float4 _Color;\n") + '''
			float4 frag(v2f i) : COLOR
			{
				float4 c = ''' + ("tex2D(_MainTex, i.uv) * " if sample_tex else "") + '''_Color * i.color;
				return c;
			}
'''
    return f'''Shader "{name}" {{
	Properties {{
{chr(10).join(chr(9)+chr(9)+p for p in props)}
	}}
	SubShader {{
		Tags {{ "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }}
		Pass {{
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			ZTest Always
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
{body}
			ENDCG
		}}
	}}
}}
'''

pickers = [
    ("Hidden/ProBuilder/EdgePicker", ['_Color("Color", Color) = (1,1,1,1)'], False),
    ("Hidden/ProBuilder/FacePicker", ['_Color("Color", Color) = (1,1,1,1)'], True),
    ("Hidden/ProBuilder/VertexPicker", ['_Color("Color", Color) = (1,1,1,1)'], False),
    ("ProBuilder/UnlitVertexColor", ['_Color("Color", Color) = (1,1,1,1)'], True),
]

# ---------------------------------------------------------------- 11. volumetric
volumetric = '''Shader "Unlit/Volumetric" {
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
'''

FILES = {
    "psx/vertexlit-noambient-emissive": emissive,
    "psx/railgun": railgun,
    "psx/water": water,
    "psx/heartbeat": heartbeat,
    "ULTRAKILL/bloodstain": bloodstain,
    "ProBuilder/Standard Vertex Color": probuilder,
    "Hidden/ProBuilder/HideVertices": hidden,
    "ULTRAKILL/Invert": invert,
    "ULTRAKILL/Main_PostProcess": postprocess,
    "Unlit/Volumetric": volumetric,
}
for nm, pr, tex in pickers:
    FILES[nm] = picker(nm, pr, tex)

for name, text in FILES.items():
    safe = re.sub(r"[^a-zA-Z0-9]+", "_", name).strip("_")
    p = os.path.join(NEW, safe + ".shader")
    text = "// port: shader funcional (shadertest)\n" + text
    open(p, "w", encoding="utf-8", newline="\n").write(text)
    print(f"{name:42} -> {os.path.basename(p)}  {len(text)} B")

print("\ntotal:", len(FILES))
