Shader "Custom/HealthCircleShader"
{
    Properties
    {
        [Header(Health)]
        [MainColor] _HealthColor("Health Color", Color) = (0, 1, 0, 1)
        _DamageColor("Damage Color", Color) = (1, 0, 0, 1)
        _FillAmount("Fill Amount", Range(0.0, 1.0)) = 1.0

        [Header(Border)]
        _BorderColor("Border Color", Color) = (0, 0, 0, 1)
        _BorderThickness("Border Thickness", Range(0.0, 0.1)) = 0.02

        [Header(Pulsating Effect)] // 헤더 변경
        _PulseColor("Pulse Color", Color) = (1, 1, 0.5, 1) // 발광 색상
        _PulseSpeed("Pulse Speed", Range(0.0, 10.0)) = 2.0 // 발광 속도
        _PulseIntensity("Pulse Intensity", Range(0.0, 5.0)) = 1.0 // 발광 강도
        _PulseToggle("Pulse Toggle (0 or 1)", Float) = 0.0 // 효과 켜고 끄기

        [Header(Rendering)]
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _HealthColor, _DamageColor, _BorderColor, _PulseColor; // _ShineColor 대신 _PulseColor
                float _FillAmount, _BorderThickness, _PulseSpeed, _PulseIntensity, _PulseToggle, _Cutoff; // shine 대신 pulse
            CBUFFER_END

            struct Attributes { float4 p:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 p:SV_POSITION; float2 uv:TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.p = TransformObjectToHClip(IN.p.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 centeredUV = IN.uv - 0.5;
                float dist = length(centeredUV);
                
                clip(step(dist, 0.5) - _Cutoff);

                float angle = atan2(centeredUV.y, centeredUV.x);
                float normalizedAngle = (angle / (2.0 * PI)) + 0.5;

                float4 finalColor = lerp(_DamageColor, _HealthColor, step(normalizedAngle, _FillAmount));
                
                float borderMask = step(0.5 - _BorderThickness, dist);
                finalColor = lerp(finalColor, _BorderColor, borderMask);

                // --- 숨 쉬는 듯한 발광 효과 로직 ---
                if (_PulseToggle > 0.0)
                {
                    // 1. 시간에 따라 0~1 사이를 부드럽게 반복하는 값 생성
                    float pulse = (sin(_Time.y * _PulseSpeed) + 1.0) * 0.5; // -1~1 -> 0~1
                    
                    // 2. 강도 적용
                    pulse *= _PulseIntensity;
                    
                    // 3. 최종 색상에 발광 색상을 더함 (기존 색상 위에 덧씌우는 방식)
                    finalColor.rgb += _PulseColor.rgb * pulse;
                }
                // --- 여기까지 ---

                finalColor.a = 1.0;
                return finalColor;
            }
            ENDHLSL

            ZWrite On
            Cull Off
        }
    }
}
