// PointCloudMeshColors Alpha Fade By Distance

Shader "UnityCoder/PointCloud/Mesh/ColorAlphaFadeByDistance"
{
	Properties
	{
	    _Color ("Color", Color) = (0.5,0.5,0.5,1)
		_FadeDistance ("Fade Start Distance", float) = 1
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
	        #include "UnityCG.cginc"

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
	       
		   float _FadeDistance;

	        v2f vert (appdata v)
	        {
	            v2f o;
	            o.pos = UnityObjectToClipPos(v.vertex);

				float depth = -UnityObjectToViewPos(v.vertex.xyz).z;
				float alpha = (depth - _ProjectionParams.y)/_FadeDistance;
				_Color.a = min(alpha, 1);

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
