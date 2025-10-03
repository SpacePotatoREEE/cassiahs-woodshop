using UnityEngine;

public static class CaptureOverlayHub
{
    private static CaptureFreezeOverlay _current;
    public static CaptureFreezeOverlay Current => _current;

    public static void Register(CaptureFreezeOverlay overlay)
    {
        _current = overlay;
    }

    public static void Unregister(CaptureFreezeOverlay overlay)
    {
        if (_current == overlay) _current = null;
    }
}