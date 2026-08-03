using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class BodycamPostEffect : MonoBehaviour
{
    [SerializeField] private Shader _shader;

    [Range(0f, 0.2f)]
    [SerializeField] private float _lensDistortion = 0.08f;

    [Range(0f, 10f)]
    [SerializeField] private float _vignette = 0.38f;

    [Range(0f, 0.01f)]
    [SerializeField] private float _chromaticAberration = 0.0015f;

    [Range(0f, 0.15f)]
    [SerializeField] private float _noiseIntensity = 0.025f;

    [Range(0f, 0.2f)]
    [SerializeField] private float _scanlineIntensity = 0.025f;

    [Header("CRT Scanlines")]

    [Range(0f, 0.5f)]
    [SerializeField] private float _crtScanlineIntensity = 0.12f;

    [Range(80f, 600f)]
    [SerializeField] private float _crtScanlineDensity = 240f;

    [Range(0.01f, 0.35f)]
    [SerializeField] private float _crtScanlineThickness = 0.12f;

    [Range(-2f, 2f)]
    [SerializeField] private float _crtScanlineScrollSpeed = 0.35f;

    [Range(0f, 0.05f)]
    [SerializeField] private float _crtRedBlueShift = 0.0007f;

    [Header("Hallucination Wave")]

    [Range(0f, 0.03f)]
    [SerializeField] private float _hallucinationWaveIntensity = 0.008f;

    [Range(0.25f, 5f)]
    [SerializeField] private float _hallucinationWaveFrequency = 1.8f;

    [Range(0f, 3f)]
    [SerializeField] private float _hallucinationWaveSpeed = 1f;

    [Header("Pixelation")]

    [Range(1f, 64f)]
    [SerializeField] private float _pixelationBlockSize = 1f;

    private Material _material;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (_shader == null || !_shader.isSupported)
        {
            Graphics.Blit(source, destination);
            return;
        }

        if (_material == null)
        {
            _material = new Material(_shader)
            {
                hideFlags = HideFlags.DontSave
            };
        }

        _material.SetFloat("_LensDistortion", _lensDistortion);
        _material.SetFloat("_Vignette", _vignette);
        _material.SetFloat("_ChromaticAberration", _chromaticAberration);
        _material.SetFloat("_NoiseIntensity", _noiseIntensity);
        _material.SetFloat("_ScanlineIntensity", _scanlineIntensity);
        _material.SetFloat("_CrtScanlineIntensity", _crtScanlineIntensity);
        _material.SetFloat("_CrtScanlineDensity", _crtScanlineDensity);
        _material.SetFloat("_CrtScanlineThickness", _crtScanlineThickness);
        _material.SetFloat(
            "_CrtScanlineScrollSpeed",
            _crtScanlineScrollSpeed
        );
        _material.SetFloat("_CrtRedBlueShift", _crtRedBlueShift);
        _material.SetFloat(
            "_HallucinationWaveIntensity",
            _hallucinationWaveIntensity
        );
        _material.SetFloat(
            "_HallucinationWaveFrequency",
            _hallucinationWaveFrequency
        );
        _material.SetFloat("_HallucinationWaveSpeed", _hallucinationWaveSpeed);
        _material.SetFloat("_PixelationBlockSize", _pixelationBlockSize);

        // The camera uses an HDR deferred target.  Copy it to a regular,
        // single-sample texture before applying a second legacy image effect;
        // sampling the deferred source directly produced a flat grey frame.
        RenderTextureDescriptor descriptor = source.descriptor;
        descriptor.depthBufferBits = 0;
        descriptor.msaaSamples = 1;
        descriptor.useMipMap = false;
        descriptor.autoGenerateMips = false;

        RenderTexture intermediate = RenderTexture.GetTemporary(descriptor);
        try
        {
            Graphics.Blit(source, intermediate);
            _material.SetTexture("_MainTex", intermediate);
            Graphics.Blit(intermediate, destination, _material);
        }
        finally
        {
            RenderTexture.ReleaseTemporary(intermediate);
        }
    }

    private void OnDisable()
    {
        if (_material == null) return;

        if (Application.isPlaying)
            Destroy(_material);
        else
            DestroyImmediate(_material);
    }
}
