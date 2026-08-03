using UnityEngine;

public class CameraRecoil : MonoBehaviour
{
    [Header("Recoil Settings")]
    public Vector3 RecoilRotation = new Vector3(-2f, 1f, 0f);
    public float returnSpeed = 5f;
    //¡È√Ù∂»
    public float snappiness = 5f;

    private Vector3 currentRotation;
    private Vector3 targetRotation;

    private void Update()
    {
        targetRotation = Vector3.Lerp(targetRotation,Vector3.zero,returnSpeed * Time.deltaTime);
        currentRotation = Vector3.Slerp(currentRotation,targetRotation,snappiness * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(currentRotation);
    }

    public void ApplyRecoil()
    {
        targetRotation += new Vector3(RecoilRotation.x,Random.Range(-RecoilRotation.y, RecoilRotation.y),Random.Range(-RecoilRotation.z, RecoilRotation.z));
    }
}