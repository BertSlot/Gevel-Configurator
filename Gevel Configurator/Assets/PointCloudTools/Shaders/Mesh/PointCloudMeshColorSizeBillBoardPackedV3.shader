Shader "UnityCoder/PointCloud/Mesh/ColorSize-Billboard-PackedV3"
{
    Properties 
    {
        _PointSize("PointSize", Float) = 0.01
		_Offset("Offset", Vector) = (0,0,0,0)
		_GridSizeAndPackMagic("GridSizeAndPackMagic", Float) = 1
    }

	SubShader
	{
		Tags { "Queue"="Geometry"}
		Lighting Off
		Fog { Mode Off }
		Cull Off

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
				//fixed4 color : COLOR;
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
			float _GridSizeAndPackMagic;
			float4 _Offset;

			float2 SuperUnpacker(float f)
			{
				return float2(f - floor(f), floor(f) / _GridSizeAndPackMagic);
			}

			GS_INPUT VS_Main(appdata v)
			{
				GS_INPUT o = (GS_INPUT)0;

				float3 rawpos = v.vertex;
				float2 xr = SuperUnpacker(rawpos.x);
				float2 yg = SuperUnpacker(rawpos.y);
				float2 zb = SuperUnpacker(rawpos.z);
				rawpos = float3(xr.y + _Offset.x, yg.y + _Offset.y, zb.y + _Offset.z);

				o.pos = UnityObjectToClipPos(rawpos);

				fixed3 col = fixed3(saturate(xr.x), saturate(yg.x), saturate(zb.x)) * 1.02; // restore colors a bit
				#if !UNITY_COLORSPACE_GAMMA
				col = col * col; // linear
				#endif
				o.color = col;
				return o;
			}

			[maxvertexcount(4)]
			void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
			{
				float width = _PointSize; //*(_ScreenParams.z - 1);
				float height = _PointSize;// *(_ScreenParams.w - 1);

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

			fixed3 FS_Main(FS_INPUT input) : COLOR
			{
				return input.color;
			}

			ENDCG
		} // regular pass

		//// shadow pass
		//Pass
		//{
		//	Name "ShadowCaster"
		//	Tags { "LightMode" = "ShadowCaster" }

		//	CGPROGRAM
		//	#pragma vertex VS_Main
		//	#pragma fragment FS_Main
		//	#pragma geometry GS_Main
		//	#pragma multi_compile_shadowcaster
		//	#pragma multi_compile_instancing // allow instanced shadow pass for most of the shaders
		//	#include "UnityCG.cginc"

		//	float _PointSize;

		//	struct appdata
		//	{
		//		float4 vertex : POSITION;
		//	};

		//	struct GS_INPUT
		//	{
		//		V2F_SHADOW_CASTER;
		//		UNITY_VERTEX_OUTPUT_STEREO
		//	};

		//	struct FS_INPUT
		//	{
		//		V2F_SHADOW_CASTER;
		//		UNITY_VERTEX_OUTPUT_STEREO
		//	};

		//	float _Size;

		//	GS_INPUT VS_Main(appdata v)
		//	{
		//		GS_INPUT o = (GS_INPUT)0;
		//		o.pos = UnityObjectToClipPos(v.vertex);
		//		return o;
		//	}

		//	[maxvertexcount(4)]
		//	void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
		//	{
		//		float width = _PointSize * (_ScreenParams.z - 1);
		//		float height = _PointSize * (_ScreenParams.w - 1);

		//		FS_INPUT newVert;
		//		newVert.pos = p[0].pos + float4(-width, -height, 0, 0);
		//		triStream.Append(newVert);
		//		newVert.pos = p[0].pos + float4(width, -height, 0, 0);
		//		triStream.Append(newVert);
		//		newVert.pos = p[0].pos + float4(-width, height, 0, 0);
		//		triStream.Append(newVert);
		//		newVert.pos = p[0].pos + float4(width, height, 0, 0);
		//		triStream.Append(newVert);
		//		triStream.RestartStrip();
		//	}

		//	float4 FS_Main(FS_INPUT input) : SV_Target
		//	{
		//		SHADOW_CASTER_FRAGMENT(input)
		//	}
		//	ENDCG
		//} // shadow pass

	} // subshader
} // shader