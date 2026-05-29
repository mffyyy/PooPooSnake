using UnityEngine;

public enum SnakeDir
{
    Up,
    Down,
    Left,
    Right
}

public static class SnakeGridUtil
{
    public static Vector3 GridToLocal(Vector2Int gridPos)
    {
        float x = Config.startx + Config.step * gridPos.x;
        float y = Config.starty - Config.step * gridPos.y;

        return new Vector3(x, y, 0f);
    }

    public static Vector2Int GetRandomGrid()
    {
        return new Vector2Int(
            Random.Range(1, Config.columns - 1),
            Random.Range(1, Config.rows - 1)
        );
    }

    public static Vector2Int DirToVector(SnakeDir dir)
    {
        switch (dir)
        {
            case SnakeDir.Up:
                return new Vector2Int(0, -1);
            case SnakeDir.Down:
                return new Vector2Int(0, 1);
            case SnakeDir.Left:
                return new Vector2Int(-1, 0);
            case SnakeDir.Right:
                return new Vector2Int(1, 0);
        }

        return Vector2Int.zero;
    }

    public static SnakeDir GetDirection(Vector2Int from, Vector2Int to)
    {
        Vector2Int delta = to - from;

        if (delta == new Vector2Int(0, -1))
            return SnakeDir.Up;

        if (delta == new Vector2Int(0, 1))
            return SnakeDir.Down;

        if (delta == new Vector2Int(-1, 0))
            return SnakeDir.Left;

        if (delta == new Vector2Int(1, 0))
            return SnakeDir.Right;

        Debug.LogError($"Invalid direction from {from} to {to}");
        return SnakeDir.Up;
    }

    public static bool IsOpposite(SnakeDir a, SnakeDir b)
    {
        return
            a == SnakeDir.Up && b == SnakeDir.Down ||
            a == SnakeDir.Down && b == SnakeDir.Up ||
            a == SnakeDir.Left && b == SnakeDir.Right ||
            a == SnakeDir.Right && b == SnakeDir.Left;
    }

    public static bool IsStraight(SnakeDir a, SnakeDir b)
    {
        return IsOpposite(a, b);
    }

    public static Quaternion GetRotation(SnakeDir dir)
    {
        switch (dir)
        {
            case SnakeDir.Up:
                return Quaternion.Euler(0f, 0f, -90f);
            case SnakeDir.Right:
                return Quaternion.Euler(0f, 0f, 180f);
            case SnakeDir.Down:
                return Quaternion.Euler(0f, 0f, 90f);
            case SnakeDir.Left:
                return Quaternion.Euler(0f, 0f, 0f);
        }

        return Quaternion.identity;
    }

    public static Quaternion GetStraightRotation(SnakeDir dir)
    {
        if (dir == SnakeDir.Up || dir == SnakeDir.Down)
            return Quaternion.Euler(0f, 0f, 90f);

        return Quaternion.Euler(0f, 0f, 0f);
    }

    public static Quaternion GetCornerRotation(SnakeDir a, SnakeDir b)
    {
        bool up = a == SnakeDir.Up || b == SnakeDir.Up;
        bool down = a == SnakeDir.Down || b == SnakeDir.Down;
        bool left = a == SnakeDir.Left || b == SnakeDir.Left;
        bool right = a == SnakeDir.Right || b == SnakeDir.Right;

        if (up && right)
            return Quaternion.Euler(0f, 0f, 180f);

        if (right && down)
            return Quaternion.Euler(0f, 0f, 90f);

        if (down && left)
            return Quaternion.Euler(0f, 0f, 0f);

        if (left && up)
            return Quaternion.Euler(0f, 0f, -90f);

        return Quaternion.identity;
    }
}
