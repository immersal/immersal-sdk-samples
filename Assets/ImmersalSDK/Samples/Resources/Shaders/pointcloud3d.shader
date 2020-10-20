Shader "Immersal/pointcloud3d"
{
	Properties
	{
        _PointSize("Point Size", Float) = 0.003
	}

		SubShader
	{
		Tags{ "RenderType" = "Opaque" }

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

            #pragma multi_compile_fog

			#include "UnityCG.cginc"

			half _PointSize;

			struct Vertex
			{
				float3 vertex : POSITION;
				fixed4 color : COLOR;
			};

			struct VertexOut
			{
				fixed4 color : COLOR;
				float psize : PSIZE;
				UNITY_FOG_COORDS(0)
			};

			VertexOut vert(Vertex vertex, out float4 outpos : SV_POSITION)
			{
				VertexOut o;
				outpos = UnityObjectToClipPos(vertex.vertex);
				o.color = vertex.color;
				o.psize = _PointSize / outpos.w * _ScreenParams.y;
				UNITY_TRANSFER_FOG(o, outpos);
				return o;
			}
            
			fixed4 frag(VertexOut i, UNITY_VPOS_TYPE vpos : VPOS) : SV_Target
			{
				fixed4 c = fixed4(i.color);
				UNITY_APPLY_FOG(input.fogCoord, c);
				return c;
			}
			ENDCG
		}
	}
}