// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/PointMeshSizeDX11VertexID" 
{
	Properties 
	{
	    _Color ("ColorTint", Color) = (1,1,1,1)
		_Size ("Size", Float) = 30
	}

	SubShader 
	{
		Pass
		{
			Tags { "RenderType"="Opaque" }
			//Cull Off
			LOD 200
		
			CGPROGRAM
			#pragma vertex VS_Main
			#pragma fragment FS_Main
			#pragma geometry GS_Main
			
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
				float2 uv : TEXCOORD0;
				fixed4 color 	: COLOR;
			};

			float _Size;
	        fixed4 _Color;

			GS_INPUT VS_Main(appdata v, uint i:SV_VertexID)
			{
				GS_INPUT o;// = (GS_INPUT)0;
				//v.vertex += float4(1, 1, 1, 1);
				//o.pos = UnityObjectToClipPos(v.vertex);
				o.pos = UnityObjectToClipPos(v.vertex);
				fixed4 col = v.color;
				#if !UNITY_COLORSPACE_GAMMA
				col = col*col; // linear
				#endif
				o.color = col*_Color;
				return o;
			}

			// source https://thebookofshaders.com/07/
			float circle(float2 _st, float _radius)
			{
				float2 dist = _st-float2(0.5,0.5);
				return 1.-smoothstep(_radius-(_radius*0.01),_radius+(_radius*0.01),dot(dist,dist)*4.0);
			}

			[maxvertexcount(4)]
			void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
			{
				float width = _Size; //*(_ScreenParams.z - 1);
				float height = _Size;// *(_ScreenParams.w - 1);
				float4 vertPos = p[0].pos;
				FS_INPUT newVert;
				newVert.pos = vertPos + float4(-width,-height,0,0);
				newVert.color = p[0].color;
				newVert.uv = float2(0,0);
				triStream.Append(newVert);
				newVert.pos = vertPos + float4(width,-height,0,0);
				newVert.uv = float2(1,0);
				triStream.Append(newVert);
				newVert.pos = vertPos + float4(-width,height,0,0);
				newVert.uv = float2(0,1);
				triStream.Append(newVert);
				newVert.pos = vertPos + float4(width,height,0,0);
				newVert.uv = float2(1,1);
				triStream.Append(newVert);
				//triStream.RestartStrip();
			}

			fixed4 FS_Main(FS_INPUT input) : COLOR
			{
				//clip(circle(input.uv,0.9) - 0.999);
				return input.color;
			}
			ENDCG
		}
	} 
}
