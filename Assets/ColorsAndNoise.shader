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
				float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				float3 pos : TEXTCOORD4;
				float4 color : COLOR;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.pos = v.vertex.xyz;
				o.color = v.color;
				o.uv = v.uv;
				o.uv2 = v.uv2;
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			float noise(float pos, float freq, float seed)
			{
				float rest = fmod(pos, freq);
				float a = frac((pos - rest) * seed * 9991.99);
				float b = frac((pos - rest + freq) * seed * 9991.99);
				return lerp(0, 1, rest / freq);
			}

			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = i.color * noise(i.pos.x, 0.17951329, 1291.7);
				/*(
					noise(i.pos.x, 0.3, 8891.7) +
					noise(i.pos.y, 0.3, 19.3) +
					noise(i.pos.z, 0.3, -581.1)
				) / 3;*/
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}

			ENDCG
		}
	}
}
