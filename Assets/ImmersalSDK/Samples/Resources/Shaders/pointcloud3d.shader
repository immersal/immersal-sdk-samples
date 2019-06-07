Shader "Immersal/pointcloud3d"
{
	SubShader
	{
		Cull Off
		Tags{ "RenderType" = "Opaque" }
		LOD 100
		
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#include "UnityCG.cginc"

			struct Vertex
			{
				float3 vertex : POSITION;
				fixed4 color : COLOR;
			};

			struct VertexOut
			{
				fixed4 color : COLOR;
				float psize : PSIZE;
				float4 center : TEXCOORD0;
				half size : TEXCOORD1;
				UNITY_FOG_COORDS(0)
			};

			VertexOut vert(Vertex vertex, out float4 outpos : SV_POSITION)
			{
                float4 f_col = vertex.color*0.7f;
                float4 n_col = vertex.color;
				VertexOut o;
				outpos = UnityObjectToClipPos(vertex.vertex);
				o.color = lerp(n_col, f_col, saturate(abs(outpos.w) / 4.0));
				o.psize = 0.02f / outpos.w * _ScreenParams.y;
				o.psize = clamp(o.psize, 4.0f, 32.f);
				o.size = o.psize;
				float4 clp = outpos;
#if !SHADER_API_GLES3
				clp.y *= -1.0;
#endif
				o.center = ComputeScreenPos(clp);
				UNITY_TRANSFER_FOG(o, o.position);
				return o;
			}
            
			fixed4 frag(VertexOut i, UNITY_VPOS_TYPE vpos : VPOS) : SV_Target
			{
				fixed4 c = fixed4(i.color);
				float4 center = i.center;
				center.xy /= center.w;
				center.xy *= _ScreenParams.xy;
				float d = distance(vpos.xy, center.xy);
				if (d > i.size*0.5) {
					discard;
				}
				UNITY_APPLY_FOG(input.fogCoord, c);
				return c;
			}
			ENDCG
		}
	}
}


