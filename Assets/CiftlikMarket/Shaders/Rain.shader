// Yağmur damlaları: köşe rengi (parçacık rengi) ile saydam, derinlik testli, ışıksız.
Shader "CiftlikMarket/Rain"
{
    Properties { _Tint ("Tint", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Tint;
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.color = v.color * _Tint; o.uv = v.uv; return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                // damlanın kenarlarını yumuşat (yatayda), uçlarını incelt (dikeyde)
                float sx = 1 - abs(i.uv.x * 2 - 1);
                float sy = 1 - abs(i.uv.y * 2 - 1);
                fixed4 c = i.color; c.a *= saturate(sx * 1.6) * saturate(sy * 2.0);
                return c;
            }
            ENDCG
        }
    }
}
