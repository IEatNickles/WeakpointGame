using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
  [SerializeField] UnityEvent m_onClick;
  [SerializeField] UnityEvent m_onHover;
  [SerializeField] UnityEvent m_onUnhover;

  public void OnPointerClick(PointerEventData eventData) {
    m_onClick?.Invoke();
  }

  public void OnPointerEnter(PointerEventData eventData) {
    m_onHover?.Invoke();
  }

  public void OnPointerExit(PointerEventData eventData) {
    m_onUnhover?.Invoke();
  }
}
