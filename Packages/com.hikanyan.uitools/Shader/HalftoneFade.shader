Shader "UI/HalftoneFade_PixelLocked"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // ---- Halftone params (PIXEL BASED) ----
        _DotSpacingPx ("Dot Spacing (px)", Float) = 18
        _MaxRadiusPx  ("Max Dot Radius (px)", Float) = 9
        _AngleDeg     ("Screen Angle (deg)", Range(0,180)) = 45
        _EdgeSoftnessPx ("Edge Softness (px)", Range(0.0, 4.0)) = 1.0

        // ---- Gradient control (still UV-based by default) ----
        _FadeStart ("Fade Start (0-1)", Range(0,1)) = 0.0
        _FadeEnd   ("Fade End (0-1)", Range(0,1)) = 1.0
        [Toggle] _Invert ("Invert", Float) = 0

        // UI default
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex    : SV_POSITION;
                fixed4 color     : COLOR;
                float2 uv        : TEXCOORD0;

                float4 worldPos  : TEXCOORD1;
                float4 screenPos : TEXCOORD2; // for pixel-locked pattern
            };

            sampler2D _MainTex;
            fixed4 _Color;

            float _DotSpacingPx;
            float _MaxRadiusPx;
            float _AngleDeg;
            float _EdgeSoftnessPx;

            float _FadeStart;
            float _FadeEnd;
            float _Invert;

            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPos = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex); // clip -> screen
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            float Remap01(float x, float a, float b)
            {
                float t = (x - a) / max(1e-5, (b - a));
                return saturate(t);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv) * i.color;

                // ===== 1) フェード係数 t（ここは従来通りUV基準。必要なら後述の拡張で“ピクセル方向”にもできる） =====
                float t = Remap01(i.uv.x, _FadeStart, _FadeEnd);
                if (_Invert > 0.5) t = 1.0 - t;

                // ===== 2) スクリーン “ピクセル座標” を作る =====
                // i.screenPos.xy / i.screenPos.w は 0..1 のスクリーンUV
                float2 screenUV = i.screenPos.xy / max(1e-5, i.screenPos.w);
                float2 pixelPos = screenUV * _ScreenParams.xy; // 実ピクセル座標

                // ===== 3) スクリーントーン角（ピクセル座標を回転） =====
                float rad = radians(_AngleDeg);
                float s = sin(rad), c = cos(rad);

                // 原点回り回転のため適当にオフセット（どこでも良いが、安定のため画面中央基準）
                float2 p = pixelPos - 0.5 * _ScreenParams.xy;
                float2 rp = float2(c * p.x - s * p.y, s * p.x + c * p.y);

                // ===== 4) ピクセル基準でグリッド化（RectTransformの伸縮の影響を受けない） =====
                float spacing = max(1e-5, _DotSpacingPx);
                float2 grid = rp / spacing;

                // セル中心からの距離（ピクセル単位に戻す）
                float2 cell = (frac(grid) - 0.5) * spacing; // [-spacing/2, spacing/2] px
                float distPx = length(cell);

                // ===== 5) 半径を t で縮める（px単位） =====
                float radiusPx = lerp(_MaxRadiusPx, 0.0, t);

                // ===== 6) ドットマスク（円の境界のみソフト） =====
                float softPx = max(1e-5, _EdgeSoftnessPx);
                float mask = smoothstep(radiusPx + softPx, radiusPx - softPx, distPx);

                tex.a *= mask;

                #ifdef UNITY_UI_CLIP_RECT
                tex.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(tex.a - 0.001);
                #endif

                return tex;
            }
            ENDCG
        }
    }
}
