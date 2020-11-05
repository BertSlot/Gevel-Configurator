// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// PointCloudMeshColor+Size
// *** DOESNT WORK WITH DX11 MODE ***
// http://unitycoder.com

Shader "UnityCoder/PointCloud/Mesh/ColorSize"
{
    Properties 
    {
        _PointSize("PointSize", Float) = 1
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
				float4 color : COLOR;
			};
			
			struct v2f
			{
				float4 pos : SV_POSITION;
				fixed4 color : COLOR;
				float psize : PSIZE;
			};
			
			float _PointSize;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				fixed4 col = v.color;
				#if !UNITY_COLORSPACE_GAMMA
				col = col*col; // linear
				#endif
				o.color = col;
				o.psize = _PointSize;
				return o;
			}
			
			half4 frag(v2f i) : COLOR
			{
				return i.color;
			}
			ENDCG
		}
	}
	FallBack "UnityCoder/Common/VertexShadowPass"
}