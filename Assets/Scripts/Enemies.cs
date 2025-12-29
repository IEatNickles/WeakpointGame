using System.Collections.Generic;

public static class Enemies {
  static readonly HashSet<Enemy> m_enemies = new();

  public static void Init() {
  }

  public static void Tick() {
    foreach (var e in m_enemies) {
      e?.Tick();
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