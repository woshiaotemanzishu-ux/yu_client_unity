Shader "Shenxiao/Effect/LayaParticleUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _Color ("Color", Color) = (1, 1, 1, 1)
        _SrcBlend ("Src Blend", Float) = 5
        _DstBlend ("Dst Blend", Float) = 10
        _SrcBlendAlpha ("Src Blend Alpha", Float) = 1
        _DstBlendAlpha ("Dst Blend Alpha", Float) = 10
        _BlendOp ("Blend Op", Float) = 0
        _ZWrite ("Z Write", Float) = 0
        _Cull ("Cull", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            BlendOp [_BlendOp]
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            sampler2D _MainTex;

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _BaseColor;
                half4 _Color;
                float4 _UIEffectClipRect;
                float4x4 _UIEffectClipWorldToLocal;
                float _UIEffectClipEnabled;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 clipPosition : TEXCOORD1;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                output.clipPosition = mul(_UIEffectClipWorldToLocal, float4(positionWS, 1.0)).xy;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                if (_UIEffectClipEnabled > 0.5)
                {
                    float4 distanceToEdge = float4(
                        input.clipPosition.x - _UIEffectClipRect.x,
                        input.clipPosition.y - _UIEffectClipRect.y,
                        _UIEffectClipRect.z - input.clipPosition.x,
                        _UIEffectClipRect.w - input.clipPosition.y);
                    clip(min(min(distanceToEdge.x, distanceToEdge.y), min(distanceToEdge.z, distanceToEdge.w)));
                }
                half4 color = tex2D(_MainTex, input.uv) * _BaseColor * input.color;
                clip(color.a - 0.001h);
                return color * 2.0h;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
