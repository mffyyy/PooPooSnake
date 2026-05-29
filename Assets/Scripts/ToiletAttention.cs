using UnityEngine;

public class ToiletAttention : MonoBehaviour
{
    private Vector3 baseScale;
    private Quaternion baseRotation;

    private void Awake()
    {
        baseScale = transform.localScale;
        baseRotation = transform.localRotation;
    }

    private void Update()
    {
        float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.06f;
        float angle = 50f + Mathf.Sin(Time.time * 6f) * 15f;

        transform.localScale = baseScale * pulse;
        transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, angle);
    }
}
