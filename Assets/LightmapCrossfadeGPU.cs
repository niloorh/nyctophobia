// LightmapCrossfadeGPU.cs
// Put this in: Assets/Scripts/Lighting/LightmapCrossfadeGPU.cs

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LightmapCrossfadeGPU : MonoBehaviour
{
    [Header("Input lightmap sets (same size/order/format per index)")]
    public LightmapData[] LightmapsA;
    public LightmapData[] LightmapsB;

    [Header("Compute shader")]
    public ComputeShader BlendCompute; // assign LightmapBlend.compute

    [Header("Timing")]
    [Range(0.05f, 10f)] public float Duration = 1.0f;

    [Header("Tuning")]
    [Tooltip("If true, also blends directional lightmaps (CombinedDirectional).")]
    public bool BlendDirectional = true;

    [Tooltip("Downsample factor for blending (1 = full res). 2 halves each dimension (¼ pixels).")]
    [Range(1, 4)] public int Downsample = 1;

    [Tooltip("How many blended updates per second (limits GPU readbacks).")]
    [Range(10, 120)] public int UpdatesPerSecond = 45;

    // Working allocations
    private LightmapData[] _workingLightmaps;
    private Texture2D[] _workColorCPU; // final CPU textures assigned to LightmapSettings
    private Texture2D[] _workDirCPU;

    private RenderTexture[] _rtColor;  // GPU outputs
    private RenderTexture[] _rtDir;

    private int _kernel;
    private int _lastW, _lastH;

    // For throttling readbacks
    private float _timeAccum;
    private float _updateInterval;

    public bool IsBlending { get; private set; }

    void Awake()
    {
        Debug.Log("Awake func...");
        _kernel = BlendCompute.FindKernel("CSMain");
        _updateInterval = 1.0f / Mathf.Max(1, UpdatesPerSecond);
    }

    // Entry point you call to start the crossfade
    public void StartCrossfade()
    {
        if (IsBlending) return;

        // Basic validation
        if (LightmapsA == null || LightmapsB == null || LightmapsA.Length == 0 || LightmapsB.Length == 0)
        {
            Debug.LogError("[LightmapCrossfadeGPU] Missing lightmap sets.");
            return;
        }
        if (LightmapsA.Length != LightmapsB.Length)
        {
            Debug.LogError("[LightmapCrossfadeGPU] A/B arrays must have same length.");
            return;
        }

        // Ensure formats/sizes per index are compatible and set up allocations
        if (!PrepareWorkingSets())
        {
            Debug.LogError("[LightmapCrossfadeGPU] Preparation failed. Check sizes/formats.");
            return;
        }

        IsBlending = true;
        StartCoroutine(CO_BlendRoutine());
    }

    private bool PrepareWorkingSets()
    {
        int n = LightmapsA.Length;
        _workingLightmaps = new LightmapData[n];
        _workColorCPU = new Texture2D[n];
        _workDirCPU = new Texture2D[n];
        _rtColor = new RenderTexture[n];
        _rtDir = new RenderTexture[n];

        for (int i = 0; i < n; i++)
        {
            var aC = LightmapsA[i].lightmapColor as Texture2D;
            var bC = LightmapsB[i].lightmapColor as Texture2D;
            if (!aC || !bC)
            {
                Debug.LogError($"[LightmapCrossfadeGPU] Missing color texture at index {i}.");
                return false;
            }

            if (aC.width != bC.width || aC.height != bC.height)
            {
                Debug.LogError($"[LightmapCrossfadeGPU] Size mismatch in color LM {i}: A={aC.width}x{aC.height}, B={bC.width}x{bC.height}");
                return false;
            }

            int w = aC.width;// / Mathf.Max(1, Downsample);
            int h = aC.height;// / Mathf.Max(1, Downsample);
            if (w < 4 || h < 4)
            {
                Debug.LogWarning($"[LightmapCrossfadeGPU] Very small target size ({w}x{h}) after downsample.");
            }

            // GPU output RT (uncompressed, HDR)
            _rtColor[i] = NewHDRRT(w, h);

            // CPU texture that LightmapSettings will reference (must be Texture2D)
            _workColorCPU[i] = new Texture2D(w, h, TextureFormat.RGBAHalf, false, true); // Linear=true

            // Working lightmap data entry
            _workingLightmaps[i] = new LightmapData
            {
                lightmapColor = _workColorCPU[i]
            };

            // Directional maps if present & requested
            var aD = LightmapsA[i].lightmapDir as Texture2D;
            var bD = LightmapsB[i].lightmapDir as Texture2D;
            if (BlendDirectional && aD && bD)
            {
                if (aD.width != bD.width || aD.height != bD.height)
                {
                    Debug.LogError($"[LightmapCrossfadeGPU] Size mismatch in dir LM {i}.");
                    return false;
                }

                int wd = aD.width / Mathf.Max(1, Downsample);
                int hd = aD.height / Mathf.Max(1, Downsample);

                _rtDir[i] = NewHDRRT(wd, hd);
                _workDirCPU[i] = new Texture2D(wd, hd, TextureFormat.RGBAHalf, false, true);
                _workingLightmaps[i].lightmapDir = _workDirCPU[i];
            }
        }

        // Pre-assign working set so we only update pixel data during the fade
        LightmapSettings.lightmaps = _workingLightmaps;
        return true;
    }

    private RenderTexture NewHDRRT(int w, int h)
    {
        var rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        rt.enableRandomWrite = true;
        rt.filterMode = FilterMode.Bilinear;
        rt.wrapMode = TextureWrapMode.Clamp;
        rt.Create();
        return rt;
    }

    private IEnumerator CO_BlendRoutine()
    {
        float t = 0f;
        _timeAccum = 0f;

        // Pre-bind static shader resources that don't change per lightmap
        int n = LightmapsA.Length;

        // We’ll update pixels at a fixed rate (UpdatesPerSecond) to avoid too-frequent GPU readbacks.
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / Mathf.Max(0.0001f, Duration));
            _timeAccum += Time.deltaTime;

            // Dispatch compute for each LM set (color & dir)
            for (int i = 0; i < n; i++)
            {
                // Color
                DispatchBlend(
                    LightmapsA[i].lightmapColor as Texture2D,
                    LightmapsB[i].lightmapColor as Texture2D,
                    _rtColor[i],
                    t
                );

                // Directional (if any)
                if (_rtDir[i] != null && _workDirCPU[i] != null)
                {
                    DispatchBlend(
                        LightmapsA[i].lightmapDir as Texture2D,
                        LightmapsB[i].lightmapDir as Texture2D,
                        _rtDir[i],
                        t
                    );
                }
            }

            // Only readback & apply at the chosen rate
            if (_timeAccum >= _updateInterval || t >= 1f)
            {
                yield return StartCoroutine(ReadbackAndApply(n));
                _timeAccum = 0f;

                // Update ambient if needed (different skybox/probes baked)
                DynamicGI.UpdateEnvironment();
            }

            // Let a frame pass
            yield return null;
        }

        // Snap to final authoritative set (frees our working textures if you want)
        LightmapSettings.lightmaps = LightmapsB;

        // Cleanup working allocations (optional; keep if you’ll blend again)
        CleanupWorking();

        IsBlending = false;
    }

    private void DispatchBlend(Texture2D a, Texture2D b, RenderTexture outRT, float blend)
    {
        int w = outRT.width;
        int h = outRT.height;

        // Set inputs
        BlendCompute.SetTexture(_kernel, "_TexA", a);
        BlendCompute.SetTexture(_kernel, "_TexB", b);
        BlendCompute.SetTexture(_kernel, "_Out", outRT);

        // Params
        BlendCompute.SetFloat("_Blend", blend);
        BlendCompute.SetVector("_InvTexSize", new Vector2(1f / w, 1f / h));

        // If the source textures are larger and we downsample, hardware sampling handles it.
        // Make sure sampler is linear clamp in the compute shader.

        int gx = (w + 7) / 8;
        int gy = (h + 7) / 8;
        BlendCompute.Dispatch(_kernel, gx, gy, 1);
    }

    private IEnumerator ReadbackAndApply(int n)
    {
        // Kick all readbacks, then wait
        var requests = new List<AsyncGPUReadbackRequest>(n * 2);
        for (int i = 0; i < n; i++)
        {
            requests.Add(AsyncGPUReadback.Request(_rtColor[i]));
            if (_rtDir[i] != null) requests.Add(AsyncGPUReadback.Request(_rtDir[i]));
        }

        // Wait without piling up new requests
        for (int k = 0; k < requests.Count; k++)
        {
            var req = requests[k];
            while (!req.done) yield return null;
            if (req.hasError)
            {
                Debug.LogWarning("[LightmapCrossfadeGPU] Readback error. Lower UpdatesPerSecond or switch graphics API (DX11/Vulkan).");
                yield break;
            }
        }

        // Apply results…
        int idx = 0;
        for (int i = 0; i < n; i++)
        {
            var col = requests[idx++].GetData<byte>();
            _workColorCPU[i].LoadRawTextureData(col);
            _workColorCPU[i].Apply(false, false);

            if (_rtDir[i] != null)
            {
                var dir = requests[idx++].GetData<byte>();
                _workDirCPU[i].LoadRawTextureData(dir);
                _workDirCPU[i].Apply(false, false);
            }
        }
    }


    private void CleanupWorking()
    {
        if (_rtColor != null)
        {
            foreach (var rt in _rtColor) if (rt) rt.Release();
        }
        if (_rtDir != null)
        {
            foreach (var rt in _rtDir) if (rt) rt.Release();
        }
        _rtColor = null;
        _rtDir = null;
        _workColorCPU = null;
        _workDirCPU = null;
        _workingLightmaps = null;
    }

#if UNITY_EDITOR
    // Quick test in editor: start on play
    [ContextMenu("Test Crossfade (Play Mode)")]
    private void TestCrossfade()
    {
        if (Application.isPlaying) StartCrossfade();
    }
#endif
}
