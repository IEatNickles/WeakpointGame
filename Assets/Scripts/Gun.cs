using UnityEngine;

public class Gun : MonoBehaviour {
  public enum Type {
    Raycast,
    Object,
  }
  [SerializeField] Type m_type;

  [SerializeField] float m_damage;
  [SerializeField] float m_range;
  [SerializeField] float m_fireRate;
  float m_cooldown;
  [SerializeField] GameObject m_bullet;

  public void Shoot(Vector3 eye, Vector3 direction) {
    switch (m_type) {
      case Type.Raycast:
        if (Physics.Raycast(eye, direction, out var hit, m_range, ~Player.PLAYER_LAYER)) {
          Debug.Log(hit.collider.name);
        }
        break;
      case Type.Object:
        break;
    }
    m_cooldown = m_fireRate;
  }

  void Update() {
    if (m_cooldown > 0f) {
      m_cooldown -= Time.deltaTime;
    }
  }
}
