Shader "DeadEarth/BodycamPost"
{
    Properties
    {
        // Graphics.Blit writes the camera image into this property.
        _MainTex ("Source", 2D) = "white" {}
        _LensDistortion ("Lens Distortion", Range(0, 0.2)) = 0
        _Vignette ("Vignette", Range(0, 10)) = 0
        _ChromaticAberration ("Chromatic Aberration", Range(0, 0.01)) = 0
        _NoiseIntensity ("Noise Intensity", Range(0, 0.15)) = 0
        _ScanlineIntensity ("Scanline Intensity", Range(0, 0.2)) = 0
        _CrtScanlineIntensity ("CRT Scanline Intensity", Range(0, 0.5)) = 0.12
        _CrtScanlineDensity ("CRT Scanline Density", Range(80, 600)) = 240
        _CrtScanlineThickness ("CRT Scanline Thickness", Range(0.01, 0.35)) = 0.12
        _CrtScanlineScrollSpeed ("CRT Scanline Scroll Speed", Range(-2, 2)) = 0.35
        _CrtRedBlueShift ("CRT Red / Blue Offset", Range(0, 0.005)) = 0.0007
        _HallucinationWaveIntensity ("Hallucination Wave Intensity", Range(0, 0.03)) = 0.008
        _HallucinationWaveFrequency ("Hallucination Wave Frequency", Range(0.25, 5)) = 1.8
        _HallucinationWaveSpeed ("Hallucination Wave Speed", Range(0, 3)) = 1
        _PixelationBlockSize ("Pixelation Block Size", Range(1, 64)) = 1
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float _LensDistortion;
            float _Vignette;
            float _ChromaticAberration;
            float _NoiseIntensity;
            float _ScanlineIntensity;
            float _CrtScanlineIntensity;
            float _CrtScanlineDensity;
            float _CrtScanlineThickness;
            float _CrtScanlineScrollSpeed;
            float _CrtRedBlueShift;
            float _HallucinationWaveIntensity;
            float _HallucinationWaveFrequency;
            float _HallucinationWaveSpeed;
            float _PixelationBlockSize;

            float2 PixelateUv(float2 uv)
            {
                // Convert a block size measured in screen pixels into a grid
                // count, then always sample the centre of that grid cell.
                float blockSize = max(floor(_PixelationBlockSize + 0.5), 1.0);
                float2 blockCount = max(
                    floor(_ScreenParams.xy / blockSize),
                    float2(1.0, 1.0)
                );
                float2 pixelIndex = min(
                    floor(saturate(uv) * blockCount),
                    blockCount - 1.0
                );
                return (pixelIndex + 0.5) / blockCount;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 centeredUv = i.uv - 0.5;
                float radius = length(centeredUv);
                float radiusSquared = dot(centeredUv, centeredUv);
                float2 direction = radius > 0.0001
                    ? centeredUv / radius
                    : float2(0, 0);

                // 轻微广角 / 鱼眼
                float2 uv = 0.5 + centeredUv *
                    (1.0 + _LensDistortion * radiusSquared * 4.0);

                // Hallucination wave: an expanding/contracting radial ripple
                // plus a slower two-axis drift.  Its intensity is zero when
                // disabled, so the normal bodycam image remains unchanged.
                float waveTime = _Time.y * _HallucinationWaveSpeed;
                float radialWave = sin(
                    radius * _HallucinationWaveFrequency * 6.2831853 -
                    waveTime * 3.0
                );
                float2 radialWaveOffset = direction * radialWave *
                    _HallucinationWaveIntensity * radius;
                float2 driftWaveOffset = float2(
                    sin((i.uv.y * _HallucinationWaveFrequency * 2.0 +
                         waveTime) * 6.2831853),
                    cos((i.uv.x * _HallucinationWaveFrequency * 1.6 -
                         waveTime * 0.8) * 6.2831853)
                ) * _HallucinationWaveIntensity * 0.25;
                uv = saturate(uv + radialWaveOffset + driftWaveOffset);

                // 边缘轻微 RGB 分离
                float2 colourOffset =
                    direction * _ChromaticAberration * radiusSquared;

                // Unlike lens chromatic aberration, CRT colour bleed is a
                // constant horizontal red/blue separation across the screen.
                float2 crtRedBlueOffset = float2(_CrtRedBlueShift, 0.0);

                // Always begin with the complete source colour (including alpha).
                // This makes a zero-strength effect an exact pass-through.
                float2 pixelUv = PixelateUv(uv);
                float4 sourceColour = tex2D(_MainTex, pixelUv);
                float3 colour = sourceColour.rgb;

                // Apply the more expensive per-channel sampling only when it is
                // actually visible; a zero value now cannot alter the image.
                if (_ChromaticAberration > 0.000001 ||
                    _CrtRedBlueShift > 0.000001)
                {
                    colour.r = tex2D(
                        _MainTex,
                        PixelateUv(uv + colourOffset + crtRedBlueOffset)
                    ).r;
                    colour.b = tex2D(
                        _MainTex,
                        PixelateUv(uv - colourOffset - crtRedBlueOffset)
                    ).b;
                }

                // 暗角
                // Keep the centre unchanged and limit how dark the corners become.
                // Keep the centre clear, then darken the outer lens area.
                // `_Vignette` now maps directly to the maximum edge darkening;
                // this keeps a value such as 0.15 visibly cinematic without
                // placing a grey veil over the whole image.
                float edge = smoothstep(0.38, 0.74, radius);
                colour *= 1.0 - edge * _Vignette;

                // 细微传感器噪点与移动扫描线
                float noise = frac(sin(dot(
                    i.uv * _ScreenParams.xy + _Time.yy,
                    float2(12.9898, 78.233))) * 43758.5453);

                float movingScanline =
                    sin((i.uv.y * _ScreenParams.y + _Time.y * 8.0)
                    * 3.14159265) * 0.5 + 0.5;

                colour += (noise - 0.5) * _NoiseIntensity;
                colour *= 1.0 - movingScanline * _ScanlineIntensity;

                // CRT horizontal scanlines.  One sine cycle creates one thin
                // dark band; density controls how many bands appear vertically.
                // The time term translates every band at the same speed.
                // The smoothstep keeps the line edge soft instead of aliasing.
                float crtWave =
                    sin((i.uv.y * _CrtScanlineDensity +
                         _Time.y * _CrtScanlineScrollSpeed) * 6.2831853)
                    * 0.5 + 0.5;
                float crtDarkLine = 1.0 - smoothstep(
                    0.0,
                    _CrtScanlineThickness,
                    crtWave
                );
                colour *= 1.0 - crtDarkLine * _CrtScanlineIntensity;

                return float4(saturate(colour), sourceColour.a);
            }
            ENDCG
        }
    }
}

