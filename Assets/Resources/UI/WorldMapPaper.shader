Shader "Chaos/UI/WorldMapPaper"
{
    Properties
    {
        [PerRendererData] _MainTex ("底图", 2D) = "white" {}
        _Color ("颜色", Color) = (1,1,1,1)
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 local : TEXCOORD1;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _ClipRect;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.local = input.vertex.xy;
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(Varyings input) : SV_Target
            {
                fixed4 source = tex2D(_MainTex, input.uv);
                // 抬升底图暗角，同时保留地形、河流和海岸原有的明暗细节。
                float light = dot(source.rgb, float3(.30, .58, .12));
                float rim = smoothstep(.33, .70, length(input.uv - .5));
                light = saturate(pow(saturate(light), .67) + rim * .21);
                float3 paper = lerp(float3(.30, .235, .16), float3(.99, .93, .77), light);
                fixed4 result = fixed4(paper, source.a) * input.color;
                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(input.local, _ClipRect);
                #endif
                return result;
            }
            ENDCG
        }
    }
}
