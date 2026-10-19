// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'PositionFog()' with multiply of UNITY_MATRIX_MVP by position
// Upgrade NOTE: replaced 'V2F_POS_FOG' with 'float4 pos : SV_POSITION'

Shader "Transparent/GlassGlowing" {
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_FColor ("Fallback Color", Color) = (1,1,1,1)
	_SpecColor ("Specular Color", Color) = (0.5,0.5,0.5,1)
	_Cube ("Reflection Cubemap", Cube) = "_Skybox" {TexGen CubeNormal}
}

Category {
	Tags {Queue=Transparent}
	ZWrite Off
	Blend SrcAlpha OneMinusSrcAlpha
	Lighting Off
	Colormask RGB

	SubShader {
		Pass {
			
CGPROGRAM
// Unity 2 directives ("// vertex vert") replaced with pragmas; without them no programs compiled.
#pragma vertex vert
#pragma fragment frag
#pragma fragmentoption ARB_precision_hint_fastest
#include "UnityCG.cginc"

struct v2f {
	float4 pos : SV_POSITION;
	float3  normal	: TEXCOORD0;
	float3	viewDir	: TEXCOORD1;
};

v2f vert (appdata_base v)
{	
	v2f o;
	o.pos = UnityObjectToClipPos (v.vertex);
	o.normal = mul( (float3x3)unity_ObjectToWorld, v.normal );
	o.viewDir = mul( (float3x3)unity_ObjectToWorld, ObjSpaceViewDir(v.vertex) );
	
	return o;
}

uniform samplerCUBE _Cube;
uniform float4 _Color;
uniform float4 _SpecColor;

float4 frag (v2f i)  : SV_Target
{
	float3 normal = normalize(i.normal);
	i.viewDir = normalize(i.viewDir);
	half nsv = saturate(dot( normal, i.viewDir ));
	
	// calculate reflection vector in world space
	half3 r = reflect(-i.viewDir, normal);
	
	half4 reflcolor = texCUBE(_Cube, r);
	
	half fresnel = .9 - nsv*0.5;
	half3 c = lerp( _Color.rgb, reflcolor.rgb+_SpecColor.rgb, fresnel );
	
	return half4( c, fresnel );
}
ENDCG  
		}
	}
	
	SubShader {
		Pass {
			Color [_FColor]
		}
	}
	
	
}

}
