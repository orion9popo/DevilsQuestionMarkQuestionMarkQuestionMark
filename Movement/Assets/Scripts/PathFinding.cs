// Pathfinding.cs
using UnityEngine;
using System.Collections.Generic;

public class Pathfinding : MonoBehaviour
{
    public Vector2Int startPosition;
    public Vector2Int endPosition;

    private Node[,] nodes;
    private List<Node> openList;
    private HashSet<Node> closedList;

    private MazeGenerator mazeGenerator;

    void Start()
{
    mazeGenerator = GetComponent<MazeGenerator>();
    InitializeNodes();

    // Set start and end positions (ensure they are walkable and within the maze bounds)
    startPosition = new Vector2Int(1, 1);
    endPosition = new Vector2Int(mazeGenerator.width - 2, mazeGenerator.height - 2);

    FindPath(startPosition, endPosition);
}

    void InitializeNodes()
    {
        int width = mazeGenerator.width;
        int height = mazeGenerator.height;
        nodes = new Node[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool isWalkable = !mazeGenerator.grid[x, y].IsWall;
                nodes[x, y] = new Node(new Vector2Int(x, y), isWalkable);
            }
        }
    }
    void FindPath(Vector2Int startPos, Vector2Int endPos)
    {
        Node startNode = nodes[startPos.x, startPos.y];
        Node endNode = nodes[endPos.x, endPos.y];

        openList = new List<Node> { startNode };
        closedList = new HashSet<Node>();

        startNode.GCost = 0;
        startNode.HCost = GetHeuristic(startNode, endNode);

        while (openList.Count > 0)
        {
            Node currentNode = GetLowestFCostNode(openList);

            if (currentNode == endNode)
            {
                // Path found
                RetracePath(startNode, endNode);
                return;
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            foreach (Node neighbor in GetNeighbors(currentNode))
            {
                if (!neighbor.IsWalkable || closedList.Contains(neighbor))
                    continue;

                int tentativeGCost = currentNode.GCost + GetDistance(currentNode, neighbor);

                if (tentativeGCost < neighbor.GCost || !openList.Contains(neighbor))
                {
                    neighbor.GCost = tentativeGCost;
                    neighbor.HCost = GetHeuristic(neighbor, endNode);
                    neighbor.ParentNode = currentNode;

                    if (!openList.Contains(neighbor))
                        openList.Add(neighbor);
                }
            }
        }
        int GetHeuristic(Node a, Node b)
        {
            // Using Manhattan Distance as the heuristic
            int dx = Mathf.Abs(a.GridPosition.x - b.GridPosition.x);
            int dy = Mathf.Abs(a.GridPosition.y - b.GridPosition.y);
            return dx + dy;
        }
        int GetDistance(Node a, Node b)
        {
            return 1;
        }
        Node GetLowestFCostNode(List<Node> nodeList)
        {
            Node lowestFCostNode = nodeList[0];

            foreach (Node node in nodeList)
            {
                if (node.FCost < lowestFCostNode.FCost ||
                    (node.FCost == lowestFCostNode.FCost && node.HCost < lowestFCostNode.HCost))
                {
                    lowestFCostNode = node;
                }
            }

            return lowestFCostNode;
        }

        List<Node> GetNeighbors(Node node)
        {
            List<Node> neighbors = new List<Node>();
            int x = node.GridPosition.x;
            int y = node.GridPosition.y;

            if (x - 1 >= 0) neighbors.Add(nodes[x - 1, y]); // Left
            if (x + 1 < mazeGenerator.width) neighbors.Add(nodes[x + 1, y]); // Right
            if (y - 1 >= 0) neighbors.Add(nodes[x, y - 1]); // Down
            if (y + 1 < mazeGenerator.height) neighbors.Add(nodes[x, y + 1]); // Up

            return neighbors;
        }
        void RetracePath(Node startNode, Node endNode)
        {
            List<Node> path = new List<Node>();
            Node currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.ParentNode;
            }

            path.Reverse();

            // Visualize the path
            StartCoroutine(DrawPath(path));
        }
        System.Collections.IEnumerator DrawPath(List<Node> path)
        {
            foreach (Node node in path)
            {
                Vector3 position = new Vector3(node.GridPosition.x, 0.5f, node.GridPosition.y);
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = position;
                sphere.transform.localScale = Vector3.one * 0.5f;
                sphere.GetComponent<Renderer>().material.color = Color.red;
                yield return new WaitForSeconds(0.05f); // For visual effect
            }
        }


    }
}