using System;
using UnityEngine;

public class Projectile : MonoBehaviour {
  [SerializeField] float m_speed = 30;
  [SerializeField] float m_radius = 1.0f;
  [SerializeField] int m_damage = 1;
  Rigidbody m_rb;

  void Awake() {
    m_rb = GetComponent<Rigidbody>();
    m_rb.linearVelocity = transform.forward * m_speed;
  }

  public void ChangeDirection(Vector3 direction) {
    transform.forward = direction;
    m_rb.linearVelocity = direction * m_speed;
  }

  void OnTriggerEnter(Collider col) {
    if (col.isTrigger) {
      return;
    }

    if (col.gameObject.TryGetComponent(out Player p)) {
      p.TakeDamage(m_damage);
    }

    Destroy(gameObject);
  }
}