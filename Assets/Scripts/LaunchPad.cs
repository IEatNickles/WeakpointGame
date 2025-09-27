using UnityEngine;

public class LaunchPad : MonoBehaviour {
  [SerializeField] float m_force;

  void OnTriggerEnter(Collider other) {
    Rigidbody rb = other.attachedRigidbody;
    if (rb) {
      float vel = Mathf.Abs(rb.linearVelocity.y);
      if (rb.TryGetComponent<Player>(out var player)) {
        if (player.MoveState == Player.MovementState.Slaming) {
          vel = 20f;
        }
        player.MoveState = Player.MovementState.Walking;
      }
      rb.linearVelocity = (transform.rotation * Vector3.up) * (m_force + vel);
    }
  }
}
