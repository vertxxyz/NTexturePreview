Shader "Hidden/RGBA3DShader"
{
	Properties
	{
	    _MainTex ("Texture", 3D) = "white" {}
		[Toggle(R)]
        _R ("R", Float) = 1
        [Toggle(G)]
        _G ("G", Float) = 1
        [Toggle(B)]
        _B ("B", Float) = 1
        _X ("X", Float) = 1
        _Y ("Y", Float) = 1
        _Z ("Z", Float) = 1
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		Cull Front
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			sampler3D _MainTex;
			float4 _MainTex_ST;
			float _R, _G, _B, _A;
			float _Mip;
			float _X, _Y, _Z;
			
			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
			    float4 pos : SV_POSITION;
				float4 v : TEXCOORD1;
			};
			
			v2f vert (appdata v)
			{
				v2f o;
				o.v = v.vertex;
				o.pos = UnityObjectToClipPos(v.vertex);
				return o;
			}
			
			#define halfBoxSize 0.5
			
			bool IntersectBox(float3 ray_o, float3 ray_d, float3 boxMin, float3 boxMax, out float tNear, out float tFar)
			{
			    // compute intersection of ray with all six bbox planes
			    float3 invR = 1.0 / ray_d;
			    float3 tBot = invR * (boxMin.xyz - ray_o);
			    float3 tTop = invR * (boxMax.xyz - ray_o);
			    // re-order intersections to find smallest and largest on each axis
			    float3 tMin = min (tTop, tBot);
			    float3 tMax = max (tTop, tBot);
			    // find the largest tMin and the smallest tMax
			    float2 t0 = max (tMin.xx, tMin.yz);
			    float largest_tMin = max (t0.x, t0.y);
			    t0 = min (tMax.xx, tMax.yz);
			    float smallest_tMax = min (t0.x, t0.y);
			    // check for hit
			    bool hit = (largest_tMin <= smallest_tMax);
			    tNear = largest_tMin;
			    tFar = smallest_tMax;
			    return hit;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
//			    _X = (_SinTime.x/2.0) + 0.5;
//			    _Y = (_SinTime.x/2.0) + 0.5;
//			    _Z = (_SinTime.x/2.0) + 0.5;
				float3 viewDirection = normalize(ObjSpaceViewDir (i.v));
				float tMin, tFar;
				float3 maxSize =  float3(min(_X-halfBoxSize, halfBoxSize), min(_Y-halfBoxSize, halfBoxSize), min(_Z-halfBoxSize, halfBoxSize));
				if(!IntersectBox(i.v.xyz, viewDirection, -halfBoxSize, maxSize, tMin, tFar))
				    clip(-1);
				float3 uv = i.v.xyz + tFar * viewDirection + halfBoxSize;
				return tex3D(_MainTex, uv) * fixed4(_R, _G, _B, 1);
			}
			ENDCG
		}
	}
}
