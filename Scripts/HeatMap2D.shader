/******************************************************************************************************************************************************
* MIT License																																		  *
*																																					  *
* Copyright (c) 2020																																  *
* Emmanuel Badier <emmanuel.badier@gmail.com>																										  *
* 																																					  *
* Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),  *
* to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,  *
* and/or sell copies of the Software, and to permit persons to whom the Software isfurnished to do so, subject to the following conditions:			  *
* 																																					  *
* The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.					  *
* 																																					  *
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, *
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 																							  *
* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 		  *
* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.							  *
******************************************************************************************************************************************************/

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