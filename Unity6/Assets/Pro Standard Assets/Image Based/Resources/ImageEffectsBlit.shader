// Blit material used by ImageEffects.Blit. Previously compiled at runtime from
// shader source strings (one shader per BlendMode); the blend factors are now
// material properties so one asset covers every mode.
Shader "Hidden/ImageEffectsBlit" {
	Properties {
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Float) = 0
	}
	SubShader { Pass {
		Blend [_SrcBlend] [_DstBlend]
		ZTest Always Cull Off ZWrite Off Fog { Mode Off }
		SetTexture [__RenderTex] { combine texture }
	}}
	Fallback Off
}
