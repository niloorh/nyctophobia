using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using DG.Tweening;                    // DOTween

public enum PickupType
{
    Battery,
    Key,
    Falshlight,
    Note
}

public class GameManager : MonoBehaviour
{
    private int current_room = 1;
    //private string brightLightingScene = "full_bright";
    //private string dimLightingScene = "dim";
    //private string currentLightingScene;

    public GameObject door_1;
    public GameObject door_2;

    public GameObject[] lights;
    public float brightIntensity = 0.7f;
    public float dimIntensity = 0.2f;
    public float darkIntensity = 0.02f;
    public float firstDuration = 3f;
    public float secondDuration = 3f;
    public float thirdDuration = 3f;

    //public GameObject lightSwitcher;

    public Texture2D[] brightDir, brightColor;
    public Texture2D[] dimDir, dimColor;
    public Texture2D[] darkDir, darkColor;

    //public InputAction toggleFlashlight;
    public GameObject flashlight;

    private LightmapData[] brightMap, dimMap, darkMap;

    //public LightmapCrossfadeGPU lightmapCrossfadeGPU;

    private float batteryDuration = 0f;
    private bool isFlashlightOn = false;

    public Image blackBackground;
    public float fadeDuration = 0.5f;

    public String[] IntroTexts;
    public TMP_Text introText;
    private int currentIntroIndex = 0;
    private bool isIntroDone = false;

    // DOTween timings (seconds)
    public float introFadeIn = 1.0f;
    public float introHold = 1.8f;
    public float introFadeOut = 0.8f;
    public float introGap = 0.2f;
    public float bgFadeOutAtEnd = 1.0f;

    // Internal DOTween state
    private Sequence _introSeq;
    private bool _introAnimating = false;


    void setIntensity(float intensity)
    {
        foreach (var light in lights)
        {
            light.GetComponent<Light>().intensity = intensity;
        }
    }

    IEnumerator LerpIntensity(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Calculate interpolation factor [0,1]
            float t = elapsed / duration;

            // Lerp between from and to
            setIntensity(Mathf.Lerp(from, to, t));

            elapsed += Time.deltaTime;
            yield return null; // wait for next frame
        }

