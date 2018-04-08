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
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _R, _G, _B, _A;
			float _Mip;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			float3 frag (v2f i) : SV_Target
			{
				float3 col = tex2Dlod(_MainTex, float4(i.uv,0,_Mip)).rgb;
				col = float3(col.r*_R, col.g*_G, col.b*_B);
				return col;
			}
			ENDCG
		}
	}
	Fallback off
}
