using DG.Tweening;
using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum PickupType
{
    Battery,
    Key,
    BoxKey,
    FlashLight,
    Note
}

public class GameManager : MonoBehaviour
{
    private int current_room = 1;
    private int current_difficulty = 1;

    public GameObject door_1;
    public GameObject door_2;

    public GameObject[] lights;
    public float[] brightIntensity = {0.7f, 0.7f, 0.7f};
    public float[] dimIntensity = {0.2f, 0.2f, 0.2f};
    public float[] darkIntensity = { 0.02f, 0.02f, 0.02f };
    private float currentIntensity = 1f;
    public float[] room1DimDuration = {60f, 60f, 60f};
    public float[] room2DimDuration = {60f, 60f, 60f};
    public float[] room3DimDuration = {5f, 5f, 5f};

    public Texture2D[] brightDir, brightColor;
    public Texture2D[] dimDir, dimColor;
    public Texture2D[] darkDir, darkColor;

    public GameObject flashlight;
    public GameObject flashlightBody;
    private bool hasFlashlight = false;

    private float batteryDuration = 0f;
    private bool isFlashlightOn = false;

    public Image blackBackground;
    public float fadeDuration = 0.5f;
    public TMP_Text letter;
    public Image letterBack;
    public float letterFade = 2f;

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
    private DG.Tweening.Sequence _Seq;
    private bool _Animating = false;

    // Inventory flags
    private bool hasKeyRoom1 = false;
    private bool hasKeyRoom2 = false;
    private bool hasKeyBox = false;

    // coroutines
    private Coroutine room2Coroutine = null;

    public InputActionReference VrIntroAction;
    public InputActionReference VrFlashlightAction;

    public GameObject hintBack;
    public TMP_Text hint;
    public float hintTimeout = 5f;
    public String[] room1Hints;
    public float[] room1HintDelays;
    public String[] room2Hints;
    public float[] room2HintDelays;
    public String[] room3Hints;
    public float[] room3HintDelays;
    private int currentHintIndex = 0;
    private Coroutine hintCoroutine = null;

    public GameObject player;

    public GameObject MotionSicknessFilter;

    private void Awake()
    {
        if (VrIntroAction != null)
        {
            VrIntroAction.action.Enable();
            VrIntroAction.action.performed += proceedIntro;
        }
        if (VrFlashlightAction != null)
        {
            VrFlashlightAction.action.Enable();
            VrFlashlightAction.action.performed += useBattery;
        }

        if (MotionSicknessFilter != null)
        {
            MotionSicknessFilter.SetActive(PlayerPrefs.GetInt("m") == 1);
        }
    }

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
            currentIntensity = Mathf.Lerp(from, to, t);

