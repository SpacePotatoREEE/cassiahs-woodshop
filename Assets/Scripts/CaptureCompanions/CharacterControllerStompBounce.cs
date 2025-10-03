using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterControllerStompBounce : MonoBehaviour, IStompBounce
{
    public float gravity = 25f;
    public float maxUpVelocity = 12f;

    private CharacterController _cc;
    private float _vy;

    void Awake() { _cc = GetComponent<CharacterController>(); }

    public void AddVerticalImpulse(float velUp) { _vy = Mathf.Clamp(velUp, 0f, maxUpVelocity); }

    void Update()
    {
        if (_vy != 0f)
        {
            _cc.Move(Vector3.up * _vy * Time.deltaTime);
            _vy = Mathf.MoveTowards(_vy, 0f, gravity * Time.deltaTime);
        }
    }
}