// use with packed color data in v3 tiles viewer

Shader "UnityCoder/PointCloud/DX11/ColorSizeV3-packed"
{
	Properties
	{
		_Size("Size", Float) = 1.0
		_Offset("Offset", Vector) = (0,0,0,0)
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
			#include "UnityCG.cginc"
			#pragma fragmentoption ARB_precision_hint_fastest

			StructuredBuffer<half3> buf_Points;

			struct appdata
			{
				fixed3 color : COLOR;
			};

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
			float _GridSizeAndPackMagic;
			float4 _Offset;

			float2 SuperUnpacker(float f)
			{
				return float2(f - floor(f), floor(f) / _GridSizeAndPackMagic);
			}

			GS_INPUT VS_Main(uint id : SV_VertexID, uint inst : SV_InstanceID)
			{
				GS_INPUT o;
				o.id = id;
				return o;
			}

			[maxvertexcount(4)]
			void GS_Main(point GS_INPUT po[1], inout TriangleStream<FS_INPUT> triStream)
			{
				uint id = po[0].id;
				float3 rawpos = buf_Points[id];
				float2 xr = SuperUnpacker(rawpos.x);
				float2 yg = SuperUnpacker(rawpos.y);
				float2 zb = SuperUnpacker(rawpos.z);
				float3 p = float3(xr.y + _Offset.x, yg.y + _Offset.y, zb.y + _Offset.z);

				fixed3 col = fixed3(saturate(xr.x), saturate(yg.x), saturate(zb.x)) * 1.02; // restore colors a bit
				#if !UNITY_COLORSPACE_GAMMA
				col = col * col; // linear
				#endif

				float3 cameraUp = UNITY_MATRIX_IT_MV[1].xyz;
				float3 cameraForward = _WorldSpaceCameraPos - p;
				float3 rightSize = normalize(cross(cameraUp, cameraForward)) * _Size;
				float3 cameraSize = _Size * cameraUp;

				FS_INPUT newVert;
				newVert.pos = UnityObjectToClipPos(float4(p + rightSize - cameraSize, 1));
				newVert.color = col;
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(float4(p + rightSize + cameraSize, 1));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(float4(p - rightSize - cameraSize, 1));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(float4(p - rightSize + cameraSize, 1));
				triStream.Append(newVert);
			}

			fixed3 FS_Main(FS_INPUT input) : SV_Target
			{
				return input.color;
			}
			ENDCG
		}
	}
}