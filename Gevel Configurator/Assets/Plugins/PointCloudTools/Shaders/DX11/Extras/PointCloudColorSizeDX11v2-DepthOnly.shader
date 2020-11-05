Shader "UnityCoder/PointCloud/DX11/ColorSizeV2-DepthOnly" 
{
	Properties 
	{
		_Size ("Size", Float) = 0.01
	}

	SubShader 
	{
		Tags { "Queue" = "Geometry" }
		LOD 200

		Pass
		{
//			ZWrite On
			ColorMask 0
		
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex VS_Main
			#pragma fragment FS_Main
			#pragma geometry GS_Main
			#include "UnityCG.cginc"
			StructuredBuffer<half3> buf_Points;
//			StructuredBuffer<fixed3> buf_Colors;
			struct appdata
			{
				half4 vertex : POSITION;
			};
		
			struct GS_INPUT
			{
				half3	pos		: TEXCOORD0;
			};

			struct FS_INPUT
			{
				half4	pos		: POSITION;
			};

			float _Size;

			GS_INPUT VS_Main(uint id : SV_VertexID, uint inst : SV_InstanceID)
			{
				GS_INPUT o = (GS_INPUT)0;
				o.pos = buf_Points[id];
				return o;
			}

			[maxvertexcount(4)]
			void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
			{
				float3 cameraUp = UNITY_MATRIX_IT_MV[1].xyz;
				float3 cameraForward = _WorldSpaceCameraPos - p[0].pos;
				float3 rightSize = normalize(cross(cameraUp, cameraForward))*_Size;
				float3 cameraSize = _Size * cameraUp;

				FS_INPUT newVert;
				newVert.pos = UnityObjectToClipPos(float4(p[0].pos + rightSize - cameraSize,1));
				triStream.Append(newVert);
				newVert.pos =  UnityObjectToClipPos(float4(p[0].pos + rightSize + cameraSize,1));
				triStream.Append(newVert);
				newVert.pos =  UnityObjectToClipPos(float4(p[0].pos - rightSize - cameraSize,1));
				triStream.Append(newVert);
				newVert.pos =  UnityObjectToClipPos(float4(p[0].pos - rightSize + cameraSize,1));
				triStream.Append(newVert);										
			}

			half4 FS_Main(FS_INPUT input) : COLOR
			{
				return half4(0,0,0,0);
			}
			ENDCG
		}
	} 
}