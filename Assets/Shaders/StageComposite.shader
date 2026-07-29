// UIModelStage/SceneCharacterStage 的透明 RT 合成 shader。RT 中的 RGB 已经是预乘结果；
// 这里以 One/OneMinusSrcAlpha 贴回 UI，并给只写 RGB 的加法光效补亮度覆盖，避免其融入金白色背景。
Shader "Shenxiao/UI/StageComposite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // RT 的 RGB 已经是预乘结果，不能再乘 alpha。纯加法材质通常只写 RGB、几乎不写 Alpha；
                // 若直接透传，在金白色背景上会被背景亮度吞掉。用自身亮度补一个软覆盖，同时保留原始 Alpha，
                // 让光效在亮底仍有轮廓，角色本体和普通透明材质的既有遮罩语义保持不变。
                fixed4 color = tex2D(_MainTex, i.texcoord) * i.color;
                fixed brightnessCoverage = saturate(max(color.r, max(color.g, color.b)));
                color.a = max(color.a, brightnessCoverage);
                return color;
            }
            ENDCG
        }
    }
}
