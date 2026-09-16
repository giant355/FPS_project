using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Shared Collections/Timed String Queue")]
public class SharedTimedStringQueue : ScriptableObject,ISerializationCallbackReceiver
{
    [SerializeField]
    [TextArea(3, 10)]
    protected string _noteToDeveloper =
        "An automated timed message delivery queue.\n\n" +
        "Usage:\n\n" +
        "Queue.Enqueue(\"My Message\");\n\n" +
        "Debug.Log(Queue.text);\n\n" +
        "A CoroutineRunner instance must exist in the current scene.";

    [SerializeField] protected float _dequeueDelay = 3.5f;

    protected float _nextDequeueTime = 0.0f;
    protected IEnumerator _coroutine = null;
    protected bool _paused = false;
    protected string _text = null;

    public string text { get { return _text; } }
    public bool paused { get { return _paused; } set { _paused = value; } }

    private Queue<string> _queue = new Queue<string>();

    public void Enqueue(string message)
    {
        CoroutineRunner runner = CoroutineRunner.Instance;

        if (runner == null)
        {
            _text = "Timed Text Queue Error: No CoroutineRunner Object present";
            return;
        }

        _queue.Enqueue(message);

        if (_coroutine == null)
        {
            _coroutine = QueueProcessor();
            runner.StartCoroutine(_coroutine);
        }
    }

    protected IEnumerator QueueProcessor()
    {
        while (true)
        {
            if (!paused)
            {
                _nextDequeueTime -= Time.unscaledDeltaTime;

                if (_nextDequeueTime < 0.0f)
                {
                    //如果队列为空，则跳出循环，结束协程
                    if (_queue.Count == 0) break;

                    _text = _queue.Dequeue();

                    _nextDequeueTime = _dequeueDelay;
                }
            }

            yield return null;
        }

        _text = null;
        _coroutine = null;
    }

    public int Count()
    {
        return _queue.Count;
    }

    public void OnBeforeSerialize() { }
    public void OnAfterDeserialize()
    {
        _text = null;
        _queue.Clear();
    }
}
