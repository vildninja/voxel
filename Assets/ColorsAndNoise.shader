Shader "Vertex Colors/Noise"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "LightMode"="ForwardBase" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc" // for _LightColor0

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float3 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 pos : TEXCOORD1;
				float4 color : COLOR;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			float3 gradient(int3 pos, float seed)
			{
				return sin(pos * seed * float3(91.99113111 + pos.y, 31.111993311 + pos.z, 11.7137137137 + pos.x));
			}

			float dotGradient(float3 dist, int3 pos, float seed)
			{
				return dot(dist, gradient(pos, seed));
			}

			float noise3(float3 pos, float freq, float seed)
			{
				float3 upscaled = pos / freq;
				int3 min = floor(upscaled);
				float3 d = upscaled - floor(upscaled);

				float c000 = dotGradient(d, min, seed);
				float c001 = dotGradient(float3(d.x, d.y, 1 - d.z), min + int3(0, 0, 1), seed);
				float c010 = dotGradient(float3(d.x, 1 - d.y, d.z), min + int3(0, 1, 0), seed);
				float c011 = dotGradient(float3(d.x, 1 - d.y, 1 - d.z), min + int3(0, 1, 1), seed);
				float c100 = dotGradient(float3(1 - d.x, d.y, d.z), min + int3(1, 0, 0), seed);
				float c101 = dotGradient(float3(1 - d.x, d.y, 1 - d.z), min + int3(1, 0, 1), seed);
				float c110 = dotGradient(float3(1 - d.x, 1 - d.y, d.z), min + int3(1, 1, 0), seed);
				float c111 = dotGradient(float3(1 - d.x, 1 - d.y, 1 - d.z), min + int3(1, 1, 1), seed);

				float cx00 = lerp(c000, c100, d.x);
				float cx10 = lerp(c010, c110, d.x);
				float cx01 = lerp(c001, c101, d.x);
				float cx11 = lerp(c011, c111, d.x);

				float cxy0 = lerp(cx00, cx10, d.y);
				float cxy1 = lerp(cx01, cx11, d.y);

				float cxyz = lerp(cxy0, cxy1, d.z);

/*
				float c0y0 = lerp(c000, c010, dist.y);
				float c1y0 = lerp(c100, c110, dist.y);
				float c0y1 = lerp(c001, c011, dist.y);
				float c1y1 = lerp(c101, c111, dist.y);
				float c00z = lerp(c000, c001, dist.z);
				float c10z = lerp(c100, c101, dist.z);
				float c01z = lerp(c010, c011, dist.z);
				float c11z = lerp(c110, c111, dist.z);
*/


				return cxyz;
			}
/*
			float noise(float pos, float freq, float seed)
			{
				int min = floor(pos / freq);
				float a = sin(min * seed * 1991.99);
				float b = sin((min + 1) * seed * 1991.99);
				return lerp(a, b, pos / freq - min) * 0.5 + 0.5;
			}
			*/
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.pos = float4(mul(_Object2World, v.vertex).xyz, o.vertex.z);
/*
				o.uv.x = (noise(o.pos.x, 0.047951329, 1291.7) +
					noise(o.pos.y, 0.047951329, 1291.7) + noise(o.pos.z, 0.047951329, 1291.7)) / 3;
				o.uv.y = (noise(o.pos.x, 0.147951329, 1291.7) +
					noise(o.pos.y, 2.047951329, 1291.7) + noise(o.pos.z, 0.347951329, 1291.7)) / 3;
*/
				float3 normal = UnityObjectToWorldNormal(v.normal);
				o.color = v.color * (max(0.3, dot(normal, _WorldSpaceLightPos0.xyz)) * _LightColor0);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}


			fixed4 frag(v2f i) : SV_Target
			{
				// sample the texture
				//fixed4(i.pos * 0.1, 1);//
				float r = noise3(i.pos, 0.077951329, 91.13913919111);
				fixed4 col = i.color * lerp(r * 0.4 + 0.6, fixed4(0.7, 0.7, 0.7, 1), i.pos.w / 60);// +fixed4(i.pos / 10, 0);
				/*(
					noise(i.pos.x, 0.3, 8891.7) +
					noise(i.pos.y, 0.3, 19.3) +
					noise(i.pos.z, 0.3, -581.1)
				) / 3;*/
				// apply fog
				//UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}

			ENDCG
		}
	}
}
