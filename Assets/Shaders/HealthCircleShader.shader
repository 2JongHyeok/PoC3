Shader "Custom/HealthCircleShader"
{
    Properties
    {
        [MainColor] _HealthColor("Health Color", Color) = (0, 1, 0, 1)
        _DamageColor("Damage Color", Color) = (1, 0, 0, 1)
        _BorderColor("Border Color", Color) = (0, 0, 0, 1) // 테두리 색상
        _FillAmount("Fill Amount", Range(0.0, 1.0)) = 1.0
        _BorderThickness("Border Thickness", Range(0.0, 0.1)) = 0.02 // 테두리 두께
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
                float4 _HealthColor;
                float4 _DamageColor;
                float4 _BorderColor; // 테두리 색상 프로퍼티
                float _FillAmount;
                float _BorderThickness; // 테두리 두께 프로퍼티
                float _Cutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 centeredUV = IN.uv - 0.5;
                float dist = length(centeredUV);
                
                float circleAlpha = step(dist, 0.5);
                clip(circleAlpha - _Cutoff);

                float angle = atan2(centeredUV.y, centeredUV.x);
                float normalizedAngle = (angle / (2.0 * PI)) + 0.5;

                float colorStep = step(normalizedAngle, _FillAmount);
                float4 finalColor = lerp(_DamageColor, _HealthColor, colorStep);
                
                // 테두리 계산
                // 원의 바깥쪽 경계 (0.5)와 안쪽 경계 (0.5 - _BorderThickness) 사이의 영역을 찾습니다.
                // dist가 (0.5 - _BorderThickness)보다 크고 0.5보다 작거나 같으면 borderMask는 1이 됩니다.
                float borderMask = step(0.5 - _BorderThickness, dist) * step(dist, 0.5);
                
                // 테두리 영역에 _BorderColor를 적용합니다.
                finalColor = lerp(finalColor, _BorderColor, borderMask);

                finalColor.a = 1.0;

                return finalColor;
            }
            ENDHLSL

            ZWrite On
            Cull Off
        }
    }
}
