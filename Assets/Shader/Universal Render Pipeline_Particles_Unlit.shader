Shader "Universal Render Pipeline/Particles/Unlit"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)

        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5

        _BumpMap("Normal Map", 2D) = "bump" {}

        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)
        _EmissionMap("Emission Map", 2D) = "white" {}

        _SoftParticlesNearFadeDistance("Soft Particles Near Fade", Float) = 0
        _SoftParticlesFarFadeDistance("Soft Particles Far Fade", Float) = 1
        _CameraNearFadeDistance("Camera Near Fade", Float) = 1
        _CameraFarFadeDistance("Camera Far Fade", Float) = 2

        _DistortionBlend("Distortion Blend", Range(0, 1)) = 0.5
        _DistortionStrength("Distortion Strength", Float) = 1

        _Surface("Surface", Float) = 0
        _Blend("Blend Mode", Float) = 0
        _Cull("Cull", Float) = 2

        [Toggle] _AlphaClip("Alpha Clip", Float) = 0
        [Toggle] _SoftParticlesEnabled("Soft Particles", Float) = 0
        [Toggle] _CameraFadingEnabled("Camera Fading", Float) = 0
        [Toggle] _DistortionEnabled("Distortion", Float) = 0
        [Toggle] _FlipbookBlending("Flipbook Blending", Float) = 0

        [HideInInspector] _BlendOp("Blend Op", Float) = 0
        [HideInInspector] _SrcBlend("Src Blend", Float) = 1
        [HideInInspector] _DstBlend("Dst Blend", Float) = 0
        [HideInInspector] _SrcBlendAlpha("Src Blend Alpha", Float) = 1
        [HideInInspector] _DstBlendAlpha("Dst Blend Alpha", Float) = 0
        [HideInInspector] _ZWrite("ZWrite", Float) = 1

        _ColorMode("Color Mode", Float) = 0
        _FlipbookMode("Flipbook Mode", Float) = 0
        _Mode("Mode", Float) = 0
        _QueueOffset("Queue Offset", Float) = 0

        [HideInInspector] _SoftParticleFadeParams("Soft Particle Fade Params", Vector) = (0,0,0,0)
        [HideInInspector] _CameraFadeParams("Camera Fade Params", Vector) = (0,0,0,0)
        [HideInInspector] _DistortionStrengthScaled("Distortion Strength Scaled", Float) = 0.1
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

        Cull[_Cull]
        ZWrite[_ZWrite]
        Blend[_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
        BlendOp[_BlendOp]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex ParticleVert
            #pragma fragment ParticleFrag

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
                float4 color : COLOR;
                float4 screenPos : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            half4 _BaseColor;
            half4 _EmissionColor;
            half _Cutoff;
            half _SoftParticlesNearFadeDistance;
            half _SoftParticlesFarFadeDistance;
            half _CameraNearFadeDistance;
            half _CameraFarFadeDistance;
            float _SoftParticlesEnabled;
            float _CameraFadingEnabled;
            float _AlphaClip;

            Varyings ParticleVert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                output.color = input.color;
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 ParticleFrag(Varyings input) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 color = baseColor * input.color * _BaseColor;

                if (_AlphaClip > 0.5)
                {
                    clip(color.a - _Cutoff);
                }

                half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb;
                color.rgb += emission;

                if (_SoftParticlesEnabled > 0.5)
                {
                    float sceneDepth = LinearEyeDepth(SampleSceneDepth(input.screenPos.xy / input.screenPos.w), _ZBufferParams);
                    float thisDepth = input.screenPos.w;
                    float fade = saturate((sceneDepth - thisDepth - _SoftParticlesNearFadeDistance)
                                         / (_SoftParticlesFarFadeDistance - _SoftParticlesNearFadeDistance + 0.001));
                    color.a *= fade;
                }

                if (_CameraFadingEnabled > 0.5)
                {
                    float distanceToCamera = length(_WorldSpaceCameraPos - TransformObjectToWorld(float3(0,0,0)));
                    float cameraFade = saturate((distanceToCamera - _CameraNearFadeDistance)
                                              / (_CameraFarFadeDistance - _CameraNearFadeDistance + 0.001));
                    color.a *= 1.0 - cameraFade;
                }

                return color;
            }
            ENDHLSL
        }
    }

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
