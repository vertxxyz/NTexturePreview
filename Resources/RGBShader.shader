Shader "Hidden/RGBShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		[Toggle(R)]
        _R ("R", Float) = 1
        [Toggle(G)]
        _G ("G", Float) = 1
        [Toggle(B)]
        _B ("B", Float) = 1
        _Mip ("Mip", Float) = 0
	}
	SubShader
	{
		Tags { "ForceSupported"="true" }

        Lighting Off
        Ztest Always
        Cull Off
        Zwrite True
        Fog { Mode Off }

		Pass
		{
			CGPROGRAM
			
			#pragma vertex vert
			#pragma fragment frag
			
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float2 clipUV : TEXCOORD1;
			};

            sampler2D _GUIClipTexture;
            uniform float4x4 unity_GUIClipTextureMatrix;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _R, _G, _B, _A;
			float _Mip;
			
			v2f vert (appdata v)
			{
				v2f o;
				float3 eyePos = UnityObjectToViewPos(v.vertex);
                o.clipUV = mul(unity_GUIClipTextureMatrix, float4(eyePos.xy, 0, 1.0));
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
			    clip(tex2D(_GUIClipTexture, i.clipUV).a-0.5);
				float3 col = tex2Dlod(_MainTex, float4(i.uv,0,_Mip)).rgb;
				col = float3(col.r*_R, col.g*_G, col.b*_B);
				float _ROnly = lerp(lerp(_R, 0, _G), 0, _B);
				float _GOnly = lerp(lerp(_G, 0, _R), 0, _B);
				float _BOnly = lerp(lerp(_B, 0, _R), 0, _G);
				col = float3(
				    lerp(lerp(col.r, col.g, _GOnly), col.b, _BOnly),
				    lerp(lerp(col.g, col.r, _ROnly), col.b, _BOnly),
				    lerp(lerp(col.b, col.r, _ROnly), col.g, _GOnly));
				return float4(col, 1);
			}
			ENDCG
		}
	}
	Fallback off
}
