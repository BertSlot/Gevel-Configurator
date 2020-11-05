Shader "UnityCoder/Mesh/MeshQuadAligned-Normals" 
{
	Properties {
			_Size ("Size", Range(0.001, 1)) = 0.1
			_ScaleDistance ("Distance Scaler", float) = 1
			_MinSize ("Minimum Size", float) = 0.8
			_MaxSize ("Maximum Size", float) = 10
	}

	SubShader 
	{ 
		Tags { "Queue" = "Geometry" "RenderType"="Opaque" }
		LOD 200
		Cull Off

		Pass 
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geom
			#include "UnityCG.cginc"
		
			float _Size;
			float _MinSize;
			float _MaxSize;
			float _ScaleDistance;

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float3 normal : NORMAL;
			};

			struct GS_INPUT
			{
				float4	pos	: POSITION;
				fixed4 color : COLOR; // .a is distance
				half3 normal : NORMAL;
			};

			struct FS_INPUT {
				float4 pos : SV_POSITION;
				fixed4 color : COLOR0;
				UNITY_VERTEX_OUTPUT_STEREO
			};


			GS_INPUT vert (appdata v) 
			{
				GS_INPUT o = (GS_INPUT)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.pos = mul(unity_ObjectToWorld, v.vertex);
				half3 worldNormal = UnityObjectToWorldNormal(v.normal);
				float depth = -UnityObjectToViewPos(v.vertex.xyz).z;
				float distance = (depth - _ProjectionParams.y)/_ScaleDistance;
				v.color.a = clamp(distance,_MinSize,_MaxSize);
				fixed4 col = v.color;
				#if !UNITY_COLORSPACE_GAMMA
				col = col*col; // linear
				#endif
				o.color = col;
				o.normal = worldNormal;
				return o;
			}

			[maxvertexcount(4)]
			void geom(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
			{
				float3 worldUp = float3(0,0,1);
				float3 normalDirection = p[0].normal;
				float3 perpendicular = normalize(cross(worldUp, normalDirection));

				_Size = _Size*p[0].color.a;
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

			fixed4 frag (FS_INPUT IN) : SV_Target
			{
				return IN.color;
			}

		ENDCG
		}
	}
}
