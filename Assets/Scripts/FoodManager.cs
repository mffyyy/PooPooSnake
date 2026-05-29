using UnityEngine;

public class FoodManager : MonoBehaviour
{
    
    public GameObject[] foodPrefabs;
    public Transform foodParent;
    public bool spawnOnStart = true;
    public SnakeManager snakeManager;
    public ToiletManager toiletManager;

    void Start()
    {
        if (spawnOnStart)
            SpawnFood();
    }

    void Update()
    {
        
    }

    public void SpawnFood()
    {
        if (foodPrefabs == null || foodPrefabs.Length == 0)
            return;
            
        int randomIndex = Random.Range(0, foodPrefabs.Length-1);

        Vector2Int foodGridPos = GetRandomEmptyGrid();

        GameObject newfood = Instantiate(foodPrefabs[randomIndex]);

        newfood.transform.SetParent(foodParent, false);
        newfood.transform.localPosition = SnakeGridUtil.GridToLocal(foodGridPos);
    }

    public void SpawnShit(Vector2Int gridPos, int score)
    {
        if (foodPrefabs == null || foodPrefabs.Length == 0)
            return;

        
        if(toiletManager != null && toiletManager.IsToiletAtGrid(gridPos))
        {
             if (GameManager.Instance != null)
             {
                 GameManager.Instance.AddScore(score);
                 GameManager.Instance.PlayAddScoreSound();
             }
        }
        else
        {
            GameObject shitPrefab = foodPrefabs[foodPrefabs.Length - 1];
            GameObject newShit = Instantiate(shitPrefab);
            newShit.transform.SetParent(foodParent, false);
            newShit.transform.localPosition = SnakeGridUtil.GridToLocal(gridPos);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(Config.shitMinusScore);
                GameManager.Instance.PlayPooPooSound();
            }
        }
    }

    public void ActivateToiletAfterFirstFood()
    {
        if (toiletManager != null)
            toiletManager.ActivateToilet();
    }

    public float GetFoodTimerByIndex(int foodIndex)
    {
        switch (foodIndex)
        {
            case 0:
                return Config.appleTimer;
            case 1:
                return Config.meatTimer;
            case 2:
                return Config.riceTimer;
            case 3:
                return Config.shitTimer;
        }

        return 0f;
    }

    public int GetFoodScoreByIndex(int foodIndex)
    {
        switch (foodIndex)
        {
            case 0:
                return Config.appleScore;
            case 1:
                return Config.meatScore;
            case 2:
                return Config.riceScore;
            case 3:
                return Config.shitScore;
        }

        return 0;
    }




    private Vector2Int GetRandomEmptyGrid()
    {
        Vector2Int gridPos;
        do
        {
            gridPos = SnakeGridUtil.GetRandomGrid();
        }
        while (snakeManager.IsSnakeAtGrid(gridPos) || toiletManager.IsToiletAtGrid(gridPos));

        return gridPos;
    }

    public bool IsFoodAtGrid(Vector2Int gridPos)
    {
        if (transform.childCount == 0)
        return false;

        Vector3 localPosition = SnakeGridUtil.GridToLocal(gridPos);
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).localPosition == localPosition)
                return true;
        }

        return false;
    }

}
