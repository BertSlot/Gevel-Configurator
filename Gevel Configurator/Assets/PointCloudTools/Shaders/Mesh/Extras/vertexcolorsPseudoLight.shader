// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// some parts of the source: http://forum.unity3d.com/threads/5785-shader-SetGlobalVector?p=43611&viewfull=1#post43611
// modified by http://unitycoder.com/blog/

Shader "UnityCoder/PointCloud/Mesh/Extras/VertexColorPseudoLight"
{
    Properties {
	  	_LightPos("LightPosition", Vector) = (0,0,0,0)
		_LightLimit("Light Limit", Float) = 200
    }

	SubShader
	{
		Tags { "Queue"="Geometry"}
		ZWrite On
		Fog { Mode Off }
	Pass
	{
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma fragmentoption ARB_precision_hint_fastest
		
		float _LightLimit;
		float3 _LightPos;
		
		struct appdata
		{
			float4 vertex : POSITION;
			float4 color : COLOR;
		};
		struct v2f
		{
			float4 pos : SV_POSITION;
			fixed4 color : COLOR;
			//float size : PSIZE;
		};
		v2f vert (appdata v)
		{
			v2f o;
			// TODO: should take direction the camera is facing, now bulge comes at exact point position, not towards dir..
//			float distMulti = (_Limit-min(_Limit,distance(v.vertex.xyz, _Pos)))/_Limit; //distance falloff
//			float3 dir = normalize(v.vertex.xyz-_Pos);
//			v.vertex.xyz += dir * (distMulti*_Amount);
//			v.vertex.xz += dir * (distMulti*_Amount);
			o.pos = UnityObjectToClipPos(v.vertex);
			// fakelight effect for now..
			float distLight = (_LightLimit-min(_LightLimit,distance(v.vertex.xyz, _LightPos)))/_LightLimit; //distance falloff
			o.color = v.color-(1-distLight);
			//o.size = 10;
//			o.color.a = 1-distMulti;
			return o;
		}
		half4 frag(v2f i) : COLOR
		{
			return  i.color;
		}
		ENDCG
		}
	}
}