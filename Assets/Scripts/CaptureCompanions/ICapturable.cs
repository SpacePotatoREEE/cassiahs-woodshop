using UnityEngine;

public interface ICapturable
{
    bool CanCapture { get; }
    float GetCaptureChance01(); // 0..1
    void OnCaptureStart();      // disable AI/colliders, etc.
    void OnCaptureComplete(bool success);
    CreatureDefinition GetCreatureDefinition();
    Transform GetCaptureRoot(); // for shrink-to-ball visual
}