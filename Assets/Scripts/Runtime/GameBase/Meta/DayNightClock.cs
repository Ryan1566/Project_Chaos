using UnityEngine;

public enum DayNightPhase { Day, Dusk, Night, Dawn }

/// <summary>每次采样的天气参数。天空表现可以换用独立的局外或局内天气来源。</summary>
public readonly struct DayNightWeather
{
    public readonly float CloudCover;
    public readonly float WindSpeed;
    public readonly Color Tint;

    public DayNightWeather(float cloudCover, float windSpeed, Color tint)
    {
        CloudCover = cloudCover;
        WindSpeed = windSpeed;
        Tint = tint;
    }

    public static DayNightWeather Fair => new DayNightWeather(.46f, .8f, Color.white);
}

public interface IDayNightWeatherSource
{
    DayNightWeather Sample(float minuteOfDay);
}

public readonly struct DayNightSample
{
    public readonly DayNightPhase Phase;
    public readonly float Daylight;
    public readonly float Twilight;
    public readonly float SolarProgress;

    public DayNightSample(DayNightPhase phase, float daylight, float twilight, float solarProgress)
    {
        Phase = phase;
        Daylight = daylight;
        Twilight = twilight;
        SolarProgress = solarProgress;
    }
}

/// <summary>一分钟现实游玩对应一小时钟面时间。改钟只改显示与环境采样。</summary>
public static class DayNightClock
{
    public const float DayLength = 1440;
    public const float StartMinutes = 480;
    private static bool runStarted;
    public static float RunMinutes { get; private set; } = StartMinutes;
    internal static void RestoreRunTime(float minutes) => RunMinutes = Mathf.Repeat(minutes, DayLength);
    public static float VillageMinutes
    {
        get
        {
            var save = GameSession.Instance.LoadedPayload;
            return save?.hasVillageDayTime == true ? Normalize(save.villageDayMinutes) : StartMinutes;
        }
    }

    public static IDayNightWeatherSource VillageWeather { get; set; }
    public static IDayNightWeatherSource RunWeather { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntime()
    {
        ClearRun();
        VillageWeather = null;
        RunWeather = null;
    }

    public static void BeginRun()
    {
        RunMinutes = StartMinutes;
        runStarted = true;
    }

    public static void ClearRun()
    {
        RunMinutes = StartMinutes;
        runStarted = false;
    }

    internal static void TickVillage(SavePayload save, float elapsedSeconds)
    {
        if (save == null || !ValidElapsed(elapsedSeconds)) return;
        float previous = save.hasVillageDayTime ? Normalize(save.villageDayMinutes) : StartMinutes;
        save.villageDayMinutes = Normalize(previous + elapsedSeconds);
        save.hasVillageDayTime = true;
    }

    internal static void TickRun(float elapsedSeconds)
    {
        if (runStarted && ValidElapsed(elapsedSeconds)) RunMinutes = Normalize(RunMinutes + elapsedSeconds);
    }

    internal static bool TrySetRunHour(int hour)
    {
        if (!runStarted || hour < 0 || hour > 23) return false;
        RunMinutes = hour * 60;
        return true;
    }

    public static DayNightSample Evaluate(float minute, bool inRun)
    {
        minute = Normalize(minute);
        float dawn = inRun ? 360 : 300;
        float dusk = inRun ? 1080 : 1140;
        float halfTransition = inRun ? 45 : 60;
        float daylight;
        DayNightPhase phase;
        if (minute >= dawn - halfTransition && minute < dawn + halfTransition)
        {
            phase = DayNightPhase.Dawn;
            daylight = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(dawn - halfTransition, dawn + halfTransition, minute));
        }
        else if (minute >= dusk - halfTransition && minute < dusk + halfTransition)
        {
            phase = DayNightPhase.Dusk;
            daylight = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(dusk - halfTransition, dusk + halfTransition, minute));
        }
        else
        {
            phase = minute >= dawn && minute < dusk ? DayNightPhase.Day : DayNightPhase.Night;
            daylight = phase == DayNightPhase.Day ? 1 : 0;
        }
        float twilight = 4 * daylight * (1 - daylight);
        return new DayNightSample(phase, daylight, twilight, Mathf.InverseLerp(dawn, dusk, minute));
    }

    public static string DescribeRunHour(int hour)
    {
        if (hour < 0 || hour > 23) return "时间无效";
        return DescribeRunMinute(hour * 60);
    }

    public static string DescribeRunMinute(float minute)
    {
        switch (Evaluate(minute, true).Phase)
        {
            case DayNightPhase.Day: return "白昼";
            case DayNightPhase.Dusk: return "黄昏";
            case DayNightPhase.Dawn: return "黎明";
            default: return "黑夜";
        }
    }

    private static bool ValidElapsed(float value) => value > 0 && !float.IsInfinity(value);
    private static float Normalize(float value) => float.IsNaN(value) || float.IsInfinity(value)
        ? StartMinutes : Mathf.Repeat(value, DayLength);
}
