// PointCloud Shader for DX11 Viewer with PointSize and ColorTint

Shader "UnityCoder/PointCloud/DX11/ColorSizeV2-Circle" 
{
	Properties 
	{
	    _Tint ("Tint", Color) = (1,1,1,1)
		_Size ("Size", Float) = 0.01
		[KeywordEnum(Off, On)] _Circle ("Circular Point",float) = 1
		[KeywordEnum(World, Camera)] _Up ("Up Vector",float) = 1
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
			#pragma multi_compile _UP_WORLD _UP_CAMERA
			#include "UnityCG.cginc"
			
			StructuredBuffer<half3> buf_Points;
			StructuredBuffer<fixed3> buf_Colors;

			struct appdata
			{
				fixed3 color : COLOR;
			};
		
			struct GS_INPUT
			{
				half3	pos		: TEXCOORD0;
				fixed3 color 	: COLOR;
			};

			struct FS_INPUT
			{
				half4	pos		: POSITION;
				fixed3 color 	: COLOR;
				#ifdef _CIRCLE_ON
				float2 uv : TEXCOORD0;
				#endif
			};

			float _Size;
	        fixed4 _Tint;

			GS_INPUT VS_Main(uint id : SV_VertexID, uint inst : SV_InstanceID)
			{
				GS_INPUT o = (GS_INPUT)0;
				o.pos = buf_Points[id];

				fixed3 col = buf_Colors[id];
				#if !UNITY_COLORSPACE_GAMMA
				col = col*col; // linear
				#endif

				o.color = col * _Tint.rgb;

				return o;
			}

			[maxvertexcount(4)]
			void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
			{
				float3 cameraUp = UNITY_MATRIX_IT_MV[1].xyz;
				#ifdef _UP_WORLD
				cameraUp = float3(0,1,0);
				#endif

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
				#ifdef _CIRCLE_ON
				newVert.uv = float2(1,0);
				#endif
				triStream.Append(newVert);
				newVert.pos =  UnityObjectToClipPos(float4(p[0].pos - rightSize - cameraSize,1));
				#ifdef _CIRCLE_ON
				newVert.uv = float2(0,1);
				#endif
				triStream.Append(newVert);
				newVert.pos =  UnityObjectToClipPos(float4(p[0].pos - rightSize + cameraSize,1));
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


			fixed3 FS_Main(FS_INPUT input) : COLOR
			{
				#ifdef _CIRCLE_ON
				clip(circle(input.uv,0.9) - 0.999);
				#endif
				return input.color;
			}
			ENDCG
		}
	} 
}