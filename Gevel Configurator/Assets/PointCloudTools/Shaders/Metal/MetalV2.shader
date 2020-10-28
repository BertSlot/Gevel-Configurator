// Mac Point Cloud Shader (similar to DX11-V2)

Shader "UnityCoder/PointCloud/Metal/ColorSizeV2" 
{
    Properties 
    {
		_Size("PointSize", Float) = 1
    }

	SubShader 
	{
		Pass 
		{
			Tags { "RenderType"="Opaque"}
			Lighting Off
			CGPROGRAM
			#pragma target 4.5
			#pragma only_renderers metal
			#pragma exclude_renderers d3d11
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			StructuredBuffer<half3> buf_Points;
			StructuredBuffer<fixed3> buf_Colors;

			struct appdata {
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				fixed3 color : COLOR0;
				float psize : PSIZE;
			};
			
			float _Size;

			v2f vert (uint id : SV_VertexID, uint inst : SV_InstanceID)
			{
				v2f o;
				half4 pos = half4(buf_Points[id],1.0f);
				o.pos = UnityObjectToClipPos(pos);
				fixed3 col = fixed3(buf_Colors[id]);
				#if !UNITY_COLORSPACE_GAMMA
				col = col*col; // linear hack
				#endif
				o.color = col;
				o.psize = _Size;
				return o;
			}
			
			fixed4 frag(v2f i) : COLOR
			{
				return fixed4(i.color,1);
			}

			ENDCG
		}
	}
	// i guess for DX11 editor needs this, otherwise warning
	Fallback "UnityCoder/PointCloud/DX11/ColorSizeV2"
}