using System;
using UnityEngine;

public class drawer : MonoBehaviour
{
    private Rigidbody rb;
    private bool isDragging = false;
    private Vector3 offset;

    void Start()
    {
        //Debug.Log("In mouse funciton");
        rb = GetComponent<Rigidbody>();
    }

    void OnMouseDown()
    {
        //Debug.Log("In mouse funciton");
        isDragging = true;
        offset = transform.position - GetMouseWorldPos();
    }

    void OnMouseUp()
    {
        isDragging = false;
    }

    void FixedUpdate()
    {
        if (isDragging)
        {
            Vector3 targetPos = GetMouseWorldPos() + offset;
            //Debug.Log("Target: " + targetPos);
            // Only allow Z movement
            Vector3 newPos = new Vector3(targetPos.x, transform.position.y, transform.position.z);
            //Debug.Log("new: " + newPos);
            //rb.MovePosition(newPos);
            Vector3 force = new Vector3(targetPos.x - transform.position.x, 0, 0);
            //Debug.Log("Force: " + force);
            rb.AddForce(force.normalized * 5);
        }
    }

    Vector3 GetMouseWorldPos()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, transform.position);
        plane.Raycast(ray, out float distance);
        return ray.GetPoint(distance);
    }
}
