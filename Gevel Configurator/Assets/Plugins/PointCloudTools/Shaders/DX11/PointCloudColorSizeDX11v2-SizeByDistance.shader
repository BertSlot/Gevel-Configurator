// PointCloud Shader for DX11 Viewer with PointSize and ColorTint (version 2, less math)

Shader "UnityCoder/PointCloud/DX11/SizeByDistance" 
{
	Properties 
	{
		_MinDist ("Min Distance", float) = 0
		_MaxDist ("Max Distance", float) = 100
		_MinSize ("Min Size", float) = 0.01
		_MaxSize ("Max Size", float) = 0.1
	}

	SubShader 
	{
		Pass
		{
			Tags { "Queue" = "Geometry" "RenderType"="Opaque" }
			LOD 200
		
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex VS_Main
			#pragma fragment FS_Main
			#pragma geometry GS_Main
			#include "UnityCG.cginc"
			
			StructuredBuffer<half3> buf_Points;
			StructuredBuffer<fixed3> buf_Colors;

			struct appdata
			{
				half4 vertex : POSITION;
				fixed4 color : COLOR;
			};
		
			struct GS_INPUT
			{
				half3	pos		: TEXCOORD0;
				fixed4 color 	: COLOR;
			};

			struct FS_INPUT
			{
				half4	pos		: POSITION;
				fixed4 color 	: COLOR;
			};

			float _MinDist, _MaxDist;
			float _MinSize, _MaxSize;

			GS_INPUT VS_Main(uint id : SV_VertexID, uint inst : SV_InstanceID)
			{
				GS_INPUT o = (GS_INPUT)0;
				o.pos = buf_Points[id];
				o.color = fixed4(buf_Colors[id],1);
				return o;
			}

			[maxvertexcount(4)]
			void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
			{
				float dist = distance(p[0].pos, _WorldSpaceCameraPos);
				float _Size = _MinSize + (dist-_MinDist)*(_MaxSize-_MinSize)/(_MaxDist-_MinDist);

				float3 cameraUp = UNITY_MATRIX_IT_MV[1].xyz;
				float3 cameraForward = _WorldSpaceCameraPos - p[0].pos;
				float3 rightSize = normalize(cross(cameraUp, cameraForward))*_Size;
				float3 cameraSize = _Size * cameraUp;

				FS_INPUT newVert;
				newVert.pos = UnityObjectToClipPos(float4(p[0].pos + rightSize - cameraSize,1));
				newVert.color = p[0].color;
				triStream.Append(newVert);
				newVert.pos =  UnityObjectToClipPos(float4(p[0].pos + rightSize + cameraSize,1));
				newVert.color = p[0].color;
				triStream.Append(newVert);
				newVert.pos =  UnityObjectToClipPos(float4(p[0].pos - rightSize - cameraSize,1));
				newVert.color = p[0].color;
				triStream.Append(newVert);
				newVert.pos =  UnityObjectToClipPos(float4(p[0].pos - rightSize + cameraSize,1));
				newVert.color = p[0].color;
				triStream.Append(newVert);										
			}

			fixed4 FS_Main(FS_INPUT input) : COLOR
			{
				return input.color;
			}
			ENDCG
		}
	} 
}