Shader "UnityCoder/PointCloud/Mesh/Extras/VertexColorLighting" 
{
SubShader {
	Pass {
		Lighting On
		ColorMaterial AmbientAndDiffuse
		SetTexture [_MainTex] {
			Combine primary  * primary DOUBLE
		}
		}
	}
}