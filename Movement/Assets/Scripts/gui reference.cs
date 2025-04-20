using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GUIreference : MonoBehaviour
{
    public GameObject player, camera;
    public Transform[] viewPos;
    public Transform[] PlayerPos;

    public void Begin(){
        SceneManager.LoadScene("Training");
    }
    public void End(){
        Application.Quit();
    }
    public void SitDown(){
        camera.transform.position = viewPos[0].position;
        camera.transform.rotation = viewPos[0].rotation;
        player.transform.position = PlayerPos[0].position;
        player.transform.rotation = PlayerPos[0].rotation;
        Animator animator = player.GetComponent<Animator>();
        animator.SetTrigger("Sit");
    }

}
