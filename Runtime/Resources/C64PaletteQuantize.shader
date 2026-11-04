Shader "Hidden/UnityCoder/C64PaletteQuantize"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        ZTest Always
        ZWrite Off
        Cull Off
        Blend Off

        Pass
        {
            Name "C64Palette"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_PaletteTex);

            float _Intensity;
            float _Brightness;
            float _Contrast;
            float _Saturation;
            float _ColorWeighting;
            float _Dithering;
            float _DitherScale;
            float _PixelSize;

            float3 ToMatchSpace(float3 color)
            {
                #if defined(UNITY_COLORSPACE_GAMMA)
                    return saturate(color);
                #else
                    return LinearToSRGB(saturate(color));
                #endif
            }

            float Bayer4x4(float2 pixelCoordinate)
            {
                int2 p = int2(pixelCoordinate) & 3;
                int index = p.x + p.y * 4;
                const float bayer[16] = {
                    0.0,  8.0,  2.0, 10.0,
                   12.0,  4.0, 14.0,  6.0,
                    3.0, 11.0,  1.0,  9.0,
                   15.0,  7.0, 13.0,  5.0
                };
                return (bayer[index] + 0.5) / 16.0 - 0.5;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float pixelSize = max(1.0, _PixelSize);
                if (pixelSize > 1.0)
                {
                    float2 screenPixels = floor(uv * _BlitTexture_TexelSize.zw / pixelSize);
                    uv = (screenPixels + 0.5) * pixelSize * _BlitTexture_TexelSize.xy;
                    uv = saturate(uv);
                }

                float4 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float3 target = ToMatchSpace(source.rgb);

                target = (target - 0.5) * _Contrast + 0.5 + _Brightness;
                float luminance = dot(target, float3(0.2126, 0.7152, 0.0722));
                target = lerp(luminance.xxx, target, _Saturation);

                if (_Dithering > 0.0)
                {
                    float2 ditherPixel = floor(input.positionCS.xy / max(1.0, _DitherScale));
                    float dither = Bayer4x4(ditherPixel) * _Dithering * 0.24;
                    target += dither;
                }
                target = saturate(target);

                float3 weights = lerp(float3(1.0, 1.0, 1.0), float3(2.0, 4.0, 3.0), _ColorWeighting);
                float closestDistance = 1e20;
                float3 closestLinearRGB = 0.0;

                [unroll]
                for (int i = 0; i < 16; ++i)
                {
                    float3 paletteColor = LOAD_TEXTURE2D(_PaletteTex, int2(i, 0)).rgb;
                    float3 difference = target - ToMatchSpace(paletteColor);
                    float distanceSquared = dot(difference * difference, weights);
                    if (distanceSquared < closestDistance)
                    {
                        closestDistance = distanceSquared;
                        closestLinearRGB = paletteColor;
                    }
                }

                return float4(lerp(source.rgb, closestLinearRGB, _Intensity), source.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
