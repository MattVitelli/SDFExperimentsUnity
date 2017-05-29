Shader "Unlit/DebugCS"
{
	Properties
	{
		_hmTex("Height Texture", 2D) = "white" {}
		_cmpTex("Compare Texture", 3D) = "white" {}
		_width("Width", Int) = 1
		_depth("Depth", Int) = 1
		_height("Height", Int) = 1
		_texWidth("TexWidth", Int) = 1
		_texHeight("TexHeight", Int) = 1
		_lerpAmount("Lerp Amount", Range(0,1)) = 0
		_sliceIndex("Slice Amount", Range(0,1)) = 0
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
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _hmTex;
			sampler3D _cmpTex;
			uint _width;
			uint _height;
			uint _depth;
			uint _texWidth;
			uint _texHeight;
			float _lerpAmount;
			float _sliceIndex;
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				uint2 id = (uint2)(i.uv*float2(_texWidth, _texHeight));
				uint texIdx = id.x + id.y * _texWidth;
				uint3 cellIdx = uint3(texIdx % _width, (texIdx / _width) % _height, (texIdx / (_width * _height)));
				float4 result = float4((float)cellIdx.x / _width, (float)cellIdx.y / _height, (float)cellIdx.z / _depth, 1.0);
				float4 resultTx = 0;// tex2D(_cmpTex, i.uv);
				if (texIdx > _width*_height*_depth)
				{
					result.a = 0;
				}
				float4 h = tex3D(_cmpTex, float3(i.uv.x, _sliceIndex, i.uv.y));
				return h;// lerp(result, resultTx, _lerpAmount).a;
			}
			ENDCG
		}
	}
}
