Shader "Custom/WallOutline"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}

        [HDR]
        _Color ("Outline Color", Color) = (1, 1, 1, 1)

        _Thickness ("Outline Thickness", Range(0.0001, 0.05)) = 0.001
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "UniversalMaterialType"="Unlit"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "Outline"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)

                float4 _Color;
                float4 _MainTex_TexelSize;
                float _Thickness;

            CBUFFER_END


            Varyings vert(Attributes input)
            {
                Varyings output;

                output.positionCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                output.uv = input.uv;

                return output;
            }


            float SampleAlpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    uv
                ).a;
            }


            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                float centerAlpha =
                    SampleAlpha(uv);

                // -------------------------------------------------
                // 8 Nachbarn prüfen
                // -------------------------------------------------

                float2 offsetX =
                    float2(_Thickness, 0);

                float2 offsetY =
                    float2(0, _Thickness);

                float2 offsetXY =
                    float2(_Thickness, _Thickness);


                float alphaRight =
                    SampleAlpha(uv + offsetX);

                float alphaLeft =
                    SampleAlpha(uv - offsetX);

                float alphaUp =
                    SampleAlpha(uv + offsetY);

                float alphaDown =
                    SampleAlpha(uv - offsetY);


                float alphaUpRight =
                    SampleAlpha(uv + offsetXY);

                float alphaUpLeft =
                    SampleAlpha(
                        uv + float2(-_Thickness, _Thickness)
                    );

                float alphaDownRight =
                    SampleAlpha(
                        uv + float2(_Thickness, -_Thickness)
                    );

                float alphaDownLeft =
                    SampleAlpha(
                        uv - offsetXY
                    );


                // -------------------------------------------------
                // Stärkste Alpha der Nachbarn
                // -------------------------------------------------

                float neighbourAlpha =
                    max(alphaRight, alphaLeft);

                neighbourAlpha =
                    max(neighbourAlpha, alphaUp);

                neighbourAlpha =
                    max(neighbourAlpha, alphaDown);

                neighbourAlpha =
                    max(neighbourAlpha, alphaUpRight);

                neighbourAlpha =
                    max(neighbourAlpha, alphaUpLeft);

                neighbourAlpha =
                    max(neighbourAlpha, alphaDownRight);

                neighbourAlpha =
                    max(neighbourAlpha, alphaDownLeft);


                // -------------------------------------------------
                // NUR der Bereich außerhalb des Sprites
                // -------------------------------------------------

                float outlineAlpha =
                    saturate(
                        neighbourAlpha - centerAlpha
                    );


                // -------------------------------------------------
                // Innenbereich komplett entfernen
                // -------------------------------------------------

                if (outlineAlpha <= 0.001)
                    discard;


                // -------------------------------------------------
                // Nur Outline-Farbe ausgeben
                // -------------------------------------------------

                return half4(
                    _Color.rgb,
                    _Color.a * outlineAlpha
                );
            }

            ENDHLSL
        }
    }
}