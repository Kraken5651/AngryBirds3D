using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    [Header("Target")]
    public Transform cameraTransform;

    private Vector3 _origin;
    private bool    _shaking = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Removed DontDestroyOnLoad — CameraShake lives in the game scene
        // and gets recreated each time the scene loads. No persistence needed.

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform != null)
            _origin = cameraTransform.localPosition;
    }

    public void Shake(float duration, float magnitude)
    {
        if (this == null || !gameObject.activeInHierarchy) return;
        if (_shaking) StopAllCoroutines();
        StartCoroutine(DoShake(duration, magnitude));
    }

    IEnumerator DoShake(float duration, float magnitude)
    {
        _shaking = true;
        _origin  = cameraTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (cameraTransform == null) break;
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            cameraTransform.localPosition = _origin + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (cameraTransform != null)
            cameraTransform.localPosition = _origin;

        _shaking = false;
    }
}