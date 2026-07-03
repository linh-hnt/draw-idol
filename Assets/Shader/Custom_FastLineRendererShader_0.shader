Shader "Custom/FastLineRendererShader" {
	Properties {
		[PerRendererData] _MainTex ("Line Texture (RGB) Alpha (A)", 2D) = "white" {}
		[PerRendererData] _MainTexStartCap ("Line Texture Start Cap (RGB) Alpha (A)", 2D) = "transparent" {}
		[PerRendererData] _MainTexEndCap ("Line Texture End Cap (RGB) Alpha (A)", 2D) = "transparent" {}
		[PerRendererData] _MainTexRoundJoin ("Line Texture Round Join (RGB) Alpha (A)", 2D) = "transparent" {}
		_TintColor ("Tint Color (RGB)", Vector) = (1,1,1,1)
		[PerRendererData] _AnimationSpeed ("Animation Speed (Float)", Float) = 0
		[PerRendererData] _UVXScale ("UV X Scale (Float)", Float) = 1
		[PerRendererData] _UVYScale ("UV Y Scale (Float)", Float) = 1
		[PerRendererData] _GlowIntensityMultiplier ("Glow Intensity (Float)", Float) = 1
		[PerRendererData] _GlowWidthMultiplier ("Glow Width Multiplier (Float)", Float) = 1
		[PerRendererData] _GlowLengthMultiplier ("Glow Length Multiplier (Float)", Float) = 0.33
		[PerRendererData] _AnimationSpeedGlow ("Glow Animation Speed (Float)", Float) = 0
		[PerRendererData] _UVXScaleGlow ("Glow UV X Scale (Float)", Float) = 1
		[PerRendererData] _UVYScaleGlow ("Glow UV Y Scale (Float)", Float) = 1
		[PerRendererData] _InvFade ("Soft Particles Factor", Range(0.01, 3)) = 1
		[PerRendererData] _JitterMultiplier ("Jitter Multiplier (Float)", Float) = 0
		[PerRendererData] _Turbulence ("Turbulence (Float)", Float) = 0
		[PerRendererData] _ScreenRadiusMultiplier ("Screen Radius Multiplier (Float)", Float) = 0
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;
			float4 _MainTex_ST;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Vertex_Stage_Output
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.uv = (input.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			Texture2D<float4> _MainTex;
			SamplerState sampler_MainTex;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(sampler_MainTex, input.uv.xy);
			}

			ENDHLSL
		}
	}
}