Shader "UI/TranslucentImage_GrabPass"
{
    Properties
    {
        // 通常Image互換（Spriteをマスクとして使える）
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint (multiplied by vertex color)", Color) = (1,1,1,1)

        // ぼかし
        _BlurRadius ("Blur Radius (px-ish)", Range(0, 20)) = 2

        // ぼかした色をどう変えるか
        _GlassColor ("Glass Color", Color) = (1,1,1,1)
        _GlassStrength ("Glass Strength", Range(0,1)) = 0.35  // 0=元のぼかし 1=ガラス色に寄せる

        // ぼかし結果に対する全体不透明度（好みで）
        _Opacity ("Opacity", Range(0,1)) = 1
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

        // GrabPass（非SRPでのみ有効）
        GrabPass { "_GrabTexture" }

        Pass
        {
            Name "TranslucentUI"
            Tags { "LightMode"="Always" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest [unity_GUIZTestMode]

            Stencil
            {
                Ref [unity_GUIStencilRef]
                Comp [unity_GUIStencilComp]
                Pass [unity_GUIStencilOp]
                ReadMask [unity_GUIStencilReadMask]
                WriteMask [unity_GUIStencilWriteMask]
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _GrabTexture;
            float4 _GrabTexture_TexelSize; // x=1/w, y=1/h

            fixed4 _Color;
            float _BlurRadius;

            fixed4 _GlassColor;
            float _GlassStrength;
            float _Opacity;

            float4 _ClipRect;

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
                float2 uv       : TEXCOORD0;
                float4 grabPos  : TEXCOORD1;
                float4 worldPos : TEXCOORD2;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex  = UnityObjectToClipPos(v.vertex);
                o.uv      = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color   = v.color * _Color;      // ←頂点色（GradientImage含む）× Image Tint
                o.grabPos = ComputeGrabScreenPos(o.vertex);
                o.worldPos = v.vertex;
                return o;
            }

            fixed4 SampleGrab(float2 uv)
            {
                return tex2D(_GrabTexture, uv);
            }

            fixed4 Blur9(float2 uv, float radius)
            {
                // radius をピクセルっぽく扱う
                float2 d = _GrabTexture_TexelSize.xy * radius;

                fixed4 c = 0;
                c += SampleGrab(uv + d * float2(-1,-1));
                c += SampleGrab(uv + d * float2( 0,-1));
                c += SampleGrab(uv + d * float2( 1,-1));
                c += SampleGrab(uv + d * float2(-1, 0));
                c += SampleGrab(uv + d * float2( 0, 0));
                c += SampleGrab(uv + d * float2( 1, 0));
                c += SampleGrab(uv + d * float2(-1, 1));
                c += SampleGrab(uv + d * float2( 0, 1));
                c += SampleGrab(uv + d * float2( 1, 1));
                return c / 9.0;
            }

            fixed4 Blur25Tent(float2 uv, float radius)
            {
                float2 d = _GrabTexture_TexelSize.xy * radius;

                // tent weights (1,2,1) を縦横に掛けるイメージ
                // 合計重み = 16 * 16 = 256 ではなく、ここは2Dで 36 になるよう正規化しています（簡易）
                // 実務上は見た目優先で問題ないです。
                const float w0 = 4; // center
                const float w1 = 2; // axis near
                const float w2 = 1; // diag / far

                fixed4 c = 0;
                float sum = 0;

                // offsets: -2,-1,0,1,2
                for (int x = -2; x <= 2; x++)
                {
                    for (int y = -2; y <= 2; y++)
                    {
                        float ax = abs(x);
                        float ay = abs(y);

                        float wx = (ax == 0) ? w0 : (ax == 1 ? w1 : w2);
                        float wy = (ay == 0) ? w0 : (ay == 1 ? w1 : w2);
                        float w = wx * wy;

                        c += SampleGrab(uv + d * float2(x, y)) * w;
                        sum += w;
                    }
                }

                return c / sum;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                // Sprite（角丸/三角など）の形状マスク
                fixed4 mask = tex2D(_MainTex, i.uv);

                // 画面UV（Grab）
                float2 guv = i.grabPos.xy / i.grabPos.w;

                // ぼかし
                fixed4 blurred = Blur25Tent(guv, _BlurRadius);

                // 色味変更（ガラス色へ寄せる）
                blurred.rgb = lerp(blurred.rgb, _GlassColor.rgb, saturate(_GlassStrength));

                // UIの頂点色（GradientImage/ Image.color）を乗算
                fixed4 outCol = blurred * i.color;

                // マスク（Spriteのalpha）を適用
                outCol.a *= mask.a;

                // 全体透明度
                outCol.a *= _Opacity;

                // RectMask2D / Mask対応
                #ifdef UNITY_UI_CLIP_RECT
                outCol.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif

                // AlphaClipが必要なら（UI/Default互換）
                #ifdef UNITY_UI_ALPHACLIP
                clip(outCol.a - 0.001);
                #endif

                return outCol;
            }
            ENDCG
        }
    }
}
