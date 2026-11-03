using RunMap;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>局部相机背景与世界着色。屏幕覆盖式 HUD 在相机绘制后合成。</summary>
[RequireComponent(typeof(Camera))]
public sealed class DayNightSkyRenderer : MonoBehaviour
{
    private Camera output;
    private Material sky;
    private Mesh quad;
    private CommandBuffer background, tint;
    private GameplayCameraRig.CameraMode mode;
    private NaturalMapPlan sampledPlan;
    private Texture2D surfaceProfile;
    private float previousMinute, cloudPhase;
    private bool cloudClockValid;

    public static DayNightSkyRenderer Attach(Camera camera, GameplayCameraRig.CameraMode mode)
    {
        var renderer = camera.GetComponent<DayNightSkyRenderer>();
        if (renderer == null) renderer = camera.gameObject.AddComponent<DayNightSkyRenderer>();
        renderer.mode = mode;
        renderer.enabled = true;
        return renderer;
    }

    private void Awake()
    {
        output = GetComponent<Camera>();
        var shader = Resources.Load<Shader>("3C/DayNightSky");
        if (shader == null || !shader.isSupported)
        {
            Debug.LogWarning("昼夜天空材质不可用，保留原相机背景。");
            return;
        }
        sky = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        quad = new Mesh { name = "DayNightScreenQuad", hideFlags = HideFlags.HideAndDontSave };
        quad.vertices = new[] { new Vector3(-1,-1), new Vector3(-1,1), new Vector3(1,1), new Vector3(1,-1) };
        quad.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
        quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        quad.UploadMeshData(true);
        background = new CommandBuffer { name = "昼夜 · 天空背景" };
        tint = new CommandBuffer { name = "昼夜 · 世界调色" };
        background.DrawMesh(quad, Matrix4x4.identity, sky, 0, 0);
        tint.DrawMesh(quad, Matrix4x4.identity, sky, 0, 1);
    }

    private void OnEnable()
    {
        if (background == null) return;
        output.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, background);
        output.AddCommandBuffer(CameraEvent.AfterForwardAlpha, tint);
    }

    private void OnDisable()
    {
        if (output == null || background == null) return;
        output.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, background);
        output.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, tint);
    }

    private void OnPreCull()
    {
        if (sky == null) return;
        bool inRun = mode == GameplayCameraRig.CameraMode.Run;
        float minute = inRun ? DayNightClock.RunMinutes : DayNightClock.VillageMinutes;
        var sample = DayNightClock.Evaluate(minute, inRun);
        var source = inRun ? DayNightClock.RunWeather : DayNightClock.VillageWeather;
        var weather = source?.Sample(minute) ?? DayNightWeather.Fair;
        if (cloudClockValid)
        {
            // 午夜回绕保持连续，扎营大幅拨钟只移动太阳和光照。
            float elapsed = Mathf.DeltaAngle(previousMinute / 4, minute / 4) * 4;
            if (elapsed > 0 && elapsed < 2) cloudPhase += elapsed * .00044f * Mathf.Clamp(weather.WindSpeed, -2, 2);
        }
        previousMinute = minute;
        cloudClockValid = true;
        sky.SetVector("_Environment", new Vector4(sample.Daylight, sample.Twilight, sample.SolarProgress, output.aspect));
        sky.SetColor("_WeatherTint", weather.Tint);
        sky.SetVector("_Clouds", new Vector4(Mathf.Clamp01(weather.CloudCover), cloudPhase, 0, 0));

        // 最暗的红色通道也保留 75%，避免地面边沿与角色轮廓陷入黑色。
        Color world = Color.Lerp(new Color(.75f, .82f, .93f, 1), Color.white, sample.Daylight);
        world.g *= Mathf.Lerp(1, .95f, sample.Twilight);
        sky.SetVector("_WorldTint", new Vector4(world.r, world.g, world.b, 1));

        NaturalMapPlan plan = inRun ? NaturalMapRuntime.Current : null;
        if (!ReferenceEquals(sampledPlan, plan)) RefreshSurface(plan);
        float height = output.orthographicSize * 2;
        float width = height * output.aspect;
        Vector3 origin = output.transform.position;
        sky.SetVector("_WorldRect", new Vector4(origin.x - width / 2, origin.y - height / 2, width, height));
        bool hasSurface = plan != null && surfaceProfile != null;
        sky.SetVector("_CaveParams", hasSurface ? new Vector4(1, 1f / plan.Width, plan.Height) : Vector4.zero);
    }

    private void RefreshSurface(NaturalMapPlan plan)
    {
        sampledPlan = plan;
        if (surfaceProfile != null) Destroy(surfaceProfile);
        surfaceProfile = null;
        if (plan?.SurfaceY == null || plan.Width <= 0 || plan.Height <= 0) return;
        surfaceProfile = new Texture2D(plan.Width, 1, TextureFormat.RGBA32, false, true)
        {
            name = "DayNightSurfaceProfile",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
            hideFlags = HideFlags.HideAndDontSave
        };
        var pixels = new Color32[plan.Width];
        for (int x = 0; x < pixels.Length; x++)
        {
            byte height = (byte)Mathf.RoundToInt(Mathf.Clamp01(plan.SurfaceY[x] / (float)plan.Height) * 255);
            pixels[x] = new Color32(height, 0, 0, 255);
        }
        surfaceProfile.SetPixels32(pixels);
        surfaceProfile.Apply(false, true);
        sky.SetTexture("_SurfaceProfile", surfaceProfile);
    }

    private void OnDestroy()
    {
        background?.Release();
        tint?.Release();
        if (sky != null) Destroy(sky);
        if (quad != null) Destroy(quad);
        if (surfaceProfile != null) Destroy(surfaceProfile);
    }
}
