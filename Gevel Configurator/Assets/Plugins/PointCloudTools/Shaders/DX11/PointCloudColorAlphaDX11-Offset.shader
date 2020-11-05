// basic DX11 point color shader, with alpha adjust

Shader "UnityCoder/PointCloud/DX11/ColorAlpha-Offset" 
{
	Properties 
	{
		_Alpha ("Alpha", Range (0,1)) = 0.5
	}

	SubShader 
	{
		Tags { "RenderType"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Lighting Off
		Blend SrcAlpha OneMinusSrcAlpha
		Pass 
		{
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			StructuredBuffer<half3> buf_Points;
			StructuredBuffer<fixed3> buf_Colors;

			fixed _Alpha;

			struct ps_input 
			{
				half4 pos : SV_POSITION;
				fixed4 customColor : TEXCOORD1;
			};

			ps_input vert (uint id : SV_VertexID, uint inst : SV_InstanceID)
			{
				ps_input o;
				o.pos = mul(UNITY_MATRIX_VP, half4(buf_Points[id],1));
				fixed3 col = buf_Colors[id];
				#if !UNITY_COLORSPACE_GAMMA
				col = col*col; // linear
				#endif
				o.customColor = fixed4(col,_Alpha);
				return o;
			}

			float4 frag (ps_input i) : SV_Target
			{
				return i.customColor;
			}
			ENDCG
		}
	}
}