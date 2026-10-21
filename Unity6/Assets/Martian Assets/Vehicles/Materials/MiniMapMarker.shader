// Flat colour drawn over everything, for the minimap markers and the green
// HUD arrow. The original was a fixed-function pass ("Color [_Color]"), which
// Unity 6 on Metal draws white; this is the same output as a vertex/fragment
// shader. Culling is off because the recovered marker mesh's winding is
// inferred; a flat-coloured silhouette looks the same either way.
Shader "FX/MiniMapMarker" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,0)
	}
	SubShader {
		ZTest Always
		Cull Off
		Tags {"Queue" = "Overlay" }
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			fixed4 _Color;
			float4 vert (float4 vertex : POSITION) : SV_POSITION { return UnityObjectToClipPos(vertex); }
			fixed4 frag () : SV_Target { return _Color; }
			ENDCG
		}
	}
}
