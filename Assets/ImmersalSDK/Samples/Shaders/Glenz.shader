Shader "Immersal/Glenz" {
	Properties{
		_Color("Color Tint", Color) = (1,1,1,1)
	}
		SubShader{
			Tags {"Queue" = "Transparent" "RenderType" = "Transparent" }
			LOD 100
			Cull off
			ZWrite Off
			//Blend OneMinusDstColor One
			Blend DstColor One

			CGPROGRAM
			#pragma surface surf Lambert noforwardadd


			fixed4 _Color;

			struct Input {
				fixed4 color : COLOR;
			};

			void surf(Input IN, inout SurfaceOutput o) {
				o.Albedo = _Color.rgb * IN.color.rgb;
				o.Alpha = _Color.a;
			}
			ENDCG
	}

		Fallback "Mobile/VertexLit"
}