Shader "Immersal/LogoShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_NoiseTex01 ("Texture", 2D) = "white" {}
		_NoiseTex02("Texture", 2D) = "white" {}
		_MainColor("_MainColor", Color) = (1,1,1,1)
		_AnimSpeed("Animation Speed", Range(-1.0, 1.0)) = 0.25
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" }
			LOD 100
			Cull Off
			ZWrite Off
			Blend One One
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
				fixed4 color : COLOR;
			};

			struct v2f
			{
				float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				float2 uv2 : TEXCOORD2;
				float3 worldPos : TEXCOORD3;
				half3 worldNormal : TEXCOORD4;
				fixed4 color : COLOR;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _NoiseTex01;
			float4 _NoiseTex01_ST;
			sampler2D _NoiseTex02;
			float4 _NoiseTex02_ST;
			fixed4 _MainColor;
			half _AnimSpeed;
			
			v2f vert(float4 vertex : POSITION, float3 normal : NORMAL, appdata v)
			{
				v2f o;
				float2 offset = float2(0,_Time.y * _AnimSpeed);
				float2 offset2 = float2(0, _Time.y * _AnimSpeed*0.5);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, vertex).xyz;
				o.worldNormal = UnityObjectToWorldNormal(normal);
				o.uv0 = TRANSFORM_TEX(v.uv, _MainTex);
				o.uv1 = TRANSFORM_TEX(v.uv + offset, _NoiseTex01);
				o.uv2 = TRANSFORM_TEX(v.uv + offset2, _NoiseTex01);
				o.color = v.color;
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{ 
				half3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
				fixed4 col = tex2D(_MainTex, i.uv0) * tex2D(_NoiseTex01, i.uv1) * tex2D(_NoiseTex02, i.uv2) * _MainColor * i.color;
				col.a = col.r * col.a;
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
