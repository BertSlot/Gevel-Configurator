Shader "UnityCoder/PointCloud/DX11/ColorSizeV2-packed"
{
	Properties
	{
		_Size("Size", Float) = 1.0
		[KeywordEnum(Off, On)] _Circle("Circular Point",float) = 1
	}

		SubShader
	{
		Pass
		{
			Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }
			ZWrite On
			LOD 200
			Cull Off

			CGPROGRAM
			#pragma target 5.0
			#pragma vertex VS_Main
			#pragma fragment FS_Main
			#pragma geometry GS_Main
			#pragma multi_compile _CIRCLE_ON _CIRCLE_OFF
			#include "UnityCG.cginc"
			#pragma fragmentoption ARB_precision_hint_fastest

			struct Point
			{
				float x;
				float y;
				float z;
			};
			StructuredBuffer<Point> buf_Points;

			struct appdata
			{
				fixed3 color : COLOR;
			};

			struct GS_INPUT
			{
				float4	pos		: POSITION;
				fixed3 color : COLOR;
			};

			struct FS_INPUT
			{
				float4	pos		: POSITION;
				fixed3 color : COLOR;
				#ifdef _CIRCLE_ON
				float2 uv : TEXCOORD0;
				#endif
			};

			float _Size;

			float2 SuperUnpacker(float f)
			{
				return float2(f - floor(f), floor(f) / 1024);
			}

			GS_INPUT VS_Main(uint id : SV_VertexID, uint inst : SV_InstanceID)
			{
				GS_INPUT o = (GS_INPUT)0;

				float2 xr = SuperUnpacker(buf_Points[id].x);
				float2 yg = SuperUnpacker(buf_Points[id].y);
				float2 zb = SuperUnpacker(buf_Points[id].z);

				float3 p = float3(xr.y,yg.y,zb.y);
				o.pos = UnityObjectToClipPos(p);

				//fixed3 col = float3(abs(xr.x),abs(yg.x),abs(zb.x));
				fixed3 col = fixed3(saturate(xr.x), saturate(yg.x), saturate(zb.x)) * 1.02;

				#if !UNITY_COLORSPACE_GAMMA
				col = col * col; // linear
				#endif
				o.color = col;
				return o;
			}

			[maxvertexcount(4)]
			void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
			{
				float width = _Size * (_ScreenParams.z - 1);
				float height = _Size * (_ScreenParams.w - 1);
				float4 vertPos = p[0].pos;

				FS_INPUT newVert;
				newVert.pos = vertPos + float4(-width,-height,0,0);
				newVert.color = p[0].color;
				#ifdef _CIRCLE_ON
				newVert.uv = float2(0,0);
				#endif
				triStream.Append(newVert);

				newVert.pos = vertPos + float4(-width,height,0,0);
				#ifdef _CIRCLE_ON
				newVert.uv = float2(1,0);
				#endif
				triStream.Append(newVert);

				newVert.pos = vertPos + float4(width,-height,0,0);
				#ifdef _CIRCLE_ON
				newVert.uv = float2(0,1);
				#endif
				triStream.Append(newVert);

				newVert.pos = vertPos + float4(width,height,0,0);
				#ifdef _CIRCLE_ON
				newVert.uv = float2(1,1);
				#endif
				triStream.Append(newVert);
			}

			#ifdef _CIRCLE_ON
			// source https://thebookofshaders.com/07/
			float circle(float2 _st, float _radius)
			{
				float2 dist = _st - float2(0.5,0.5);
				return 1. - smoothstep(_radius - (_radius * 0.01),_radius + (_radius * 0.01),dot(dist,dist) * 4.0);
			}
			#endif

			float4 FS_Main(FS_INPUT input) : SV_Target
			{
				#ifdef _CIRCLE_ON
				clip(circle(input.uv,0.9) - 0.999);
				#endif
				return float4(input.color,1);
			}
			ENDCG
		}
	}
}