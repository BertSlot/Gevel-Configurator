// NOT working, because structurebuffers
Shader "UnityCoder/PointCloud/WEBGL/ColorSize"
{
	Properties
	{
		_PointSize("PointSize", Float) = 1
	}

	SubShader 
	{
		Pass 
		{
			CGPROGRAM
			#pragma target 3.5
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			StructuredBuffer<half3> buf_Points;
			StructuredBuffer<fixed3> buf_Colors;
			float _PointSize;

			struct ps_input {
				half4 pos : SV_POSITION;
				fixed3 col : COLOR;
				float psize : PSIZE;
			};

			ps_input vert (uint id : SV_VertexID, uint inst : SV_InstanceID)
			{
				ps_input o;
				o.pos = mul(UNITY_MATRIX_VP, half4(buf_Points[id], 1.0f));
				o.col = buf_Colors[id];
				o.psize = _PointSize;
				return o;
			}

			fixed3 frag (ps_input i) : COLOR
			{
				return i.col;
			}
			ENDCG
		}
	}
	Fallback Off
}