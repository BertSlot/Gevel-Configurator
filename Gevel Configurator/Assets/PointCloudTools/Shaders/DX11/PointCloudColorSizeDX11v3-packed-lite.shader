// use with packed color data in v3 tiles viewer

Shader "UnityCoder/PointCloud/DX11/ColorSizeV3-packed-lite"
{
	Properties
	{
		_Size("Size", Float) = 1.0
		_Offset("Offset", Vector) = (0,0,0,0)
		[KeywordEnum(Off, On)] _Square("Force Square",float) = 0
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
			#pragma multi_compile _SQUARE_ON _SQUARE_OFF
			#include "UnityCG.cginc"
			#pragma fragmentoption ARB_precision_hint_fastest

			StructuredBuffer<half3> buf_Points;

			struct appdata
			{
				//fixed3 color : COLOR;
			};

			struct GS_INPUT
			{
				uint id : VERTEXID;
			};

			struct FS_INPUT
			{
				half4 pos : POSITION;
				fixed3 color : COLOR;
				#ifdef _SQUARE_ON
				float2 uv : TEXCOORD0;
				#endif
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

			[maxvertexcount(3)]
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
				
				#ifdef _SQUARE_ON
				// top left
				newVert.pos = UnityObjectToClipPos(float4(p + rightSize + cameraSize, 1));
				newVert.uv = float2(0, 1);
				#else
				// top middle
				newVert.pos = UnityObjectToClipPos(float4(p + cameraSize, 1));
				#endif
				newVert.color = col;
				triStream.Append(newVert);

				// bottom right
				newVert.pos = UnityObjectToClipPos(float4(p - rightSize - cameraSize, 1));
#ifdef _SQUARE_ON
				newVert.uv = float2(1, 0);
#endif
				triStream.Append(newVert);

				// bottom left
				newVert.pos = UnityObjectToClipPos(float4(p + rightSize - cameraSize, 1));
#ifdef _SQUARE_ON
				newVert.uv = float2(0, 0);
#endif
				triStream.Append(newVert);
			}

			fixed3 FS_Main(FS_INPUT i) : SV_Target
			{
#ifdef _SQUARE_ON
				clip(-step(0.5,i.uv.x)- step(0.5,i.uv.y));
#endif
				return i.color;
			}
			ENDCG
		}
	}
}