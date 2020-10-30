// UnityCoder.com
// PointCloud Shader for DX11 Viewer with PointSize, ColorTint and GammaFix

Shader "UnityCoder/PointCloud/DX11/ColorSizeLinear"
{
	Properties 
	{
	    _Tint ("Tint", Color) = (1,1,1,1)
		_Size ("Size", Range(0.001, 0.2)) = 0.01
	}

	SubShader 
	{
		Pass
		{
			Tags { "RenderType"="Opaque" }
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
			};

			float _Size;
	        fixed4 _Tint;

			GS_INPUT VS_Main(uint id : SV_VertexID, uint inst : SV_InstanceID)
			{
				GS_INPUT o = (GS_INPUT)0;
				o.pos = mul (unity_ObjectToWorld, half4(buf_Points[id],1.0f));
				#if UNITY_COLORSPACE_GAMMA
				o.color = fixed4(buf_Colors[id],1); // original
				#else
				o.color = fixed4(buf_Colors[id]*buf_Colors[id],1)*_Tint; // linear
				#endif
				return o;
			}

			[maxvertexcount(4)]
			void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
			{
				float3 cameraUp = UNITY_MATRIX_IT_MV[1].xyz;
				float3 cameraForward = _WorldSpaceCameraPos - p[0].pos;
				float3 right = normalize(cross(cameraUp, cameraForward));
				float3 rightSize = _Size * right;
				float3 cameraSize = _Size * cameraUp;

				float4 v[4];
				v[0] = float4(p[0].pos + rightSize - cameraSize, 1.0f);
				v[1] = float4(p[0].pos + rightSize + cameraSize, 1.0f);
				v[2] = float4(p[0].pos - rightSize - cameraSize, 1.0f);
				v[3] = float4(p[0].pos - rightSize + cameraSize, 1.0f);

				FS_INPUT newVert;
				newVert.pos = UnityObjectToClipPos(v[0]);
				newVert.color = p[0].color;
				triStream.Append(newVert);
				newVert.pos =  UnityObjectToClipPos(v[1]);
				newVert.color = p[0].color;
				triStream.Append(newVert);
				newVert.pos =  UnityObjectToClipPos(v[2]);
				newVert.color = p[0].color;
				triStream.Append(newVert);
				newVert.pos =  UnityObjectToClipPos(v[3]);
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