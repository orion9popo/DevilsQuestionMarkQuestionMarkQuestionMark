using System;
using Microsoft.Unity.VisualStudio.Editor;
using Unity.Mathematics;
using UnityEngine;
using Image = UnityEngine.UI.Image;

public class PlayerUI : MonoBehaviour
{
    public Image LockOnUI;
    public Camera Cam;

    public void Awake()
    {
        PlayerController characterController = GetComponent<PlayerController>();
        characterController.LockOnEvent += onLockOn;
        characterController.LockOffEvent += onLockOff;
    }

    private void onLockOff(object sender, EventArgs args)
    {
        LockOnUI.enabled = false;
    }

    void onLockOn(object sender, PlayerController.LockOnEventArgs args)
    {

        LockOnUI.enabled = true;

        Vector3 screenPoint = Cam.WorldToScreenPoint(args.Enemy.position);

        LockOnUI.transform.localScale = new(3, 3, 3);
        LockOnUI.transform.LeanScale(new(1, 1, 1), 0.5f);

        if (screenPoint.x > 0)
            LockOnUI.transform.position = new Vector2(650, 150);
        else
            LockOnUI.transform.position = new(-650, 150);

        LockOnUI.transform.LeanMove(new Vector2(screenPoint.x, screenPoint.y), 0.5f);

        LockOnUI.transform.rotation = quaternion.Euler(0, 0, 135);
        LockOnUI.transform.LeanRotate(new(0, 0, 0), 0.5f);
        
    }

}
