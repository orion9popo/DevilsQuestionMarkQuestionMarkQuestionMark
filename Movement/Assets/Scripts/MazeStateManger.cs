using UnityEngine;
using System.Collections.Generic;

public class MazeStateManager : MonoBehaviour
{
    public Dictionary<(int, int), CellState> cellStates = new Dictionary<(int, int), CellState>();
    public int width;
    public int height;

    public void RegisterCell(CellState cell)
    {
        cellStates[(cell.x, cell.y)] = cell;
    }

    // Actions represented as directions:
    // Up:    (0, 1)
    // Down:  (0, -1)
    // Left:  (-1, 0)
    // Right: (1, 0)
    public CellState GetNextState(CellState current, Vector2Int direction)
    {
        int nx = current.x + direction.x;
        int ny = current.y + direction.y;

        if (nx < 0 || nx >= width || ny < 0 || ny >= height) 
            return current; // out of bounds, no move
        
        CellState nextState = cellStates[(nx, ny)];
        if (!nextState.isWalkable) 
            return current; // blocked by a wall
        
        return nextState;
    }
}
