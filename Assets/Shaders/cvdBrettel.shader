Shader "Custom/cvdBrettel" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
	_CVDType ("CVD Type", Int) = 0
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
int _CVDType;

fixed4 frag (v2f_img i) : SV_Target
{
	float4 original_color = tex2D(_MainTex, i.uv);
	float4 gamma_uncompressed_color = pow(original_color,2.2f); //This is pretty accurate gamma for DK2

	//By measurement of DK2, using cone fundamentals
	float4x4 rgb2lms = float4x4(0.10742840, 0.25736297, 0.04052394, 0.0,
							    0.02921477, 0.28452060, 0.06818306, 0.0,
							    0.00002596, 0.00410198, 0.20863832, 0.0,
								0.0,    0.0,    0.0,    1.0);
	
	float3 anchor_e = float3(0.294595733, 0.251084745, 0.135999523); //LMS values for equal energy illuminant. Note: NOT white! E is actually reddish, similar to 5455K
	
	//LMS values for other anchor colors. Wavelengths 475 and 575 are used for protan/deutan, 485 and 660 for tritan
	//Values are taken from the Stockman and Sharpe 2-deg cone fundamentals from http://www.cvrl.org/cones.htm
	//Note that the values used widely across the internet (e.g. in GIMP) don't seem to match any cone fundamentals that I can
	// find online.
	float3 anchor_475 = float3(0.1188, 0.2054, 0.5164);
	float3 anchor_485 = float3(0.1640, 0.2681, 0.2903);
	float3 anchor_575 = float3(0.9923, 0.7403, 0.0002);
	float3 anchor_660 = float3(0.0930, 0.0073, 0.0000);

	float4 simulation_applied_color = mul(gamma_uncompressed_color,transpose(rgb2lms)); 

	//This is for Deutan. See Brettel et al for other versions
	float Q_ratio;
	float E_ratio;

	switch(_CVDType){
	case 0: //Deutan
	default:
		Q_ratio = simulation_applied_color.b / simulation_applied_color.r;
		E_ratio = anchor_e.z/anchor_e.x;
		break;
	case 1: //Protan
		Q_ratio = simulation_applied_color.b / simulation_applied_color.g;
		E_ratio = anchor_e.z/anchor_e.y;
		break;
	case 2://Tritan
		Q_ratio = simulation_applied_color.g / simulation_applied_color.r;
		E_ratio = anchor_e.y/anchor_e.x;
		break;
	}

	float3 A;
	switch(_CVDType){
	case 0:
	case 1:
	default:
		A = (Q_ratio < E_ratio) ? anchor_575 : anchor_475;
		break;
	case 2:
		A = (Q_ratio < E_ratio) ? anchor_660 : anchor_485;
		break;
	}

	float a = anchor_e.y*A.z - anchor_e.z*A.y; //MeSa - SeMa
	float b = anchor_e.z*A.x - anchor_e.x*A.z; //SeLa - LeSa
	float c = anchor_e.x*A.y - anchor_e.y*A.x; //LeMa - MeLa

	switch(_CVDType){
	case 0: //Deutan
	default:
		//Mq = -(aLq + cSq)/b
		simulation_applied_color.g = -(a * simulation_applied_color.r + c * simulation_applied_color.b) / b;
		break;
	case 1: //Protan
		//Lq = -(bMq + cSq)/a
		simulation_applied_color.r = -(b * simulation_applied_color.g + c * simulation_applied_color.b) / a;
		break;
	case 2://Tritan
		//Sq = -(aLq + bMq)/c
		simulation_applied_color.b = -(a * simulation_applied_color.r + b * simulation_applied_color.g) / c;
		break;
	}

	//Calculated in Excel using MINVERSE, from the rgb2lms
    float4x4 lms2rgb = float4x4(12.35148362, -11.19066632,   1.258076675, 0.0,
								-1.273892622,  4.685491506, -1.283790923, 0.0,
								 0.023508879, -0.090727739,  4.818067155, 0.0,
								 0.0,     0.0,     0.0,    1.0);

	simulation_applied_color = pow(mul(simulation_applied_color,transpose(lms2rgb)),1.0f/2.2f);

	simulation_applied_color.a = original_color.a;
	return simulation_applied_color;
}
ENDCG

	}
}

Fallback off

}