// UIModelStage/SceneCharacterStage 的透明 HDR RT 合成 shader。RT 中的 RGB 已经是预乘结果；
// 这里只以 One/OneMinusSrcAlpha 原样贴回 UI，不再二次乘 Alpha，也不从亮度伪造覆盖度。
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
                // RGB 已在模型 RT 内按各材质自己的混合方式生成。再次乘 Alpha 会双重衰减；
                // 用 RGB 反推 Alpha 又会把纯加法光环错误变成实心遮罩，因此必须原样返回。
                return tex2D(_MainTex, i.texcoord) * i.color;
            }
            ENDCG
        }
    }
}
