using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class Enemies {
  static HashSet<Enemy> m_enemies = new();

  public static void Init() {
  }

  public static void Tick() {
    foreach (var e in m_enemies) {
      if (e != null) {
        e.Tick();
      }
    }
  }

  public static void AddEnemy(Enemy enemy) {
    m_enemies.Add(enemy);
  }

  public static void RemoveEnemy(Enemy enemy) {
    m_enemies.Remove(enemy);
  }

  public static void Dispose() {
    m_enemies.Clear();
  }
}
