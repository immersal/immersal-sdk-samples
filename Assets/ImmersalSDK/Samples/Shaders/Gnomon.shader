Shader "Immersal/Axis"
{
	Properties
	{
		_Incidence("Incidence", Range(0, 1.0)) = 0.5
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
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
				float4 tangent : TANGENT;
				float3 normal : NORMAL;
				fixed4 vertColor : COLOR;
			};

			struct v2f
			{
				float3 worldPos : TEXCOORD0;
				half3 worldNormal : TEXCOORD1;
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				UNITY_FOG_COORDS(1)
			};

			float _Incidence;

			v2f vert(float4 vertex : POSITION, float3 normal : NORMAL, appdata v)
			{
				v2f o;
				o.worldPos = mul(unity_ObjectToWorld, vertex).xyz;
				o.worldNormal = UnityObjectToWorldNormal(normal);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color.rgba = v.vertColor.rgba;
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				half3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));

				half rim = max(0, (dot(normalize(worldViewDir), i.worldNormal)));
				half rimPow = pow(rim, 2) * _Incidence;

				fixed4 col = i.color.rgba * (1 - rimPow);

				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
		ENDCG
	}
	}
}