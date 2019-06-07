Shader "Immersal/CordShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_MainColor("_MainColor", Color) = (1,1,1,1)
		_FresnelStrength("FresnelStrength", Range(0.0, 1.0)) = 0.75
		_AnimSpeed("Animation Speed", Range(-1.0, 1.0)) = 0.25
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
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
				half3 worldNormal : TEXCOORD2;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _MainColor;
			half _FresnelStrength;
			half _AnimSpeed;
			
			v2f vert(float4 vertex : POSITION, float3 normal : NORMAL, appdata v)
			{
				v2f o;
				float2 offset = float2(0,_Time.y * _AnimSpeed);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, vertex).xyz;
				o.worldNormal = UnityObjectToWorldNormal(normal);
				o.uv = TRANSFORM_TEX(v.uv + offset, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{ 
				half3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
				half rim = max(0, (dot(normalize(worldViewDir), i.worldNormal)));
				half rimPow = rim * _FresnelStrength;
				fixed4 col = tex2D(_MainTex, i.uv) * _MainColor + (1 - rimPow);
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
