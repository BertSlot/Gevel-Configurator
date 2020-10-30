Shader "UnityCoder/Mesh/MeshQuadAligned-Normals-DirectionalLight" 
{
	Properties {
			_Size ("Size", Range(0.001, 1)) = 0.1
	}

	SubShader 
	{ 
		Tags { "Queue" = "Geometry" "RenderType"="Opaque" }
		LOD 200
		// NOTE you can disable culling if want to show backfacing points also
		Cull Off

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
				float3 color : COLOR;
				float3 normal : NORMAL;
			};

			struct GS_INPUT
			{
				float4	pos	: POSITION;
				fixed3 color : COLOR;
				half3 normal : NORMAL;
			};

			struct FS_INPUT {
				float4 pos : SV_POSITION;
				fixed3 color : COLOR0;
				UNITY_VERTEX_OUTPUT_STEREO
			};


			GS_INPUT vert (appdata v) 
			{
				GS_INPUT o = (GS_INPUT)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.pos = v.vertex;
				half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
                fixed3 light = nl * _LightColor0.rgb;
				
				fixed3 col = v.color;
				#if !UNITY_COLORSPACE_GAMMA
				col = col*col; // linear
				#endif

				o.color = col*light;
				//o.normal = worldNormal;
				o.normal = v.normal;

				return o;
			}

			[maxvertexcount(4)]
			void geom(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
			{
				float3 worldUp = normalize(cross(float3(0,0,1),p[0].normal));
				float3 normalDirection = p[0].normal;
				float3 perpendicular = normalize(cross(worldUp, normalDirection));

				float3 worldUpSize = _Size * worldUp;
				float3 perpSize = _Size * perpendicular;

				float4 v[4];
				v[0] = float4(p[0].pos + perpSize - worldUpSize, 1.0f);
				v[1] = float4(p[0].pos + perpSize + worldUpSize, 1.0f);
				v[2] = float4(p[0].pos - perpSize - worldUpSize, 1.0f);
				v[3] = float4(p[0].pos - perpSize + worldUpSize, 1.0f);

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
			}

			fixed3 frag (FS_INPUT IN) : SV_Target
			{
				return IN.color;
			}

		ENDCG
		}
	}
}
