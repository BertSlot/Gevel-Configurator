Shader "UnityCoder/PointCloud/Mesh/ColorSize-Billboard"
{
    Properties 
    {
        _PointSize("PointSize", Float) = 0.01
    }

	SubShader
	{
		Tags { "Queue"="Geometry"}
		Lighting Off
		Fog { Mode Off }
		
		// regular pass
		Pass
		{
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex VS_Main
			#pragma fragment FS_Main
			#pragma geometry GS_Main
			#include "UnityCG.cginc"
			#pragma fragmentoption ARB_precision_hint_fastest
			
			struct appdata
			{
				float4 vertex : POSITION;
				fixed4 color : COLOR;
			};
		
			struct GS_INPUT
			{
				float4	pos		: POSITION;
				fixed3 color 	: COLOR;
			};

			struct FS_INPUT
			{
				float4	pos		: SV_POSITION;
				fixed3 color 	: COLOR;
			};
			
			float _PointSize;
			
			GS_INPUT VS_Main(appdata v)
			{
				GS_INPUT o = (GS_INPUT)0;
				o.pos = UnityObjectToClipPos(v.vertex);

				fixed4 col = v.color;
				#if !UNITY_COLORSPACE_GAMMA
				col = col*col; // linear
				#endif
				o.color = col;
				return o;
			}


			[maxvertexcount(4)]
			void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
			{
				float width = _PointSize*(_ScreenParams.z-1);
				float height = _PointSize*(_ScreenParams.w-1);

				FS_INPUT newVert;
				newVert.pos = p[0].pos + float4(-width,-height,0,0);
				newVert.color = p[0].color;
				triStream.Append(newVert);
				newVert.pos = p[0].pos + float4(width,-height,0,0);
				newVert.color = p[0].color;
				triStream.Append(newVert);
				newVert.pos = p[0].pos + float4(-width,height,0,0);
				newVert.color = p[0].color;
				triStream.Append(newVert);
				newVert.pos = p[0].pos + float4(width,height,0,0);
				newVert.color = p[0].color;
				triStream.Append(newVert);			
				triStream.RestartStrip();
			}

			fixed4 FS_Main(FS_INPUT input) : SV_Target
			{
				return half4(input.color,1);
			}

			ENDCG
		} // regular pass

		// shadow pass
		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			CGPROGRAM
			#pragma vertex VS_Main
			#pragma fragment FS_Main
			#pragma geometry GS_Main
			#pragma multi_compile_shadowcaster
			#pragma multi_compile_instancing // allow instanced shadow pass for most of the shaders
			#include "UnityCG.cginc"

			float _PointSize;

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct GS_INPUT
			{
				V2F_SHADOW_CASTER;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			struct FS_INPUT
			{
				V2F_SHADOW_CASTER;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			float _Size;

			GS_INPUT VS_Main(appdata v)
			{
				GS_INPUT o = (GS_INPUT)0;
				o.pos = UnityObjectToClipPos(v.vertex);
				return o;
			}

			[maxvertexcount(4)]
			void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
			{
				float width = _PointSize * (_ScreenParams.z - 1);
				float height = _PointSize * (_ScreenParams.w - 1);

				FS_INPUT newVert;
				newVert.pos = p[0].pos + float4(-width, -height, 0, 0);
				triStream.Append(newVert);
				newVert.pos = p[0].pos + float4(width, -height, 0, 0);
				triStream.Append(newVert);
				newVert.pos = p[0].pos + float4(-width, height, 0, 0);
				triStream.Append(newVert);
				newVert.pos = p[0].pos + float4(width, height, 0, 0);
				triStream.Append(newVert);
				triStream.RestartStrip();
			}

			float4 FS_Main(FS_INPUT input) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(input)
			}
			ENDCG
		} // shadow pass

	} // subshader
} // shader