Shader "Vertex Colors/Diffuse" {
	Properties {
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert
		#pragma multi_compile_fog

		struct Input {
			float3 vertexColor : COLOR;
		};

		void vert (inout appdata_full v, out Input o)
        {
             UNITY_INITIALIZE_OUTPUT(Input,o);
             o.vertexColor = v.color;
        }

		void surf (Input IN, inout SurfaceOutput o) 
		{
			o.Albedo = IN.vertexColor;
		}
		ENDCG
	} 
}
