// PointCloud Shader for DX11 Viewer with PointSize and ColorTint (version 2, less math)
// Fog Support version - thanks to sjm-tech
     
    Shader "UnityCoder/PointCloud/DX11/ColorSizeV2-Fog"
    {
        Properties
        {
            _Tint ("Tint", Color) = (1,1,1,1)
            _Size ("Size", Range(0.001, 0.25)) = 0.01
        }
     
        SubShader
        {
            Pass
            {
                Tags { "Queue" = "Geometry" "RenderType"="Opaque" }
                LOD 200
                CGPROGRAM
                #pragma target 5.0
                #pragma vertex Vert
                #pragma fragment Frag
                #pragma geometry Geom
                #pragma multi_compile_fog
     
                #include "UnityCG.cginc"
               
                StructuredBuffer<half3> buf_Points;
                StructuredBuffer<fixed3> buf_Colors;
     
                struct appdata
                {
                    half4 vertex : POSITION;
                    fixed4 color : COLOR;
                };
     
                struct GS_INPUT
                {
                    half3    pos        : TEXCOORD0;
                    fixed4 color     : COLOR;
                };
     
                struct FS_INPUT
                {
                    half4    pos        : POSITION;
                    fixed4 color     : COLOR;
                    UNITY_FOG_COORDS(2)
                };
     
                float _Size;
                fixed4 _Tint;
     
                GS_INPUT Vert(uint id : SV_VertexID, uint inst : SV_InstanceID)
                {
                    GS_INPUT o = (GS_INPUT)0;
                    o.pos = buf_Points[id];

					fixed3 col = buf_Colors[id];
					#if !UNITY_COLORSPACE_GAMMA
					col = col*col; // linear
					#endif

					o.color = fixed4(col,1)* _Tint;

                    return o;
                }
     
                [maxvertexcount(4)]
                void Geom(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
                {
                    float3 cameraUp = UNITY_MATRIX_IT_MV[1].xyz;
                    float3 cameraForward = _WorldSpaceCameraPos - p[0].pos;
                    float3 rightSize = normalize(cross(cameraUp, cameraForward))*_Size;
                    float3 cameraSize = _Size * cameraUp;
     
                    FS_INPUT newVert;
                    UNITY_INITIALIZE_OUTPUT(FS_INPUT,newVert);
                    newVert.pos = UnityObjectToClipPos(float4(p[0].pos + rightSize - cameraSize,1));
                    newVert.color = p[0].color;
                    UNITY_TRANSFER_FOG(newVert, newVert.pos);
                    triStream.Append(newVert);
                    newVert.pos =  UnityObjectToClipPos(float4(p[0].pos + rightSize + cameraSize,1));
                    newVert.color = p[0].color;
                    UNITY_TRANSFER_FOG(newVert, newVert.pos);
                    triStream.Append(newVert);
                    newVert.pos =  UnityObjectToClipPos(float4(p[0].pos - rightSize - cameraSize,1));
                    newVert.color = p[0].color;
                    UNITY_TRANSFER_FOG(newVert, newVert.pos);
                    triStream.Append(newVert);
                    newVert.pos =  UnityObjectToClipPos(float4(p[0].pos - rightSize + cameraSize,1));
                    newVert.color = p[0].color;
                    UNITY_TRANSFER_FOG(newVert, newVert.pos);
                    triStream.Append(newVert);                                      
                }
     
                fixed4 Frag(FS_INPUT i ) : COLOR
                {
                    UNITY_APPLY_FOG(i.fogCoord, i.color);
                    return i.color;
                }
                ENDCG
            }
        }
    }
