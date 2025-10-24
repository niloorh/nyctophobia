using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Pickup : MonoBehaviour
{
    public GameObject gameManager;
    public PickupType pickupType;
    public InputActionReference VrPickupAction;
    public Transform rightController;
    public float pickupDistance = 2f;

    void OnMouseDown()
    {
        //Debug.Log("In mouse funciton");
        //isDragging = true;
        //offset = transform.position - GetMouseWorldPos();
        gameManager.GetComponent<GameManager>().pickupObject(pickupType);
        gameObject.SetActive(false);
    }

    private void Awake()
    {
        if (VrPickupAction != null)
        {
            VrPickupAction.action.Enable();
            VrPickupAction.action.performed += VrPickup;
        }
    }

    private void VrPickup(InputAction.CallbackContext context)
    {
        // Raycast from controller
        Ray ray = new Ray(rightController.position, rightController.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance)) // 3 meters max distance
        {
            // Check if THIS object is what we hit
            if (hit.collider.gameObject == gameObject)
            {
                gameManager.GetComponent<GameManager>().pickupObject(pickupType);
                gameObject.SetActive(false);
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //if (OVRInput.GetDown(OVRInput.Button.One)) // A button
        //{
        //    Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        //    if (Physics.Raycast(ray, out RaycastHit hit, 3f))
        //    {
        //        if (hit.transform == transform)
        //        {
        //            gameManager.GetComponent<GameManager>().pickupObject(pickupType);
        //            gameObject.SetActive(false);
        //        }
        //    }
        //}
    }
}
