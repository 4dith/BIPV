Shader "Unlit/Hologram"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TintColor ("Tine Color", Color) = (1, 1, 1, 1)
        _Transparency ("Transparency", Range(0.0, 0.5)) = 0.25
        _CutoutThresh ("Cutout Threshold", Range(0.0, 10.0)) = 0.2
        _Distance ("Distance", Float) = 1
        _Amplitude ("Amplitude", Float) = 1
        _Speed ("Speed", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _TintColor;
            float _Transparency;
            float _CutoutThresh;
            float _Distance;
            float _Amplitude;
            float _Speed;

            v2f vert (appdata v)
            {
                v2f o;
                v.vertex.x += sin(_Time.y * _Speed + v.vertex.y * _Amplitude) * _Distance;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float4 rand;
                rand[0] = i.vertex.x * _Time.y % 4;
                rand[1] = i.vertex.y * _Time.z % 4;
                rand[2] = i.vertex.z * _Time.x % 4;
                rand[3] = 1;
                fixed4 col = tex2D(_MainTex, i.uv) * (1 + _TintColor * rand);
                col.a = _Transparency;
                clip(_CutoutThresh - col.b * col.r * col.g);
                return col;
            }
            ENDCG
        }
    }
}
