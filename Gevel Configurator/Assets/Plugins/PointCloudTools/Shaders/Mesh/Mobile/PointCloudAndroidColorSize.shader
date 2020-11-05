// This works better in some Android models (than the default PointCloudMeshSingleColorSize.shader)

Shader "UnityCoder/PointCloud/Mesh/AndroidColorSize" 
{
    Properties 
    {
        _PointSize("PointSize", Float) = 1
    }

	SubShader 
	{
		Pass 
		{
			GLSLPROGRAM

			#ifdef VERTEX
			varying vec4 v_color;
			uniform float _PointSize;

			void main()
			{   
				gl_PointSize = _PointSize;
				gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
				v_color = gl_Color;
			}
			#endif

			#ifdef FRAGMENT
			varying vec4 v_color;
			void main()
			{
				gl_FragColor = v_color;
			}
			#endif

			ENDGLSL
		}
	}

	FallBack "UnityCoder/PointCloud/Mesh/ColorSize"
}
