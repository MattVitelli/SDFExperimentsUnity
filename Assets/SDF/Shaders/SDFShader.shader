// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/SDFShader"
{
	Properties
	{
		_Rect0("Rect0", Vector) = (0,0,1,1)
		_Light("Light", Vector) = (0.5,1,0,0)
		_AOFactor("Ambient Occlusion", Range(0,1)) = 0.2
		_MainTex ("Texture", 2D) = "white" {}
		_shadowK("Shadow Factor", Float) = 1
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
				float3 wp : TEXCOORD1;
				float3 wn : TEXCOORD2;
			};
			uniform float4 _Rect0;

			float sdBox2D(float2 p, float4 b)
			{
				float2 d = abs(p - b.xy) - b.zw;
				return min(max(d.x, d.y), 0.0) + length(max(d, 0.0));
			}

			float distFn(float2 p, out float attrib)
			{
				float d = sdBox2D(p.xy, _Rect0);
				if (d < 0.0)
					attrib = 1.0;
				else
					attrib = 0.0;
				return d;
			}

			float heightFn(float2 pt)
			{
				float attrib;
				float d = distFn(pt, attrib);
				if (d <= 0.0)
				{
					return 1.0;
				}
				else
				{
					return 0.0;
				}
			}

			float distFn3D(float3 p, out float attrib)
			{
				float d = distFn(p.xz, attrib);
				if (d < 0.0)
					attrib = 1.0;
				else
					attrib = 0.0;
				return d;
			}

			float worldIntersection(float3 ro, float3 rd)
			{
				float attrib;
				float d = distFn(ro.xz, attrib);
				float dOrig = d;
				int raySamples = 24;
				const float EPS = 1.0e-4;
				float3 origPt = ro;
				float h = heightFn(ro.xz);
				for (int i = 0; i<raySamples && abs(d) > EPS && (origPt.y >= h); i++)
				{
					ro.xz += rd.xz*d;
					d = distFn(ro.xz, attrib);
					h = heightFn(ro.xz);
				}
				
				return length(ro.xz - origPt.xz);
			}

			float worldShadow(float3 ro, float3 rd, float k)
			{
				float attrib;
				int raySamples = 24;
				const float EPS = 1.0e-4;// 4;
				float t = 0;
				float res = 1.0;
				for (int i = 0; i<raySamples; i++)
				{
					float3 intersection = ro + rd * t;
					float h = distFn(intersection.xz, attrib);
					if (h < EPS && intersection.y < heightFn(intersection.xz))
						return 0.0;
					res = min(res, k*abs(h) / t);
					t += h;
				}
				return res;
			}

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Light;
			float _shadowK;
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				o.wp = mul(unity_ObjectToWorld, v.vertex);
				o.wn = normalize(mul(unity_ObjectToWorld, float4(v.normal,0.0)).xyz);
				return o;
			}
			
			uniform float _AOFactor;
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				
				float2 uv = i.wp.xz;
				float attrib;
				float d = distFn(uv, attrib);

				float3 lDir = normalize(_Light.xyz);

				float h = heightFn(uv);
				float finalColor = 0.75;
				float aoTerm = saturate(pow(distFn(uv, attrib), _AOFactor));

				float3 wp = float3(uv.x, h, uv.y);
				float3 wn = normalize(i.wn);
				float lPlane = 1.0e-3;
				float3 shadowPos = float3(uv.x, h, uv.y);
				float shade = worldShadow(shadowPos + wn * lPlane, lDir, _shadowK);

				if (d <= 0.0)
				{
					finalColor = 1.0;
				}
				else
				{
					finalColor *= aoTerm*shade;
				}
				finalColor = shade;
				//return float4(shadowPos,1.0);
				return finalColor;
			}
			ENDCG
		}
	}
}
