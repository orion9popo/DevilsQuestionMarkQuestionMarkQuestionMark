using UnityEngine;
public class CellState : MonoBehaviour
{
    public int x;
    public int y;
    public bool isWalkable = true;

    // Initially, the reward is hidden. We'll assign it once and reveal it later.
    public float hiddenReward = 0f;
    public bool isRevealed = false;

    // This method can be called when we sense the cell
    public void Reveal()
    {
        isRevealed = true;
        UpdateCellVisual();
    }

   // Update the cell’s appearance based on its state
    // In this lab, we’ll color good cells green and bad cells red once revealed
    private void UpdateCellVisual()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (isRevealed)
        {
            if (hiddenReward > 0)
                renderer.material.color = Color.green;
            else if (hiddenReward < 0)
                renderer.material.color = Color.red;
            else
                renderer.material.color = Color.gray; // Neutral
        }
        else
        {
            renderer.material.color = Color.white; // Unknown
        }
    }
}
