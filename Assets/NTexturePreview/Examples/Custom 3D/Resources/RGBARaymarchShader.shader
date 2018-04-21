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
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		//Raymarch from the back-faces to the front
		cull Front
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
			
			
			#define MAX_STEPS 256.0
			#define ROOT3 1.73205081
			#define DIST ROOT3/MAX_STEPS

            //This implementation raymarches back-to-front to accumulate, as it's easier to support zooming, and doesn't require a box-cast,
            //it just starts from back-faces first.
            //origin is the camera position - we need to check whether we're behind the camera because we support zooming!
			float4 raymarch (float3 position, float3 origin, float3 direction) {
				float4 accumulation = 0;
				for(int i = 0; i<MAX_STEPS; i++){
				    float3 absPos = abs(position);
				    if(dot(normalize(position-origin), direction)>0){ //If the position is behind the camera
				        accumulation += accumulation * DIST;
					} else if(absPos.x>0.5 || absPos.y>0.5 || absPos.z>0.5){ //If the position is outside of the object-cube
					    accumulation += accumulation * DIST;
					}else{ //If the position is inside the object-cube
					    accumulation += tex3D(_MainTex, position + 0.5) / MAX_STEPS;
					}
					position += DIST * direction;
				}
				return accumulation;
			}

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
			
			fixed4 frag (v2f i) : SV_Target
			{
				float3 viewDirection = normalize(ObjSpaceViewDir (i.v));
				float3 pW = i.v.xyz;
				return raymarch(pW.xyz, mul(unity_WorldToObject, _WorldSpaceCameraPos), viewDirection) * fixed4(_R, _G, _B, 1);
			}
			ENDCG
		}
	}
}
