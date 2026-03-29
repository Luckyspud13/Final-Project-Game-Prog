using UnityEngine;

public class EndLevel : MonoBehaviour
{
    public GameObject levelManager;

    void OnTriggerEnter()
    {
        Debug.Log("I was hit!");
        levelManager.GetComponent<LevelManager>().IsBeaten();
    }
}