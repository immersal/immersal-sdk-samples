Shader "Immersal/Samples/Additive"
{
	Properties
	{
		_MainTex("Color Gradient Texture", 2D) = "white" {}
		_AlphaTex("Alpha Texture", 2D) = "white" {}

		_Tint("Tint", Color) = (1.0, 1.0, 1.0, 1.0)
		_Intensity("Intensity", Range(0, 5)) = 1.0

		_TrailSpeed("Trail Speed", Range(-1.0, 1.0)) = 0.0
	}
		SubShader
		{
			Tags
			{
				"RenderType" = "Transparent"
				"Queue" = "Transparent"
				"IgnoreProjector" = "True"
			}

			LOD 100
			Lighting Off

			Pass
			{
				Blend One One
				Cull Off
				ZTest LEqual
				ZWrite Off
			//Offset 0, -1

			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD2;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;

				float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD2;

				UNITY_FOG_COORDS(1)
			};

			sampler2D _MainTex;
			sampler2D _AlphaTex;
			float4 _MainTex_ST;
			float4 _AlphaTex_ST;

			fixed4 _Tint;
			half _Intensity;
			half _TrailSpeed;

			v2f vert(appdata v)
			{
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv0 = TRANSFORM_TEX(v.uv0, _AlphaTex);
				o.uv1 = TRANSFORM_TEX(v.uv1, _MainTex);

				UNITY_TRANSFER_FOG(o,o.vertex);

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				half2 uv0_anim = half2(i.uv0.x + _Time.y * _TrailSpeed, i.uv0.y);

				fixed alpha = tex2D(_AlphaTex, uv0_anim);
				fixed4 col = tex2D(_MainTex, i.uv1);

				col *= _Tint * _Intensity * alpha;

				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
}
