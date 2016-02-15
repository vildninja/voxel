Shader "Vertex Colors/Noise"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
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

			float noise(float pos, float freq, float seed)
			{
				int min = floor(pos / freq);
				float a = sin(min * seed * 1991.99);
				float b = sin((min + 1) * seed * 1991.99);
				return lerp(a, b, pos / freq - min) * 0.5 + 0.5;
			}
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.pos = float4(mul(_Object2World, v.vertex).xyz, o.vertex.z);

				o.uv.x = (noise(o.pos.x, 0.047951329, 1291.7) +
					noise(o.pos.y, 0.047951329, 1291.7) + noise(o.pos.z, 0.047951329, 1291.7)) / 3;
				o.uv.y = (noise(o.pos.x, 0.147951329, 1291.7) +
					noise(o.pos.y, 2.047951329, 1291.7) + noise(o.pos.z, 0.347951329, 1291.7)) / 3;

				o.color = v.color;
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}


			fixed4 frag(v2f i) : SV_Target
			{
				// sample the texture
				//fixed4(i.pos * 0.1, 1);// 
				fixed4 col = i.color * lerp((tex2D(_MainTex, i.uv) +
					noise(i.pos.x + i.pos.y * 0.2 + i.pos.z * 0.3, 0.047951329, 1291.7) +
					noise(i.pos.x * 0.3 + i.pos.y + i.pos.z * 0.2, 0.047951329, 1291.7) +
					noise(i.pos.x * 0.2 + i.pos.y * 0.3 + i.pos.z, 0.047951329, 1291.7)) / 4, fixed4(0.5, 0.5, 0.5, 1), i.pos.w / 60);// +fixed4(i.pos / 10, 0);
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
