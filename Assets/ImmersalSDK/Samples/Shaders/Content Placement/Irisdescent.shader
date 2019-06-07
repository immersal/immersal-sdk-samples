Shader "Immersal/Samples/Irisdescent"
{
	Properties
	{
		_DirectSpecular("Direct Specular Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_DirectGlossiness("Direct Specular Glossiness", Range(1, 50)) = 10

		_IndirectSpecular("indirect Specular Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_IndirectGlossiness("Indirect Specular Glossiness", Range(0, 1)) = 0.5
		_FresnelExponent("Fresnel Exponent", Range(0, 10)) = 1.0

		_MainTex("Glossiness Texture", 2D) = "white" {}
		[NoScaleOffset]
		_IBLTexCube("IBL Cubemap", Cube) = "black" {}
	}

		SubShader
	{
		Tags
		{
			"LightMode" = "ForwardBase"
		}

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
				float4 P : POSITION;
				half3 N : NORMAL;
				half2 uv0 : TEXCOORD0;
			};

			struct v2f
			{
				float4 P : SV_POSITION;
				half3 N : NORMAL;
				half2 uv0 : TEXCOORD0;
				fixed3 Cd : COLOR;
				fixed3 I : TEXCOORD1;
				fixed3 R : TEXCOORD2;
				fixed3 L : TEXCOORD3;
				fixed falloff : TEXCOORD4;

				UNITY_FOG_COORDS(5)
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			samplerCUBE _IBLTexCube;

			fixed3 _IndirectSpecular;
			fixed _IndirectGlossiness;
			fixed3 _DirectSpecular;
			fixed _DirectGlossiness;
			half _FresnelExponent;

			#define GLOSSY_MIP_COUNT 6

			fixed3 SampleTexCube(samplerCUBE cube, half3 normal, half mip)
			{
				return texCUBElod(cube, half4(normal, mip));
			}

			v2f vert(appdata v)
			{
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);

				o.P = UnityObjectToClipPos(v.P);
				o.N = UnityObjectToWorldNormal(v.N);
				o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);

				float3 worldPos = mul(unity_ObjectToWorld, v.P).xyz;

				o.I = normalize(worldPos - _WorldSpaceCameraPos);
				o.R = reflect(o.I, o.N);
				o.L = reflect(_WorldSpaceLightPos0, o.N);

				half3 H = normalize((o.N + o.R) * 0.5);
				fixed mix = pow(1 - 0 - max(0.0, dot(H, -o.I)), _FresnelExponent);
				o.falloff = lerp(0.2, 1.0, mix);

				UNITY_TRANSFER_FOG(o, o.P);

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed glossiness = 1.0 - (tex2D(_MainTex, i.uv0).a * _IndirectGlossiness);
				fixed3 directSpecular = pow(max(0.0, dot(i.L, i.I)), _DirectGlossiness) * _DirectSpecular * i.falloff;
				fixed3 indirectSpecular = SampleTexCube(_IBLTexCube, i.R, glossiness * GLOSSY_MIP_COUNT) * _IndirectSpecular * i.falloff;

				fixed edges = pow(1.0 - max(dot(-i.I, i.N), 0.0), 3.2);

				fixed4 color = 1.0;
				color.rgb = indirectSpecular + directSpecular + (_DirectSpecular * edges);

				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, color);

				return color;
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}