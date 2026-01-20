Shader "Hidden/Outline/PostProcess"
{
    // 마스크 텍스처를 기반으로 아웃라인을 그리는 포스트 프로세싱 셰이더
    
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _MaskTex ("Mask Texture", 2D) = "black" {}
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Float) = 2.0
        _SampleCount ("Sample Count", Int) = 8
        _OutlineStyle ("Outline Style", Int) = 0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            Name "OutlinePostProcess"
            
            ZTest Always
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            
            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                int _SampleCount;
                int _OutlineStyle;
            CBUFFER_END
            
            // 8방향 고정 오프셋 (대각선 + 직선)
            static const float2 SAMPLE_OFFSETS[8] = 
            {
                float2( 1.0,  0.0),  // 오른쪽
                float2(-1.0,  0.0),  // 왼쪽
                float2( 0.0,  1.0),  // 위
                float2( 0.0, -1.0),  // 아래
                float2( 0.707,  0.707),  // 오른쪽 위
                float2(-0.707,  0.707),  // 왼쪽 위
                float2( 0.707, -0.707),  // 오른쪽 아래
                float2(-0.707, -0.707)   // 왼쪽 아래
            };
            
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                
                // 원본 화면 색상
                float4 sceneColor = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, uv, 0);
                
                // 중심 마스크 값
                float centerMask = SAMPLE_TEXTURE2D_LOD(_MaskTex, sampler_MaskTex, uv, 0).r;
                
                // 이미 마스크 영역 내부라면 아웃라인 없음
                if (centerMask > 0.5)
                    return sceneColor;
                
                // 텍셀 사이즈 계산
                float2 texelSize = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);
                float radius = _OutlineWidth;
                
                // 8방향 샘플링으로 아웃라인 검출 (고정 루프)
                float outline = 0.0;
                
                [unroll]
                for (int i = 0; i < 8; i++)
                {
                    float2 offset = SAMPLE_OFFSETS[i] * radius * texelSize;
                    float maskSample = SAMPLE_TEXTURE2D_LOD(_MaskTex, sampler_MaskTex, uv + offset, 0).r;
                    outline = max(outline, maskSample);
                }
                
                // 아웃라인이 검출되면 색상 적용
                if (outline > 0.01)
                {
                    float4 outlineColor;
                    
                    // 스타일별 처리
                    if (_OutlineStyle == 1) // Glow 스타일
                    {
                        float glow = outline * outline;
                        outlineColor = float4(_OutlineColor.rgb, glow * _OutlineColor.a);
                    }
                    else // Default 스타일
                    {
                        outlineColor = float4(_OutlineColor.rgb, outline * _OutlineColor.a);
                    }
                    
                    // 원본 화면과 아웃라인 블렌딩
                    sceneColor.rgb = lerp(sceneColor.rgb, outlineColor.rgb, outlineColor.a);
                }
                
                return sceneColor;
            }
            ENDHLSL
        }
    }
    
    Fallback Off
}
