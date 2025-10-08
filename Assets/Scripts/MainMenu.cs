using UnityEngine;
using TMPro;

[System.Serializable]
public enum MenuType {
  Main,
  Play,
  Settings
}

public class MainMenu : MonoBehaviour {
  Transform m_currentMenu;
  Transform m_nextMenu;

  [SerializeField] Transform m_mainMenu;
  [SerializeField] Transform m_playMenu;
  [SerializeField] Transform m_settingsMenu;

  [SerializeField] float m_transitionTime = 0.2f;
  float m_endTime = 0f;

  [SerializeField] float m_steps = 12f;

  static readonly float m_hiddenBotY = -Screen.height;
  static readonly float m_shownY = 0;
  static readonly float m_hiddenTopY = Screen.height;

  bool m_transitioning = false;

  [SerializeField] TMP_Text m_commandText;
  [SerializeField] float m_timeBetweenCommandChars = 0.05f;
  int m_currentCommandIdx = 0;
  bool m_enteringCommand = false;
  float m_nextCharTime = 0f;

  void Start() {
    m_playMenu.gameObject.SetActive(false);
    m_settingsMenu.gameObject.SetActive(false);
    TransitionTo(MenuType.Main);
    ResetCommand();
  }

  void Update() {
    if (m_transitioning) {
      float t = 1f - (m_endTime - Time.time) / m_transitionTime;
      float hs = Screen.height / m_steps;
      if (m_currentMenu) {
        Vector3 pos = m_currentMenu.localPosition;
        pos.y = Mathf.Floor(Mathf.Lerp(m_shownY, m_hiddenTopY, t) / hs) * hs;
        m_currentMenu.localPosition = pos;
      }
      {
        Vector3 pos = m_nextMenu.localPosition;
        pos.y = Mathf.Floor(Mathf.Lerp(m_hiddenBotY, m_shownY, t) / hs) * hs;
        m_nextMenu.localPosition = pos;
      }

      if (t >= 1f) {
        if (m_currentMenu) {
          m_currentMenu.gameObject.SetActive(false);
        }
        m_currentMenu = m_nextMenu;
        m_currentMenu.localPosition = new Vector2(0, 0);
        m_transitioning = false;
      }
    }
    if (m_enteringCommand) {
      if (m_nextCharTime - Time.time <= 0f) {
        m_nextCharTime = Time.time + m_timeBetweenCommandChars;
        m_commandText.maxVisibleCharacters = m_currentCommandIdx;
        ++m_currentCommandIdx;
        if (m_currentCommandIdx > m_commandText.text.Length) {
          m_enteringCommand = false;
        }
      }
    }
  }

  public void Quit() {
    Application.Quit();
  }

  public void TransitionToMain() => TransitionTo(MenuType.Main);
  public void TransitionToPlay() => TransitionTo(MenuType.Play);
  public void TransitionToSettings() => TransitionTo(MenuType.Settings);

  public void TransitionTo(MenuType menu) {
    if (m_transitioning) {
      return;
    }
    m_nextMenu = GetMenu(menu);
    m_nextMenu.localPosition = new Vector2(0, m_hiddenBotY);
    m_nextMenu.gameObject.SetActive(true);
    m_endTime = Time.time + m_transitionTime;
    m_transitioning = true;
    ResetCommand();
  }

  public Transform GetMenu(MenuType menu) {
    switch (menu) {
      case MenuType.Main:
        return m_mainMenu;
      case MenuType.Play:
        return m_playMenu;
      case MenuType.Settings:
        return m_settingsMenu;
    }
    return null;
  }

  public void EnterCommand(string command) {
    if (m_transitioning) {
      return;
    }
    ResetCommand();
    m_commandText.text = command;
    m_enteringCommand = true;
  }

  public void ResetCommand() {
    m_commandText.maxVisibleCharacters = 0;
    m_enteringCommand = false;
    m_currentCommandIdx = 0;
  }
}
