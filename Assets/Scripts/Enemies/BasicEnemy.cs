using UnityEngine;

public class BasicEnemy : Enemy {
  private static readonly int SwingIdx = Animator.StringToHash("SwingIdx");
  private static readonly int Swing = Animator.StringToHash("Swing");

  [SerializeField] float m_attackDistance = 1.5f;
  [SerializeField] float m_attackDelay = 1f;
  float m_attackTime;
  [SerializeField] int m_damage = 1;

  [SerializeField] Animator m_anim;

  public override void Tick() {
    if ((Time.time - m_attackTime) >= m_attackDelay &&
        Vector3.Distance(transform.position, m_target.position) <= m_attackDistance) {
      if (m_target.TryGetComponent<Player>(out var plr)) {
        m_anim.SetInteger(SwingIdx, Random.Range(0, 2));
        m_anim.SetTrigger(Swing);
        plr.TakeDamage(m_damage);
        m_attackTime = Time.time;
      }
    }

    m_agent.SetDestination(m_target.position);
  }
}