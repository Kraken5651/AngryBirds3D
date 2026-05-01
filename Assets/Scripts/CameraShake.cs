using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;
    private Vector3 _origin;

    void Awake()
    {
        Instance = this;
        _origin  = transform.localPosition;
    }

    public void Shake(float duration, float magnitude)
    {
        StopAllCoroutines();
        StartCoroutine(DoShake(duration, magnitude));
    }

    IEnumerator DoShake(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            transform.localPosition = _origin + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = _origin;
    }
}