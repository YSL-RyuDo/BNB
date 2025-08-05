Shader "Custom/FlatOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0,0,1,1) // 파란색
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Cull Front
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 pos : SV_POSITION;
            };

            uniform float4 _OutlineColor;

            v2f vert(appdata v) {
                float3 norm = normalize(v.normal);
                v.vertex.xyz += norm * 0.02; // 외곽선 두께 조절
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                return _OutlineColor;
            }
            ENDCG
        }
    }
}