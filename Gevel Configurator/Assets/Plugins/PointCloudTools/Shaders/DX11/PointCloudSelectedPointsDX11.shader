Shader "UnityCoder/PointCloud/DX11/SelectedPoints(Alpha)"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_Size("Size", Float) = 0.01
	}

		SubShader
	{
		Pass
		{
			//Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }
			Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
			Blend SrcAlpha OneMinusSrcAlpha
			LOD 200
			
			Offset -1,-1

			CGPROGRAM
			#pragma target 5.0
			#pragma vertex VS_Main
			#pragma fragment FS_Main
			#pragma geometry GS_Main
			#include "UnityCG.cginc"

			StructuredBuffer<half3> buf_Points;

			struct GS_INPUT
			{
				uint id : VERTEXID;
			};

			struct FS_INPUT
			{
				half4	pos		: POSITION;
			};

			float _Size;
			fixed4 _Color;

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
				float3 pos = buf_Points[id];

				float3 cameraUp = UNITY_MATRIX_IT_MV[1].xyz;
				float3 cameraForward = _WorldSpaceCameraPos - pos;
				float3 rightSize = normalize(cross(cameraUp, cameraForward))*_Size;
				float3 cameraSize = _Size * cameraUp;

				FS_INPUT newVert;
				newVert.pos = UnityObjectToClipPos(float4(pos + rightSize - cameraSize,1));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(float4(pos + rightSize + cameraSize,1));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(float4(pos - rightSize - cameraSize,1));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(float4(pos - rightSize + cameraSize,1));
				triStream.Append(newVert);
			}

			fixed4 FS_Main(FS_INPUT input) : COLOR
			{
				return _Color;
			}
			ENDCG
		}
	}
}