Shader "Custom/GrassShader"
{
    Properties
    {
        GrassColor ("Color", Color) = (0,1,0,1)
        _MainTex ("Texture", 2D) = "white" {}

        _WindStrength ("Wind Strength", Float) = 0.2
        _WindSpeed ("Wind Speed", Float) = 1.0

        _PushStrength ("Push Strength", Float) = 1.0
        _PushRadius ("Push Radius", Float) = 2.0
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" }

        Pass
        {
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            sampler2D _MainTex;

            float _WindStrength;
            float _WindSpeed;

            float _PushStrength;
            float _PushRadius;

            float4 GrassColor;
            float4 _PlayerPos;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                v2f o;

                float3 localPos = v.vertex.xyz;


                float mask = smoothstep(0.1, 0.9, localPos.y);

                float3 worldPos =
                    mul(unity_ObjectToWorld, float4(localPos, 1)).xyz;


                float3 toPlayer = worldPos - _PlayerPos.xyz;
                toPlayer.y = 0;

                float dist = length(toPlayer);

                float influence = saturate(1.0 - (dist / _PushRadius));

                influence = smoothstep(0.1, 0.8, influence);

                float3 dir = normalize(toPlayer);


                float strength = influence * _PushStrength;


                float2 tilt = dir.xz * strength;



                worldPos.x += tilt.x * mask;
                worldPos.z += tilt.y * mask;

                // y´Â ´¯´Â ´À³¦ º¸Á¤
                worldPos.y -= (abs(tilt.x) + abs(tilt.y)) * 0.25 * mask;

                o.vertex = UnityWorldToClipPos(worldPos);
                o.uv = v.uv;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                clip(tex.a - 0.5);
                return tex * GrassColor;
            }

            ENDHLSL
        }
    }
}