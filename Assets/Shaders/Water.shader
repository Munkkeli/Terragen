Shader  "Custom/Water Waving" {
	Properties {
	    _Color ("Main Color", Color ) = (1,1,1,1)
		_WaveSpeed ("Wave Speed", float) = 50.0
	    _WaveHeight ("Wave Height", float) = 0.2
		_WaveDeform ("Wave Deform", float) = 0.2
	}

	SubShader {
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
	    Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
		BindChannels {
			Bind "Color", color
			Bind "Vertex", vertex
		}

		CGPROGRAM
		#pragma surface surf Lambert vertex:vert fragment frag
		#include "UnityCG.cginc"

		fixed4 _Color;
		float _WaveSpeed;
		float _WaveHeight;
		float _WaveDeform;

		struct Input {
		    float2 uv_MainTex;
		    float3 worldRefl; 
			float3 localPos;

			float4 pos : SV_POSITION;
            float4 color : COLOR;
		    INTERNAL_DATA
		};

		void vert (inout appdata_full v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input, o);

		    float phase  = _Time * 20.0;
			float3 worldPos = mul(_Object2World, v.vertex).xyz;
		    float offset = (worldPos.x + worldPos.z);

		    v.vertex.y = (sin(phase + offset) * _WaveHeight) + v.vertex.y - 0.2;
			v.vertex.x = (sin(phase + offset) * _WaveDeform) + v.vertex.x;
			o.localPos = v.vertex.xyz;

			o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		}

		void surf (Input IN, inout SurfaceOutput o) {
		    o.Albedo = _Color.rgb * IN.color;
		    o.Alpha = _Color.a;
		}
		ENDCG

	}

	FallBack "VertexLit"
}