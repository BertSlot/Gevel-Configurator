// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "UnityCoder/PointCloud/DX11/ColorSizeV2-dev-pick"
{
	Properties
	{
		_Tint("Tint", Color) = (1,1,1,1)
		_Size("Size", Float) = 0.01
		_MouseX("_MouseX", Float) = 0
		_MouseY("_MouseY", Float) = 0
	}

		SubShader
	{
		Pass
		{
			Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }
			LOD 200

			CGPROGRAM
			#pragma target 5.0
			#pragma vertex VS_Main
			#pragma fragment FS_Main
			#pragma geometry GS_Main
			#include "UnityCG.cginc"

			StructuredBuffer<half3> buf_Points;
			StructuredBuffer<fixed3> buf_Colors;

			float _MouseX;
			float _MouseY;

			struct GS_INPUT
			{
				uint id : VERTEXID;
			};

			struct FS_INPUT
			{
				half4	posx		: POSITION;
				fixed3 color : COLOR;
				//float2 screenPos : TEXCOORD2;
			};

			float _Size;
			fixed4 _Tint;

			GS_INPUT VS_Main(uint id : SV_VertexID)
			{
				GS_INPUT o;// = (GS_INPUT)0;
				o.id = id;
				return o;
			}

			// this is like new mesh vertices, for next pass?
			//AppendStructuredBuffer<int> selectedPoints : register(u1);

			//RWStructuredBuffer<float2> pointBuffer;
			uniform RWStructuredBuffer<int> data : register(u1);
			// write to RWBuffer directly!
			// https://forum.unity.com/threads/rwstructuredbuffer-in-vertex-shader.406592/#post-3480575

			[maxvertexcount(4)]
			void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
			{
				uint id = p[0].id;
				float3 pos = buf_Points[id];

				float3 cameraUp = UNITY_MATRIX_IT_MV[1].xyz;
				float3 cameraForward = _WorldSpaceCameraPos - pos;
				float3 rightSize = normalize(cross(cameraUp, cameraForward))*_Size;
				float3 cameraSize = _Size * cameraUp;

				fixed3 col = buf_Colors[id] * _Tint.rgb;
				#if !UNITY_COLORSPACE_GAMMA
				col = col * col; // linear
				#endif

				//float4 spos = ComputeScreenPos(UnityObjectToClipPos(float4(pos.xyz, 1)));

				float4 spos = ComputeScreenPos(UnityObjectToClipPos(float4(pos.xyz, 1)));
				spos.xy /= spos.w;
				spos.x *= _ScreenParams.x;
				spos.y *= _ScreenParams.y;
				float d = distance(spos.xy, float2(_MouseX, _MouseY));

				if (d < 1)
				{
					//selectedPoints.Append(id);
					//data[.. insert to rw?
					col = spos.xyz;// fixed3(1, 0, 0);
				}


				FS_INPUT newVert;
				newVert.posx = UnityObjectToClipPos(float4(pos + rightSize - cameraSize,1));
				newVert.color = col;
				//newVert.color = 

				//float dist = distance( (float3(newVert.pos.xy, 0) + 1) / 2), float3(_MouseX, _MouseY, 1));
				//float distX = distance( (newVert.pos.x + 1) / 2, _MouseX);
				//float distY = distance( (newVert.pos.y + 1) / 2, _MouseY);

				//float4 locPos = UnityObjectToClipPos(pos);
				//newVert.screenPos = ComputeScreenPos(newVert.pos);


				//float2 sp = ComputeScreenPos(float4(pos,-1));

				//newVert.color = distX*distY;// float3(_MouseX, _MouseY, 1);
				//newVert.color = float3(locPos.x,0,0);

				triStream.Append(newVert);
				newVert.posx = UnityObjectToClipPos(float4(pos + rightSize + cameraSize,1));
				triStream.Append(newVert);
				newVert.posx = UnityObjectToClipPos(float4(pos - rightSize - cameraSize,1));
				triStream.Append(newVert);
				newVert.posx = UnityObjectToClipPos(float4(pos - rightSize + cameraSize,1));
				triStream.Append(newVert);
			}


			fixed3 FS_Main(FS_INPUT input, UNITY_VPOS_TYPE screenPosReal : POSITION) : COLOR
			{
				//float2 screenPos = floor(screenPosReal.xy * 0.25) * 0.5;
				//float checker = -frac(screenPos.r + screenPos.g);
				//clip(checker);

				//float distX = distance(screenPosReal.x, _MouseX);
				//float distY = distance(screenPosReal.y, _ScreenParams.y-_MouseY);

				float d = distance(screenPosReal.xy, float2(_MouseX, _ScreenParams.y-_MouseY));

				//float4 c = float4(screenPosReal.x / _ScreenParams.x,screenPosReal.y / _ScreenParams.y,0,1);
				float c = step(d,1);

				//return screenPosReal.xyx/1024;
				return lerp(input.color,float4(0,1,0,1),c);
			}

			ENDCG
		} // pass

		//Pass
		//{
		//	ZWrite Off ZTest Always Cull Off Fog { Mode Off }
		//	//Blend SrcAlpha One

		//	CGPROGRAM
		//	#pragma target 5.0

		//	#pragma vertex vert
		//	#pragma geometry geom
		//	#pragma fragment frag

		//	#include "UnityCG.cginc"

		//	StructuredBuffer<float2> selectedPoints;

		//	struct vs_out {
		//		float4 pos : SV_POSITION;
		//	};

		//	vs_out vert(uint id : SV_VertexID)
		//	{
		//		vs_out o;
		//		o.pos = float4(selectedPoints[id] * 2.0 - 1.0, 0, 1);
		//		return o;
		//	}

		//	struct gs_out {
		//		float4 pos : SV_POSITION;
		//		float2 uv : TEXCOORD0;
		//	};

		//	float _Size;

		//	// build quads
		//	[maxvertexcount(4)]
		//	void geom(point vs_out input[1], inout TriangleStream<gs_out> outStream)
		//	{
		//		float dx = _Size;
		//		float dy = _Size * _ScreenParams.x / _ScreenParams.y;
		//		gs_out output;
		//		output.pos = input[0].pos + float4(-dx, dy,0,0);
		//		output.uv = float2(0,0);
		//		outStream.Append(output);
		//		output.pos = input[0].pos + float4(dx, dy,0,0);
		//		output.uv = float2(1,0);
		//		outStream.Append(output);
		//		output.pos = input[0].pos + float4(-dx,-dy,0,0);
		//		output.uv = float2(0,1);
		//		outStream.Append(output);
		//		output.pos = input[0].pos + float4(dx,-dy,0,0);
		//		output.uv = float2(1,1);
		//		outStream.Append(output);
		//		outStream.RestartStrip();
		//	}

		//	//sampler2D _Sprite;
		//	//fixed4 _Color;

		//	fixed4 frag(gs_out i) : COLOR0
		//	{
		//		//fixed4 col = tex2D (_Sprite, i.uv);
		//		return float4(0,0,1,1);
		//	}

		//	ENDCG
		//} // pass
	} // subshader
} // shader