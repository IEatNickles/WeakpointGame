using UnityEngine;

public enum BillboardType {
  LookAt,
  Face,
}

public class Billboard : MonoBehaviour {
  [SerializeField] BillboardType m_type;
  [SerializeField] Camera m_camera;

  [SerializeField] bool m_lockX;
  [SerializeField] bool m_lockY;
  [SerializeField] bool m_lockZ;

  Vector3 m_originalRot;

  void Awake() {
    if (!m_camera) {
      m_camera = Camera.main;
    }
    m_originalRot = transform.eulerAngles;
  }

  void Update() {
    switch (m_type) {
      case BillboardType.LookAt:
        transform.LookAt(m_camera.transform.position);
        break;
      case BillboardType.Face:
        transform.forward = -m_camera.transform.forward;
        break;
    }
    Vector3 rot = transform.eulerAngles;
    if (m_lockX)
      rot.x = m_originalRot.x;
    if (m_lockY)
      rot.y = m_originalRot.y;
    if (m_lockZ)
      rot.z = m_originalRot.z;
    transform.eulerAngles = rot;
  }
}
