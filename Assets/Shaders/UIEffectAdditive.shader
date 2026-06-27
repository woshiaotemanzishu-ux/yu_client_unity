// UGUI 叠加合成:把离屏特效 RT 贴回屏幕。
// 为什么需要它:UI 特效用加色混合渲进 RT 后,RT 的 RGB 很亮但 ALPHA≈0,默认 RawImage 走标准 alpha 混合就全黑。
// 纯加色(Blend One One)能让亮粒子发光,但【有形状的暗一点的部分(如旋转圆弧线条)会被亮/花的图标底冲淡看不见】
// ——这正是「只剩粒子、圆弧没了」的根因。
// 解法:Blend One OneMinusSrcAlpha + 用【亮度当覆盖度 alpha】(特效是黑底加色,亮度≈不透明度):
//   亮处(圆弧/粒子)按亮度盖住下面的图标再叠加自身色 → 清晰成形;暗处(空白)不遮挡 → 透出图标。
//   既保留加色发光,又让圆弧这种成形元素不被亮底吃掉。纯 UI 合成,不动全局粒子 shader(场景内特效不受影响)。
Shader "Shenxiao/UI/UIEffectAdditive"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha // 加色 + 按亮度软遮挡背景(见文件头)

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
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
                fixed4 c = tex2D(_MainTex, i.texcoord) * i.color;
                fixed fade = i.color.a; // RawImage.color.a / CanvasGroup 整体调光
                // 亮度≈覆盖度:亮处当不透明(盖背景),空白当透明。Blend One OneMinusSrcAlpha 下:
                // screen = rgb*fade + 图标*(1 - lum*fade) —— 圆弧/粒子按亮度成形显现,不被亮底冲淡。
                fixed lum = max(c.r, max(c.g, c.b));
                return fixed4(c.rgb * fade, saturate(lum) * fade);
            }
            ENDCG
        }
    }

    Fallback Off
}
