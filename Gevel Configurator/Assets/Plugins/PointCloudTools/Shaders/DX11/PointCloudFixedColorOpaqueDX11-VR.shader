// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

// UnityCoder.com
// PointCloud Shader for DX11 Viewer with fixed color (opaque) for VR singlepass
Shader "UnityCoder/PointCloud/DX11/FixedColorOpaque-VR"
{
	Properties 
	{
		_Color ("Main Color", Color) = (0,1,0,1)
	}

	SubShader 
	{
		Tags { "RenderType"="Opaque"}
		Pass 
		{
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"

			fixed4 _Color;

			UNITY_INSTANCING_BUFFER_START(Props)
			StructuredBuffer<half3> buf_Points;
			UNITY_INSTANCING_BUFFER_END(Props)

			struct appdata
			{
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f {
				half4 vertex : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert (appdata input, uint id : SV_VertexID)
			{ 
				v2f o;
				UNITY_SETUP_INSTANCE_ID (input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(buf_Points[id]);
				return o;
			}

			fixed4 frag (v2f i) : COLOR
			{
				return _Color;
			}
			ENDCG
		}
	}
	Fallback Off
}