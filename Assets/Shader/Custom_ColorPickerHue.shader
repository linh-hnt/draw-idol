Shader "Custom/ColorPickerHue"
{
    Properties
    {
        [HideInInspector] _StencilComp("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask("Color Mask", Float) = 15
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

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                return output;
            }

            /// <summary>
            /// Converts HSV to RGB. Used to render the full hue spectrum
            /// where UV.x drives hue from 0 (red) to 1 (red, wrapping).
            /// </summary>
            float3 HSVToRGB(float3 hsv)
            {
                float h = hsv.x * 6.0;
                float s = hsv.y;
                float v = hsv.z;

                int i = (int)floor(h);
                float f = h - (float)i;
                float p = v * (1.0 - s);
                float q = v * (1.0 - s * f);
                float t = v * (1.0 - s * (1.0 - f));

                switch (i % 6)
                {
                    case 0: return float3(v, t, p);
                    case 1: return float3(q, v, p);
                    case 2: return float3(p, v, t);
                    case 3: return float3(p, q, v);
                    case 4: return float3(t, p, v);
                    case 5: return float3(v, p, q);
                    default: return float3(0, 0, 0);
                }
            }

            half4 Frag(Varyings input) : SV_Target
            {
                /// Hue = UV.x (full spectrum 0→1), Saturation = 1, Value = 1
                float3 hsv = float3(input.uv.x, 1.0, 1.0);
                float3 rgb = HSVToRGB(hsv);
                return half4(rgb, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
