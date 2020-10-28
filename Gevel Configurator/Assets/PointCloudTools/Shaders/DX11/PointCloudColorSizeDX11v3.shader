// PointCloud Shader for DX11 Viewer with PointSize

Shader "UnityCoder/PointCloud/DX11/ColorSizeV3"
{
	Properties
	{
		_Size("Size", Float) = 0.01
	}

	SubShader
	{
		Pass
		{
			Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }
			LOD 200

			CGPROGRAM
			#pragma target 5.0
			#pragma vertex VS_Main
			#pragma fragment FS_Main
			#pragma geometry GS_Main
			#include "UnityCG.cginc"

			StructuredBuffer<half3> buf_Points;
			StructuredBuffer<fixed3> buf_Colors;

			struct GS_INPUT
			{
				uint id : VERTEXID;
			};

			struct FS_INPUT
			{
				half4	pos		: POSITION;
				fixed3 color : COLOR;
			};

			float _Size;

			GS_INPUT VS_Main(uint id : SV_VertexID)
			{
				GS_INPUT o;
				o.id = id;
				return o;
			}

			[maxvertexcount(4)]
			void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
			{
				uint id = p[0].id;
				float4 pos = UnityObjectToClipPos(buf_Points[id]);

				fixed3 col = buf_Colors[id];
				#if !UNITY_COLORSPACE_GAMMA
				col = col * col;
				#endif

				// TODO fix aspect ratio
				half4 s = half4(_Size, -_Size,0,0);

				FS_INPUT newVert;
				newVert.pos = pos + s.xxww;
				newVert.color = col;
				triStream.Append(newVert);

				newVert.pos = pos + s.xyww;
				triStream.Append(newVert);

				newVert.pos = pos + s.yxww;
				triStream.Append(newVert);

				newVert.pos = pos + s.yyww;
				triStream.Append(newVert);
			}

			fixed3 FS_Main(FS_INPUT input) : COLOR
			{
				return input.color;
			}
			ENDCG
		}
	}
}