using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class Player : MonoBehaviour {
  public enum MovementState {
    Walking,
    Sliding,
    Crouching,
    Slaming,
    Dashing,
  };
  public MovementState MoveState {
    get { return m_moveState; }
    set { m_moveState = value; }
  }
  MovementState m_moveState;

  public const int PLAYER_LAYER = 1 << 3;

  [SerializeField] float m_height = 2f;
  [SerializeField] float m_radius = 0.3f;

  [SerializeField] float m_speed = 16.5f;
  [SerializeField] float m_dashSpeed = 10f;
  [SerializeField] float m_dashTime = 10f;
  float m_lastDash = 0f;
  Vector3 m_dashDir;
  [SerializeField] float m_slideForce = 24f;
  Vector3 m_slideDir;
  [SerializeField] float m_airMultiplier = 0.1f;
  [SerializeField] float m_jumpForce = 4f;
  [SerializeField] float m_jumpCooldown = 0.2f;
  float m_lastJump = 0f;
  [SerializeField] float m_groundDrag = 10f;

  [SerializeField] float m_cameraSensitivity = 0.1f;
  [SerializeField] Transform m_camera;
  Vector2 m_cameraRot;

  [SerializeField] TMP_Text m_speedText;
  [SerializeField] TMP_Text m_verticalSpeedText;

  Vector2 m_inputDir;

  PlayerInput m_playerInput;
  InputAction m_moveAction;
  InputAction m_lookAction;
  InputAction m_jumpAction;
  InputAction m_crouchAction;
  InputAction m_dashAction;
  InputAction m_shootAction;

  bool m_grounded;
  bool m_onSlope;
  bool m_jumping;

  Vector3 m_slopeNormal;
  RaycastHit m_slopeHit;

  Vector3 forward;
  Vector3 right;

  Vector3 m_velocity;

  void Awake() {
    m_playerInput = GetComponent<PlayerInput>();

    m_moveAction = m_playerInput.actions.FindAction("Move");
    m_lookAction = m_playerInput.actions.FindAction("Look");
    m_jumpAction = m_playerInput.actions.FindAction("Jump");
    m_jumpAction.performed += OnJump;
    m_crouchAction = m_playerInput.actions.FindAction("Crouch");
    m_crouchAction.performed += OnCrouch;
    m_dashAction = m_playerInput.actions.FindAction("Dash");
    m_dashAction.performed += OnDash;

    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
  }

  void Update() {
    m_inputDir = m_moveAction.ReadValue<Vector2>();
    m_cameraRot += m_lookAction.ReadValue<Vector2>() * m_cameraSensitivity;
    m_cameraRot.y = Mathf.Clamp(m_cameraRot.y, -90f, 90f);
    forward = new Vector3(Mathf.Sin(m_cameraRot.x * Mathf.Deg2Rad), 0f, Mathf.Cos(m_cameraRot.x * Mathf.Deg2Rad));
    right = new Vector3(Mathf.Cos(m_cameraRot.x * Mathf.Deg2Rad), 0f, -Mathf.Sin(m_cameraRot.x * Mathf.Deg2Rad));

    m_camera.position = transform.position + Vector3.up * (2.0f * 0.4f);

    m_camera.localEulerAngles = new Vector3(-m_cameraRot.y, m_cameraRot.x);
  }

  void FixedUpdate() {
    Vector3 moveDir = m_inputDir.y * forward + m_inputDir.x * right;

    m_velocity = CollideAndSlide(moveDir * m_speed, transform.position);
    m_velocity += CollideAndSlide(Vector3.down * 9.81f, transform.position + m_velocity, 0, true);
    transform.position += m_velocity * Time.deltaTime;
  }

  Vector3 CollideAndSlide(Vector3 vel, Vector3 pos, int depth = 0, bool gravityPass = false) {
    if (depth >= 5) {
      return Vector3.zero;
    }

    float dist = vel.magnitude * Time.deltaTime;
    Vector3 off = Vector3.up * (m_height * 0.5f - m_radius);
    if (Physics.CapsuleCast(pos - off, pos + off, m_radius, vel.normalized, out var hit, dist, ~PLAYER_LAYER)) {
      Vector3 snapToSurface = (vel.normalized * hit.distance) + (hit.normal * 0.015f);
      Vector3 leftover = vel * Time.deltaTime - snapToSurface * Time.deltaTime;
      if (snapToSurface.magnitude <= 0.015f) {
        snapToSurface = Vector3.zero;
      }
      if (gravityPass) {
        return snapToSurface;
      }
      leftover = Vector3.ProjectOnPlane(leftover, hit.normal);
      return snapToSurface + CollideAndSlide(leftover, pos + snapToSurface, depth + 1, gravityPass);
    }

    return vel;
  }

  void OnJump(InputAction.CallbackContext ctx) {
    m_jumping = true;
    m_lastJump = Time.time;
    if (m_grounded) {
      if (m_moveState == MovementState.Dashing) {
        m_moveState = MovementState.Walking;
      }
    }
  }

  void OnCrouch(InputAction.CallbackContext ctx) {
    // if (ctx.action.IsPressed()) {
    //   if (Physics.Raycast(transform.position, Vector3.down, out var hit, 4f, ~PLAYER_LAYER)) {
    //     m_moveState = MovementState.Sliding;
    //     if (m_inputDir.sqrMagnitude > 0f) {
    //       m_slideDir = (forward * m_inputDir.y + right * m_inputDir.x);
    //     } else {
    //       m_slideDir = forward;
    //     }
    //     m_cc.height = 1f;
    //     m_rb.MovePosition(hit.point + Vector3.up * (m_cc.height * 0.5f));
    //     if (m_rb.linearVelocity.magnitude < m_slideForce) {
    //       m_rb.linearVelocity = m_slideDir * m_slideForce;
    //     }
    //   } else {
    //     m_moveState = MovementState.Slaming;
    //   }
    // } else if (m_moveState != MovementState.Slaming) {
    //   m_moveState = MovementState.Walking;
    //   m_cc.height = 2f;
    // }
  }

  void OnDash(InputAction.CallbackContext ctx) {
    if (ctx.action.IsPressed()) {
      m_moveState = MovementState.Dashing;
      m_lastDash = Time.time;
      if (m_inputDir.sqrMagnitude > 0f) {
        m_dashDir = forward * m_inputDir.y + right * m_inputDir.x;
      } else {
        m_dashDir = forward;
      }
    }
  }
}
