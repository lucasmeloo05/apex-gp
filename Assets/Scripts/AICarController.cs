using UnityEngine;

public class AICarController : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Waypoints")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float waypointReachDistance = 3f;

    [Header("Engine")]
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float maxSpeed = 30f;

    [Header("Braking")]
    [SerializeField] private float braking = 30f;
    [SerializeField] private float naturalDeceleration = 4f;

    [Header("Steering")]
    [SerializeField] private float steering = 180f;
    [SerializeField] private float lowSpeedSteering = 0.5f;

    [Header("Tire Grip")]
    [SerializeField] private float lateralGrip = 5f;

    [Header("AI")]
    [SerializeField] private float maximumCornerAngle = 70f;
    [SerializeField] private float minimumCornerSpeed = 10f;
    [SerializeField] private float steeringDeadZone = 5f;

    private int currentWaypoint = 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        UpdateWaypoint();

        HandleAIEngine();
        ApplyLateralGrip();
        LimitSpeed();
        ApplySteering();
    }

    // =========================================================
    // WAYPOINT
    // =========================================================

    private void UpdateWaypoint()
    {
        Transform target = waypoints[currentWaypoint];

        float distance = Vector2.Distance(
            transform.position,
            target.position
        );

        if (distance <= waypointReachDistance)
        {
            currentWaypoint++;

            if (currentWaypoint >= waypoints.Length)
            {
                currentWaypoint = 0;
            }
        }
    }

    // =========================================================
    // MOTOR DA IA
    // =========================================================

    private void HandleAIEngine()
    {
        Vector2 forward = -transform.up;

        float forwardSpeed = Vector2.Dot(
            rb.linearVelocity,
            forward
        );

        float angle = GetTargetAngle();

        float targetSpeed = CalculateTargetSpeed(angle);

        // =====================================================
        // ACELERA
        // =====================================================

        if (forwardSpeed < targetSpeed)
        {
            rb.AddForce(
                forward * acceleration
            );
        }

        // =====================================================
        // FREIA
        // =====================================================

        else if (forwardSpeed > targetSpeed + 1f)
        {
            rb.AddForce(
                -forward * braking
            );
        }

        // =====================================================
        // DESACELERA NATURALMENTE
        // =====================================================

        if (Mathf.Abs(targetSpeed - forwardSpeed) < 1f)
        {
            rb.linearVelocity = Vector2.MoveTowards(
                rb.linearVelocity,
                rb.linearVelocity.normalized * targetSpeed,
                naturalDeceleration * Time.fixedDeltaTime
            );
        }
    }

    // =========================================================
    // CALCULA ÂNGULO ATÉ O WAYPOINT
    // =========================================================

    private float GetTargetAngle()
    {
        Vector2 forward = -transform.up;

        Vector2 directionToTarget =
            (Vector2)waypoints[currentWaypoint].position -
            rb.position;

        if (directionToTarget.sqrMagnitude < 0.01f)
            return 0f;

        directionToTarget.Normalize();

        return Vector2.SignedAngle(
            forward,
            directionToTarget
        );
    }

    // =========================================================
    // VELOCIDADE PARA CURVAS
    // =========================================================

    private float CalculateTargetSpeed(float angle)
    {
        float normalizedAngle =
            Mathf.Clamp01(
                Mathf.Abs(angle) / maximumCornerAngle
            );

        float targetSpeed = Mathf.Lerp(
            maxSpeed,
            minimumCornerSpeed,
            normalizedAngle
        );

        return targetSpeed;
    }

    // =========================================================
    // DIREÇÃO
    // =========================================================

    private void ApplySteering()
    {
        float speed = rb.linearVelocity.magnitude;

        if (speed <= 0.1f)
            return;

        float angle = GetTargetAngle();

        // =========================
        // ZONA MORTA
        // =========================

        if (Mathf.Abs(angle) < steeringDeadZone)
        {
            angle = 0f;
        }

        // =========================
        // DIREÇÃO PROPORCIONAL
        // =========================

        float steeringDirection =
            Mathf.Clamp(angle / 45f, -1f, 1f);

        // =========================
        // REDUZ DIREÇÃO EM ALTA VELOCIDADE
        // =========================

        float speedFactor =
            Mathf.Clamp01(speed / maxSpeed);

        float steeringFactor =
            Mathf.Lerp(
                lowSpeedSteering,
                1f,
                speedFactor
            );

        float steeringAmount =
            steeringDirection *
            steering *
            steeringFactor *
            Time.fixedDeltaTime;

        rb.MoveRotation(
            rb.rotation + steeringAmount
        );
    }

    // =========================================================
    // ADERÊNCIA LATERAL
    // =========================================================

    private void ApplyLateralGrip()
    {
        Vector2 forward = -transform.up;

        Vector2 forwardVelocity =
            forward *
            Vector2.Dot(
                rb.linearVelocity,
                forward
            );

        Vector2 lateralVelocity =
            transform.right *
            Vector2.Dot(
                rb.linearVelocity,
                transform.right
            );

        rb.linearVelocity =
            forwardVelocity +
            lateralVelocity /
            (1f + lateralGrip * Time.fixedDeltaTime);
    }

    // =========================================================
    // LIMITE DE VELOCIDADE
    // =========================================================

    private void LimitSpeed()
    {
        Vector2 forward = -transform.up;

        Vector2 forwardVelocity =
            forward *
            Vector2.Dot(
                rb.linearVelocity,
                forward
            );

        Vector2 lateralVelocity =
            transform.right *
            Vector2.Dot(
                rb.linearVelocity,
                transform.right
            );

        float forwardSpeed =
            Vector2.Dot(
                rb.linearVelocity,
                forward
            );

        if (forwardSpeed > maxSpeed)
        {
            forwardVelocity =
                forward * maxSpeed;
        }

        rb.linearVelocity =
            forwardVelocity +
            lateralVelocity;
    }
}