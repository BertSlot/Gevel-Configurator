Shader "Custom/PointMeshSizeDX11Box" 
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
				fixed4 color 	: COLOR;
			};

			float _Size;
	        fixed4 _Color;

			GS_INPUT VS_Main(appdata v)
			{
				GS_INPUT o = (GS_INPUT)0;
				o.pos = v.vertex;
				fixed4 col = v.color;
				#if !UNITY_COLORSPACE_GAMMA
				col = col*col; // linear
				#endif
				o.color = col*_Color;
				return o;
			}

			[maxvertexcount(24)]
			void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
			{
				float width = _Size;//*(_ScreenParams.z-1);
				float height = _Size;//*(_ScreenParams.w-1);
				float4 vertPos = p[0].pos;
				FS_INPUT newVert;
				// front face 4
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-width,-height,_Size,0));
				newVert.color = p[0].color;
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(width,-height,_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-width,height,_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(width,height,_Size,0));
				triStream.Append(newVert);
				//triStream.RestartStrip();

				// back face 8
				newVert.pos = UnityObjectToClipPos(vertPos + float4(width,height,-_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(width,-height,-_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-width,height,-_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-width,-height,-_Size,0));
				triStream.Append(newVert);
				//triStream.RestartStrip();

				// left face 12
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,-width,-height,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,width,-height,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,-width,height,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,width,height,0));
				triStream.Append(newVert);
				//triStream.RestartStrip();

				// right face 16
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,width,height,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,width,-height,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,-width,height,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,-width,-height,0));
				triStream.Append(newVert);
				//triStream.RestartStrip();

				// top face 20
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,_Size,_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,_Size,-_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,_Size,_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,_Size,-_Size,0));
				triStream.Append(newVert);
				//triStream.RestartStrip();

				// bottom face 24
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,-_Size,-_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,-_Size,-_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,-_Size,_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,-_Size,_Size,0));
				triStream.Append(newVert);
				triStream.RestartStrip();

			}

			fixed4 FS_Main(FS_INPUT input) : COLOR
			{
				return input.color;
			}
			ENDCG
		}
	} 
}
