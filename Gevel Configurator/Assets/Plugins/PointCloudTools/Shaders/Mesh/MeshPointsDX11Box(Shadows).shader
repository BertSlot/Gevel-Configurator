Shader "Custom/PointMeshSizeDX11Box(Shadows)" 
{
	Properties 
	{
	    _Color ("ColorTint", Color) = (1,1,1,1)
		_Size ("Size", Range(0.001, 0.3)) = 0.1
	}

	SubShader 
	{
		Tags { "RenderType"="Opaque" "IgnoreProjector" = "True" "Queue"="Geometry"}
		LOD 200

		Pass
		{
		
			CGPROGRAM
			#pragma vertex VS_Main
			#pragma fragment FS_Main
			#pragma geometry GS_Main
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
			#pragma multi_compile_fog 
            #include "AutoLight.cginc"
			#include "UnityCG.cginc"
			
			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
			};
		
			struct GS_INPUT
			{
				float4	pos		: POSITION;
				fixed4 color 	: COLOR;
                SHADOW_COORDS(1)
				UNITY_FOG_COORDS(2)
			};

			struct FS_INPUT
			{
				float4	pos		: POSITION;
				fixed4 color 	: COLOR;
                SHADOW_COORDS(1)
				UNITY_FOG_COORDS(2)
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
				o.color =col*_Color;
				return o;
			}

			[maxvertexcount(24)]
			void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
			{
				float width = _Size;
				float height = _Size;
				float4 vertPos = p[0].pos;
				FS_INPUT newVert;
				// front face 4
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-width,-height,_Size,0));
				newVert.color = p[0].color;
				UNITY_TRANSFER_FOG(newVert, newVert.pos);
                TRANSFER_SHADOW(newVert);
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(width,-height,_Size,0));
                TRANSFER_SHADOW(newVert);
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-width,height,_Size,0));
                TRANSFER_SHADOW(newVert);
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(width,height,_Size,0));
				UNITY_TRANSFER_FOG(newVert, newVert.pos);
                TRANSFER_SHADOW(newVert);
				triStream.Append(newVert);
				triStream.RestartStrip();

				// back face 8
				newVert.pos = UnityObjectToClipPos(vertPos + float4(width,height,-_Size,0));
				UNITY_TRANSFER_FOG(newVert, newVert.pos);
                TRANSFER_SHADOW(newVert);
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(width,-height,-_Size,0));
                TRANSFER_SHADOW(newVert);
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-width,height,-_Size,0));
                TRANSFER_SHADOW(newVert);
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-width,-height,-_Size,0));
                TRANSFER_SHADOW(newVert);
				triStream.Append(newVert);
				triStream.RestartStrip();

				// left face 12
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,-width,-height,0));
                TRANSFER_SHADOW(newVert);
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,width,-height,0));
                TRANSFER_SHADOW(newVert);
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,-width,height,0));
                TRANSFER_SHADOW(newVert);
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,width,height,0));
                TRANSFER_SHADOW(newVert);
				triStream.Append(newVert);
				triStream.RestartStrip();

				// right face 16
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,width,height,0));
                TRANSFER_SHADOW(newVert);
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,width,-height,0));
                TRANSFER_SHADOW(newVert);
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,-width,height,0));
                TRANSFER_SHADOW(newVert);
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,-width,-height,0));
                TRANSFER_SHADOW(newVert);
				triStream.Append(newVert);
				triStream.RestartStrip();

				// top face 20
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,_Size,_Size,0));
                TRANSFER_SHADOW(newVert);
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,_Size,-_Size,0));
                TRANSFER_SHADOW(newVert);
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,_Size,_Size,0));
                TRANSFER_SHADOW(newVert);
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,_Size,-_Size,0));
                TRANSFER_SHADOW(newVert);
				triStream.Append(newVert);
				triStream.RestartStrip();

				// bottom face 24
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,-_Size,-_Size,0));
                TRANSFER_SHADOW(newVert);
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,-_Size,-_Size,0));
                TRANSFER_SHADOW(newVert);
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,-_Size,_Size,0));
                TRANSFER_SHADOW(newVert);
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,-_Size,_Size,0));
                TRANSFER_SHADOW(newVert);
				triStream.Append(newVert);
				triStream.RestartStrip();

			}

			fixed4 FS_Main(FS_INPUT input) : COLOR
			{
				fixed4 col = input.color;
                col *= SHADOW_ATTENUATION(input);
				UNITY_APPLY_FOG(input.fogCoord, col);
				return col;			
			}
			ENDCG
		} // pass

		// shadow pass
		/*
		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile_instancing // allow instanced shadow pass for most of the shaders
			#include "UnityCG.cginc"

			struct v2f { 
				V2F_SHADOW_CASTER;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert( appdata_base v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}

			float4 frag( v2f i ) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		} // pass
		*/

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
				o.pos = v.vertex;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				//TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}

			[maxvertexcount(24)]
			void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
			{
				float width = _Size;
				float height = _Size;
				float4 vertPos = p[0].pos;
				FS_INPUT newVert;
				// front face 4
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-width,-height,_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(width,-height,_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-width,height,_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(width,height,_Size,0));
				triStream.Append(newVert);
				triStream.RestartStrip();

				// back face 8
				newVert.pos = UnityObjectToClipPos(vertPos + float4(width,height,-_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(width,-height,-_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-width,height,-_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-width,-height,-_Size,0));
				triStream.Append(newVert);
				triStream.RestartStrip();

				// left face 12
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,-width,-height,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,width,-height,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,-width,height,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,width,height,0));
				triStream.Append(newVert);
				triStream.RestartStrip();

				// right face 16
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,width,height,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,width,-height,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,-width,height,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,-width,-height,0));
				triStream.Append(newVert);
				triStream.RestartStrip();

				// top face 20
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,_Size,_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(_Size,_Size,-_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,_Size,_Size,0));
				triStream.Append(newVert);
				newVert.pos = UnityObjectToClipPos(vertPos + float4(-_Size,_Size,-_Size,0));
				triStream.Append(newVert);
				triStream.RestartStrip();

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

			float4 FS_Main(FS_INPUT input) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(input)
			}
			ENDCG
		} // pass


	}  // subshader
} // shader
