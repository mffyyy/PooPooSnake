using UnityEngine;
using UnityEngine.UI;

public class SnakeSegmentView : MonoBehaviour
{
    private Image bodyImage;
    private Image foodXImage;
    private Collider2D segmentCollider;
    private Rigidbody2D segmentRigidbody;
    private SnakeHead snakeHead;
    private Animator animator;
    private Sprite currentBodySprite;
    private bool isHeadSegment;
    private bool isEating;
    private float eatingElapsed;
    private float eatingDuration = 0.52f;
    private bool foodBlinking;
    private float foodBlinkProgress;

    public void Render(
        Sprite bodySprite,
        Vector3 localPosition,
        Quaternion bodyRotation,
        bool isHead,
        SnakeManager snakeManager,
        Sprite foodSprite,
        Quaternion foodRotation,
        bool isFoodBlinking,
        float foodTimeLeft)
    {
        EnsureReferences();

        transform.localPosition = localPosition;
        transform.localRotation = bodyRotation;
        transform.localScale = Vector3.one;

        currentBodySprite = bodySprite;
        isHeadSegment = isHead;

        if (!isHeadSegment)
            StopEatingAnimation();

        if (bodyImage != null && !isEating)
            bodyImage.sprite = bodySprite;

        if (snakeHead != null)
        {
            snakeHead.enabled = isHead;
            snakeHead.snakeManager = isHead ? snakeManager : null;
        }

        if (segmentCollider != null)
            segmentCollider.enabled = isHead;

        if (segmentRigidbody != null)
            segmentRigidbody.simulated = isHead;

        if (animator != null && !isEating)
            animator.enabled = false;


        if (foodXImage != null)
        {
            foodBlinking = isFoodBlinking;
            foodBlinkProgress = Mathf.Clamp01(1f - foodTimeLeft / 6f);
            foodXImage.enabled = foodSprite != null;
            foodXImage.sprite = foodSprite;
            foodXImage.transform.localRotation = Quaternion.Inverse(bodyRotation) * foodRotation;
            foodXImage.color = Color.white;
        }
    }

    private void Update()
    {
        UpdateEatingAnimation();

        if (foodXImage == null || foodXImage.sprite == null || !foodBlinking)
            return;

        UpdateFoodBlink();
    }

    public void PlayEatingAnimation(string stateName)
    {
        EnsureReferences();

        if (!isHeadSegment || animator == null)
            return;

        isEating = true;
        eatingElapsed = 0f;
        animator.enabled = true;
        animator.Play(stateName, 0, 0f);
        animator.Update(0f);

        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        if (clipInfo.Length > 0 && clipInfo[0].clip != null)
            eatingDuration = clipInfo[0].clip.length;
    }

    private void UpdateEatingAnimation()
    {
        if (!isEating)
            return;

        eatingElapsed += Time.deltaTime;
        if (eatingElapsed < eatingDuration)
            return;

        StopEatingAnimation();
    }

    private void StopEatingAnimation()
    {
        isEating = false;
        eatingElapsed = 0f;

        if (animator != null)
            animator.enabled = false;

        if (bodyImage != null)
            bodyImage.sprite = currentBodySprite;
    }

    private void UpdateFoodBlink()
    {
        float blinkSpeed = Mathf.Lerp(6f, 18f, foodBlinkProgress);
        float minAlpha = Mathf.Lerp(0.8f, 0.25f, foodBlinkProgress);
        float alpha = Mathf.Lerp(minAlpha, 1f, Mathf.PingPong(Time.time * blinkSpeed, 1f));

        foodXImage.enabled = true;
        foodXImage.color = new Color(1f, 1f, 1f, alpha);
    }

    private void EnsureReferences()
    {
        if (bodyImage == null)
            bodyImage = GetComponent<Image>();

        if (segmentCollider == null)
            segmentCollider = GetComponent<Collider2D>();

        if (segmentRigidbody == null)
            segmentRigidbody = GetComponent<Rigidbody2D>();

        if (snakeHead == null)
            snakeHead = GetComponent<SnakeHead>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator != null && !isEating)
                animator.enabled = false;
        }

        if (foodXImage == null)
            foodXImage = CreateFoodImage();
    }

    private Image CreateFoodImage()
    {
        Transform existing = transform.Find("FoodIcon");
        if (existing != null)
            return existing.GetComponent<Image>();

        GameObject foodIcon = new GameObject("FoodIcon", typeof(RectTransform), typeof(Image));
        foodIcon.transform.SetParent(transform, false);

        RectTransform rect = foodIcon.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = foodIcon.GetComponent<Image>();
        image.raycastTarget = false;
        image.enabled = false;

        return image;
    }
}
