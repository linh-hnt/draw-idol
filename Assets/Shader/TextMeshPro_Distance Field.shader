Shader "TextMeshPro/Distance Field"
{
    Properties
    {
        _FaceTex("Face Texture", 2D) = "white" {}
        _FaceUVSpeedX("Face UV Speed X", Range(-5, 5)) = 0
        _FaceUVSpeedY("Face UV Speed Y", Range(-5, 5)) = 0
        _FaceColor("Face Color", Color) = (1,1,1,1)
        _FaceDilate("Face Dilate", Range(-1, 1)) = 0

        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineTex("Outline Texture", 2D) = "white" {}
        _OutlineUVSpeedX("Outline UV Speed X", Range(-5, 5)) = 0
        _OutlineUVSpeedY("Outline UV Speed Y", Range(-5, 5)) = 0
        _OutlineWidth("Outline Thickness", Range(0, 1)) = 0
        _OutlineSoftness("Outline Softness", Range(0, 1)) = 0

        _Bevel("Bevel", Range(0, 1)) = 0.5
        _BevelOffset("Bevel Offset", Range(-0.5, 0.5)) = 0
        _BevelWidth("Bevel Width", Range(-0.5, 0.5)) = 0
        _BevelClamp("Bevel Clamp", Range(0, 1)) = 0
        _BevelRoundness("Bevel Roundness", Range(0, 1)) = 0

        _LightAngle("Light Angle", Range(0, 6.2831855)) = 3.1416
        _SpecularColor("Specular", Color) = (1,1,1,1)
        _SpecularPower("Specular", Range(0, 4)) = 2

        _UnderlayColor("Border Color", Color) = (0,0,0,0.5)
        _UnderlayOffsetX("Border OffsetX", Range(-1, 1)) = 0
        _UnderlayOffsetY("Border OffsetY", Range(-1, 1)) = 0
        _UnderlayDilate("Border Dilate", Range(-1, 1)) = 0
        _UnderlaySoftness("Border Softness", Range(0, 1)) = 0

        _GlowColor("Glow Color", Color) = (0,1,0,0.5)
        _GlowOffset("Glow Offset", Range(-1, 1)) = 0
        _GlowInner("Glow Inner", Range(0, 1)) = 0.05
        _GlowOuter("Glow Outer", Range(0, 1)) = 0.05
        _GlowPower("Glow Falloff", Range(1, 0)) = 0.75

        _WeightNormal("Weight Normal", Float) = 0
        _WeightBold("Weight Bold", Float) = 0.5

        _ShaderFlags("Flags", Float) = 0
        _ScaleRatioA("Scale RatioA", Float) = 1
        _ScaleRatioB("Scale RatioB", Float) = 1
        _ScaleRatioC("Scale RatioC", Float) = 1
        _MainTex("Font Atlas", 2D) = "white" {}
        _TextureWidth("Texture Width", Float) = 512
        _TextureHeight("Texture Height", Float) = 512
        _GradientScale("Gradient Scale", Float) = 5
        _ScaleX("Scale X", Float) = 1
        _ScaleY("Scale Y", Float) = 1
        _PerspectiveFilter("Perspective Correction", Range(0, 1)) = 0.875
        _Sharpness("Sharpness", Range(-1, 1)) = 0

        _VertexOffsetX("Vertex OffsetX", Float) = 0
        _VertexOffsetY("Vertex OffsetY", Float) = 0

        _MaskCoord("Mask Coordinates", Vector) = (0,0,32767,32767)
        _ClipRect("Clip Rect", Vector) = (-32767,-32767,32767,32767)
        _MaskSoftnessX("Mask SoftnessX", Float) = 0
        _MaskSoftnessY("Mask SoftnessY", Float) = 0

        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255

        _CullMode("Cull Mode", Float) = 0
        _ColorMask("Color Mask", Float) = 15
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
            Ref[_Stencil]
            Comp[_StencilComp]
            Pass[_StencilOp]
            ReadMask[_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }

        ColorMask[_ColorMask]
        Cull[_CullMode]
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex TMPVert
            #pragma fragment TMPFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
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

            half4 _FaceColor;
            half _FaceDilate;
            half4 _OutlineColor;
            half _OutlineWidth;
            half _OutlineSoftness;

            half4 _UnderlayColor;
            half _UnderlayOffsetX;
            half _UnderlayOffsetY;
            half _UnderlayDilate;
            half _UnderlaySoftness;

            half4 _GlowColor;
            half _GlowOffset;
            half _GlowInner;
            half _GlowOuter;
            half _GlowPower;

            half _WeightNormal;
            half _WeightBold;
            half _GradientScale;
            half _ScaleX;
            half _ScaleY;
            half _PerspectiveFilter;
            half _Sharpness;

            float4 _ClipRect;
            half _MaskSoftnessX;
            half _MaskSoftnessY;

            float _VertexOffsetX;
            float _VertexOffsetY;

            CBUFFER_START(UnityPerMaterial)
            CBUFFER_END

            Varyings TMPVert(Attributes input)
            {
                Varyings output;

                float3 posOS = input.positionOS.xyz;
                posOS.x += _VertexOffsetX;
                posOS.y += _VertexOffsetY;

                float3 positionWS = TransformObjectToWorld(posOS);
                output.positionCS = TransformWorldToHClip(positionWS);

                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 TMPFrag(Varyings input) : SV_Target
            {
                half distanceValue = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;

                half faceDilate = _FaceDilate * _GradientScale;
                half outlineDilate = (_OutlineWidth * _GradientScale);

                half scale = 1.0;
                scale *= abs(input.uv.y - 0.5) * 2.0 * _ScaleY;
                scale *= _GradientScale;

                half outlineSoftness = _OutlineSoftness * scale;
                half faceSoftness = min(_PerspectiveFilter, 1.0) * scale;

                half faceAlpha = smoothstep(0.5 + faceDilate + faceSoftness, 0.5 + faceDilate - faceSoftness, distanceValue);
                half outlineAlpha = smoothstep(0.5 + outlineDilate + outlineSoftness, 0.5 + outlineDilate - outlineSoftness, distanceValue);

                half outlineWeight = outlineAlpha - faceAlpha;

                half underlayDilate = _UnderlayDilate * _GradientScale;
                half underlaySoftness = _UnderlaySoftness * scale;
                half underlayAlpha = smoothstep(0.5 + underlayDilate + underlaySoftness, 0.5 + underlayDilate - underlaySoftness, distanceValue);
                underlayAlpha = max(underlayAlpha - outlineAlpha, 0.0) * _UnderlayColor.a;

                half glowOffset = _GlowOffset * _GradientScale;
                half glowOuter = _GlowOuter * scale;
                half glowInner = _GlowInner * scale;
                half glowAlpha = smoothstep(0.5 + glowOffset + glowOuter, 0.5 + glowOffset + glowInner, distanceValue) * _GlowPower;
                glowAlpha = max(glowAlpha - outlineAlpha, 0.0);

                half4 color = _FaceColor * input.color;
                color.a *= faceAlpha;
                color.a = max(color.a, 0.0);

                color.rgb = lerp(_OutlineColor.rgb, color.rgb, saturate(faceAlpha));

                color.rgb += _UnderlayColor.rgb * underlayAlpha;
                color.a = max(color.a, underlayAlpha);

                color.rgb += _GlowColor.rgb * glowAlpha * _GlowColor.a;
                color.a = max(color.a, glowAlpha * _GlowColor.a);

                float2 clipUV = input.positionCS.xy;
                float2 clipMin = _ClipRect.xy - _ClipRect.zw * 0.5;
                float2 clipMax = _ClipRect.xy + _ClipRect.zw * 0.5;
                float2 clipMask = smoothstep(clipMin, clipMin + float2(_MaskSoftnessX, _MaskSoftnessY), clipUV);
                clipMask *= smoothstep(clipMax, clipMax - float2(_MaskSoftnessX, _MaskSoftnessY), clipUV);
                color.a *= clipMask.x * clipMask.y;

                return color;
            }
            ENDHLSL
        }
    }

    Fallback "TextMeshPro/Mobile/Distance Field"
}
