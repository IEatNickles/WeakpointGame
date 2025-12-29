using UnityEngine;

public class RangedEnemy : Enemy {
  [SerializeField] float m_followDistance = 5.0f;
  [SerializeField] float m_maxDistance = 30.0f;
  [SerializeField] float m_eyeHeight = 1.8f;
  [SerializeField] float m_fireRate = 1.0f;
  float m_fireTime;
  [SerializeField] GameObject m_projectile;

  public override void Tick() {
    if ( /*(transform.position - m_target.position).sqrMagnitude > m_maxDistance * m_maxDistance || */
        Physics.Linecast(transform.position + Vector3.up * m_eyeHeight, m_target.position,
          ~(gameObject.layer | Player.PLAYER_LAYER), QueryTriggerInteraction.Ignore)) {
      var dir = (transform.position - m_target.position).normalized;
      m_agent.SetDestination(m_target.position + dir * m_followDistance);
    } else {
      var look = m_target.position - transform.position;
      look.y = 0f;
      look.Normalize();
      transform.rotation = Quaternion.LookRotation(look);

      m_fireTime -= Time.deltaTime;
      if (m_fireTime <= 0f) {
        Instantiate(m_projectile, transform.position + Vector3.up * m_eyeHeight + transform.forward,
          Quaternion.LookRotation(m_target.position - transform.position).normalized);
        m_fireTime = m_fireRate;
      }
    }
  }
}
