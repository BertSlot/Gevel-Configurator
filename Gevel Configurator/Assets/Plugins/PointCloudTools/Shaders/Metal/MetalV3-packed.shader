// Mac Point Cloud Shader (alternative to DX11-V3-Packed)

Shader "UnityCoder/PointCloud/Metal/V3-packed"
{
    Properties 
    {
		_Size("PointSize", Float) = 1
		_Offset("Offset", Vector) = (0,0,0,0)
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
			float _GridSizeAndPackMagic;
			float4 _Offset;

			float2 SuperUnpacker(float f)
			{
				return float2(f - floor(f), floor(f) / _GridSizeAndPackMagic);
			}

			v2f vert (uint id : SV_VertexID, uint inst : SV_InstanceID)
			{
				v2f o;
				
				float3 rawpos = buf_Points[id];
				float2 xr = SuperUnpacker(rawpos.x);
				float2 yg = SuperUnpacker(rawpos.y);
				float2 zb = SuperUnpacker(rawpos.z);
				float3 p = float3(xr.y + _Offset.x, yg.y + _Offset.y, zb.y + _Offset.z);

				o.pos = UnityObjectToClipPos(p);

				fixed3 col = fixed3(saturate(xr.x), saturate(yg.x), saturate(zb.x)) * 1.02;

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
	Fallback "UnityCoder/PointCloud/DX11/ColorSizeV3-packed"
}