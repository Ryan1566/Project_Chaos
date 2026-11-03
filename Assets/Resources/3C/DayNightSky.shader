Shader "Chaos/DayNightSky2D"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        CGINCLUDE
        #include "UnityCG.cginc"
        struct ScreenVertex { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
        struct ScreenPixel { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

        ScreenPixel ScreenVert(ScreenVertex input)
        {
            ScreenPixel output;
            output.vertex = float4(input.vertex.xy, 0, 1);
            output.vertex.y *= _ProjectionParams.x;
            output.uv = input.uv;
            return output;
        }
        ENDCG

        Pass
        {
            CGPROGRAM
            #pragma vertex ScreenVert
            #pragma fragment Sky
            float4 _Environment, _WeatherTint, _Clouds, _WorldRect;
            float4 _CaveParams;
            sampler2D _SurfaceProfile;

            float Ellipse(float2 samplePosition, float2 center, float2 radii)
            {
                float edgeDistance = length((samplePosition - center) / radii);
                return 1 - smoothstep(.80, 1, edgeDistance);
            }

            // 手绘轮廓由四块叠放的云瓣组成，边缘保留柔和留白。
            float Cloud(float2 p, float altitude, float phase, float stretch)
            {
                p.x = frac(p.x * stretch + phase);
                float base = Ellipse(p, float2(.50, altitude), float2(.38, .032));
                float left = Ellipse(p, float2(.29, altitude + .024), float2(.16, .046));
                float top = Ellipse(p, float2(.47, altitude + .040), float2(.23, .070));
                float right = Ellipse(p, float2(.69, altitude + .028), float2(.16, .052));
                return max(max(base, top), max(left, right));
            }

            float4 Sky(ScreenPixel input) : SV_Target
            {
                float2 uv = input.uv;
                float day = _Environment.x;
                float dusk = _Environment.y;
                float solar = _Environment.z;
                float aspect = _Environment.w;
                float gradient = smoothstep(0, 1, uv.y);
                float3 night = lerp(float3(.36,.47,.61), float3(.19,.28,.45), gradient);
                float3 clear = lerp(float3(.88,.93,.89), float3(.40,.66,.84), gradient);
                float3 amber = lerp(float3(.98,.68,.43), float3(.43,.48,.68), gradient);
                float3 sky = lerp(lerp(night, clear, day), amber, dusk * .76);

                float2 sun = float2(lerp(.12,.88,solar), .25 + .53 * sin(solar * UNITY_PI));
                float2 toSun = (uv - sun) * float2(aspect,1);
                float distance = length(toSun);
                float disk = 1 - smoothstep(.043,.049,distance);
                float glow = (1 - smoothstep(.038,.14,distance)) * .28;
                sky = lerp(sky, float3(1,.91,.64), saturate((disk + glow) * day));

                // 前云在日轮之后合成，因此云瓣经过时能完全遮住太阳。
                float farCloud = Cloud(uv,.60,_Clouds.y * .55 + .17,1.75) * saturate(_Clouds.x * 1.4);
                float nearCloud = Cloud(uv,.74,_Clouds.y - .05,2.15) * saturate(_Clouds.x * 2.8);
                float3 cloud = lerp(float3(.42,.50,.65), float3(.98,.965,.91), day);
                cloud = lerp(cloud,float3(.95,.77,.65),dusk*.6);
                sky = lerp(sky, cloud, farCloud*.75);
                sky = lerp(sky, cloud, nearCloud);

                if (_CaveParams.x > 0.5)
                {
                    float2 world = _WorldRect.xy + uv * _WorldRect.zw;
                    float surface = tex2D(_SurfaceProfile, float2(saturate(world.x * _CaveParams.y),.5)).r * _CaveParams.z;
                    if (world.y < surface - 3)
                    {
                        float deep = saturate((surface - world.y) * .06);
                        return float4(lerp(float3(.24,.27,.34), float3(.13,.16,.23), deep),1);
                    }
                }
                return float4(sky * _WeatherTint.rgb,1);
            }
            ENDCG
        }

        Pass
        {
            Blend DstColor Zero
            ColorMask RGB
            CGPROGRAM
            #pragma vertex ScreenVert
            #pragma fragment Tint
            float4 _WorldTint;
            float4 Tint(ScreenPixel input) : SV_Target { return _WorldTint; }
            ENDCG
        }
    }
}
