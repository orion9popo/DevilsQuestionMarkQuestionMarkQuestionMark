using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Gate : MonoBehaviour
{
    void OnTriggerEnter(Collider other){
        Debug.Log(other.gameObject);
        if( other.tag == "Player"){
        SceneManager.LoadScene("Maze");
        }
    }
}
