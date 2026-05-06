Shader "Custom/OutLine"
{
    Properties
    {
        [HDR] _OutlineColor ("Color", Color) = (1,0,0,1)
        _OutlineWidth ("Width", Float) = 0.05
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="UniversalForward" }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _OutlineWidth;
            float4 _OutlineColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;

                float3 normalWS = normalize(TransformObjectToWorldNormal(v.normalOS));
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);

                posWS += normalWS * _OutlineWidth;

                o.positionHCS = TransformWorldToHClip(posWS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                return _OutlineColor;
            }

            ENDHLSL
        }
    }
}