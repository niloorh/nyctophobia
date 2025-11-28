using DG.Tweening.Core.Easing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enterScript : MonoBehaviour
{
    public int room = 0;
    public GameObject gameManager;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            gameManager.GetComponent<GameManager>().enteredRoom(room);
        }
    }
}
