Shader "Immersal/Samples/AdditiveFalloff"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_RandomTex("Random Color Texture", 2D) = "white" {}

		_TintInner("Incident Angle Color", Color) = (0.0, 0.0, 0.0)
		_TintOuter("Grazing Angle Color", Color) = (0.9, 0.1, 0.4)
		_Intensity("Intensity", Range(0, 5)) = 1.0
		_K("Fresnel k", Range(0, 8)) = 1.35

		_Displacement("Displacement Strength", Range(0, 0.1)) = 0.02
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
				Cull Back
				ZTest LEqual
				ZWrite Off
				Offset 0, -1

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
				half3 N : NORMAL;
				fixed4 Cd : COLOR;
				float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD2;
				fixed fresnel : TEXCOORD3;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD2;

				fixed fresnel : TEXCOORD3;

				UNITY_FOG_COORDS(1)
			};

			sampler2D _MainTex;
			sampler2D _RandomTex;
			float4 _MainTex_ST;
			float4 _RandomTex_ST;

			fixed4 _TintInner;
			fixed4 _TintOuter;
			half _Intensity;
			half _K;

			fixed _Displacement;

			fixed Fresnel(float3 worldP, fixed3 N, half k)
			{
				fixed3 I = normalize(worldP - _WorldSpaceCameraPos);
				fixed3 R = reflect(I, N);
				fixed3 H = normalize((N + R) * 0.5);

				fixed fresnel = pow(1.0 - max(0.0, dot(H, -I)), k);
				return fresnel;
			}

			v2f vert(appdata v)
			{
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);

				fixed displacement = (sin(_Time.y + v.Cd) + 1) * 0.5 * _Displacement;

				o.vertex = UnityObjectToClipPos(v.vertex + v.N * displacement);
				//o.vertex = UnityObjectToClipPos(v.vertex);
				fixed3 N = UnityObjectToWorldNormal(v.N);

				o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
				o.uv1 = TRANSFORM_TEX(v.uv1, _RandomTex);

				UNITY_TRANSFER_FOG(o, o.vertex);

				o.fresnel = Fresnel(mul(unity_ObjectToWorld, v.vertex).xyz, N, _K);

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 random = tex2D(_RandomTex, i.uv1);
				fixed4 tex = tex2D(_MainTex, i.uv0);

				fixed4 col = lerp(_TintInner, _TintOuter, i.fresnel) * tex * random * _Intensity;

				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
		}
			FallBack "Diffuse"
}
