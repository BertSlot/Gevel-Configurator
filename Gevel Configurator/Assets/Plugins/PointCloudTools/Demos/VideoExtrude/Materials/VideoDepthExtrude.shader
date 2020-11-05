// use with high resolution mesh, extrudes vertices based on _ExtrudeTex color

Shader "UnityCoder/Extras/VideoDepthExtrude" 
{
	Properties 
	{
		_Color ("ColorTint", Color) = (1,1,1,1)
		_MainTex ("Video Texture", 2D) = "white" {}
		_ExtrudeTex ("Depth Video Texture", 2D) = "white" {}
		_Amount ("Extrusion Amount", Range(-10,10)) = 0.5
		
 		[MaterialToggle(_INVERT_OFF)] _Invert ("Invert Depth", Float) = 0 	
 		
 	}

	SubShader 
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }

		Blend One One
		Lighting Off
		//ZWrite Off
		Fog { Mode Off }

		CGPROGRAM
		#pragma target 3.0 
		#pragma surface surf Lambert alpha vertex:vert
		#pragma fragmentoption ARB_precision_hint_fastest
        #pragma multi_compile _INVERT_ON _INVERT_OFF
                
		struct Input 
		{
			float2 _ExtrudeTex;
			float4 pos;
			float2 uv_MainTex;
		};

		float _Amount;
		fixed4 _Color;
		sampler2D _ExtrudeTex;
		float4  _ExtrudeTex_ST;

		void vert (inout appdata_full v) 
		{
			float4 tex = tex2Dlod (_ExtrudeTex, float4(v.texcoord.xy,0,0));
            #if _INVERT_ON
            v.vertex.z = tex.r * _Amount;
			#else
			v.vertex.z -= tex.r * _Amount;
			#endif
		}

		sampler2D _MainTex;

		void surf (Input IN, inout SurfaceOutput o) 
		{
			half4 col = tex2D (_MainTex, IN.uv_MainTex)*_Color;
			o.Albedo = col.rgb;
			o.Alpha = 1;// col.r;
		}

		ENDCG
	} 
	Fallback "Diffuse"
}
