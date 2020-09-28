Shader "Custom/PointMeshSizeDX11Box (DirectionalLight)" 
{
	Properties 
	{
	    _Color ("ColorTint", Color) = (1,1,1,1)
		_Size ("Size", Float) = 30
	}

	SubShader 
	{
		Tags { "Queue" = "Geometry" "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			Tags { "LightMode" = "ForwardBase" }
			Name "ForwardBase"
		
			CGPROGRAM
			#pragma vertex VS_Main
			#pragma fragment FS_Main
			#pragma geometry GS_Main
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap
			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"
			
			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
			};
		
			struct GS_INPUT
			{
				float4	pos		: POSITION;
				fixed4 color 	: COLOR;
			};

			struct FS_INPUT
			{
				float4	pos		: POSITION;
				fixed4 color 	: COLOR;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			float _Size;
	        fixed4 _Color;

			GS_INPUT VS_Main(appdata v)
			{
				GS_INPUT o = (GS_INPUT)0;
				o.pos = v.vertex;
				fixed4 col = v.color;
				#if !UNITY_COLORSPACE_GAMMA
				col = col*col; // linear
				#endif
				o.color = col*_Color;
				return o;
			}

			fixed4 GetLight(half3 worldNormal)
			{
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
                fixed4 light = nl * _LightColor0;
				return light;
			}

			[maxvertexcount(24)]
			void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
			{
				float4 vertPos = p[0].pos;
				FS_INPUT newVert;

				// front face 4
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,-_Size,_Size,0));
				newVert.color = p[0].color*GetLight(half3(0,0,1));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,-_Size,_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,_Size,_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,_Size,_Size,0));
				triStream.Append(newVert);
				triStream.RestartStrip();

				// back face 8
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,_Size,-_Size,0));
				newVert.color = p[0].color*GetLight(half3(0,0,-1));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,-_Size,-_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,_Size,-_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,-_Size,-_Size,0));
				triStream.Append(newVert);
				triStream.RestartStrip();

				// left face 12
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,-_Size,-_Size,0));
				newVert.color = p[0].color*GetLight(half3(1,0,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,_Size,-_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,-_Size,_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,_Size,_Size,0));
				triStream.Append(newVert);
				triStream.RestartStrip();

				// right face 16
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,_Size,_Size,0));
				newVert.color = p[0].color*GetLight(half3(-1,0,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,_Size,-_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,-_Size,_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,-_Size,-_Size,0));
				triStream.Append(newVert);
				triStream.RestartStrip();

				// top face 20
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,_Size,_Size,0));
				newVert.color = p[0].color*GetLight(half3(0,1,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,_Size,-_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,_Size,_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,_Size,-_Size,0));
				triStream.Append(newVert);
				triStream.RestartStrip();

				// bottom face 24
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,-_Size,-_Size,0));
				newVert.color = p[0].color*GetLight(half3(0,-1,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,-_Size,-_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,-_Size,_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,-_Size,_Size,0));
				triStream.Append(newVert);
				triStream.RestartStrip();
			}

			fixed4 FS_Main(FS_INPUT input) : COLOR
			{
				return input.color;
			}
			ENDCG
		}
	} 
}
