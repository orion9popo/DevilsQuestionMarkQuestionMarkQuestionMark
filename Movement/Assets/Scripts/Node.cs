// Node.cs
using UnityEngine;

public class Node
{
    public Vector2Int GridPosition;
    public bool IsWalkable;
    public int GCost; // Cost from start node
    public int HCost; // Heuristic cost to end node
    public int FCost => GCost + HCost; // Total cost
    public Node ParentNode;

    public Node(Vector2Int gridPosition, bool isWalkable)
    {
        GridPosition = gridPosition;
        IsWalkable = isWalkable;
    }
}
