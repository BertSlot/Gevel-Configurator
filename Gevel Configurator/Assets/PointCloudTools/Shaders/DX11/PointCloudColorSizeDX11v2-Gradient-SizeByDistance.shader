// PointCloud Shader for DX11 Viewer with PointSize and ColorTint (version 2, less math)

Shader "UnityCoder/PointCloud/DX11/Gradient-SizeByDistance" 
{
	Properties 
	{
		_GradientMap ("Gradient Texture", 2D) = "white" {}
		_Origin ("Origin", vector) = (0,0,0)
		_MinDist ("Min Distance", float) = 0
		_MaxDist ("Max Distance", float) = 100
		_MinSize ("Min Size", float) = 0.01
		_MaxSize ("Max Size", float) = 0.1
		[KeywordEnum(Off, On)] _Circle ("Circular Point",float) = 1
		[KeywordEnum(Off, On)] _Filter ("Distance Filter",float) = 1
		_FilterDistance ("Filtering Distance", float) = 0
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
			#pragma multi_compile _CIRCLE_ON _CIRCLE_OFF
			#pragma multi_compile _FILTER_ON _FILTER_OFF
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
				#ifdef _CIRCLE_ON
				float2 uv : TEXCOORD0;
				#endif
			};

			sampler2D _GradientMap;
			float _MinDist, _MaxDist;
			float _MinSize, _MaxSize;
			float3 _Origin;
			#ifdef _FILTER_ON
			float _FilterDistance;
			#endif

			GS_INPUT VS_Main(uint id : SV_VertexID, uint inst : SV_InstanceID)
			{
				GS_INPUT o = (GS_INPUT)0;
				o.pos = buf_Points[id];

				fixed3 col = buf_Colors[id];
				#if !UNITY_COLORSPACE_GAMMA
				col = col*col; // linear
				#endif

				o.color = tex2Dlod(_GradientMap, float4(col[0],0.5f,0,0));
				return o;
			}

			[maxvertexcount(4)]
			void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
			{
				float dist = distance(p[0].pos,_Origin);

				#ifdef _FILTER_ON
				if (dist<_FilterDistance) return;
				#endif

				//float dist = length(p[0].pos-_Origin);
				float _Size = _MinSize + (dist-_MinDist)*(_MaxSize-_MinSize)/(_MaxDist-_MinDist);

				float3 cameraUp = UNITY_MATRIX_IT_MV[1].xyz;
				float3 cameraForward = _WorldSpaceCameraPos - p[0].pos;
				float3 rightSize = normalize(cross(cameraUp, cameraForward))*_Size;
				float3 cameraSize = _Size * cameraUp;

				FS_INPUT newVert;
				newVert.pos = UnityObjectToClipPos(float4(p[0].pos + rightSize - cameraSize,1));
				newVert.color = p[0].color;
				#ifdef _CIRCLE_ON
				newVert.uv = float2(0,0);
				#endif
				triStream.Append(newVert);
				newVert.pos =  UnityObjectToClipPos(float4(p[0].pos + rightSize + cameraSize,1));
				newVert.color = p[0].color;
				#ifdef _CIRCLE_ON
				newVert.uv = float2(1,0);
				#endif
				triStream.Append(newVert);
				newVert.pos =  UnityObjectToClipPos(float4(p[0].pos - rightSize - cameraSize,1));
				newVert.color = p[0].color;
				#ifdef _CIRCLE_ON
				newVert.uv = float2(0,1);
				#endif
				triStream.Append(newVert);
				newVert.pos =  UnityObjectToClipPos(float4(p[0].pos - rightSize + cameraSize,1));
				newVert.color = p[0].color;
				#ifdef _CIRCLE_ON
				newVert.uv = float2(1,1);
				#endif
				triStream.Append(newVert);										
			}

			#ifdef _CIRCLE_ON
			// source https://thebookofshaders.com/07/
			float circle(float2 _st, float _radius)
			{
				float2 dist = _st-float2(0.5,0.5);
				return 1.-smoothstep(_radius-(_radius*0.01),_radius+(_radius*0.01),dot(dist,dist)*4.0);
			}
			#endif

			fixed4 FS_Main(FS_INPUT input) : COLOR
			{
				fixed4 col = input.color;
				#ifdef _CIRCLE_ON
				clip(circle(input.uv,0.9) - 0.999);
				#endif
				return col;
			}
			ENDCG
		}
	} 
}