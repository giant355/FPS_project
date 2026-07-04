using UnityEngine;

[ExecuteInEditMode]
public class CameraBloodEffect : MonoBehaviour
{
    [SerializeField] private Shader shader;
    [SerializeField] private Texture2D bloodTexture;
    [SerializeField] private Texture2D bloodNormalMap;

    [Range(0f, 1f)]
    [SerializeField] private float bloodAmount = 0f;

    [Range(0f, 2f)]
    [SerializeField] private float distortion = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float minBloodAmount = 0f;

    [SerializeField] private bool autoFade = true;

    [SerializeField] private float fadeSpeed = 0.05f;

    private Material material;

    public float BloodAmount
    {
        get => bloodAmount;
        set => bloodAmount = value;
    }

    public float MinBloodAmount
    {
        get => minBloodAmount;
        set => minBloodAmount = value;
    }
    
    public float FadeSpeed
    {
        get { return fadeSpeed; }
        set { fadeSpeed = value; }
    }

    public bool AutoFade
    {
        get { return autoFade; }
        set { autoFade = value; }
    }
    private void Update()
    {
        if (!autoFade)
            return;

        bloodAmount -= fadeSpeed * Time.deltaTime;
        bloodAmount = Mathf.Max(bloodAmount, minBloodAmount);
    }
   
    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (shader == null)
        {
            Graphics.Blit(src, dest);
            return;
        }

        if (material == null)
        {
            material = new Material(shader);
        }

        if (bloodTexture != null)
            material.SetTexture("_BloodTex", bloodTexture);

        if (bloodNormalMap != null)
            material.SetTexture("_BloodBump", bloodNormalMap);

        material.SetFloat("_BloodAmount", bloodAmount);
        material.SetFloat("_Distortion", distortion);

        Graphics.Blit(src, dest, material);
    }
}