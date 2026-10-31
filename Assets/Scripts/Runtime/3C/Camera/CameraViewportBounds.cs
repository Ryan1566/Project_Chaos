using Cinemachine;
using UnityEngine;

/// <summary>Clamps the final view, including impulse, without writing the output Camera.</summary>
public sealed class CameraViewportBounds : CinemachineExtension
{
    public Camera Output { get; set; }
    public Rect Bounds { get; set; }
    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase camera,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Finalize || Output == null || Bounds.width <= 0 || Bounds.height <= 0) return;
        Vector3 position = state.CorrectedPosition;
        float hy = state.Lens.OrthographicSize, hx = hy * Output.aspect;
        Vector3 confined = position;
        confined.x = Bounds.width <= hx * 2 ? Bounds.center.x : Mathf.Clamp(position.x, Bounds.xMin + hx, Bounds.xMax - hx);
        confined.y = Bounds.height <= hy * 2 ? Bounds.center.y : Mathf.Clamp(position.y, Bounds.yMin + hy, Bounds.yMax - hy);
        state.PositionCorrection += confined - position;
    }
}
