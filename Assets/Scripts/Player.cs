using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using KinematicCharacterController;

public class Player : MonoBehaviour, ICharacterController {
  public enum MovementState {
    Walking,
    Sliding,
    Crouching,
    Dashing,
  }
  public MovementState MoveState {
    get { return m_moveState; }
    set { m_moveState = value; }
  }
  MovementState m_moveState;
  MovementState m_lastMoveState;

  public KinematicCharacterMotor Motor;

  [SerializeField] float m_speed = 16f;
  [SerializeField] float m_dashSpeed = 30f;
  [SerializeField] float m_dashTime = 3f;
  float m_lastDash = 0f;
  Vector3 m_dashDir;
  [SerializeField] float m_airAccel = 0.1f;
  [SerializeField] float m_jumpForce = 15f;
  [SerializeField] float m_jumpCooldown = 0.2f;
  float m_lastJump = 0f;
  [SerializeField] float m_drag = 0.6f;

  [SerializeField] int m_maxWallJumps = 3;
  int m_wallJumps;
  [SerializeField] float m_wallJumpVerticalForce = 23f;
  [SerializeField] float m_wallJumpHorizontalForce = 8f;

  [SerializeField] float m_cameraSensitivity = 0.1f;
  [SerializeField] Transform m_camera;
  Vector3 m_cameraRot;
  [SerializeField] float m_cameraTiltAngle = 5f;
  [SerializeField] float m_cameraTiltSpeed = 8f;

  [SerializeField] TMP_Text m_speedText;
  [SerializeField] TMP_Text m_verticalSpeedText;

  [SerializeField] int m_maxHealth = 100;
  int m_health;
  [SerializeField] Slider m_healthBar;
  bool m_isDead;

  public const int PLAYER_LAYER = 1 << 3;

  Vector2 m_inputDir;

  PlayerInput m_playerInput;
  InputAction m_moveAction;
  InputAction m_lookAction;
  InputAction m_jumpAction;
  InputAction m_dashAction;
  InputAction m_shootAction;

  bool m_jumping;

  Vector3 m_slopeNormal;
  RaycastHit m_slopeHit;

  Vector3 forward;
  Vector3 right;

  [SerializeField] Transform m_gunHolder;
  List<Gun> m_guns = new();

  Vector3 m_velocity;

  void Awake() {
    m_health = m_maxHealth;
    m_healthBar.value = 1f;

    m_wallJumps = m_maxWallJumps;

    Motor.CharacterController = this;

    m_playerInput = GetComponent<PlayerInput>();

    m_moveAction = m_playerInput.actions.FindAction("Move");
    m_lookAction = m_playerInput.actions.FindAction("Look");
    m_jumpAction = m_playerInput.actions.FindAction("Jump");
    m_jumpAction.performed += OnJump;
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
    Vector3 camDelta = m_lookAction.ReadValue<Vector2>() * m_cameraSensitivity;
    m_cameraRot.x += camDelta.x;
    m_cameraRot.y += camDelta.y;
    m_cameraRot.y = Mathf.Clamp(m_cameraRot.y, -90f, 90f);
    forward = new Vector3(Mathf.Sin(m_cameraRot.x * Mathf.Deg2Rad), 0f, Mathf.Cos(m_cameraRot.x * Mathf.Deg2Rad));
    right = new Vector3(Mathf.Cos(m_cameraRot.x * Mathf.Deg2Rad), 0f, -Mathf.Sin(m_cameraRot.x * Mathf.Deg2Rad));

    m_camera.position = transform.position + Vector3.up * (Motor.Capsule.height * 0.4f);

    if (m_inputDir.x > 0) {
      m_cameraRot.z = Mathf.Lerp(m_cameraRot.z, -m_cameraTiltAngle, 1f - Mathf.Exp(-m_cameraTiltSpeed * Time.deltaTime));
    } else if (m_inputDir.x < 0) {
      m_cameraRot.z = Mathf.Lerp(m_cameraRot.z, m_cameraTiltAngle, 1f - Mathf.Exp(-m_cameraTiltSpeed * Time.deltaTime));
    } else {
      m_cameraRot.z = Mathf.Lerp(m_cameraRot.z, 0f, 1f - Mathf.Exp(-m_cameraTiltSpeed * Time.deltaTime));
    }
    m_camera.localEulerAngles = new Vector3(-m_cameraRot.y, m_cameraRot.x, m_cameraRot.z);
  }

  void OnJump(InputAction.CallbackContext ctx) {
    m_jumping = true;
    m_lastJump = Time.time;
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

  public void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
  }

