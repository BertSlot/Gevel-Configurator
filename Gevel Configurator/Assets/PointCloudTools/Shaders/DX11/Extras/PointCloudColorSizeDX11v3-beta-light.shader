// Upgrade NOTE: replaced '_LightMatrix0' with 'unity_WorldToLight'

Shader "UnityCoder/PointCloud/DX11/ColorSizeV3-beta-light" 
{
	Properties 
	{
		_Size ("Size", Float) = 0.1
	}

	SubShader 
	{
		Tags { "Queue" = "Geometry" "RenderType"="Opaque" }

		Pass
		{
            Tags { "LightMode" = "ForwardAdd" }
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex VS_Main
			#pragma fragment FS_Main
			#pragma geometry GS_Main
			#pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_fwdadd_fullshadows
			#include "AutoLight.cginc"
			#include "Lighting.cginc"
												
			StructuredBuffer<half3> buf_Points;
			StructuredBuffer<fixed3> buf_Colors;
	
			struct GS_INPUT
			{
				half4	pos		: POSITION;
				fixed3 color 	: COLOR;
                fixed3 _LightCoord : TEXCOORD1;
				fixed3 _ShadowCoord : TEXCOORD2;
			};

			struct FS_INPUT
			{
				half4	pos		: POSITION;
				fixed3 color 	: COLOR;
                fixed3 _LightCoord : TEXCOORD1;
				fixed3 _ShadowCoord : TEXCOORD2;
			};

			float _Size;

			GS_INPUT VS_Main(uint id : SV_VertexID, uint inst : SV_InstanceID)
			{
				GS_INPUT o = (GS_INPUT)0;
				o.pos = UnityObjectToClipPos(buf_Points[id]);
				
				fixed3 col = buf_Colors[id];
				#if !UNITY_COLORSPACE_GAMMA
				col = col*col; // linear
				#endif
				o.color = col;
				o._LightCoord.xy = mul(unity_WorldToLight, mul(unity_ObjectToWorld, float4(buf_Points[id],1))).xy;
				TRANSFER_SHADOW(o);
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
				newVert._LightCoord = p[0]._LightCoord;
				newVert._ShadowCoord = p[0]._ShadowCoord;
				triStream.Append(newVert);
				newVert.pos = vertPos + float4(width,-height,0,0);
				triStream.Append(newVert);
				newVert.pos = vertPos + float4(-width,height,0,0);
				triStream.Append(newVert);
				newVert.pos = vertPos + float4(width,height,0,0);
				triStream.Append(newVert);
			}

			float4 FS_Main(FS_INPUT input) : SV_Target
			{
                return LIGHT_ATTENUATION(input);
//				return float4(input.color,1);
			}
			ENDCG
		}
	} 
}