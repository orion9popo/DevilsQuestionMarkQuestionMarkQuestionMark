// Pathfinding.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Rendering;
using System;
using Random = UnityEngine.Random;
using System.Data.SqlTypes;

public class PathFinding : MonoBehaviour
{
    public Vector2Int startPosition;
    public Vector2Int endPosition;
    public float speed = 10f;
    public GameObject[] packlings;
    public GameObject player;
    private Node[,] nodes;
    private List<Node> openList;
    private HashSet<Node> closedList;
    public MazeGenerator mazeGenerator;
    private Vector3 desiredNode;
    private bool firstNode = true;

    void Start()
    {
        InitializeNodes();
        Debug.Log(startPosition);
        if(mazeGenerator == null || player == null){ Destroy(gameObject); Debug.Log("destorying");}
        //Debug.Log( "from PathFinding " + mazeGenerator +" | " +player);

        // Set start and end positions (ensure they are walkable and within the maze bounds)
        //startPosition = new Vector2Int(1, 1);
        //endPosition = new Vector2Int(mazeGenerator.width - 2, mazeGenerator.height - 2);

        FindPath(startPosition, endPosition);
        Collider hitbox = GameObject.Find("M1Hitbox").GetComponent<Collider>();
        foreach (GameObject packling in packlings)
        {
            EnemyAI packlingAI = packling.GetComponent<EnemyAI>();
            packlingAI.player = player.transform;
            packlingAI.thePack = true;
            packlingAI.hitbox = hitbox;
        }
    }
    void Update()
    {
        /*RaycastHit hit;
        if(Physics.Raycast(transform.position, (transform.position - player.transform.position).normalized, out hit, 10f) && hit.transform.tag == "Player"){
            Debug.DrawLine(transform.position, hit.point);
            foreach (GameObject packling in packlings)
            {
                packling.transform.SetParent(null);
                packling.GetComponent<EnemyAI>().thePack = false;
            }
            Destroy(gameObject);
        }*/
        
        try{if((player.transform.position - transform.position).magnitude < 8){
            foreach (GameObject packling in packlings)
            {
                packling.transform.SetParent(null);
                packling.GetComponent<EnemyAI>().thePack = false;
                packling.GetComponent<Rigidbody>().isKinematic = false;
            }
            Destroy(gameObject);
        }
        transform.LookAt(desiredNode);
        transform.position = Vector3.MoveTowards(transform.position, desiredNode, 10 * Time.deltaTime);
    }catch{
        Debug.Log("2");
        Destroy(gameObject);
    }
    }

    void InitializeNodes()
    {
        try{
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
    catch{
        Debug.Log("1");
        Destroy(gameObject);
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

            // Visualize the path
            StartCoroutine(DrawPath(path));
        }
        System.Collections.IEnumerator DrawPath(List<Node> path)
        {
            foreach (Node node in path)
            {
                Debug.Log(node.GridPosition);
                desiredNode = new Vector3(node.GridPosition.x * 10, 3, node.GridPosition.y * 10);
                Debug.DrawLine(transform.position, desiredNode, Color.red, 10000f);
                /*if(firstNode){
                    transform.position = desiredNode;
                    firstNode = false;
                }*/
                yield return new WaitUntil(gotToNode);
            }
            /*startPosition = endPosition;
            do{
                endPosition = new Vector2Int(Random.Range(0,mazeGenerator.width),Random.Range(0,mazeGenerator.height));
            }
            while(nodes[endPosition.x, endPosition.y].IsWalkable);
            Debug.Log(endPosition); */
            //firstNode = true;
            //FindPath(endPosition, startPos);
        }
    }
    Boolean gotToNode(){
        if((desiredNode - transform.position).magnitude < 0.01f){
            return true;
        }
        return false;
    }
}