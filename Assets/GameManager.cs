using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private int current_room = 1;
    //private string brightLightingScene = "full_bright";
    //private string dimLightingScene = "dim";
    //private string currentLightingScene;

    public GameObject door_1;
    public GameObject door_2;

    public GameObject[] lights;

    public GameObject lightSwitcher;

    // Start is called before the first frame update
    void Start()
    {
        lightSwitcher.GetComponent<LevelLightmapData>().LoadLightingScenario(0);
        //if (!SceneManager.GetSceneByName(brightLightingScene).isLoaded &&
        //    !SceneManager.GetSceneByName(dimLightingScene).isLoaded)
        //{
        //    StartCoroutine(SwitchTo(brightLightingScene));
        //}
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    //private IEnumerator SwitchTo(string targetLightingScene)
    //{
    //    if (currentLightingScene == targetLightingScene) yield break;

    //    // 1) Load target additively
    //    Debug.Log("Loading scene...");
    //    var load =  SceneManager.LoadSceneAsync(targetLightingScene, LoadSceneMode.Additive);
    //    while (!load.isDone) yield return null;

    //    Debug.Log("Activating scene...");
    //    // 2) Make target the Active Scene so its RenderSettings (skybox/ambient/fog) apply
    //    var target = SceneManager.GetSceneByName(targetLightingScene);
    //    SceneManager.SetActiveScene(target);

    //    // 3) Unload previous lighting scene (if any)
    //    if (!string.IsNullOrEmpty(currentLightingScene))
    //    {
    //        Debug.Log("Unloading scene...");
    //        var unload = SceneManager.UnloadSceneAsync(currentLightingScene);
    //        while (unload != null && !unload.isDone) yield return null;
    //    }

    //    currentLightingScene = targetLightingScene;

    //    // Optional: if you change skybox via code, call this:
    //    // DynamicGI.UpdateEnvironment();
    //    Debug.Log("Done!");
    //    yield break;
    //}
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
                    //StartCoroutine(SwitchTo(dimLightingScene));
                    lightSwitcher.GetComponent<LevelLightmapData>().LoadLightingScenario(1);
                    foreach (GameObject light in lights)
                    {
                        light.GetComponent<Light>().intensity = 0.5f;
                    }
                    break;
                }
            case 2:
                {
                    current_room = 3;
                    lightSwitcher.GetComponent<LevelLightmapData>().LoadLightingScenario(2);
                    foreach (GameObject light in lights)
                    {
                        light.GetComponent<Light>().intensity = 0.25f;
                    }
                    break;
                }
            case 3:
                {
                    Debug.Log("Finished!");
                    current_room = 0;
                    foreach (GameObject light in lights)
                    {
                        light.GetComponent<Light>().intensity = 0f;
                    }
                    break;
                }
        }
    }
}
