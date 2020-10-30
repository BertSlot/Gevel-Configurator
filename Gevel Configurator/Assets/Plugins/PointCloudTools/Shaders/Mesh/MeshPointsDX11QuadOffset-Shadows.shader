Shader "UnityCoder/Mesh/PointMeshSizeDX11(Shadows)" 
{
	Properties 
	{
	    _Color ("ColorTint", Color) = (1,1,1,1)
		_Size ("Size", Float) = 30
	}

	SubShader 
	{
		// +501 is to avoid shadows showing through
		Tags { "RenderType"="Opaque" "IgnoreProjector" = "True" "Queue"="Geometry+501"}
		LOD 200

		Pass
		{
		    Tags{ "LightMode" = "ForwardBase" }
			CGPROGRAM
			#pragma vertex VS_Main
			#pragma fragment FS_Main
			#pragma geometry GS_Main
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #include "AutoLight.cginc"
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
			};
		
			struct GS_INPUT
			{
				float4	pos		: POSITION;
				fixed4 color 	: COLOR;
                SHADOW_COORDS(1)
			};

			struct FS_INPUT
			{
				float4	pos		: POSITION;
				fixed4 color 	: COLOR;
                SHADOW_COORDS(1)
			};

			float _Size;
	        fixed4 _Color;

			GS_INPUT VS_Main(appdata v)
			{
				GS_INPUT o;
                UNITY_INITIALIZE_OUTPUT(GS_INPUT, o);
				o.pos = UnityObjectToClipPos(v.vertex);
				fixed4 col = v.color;
				#if !UNITY_COLORSPACE_GAMMA
				col = col*col; // linear
				#endif
				o.color = col*_Color;
				return o;
			}

			[maxvertexcount(4)]
			void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
			{
				float width = _Size*(_ScreenParams.z-1);
				float height = _Size*(_ScreenParams.w-1);
				float4 vertPos = p[0].pos;
				FS_INPUT newVert;
				newVert.pos = vertPos + float4(-width,-height,0,0);
				newVert.color = p[0].color;
                TRANSFER_SHADOW(newVert);
				triStream.Append(newVert);
				newVert.pos = vertPos + float4(width,-height,0,0);
				triStream.Append(newVert);
				newVert.pos = vertPos + float4(-width,height,0,0);
				triStream.Append(newVert);
				newVert.pos = vertPos + float4(width,height,0,0);
				triStream.Append(newVert);	

			}

			fixed4 FS_Main(FS_INPUT input) : COLOR
			{
				fixed4 col = input.color;
                col *= SHADOW_ATTENUATION(input);
				return col;
			}
			ENDCG
		}
	}
	FallBack "UnityCoder/Common/VertexShadowPass"
}