  public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
    if (m_isDead) {
      return;
    }

    if (!Motor.GroundingStatus.IsStableOnGround) {
      currentVelocity += Physics.gravity * deltaTime;
    } else {
      m_wallJumps = m_maxWallJumps;
    }

    Vector3 moveDir = forward * m_inputDir.y + right * m_inputDir.x;
    switch (m_moveState) {
      case MovementState.Walking:
      case MovementState.Crouching:
        if (Motor.GroundingStatus.IsStableOnGround) {
          currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, Motor.GroundingStatus.GroundNormal).normalized * currentVelocity.magnitude;
          Vector3 targetVel = Motor.GetDirectionTangentToSurface(moveDir, Motor.GroundingStatus.GroundNormal).normalized * m_speed + Vector3.up * currentVelocity.y;

          currentVelocity = Vector3.Lerp(currentVelocity, targetVel, 1f - Mathf.Exp(-15 * deltaTime));
        } else {
          Vector3 targetVel = moveDir * m_speed;
          Vector3 airVel = Vector3.zero;
          if ((targetVel.x > 0f && currentVelocity.x < targetVel.x) || (targetVel.x < 0f && currentVelocity.x > targetVel.x)) {
            airVel.x = targetVel.x;
          }
          if ((targetVel.z > 0f && currentVelocity.z < targetVel.z) || (targetVel.z < 0f && currentVelocity.z > targetVel.z)) {
            airVel.z = targetVel.z;
          }

          currentVelocity += airVel * m_airAccel;
        }
        break;
      // case MovementState.Sliding:
      //   Vector3 r = right * m_inputDir.x * deltaTime * m_slideStrafeForce;
      //   currentVelocity += r;
      //   if (Motor.GroundingStatus.IsStableOnGround) {
      //     currentVelocity += Physics.gravity * deltaTime;
      //     if (currentVelocity.magnitude < m_slideForce) {
      //       currentVelocity = Motor.GetDirectionTangentToSurface(m_slideDir + r, Motor.GroundingStatus.GroundNormal).normalized * m_slideForce;
      //     }
      //   }
      //   break;
      case MovementState.Dashing:
        if (Time.time - m_lastDash >= m_dashTime) {
          m_moveState = MovementState.Walking;
          currentVelocity = (m_dashDir * m_speed);
        } else {
          currentVelocity = (m_dashDir * m_dashSpeed);
        }
        break;
    }

    m_speedText.text = new Vector2(Motor.Velocity.x, Motor.Velocity.z).magnitude.ToString("0.00");
    m_verticalSpeedText.text = Mathf.Abs(Motor.Velocity.y).ToString("0.00");

    if (m_jumping) {
      Collider[] cols = new Collider[4];
      bool wallJump = Physics.OverlapSphereNonAlloc(Motor.TransientPosition, Motor.Capsule.radius + 0.1f, cols, ~PLAYER_LAYER) > 0 && m_wallJumps > 0;
      if (Motor.GroundingStatus.IsStableOnGround) {
        currentVelocity.y = m_jumpForce;
        Motor.ForceUnground();
        m_jumping = false;
      } else if (Time.time - m_lastJump >= m_jumpCooldown) {
        m_jumping = false;
      } else if (wallJump) {
        --m_wallJumps;
        Motor.ForceUnground();
        Vector3 dir = Vector3.zero;
        foreach (var col in cols) {
          if (col == null) {
            break;
          }
          dir += (Motor.TransientPosition - col.ClosestPointOnBounds(Motor.TransientPosition)).normalized;
        }
        dir = dir.normalized * m_wallJumpHorizontalForce;
        dir.y = m_wallJumpVerticalForce;
        currentVelocity = dir;
        m_jumping = false;
      }
    }

    currentVelocity *= 1f / (1f + (m_drag * deltaTime));
  }

  public void TakeDamage(int damage) {
    m_health -= damage;
    m_healthBar.value = (float)m_health / (float)m_maxHealth;
    if (m_health <= 0) {
      m_isDead = true;
    }
  }

  public void BeforeCharacterUpdate(float deltaTime) {
  }

  public void PostGroundingUpdate(float deltaTime) {
  }

  public void AfterCharacterUpdate(float deltaTime) {
    m_lastMoveState = m_moveState;
  }

  public bool IsColliderValidForCollisions(Collider coll) {
    return true;
  }

  public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {
  }

  public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {
  }

  public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) {
  }

  public void OnDiscreteCollisionDetected(Collider hitCollider) {
  }
}
