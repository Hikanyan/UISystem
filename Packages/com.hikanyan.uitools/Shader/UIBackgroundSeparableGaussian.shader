Shader "UI/SeparableGaussian"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint (multiplied by vertex color)", Color) = (1,1,1,1)

        // ガウスの半径（あなたのGlassShaderの _Blur 相当）
        _BlurRadius ("Blur Radius", Range(1, 60)) = 10

        // ガウス重みの落ち方（あなたの exp(...)*5.0 の 5.0 部分）
        _GaussianFalloff ("Gaussian Falloff", Range(0.5, 12)) = 5.0

        // ぼかした色をどう変えるか
        _GlassColor ("Glass Color", Color) = (1,1,1,1)
        _GlassStrength ("Glass Strength", Range(0,1)) = 0.35

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

        // 1回目：現在の画面を掴む
        GrabPass { }

        // -------------------------
        // Pass 1 : Horizontal Gaussian
        // -------------------------
        Pass
        {
            Name "GaussianH"
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
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment fragH
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _GrabTexture;
            float4 _GrabTexture_TexelSize;

            fixed4 _Color;
            float _BlurRadius;
            float _GaussianFalloff;

            fixed4 _GlassColor;
            float _GlassStrength;
            float _Opacity;

            float4 _ClipRect;

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;     // GradientImage 等の頂点色
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float4 grabPos  : TEXCOORD0;
                float2 uv       : TEXCOORD1;
                fixed4 vcol     : COLOR;
                float4 worldPos : TEXCOORD2;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.grabPos = ComputeGrabScreenPos(o.pos);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.vcol = v.color * _Color;
                o.worldPos = v.vertex;
                return o;
            }

            fixed4 Gaussian1D_H(float4 grabPos, float blur)
            {
                blur = max(1.0, blur);

                fixed4 col = 0;
                float wsum = 0;

                // あなたのGlassShaderと同じ重み
                [loop]
                for (float x = -blur; x <= blur; x += 1.0)
                {
                    float dn = abs(x / blur);
                    float w  = exp(-0.5 * dn * dn * _GaussianFalloff);
                    wsum += w;

                    float4 gp = grabPos + float4(x * _GrabTexture_TexelSize.x, 0, 0, 0);
                    col += tex2Dproj(_GrabTexture, gp) * w;
                }

                return col / max(wsum, 1e-5);
            }

            fixed4 ApplyUIExtras(fixed4 blurred, v2f i)
            {
                // ガラス色へ寄せる
                blurred.rgb = lerp(blurred.rgb, _GlassColor.rgb, saturate(_GlassStrength));

                // 頂点色（GradientImage/ Image.color）を掛ける
                fixed4 outCol = blurred * i.vcol;

                // Spriteのalphaで形状マスク（角丸/三角など）
                fixed4 mask = tex2D(_MainTex, i.uv);
                outCol.a *= mask.a;

                // 透明度
                outCol.a *= _Opacity;

                // RectMask2D / Mask
                #ifdef UNITY_UI_CLIP_RECT
                outCol.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(outCol.a - 0.001);
                #endif

                return outCol;
            }

            fixed4 fragH(v2f i) : SV_Target
            {
                fixed4 blurred = Gaussian1D_H(i.grabPos, _BlurRadius);
                return ApplyUIExtras(blurred, i);
            }
            ENDCG
        }

        // 2回目：Pass1 を合成した後の画面を掴む（あなたのGlassShaderと同じ構造）
        GrabPass { }

        // -------------------------
        // Pass 2 : Vertical Gaussian
        // -------------------------
        Pass
        {
            Name "GaussianV"
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
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment fragV
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _GrabTexture;
            float4 _GrabTexture_TexelSize;

            fixed4 _Color;
            float _BlurRadius;
            float _GaussianFalloff;

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
                float4 pos      : SV_POSITION;
                float4 grabPos  : TEXCOORD0;
                float2 uv       : TEXCOORD1;
                fixed4 vcol     : COLOR;
                float4 worldPos : TEXCOORD2;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.grabPos = ComputeGrabScreenPos(o.pos);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.vcol = v.color * _Color;
                o.worldPos = v.vertex;
                return o;
            }

            fixed4 Gaussian1D_V(float4 grabPos, float blur)
            {
                blur = max(1.0, blur);

                fixed4 col = 0;
                float wsum = 0;

                [loop]
                for (float y = -blur; y <= blur; y += 1.0)
                {
                    float dn = abs(y / blur);
                    float w  = exp(-0.5 * dn * dn * _GaussianFalloff);
                    wsum += w;

                    float4 gp = grabPos + float4(0, y * _GrabTexture_TexelSize.y, 0, 0);
                    col += tex2Dproj(_GrabTexture, gp) * w;
                }

                return col / max(wsum, 1e-5);
            }

            fixed4 ApplyUIExtras(fixed4 blurred, v2f i)
            {
                blurred.rgb = lerp(blurred.rgb, _GlassColor.rgb, saturate(_GlassStrength));
                fixed4 outCol = blurred * i.vcol;

                fixed4 mask = tex2D(_MainTex, i.uv);
                outCol.a *= mask.a;

                outCol.a *= _Opacity;

                #ifdef UNITY_UI_CLIP_RECT
                outCol.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(outCol.a - 0.001);
                #endif

                return outCol;
            }

            fixed4 fragV(v2f i) : SV_Target
            {
                fixed4 blurred = Gaussian1D_V(i.grabPos, _BlurRadius);
                return ApplyUIExtras(blurred, i);
            }
            ENDCG
        }
    }
}
