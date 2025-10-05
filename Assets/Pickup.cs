using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickup : MonoBehaviour
{
    public GameObject gameManager;
    public PickupType pickupType;

    void OnMouseDown()
    {
        //Debug.Log("In mouse funciton");
        //isDragging = true;
        //offset = transform.position - GetMouseWorldPos();
        gameManager.GetComponent<GameManager>().pickupObject(pickupType);
        gameObject.SetActive(false);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
