// Legacy Shaders/Reflective/Parallax Specular (Unity 5.0.0f4 built-in source)
// with keepalpha. Unity 5 and later write alpha 1 for opaque surface shaders;
// the original wrote o.Alpha, which the contrast stretch reads.
Shader "Martian/Reflective Parallax Specular Keep Alpha" {
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_SpecColor ("Specular Color", Color) = (0.5,0.5,0.5,1)
	_Shininess ("Shininess", Range (0.01, 1)) = 0.078125
	_ReflectColor ("Reflection Color", Color) = (1,1,1,0.5)
	_Parallax ("Height", Range (0.005, 0.08)) = 0.02
	_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" { }
	_Cube ("Reflection Cubemap", Cube) = "_Skybox" {}
	_BumpMap ("Normalmap", 2D) = "gray" { }
	_ParallaxMap ("Heightmap (A)", 2D) = "black" {}
}
SubShader {
	Tags { "RenderType"="Opaque" }
	LOD 600
	
CGPROGRAM
#pragma surface surf BlinnPhong keepalpha
#pragma target 3.0

sampler2D _MainTex;
sampler2D _BumpMap;
samplerCUBE _Cube;
sampler2D _ParallaxMap;

fixed4 _Color;
fixed4 _ReflectColor;
half _Shininess;
float _Parallax;

struct Input {
	float2 uv_MainTex;
	float2 uv_BumpMap;
	float3 worldRefl;
	float3 viewDir;
	INTERNAL_DATA
};

void surf (Input IN, inout SurfaceOutput o) {
	half h = tex2D (_ParallaxMap, IN.uv_BumpMap).w;
	float2 offset = ParallaxOffset (h, _Parallax, IN.viewDir);
	IN.uv_MainTex += offset;
	IN.uv_BumpMap += offset;
	
	fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
	o.Albedo = tex.rgb * _Color.rgb;
	o.Gloss = tex.a;
	o.Specular = _Shininess;
	
	// Unity 2.6's reflection pass decoded the bump map as rgb * 2 - 1 and
	// its unassigned default to a zero normal, so the reflection followed
	// the view ray. Lighting keeps the geometric normal, as its own pass did.
	half3 reflNormal = tex2D(_BumpMap, IN.uv_BumpMap).rgb * 2 - 1;
	o.Normal = half3(0, 0, 1);

	float3 worldRefl = WorldReflectionVector (IN, reflNormal);
	fixed4 reflcol = texCUBE (_Cube, worldRefl);
	reflcol *= tex.a;
	o.Emission = reflcol.rgb * _ReflectColor.rgb;
	o.Alpha = reflcol.a * _ReflectColor.a;
}
ENDCG
}

FallBack "Legacy Shaders/Reflective/Bumped Specular"
}
