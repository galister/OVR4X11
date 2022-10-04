namespace EasyOverlay.Overlay
{
    /// <summary>
    /// Compatible with libopenvr_api tracked device constants.
    /// </summary>
    public enum TrackedDevice : uint
    {
        Hmd = 0u,
        LeftHand,
        RightHand,
        
        None = 64 // Max supported device + 1
    }
}