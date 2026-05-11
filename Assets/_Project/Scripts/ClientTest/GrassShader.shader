Shader "Custom/GrassShader"
{
    Properties
    {
        GrassColor ("Color", Color) = (0,0,0,1)
        _MainTex ("Texture", 2D) = "white" {}

        _WindStrength ("Wind Strength", Float) = 0.2
        _WindSpeed ("Wind Speed", Float) = 1.0

        _PushStrength ("Push Strength", Float) = 0.5
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

            float4 GrassColor;

            float4 _PlayerPos;
            float _PushRadius;
            float4 _MoveDir;

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

                float3 worldPos =
                    mul(unity_ObjectToWorld, v.vertex).xyz;

                float mask =
                    smoothstep(0.1, 0.8, v.vertex.y);

                float wind =
                    sin(_Time.y * _WindSpeed + worldPos.x * 0.5)
                    * _WindStrength
                    * mask;

                float3 toPlayer =
                    worldPos - _PlayerPos.xyz;

                float dist =
                    length(toPlayer);

                float influence =
                    saturate(1.0 - (dist / _PushRadius));

                float3 dir =
                    normalize(worldPos - _PlayerPos.xyz);

                dir.y = 0;

                float3 push =
                    dir
                    * influence
                    * _PushStrength
                    * mask;

                worldPos += float3(wind, 0, 0);

                worldPos.xz += push.xz;

                worldPos.y -=
                    length(push.xz)
                    * 0.25
                    * mask;

                o.vertex =
                    UnityWorldToClipPos(worldPos);

                o.uv = v.uv;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex =
                    tex2D(_MainTex, i.uv);

                clip(tex.a - 0.5);

                return tex * GrassColor;
            }

            ENDHLSL
        }
    }
}