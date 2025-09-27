using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using TMPro;

public class PlayerPhysicsBased : MonoBehaviour {
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

  [SerializeField] float m_speed = 16.5f;
  [SerializeField] float m_dashSpeed = 10f;
  [SerializeField] float m_dashTime = 10f;
  float m_lastDash = 0f;
  Vector3 m_dashDir;
  [SerializeField] float m_slideForce = 24f;
  Vector3 m_slideDir;
  [SerializeField] float m_airAccel = 0.1f;
  [SerializeField] float m_jumpForce = 4f;
  [SerializeField] float m_jumpCooldown = 0.2f;
  float m_lastJump = 0f;
  [SerializeField] float m_groundDrag = 10f;

  [SerializeField] float m_cameraSensitivity = 0.1f;
  [SerializeField] Transform m_camera;
  Vector2 m_cameraRot;

  [SerializeField] TMP_Text m_speedText;
  [SerializeField] TMP_Text m_verticalSpeedText;

  [SerializeField] float m_friction = 0.5f;

  public const int PLAYER_LAYER = 1 << 3;

  Rigidbody m_rb;
  CapsuleCollider m_cc;

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

  [SerializeField] Transform m_gunHolder;
  List<Gun> m_guns = new();

  void Start() {
    m_rb = GetComponent<Rigidbody>();
    m_cc = GetComponent<CapsuleCollider>();
    m_playerInput = GetComponent<PlayerInput>();

    m_moveAction = m_playerInput.actions.FindAction("Move");
    m_lookAction = m_playerInput.actions.FindAction("Look");
    m_jumpAction = m_playerInput.actions.FindAction("Jump");
    m_jumpAction.performed += OnJump;
    m_crouchAction = m_playerInput.actions.FindAction("Crouch");
    m_crouchAction.performed += OnCrouch;
    m_dashAction = m_playerInput.actions.FindAction("Dash");
    m_dashAction.performed += OnDash;
    m_shootAction = m_playerInput.actions.FindAction("Shoot");
    m_shootAction.performed += OnShoot;

    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;

    foreach (Transform g in m_gunHolder) {
      if (g.TryGetComponent<Gun>(out var gun)) {
        m_guns.Add(gun);
      }
    }
  }

  void Update() {
    m_inputDir = m_moveAction.ReadValue<Vector2>();
    m_cameraRot += m_lookAction.ReadValue<Vector2>() * m_cameraSensitivity;
    m_cameraRot.y = Mathf.Clamp(m_cameraRot.y, -90f, 90f);
    forward = new Vector3(Mathf.Sin(m_cameraRot.x * Mathf.Deg2Rad), 0f, Mathf.Cos(m_cameraRot.x * Mathf.Deg2Rad));
    right = new Vector3(Mathf.Cos(m_cameraRot.x * Mathf.Deg2Rad), 0f, -Mathf.Sin(m_cameraRot.x * Mathf.Deg2Rad));

    m_camera.position = transform.position + Vector3.up * (m_cc.height * 0.4f);

    m_camera.localEulerAngles = new Vector3(-m_cameraRot.y, m_cameraRot.x);
  }

  void FixedUpdate() {
    m_grounded = Physics.SphereCast(transform.position, m_cc.radius * 0.99f, Vector3.down, out m_slopeHit, m_cc.height * 0.5f + 0.1f, ~PLAYER_LAYER);
    m_slopeNormal = m_slopeHit.normal;
    m_onSlope = Vector3.Angle(m_slopeNormal, Vector3.up) > 0f;
    m_rb.useGravity = !m_onSlope;
    // if ((m_grounded && m_rb.linearVelocity.y <= 0.1f) || m_onSlope) {
    //   if (m_moveState == MovementState.Sliding || m_jumping) {
    //     m_rb.linearDamping = 0f;
    //   } else {
    //     m_rb.linearDamping = m_groundDrag;
    //   }
    // } else {
    //   m_rb.linearDamping = 0f;
    // }

    Vector3 moveDir = forward * m_inputDir.y + right * m_inputDir.x;
    switch (m_moveState) {
      case MovementState.Walking:
        Move(moveDir, m_speed);
        break;
      case MovementState.Crouching:
        Move(moveDir, m_speed * 0.5f);
        break;
      case MovementState.Slaming:
        m_rb.linearVelocity = new Vector3(0f, -100f, 0f);
        if (m_grounded) {
          m_moveState = MovementState.Walking;
        }
        break;
      case MovementState.Sliding:
        if (m_rb.linearVelocity.magnitude <= 0.1f) {
          m_moveState = MovementState.Walking;
        }
        if (m_rb.linearVelocity.magnitude < m_slideForce) {
          m_rb.linearVelocity = Vector3.ProjectOnPlane(m_slideDir * m_slideForce, m_slopeNormal);
        }
        break;
      case MovementState.Dashing:
        if (Time.time - m_lastDash >= m_dashTime) {
          m_moveState = MovementState.Walking;
          m_rb.linearVelocity = m_dashDir * m_speed;
        } else {
          m_rb.linearVelocity = m_dashDir * m_dashSpeed;
        }
        break;
    }

    m_speedText.text = new Vector2(m_rb.linearVelocity.x, m_rb.linearVelocity.z).magnitude.ToString("0.00");
    m_verticalSpeedText.text = Mathf.Abs(m_rb.linearVelocity.y).ToString("0.00");

    if (m_jumping) {
      if (m_grounded) {
        m_rb.linearDamping = 0f;
        m_rb.linearVelocity = new Vector3(m_rb.linearVelocity.x, m_jumpForce, m_rb.linearVelocity.z);
        m_jumping = false;
      } else if (Time.time - m_lastJump >= m_jumpCooldown) {
        m_jumping = false;
      }
    }
  }

