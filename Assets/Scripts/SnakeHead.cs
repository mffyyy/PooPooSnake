using UnityEngine;

public class SnakeHead : MonoBehaviour
{
    public SnakeManager snakeManager;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Food") || collision.CompareTag("Shit"))
        {
            snakeManager.EatFood(collision.gameObject);
        }
        else if (collision.CompareTag("Border"))
        {
             GameManager.Instance.GameOver();
        }
    }

}
