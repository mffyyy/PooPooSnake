using UnityEngine;

public class ToiletManager : MonoBehaviour
{
    public GameObject toiletPrefabs;
    public Transform toiletParent;
    public SnakeManager snakeManager;
    public FoodManager foodManager;
    public bool spawnOnStart = false;
    public bool spawnAfterFirstFood = true;
    private float moveElapsed;
    private bool hasStarted;

    void Start()
    {
        if (spawnOnStart && !spawnAfterFirstFood)
            ActivateToilet();
    }

    
    void Update()
    {
        if (!hasStarted)
            return;

        moveElapsed += Time.deltaTime;

        if (moveElapsed >= Config.toiletRefreshTimer)
        {
            moveElapsed = 0f;
            if (transform.childCount > 0)
                Destroy(transform.GetChild(0).gameObject);

            SpawnToilet();
        }
    }

    public void ActivateToilet()
    {
        if (hasStarted)
            return;

        hasStarted = true;
        moveElapsed = 0f;
        SpawnToilet();
    }

     public void SpawnToilet()
    {
        Vector2Int toiletGridPos = GetRandomEmptyGrid();

        GameObject newtoilet = Instantiate(toiletPrefabs);

        newtoilet.transform.SetParent(toiletParent, false);
        newtoilet.transform.localPosition = SnakeGridUtil.GridToLocal(toiletGridPos);

        if (newtoilet.GetComponent<ToiletAttention>() == null)
            newtoilet.AddComponent<ToiletAttention>();
    }

     private Vector2Int GetRandomEmptyGrid()
    {
        Vector2Int gridPos;
        do
        {
            gridPos = SnakeGridUtil.GetRandomGrid();
        }
        while (snakeManager.IsSnakeAtGrid(gridPos) || foodManager.IsFoodAtGrid(gridPos));

        return gridPos;
    }
    public bool IsToiletAtGrid(Vector2Int gridPos)
    {
        if (transform.childCount == 0)
        return false;

        return transform.GetChild(0).localPosition == SnakeGridUtil.GridToLocal(gridPos);
    }



}
