// UnityCoder.com

Shader "UnityCoder/PointCloud/DX11/HeightGradientColor-Opaque"
{
	Properties {
		_ColorTop ("Top Color", Color) = (1,0,0,1)
		_ColorBottom ("Bottom Color", Color) = (0,1,0,1)
		_MaxY("Maximum Y", float) = 50
		_MinY("Minimum Y", float) = 0
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

			StructuredBuffer<half3> buf_Points;

			fixed4 _ColorTop;
			fixed4 _ColorBottom;
			float _MaxY;
			float _MinY;

			struct ps_input {
				half4 pos : SV_POSITION;
				fixed4 color : COLOR;
			};

			ps_input vert (uint id : SV_VertexID, uint inst : SV_InstanceID)
			{
				ps_input o;
				half3 worldPos = buf_Points[id];
				o.pos = mul (UNITY_MATRIX_VP, half4(worldPos,1.0f));
				o.color = lerp(_ColorBottom, _ColorTop, (worldPos.y-_MinY)/(_MaxY-_MinY));
				return o;
			}

			float4 frag (ps_input i) : COLOR
			{
				return i.color;
			}
			ENDCG
		}
	}
	Fallback Off
}