// Spine skeleton shader that renders at reduced alpha.
// Used by URP Render Objects Feature for occluded area rendering.
Shader "Spine/Skeleton SemiTransparent" {
    Properties {
        [NoScaleOffset] _MainTex ("Main Texture", 2D) = "black" {}
        [Toggle(_STRAIGHT_ALPHA_INPUT)] _StraightAlphaInput("Straight Alpha Texture", Int) = 0
        _OccludedAlpha ("Occluded Alpha", Range(0, 1)) = 0.3
    }

    SubShader {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }

        Fog { Mode Off }
        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Lighting Off

        Stencil {
            Ref 1
            Comp Equal
            Pass Keep
        }

        Pass {
            Name "OccludedRender"

            CGPROGRAM
            #pragma shader_feature _ _STRAIGHT_ALPHA_INPUT
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Packages/com.esotericsoftware.spine.spine-unity/Runtime/spine-unity/Shaders/CGIncludes/Spine-Common.cginc"
            sampler2D _MainTex;
            fixed _OccludedAlpha;

            struct VertexInput {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 vertexColor : COLOR;
            };

            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 vertexColor : COLOR;
            };

            VertexOutput vert (VertexInput v) {
                VertexOutput o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.vertexColor = PMAGammaToTargetSpace(v.vertexColor);
                return o;
            }

            float4 frag (VertexOutput i) : SV_Target {
                float4 texColor = tex2D(_MainTex, i.uv);
                #if defined(_STRAIGHT_ALPHA_INPUT)
                texColor.rgb *= texColor.a;
                #endif
                float4 result = texColor * i.vertexColor;
                result *= _OccludedAlpha;
                return result;
            }
            ENDCG
        }
    }
}
