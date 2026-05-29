using UnityEngine;
using UnityEngine.UI;

public class PoopSmokeEffect : MonoBehaviour
{
    private Image image;
    private Sprite[] frames;
    private float duration;
    private float elapsed;

    public void Play(Sprite[] smokeFrames, float playDuration)
    {
        frames = smokeFrames;
        duration = Mathf.Max(0.01f, playDuration);
        elapsed = 0f;

        EnsureImage();
        if (frames != null && frames.Length > 0)
        {
            image.sprite = frames[0];
            image.enabled = true;
        }
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0)
        {
            Destroy(gameObject);
            return;
        }

        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / duration);
        int frameIndex = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(progress * frames.Length));

        image.sprite = frames[frameIndex];
        image.color = new Color(1f, 1f, 1f, 1f - progress);

        if (elapsed >= duration)
            Destroy(gameObject);
    }

    private void EnsureImage()
    {
        if (image != null)
            return;

        image = GetComponent<Image>();
        if (image == null)
            image = gameObject.AddComponent<Image>();

        image.raycastTarget = false;
        image.preserveAspect = true;
    }
}
