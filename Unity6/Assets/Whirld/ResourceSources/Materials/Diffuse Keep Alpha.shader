// Legacy Shaders/Diffuse as Unity 4 and earlier drew it. Unity 5 and later
// write alpha 1 for opaque surface shaders; the original wrote the surface's
// own alpha (texture alpha times the colour's alpha), which the contrast
// stretch reads. Otherwise identical to the built-in shader.
Shader "Whirld/Diffuse Keep Alpha" {
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_MainTex ("Base (RGB)", 2D) = "white" {}
}
SubShader {
	Tags { "RenderType"="Opaque" }
	LOD 200

CGPROGRAM
#pragma surface surf Lambert keepalpha

sampler2D _MainTex;
fixed4 _Color;

struct Input {
	float2 uv_MainTex;
};

void surf (Input IN, inout SurfaceOutput o) {
	fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
	o.Albedo = c.rgb;
	o.Alpha = c.a;
}
ENDCG
}

Fallback "Legacy Shaders/VertexLit"
}
