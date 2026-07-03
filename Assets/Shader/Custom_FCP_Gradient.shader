Shader "Custom/FCP_Gradient"
{
    Properties
    {
        [PerRendererData] _Color1("Color 1", Color) = (1,1,1,1)
        [PerRendererData] _Color2("Color 2", Color) = (1,1,1,1)
        [PerRendererData][Enum(Horizontal,0,Vertical,1,Diagonal,2)] _Mode("Color mode", Float) = 0
        [PerRendererData] _Rotation("Gradient Rotation", Range(0, 360)) = 0

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
            #pragma vertex GradientVert
            #pragma fragment GradientFrag

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

            half4 _Color1;
            half4 _Color2;
            float _Mode;
            float _Rotation;

            Varyings GradientVert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                return output;
            }

            half4 GradientFrag(Varyings input) : SV_Target
            {
                float t;

                if (_Mode < 0.5)
                {
                    t = input.uv.x;
                }
                else if (_Mode < 1.5)
                {
                    t = input.uv.y;
                }
                else
                {
                    float rad = radians(_Rotation);
                    float2 dir = float2(cos(rad), sin(rad));
                    t = dot(input.uv - 0.5, dir) + 0.5;
                }

                t = saturate(t);
                return lerp(_Color1, _Color2, t);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
