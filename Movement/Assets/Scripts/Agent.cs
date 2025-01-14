
using UnityEngine;
//using UnityEngine.InputSystem; // If using new Input System

public class AgentController : MonoBehaviour
{
    public MazeStateManager stateManager;
    private CellState currentState;

    void Start()
    {
        if (stateManager == null)
            stateManager = FindObjectOfType<MazeStateManager>();

        // Assume start at (0,0)
        var startCoords = (0, 0);
        if (stateManager.cellStates.ContainsKey(startCoords))
        {
            currentState = stateManager.cellStates[startCoords];
            transform.position = new Vector3(currentState.x, 0, currentState.y);
        }
        else
        {
            Debug.LogError("Start state not found!");
        }

        // Initially, no sensing action taken
    }

    void Update()
    {
        // For this lab, let's say pressing 'S' senses the current cell.
        if (Input.GetButtonDown("Vertical"))
        {
            SenseCurrentCell();
        }
    }

    void SenseCurrentCell()
    {
        if (!currentState.isRevealed)
        {
            currentState.Reveal();
            Debug.Log($"Cell ({currentState.x}, {currentState.y}) revealed with reward {currentState.hiddenReward}");
        }
        else
        {
            Debug.Log("Cell already revealed.");
        }
    }
}
