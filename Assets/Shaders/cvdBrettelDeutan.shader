Shader "Custom/cvdBrettelDeutan" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
}

SubShader {
	Pass {
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }
				
CGPROGRAM
#pragma vertex vert_img
#pragma fragment frag
#pragma fragmentoption ARB_precision_hint_fastest 
#pragma target 3.0
#include "UnityCG.cginc"

uniform sampler2D _MainTex;

fixed4 frag (v2f_img i) : SV_Target
{
	//Very simple gamma correction model for now
	float4 original = tex2D(_MainTex, i.uv);
	float4 cvp_applied_color = pow(original,2.2f);

	//TODO: check rgb2lms for DK2 
	float4x4 rgb2lms = float4x4(1.3107,0.3678,0.0012,0.0,2.9208,3.2065,0.0722,0.0,0.3118,0.5091,2.2731,0.0,0.0, 0.0, 0.0, 1.0);

	float4 simulation_applied_color = mul(cvp_applied_color,rgb2lms); 

	float tmp = simulation_applied_color.b / simulation_applied_color.r;

	if(tmp < 0.5055687428) {
    	simulation_applied_color.g = -(-1.6784703732 * simulation_applied_color.r + -0.3340041637 * simulation_applied_color.b) / 2.2589225769;
	} else {
    	simulation_applied_color.g = -(1.8282908201 * simulation_applied_color.r + 0.4198420346 * simulation_applied_color.b) / -2.4951891899;
	}

    float4x4 lms2rgb = float4x4(1.0255000591,-0.1181000024,0.0031999999,0.0,-0.9355999827,0.4212000072,-0.0129000004,0.0,0.0688999966,-0.0781000033,0.4424000084,0.0,0.0, 0.0, 0.0, 1.0);

	simulation_applied_color = pow(mul(simulation_applied_color,lms2rgb),1.0f/2.2f);

	simulation_applied_color.a = original.a;
	return simulation_applied_color;
}
ENDCG

	}
}

Fallback off

}