// http://wiki.unity3d.com/index.php/BakedVertexColorLighting

Shader "Unify/VertexColorLight" 
{
Properties {
    _Color ("Main Color", Color) = (1,1,1,1) // keep color white to show your original vertex color
    _SpecColor ("Spec Color", Color) = (1,1,1,1)
    _Emission ("Emissive Color", Color) = (0,0,0,0)
    _Shininess ("Shininess", Range (0.01, 1)) = 0.7
    _MainTex ("Base (RGB)", 2D) = "white" { }
}

SubShader {
    Pass {
        Material {
            Diffuse [_Color]
            Ambient [_Color]    
            Shininess [_Shininess]
            Specular [_SpecColor]
            Emission [_Emission]    
        }
        Lighting On
        SeparateSpecular On
        SetTexture [_MainTex] {
            constantColor [_Color]
            Combine texture * primary DOUBLE, texture * constant
        }
    }
        
    Pass {
        Fog { Color(0,0,0,0) }
        Blend One One
        Lighting Off

        BindChannels {
            Bind "Vertex", vertex
            Bind "TexCoord", texcoord
            Bind "Color", color
        } 

        SetTexture [_MainTex] {
            Combine primary * texture
        } 
        SetTexture [_MainTex] {
            constantColor [_Color]
            Combine previous * constant
        } 
    }
}
}