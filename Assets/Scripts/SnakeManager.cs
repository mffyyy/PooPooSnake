using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class SnakeManager : MonoBehaviour
{
    private const string EatingAnimationStateName = "Eating";

    public enum FoodType

    {
        Apple,
        Meat,
        Rice,
        Shit
    }
    
    public Transform snakeParent;
    public FoodManager foodManager;

    [Header("Snake Sprites")]
    public Sprite[] snakeSprites = new Sprite[4];

    [Header("FoodX Sprites")]
    public Sprite[] foodXSprites = new Sprite[4];

    [Header("Poop Smoke FX")]
    public Sprite[] poopSmokeSprites = new Sprite[5];
    public float poopSmokeMoveDuration = 2f;
    public float poopSmokeSize = 2f;

    [Header("Snake Segment Prefab")]
    public GameObject segmentPrefab;

    public class SnakeSegmentData
    {
        public Vector2Int gridPos;
        public Sprite foodXSprite;
        public Quaternion foodRotation = Quaternion.identity;
        public float foodTimeLeft;
        public int foodScore;

        public SnakeSegmentData(Vector2Int pos)
        {
            gridPos = pos;
        }
    }


    [Header("Start Settings")]
    public Vector2Int startGridPos = new Vector2Int(6, 4);
    public int startLength = 3;
    public SnakeDir startDir = SnakeDir.Left;

    private readonly List<SnakeSegmentData> segmentData = new List<SnakeSegmentData>();
    private readonly List<GameObject> segmentObjects = new List<GameObject>();

    private SnakeDir currentDir;
    private SnakeDir nextDir;
    private float moveElapsed;
    private bool hasPendingNeckFood;
    private Sprite pendingNeckFoodSprite;
    private float pendingNeckFoodTimer;
    private int pendingNeckFoodScore;
    private bool hasEatenFirstFood;
    private bool poopInputQueued;
    
    
     
    void Start()
    {
        InitSnake();
        RefreshSnakeVisual();
    }

    
    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying())
        return;

        HandleInput();
        QueuePoopInput();
        UpdateFoodTimers();

        moveElapsed += Time.deltaTime;

        if (moveElapsed >= GetCurrentMoveTimer())
        {
            moveElapsed = 0f;
            HandleQueuedPoopInput();
            Move();
        }
    }

    private void InitSnake()
    {
        segmentData.Clear();

        currentDir = startDir;
        nextDir = startDir;
        Vector2Int backDirv = -SnakeGridUtil.DirToVector(startDir);

        
        for (int i = 0; i < startLength; i++)
        {
            Vector2Int pos = startGridPos + backDirv * i;
            segmentData.Add(new SnakeSegmentData(pos));
        }
     
    }

    private float GetCurrentMoveTimer()
    {
        if (GameManager.Instance != null)
            return GameManager.Instance.GetMoveTimer();

        return Config.moveTimer;
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            SetNextDir(SnakeDir.Up);

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            SetNextDir(SnakeDir.Down);

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            SetNextDir(SnakeDir.Left);

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            SetNextDir(SnakeDir.Right);
    }

    private void QueuePoopInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            poopInputQueued = true;
    }

    private void HandleQueuedPoopInput()
    {
        if (!poopInputQueued)
            return;

        poopInputQueued = false;
        SnakeSegmentData tailSegment = segmentData[segmentData.Count - 1];
        if (tailSegment.foodXSprite != null)
            PoopTailFoodX();
        else
            PushFoodXBack();
    }


    private void Move()
    {
        currentDir = nextDir;

        Vector2Int oldHeadPos = segmentData[0].gridPos;
        Vector2Int newHeadPos = segmentData[0].gridPos + SnakeGridUtil.DirToVector(currentDir);

        if (IsSnakeBodyAtGrid(newHeadPos))
        {
            GameManager.Instance.GameOver();
            return;
        }

        if (hasPendingNeckFood)
        {
            SnakeSegmentData newNeck = new SnakeSegmentData(oldHeadPos);
            newNeck.foodXSprite = pendingNeckFoodSprite;
            newNeck.foodRotation = Quaternion.identity;
            newNeck.foodTimeLeft = pendingNeckFoodTimer;
            newNeck.foodScore = pendingNeckFoodScore;
            segmentData.Insert(1, newNeck);
            hasPendingNeckFood = false;
        }
        else
        {
            for (int i = segmentData.Count - 1; i > 0; i--)
            {
                segmentData[i].gridPos = segmentData[i - 1].gridPos;
            }
        }

        segmentData[0].gridPos = newHeadPos;

        RefreshSnakeVisual();
    }