            elapsed += Time.deltaTime;
            yield return null; // wait for next frame
        }

        // Ensure it finishes exactly at target
        setIntensity(to);
        currentIntensity = to;
    }

    // Start is called before the first frame update
    void Start()
    {
        current_difficulty = PlayerPrefs.GetInt("d", 1);

        setIntensity(brightIntensity[current_difficulty]);
        flashlight.GetComponent<Light>().enabled = false;
        flashlightBody.SetActive(false);
        stopPlayer();

        if (introText != null)
        {
            var tc = introText.color;
            tc.a = 0f;
            introText.color = tc;
        }

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
                flashlightBody.SetActive(false);
                batteryDuration = 0f;
            }
        }
    }
    
    public void startGame()
    {
        Debug.Log("Started!");
        StartCoroutine(
            LerpIntensity(
                currentIntensity,
                brightIntensity[current_difficulty],
                room1DimDuration[current_difficulty]
                )
            );
        if (room1Hints.Length > 0)
        {
            DoWait(room1HintDelays[0], () => {
                handleHints();
            });
        }
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
                        Debug.Log("Room 1 cleared");
                    }
                    break;
                }
            case 2:
                {
                    if (obj_code == 2)
                    {
                        door_2.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
                        Debug.Log("Room 2 cleared");
                    }
                    break;
                }
            case 3:
                {
                    if (obj_code == 3)
                    {
                        Debug.Log("Done!");
                    }
                    break;
                }
        }
    }

    public void enteredRoom(int room)
    {
        Debug.Log("Entered room " + room);
        switch (room)
        {
            case 2:
                {
                    door_1.transform.rotation = new Quaternion();
                    door_1.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                    room2Coroutine = StartCoroutine(
                        LerpIntensity(
                            currentIntensity,
                            dimIntensity[current_difficulty],
                            room2DimDuration[current_difficulty]
                            )
                        );
                    current_room = 2;
                    if(hintCoroutine != null)
                    {
                        StopCoroutine( hintCoroutine );
                        hintCoroutine = null;
                    }
                    currentHintIndex = 0;
                    if (room2Hints.Length > 0)
                    {
                        DoWait(room2HintDelays[0], () => {
                            handleHints();
                        });
                    }
                    break;
                }
            case 3:
                {
                    door_2.transform.rotation = new Quaternion();
                    door_2.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                    if(room2Coroutine != null)
                    {
                        StopCoroutine(room2Coroutine);
                    }
                    StartCoroutine(
                        LerpIntensity(
                            currentIntensity,
                            darkIntensity[current_difficulty],
                            room3DimDuration[current_difficulty]));
                    current_room = 3;
                    if (hintCoroutine != null)
                    {
                        StopCoroutine(hintCoroutine);
                        hintCoroutine = null;
                    }
                    currentHintIndex = 0;
                    if (room3Hints.Length > 0)
                    {
                        DoWait(room3HintDelays[0], () => {
                            handleHints();
                        });
                    }
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
            case PickupType.FlashLight:
                {
                    hasFlashlight = true;
                    if (hasKeyRoom1)
                    {
                        Debug.Log("Fuck you fl");
                        objective_is_done(1);
                    }
                    break;
                }
            case PickupType.Key:
                {
                    Debug.Log("Picked up key");
                    if (current_room == 1)
                    {
                        hasKeyRoom1 = true;
                        if (hasFlashlight)
                        {
                            Debug.Log("Fuck you");
                            objective_is_done(1);
                        }
                    }
                    else if (current_room == 2)
                    {
                        hasKeyRoom2 = true;
                        if (hasKeyBox)
                        {
                            objective_is_done(2);
                        }
                    }
                    break;
                }
            case PickupType.BoxKey:
                {
                    hasKeyBox = true;
                    if (hasKeyRoom2)
                    {
                        objective_is_done(2);
                    }
                    break;
                }
            case PickupType.Note:
                {
                    stopPlayer();
                    OpenChest();
                    break;
                }
        }
    }

    public void useBattery(InputAction.CallbackContext context)
    {
        Debug.Log("Using Battry...");
        Debug.Log(context.phase);
        //if (context.phase == InputActionPhase.Started)
        if (context.phase == InputActionPhase.Performed)
            {
            Debug.Log("Battery Action!");
            if (hasFlashlight && current_room == 3)
            {
                if (isFlashlightOn)
                {
                    isFlashlightOn = false;
                    flashlightBody.SetActive(false);
                    flashlight.GetComponent<Light>().enabled = false;
                }
                else
                {
                    if (batteryDuration > 0)
                    {
                        isFlashlightOn = true;
                        flashlightBody.SetActive(true);
                        flashlight.GetComponent<Light>().enabled = true;
                    }
                }
            }
        }
    }

    public void proceedIntro(InputAction.CallbackContext context)
    {
        //Debug.Log("Proceed intro! " + context.phase);
        if (
            //context.phase != InputActionPhase.Started
            context.phase != InputActionPhase.Performed
            || isIntroDone) return;

        if (_Animating) return;

        // If finished all lines, fade out background and end
        if (currentIntroIndex >= IntroTexts.Length)
        {
            isIntroDone = true;
            _Seq?.Kill();
            if (introText != null) introText.DOFade(0f, 0.15f).SetUpdate(true);

            if (blackBackground != null)
            {
                blackBackground.DOFade(0f, bgFadeOutAtEnd)
                               .SetUpdate(true)
                               .OnComplete(() => {
                                   if (introText != null) 
                                       introText.enabled = false;
                                   freePlayer();
                               });
            }
            else if (introText != null) introText.enabled = false;

            startGame();
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

            _Animating = true;
            _Seq?.Kill();
            _Seq = DOTween.Sequence()
                .Append(introText.DOFade(1f, introFadeIn).SetUpdate(true))
                .OnComplete(() => _Animating = false);
        }
        else
        {
            // If text is visible -> fade it OUT and go to next line
            _Animating = true;
            _Seq?.Kill();
            _Seq = DOTween.Sequence()
                .Append(introText.DOFade(0f, introFadeOut).SetUpdate(true))
                .AppendInterval(introGap)
                .OnComplete(() =>
                {
                    _Animating = false;
                    currentIntroIndex++;
                });
        }
    }

    public void FadeBackground(float toAlpha, float duration)
    {
        if (blackBackground == null) return;
        blackBackground.DOFade(toAlpha, duration).SetUpdate(true);
    }

    public void OpenChest()
    {
        blackBackground.DOFade(1f, letterFade);
        letterBack.DOFade(1f, letterFade);
        letter.DOFade(1f, letterFade);
    }

    public void PopUp(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            //hint.text = "Hey, a hint!";
            hintBack.GetComponent<RectTransform>().DOAnchorPos(new Vector2(0, 100), 2f);
        }
    }

    public void PopDown(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            hintBack.GetComponent<RectTransform>().DOAnchorPos(new Vector2(0, -100), 2f);
        }
    }

    public void stopPlayer()
    {
        player.GetComponent<PlayerController>().enabled = false;
    }

    public void freePlayer()
    {
        player.GetComponent<PlayerController>().enabled = true;
    }

    void DoWait(float duration, Action action)
    {
        hintCoroutine = StartCoroutine(WaitForThing(duration, action));
    }

    IEnumerator WaitForThing(float wait, Action action)
    {
        yield return new WaitForSeconds(wait);
        action.Invoke();
    }

    void showHint(float delay)
    {
        hintBack.GetComponent<RectTransform>().DOAnchorPos(new Vector2(0, 100), 2f);
        StartCoroutine(WaitForThing(hintTimeout, () =>
        {
            hintBack.GetComponent<RectTransform>().DOAnchorPos(new Vector2(0, -100), 2f);
            if (delay > 0)
            {
                DoWait(delay, () =>
                {
                    handleHints();
                });
            }
        }));
    }

    void handleHints()
    {
        hintCoroutine = null;
        switch (current_room)
        {
            case 1:
                {
                    if (currentHintIndex < room1Hints.Length)
                    {
                        hint.text = room1Hints[currentHintIndex];
                        currentHintIndex++;
                        if (currentHintIndex < room1Hints.Length)
                        {
                            showHint(room1HintDelays[currentHintIndex]);
                        }
                        else
                        {
                            showHint(-1);
                        }
                    }
                    break;
                }
            case 2:
                {
                    if (currentHintIndex < room2Hints.Length)
                    {
                        hint.text = room2Hints[currentHintIndex];
                        currentHintIndex++;
                        if (currentHintIndex < room2Hints.Length)
                        {
                            showHint(room2HintDelays[currentHintIndex]);
                        }
                        else
                        {
                            showHint(-1);
                        }
                    }
                    break;
                }
            case 3:
                {
                    if (currentHintIndex < room3Hints.Length)
                    {
                        hint.text = room3Hints[currentHintIndex];
                        currentHintIndex++;
                        if (currentHintIndex < room3Hints.Length)
                        {
                            showHint(room3HintDelays[currentHintIndex]);
                        }
                        else
                        {
                            showHint(-1);
                        }
                    }
                    break;
                }
        }
    }
}

