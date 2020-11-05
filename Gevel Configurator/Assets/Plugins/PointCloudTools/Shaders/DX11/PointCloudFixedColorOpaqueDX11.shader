// UnityCoder.com
// PointCloud Shader for DX11 Viewer with fixed color (opaque)
Shader "UnityCoder/PointCloud/DX11/FixedColorOpaque"
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
			#include "UnityCG.cginc"

			fixed4 _Color;
			StructuredBuffer<half3> buf_Points;

			struct ps_input 
			{
				half4 pos : SV_POSITION;
			};

			ps_input vert (uint id : SV_VertexID, uint inst : SV_InstanceID)
			{
				ps_input o;
				half3 worldPos = buf_Points[id];
				o.pos = mul (UNITY_MATRIX_VP, half4(worldPos,1.0f));
				return o;
			}

			float4 frag (ps_input i) : COLOR
			{
				return _Color;
			}
			ENDCG
		}
	}
	Fallback Off
}