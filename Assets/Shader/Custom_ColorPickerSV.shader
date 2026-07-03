Shader "Custom/ColorPickerSV"
{
    Properties
    {
        _Hue("Hue", Range(0, 1)) = 0

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

            float _Hue;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                return output;
            }

            /// <summary>
            /// Converts HSV to RGB in the fragment shader.
            /// UV.x maps to Saturation (0→1), UV.y maps to Value (0→1).
            /// Hue is controlled by the _Hue material property.
            /// Bottom-left = black (V=0), top-right = pure color (S=1, V=1).
            /// </summary>
            float3 HSVToRGB(float3 hsv)
            {
                float h = hsv.x * 6.0;
                float s = hsv.y;
                float v = hsv.z;

                int i = floor(h);
                float f = h - i;
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
                float3 hsv = float3(_Hue, input.uv.x, input.uv.y);
                float3 rgb = HSVToRGB(hsv);
                return half4(rgb, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
