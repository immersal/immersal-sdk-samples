Shader "Immersal/Samples/Falloff"
{
	Properties
	{
		_TintOuter("Grazing Angle Color", Color) = (0.9, 0.1, 0.4)
		_K("Fresnel k", Range(0, 8)) = 1.35
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
			Blend SrcAlpha OneMinusSrcAlpha
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
				fixed fresnel : TEXCOORD3;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				fixed fresnel : TEXCOORD3;

				UNITY_FOG_COORDS(1)
			};

			fixed4 _TintOuter;
			half _K;

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

				o.vertex = UnityObjectToClipPos(v.vertex);
				fixed3 N = UnityObjectToWorldNormal(v.N);

				UNITY_TRANSFER_FOG(o, o.vertex);

				o.fresnel = Fresnel(mul(unity_ObjectToWorld, v.vertex).xyz, N, _K);

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 transparent = fixed4(0.0, 0.0, 0.0, 0.0);
				fixed4 col = lerp(transparent, _TintOuter, i.fresnel);

				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}
