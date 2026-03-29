// Spine/Skeleton variant for URP building occlusion.
// Pass 1 (SRPDefaultUnlit): normal render, stencil NotEqual 1
// Pass 2 (OccludedPass): semi-transparent render, stencil Equal 1 — drawn by OccludedCharacterFeature
Shader "Spine/Skeleton Occludable" {
    Properties {
        _Cutoff ("Shadow alpha cutoff", Range(0,1)) = 0.1
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

        // Pass 1: Normal rendering (default URP transparent pass picks this up)
        Pass {
            Name "Normal"

            Stencil {
                Ref 1
                Comp NotEqual
                Pass Keep
            }

            CGPROGRAM
            #pragma shader_feature _ _STRAIGHT_ALPHA_INPUT
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Packages/com.esotericsoftware.spine.spine-unity/Runtime/spine-unity/Shaders/CGIncludes/Spine-Common.cginc"
            sampler2D _MainTex;

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
                return (texColor * i.vertexColor);
            }
            ENDCG
        }

        // Pass 2: Semi-transparent in occluded area (OccludedCharacterFeature picks this up)
        Pass {
            Name "Occluded"
            Tags { "LightMode"="OccludedPass" }

            Stencil {
                Ref 1
                Comp Equal
                Pass Keep
            }

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

        Pass {
            Name "Caster"
            Tags { "LightMode"="ShadowCaster" }
            Offset 1, 1
            ZWrite On
            ZTest LEqual
            Fog { Mode Off }
            Cull Off
            Lighting Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #pragma fragmentoption ARB_precision_hint_fastest
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            fixed _Cutoff;

            struct VertexOutput {
                V2F_SHADOW_CASTER;
                float4 uvAndAlpha : TEXCOORD1;
            };

            VertexOutput vert (appdata_base v, float4 vertexColor : COLOR) {
                VertexOutput o;
                o.uvAndAlpha = v.texcoord;
                o.uvAndAlpha.a = vertexColor.a;
                TRANSFER_SHADOW_CASTER(o)
                return o;
            }

            float4 frag (VertexOutput i) : SV_Target {
                fixed4 texcol = tex2D(_MainTex, i.uvAndAlpha.xy);
                clip(texcol.a * i.uvAndAlpha.a - _Cutoff);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
