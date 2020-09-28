Shader "Custom/PointMeshSizeDX11TextureMask" 
{
	Properties 
	{
		_Mask ("Mask", 2D) = "white" {}
		_Cutoff ("Alpha cutoff", Range(0,1)) = 0.66
	    _Color ("ColorTint", Color) = (1,1,1,1)
		_Size ("Size", Float) = 30
	}

	SubShader 
	{
		Pass
		{
			Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
		
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

			sampler2D _Mask;
			float _Size;
	        fixed4 _Color;
			fixed _Cutoff;

			GS_INPUT VS_Main(appdata v)
			{
				GS_INPUT o = (GS_INPUT)0;
				o.pos = UnityObjectToClipPos(v.vertex);

				fixed4 col = v.color;
				#if !UNITY_COLORSPACE_GAMMA
				col = col*col; // linear
				#endif
				o.color = col*_Color;
				return o;
			}

			[maxvertexcount(4)]
			void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
			{
				float width = _Size*(_ScreenParams.z-1);
				float height = _Size*(_ScreenParams.w-1);
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
			}

			fixed4 FS_Main(FS_INPUT input) : COLOR
			{
				fixed4 col = tex2D(_Mask, input.uv);
				clip(col.a - _Cutoff);
				return input.color;
			}
			ENDCG
		}
	} 
}
