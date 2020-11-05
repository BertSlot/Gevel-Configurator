Shader "UnityCoder/PointCloud/DX11/ColorSizeV2-Gradient-By-Distance-Multiplied" 
{
	Properties 
	{
		_GradientMap ("Intensity Gradient", 2D) = "white" {}
		_GradientDistance ("Distance Gradient", 2D) = "white" {}
		_Size ("Size", Range(0.001, 0.5)) = 0.1
		_Origin ("Origin", vector) = (0,0,0)
		_Repeat ("Repeat Multiplier", float) = 0.05
		_GradientMultiplier ("Gradient Multiplier", float) = 4 // makes gradient color show through distance gradient
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

			sampler2D _GradientMap;
			sampler2D _GradientDistance;
			float _Size;
			float _Repeat;//, _MaxDist;
			float3 _Origin;
			float _GradientMultiplier;

			GS_INPUT VS_Main(uint id : SV_VertexID, uint inst : SV_InstanceID)
			{
				GS_INPUT o = (GS_INPUT)0;
				o.pos = buf_Points[id];
				float dist = distance(buf_Points[id],_Origin);

				fixed3 col = buf_Colors[id];
				#if !UNITY_COLORSPACE_GAMMA
				col = col*col; // linear
				#endif

				float4 gradcolor = tex2Dlod(_GradientMap, float4(col[0],0.5f,0,0));
				float4 distcolor = tex2Dlod(_GradientDistance, float4(dist*_Repeat,0.5f,0,0));
				o.color = lerp(distcolor,gradcolor,col[0]*_GradientMultiplier);
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