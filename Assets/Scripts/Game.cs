using UnityEngine;

public class Game : MonoBehaviour {
  public static Game Instance => m_instance;
  static Game m_instance;

  [SerializeField] Player m_player;

  void Start() {
    m_instance = this;
    Enemies.Init();
  }

  void Update() {
    Enemies.Tick();
  }

  void OnDestroy() {
    Enemies.Dispose();
  }
}
