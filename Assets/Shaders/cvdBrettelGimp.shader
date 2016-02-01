Shader "Custom/cvdBrettelGimp" {
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

			#define GIMP_RGBLMS true
			#define GIMP_ANCHORS true
			#define NEUTRAL_IS_WHITE true

			fixed4 frag (v2f_img i) : SV_Target
			{
				//This is pretty accurate gamma for DK2
				float4 original_color = tex2D(_MainTex, i.uv);
				float4 gamma_uncompressed_color = pow(original_color,2.2f);

				//By measurement of DK2, using cone fundamentals
				float4x4 rgb2lms;
				if(GIMP_RGBLMS){
				  //GIMP original
					rgb2lms = float4x4(0.05059983, 0.08585369, 0.00952420, 0.0,
														 0.01893033, 0.08925308, 0.01370054, 0.0,
														 0.00292202, 0.00975732, 0.07145979, 0.0,
														 0.0,        0.0,        0.0,        1.0);
				} else {
				  //Measured, DK2
					rgb2lms = float4x4(0.10742840, 0.25736297, 0.04052394, 0.0,
														 0.02921477, 0.28452060, 0.06818306, 0.0,
														 0.00002596, 0.00410198, 0.20863832, 0.0,
														 0.0,        0.0,        0.0,        1.0);
				}

				float3 anchor_e;
				if(NEUTRAL_IS_WHITE){
					anchor_e = float3(rgb2lms[0][0]+rgb2lms[0][1]+rgb2lms[0][2],
							rgb2lms[1][0]+rgb2lms[1][1]+rgb2lms[1][2],
							rgb2lms[2][0]+rgb2lms[2][1]+rgb2lms[2][2]);
				} else if(GIMP_ANCHORS) {
					//Use E, for GIMP rgb2lms matrix
					anchor_e = float3(0.13425746, 0.114428, 0.06197968);
				} else {
					//Use E, for DK2 rgb2lms matrix
					//LMS values for equal energy illuminant. Note: NOT white!
					//  E is actually reddish, similar to 5455K black body
					anchor_e = float3(0.294595733, 0.251084745, 0.135999523);
				}

				//LMS values for other anchor colors. Wavelengths 475 and 575 are
				//  used for protan/deutan, 485 and 660 for tritan
				float3 anchor_475;
				float3 anchor_485;
				float3 anchor_575;
				float3 anchor_660;
				if(GIMP_ANCHORS){
					//Values taken from GIMP
					anchor_475 = float3(0.08008, 0.1579,   0.5897);
					anchor_485 = float3(0.1284,  0.2237,   0.3636);
					anchor_575 = float3(0.9856,  0.7325,   0.001079);
					anchor_660 = float3(0.0914,  0.007009, 0.0);
				} else {
					//From Stockman and Share 2-deg cone fundamentals
					//  http://www.cvrl.org/cones.htm
					anchor_475 = float3(0.1188, 0.2054, 0.5164);
					anchor_485 = float3(0.1640, 0.2681, 0.2903);
					anchor_575 = float3(0.9923, 0.7403, 0.0002);
					anchor_660 = float3(0.0930, 0.0073, 0.0000);
				}

				float4 simulation_applied_color = mul(gamma_uncompressed_color,
					transpose(rgb2lms));

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
						simulation_applied_color.g = -(a * simulation_applied_color.r + c
							* simulation_applied_color.b) / b;
						break;
					case 1: //Protan
						//Lq = -(bMq + cSq)/a
						simulation_applied_color.r = -(b * simulation_applied_color.g + c
							* simulation_applied_color.b) / a;
						break;
					case 2://Tritan
						//Sq = -(aLq + bMq)/c
						simulation_applied_color.b = -(a * simulation_applied_color.r + b
							* simulation_applied_color.g) / c;
						break;
				}

				//Calculated in Excel using MINVERSE, from the rgb2lms
				float4x4 lms2rgb;
				if(GIMP_RGBLMS){
					lms2rgb = float4x4(30.830854, -29.832659,  1.610474, 0.0,
														 -6.481468,  17.715578, -2.532642, 0.0,
														 -0.375690,  -1.199062, 14.273846, 0.0,
														  0.0,        0.0,       0.0,      1.0);
				}else{
					lms2rgb = float4x4(12.35148362, -11.19066632,   1.258076675, 0.0,
													   -1.273892622,  4.685491506, -1.283790923, 0.0,
														  0.023508879, -0.090727739,  4.818067155, 0.0,
															0.0,          0.0,          0.0,         1.0);
				}

				simulation_applied_color = pow(mul(simulation_applied_color,
					transpose(lms2rgb)),1.0f/2.2f);

				simulation_applied_color.a = original_color.a;
				return simulation_applied_color;
			}
			ENDCG

		}
	}

	Fallback off
}
