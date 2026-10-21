// Upgrade NOTE: replaced 'samplerRECT' with 'sampler2D'
// Upgrade NOTE: replaced 'texRECT' with 'tex2D'

// Outputs luminance (grayscale) of the input image _MainTex

Shader "Hidden/Contrast Stretch Luminance" {
	
Properties {
	_MainTex ("Base (RGB)", RECT) = "white" {}
}

Category {
	SubShader {
		Pass {
			ZTest Always Cull Off ZWrite Off
			Fog { Mode off }
				
CGPROGRAM
#pragma vertex vert_img
#pragma fragment frag
#pragma fragmentoption ARB_precision_hint_fastest 
#include "UnityCG.cginc"

uniform sampler2D _MainTex;

float4 frag (v2f_img i) : COLOR
{
	float4 col = tex2D(_MainTex, i.uv);
	// The original weighted this by (1 + 2 * alpha). Unity 5 and later write
	// alpha 1 for every opaque surface, which tripled the floor's luminance,
	// pinned the measured maximum at 1 and all but disabled the stretch.
	// Without the weighting the stretch is close to the original's.
	col.rgb = Luminance(col.rgb);
	return col;
}
ENDCG

		}
	}
}

Fallback off

}