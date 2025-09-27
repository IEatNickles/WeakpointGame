using UnityEngine.AI;
using UnityEngine;

public class EnemyAI : MonoBehaviour {
  [SerializeField] Transform m_target;
  [SerializeField] float m_speed;
  [SerializeField] int m_maxHealth;
  int m_health;

  NavMeshAgent m_agent;

  void Awake() {
    m_health = m_maxHealth;
    if (m_target == null) {
      m_target = GameObject.FindGameObjectWithTag("Player").transform;
    }

    m_agent = GetComponent<NavMeshAgent>();
  }

  void Update() {
    m_agent.SetDestination(m_target.position);
  }
}
