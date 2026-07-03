Shader "Spine/Sprite/Unlit"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)

        [Toggle] PixelSnap("Pixel Snap", Float) = 0

        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.1
        _ShadowAlphaCutoff("Shadow Alpha Cutoff", Range(0, 1)) = 0.1

        _OverlayColor("Overlay Color", Color) = (0,0,0,0)

        _Hue("Hue", Range(-0.5, 0.5)) = 0
        _Saturation("Saturation", Range(0, 2)) = 1
        _Brightness("Brightness", Range(0, 2)) = 1

        _BlendTex("Blend Texture", 2D) = "white" {}
        _BlendAmount("Blend", Range(0, 1)) = 0

        [HideInInspector] _SrcBlend("Src Blend", Float) = 1
        [HideInInspector] _DstBlend("Dst Blend", Float) = 0
        [HideInInspector] _ZWrite("Depth Write", Float) = 0
        [HideInInspector] _Cull("Cull", Float) = 0
        [HideInInspector] _RenderQueue("Render Queue", Float) = 0

        [HideInInspector] _StencilRef("Stencil Reference", Float) = 0
        [HideInInspector] _StencilComp("Stencil Comparison", Float) = 8

        [HideInInspector] _OutlineWidth("Outline Width", Range(0, 8)) = 3
        [HideInInspector] _OutlineColor("Outline Color", Color) = (1,1,0,1)
        [HideInInspector] _OutlineReferenceTexWidth("Reference Texture Width", Float) = 1024
        [HideInInspector] _ThresholdEnd("Outline Threshold", Range(0, 1)) = 0.25
        [HideInInspector] _OutlineSmoothness("Outline Smoothness", Range(0, 1)) = 1
        [HideInInspector][Toggle] _Use8Neighbourhood("Sample 8 Neighbours", Float) = 1
        [HideInInspector] _OutlineMipLevel("Outline Mip Level", Range(0, 3)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Stencil
        {
            Ref[_StencilRef]
            Comp[_StencilComp]
        }

        Cull[_Cull]
        ZWrite[_ZWrite]
        Blend[_SrcBlend][_DstBlend]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex SpineVert
            #pragma fragment SpineFrag

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
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_BlendTex);
            SAMPLER(sampler_BlendTex);

            half4 _Color;
            half4 _OverlayColor;
            half _Hue;
            half _Saturation;
            half _Brightness;
            half _BlendAmount;
            half _Cutoff;

            CBUFFER_START(UnityPerMaterial)
            CBUFFER_END

            half3 ApplyHue(half3 col, half shift)
            {
                half3 weights = half3(0.299, 0.587, 0.114);
                half v = dot(col, weights);
                half u = cos(shift * 6.283185) * (col.r - v) + sin(shift * 6.283185) * (col.b - col.g) * 0.5;
                half w = col.r - col.g + col.b - v;
                return half3(v + u, v + w - u, v - w);
            }

            half3 ApplyHSV(half3 col, half hue, half sat, half bri)
            {
                col = ApplyHue(col, hue);
                half gray = dot(col, half3(0.299, 0.587, 0.114));
                col = lerp(half3(gray, gray, gray), col, sat);
                col *= bri;
                return col;
            }

            Varyings SpineVert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 SpineFrag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 color = texColor * input.color * _Color;

                clip(color.a - _Cutoff);

                half mask = step(0.001, texColor.a);

                color.rgb = lerp(color.rgb, ApplyHSV(color.rgb, _Hue, _Saturation, _Brightness), mask);
                color.rgb += _OverlayColor.rgb * _OverlayColor.a * mask;

                if (_BlendAmount > 0.001)
                {
                    half4 blendTex = SAMPLE_TEXTURE2D(_BlendTex, sampler_BlendTex, input.uv);
                    color.rgb = lerp(color.rgb, blendTex.rgb, _BlendAmount * mask);
                }

                return color;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
