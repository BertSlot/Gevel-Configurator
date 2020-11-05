// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// PointCloudMeshColors Alpha

Shader "UnityCoder/PointCloud/Mesh/ColorAlpha"
{
	Properties
	{
	    _Color ("Color", Color) = (0.5,0.5,0.5,1)
	}

	SubShader
	{
	    Tags {"Queue"="Transparent" "RenderType"="Transparent"}
	    Blend SrcAlpha OneMinusSrcAlpha     // Alpha blending
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
	            float4 color : COLOR;
	        };
	       
	        struct v2f
	        {
	            float4 pos : SV_POSITION;
	            fixed4 color : COLOR;
	        };
	       
	        v2f vert (appdata v)
	        {
	            v2f o;
	            o.pos = UnityObjectToClipPos(v.vertex);

				fixed4 col = v.color;
				#if !UNITY_COLORSPACE_GAMMA
				col = col*col; // linear
				#endif
	            o.color = col*_Color;
	            return o;
	        }
	       
	        half4 frag(v2f i) : COLOR
	        {
	            return i.color;
	        }
	        ENDCG
	    }
	}
	Fallback Off
}
