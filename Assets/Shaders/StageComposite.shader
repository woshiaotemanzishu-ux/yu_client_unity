// UIModelStage/SceneCharacterStage 的 RT 合成 shader。场景角色台始终启用；UI 模型台目前给
// ArtModelRenderProfile 整模启用：
// 展示相机把模型渲到透明底 RT,内容天然是"预乘 alpha"形式;默认 UI 材质按 SrcAlpha 混合会把
// 加法混合的特效(写了 alpha 的光团)洗成一大块白。这里改用 One/OneMinusSrcAlpha 预乘合成:
// 加法特效的光正确叠加到 UI 背景上,半透/不透明部分照常覆盖。
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
                // 直接输出,不再乘 alpha——预乘内容乘了就双重衰减/加法光被洗掉
                return tex2D(_MainTex, i.texcoord) * i.color;
            }
            ENDCG
        }
    }
}
