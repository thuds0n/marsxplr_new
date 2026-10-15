// Blur iteration shader used by BlurEffectIsland. Previously compiled at runtime
// from a shader source string.
Shader "Hidden/BlurConeTapIsland" {
	SubShader {
		Pass {
			ZTest Always Cull Off ZWrite Off Fog { Mode Off }
			SetTexture [_RenderTex] {constantColor (0,0,0,0.25) combine texture * constant alpha}
			SetTexture [_RenderTex] {constantColor (0,0,0,0.25) combine texture * constant + previous}
			SetTexture [_RenderTex] {constantColor (0,0,0,0.25) combine texture * constant + previous}
			SetTexture [_RenderTex] {constantColor (0,0,0,0.25) combine texture * constant + previous}
		}
	}
	Fallback off
}
