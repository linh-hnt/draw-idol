Shader "Sprite/Overlay"
{
    Properties
    {
        [MainTexture] _MainTex("Main Texture", 2D) = "white" {}
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)

        [Toggle] _ShowMain("Show Main Texture", Float) = 1
        _MainColor("Main Color", Color) = (1,1,1,1)

        _OverlayTex("Overlay Texture", 2D) = "white" {}
        _OverlayColor("Overlay Color", Color) = (1,1,1,1)
        _OverlayGlow("Overlay Glow", Range(0, 25)) = 1
        _OverlayBlend("Overlay Blend", Range(0, 1)) = 1
        _OverlayScale("Overlay Scale", Range(0.1, 10)) = 1

        [HideInInspector] _MinXUV("MinXUV", Range(0, 1)) = 0
        [HideInInspector] _MaxXUV("MaxXUV", Range(0, 1)) = 1
        [HideInInspector] _MinYUV("MinYUV", Range(0, 1)) = 0
        [HideInInspector] _MaxYUV("MaxYUV", Range(0, 1)) = 1

        [HideInInspector] _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5

        [HideInInspector] _RandomSeed("RandomSeed", Range(0, 10000)) = 0
        [HideInInspector] _MySrcMode("SrcMode", Float) = 5
        [HideInInspector] _MyDstMode("DstMode", Float) = 10

        [HideInInspector] _Stencil("Stencil ID", Float) = 0
        [HideInInspector] _StencilComp("Stencil Comparison", Float) = 8
        [HideInInspector] _StencilOp("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask("Color Mask", Float) = 15

        [Toggle] _AlphaClip("Alpha Clip", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref[_Stencil]
            Comp[_StencilComp]
            Pass[_StencilOp]
            ReadMask[_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }

        ColorMask[_ColorMask]

        Cull Off
        ZWrite Off
        Blend[_MySrcMode][_MyDstMode]

        Pass
        {
            Name "SpriteOverlay"

            HLSLPROGRAM
            #pragma vertex SpriteOverlayVert
            #pragma fragment SpriteOverlayFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uvOverlay : TEXCOORD1;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            TEXTURE2D(_OverlayTex);
            SAMPLER(sampler_OverlayTex);
            float4 _OverlayTex_ST;

            half4 _Color;
            half4 _OverlayColor;
            half _OverlayGlow;
            half _OverlayBlend;
            half _OverlayScale;
            half _Cutoff;
            half _ShowMain;
            half4 _MainColor;

            float _MinXUV;
            float _MaxXUV;
            float _MinYUV;
            float _MaxYUV;

            CBUFFER_START(UnityPerMaterial)
            CBUFFER_END

            Varyings SpriteOverlayVert(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);

                float2 atlasUV = input.uv;
                float2 clampedUV = float2(
                    lerp(_MinXUV, _MaxXUV, atlasUV.x),
                    lerp(_MinYUV, _MaxYUV, atlasUV.y)
                );

                output.uv = TRANSFORM_TEX(clampedUV, _MainTex);

                float2 overlayUV = input.uv * _OverlayTex_ST.xy + _OverlayTex_ST.zw;
                overlayUV = (overlayUV - 0.5) / _OverlayScale + 0.5;
                output.uvOverlay = overlayUV;
                output.color = input.color;

                return output;
            }

            half4 SpriteOverlayFrag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 mainColor = tex * _MainColor;

                #ifdef _ALPHACLIP_ON
                    clip(mainColor.a - _Cutoff);
                #endif

                half4 overlayColor = SAMPLE_TEXTURE2D(_OverlayTex, sampler_OverlayTex, input.uvOverlay);
                overlayColor *= _OverlayColor;
                overlayColor.rgb *= _OverlayGlow;

                half mainMask = step(0.001, tex.a);
                half3 mainRGB = mainColor.rgb * _ShowMain;
                half3 overlayRGB = overlayColor.rgb * overlayColor.a * _OverlayBlend;
                half3 result = lerp(mainRGB, overlayRGB, overlayColor.a * _OverlayBlend) * mainMask;
                half alpha = max(mainColor.a * _ShowMain, overlayColor.a * _OverlayBlend) * mainMask;

                result *= input.color.rgb;
                alpha *= input.color.a;

                return half4(result, alpha);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
