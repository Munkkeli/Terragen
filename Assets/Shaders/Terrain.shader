Shader "Custom/Terrain Lit" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }

        CGPROGRAM
        #pragma surface surf Lambert

        struct Input {
            float2 uv_MainTex;
            float4 color : COLOR;
        };

        sampler2D _MainTex;

        void surf (Input IN, inout SurfaceOutput o) {
            o.Albedo = tex2D (_MainTex, IN.uv_MainTex).rgb * IN.color;
        }

        ENDCG
    }

    Fallback "Diffuse"
}