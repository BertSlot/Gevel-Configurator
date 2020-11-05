Shader "UnityCoder/PointCloud/DX11/Uber" 
{
	Properties 
	{
		_Size ("Size", Float) = 1.0
		[KeywordEnum(Off, On)] _Circle ("Circular Point",float) = 0
		[KeywordEnum(Off, On)] _EnableColor ("Enable Tint",float) = 0
		_Tint ("Color Tint", Color) = (0,1,0,1)
		[KeywordEnum(Off, On)] _EnableScaling ("Enable Distance Scaling",float) = 0
		//_Origin ("Origin", vector) = (0,0,0)
		_MinDist ("Min Distance", float) = 0
		_MaxDist ("Max Distance", float) = 100
		_MinSize ("Min Size", float) = 0.1
		_MaxSize ("Max Size", float) = 1.0
	}

	SubShader 
	{
		Pass
		{
			Tags { "Queue" = "Geometry" "RenderType"="Opaque" }
			ZWrite On
			LOD 200
			Cull Off
		
			CGPROGRAM
			#pragma target 4.0
			#pragma vertex VS_Main
			#pragma fragment FS_Main
			#pragma geometry GS_Main
			#pragma multi_compile _CIRCLE_ON _CIRCLE_OFF
			#pragma multi_compile _ENABLECOLOR_ON _ENABLECOLOR_OFF
			#pragma multi_compile _ENABLESCALING_ON _ENABLESCALING_OFF
			#include "UnityCG.cginc"
			#pragma fragmentoption ARB_precision_hint_fastest

			StructuredBuffer<half3> buf_Points;
			StructuredBuffer<fixed3> buf_Colors;

			struct appdata
			{
				fixed3 color : COLOR;
			};
		
			struct GS_INPUT
			{
				float4	pos		: POSITION;
				fixed3 color 	: COLOR;
				#ifdef _ENABLESCALING_ON
				float dist      : TEXCOORD1;
				#endif
			};

			struct FS_INPUT
			{
				float4	pos		: POSITION;
				fixed3 color 	: COLOR;
				#ifdef _CIRCLE_ON
				float2 uv : TEXCOORD0;
				#endif
			};

			float _Size;

			#ifdef _ENABLESCALING_ON
			//float3 _Origin;
			float _MinDist, _MaxDist;
			float _MinSize, _MaxSize;
			#endif


			#ifdef _ENABLECOLOR_ON
			fixed4 _Tint;
			#endif

			GS_INPUT VS_Main(uint id : SV_VertexID, uint inst : SV_InstanceID)
			{
				GS_INPUT o = (GS_INPUT)0;

				float3 pos = buf_Points[id];
				#ifdef _ENABLESCALING_ON
				//pos+=_Origin; // TODO check if issues when transform is moved, since transform.pos gets added to points?
				float dist = distance(buf_Points[id],_WorldSpaceCameraPos);
				o.dist = dist;
				#endif

				o.pos = UnityObjectToClipPos(buf_Points[id]);
				fixed3 col = buf_Colors[id];

				#ifdef _ENABLECOLOR_ON
				col *= _Tint;
				#endif

				#if !UNITY_COLORSPACE_GAMMA
				col = col*col; // linear
				#endif
				o.color = col;
				return o;
			}

			[maxvertexcount(4)]
			void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
			{
				#ifdef _ENABLESCALING_ON
				_Size = _MinSize + (p[0].dist-_MinDist)*(_MaxSize-_MinSize)/(_MaxDist-_MinDist);
				#endif

				float width = _Size*(_ScreenParams.z-1);
				float height = _Size*(_ScreenParams.w-1);
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
				float2 dist = _st-float2(0.5,0.5);
				return 1.-smoothstep(_radius-(_radius*0.01),_radius+(_radius*0.01),dot(dist,dist)*4.0);
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