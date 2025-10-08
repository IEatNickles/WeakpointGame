using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine;

public class Enemy : MonoBehaviour {
  [SerializeField] protected Transform m_target;

  public Material WeakpointActiveMat => m_weakpointActiveMat;
  [SerializeField] Material m_weakpointActiveMat;
  public Material WeakpointInactiveMat => m_weakpointInactiveMat;
  [SerializeField] Material m_weakpointInactiveMat;

  protected NavMeshAgent m_agent;

  [SerializeField] List<Weakpoint> m_weakpoints = new();
  int m_activeWeakpoint = 0;

  void Awake() {
    Debug.Assert(m_weakpoints.Count > 0, "Enemy needs at least one weakpoint");
    foreach (var wp in m_weakpoints) {
      wp.Init(this);
      wp.SetInactive();
    }
    m_weakpoints[0].SetActive();

    if (m_target == null) {
      m_target = GameObject.FindGameObjectWithTag("Player").transform;
    }

    m_agent = GetComponent<NavMeshAgent>();
    m_agent.enabled = false;
    m_agent.enabled = true;

    Enemies.AddEnemy(this);
  }

  public virtual void Tick() {
    m_agent.SetDestination(m_target.position);
  }

  public void TakeDamage(Weakpoint wp) {
    if (wp.Health <= 0) {
      Destroy(wp.gameObject);
      m_activeWeakpoint += 1;
      if (m_activeWeakpoint >= m_weakpoints.Count) {
        Enemies.RemoveEnemy(this);
        Destroy(gameObject);
      } else {
        m_weakpoints[m_activeWeakpoint].SetActive();
      }
    }
  }
}
