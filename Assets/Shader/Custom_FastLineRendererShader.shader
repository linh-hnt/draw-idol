Shader "Custom/FastLineRendererShader"
{
    Properties
    {
        [PerRendererData] _MainTex("Line Texture", 2D) = "white" {}
        [PerRendererData] _MainTexStartCap("Start Cap", 2D) = "white" {}
        [PerRendererData] _MainTexEndCap("End Cap", 2D) = "white" {}
        [PerRendererData] _MainTexRoundJoin("Round Join", 2D) = "white" {}

        _TintColor("Tint Color", Color) = (1,1,1,1)

        [PerRendererData] _AnimationSpeed("Animation Speed", Float) = 0
        [PerRendererData] _UVXScale("UV X Scale", Float) = 1
        [PerRendererData] _UVYScale("UV Y Scale", Float) = 1

        [PerRendererData] _GlowIntensityMultiplier("Glow Intensity", Float) = 1
        [PerRendererData] _GlowWidthMultiplier("Glow Width", Float) = 1
        [PerRendererData] _GlowLengthMultiplier("Glow Length", Float) = 0.33
        [PerRendererData] _AnimationSpeedGlow("Glow Anim Speed", Float) = 0
        [PerRendererData] _UVXScaleGlow("Glow UV X Scale", Float) = 1
        [PerRendererData] _UVYScaleGlow("Glow UV Y Scale", Float) = 1

        [PerRendererData] _InvFade("Soft Particles Factor", Range(0.01, 3)) = 1
        [PerRendererData] _JitterMultiplier("Jitter", Float) = 0
        [PerRendererData] _Turbulence("Turbulence", Float) = 0
        [PerRendererData] _ScreenRadiusMultiplier("Screen Radius", Float) = 0

        [HideInInspector] _SoftParticlesEnabled("Soft Particles", Float) = 0
        [HideInInspector] _CameraFadingEnabled("Camera Fade", Float) = 0
        [HideInInspector] _DistortionEnabled("Distortion", Float) = 0
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
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex LineVert
            #pragma fragment LineFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

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
                float2 uvGlow : TEXCOORD1;
                float4 color : COLOR;
                float4 screenPos : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_MainTexStartCap);
            SAMPLER(sampler_MainTexStartCap);

            TEXTURE2D(_MainTexEndCap);
            SAMPLER(sampler_MainTexEndCap);

            TEXTURE2D(_MainTexRoundJoin);
            SAMPLER(sampler_MainTexRoundJoin);

            half4 _TintColor;
            float _AnimationSpeed;
            float _UVXScale;
            float _UVYScale;
            float _GlowIntensityMultiplier;
            float _GlowWidthMultiplier;
            float _GlowLengthMultiplier;
            float _AnimationSpeedGlow;
            float _UVXScaleGlow;
            float _UVYScaleGlow;
            float _InvFade;
            float _JitterMultiplier;
            float _Turbulence;
            float _ScreenRadiusMultiplier;
            float _SoftParticlesEnabled;

            CBUFFER_START(UnityPerMaterial)
            CBUFFER_END

            Varyings LineVert(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);

                float animOffset = _AnimationSpeed * _Time.y;
                output.uv = input.uv * float2(_UVXScale, _UVYScale) + float2(animOffset, 0);

                float glowAnimOffset = _AnimationSpeedGlow * _Time.y;
                output.uvGlow = input.uv * float2(_UVXScaleGlow, _UVYScaleGlow) + float2(glowAnimOffset, 0);

                output.color = input.color * _TintColor;
                output.screenPos = ComputeScreenPos(output.positionCS);

                return output;
            }

            half4 LineFrag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 color = texColor * input.color;

                half glow = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uvGlow).a;
                float glowLength = abs(input.uv.x - 0.5) * 2.0;
                float glowFalloff = 1.0 - saturate(glowLength / max(_GlowLengthMultiplier, 0.01));
                color.rgb += glow * _GlowIntensityMultiplier * glowFalloff * input.color.rgb;

                if (_SoftParticlesEnabled > 0.5)
                {
                    float sceneDepth = LinearEyeDepth(SampleSceneDepth(input.screenPos.xy / input.screenPos.w), _ZBufferParams);
                    float thisDepth = input.screenPos.w;
                    float fade = saturate(_InvFade * (sceneDepth - thisDepth));
                    color.a *= fade;
                }

                return color;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
