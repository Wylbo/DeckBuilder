Shader "UI/MinimapOutline"
{
    Properties
    {
        [PerRendererData] _MainTex("Minimap RT", 2D) = "white" {}
        _Color("Minimap Tint", Color) = (1,1,1,1)

        _GroundTex("Ground Texture", 2D) = "white" {}
        _GroundColor("Ground Color", Color) = (1,1,1,1)
        _GroundTiling("Ground Tiling", Float) = 1.0
        _GroundWhiteThreshold("Ground Detect Threshold", Range(0,1)) = 0.05
        _OutlineTex("Outline Texture", 2D) = "white" {}
        _OutlineColor("Outline Color", Color) = (0,1,0,1)
        _OutlineTiling("Outline Tiling", Float) = 1.0
        _OutlineThickness("Outline Thickness (px)", Float) = 1.5
        _EdgeThreshold("Edge Threshold", Range(0,1)) = 0.1
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "UI"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex; float4 _MainTex_ST; float4 _MainTex_TexelSize;
            sampler2D _GroundTex; float4 _GroundTex_ST;
            sampler2D _OutlineTex; float4 _OutlineTex_ST;

            fixed4 _Color;
            fixed4 _GroundColor;
            float _GroundTiling;
            float _GroundWhiteThreshold;
            fixed4 _OutlineColor;
            float _OutlineTiling;
            float _OutlineThickness;
            float _EdgeThreshold;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Base minimap sample
                fixed4 baseSample = tex2D(_MainTex, i.uv) * i.color;
                float alpha = baseSample.a;
                float3 baseRGB = baseSample.rgb;

                // Ground fill (only where alpha > 0)
                float2 groundUV = (i.uv * _GroundTiling);
                fixed4 groundSample = tex2D(_GroundTex, TRANSFORM_TEX(groundUV, _GroundTex));
                fixed4 ground = groundSample * _GroundColor;
                float deviation = max(abs(baseRGB.r - 1), max(abs(baseRGB.g - 1), abs(baseRGB.b - 1)));
                float groundMask = step(deviation, _GroundWhiteThreshold);
                fixed4 groundLayer = ground * alpha * groundMask;

                // Edge detection: compare center alpha to neighbors
                float2 texel = _MainTex_TexelSize.xy * _OutlineThickness;
                float aL = tex2D(_MainTex, i.uv + float2(-texel.x, 0)).a;
                float aR = tex2D(_MainTex, i.uv + float2(texel.x, 0)).a;
                float aU = tex2D(_MainTex, i.uv + float2(0, texel.y)).a;
                float aD = tex2D(_MainTex, i.uv + float2(0, -texel.y)).a;
                float neighborMin = min(min(aL, aR), min(aU, aD));
                float edge = step(_EdgeThreshold, alpha) * step(_EdgeThreshold, alpha - neighborMin);

                // Outline texture/color
                float2 outlineUV = (i.uv * _OutlineTiling);
                fixed4 outlineSample = tex2D(_OutlineTex, TRANSFORM_TEX(outlineUV, _OutlineTex));
                fixed4 outline = outlineSample * _OutlineColor;
                outline.a *= edge; // only on edges

                // Composite: base RT, then ground on white, outline on edges
                fixed4 col = baseSample;
                col.rgb = lerp(col.rgb, groundLayer.rgb, groundMask);
                col.rgb = lerp(col.rgb, outline.rgb, outline.a);
                float groundAlpha = lerp(alpha, groundLayer.a, groundMask);
                col.a = saturate(max(groundAlpha, outline.a));

                return col;
            }
            ENDCG
        }
    }
}
