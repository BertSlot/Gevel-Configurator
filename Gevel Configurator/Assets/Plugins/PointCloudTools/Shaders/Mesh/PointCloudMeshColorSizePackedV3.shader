// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// PointCloudMeshColor+Size
// *** DOESNT WORK WITH DX11 MODE ***
// http://unitycoder.com

Shader "UnityCoder/PointCloud/Mesh/ColorSize-PackedV3"
{

    Properties 
    {
        _PointSize("PointSize", Float) = 1
		_Offset("Offset", Vector) = (0,0,0,0)
		_GridSizeAndPackMagic("GridSizeAndPackMagic", Float) = 1
    }

	SubShader
	{
		Tags { "Queue"="Geometry"}
		Blend SrcAlpha OneMinusSrcAlpha     // Alpha blending 
		Lighting Off
		Fog { Mode Off }
		
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma exclude_renderers flash
			
			struct appdata
			{
				float4 vertex : POSITION;
				//fixed3 color : COLOR;
			};
			
			struct v2f
			{
				float4 pos : SV_POSITION;
				fixed3 color : COLOR;
				float psize : PSIZE;
			};
			
			float _PointSize;
			float _GridSizeAndPackMagic;
			float4 _Offset;
			
			float2 SuperUnpacker(float f)
			{
				//_GridSizeAndPackMagic = 8000;
				return float2(f - floor(f), floor(f) / _GridSizeAndPackMagic);
			}

			v2f vert (appdata v)
			{
				v2f o;

				float3 rawpos = v.vertex.xyz;
				float2 xr = SuperUnpacker(rawpos.x);
				float2 yg = SuperUnpacker(rawpos.y);
				float2 zb = SuperUnpacker(rawpos.z);
				float3 p = float3(xr.y + _Offset.x, yg.y + _Offset.y, zb.y + _Offset.z);

				o.pos = UnityObjectToClipPos(float4(p,-1));

				fixed3 col = fixed3(saturate(xr.x), saturate(yg.x), saturate(zb.x)) * 1.02; // restore colors a bit
				#if !UNITY_COLORSPACE_GAMMA
				col = col * col; // linear
				#endif
				o.color = col;
				o.psize = _PointSize;
				return o;
			}
			
			fixed3 frag(v2f i) : COLOR
			{
				return i.color;
			}
			ENDCG
		}
	}
	FallBack "UnityCoder/Common/VertexShadowPass"
}