using UnityEngine;

public class Weakpoint : MonoBehaviour {
  Enemy m_enemy;
  public int Health = 1;
  // bool m_active = true;

  [SerializeField] MeshRenderer m_renderer;
  [SerializeField] Collider m_collider;

  public void Init(Enemy enemy) {
    m_enemy = enemy;
  }

  public void TakeDamage(int damage) {
    Health -= damage;
    m_enemy.TakeDamage(this);
  }

  public void SetActive() {
    m_renderer.material = m_enemy.WeakpointActiveMat;
    m_collider.enabled = true;
    // m_active = true;
  }

  public void SetInactive() {
    m_renderer.material = m_enemy.WeakpointInactiveMat;
    m_collider.enabled = false;
    // m_active = false;
  }
}