        // Ensure it finishes exactly at target
        setIntensity(to);
    }

    // Start is called before the first frame update
    void Start()
    {
        setIntensity(brightIntensity);
        //toggleFlashlight.performed += useBattery;
        flashlight.GetComponent<Light>().enabled = false;
        //lightSwitcher.GetComponent<LevelLightmapData>().LoadLightingScenario(0);
        //if (!SceneManager.GetSceneByName(brightLightingScene).isLoaded &&
        //    !SceneManager.GetSceneByName(dimLightingScene).isLoaded)
        //{
        //    StartCoroutine(SwitchTo(brightLightingScene));
        //}

        ///////////////////////////////////////////////////////
        //List<LightmapData> lightmap = new List<LightmapData>();

        //for (int i = 0; i < brightDir.Length; i++)
        //{
        //    LightmapData lmdata = new LightmapData();

        //    lmdata.lightmapDir = brightDir[i];
        //    //Debug.Log(brightDir[i]);
        //    lmdata.lightmapColor = brightColor[i];

        //    lightmap.Add(lmdata);
        //}

        //brightMap = lightmap.ToArray();
        ///////////////////////////////////////////////////////
        //lightmap = new List<LightmapData>();

        //for (int i = 0; i < dimDir.Length; i++)
        //{
        //    LightmapData lmdata = new LightmapData();

        //    lmdata.lightmapDir = dimDir[i];
        //    lmdata.lightmapColor = dimColor[i];

        //    lightmap.Add(lmdata);
        //}

        //dimMap = lightmap.ToArray();
        ///////////////////////////////////////////////////////
        //lightmap = new List<LightmapData>();

        //for (int i = 0; i < darkDir.Length; i++)
        //{
        //    LightmapData lmdata = new LightmapData();

        //    lmdata.lightmapDir = darkDir[i];
        //    lmdata.lightmapColor = darkColor[i];

        //    lightmap.Add(lmdata);
        //}

        //darkMap = lightmap.ToArray();
        ///////////////////////////////////////////////////////
        //LightmapSettings.lightmaps = brightMap;

        if (introText != null)
        {
            var tc = introText.color;
            tc.a = 0f;
            introText.color = tc;
        }

        // Ensure background starts fully opaque black
        if (blackBackground != null)
        {
            var bc = blackBackground.color;
            bc.a = 1f;
            blackBackground.color = bc;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isFlashlightOn)
        {
            batteryDuration -= Time.deltaTime;
            Debug.Log("Flashlight on! " + batteryDuration);
            if (batteryDuration <= 0f)
            {
                isFlashlightOn = false;
                flashlight.GetComponent<Light>().enabled = false;
                batteryDuration = 0f;
            }
        }
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
    public void startGame()
    {
        StartCoroutine(LerpIntensity(brightIntensity, dimIntensity, firstDuration));
        //fade();
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
                    //StartCoroutine(SwitchTo(dimLightingScene));
                    //lightSwitcher.GetComponent<LevelLightmapData>().LoadLightingScenario(1);
                    //LightmapSettings.lightmaps = dimMap;
                    //lightmapCrossfadeGPU.LightmapsA = LightmapSettings.lightmaps;
                    //lightmapCrossfadeGPU.LightmapsB = dimMap;
                    //lightmapCrossfadeGPU.StartCrossfade();
                    StartCoroutine(LerpIntensity(dimIntensity, darkIntensity, firstDuration));
                    //foreach (GameObject light in lights)
                    //{
                    //    light.GetComponent<Light>().intensity = 0.5f;
                    //}
                    break;
                }
            case 2:
                {
                    current_room = 3;
                    //lightSwitcher.GetComponent<LevelLightmapData>().LoadLightingScenario(2);
                    //LightmapSettings.lightmaps = darkMap;
                    //lightmapCrossfadeGPU.LightmapsA = LightmapSettings.lightmaps;
                    //lightmapCrossfadeGPU.LightmapsB = darkMap;
                    //lightmapCrossfadeGPU.StartCrossfade();
                    //StartCoroutine(LerpIntensity(dimIntensity, darkIntensity, secondDuration));
                    //foreach (GameObject light in lights)
                    //{
                    //    light.GetComponent<Light>().intensity = 0.25f;
                    //}
                    break;
                }
            case 3:
                {
                    //LightmapSettings.lightmaps = brightMap;
                    //lightmapCrossfadeGPU.LightmapsA = LightmapSettings.lightmaps;
                    //lightmapCrossfadeGPU.LightmapsB = brightMap;
                    //lightmapCrossfadeGPU.StartCrossfade();
                    StartCoroutine(LerpIntensity(darkIntensity, brightIntensity, thirdDuration));
                    Debug.Log("Finished!");
                    current_room = 0;
                    //foreach (GameObject light in lights)
                    //{
                    //    light.GetComponent<Light>().intensity = 0f;
                    //}
                    break;
                }
        }
    }

    public void pickupObject(PickupType type)
    {
        switch (type)
        {
            case PickupType.Battery:
                {
                    batteryDuration += 10f;
                    Debug.Log("Current charge: " + batteryDuration);
                    break;
                }
        }
    }

    public void useBattery(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            Debug.Log("Battery Action!");
            if (isFlashlightOn)
            {
                isFlashlightOn = false;
                flashlight.GetComponent<Light>().enabled = false;
            }
            else
            {
                if (batteryDuration > 0)
                {
                    isFlashlightOn = true;
                    flashlight.GetComponent<Light>().enabled = true;
                }
            }
        }
    }

    //public void proceedIntro(InputAction.CallbackContext context)
    //{
    //    if (context.phase == InputActionPhase.Started && !isIntroDone)
    //    {
    //        if(currentIntroIndex >= IntroTexts.Length)
    //        {
    //            introText.enabled = false;
    //            StartCoroutine(LerpBackground(1, 0, fadeDuration));
    //            isIntroDone = true;
    //            return;
    //        }
    //        currentIntroIndex += 1;
    //        string text = "";
    //        for (int i = 0; i < currentIntroIndex; i++)
    //        {
    //            text += IntroTexts[i] + " ";
    //        }
    //        introText.text = text;
    //    }
    //}

    public void proceedIntro(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Started || isIntroDone) return;
        if (_introAnimating) return;

        // If finished all lines, fade out background and end
        if (currentIntroIndex >= IntroTexts.Length)
        {
            isIntroDone = true;
            _introSeq?.Kill();
            if (introText != null) introText.DOFade(0f, 0.15f).SetUpdate(true);

            if (blackBackground != null)
            {
                blackBackground.DOFade(0f, bgFadeOutAtEnd)
                               .SetUpdate(true)
                               .OnComplete(() => { if (introText != null) introText.enabled = false; });
            }
            else if (introText != null) introText.enabled = false;

            return;
        }

        // If text is currently invisible -> fade IN the current line
        if (introText.color.a <= 0.01f)
        {
            string line = IntroTexts[currentIntroIndex];
            introText.text = line;

            var c = introText.color;
            c.a = 0f;
            introText.color = c;

            _introAnimating = true;
            _introSeq?.Kill();
            _introSeq = DOTween.Sequence()
                .Append(introText.DOFade(1f, introFadeIn).SetUpdate(true))
                .OnComplete(() => _introAnimating = false);
        }
        else
        {
            // If text is visible -> fade it OUT and go to next line
            _introAnimating = true;
            _introSeq?.Kill();
            _introSeq = DOTween.Sequence()
                .Append(introText.DOFade(0f, introFadeOut).SetUpdate(true))
                .AppendInterval(introGap)
                .OnComplete(() =>
                {
                    _introAnimating = false;
                    currentIntroIndex++;
                });
        }
    }


    //public void fade()
    //{
    //    StartCoroutine(LerpBackground(1, 0, fadeDuration));

    //    blackBackground.color = new Color(0, 0, 0, 0);
    //}

    //IEnumerator LerpBackground(float from, float to, float duration)
    //{
    //    float elapsed = 0f;

    //    while (elapsed < duration)
    //    {
    //        // Calculate interpolation factor [0,1]
    //        float t = elapsed / duration;

    //        // Lerp between from and to
    //        blackBackground.color = new Color(0, 0, 0, Mathf.Lerp(from, to, t));

    //        elapsed += Time.deltaTime;
    //        yield return null; // wait for next frame
    //    }

    //    // Ensure it finishes exactly at target
    //    blackBackground.color = new Color(0, 0, 0, 0);
    //}

    public void FadeBackground(float toAlpha, float duration)
    {
        if (blackBackground == null) return;
        blackBackground.DOFade(toAlpha, duration).SetUpdate(true);
    }

}

