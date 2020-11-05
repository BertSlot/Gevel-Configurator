// UnityCoder.com

Shader "UnityCoder/PointCloud/DX11/HeightGradientTexture-Opaque"
{
	Properties {
		_GradientMap ("Gradient Texture", 2D) = "white" {}
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

			uniform sampler2D _GradientMap;
			uniform float _MaxY;
			uniform float _MinY;

			struct ps_input {
				half4 pos : SV_POSITION;
				fixed4 color : COLOR;
			};

			ps_input vert (uint id : SV_VertexID, uint inst : SV_InstanceID)
			{
				ps_input o;
				half3 worldPos = buf_Points[id];
				o.pos = mul (UNITY_MATRIX_VP, half4(worldPos,1.0f));
				float v = (worldPos.y-_MinY)/(_MaxY-_MinY);
				o.color = tex2Dlod(_GradientMap, float4(v,0.5f,0,0));
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