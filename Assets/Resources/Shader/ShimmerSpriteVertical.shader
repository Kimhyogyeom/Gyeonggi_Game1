Shader "UI/ShimmerSpriteVertical"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _ShineColor ("Shine Color", Color) = (1,1,1,1)
        _ShineIntensity ("Shine Intensity", Range(0, 3)) = 1
        _ShineWidth ("Shine Width", Range(0.01, 0.5)) = 0.18
        _ShineSoftness ("Shine Softness", Range(0.001, 0.5)) = 0.10
        _ShineAngleDeg ("Shine Angle (Deg)", Range(-90, 90)) = 25
        _ShineSpeed ("Shine Speed", Range(-3, 3)) = 1.0

        // Optional sparkle/noise
        _NoiseTex ("Noise (Optional)", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Range(0.1, 10)) = 2.0
        _NoiseAmount ("Noise Amount", Range(0, 1)) = 0.25

        // --- UI Mask/Stencil support (Unity UI/Default 스타일)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;

            fixed4 _ShineColor;
            float _ShineIntensity;
            float _ShineWidth;
            float _ShineSoftness;
            float _ShineAngleDeg;
            float _ShineSpeed;

            sampler2D _NoiseTex;
            float _NoiseScale;
            float _NoiseAmount;

            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPos = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 baseCol = tex2D(_MainTex, i.uv) * i.color;

                // UI 마스크/클리핑 대응
                #ifdef UNITY_UI_CLIP_RECT
                baseCol.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif

                // 알파클립(필요할 때만 켜세요)
                #ifdef UNITY_UI_ALPHACLIP
                clip(baseCol.a - 0.001);
                #endif

                // --- Shimmer 계산 ---
                // 회전된 UV 축을 만들어서 빛 띠가 대각선으로 이동하게 함
                float ang = radians(_ShineAngleDeg);
                float2x2 R = float2x2(cos(ang), -sin(ang),
                                      sin(ang),  cos(ang));
                float2 ruv = mul(R, (i.uv - 0.5)) + 0.5;

                // 움직이는 중심(0~1) - 세로 방향으로 변경 (ruv.x -> ruv.y)
                float t = frac(ruv.y + _Time.y * _ShineSpeed);

                // 중심에서의 거리(0 기준) 만들기: [-0.5, 0.5]
                float d = abs(t - 0.5);

                // 폭/부드러움으로 띠 모양
                float w = _ShineWidth * 0.5;
                float s = _ShineSoftness;
                float shine = 1.0 - smoothstep(w, w + s, d);

                // Optional noise로 반짝 "입자감" 살짝 추가
                // 노이즈 텍스처가 없으면(흰색) 영향이 거의 없도록 Amount로 조절
                float n = tex2D(_NoiseTex, i.uv * _NoiseScale + _Time.yy * 0.07).r;
                shine *= lerp(1.0, n, _NoiseAmount);

                // 원본 위에 하이라이트 얹기(알파가 0이면 반짝도 0)
                fixed3 add = _ShineColor.rgb * (shine * _ShineIntensity) * baseCol.a;

                fixed4 outCol = baseCol;
                outCol.rgb += add;

                return outCol;
            }
            ENDCG
        }
    }
}
