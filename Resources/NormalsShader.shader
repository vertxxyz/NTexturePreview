Shader "Hidden/NormalsShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "bump" {}
		[Toggle(R)]
        _R ("R", Float) = 1
        [Toggle(G)]
        _G ("G", Float) = 1
        [Toggle(B)]
        _B ("B", Float) = 1
        _Mip ("Mip", Float) = 0
        _LightX ("LightX", Float) = 0.5
        _LightY ("LightY", Float) = 0.5
        _LightZ ("LightZ", Float) = 0.1
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
			#pragma multi_compile PREVIEW_NORMAL PREVIEW_DIFFUSE
			
			
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
			float _LightX, _LightY, _LightZ;
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
			    float3 normal = UnpackNormal(tex2Dlod(_MainTex, float4(i.uv,0,_Mip)));
			    #if PREVIEW_NORMAL
                    float3 col = normal*0.5+0.5;
                    col = float3(col.r*_R, col.g*_G, col.b*_B);
                    return float4(col, 1);
                #endif
				#if PREVIEW_DIFFUSE
                    float3 fakeLightPos = float3(_LightX, _LightY, _LightZ);
                    float3 position = float3(i.uv.x, i.uv.y, 0);
                    float diffuse = dot(normalize(fakeLightPos-position), normalize(normal));
                    return float4(diffuse, diffuse, diffuse, 1);
				#endif
			}
			ENDCG
		}
	}
	Fallback off
}
