// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Custom/MyShader"
{
	Properties
	{
		_minPointCube ("MinCube", Vector) = (0,0,0,0)
		_maxPointCube ("MaxCube", Vector) = (1,1,1,1)
		_heightMap ("Height Image", 2D) = "white" {}
		_sdfMap("SDF Image", 3D) = "blue" {}
		_encodingRange("Encoding Range", Vector) = (0,0,0,0)
		_sliceIndex("Slice Index", Range(0, 1)) = 0
		_lerpAmount("Lerp Amount", Range(0,1)) = 0
		_invDims("Inverse Dimensions", Vector) = (0,0,0,0)
		_lightDir("Light Direction", Vector) = (1,1,1,0)
		_shadowCoeff("Shadow Softness", Float) = 32
		_aoCoeff("Ambient Occlusion", Float) = 1
		_aoBias("Ambient Occlusion Bias", Vector) = (0.2, 0.5, 0, 0)
		_giRadius("GI Radius", Float) = 1
			_directStrength("Direct Strength", Float) = 1
			_giScaleFactor("GI Scale", Float) = 50
			_ambientStrength("Ambient Strength", Float) = 0.5
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				float3 rayDir : TEXCOORD1;
				float3 rayO	  : TEXCOORD2;
				float3 cubeUV : TEXCOORD3;
				float3 normal : TEXCOORD4;
			};

			sampler2D _heightMap;
			sampler3D _sdfMap;

			float4 _encodingRange;
			float _sliceIndex;
			float _lerpAmount;
			float4 _MainTex_ST;
			float4 _invDims;
			float4 _lightDir;
			float _shadowCoeff;
			float4 _minPointCube;
			float4 _maxPointCube;
			float _aoCoeff;
			float4 _aoBias;
			
			float _giScaleFactor;

			float sampleSDF(float3 wp)
			{
				float4 samp = tex3D(_sdfMap, wp);
				return samp.x;
				
				//float result = dot(samp, float4(1.0, 1 / 255.0, 1 / 65025.0, 1 / 160581375.0));
				//result = result * _encodingRange.y + _encodingRange.x;
				//return result;
			}
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;// TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				float3 toCamera = normalize(worldPos.xyz - _WorldSpaceCameraPos.xyz);
				o.rayDir = mul(unity_WorldToObject, float4(toCamera,0)).xyz;
				o.rayO = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1)).xyz;
				o.cubeUV = (worldPos - _minPointCube) / max(_maxPointCube - _minPointCube, 1.0e-3);
				o.normal = normalize(mul(unity_ObjectToWorld, float4(v.normal, 0)).xyz);
				return o;
			}

			float3 worldPoint(float3 rO, float3 rD, float mint, float maxt)
			{
				float eps = 1.0e-2;
				float t = mint;
				const int numIters = 15;
				for (int i = 0; i < numIters && t < maxt; i++)
				{
					float h = sampleSDF(rO + rD*t);
					if (h<eps)
						return rO + rD * t;
					t += h;
				}
				return float3(-1,-1,-1);
			}

			float3 worldNormal(float3 rO)
			{
				float3 n;
				float3 eps = _invDims;
				n.x = sampleSDF(rO + float3(eps.x, 0, 0)) - sampleSDF(rO - float3(eps.x, 0, 0));
				n.y = sampleSDF(rO + float3(0, eps.y, 0)) - sampleSDF(rO - float3(0, eps.y, 0));
				n.z = sampleSDF(rO + float3(0, 0, eps.z)) - sampleSDF(rO - float3(0, 0, eps.z));
				return normalize(n);
			}

			float worldShadow(float3 rO, float3 rD, float mint, float maxt)
			{
				float res = 1.0;
				float eps = 1.0e-2;
				float t = mint;
				const int numIters = 15;
				for (int i = 0; i < numIters && t < maxt; i++)
				{
					float h = sampleSDF(rO + rD*t);
					if (h<eps)
						return 0.0;
					res = min(res, _shadowCoeff*h / t);
					t += h;
				}
				return res;
			}

			const int _GIBounces = 2;
			float _giRadius;
			float _directStrength = 1.2;
			float _ambientStrength = 0.9;

			// Trigonometric function utility
			float2 CosSin(float theta)
			{
				float sn, cs;
				sincos(theta, sn, cs);
				return float2(cs, sn);
			}

			// Pseudo random number generator with 2D coordinates
			float UVRandom(float u, float v)
			{
				float f = dot(float2(12.9898, 78.233), float2(u, v));
				return frac(43758.5453 * sin(f));
			}

			// Interleaved gradient function from Jimenez 2014 http://goo.gl/eomGso
			float GradientNoise(float2 uv)
			{
				uv = floor(uv);// *_ScreenParams.xy);
				float f = dot(float2(0.06711056f, 0.00583715f), uv);
				return frac(52.9829189f * frac(f));
			}

			// Sample point picker
			float3 PickSamplePoint(float2 uv, float index, float _SampleCountGI)
			{
				// Uniformaly distributed points on a unit sphere http://goo.gl/X2F1Ho
				float gn = GradientNoise(uv);
				float u = frac(UVRandom(0, index)) * 2 - 1;
				float theta = (UVRandom(1, index)) * UNITY_PI * 2;

				//float u = UVRandom(uv.x + _Time.x, uv.y + index) * 2 - 1;
				//float theta = UVRandom(-uv.x - _Time.x, uv.y + index) * UNITY_PI * 2;

				float3 v = float3(CosSin(theta) * sqrt(1 - u * u), u);
				// Make them distributed between [0, _Radius]
				float l = sqrt((index + 1) / _SampleCountGI) * _giRadius;
				return normalize(v * l);
			}

			float4 worldColor3(float3 rO, float3 rD, float3 L)
			{
				float3 wp = worldPoint(rO, rD, 0.01, 3.0);
				float3 wn = worldNormal(wp);
				float d = sampleSDF(wp);
				float shadow = _directStrength*worldShadow(wp, normalize(L*_invDims), 0.01, 5.0) *saturate(dot(wn, L));
				float ambient = _ambientStrength*saturate(pow((d + _aoBias.x)*_aoBias.y, _aoCoeff)*_aoBias.z);
				float4 col = 1.0;
				col.rgb = shadow + ambient;// worldColor(wp, wn, L);
				return col;
			}

			float4 worldGI3(float3 wp, float3 wn, float3 L)
			{
				float4 gi = 0.0;
				const int _SampleCountGI = 24;
				for (int s = 0; s < _SampleCountGI; s++)
				{
					float3 rd = PickSamplePoint((wp + wn) * _giScaleFactor, s, _SampleCountGI);
					float3 n = faceforward(rd, -wn, rd);
					gi += worldColor3(wp + wn * _invDims, n, L);
				}
				return gi / _SampleCountGI;
			}

			float4 worldColor2(float3 rO, float3 rD, float3 L)
			{
				float3 wp = worldPoint(rO, rD, 0.01, 3.0);
				float3 wn = worldNormal(wp);
				float d = sampleSDF(wp);
				float shadow = _directStrength*worldShadow(wp, normalize(L*_invDims), 0.01, 5.0) *saturate(dot(wn, L));
				float ambient = _ambientStrength*saturate(pow((d + _aoBias.x)*_aoBias.y, _aoCoeff)*_aoBias.z);
				float4 col = 1.0;
				col.rgb = shadow + ambient;// worldGI3(wp, wn, L);
				return col;
			}

			float4 worldGI2(float3 wp, float3 wn, float3 L)
			{
				float4 gi = 0.0;
				const int _SampleCountGI = 24;
				for (int s = 0; s < _SampleCountGI; s++)
				{
					float3 rd = PickSamplePoint((wp + wn) * _giScaleFactor, s, _SampleCountGI);
					float3 n = faceforward(rd, -wn, rd);
					gi += worldColor2(wp + wn * _invDims, n, L);
				}
				return gi / _SampleCountGI;
			}

			float4 worldColor(float3 rO, float3 rD, float3 L)
			{
				float3 wp = worldPoint(rO, rD, 0.01, 3.0);
				float3 wn = worldNormal(wp);
				float d = sampleSDF(wp);
				float shadow = _directStrength*worldShadow(wp, normalize(L*_invDims), 0.01, 5.0) *saturate(dot(wn, L));
				float ambient = 0.9*saturate(pow((d + _aoBias.x)*_aoBias.y, _aoCoeff)*_aoBias.z);
				float4 col = 1.0;
				col.rgb = shadow + worldGI2(wp, wn, L);
				return col;
			}
			
			float4 worldGI(float3 wp, float3 wn, float3 L)
			{
				float4 gi = 0.0;
				const int _SampleCountGI = 24;
				for (int s = 0; s < _SampleCountGI; s++)
				{
					float3 rd = PickSamplePoint((wp + wn)*_giScaleFactor, s, _SampleCountGI);
					float3 n = faceforward(rd, -wn, rd);
					gi += worldColor(wp + wn * _invDims, n, L);
				}
				return gi / _SampleCountGI;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float2 uv = i.uv;
				//float3 uv3 = float3(i.uv.x, _sliceIndex, i.uv.y);
				float3 N = i.normal;
				float3 uv3 = i.cubeUV + N * _invDims;
				// sample the texture
				//fixed4 col = tex2D(_MainTex, i.uv);
				// apply fog
				//UNITY_APPLY_FOG(i.fogCoord, col);
				float3 rD = normalize(i.rayDir);
				float3 rO = i.rayO;
				
				rO *= 0.5 + 0.5;// _invDims.xyz;
				
				float4 col = 1;
				float h = tex2D(_heightMap, uv);
				float d = sampleSDF(uv3);
				col = lerp(h, d, _lerpAmount);
				float3 L = -normalize(_lightDir.xyz);
				float shadow = _directStrength*worldShadow(uv3, normalize(L*_invDims), 0.01, 5.0) * saturate(dot(N, L));
				float ambient = 0.9*saturate(pow((d + _aoBias.x)*_aoBias.y, _aoCoeff)*_aoBias.z);
				col.rgb = shadow + worldGI(uv3, N, L);// *ambient;
				//col.rgb = abs(worldGI(uv3, N, L).rgb);
				//col.rgb = abs(uv3);
				return col;
			}
			ENDCG
		}
	}
}
