Shader "Unlit/OutlineShader"
{
    Properties
    {
        _Outline_Thickness ("Outline Thickness", float) = 0.02
        _Outline_Color ("Outline Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags {
            "RenderType"="Opaque"
        }
        LOD 100

        Pass
        {
            Cull Front
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _Outline_Color;
            float _Outline_Thickness;

            // inverse-hull
            v2f vert(appdata v)
            {
                v2f o;

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
                float3 worldNormal = normalize(mul(unity_ObjectToWorld, v.normal));
                
                worldPos += _Outline_Thickness * worldNormal;  // apply offset
                
                o.vertex = UnityWorldToClipPos(worldPos);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _Outline_Color;
            }
            ENDCG
        }
    }
}
