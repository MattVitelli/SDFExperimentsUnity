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
		_invDims("Inverse Dimensions", Vector) = (0,0,0,0)
		_lightDir("Light Direction", Vector) = (1,1,1,0)
		_shadowCoeff("Shadow Softness", Float) = 32
		_aoCoeff("Ambient Occlusion", Float) = 1
		_directStrength("Direct Strength", Float) = 1
		_giScaleFactor("GI Scale", Float) = 50
		_ambientStrength("Ambient Strength", Float) = 0.5
		_skyColor("Sky Color", Color) = (1,1,1,1)
		_directColor("Direct Color", Color) = (1,1,1,1)
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

			uniform sampler2D _heightMap;
			uniform sampler3D _sdfMap;

			uniform float4 _encodingRange;
			uniform float4 _invDims;
			uniform float4 _lightDir;
			uniform float _shadowCoeff;
			uniform float4 _minPointCube;
			uniform float4 _maxPointCube;
			uniform float _aoCoeff;
			uniform float4 _skyColor;
			uniform float _giScaleFactor;
			uniform float _directStrength;
			uniform float4 _directColor;
			uniform float _ambientStrength;
			#define _SampleCountGI 128

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

			float3 worldPoint(float3 rO, float3 rD, float mint, float maxt, out float attrib)
			{
				float eps = 1.0e-2;
				float t = mint;
				const int numIters = 15;
				for (int i = 0; i < numIters && t < maxt; i++)
				{
					float h = sampleSDF(rO + rD*t);
					if (h < eps)
					{
						attrib = 1;
						return rO + rD * t;
					}
					t += h;
				}
				attrib = 0;
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

			// Trigonometric function utility
			float2 CosSin(float theta)
			{
				float sn, cs;
				sincos(theta, sn, cs);
				return float2(cs, sn);
			}

			void orthonormal(float3 n, out float3 t, out float3 b)
			{
				t = normalize(n.zxy - dot(n.zxy, n)*n);
				//if (abs(n.x) > abs(n.y))
				//	t = normalize(float3(n.z, 0, -n.x));
				//else
				//	t = normalize(float3(0, -n.z, n.y));
				b = cross(n, t);
			}

			// Pseudo random number generator with 2D coordinates
			float UVRandom(float u, float v)
			{
				float f = dot(float2(12.9898, 78.233), float2(u, v));
				return frac(43758.5453 * sin(f));
			}

			// Interleaved gradient function from Jimenez 2014 http://goo.gl/eomGso
			float GradientNoise(float3 uvw)
			{
				uvw = floor(uvw *_giScaleFactor);
				float f = dot(float3(0.006711056f, 0.00583715f, 0.00483715f), uvw);
				return frac(52.9829189f * frac(f));
			}

			// Sample point picker
			float3 PickSamplePoint(float3 p, float3 n, float3 t, float3 b, float index, out float cosTheta)
			{
				// Uniformaly distributed points on a unit sphere http://goo.gl/X2F1Ho
				//float gn = GradientNoise(p);
				//float u = frac(UVRandom(0, index) + gn);// *2 - 1;
				//float theta = (UVRandom(1, index) + gn) * UNITY_PI * 2;
				float u = (index / sqrt(_SampleCountGI)) / sqrt(_SampleCountGI);
				float theta = UNITY_PI * 2.0 * (index % sqrt(_SampleCountGI)) / sqrt(_SampleCountGI);
				float3 v = float3(CosSin(theta) * sqrt(1 - u * u), u).xzy;
				float3 result = normalize(v.x*b + v.y*n + v.z*t);
				cosTheta = u;
				return result;
			}

			float4 worldColor(float3 rO, float3 rD, float3 L)
			{
				float attrib;
				float3 wp = worldPoint(rO, rD, 0.01, 3.0, attrib);
				float3 wn = worldNormal(wp);
				float d = sampleSDF(wp);
				float4 directLight = _directColor*_directStrength*worldShadow(wp, normalize(L*_invDims), 0.01, 5.0) *saturate(dot(wn, L));
				float indirectLight = _ambientStrength*saturate(pow(d, _aoCoeff));
				//float4 indirectLight = worldLighting2(wp, wn, L);
				//if we were a more complex GI system,
				//we'd do another recursive call in place of the ambient term
				//e.g. indirectLight = worldLighting(wp, wn, L);
				//but this proves very expensive, and our sampling scheme isn't fantastic
				//so bad aliasing will definitely occur
				float4 col = directLight + indirectLight;
				return lerp(_skyColor, col, attrib);
			}
			
			float4 worldLighting(float3 wp, float3 wn, float3 L)
			{
				float4 indirectLight = 0.0;
				float sampleDenom = (2 * UNITY_PI) / _SampleCountGI;
				float3 t, b; orthonormal(wn, t, b);
				float3 sampPoint = wp + wn * _invDims;
				for (int s = 0; s < _SampleCountGI; s++)
				{
					float cosTheta;
					float3 n = PickSamplePoint(wp / _invDims, wn, t, b, s, cosTheta);
					indirectLight += worldColor(sampPoint, n, L) * cosTheta;
				}
				indirectLight *= sampleDenom;
				float4 directLight = _directColor*_directStrength*worldShadow(wp, normalize(L*_invDims), 0.01, 5.0) * saturate(dot(wn, L));
				return directLight + indirectLight;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float2 uv = i.uv;
				float3 N = i.normal;
				float3 uv3 = i.cubeUV +N * _invDims;
				float3 L = -normalize(_lightDir.xyz);
				float4 col = worldLighting(uv3, N, L);
				return col;
			}
			ENDCG
		}
	}
}
