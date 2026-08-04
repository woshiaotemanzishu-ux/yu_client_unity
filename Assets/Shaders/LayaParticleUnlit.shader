Shader "Shenxiao/Effect/LayaParticleUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _Color ("Color", Color) = (1, 1, 1, 1)
        [Toggle] _UseBaseMapST ("Use BaseMap UV Transform", Float) = 0
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
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            sampler2D _MainTex;

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _Color;
                float _UseBaseMapST;
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
                // 少量旧资源仍驱动 _MainTex_ST；当前 Laya 导入器统一把 tilingOffset 动画
                // 写到 _BaseMap_ST。由材质显式选择，避免为了修新资源破坏已验收的旧流光。
                float4 uvTransform = lerp(_MainTex_ST, _BaseMap_ST, saturate(_UseBaseMapST));
                output.uv = input.uv * uvTransform.xy + uvTransform.zw;
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
                // Laya 粒子材质按 Gamma 数值制作：大量材质用 tint=0.5，再由原 shader 的 *2
                // 还原为中性色。项目切到 Linear 后，Unity 会在上传 Color 属性时先把 0.5
                // 转成约 0.214；若直接乘 2，整套旧特效只剩约 43% 强度（升级文字/光柱一起发淡）。
                // 纹理仍按 sRGB 正常采样；这里只把材质 tint 恢复成 Laya 制作时使用的数值，
                // 保留项目 Linear 光照与新角色材质，不做全局色彩空间回退。
                half4 layaTint = _BaseColor;
                #if !defined(UNITY_COLORSPACE_GAMMA)
                    layaTint.rgb = LinearToSRGB(layaTint.rgb);
                #endif
                half4 color = tex2D(_MainTex, input.uv) * layaTint * input.color;
                clip(color.a - 0.001h);
                return color * 2.0h;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
