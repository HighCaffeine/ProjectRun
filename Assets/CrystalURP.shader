Shader "Custom/CrystalURP"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)

        _NormalMap("Normal Map", 2D) = "bump" {}
        _MetallicMap("Metallic Map", 2D) = "black" {}

        _EmissionMap("Emission Map", 2D) = "black" {}
        _EmissionColor("Emission Color", Color) = (1,1,1,1)
        _EmissionIntensity("Emission Intensity", Range(0,20)) = 1

        _Transparency("Transparency", Range(0,1)) = 0.5

        _FresnelColor("Fresnel Color", Color) = (0.5,1,1,1)
        _FresnelPower("Fresnel Power", Range(0.1,10)) = 4
        _FresnelIntensity("Fresnel Intensity", Range(0,10)) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "ForwardLit"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            TEXTURE2D(_MetallicMap);
            SAMPLER(sampler_MetallicMap);

            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            float4 _BaseColor;
            float4 _EmissionColor;
            float4 _FresnelColor;

            float _Transparency;
            float _EmissionIntensity;
            float _FresnelPower;
            float _FresnelIntensity;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs pos =
                    GetVertexPositionInputs(IN.positionOS.xyz);

                VertexNormalInputs norm =
                    GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS = norm.normalWS;
                OUT.uv = IN.uv;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                float4 albedo =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        uv
                    ) * _BaseColor;

                float metallic =
                    SAMPLE_TEXTURE2D(
                        _MetallicMap,
                        sampler_MetallicMap,
                        uv
                    ).r;

                float3 emission =
                    SAMPLE_TEXTURE2D(
                        _EmissionMap,
                        sampler_EmissionMap,
                        uv
                    ).rgb;

                float3 normalWS =
                    normalize(IN.normalWS);

                float3 viewDir =
                    normalize(
                        _WorldSpaceCameraPos -
                        IN.positionWS
                    );

                float fresnel =
                    pow(
                        1 -
                        saturate(
                            dot(
                                normalWS,
                                viewDir
                            )
                        ),
                        _FresnelPower
                    );

                float3 fresnelGlow =
                    _FresnelColor.rgb *
                    fresnel *
                    _FresnelIntensity;

                float3 finalEmission =
                    emission *
                    _EmissionColor.rgb *
                    _EmissionIntensity +
                    fresnelGlow;

                float3 finalColor =
                    albedo.rgb +
                    finalEmission;

                return float4(
                    finalColor,
                    1 - _Transparency
                );
            }

            ENDHLSL
        }
    }
}