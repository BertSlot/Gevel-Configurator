// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "UnityCoder/PointCloud/Mesh/SingleColorPointSize"
{
	Properties 
	{
		_Color ("Main Color", Color) = (0,1,0,0.5)
        _PointSize("PointSize", Float) = 1
	}

	SubShader
	{
		Tags { "Queue"="Geometry"}
		Blend SrcAlpha OneMinusSrcAlpha     // Alpha blending 
		Lighting Off
//		Cull Off
//		ZWrite Off
		Fog { Mode Off }
		
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			
			fixed4 _Color;
			
			struct appdata
			{
				float4 vertex : POSITION;
			};
			
			struct v2f
			{
				float4 pos : SV_POSITION;
				float psize:PSIZE;
			};
			
			float _PointSize;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.psize = _PointSize;
				return o;
			}
			
			half4 frag(v2f i) : COLOR
			{
				return _Color;
			}
			
			ENDCG
		}
	}
}