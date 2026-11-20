// Legacy Shaders/Diffuse as Unity 4 and earlier drew it. Unity 5 and later
// write alpha 1 for opaque surface shaders; the original wrote the surface's
// own alpha (texture alpha times the colour's alpha), which the contrast
// stretch reads. Otherwise identical to the built-in shader.
//
// _VertexLightDir 1 stands in for the original Diffuse Fast on world objects:
// the light direction is worked out per vertex and its N.L interpolated across
// the face rather than per pixel. That makes no difference for directional
// light, but a spot or point light near a large face (the player's
// VehicleLight on Fort Boxy's castle) lights its middle less. Floor.mat keeps 0.
Shader "Whirld/Diffuse Keep Alpha" {
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_MainTex ("Base (RGB)", 2D) = "white" {}
	_VertexLightDir ("Light direction per vertex", Range(0,1)) = 0
}
SubShader {
	Tags { "RenderType"="Opaque" }
	LOD 200

CGPROGRAM
#pragma surface surf FastLambert keepalpha vertex:vert

sampler2D _MainTex;
fixed4 _Color;
half _VertexLightDir;

struct Input {
	float2 uv_MainTex;
	float vertexNdotL;
};

struct SurfaceOutputFast {
	fixed3 Albedo;
	fixed3 Normal;
	fixed3 Emission;
	half Specular;
	fixed Gloss;
	fixed Alpha;
	half VertexNdotL;
};

void vert (inout appdata_full v, out Input o) {
	UNITY_INITIALIZE_OUTPUT(Input, o);
	float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
	float3 worldNormal = UnityObjectToWorldNormal(v.normal);
	// _WorldSpaceLightPos0.w is 0 for directional light, 1 for spot and point.
	float3 lightDir = normalize(_WorldSpaceLightPos0.xyz - worldPos * _WorldSpaceLightPos0.w);
	o.vertexNdotL = dot(worldNormal, lightDir);
}

void surf (Input IN, inout SurfaceOutputFast o) {
	fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
	o.Albedo = c.rgb;
	o.Alpha = c.a;
	o.VertexNdotL = IN.vertexNdotL;
}

// Lambert, with N.L taken from the vertices when _VertexLightDir is 1.
half4 LightingFastLambert (SurfaceOutputFast s, half3 lightDir, half atten) {
	half ndotl = lerp(dot(s.Normal, lightDir), s.VertexNdotL, _VertexLightDir);
	half4 c;
	c.rgb = s.Albedo * _LightColor0.rgb * (max(0, ndotl) * atten);
	c.a = s.Alpha;
	return c;
}
ENDCG
}

Fallback "Legacy Shaders/VertexLit"
}
