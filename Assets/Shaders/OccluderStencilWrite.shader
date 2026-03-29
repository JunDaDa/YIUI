Shader "Custom/OccluderStencilWrite"
{
    Properties
    {
        _MainTex ("Mask Texture", 2D) = "white" {}
        [Toggle(_USE_ALPHA_CLIP)] _UseAlphaClip ("Use Alpha Clip", Float) = 0
        _AlphaCutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }

        Stencil
        {
            Ref 1
            Comp Always
            Pass Replace
        }

        Blend Zero One
        ZWrite Off
        ZTest Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _ _USE_ALPHA_CLIP
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed _AlphaCutoff;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                #if defined(_USE_ALPHA_CLIP)
                fixed4 c = tex2D(_MainTex, i.uv);
                clip(c.a - _AlphaCutoff);
                #endif
                return 0;
            }
            ENDCG
        }
    }
}
