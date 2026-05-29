using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StartSnakeDisplay : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite[] snakeSprites = new Sprite[4];
    public Sprite[] foodXSprites = new Sprite[4];

    [Header("Display")]
    public int snakeLength = 40;
    public int foodIconCount = 20;
    public float initialMoveDelay = 0f;
    public float maxFrameDelta = 0.05f;
    public Vector2Int routeMin = new Vector2Int(0, 3);
    public Vector2Int routeMax = new Vector2Int(10, 8);
    public int routeColumnSpacing = 2;
    public int initialRouteOffset = -1;
    public int randomSeed = 2605;

    private readonly List<Vector2Int> route = new List<Vector2Int>();
    private RectTransform[] segmentRects;
    private Image[] bodyImages;
    private Image[] foodImages;
    private Sprite[] segmentFoodSprites;
    private int headRouteIndex;
    private float moveElapsed;
    private bool skipFirstUpdate = true;

    private void Start()
    {
        snakeLength = Mathf.Max(3, snakeLength);
        BuildRoute();
        snakeLength = Mathf.Min(snakeLength, route.Count - 1);
        int startIndex = initialRouteOffset >= 0 ? initialRouteOffset : route.Count / 2;
        headRouteIndex = Mathf.Clamp(startIndex, snakeLength - 1, route.Count - 1);
        CreateSegments();
        AssignFoodIcons();
        Render();
        moveElapsed = -Mathf.Max(0f, initialMoveDelay);
    }

    private void Update()
    {
        if (skipFirstUpdate)
        {
            skipFirstUpdate = false;
            return;
        }

        moveElapsed += Mathf.Min(Time.unscaledDeltaTime, maxFrameDelta);
        if (moveElapsed < Config.moveTimer)
            return;

        moveElapsed -= Config.moveTimer;
        headRouteIndex = (headRouteIndex + 1) % route.Count;
        Render();
    }

    private void BuildRoute()
    {
        route.Clear();

        int minX = Mathf.Min(routeMin.x, routeMax.x);
        int maxX = Mathf.Max(routeMin.x, routeMax.x);
        int minY = Mathf.Min(routeMin.y, routeMax.y);
        int maxY = Mathf.Max(routeMin.y, routeMax.y);
        int returnY = maxY + 1;
        int columnSpacing = Mathf.Max(1, routeColumnSpacing);

        AddRoutePoint(new Vector2Int(minX, returnY));
        AddVertical(minX, minY);

        List<int> columns = new List<int>();
        for (int x = minX + columnSpacing; x < maxX; x += columnSpacing)
            columns.Add(x);

        for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            int currentX = columns[columnIndex];
            bool movingDown = columnIndex % 2 == 0;
            AddHorizontal(currentX);
            AddVertical(currentX, movingDown ? maxY : minY);
        }

        AddHorizontal(maxX);
        AddVertical(maxX, returnY);
        AddHorizontal(minX);

        if (route.Count > 1 && route[route.Count - 1] == route[0])
            route.RemoveAt(route.Count - 1);
    }

    private void AddHorizontal(int targetX)
    {
        Vector2Int current = route[route.Count - 1];
        int step = targetX > current.x ? 1 : -1;

        for (int x = current.x + step; x != targetX + step; x += step)
            AddRoutePoint(new Vector2Int(x, current.y));
    }

    private void AddVertical(int x, int targetY)
    {
        Vector2Int current = route[route.Count - 1];
        int step = targetY > current.y ? 1 : -1;

        for (int y = current.y + step; y != targetY + step; y += step)
            AddRoutePoint(new Vector2Int(x, y));
    }

    private void AddRoutePoint(Vector2Int point)
    {
        if (route.Count == 0 || route[route.Count - 1] != point)
            route.Add(point);
    }

    private void CreateSegments()
    {
        segmentRects = new RectTransform[snakeLength];
        bodyImages = new Image[snakeLength];
        foodImages = new Image[snakeLength];
        segmentFoodSprites = new Sprite[snakeLength];

        for (int i = 0; i < snakeLength; i++)
        {
            GameObject segment = new GameObject($"DisplaySegment_{i}", typeof(RectTransform), typeof(Image));
            segment.transform.SetParent(transform, false);

            RectTransform rect = segment.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(Config.step, Config.step);
            segmentRects[i] = rect;

            Image bodyImage = segment.GetComponent<Image>();
            bodyImage.raycastTarget = false;
            bodyImages[i] = bodyImage;

            GameObject foodIcon = new GameObject("FoodIcon", typeof(RectTransform), typeof(Image));
            foodIcon.transform.SetParent(segment.transform, false);

            RectTransform foodRect = foodIcon.GetComponent<RectTransform>();
            foodRect.anchorMin = Vector2.zero;
            foodRect.anchorMax = Vector2.one;
            foodRect.offsetMin = Vector2.zero;
            foodRect.offsetMax = Vector2.zero;

            Image foodImage = foodIcon.GetComponent<Image>();
            foodImage.raycastTarget = false;
            foodImages[i] = foodImage;
        }
    }

    private void AssignFoodIcons()
    {
        if (foodXSprites == null || foodXSprites.Length == 0)
            return;

        Random.State previousState = Random.state;
        Random.InitState(randomSeed);

        int count = Mathf.Min(foodIconCount, snakeLength - 2);
        for (int i = 0; i < count; i++)
        {
            int segmentIndex = Random.Range(1, snakeLength - 1);
            int spriteIndex = Random.Range(0, foodXSprites.Length);
            segmentFoodSprites[segmentIndex] = foodXSprites[spriteIndex];
        }

        Random.state = previousState;
    }

    private void Render()
    {
        for (int i = 0; i < snakeLength; i++)
        {
            Vector2Int gridPos = GetSegmentGridPos(i);
            Sprite bodySprite;
            Quaternion rotation;
            GetSegmentVisual(i, out bodySprite, out rotation);

            Vector3 localPosition = SnakeGridUtil.GridToLocal(gridPos);
            segmentRects[i].anchoredPosition = new Vector2(localPosition.x, localPosition.y);
            segmentRects[i].localRotation = rotation;
            bodyImages[i].sprite = bodySprite;

            foodImages[i].enabled = segmentFoodSprites[i] != null;
            foodImages[i].sprite = segmentFoodSprites[i];
            foodImages[i].transform.localRotation = Quaternion.Inverse(rotation);
        }
    }

    private Vector2Int GetSegmentGridPos(int segmentIndex)
    {
        int routeIndex = headRouteIndex - segmentIndex;
        while (routeIndex < 0)
            routeIndex += route.Count;

        return route[routeIndex % route.Count];
    }

    private void GetSegmentVisual(int segmentIndex, out Sprite bodySprite, out Quaternion rotation)
    {
        if (segmentIndex == 0)
        {
            bodySprite = GetSnakeSprite(0);
            rotation = SnakeGridUtil.GetRotation(SnakeGridUtil.GetDirection(GetSegmentGridPos(1), GetSegmentGridPos(0)));
            return;
        }

        if (segmentIndex == snakeLength - 1)
        {
            bodySprite = GetSnakeSprite(3);
            rotation = SnakeGridUtil.GetRotation(SnakeGridUtil.GetDirection(GetSegmentGridPos(segmentIndex), GetSegmentGridPos(segmentIndex - 1)));
            return;
        }

        SnakeDir dirToPrev = SnakeGridUtil.GetDirection(GetSegmentGridPos(segmentIndex), GetSegmentGridPos(segmentIndex - 1));
        SnakeDir dirToNext = SnakeGridUtil.GetDirection(GetSegmentGridPos(segmentIndex), GetSegmentGridPos(segmentIndex + 1));

        if (SnakeGridUtil.IsOpposite(dirToPrev, dirToNext))
        {
            bodySprite = GetSnakeSprite(1);
            rotation = SnakeGridUtil.GetStraightRotation(dirToPrev);
        }
        else
        {
            bodySprite = GetSnakeSprite(2);
            rotation = SnakeGridUtil.GetCornerRotation(dirToPrev, dirToNext);
        }
    }

    private Sprite GetSnakeSprite(int index)
    {
        if (snakeSprites == null || index >= snakeSprites.Length)
            return null;

        return snakeSprites[index];
    }
}
