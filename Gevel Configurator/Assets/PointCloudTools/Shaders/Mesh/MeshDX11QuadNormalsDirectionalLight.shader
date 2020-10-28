Shader "UnityCoder/Mesh/MeshQuad-Normals-DirectionalLight" 
{
	Properties {
			_Size ("Size", Range(0.001, 1)) = 0.1
	}

	SubShader 
	{ 
		Tags { "Queue" = "Geometry" "RenderType"="Opaque" }
		LOD 200
		
		Pass 
		{
			Tags { "LightMode" = "ForwardBase" }
			Name "ForwardBase"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geom
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap
			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"
		
			float _Size;

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float3 normal : NORMAL;
			};

			struct GS_INPUT
			{
				float4	pos		: POSITION;
				fixed4 color 	: COLOR;
			};

			struct FS_INPUT {
				float4 pos : SV_POSITION;
				fixed4 color : COLOR;
				UNITY_VERTEX_OUTPUT_STEREO
			};


			GS_INPUT vert (appdata v) 
			{
				GS_INPUT o = (GS_INPUT)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.pos = v.vertex;//mul(unity_ObjectToWorld, v.vertex);
				half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
                fixed4 light = nl * _LightColor0;
				fixed4 col = v.color;
				#if !UNITY_COLORSPACE_GAMMA
				col = col*col; // linear
				#endif
				o.color = col*light;
				return o;
			}

			[maxvertexcount(4)]
			void geom(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
			{
				float3 cameraUp = UNITY_MATRIX_IT_MV[1].xyz;
				float3 cameraForward = _WorldSpaceCameraPos - p[0].pos;
				float3 right = normalize(cross(cameraUp, cameraForward));
				float4 v[4];
				v[0] = float4(p[0].pos + _Size * right - _Size * cameraUp, 1.0f);
				v[1] = float4(p[0].pos + _Size * right + _Size * cameraUp, 1.0f);
				v[2] = float4(p[0].pos - _Size * right - _Size * cameraUp, 1.0f);
				v[3] = float4(p[0].pos - _Size * right + _Size * cameraUp, 1.0f);
				FS_INPUT newVert;
				newVert.pos = UnityObjectToClipPos(v[0]);
				newVert.color = p[0].color;
				triStream.Append(newVert);
				newVert.pos =  UnityObjectToClipPos(v[1]);
				newVert.color = p[0].color;
				triStream.Append(newVert);
				newVert.pos =  UnityObjectToClipPos(v[2]);
				newVert.color = p[0].color;
				triStream.Append(newVert);
				newVert.pos =  UnityObjectToClipPos(v[3]);
				newVert.color = p[0].color;
				triStream.Append(newVert);
				//triStream.RestartStrip();
			}

			fixed4 frag (FS_INPUT IN) : SV_Target
			{
				return IN.color;
			}

		ENDCG
		}
	}
}