  void Move(Vector3 moveDir, float targetSpeed) {
    // Vector3 moveDir = forward * m_inputDir.y + right * m_inputDir.x;
    // if (m_onSlope && !m_jumping) {
    //   moveDir = Vector3.ProjectOnPlane(moveDir, m_slopeNormal).normalized;
    //   m_rb.AddForce(moveDir * targetSpeed, ForceMode.VelocityChange);
    //   m_rb.AddForce(-Physics.gravity * 0.8f);
    //   if (m_rb.linearVelocity.magnitude > targetSpeed) {
    //     m_rb.linearVelocity = m_rb.linearVelocity.normalized * targetSpeed;
    //   }
    // } else {
    float yVel = m_rb.linearVelocity.y;
    if (m_onSlope && moveDir.x == 0f && moveDir.z == 0f) {
      m_rb.useGravity = false;
      yVel = 0f;
    }

    if (m_grounded) {
      // Vector3 targetVel = new Vector3(moveDir.x * targetSpeed, yVel, moveDir.z * targetSpeed);
      // m_rb.linearVelocity = Vector3.Lerp(m_rb.linearVelocity, targetVel, 0.25f * m_friction);
      Vector3 flatVel = new Vector3(m_rb.linearVelocity.x, 0f, m_rb.linearVelocity.z);
      Vector3 targetVel = flatVel.normalized * targetSpeed + Vector3.up * m_rb.linearVelocity.y;
      m_rb.AddForce(moveDir * targetSpeed * 10f);
      if (flatVel.sqrMagnitude > targetSpeed * targetSpeed) {
        m_rb.linearVelocity = targetVel;
      }
    } else {
      Vector3 moveVector = moveDir * targetSpeed;
      Vector3 airDir = Vector3.zero;
      if ((moveVector.x > 0f && m_rb.linearVelocity.x < moveVector.x) || (moveVector.x < 0f && m_rb.linearVelocity.x > moveVector.x)) {
        airDir.x = moveVector.x;
      }
      if ((moveVector.z > 0f && m_rb.linearVelocity.z < moveVector.z) || (moveVector.z < 0f && m_rb.linearVelocity.z > moveVector.z)) {
        airDir.z = moveVector.z;
      }
      m_rb.AddForce(airDir.normalized * m_airAccel, ForceMode.VelocityChange);
      // Vector3 targetVel = flatVel.normalized * targetSpeed + Vector3.up * m_rb.linearVelocity.y;
      // if (m_inputDir.sqrMagnitude > 0f) {
      //   if (flatVel.magnitude - targetSpeed >= 8f) {
      //     m_rb.linearVelocity = Vector3.MoveTowards(m_rb.linearVelocity, targetVel, Time.fixedDeltaTime);
      //   } else {
      //     m_rb.linearVelocity = targetVel;
      //   }
      // }
    }
  }

  void OnJump(InputAction.CallbackContext ctx) {
    m_jumping = true;
    m_lastJump = Time.time;
    if (m_grounded) {
      if (m_moveState == MovementState.Dashing) {
        m_moveState = MovementState.Walking;
        m_rb.linearVelocity = forward * m_dashSpeed;
      }
    }
  }

  void OnCrouch(InputAction.CallbackContext ctx) {
    if (ctx.action.IsPressed()) {
      if (Physics.Raycast(transform.position, Vector3.down, out var hit, m_cc.height * 2f, ~PLAYER_LAYER)) {
        m_moveState = MovementState.Sliding;
        if (m_inputDir.sqrMagnitude > 0f) {
          m_slideDir = (forward * m_inputDir.y + right * m_inputDir.x);
        } else {
          m_slideDir = forward;
        }
        m_cc.height = 1f;
        m_rb.MovePosition(hit.point + Vector3.up * (m_cc.height * 0.5f));
        if (m_rb.linearVelocity.magnitude < m_slideForce) {
          m_rb.linearVelocity = m_slideDir * m_slideForce;
        }
      } else {
        m_moveState = MovementState.Slaming;
      }
    } else if (m_moveState != MovementState.Slaming) {
      m_moveState = MovementState.Walking;
      m_cc.height = 2f;
    }
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

  void OnShoot(InputAction.CallbackContext ctx) {
    if (ctx.action.IsPressed()) {
      m_guns[0].Shoot(m_camera.position, m_camera.forward);
    }
  }
}
