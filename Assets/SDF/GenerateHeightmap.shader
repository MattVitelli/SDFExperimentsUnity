// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/GenerateHeightmap"
{
	Properties
	{
		_MinBounds("MinBounds", Vector) = (0,0,0,0)
		_MaxBounds("MaxBounds", Vector) = (1,1,1,0)
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
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 worldpos : TEXCOORD0;
			};

			float4 _MinBounds;
			float4 _MaxBounds;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldpos = mul(unity_ObjectToWorld, v.vertex).xyz;
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float3 posInCube = (i.worldpos - _MinBounds.xyz) / max(_MaxBounds.xyz - _MinBounds.xyz, 1.0e-3);
				return posInCube.y;
			}
			ENDCG
		}
	}
}
