using UnityEngine;

//仅仅用于在非MonoBehaviour类中启动协程的单例类
[DefaultExecutionOrder(-100)]
public class CoroutineRunner : MonoBehaviour
{
    private static CoroutineRunner _instance = null;
    public static CoroutineRunner Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError("CoroutineRunner instance is null. Make sure a CoroutineRunner object exists in the scene.");
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}