// AimLineIndicator(조준선)의 점선 스크롤 연출 전용 셰이더.
// Sprites/Default는 스프라이트 한 장을 그대로 그리는 용도라 머티리얼의 텍스처 오프셋(_MainTex_ST)을
// 아예 무시한다 — 그래서 AimLineIndicator.ScrollDashes()로 오프셋을 바꿔도 점선이 흐르지 않았다.
// 이 셰이더는 TRANSFORM_TEX로 오프셋·스케일을 제대로 반영하면서, LineRenderer의 정점 색(start/end color)도
// 그대로 곱해 반투명 틴트가 유지되게 한다.
Shader "FCC/DashedLine" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex); // 오프셋·스케일이 실제로 반영되는 부분.
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                return tex2D(_MainTex, i.uv) * i.color;
            }
            ENDCG
        }
    }
}