#region Snake Refresh

    private void RefreshSnakeVisual()
    {
        EnsureSegmentObjects();

        for (int i = 0; i < segmentData.Count; i++)
        {
            Sprite bodySprite;
            Quaternion rotation;

            if (i == 0)
            {
                bodySprite = snakeSprites[0];

                SnakeDir headDir = segmentData.Count > 1
                    ? SnakeGridUtil.GetDirection(segmentData[1].gridPos, segmentData[0].gridPos)
                    : currentDir;

                rotation = SnakeGridUtil.GetRotation(headDir);
            }
            else if (i == segmentData.Count - 1)
            {
                bodySprite = snakeSprites[3];

                SnakeDir tailDir = SnakeGridUtil.GetDirection(segmentData[i].gridPos, segmentData[i - 1].gridPos);
                rotation = SnakeGridUtil.GetRotation(tailDir);
            }
            else
            {
                Vector2Int prev = segmentData[i - 1].gridPos;
                Vector2Int curr = segmentData[i].gridPos;
                Vector2Int next = segmentData[i + 1].gridPos;

                SnakeDir dirToPrev = SnakeGridUtil.GetDirection(curr, prev);
                SnakeDir dirToNext = SnakeGridUtil.GetDirection(curr, next);

                if (SnakeGridUtil.IsStraight(dirToPrev, dirToNext))
                {
                    bodySprite = snakeSprites[1];
                    rotation = SnakeGridUtil.GetStraightRotation(dirToPrev);
                }
                else
                {
                    bodySprite = snakeSprites[2];
                    rotation = SnakeGridUtil.GetCornerRotation(dirToPrev, dirToNext);
                }
            }

            SnakeSegmentView view = segmentObjects[i].GetComponent<SnakeSegmentView>();
            if (view == null)
                view = segmentObjects[i].AddComponent<SnakeSegmentView>();

            view.Render(
                bodySprite,
                SnakeGridUtil.GridToLocal(segmentData[i].gridPos),
                rotation,
                i == 0,
                this,
                segmentData[i].foodXSprite,
                segmentData[i].foodRotation,
                IsFoodXBlinking(segmentData[i]),
                segmentData[i].foodTimeLeft);
        }
    }

    private void EnsureSegmentObjects()
    {
        GameObject prefab = segmentPrefab;

        while (segmentObjects.Count < segmentData.Count)
        {
            GameObject segmentObj = Instantiate(prefab);
            segmentObj.transform.SetParent(snakeParent, false);
            segmentObjects.Add(segmentObj);
        }

        for (int i = 0; i < segmentObjects.Count; i++)
            segmentObjects[i].SetActive(i < segmentData.Count);
    }
#endregion

    private void SetNextDir(SnakeDir dir)
    {
        if (SnakeGridUtil.IsOpposite(currentDir, dir))
            return;

        nextDir = dir;
    }

#region eat food
    public void EatFood(GameObject foodObject)
    {
         if (GameManager.Instance != null)
         {
                 GameManager.Instance.PlayEatingSound();
         }

        PlayHeadEatingAnimation();
         
        Sprite foodX;
        float foodTimer;
        int foodScore;
        bool hasFoodX = TryGetFoodXData(foodObject, out foodX, out foodTimer, out foodScore);
        if (hasFoodX && segmentData.Count > 1)
        {
             hasPendingNeckFood = true;
             pendingNeckFoodSprite = foodX;
             pendingNeckFoodTimer = foodTimer;
             pendingNeckFoodScore = foodScore;
        }

        Destroy(foodObject);
        if(foodObject.CompareTag("Food"))
        {
            if (!hasEatenFirstFood)
            {
                hasEatenFirstFood = true;
                foodManager.ActivateToiletAfterFirstFood();

                if (GameManager.Instance != null)
                    GameManager.Instance.ShowFirstFoodTutorial();
            }

            foodManager.SpawnFood();
        }        
    }

    private void PlayHeadEatingAnimation()
    {
        if (segmentObjects.Count == 0 || segmentObjects[0] == null)
            return;

        SnakeSegmentView headView = segmentObjects[0].GetComponent<SnakeSegmentView>();
        if (headView == null)
            return;

        headView.PlayEatingAnimation(EatingAnimationStateName);
    }


    private bool TryGetFoodXData(GameObject obj, out Sprite foodXSprite, out float foodTimer, out int foodScore)
    {
        foodXSprite = null;
        foodTimer = 0f;
        foodScore = 0;

        if (obj == null || foodManager == null || foodManager.foodPrefabs == null)
            return false;

        string foodName = obj.name.Replace("(Clone)", "").Trim();

        for (int i = 0; i < foodManager.foodPrefabs.Length && i < foodXSprites.Length; i++)
        {
            GameObject prefab = foodManager.foodPrefabs[i];
            if (prefab == null)
                continue;

            if (foodName == prefab.name.Trim())
            {
                foodXSprite = foodXSprites[i];
                foodTimer = foodManager.GetFoodTimerByIndex(i);
                foodScore = foodManager.GetFoodScoreByIndex(i);
                return foodXSprite != null;
            }
        }

        return false;
    }
    
