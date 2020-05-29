Shader "HeatMap2D"
{
	Properties
	{
		[NoScaleOffset] _MainTex("HeatTexture", 2D) = "white" {}
	}

	SubShader
	{
		Tags{ "Queue" = "Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha // Alpha blend
		ZWrite Off // Don't write pixels on the Z-buffer for Transparency
		ZTest Always // Always on top of Everything in Transparent Queue or below.

		Pass
		{
			CGPROGRAM
			#pragma vertex vert          
			#pragma fragment frag

			uniform float _InvRadius = 10.0f;
			uniform float _Intensity = 0.1f;
			uniform int _Count = 0;
			// Unity maximum allowed array size for shaders is 1023.
			// We can use StructuredBuffer to overpass this limit,
			// but anyway the fragment shader performance drops too badly beyond this limit.
			uniform float4 _Points[1023]; // x,y,z, w(= weight)
			//StructuredBuffer<float4> _Points; // x,y,z, w(= weight)
			sampler2D _MainTex;

			// "vertex to fragment"
			struct v2f 
			{
				float4 cpos : SV_POSITION; // clip space position
				float3 wpos : TEXCOORD0; // world space position (using texture coordinate interpolator : same algebra can be used to interpolate colors or positions)
			};

			// vertex shader
			v2f vert(float4 vertexPos : POSITION)
			{
				v2f output;
				// Compute Clip Space position.
				output.cpos = UnityObjectToClipPos(vertexPos);
				// Compute World Space position.
				output.wpos = mul(unity_ObjectToWorld, vertexPos);
				return output;
			}
			
			// fragment shader
			float4 frag(v2f input) : SV_TARGET
			{
				float heat = 0.0f, dist;
				float3 vec;
				for (int i = 0; i < _Count; ++i)
				{
					vec = input.wpos - _Points[i].xyz;
					dist = sqrt(vec.x * vec.x + vec.y * vec.y + vec.z * vec.z);
					heat += (1.0f - saturate(dist * _InvRadius)) * _Intensity * _Points[i].w;
				}
				return tex2D(_MainTex, float2(saturate(heat), 0.5f));
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
}