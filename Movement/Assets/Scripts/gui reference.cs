using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GUIreference : MonoBehaviour
{
    public void Begin(){
        SceneManager.LoadScene("Training");
    }
    public void End(){
        Application.Quit();
    }
}