#endregion
   
#region Poopshit
    private void PushFoodXBack()
    {
        for (int i = segmentData.Count - 1; i > 1; i--)
        {
            segmentData[i].foodXSprite = segmentData[i - 1].foodXSprite;
            segmentData[i].foodRotation = segmentData[i - 1].foodRotation;
            segmentData[i].foodTimeLeft = segmentData[i - 1].foodTimeLeft;
            segmentData[i].foodScore = segmentData[i - 1].foodScore;
        }

        segmentData[1].foodXSprite = null;
        segmentData[1].foodRotation = Quaternion.identity;
        segmentData[1].foodTimeLeft = 0f;
        segmentData[1].foodScore = 0;
    }

    private void PoopTailFoodX()
    {
        SnakeSegmentData tailSegment = segmentData[segmentData.Count - 1];

        if (tailSegment.foodTimeLeft > 0f)
        {
            PoopAtTailGrid();
            RemoveTailSegment();
            return;
        }

        ClearFoodX(tailSegment);
    }

    private void RemoveTailSegment()
    {
        if (segmentData.Count <= 1)
            return;

        segmentData.RemoveAt(segmentData.Count - 1);
    }

    private void UpdateFoodTimers()
    {
        bool shouldRefresh = false;

        for (int i = 0; i < segmentData.Count; i++)
        {
            if (segmentData[i].foodXSprite == null)
                continue;

            bool wasBlinking = IsFoodXBlinking(segmentData[i]);
            segmentData[i].foodTimeLeft -= Time.deltaTime;

            if (segmentData[i].foodTimeLeft <= 0f)
            {
                ClearFoodX(segmentData[i]);
                shouldRefresh = true;
                continue;
            }

            if (wasBlinking != IsFoodXBlinking(segmentData[i]))
                shouldRefresh = true;
        }

        if (shouldRefresh)
            RefreshSnakeVisual();
    }

    private bool IsFoodXBlinking(SnakeSegmentData data)
    {
        return data.foodXSprite != null && data.foodTimeLeft > 0f && data.foodTimeLeft <= 6f;
    }

    private void ClearFoodX(SnakeSegmentData data)
    {
        data.foodXSprite = null;
        data.foodRotation = Quaternion.identity;
        data.foodTimeLeft = 0f;
    }

    private void PoopAtTailGrid()
    {
        Vector2Int tailGridPos = segmentData[segmentData.Count - 1].gridPos;
        int score = segmentData[segmentData.Count - 1].foodScore;

        PlayPoopSmoke(tailGridPos);

        if (foodManager != null)
            foodManager.SpawnShit(tailGridPos, score);
    }

    private void PlayPoopSmoke(Vector2Int gridPos)
    {
        if (poopSmokeSprites == null || poopSmokeSprites.Length == 0 || poopSmokeSprites[0] == null)
            return;

        GameObject smokeObj = new GameObject("PoopSmokeFX", typeof(RectTransform), typeof(Image), typeof(PoopSmokeEffect));
        smokeObj.transform.SetParent(snakeParent, false);
        smokeObj.transform.localPosition = SnakeGridUtil.GridToLocal(gridPos);
        smokeObj.transform.SetAsLastSibling();

        RectTransform rect = smokeObj.GetComponent<RectTransform>();
        float smokeSize = Config.step * poopSmokeSize;
        rect.sizeDelta = new Vector2(smokeSize, smokeSize);

        float duration = GetCurrentMoveTimer() * poopSmokeMoveDuration;
        smokeObj.GetComponent<PoopSmokeEffect>().Play(poopSmokeSprites, duration);
    }
#endregion
 public bool IsSnakeAtGrid(Vector2Int gridPos)
    {
        for (int i = 0; i < segmentData.Count; i++)
        {
            if (segmentData[i].gridPos == gridPos)
                return true;
        }

        return false;
    }

    private bool IsSnakeBodyAtGrid(Vector2Int gridPos)
    {
        for (int i = 1; i < segmentData.Count; i++)
        {
            if (segmentData[i].gridPos == gridPos)
                return true;
        }

        return false;
    }
}
