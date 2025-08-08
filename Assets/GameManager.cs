using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private int current_room = 1;

    public GameObject door_1;
    public GameObject door_2;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void objective_is_done(int obj_code)
    {
        switch (current_room)
        {
            case 1:
                {
                    if (obj_code == 1)
                    {
                        door_1.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
                        next_room();
                    }
                    break;
                }
            case 2:
                {
                    if (obj_code == 2)
                    {
                        door_2.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
                        next_room();
                    }
                    break;
                }
            case 3:
                {
                    if (obj_code == 3)
                    {
                        Debug.Log("Done!");
                        next_room();
                    }
                    break;
                }
        }
    }

    void next_room()
    {
        switch (current_room)
        {
            case 1:
                {
                    current_room = 2;
                    break;
                }
            case 2:
                {
                    current_room = 3;
                    break;
                }
            case 3:
                {
                    Debug.Log("Finished!");
                    current_room = 0;
                    break;
                }
        }
    }
}
