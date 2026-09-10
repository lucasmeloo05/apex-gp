using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCarController : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Engine")]
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float reverseSpeed = 4f;

    [Header("Braking")]
    [SerializeField] private float braking = 12f;
    [SerializeField] private float naturalDeceleration = 2f;

    [Header("Steering")]
    [SerializeField] private float steering = 180f;
    [SerializeField] private float lowSpeedSteering = 0.5f;

    [Header("Tire Grip")]
    [SerializeField] private float lateralGrip = 5f;

    private float throttleInput;
    private float steeringInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        throttleInput = 0f;
        steeringInput = 0f;

        // =========================
        // ACELERAÇÃO / RÉ
        // =========================

        if (Keyboard.current.wKey.isPressed)
        {
            throttleInput = 1f;
        }
        else if (Keyboard.current.sKey.isPressed)
        {
            throttleInput = -1f;
        }

        // =========================
        // DIREÇÃO
        // =========================

        if (Keyboard.current.aKey.isPressed)
        {
            steeringInput = 1f;
        }
        else if (Keyboard.current.dKey.isPressed)
        {
            steeringInput = -1f;
        }
    }

    private void FixedUpdate()
    {
        HandleEngine();
        ApplyNaturalDeceleration();
        ApplyLateralGrip();
        LimitSpeed();
        ApplySteering();
    }

    private void HandleEngine()
    {
        float forwardSpeed = Vector2.Dot(
            rb.linearVelocity,
            transform.up
        );

        // =========================
        // ACELERANDO
        // =========================

        if (throttleInput > 0f)
        {
            if (forwardSpeed < maxSpeed)
            {
                rb.AddForce(transform.up * acceleration);
            }

            return;
        }

        // =========================
        // FREIO / RÉ
        // =========================

        if (throttleInput < 0f)
        {
            // Ainda está andando para frente -> freia
            if (forwardSpeed > 0.1f)
            {
                rb.AddForce(-transform.up * braking);
            }
            // Parado ou andando para trás -> ré
            else
            {
                if (forwardSpeed > -reverseSpeed)
                {
                    rb.AddForce(-transform.up * acceleration);
                }
            }
        }
    }

    private void ApplyNaturalDeceleration()
    {
        if (Mathf.Abs(throttleInput) < 0.01f)
        {
            rb.linearVelocity = Vector2.MoveTowards(
                rb.linearVelocity,
                Vector2.zero,
                naturalDeceleration * Time.fixedDeltaTime
            );
        }
    }

    private void LimitSpeed()
    {
        Vector2 forwardVelocity =
            transform.up *
            Vector2.Dot(rb.linearVelocity, transform.up);

        Vector2 lateralVelocity =
            transform.right *
            Vector2.Dot(rb.linearVelocity, transform.right);

        float signedForwardSpeed =
            Vector2.Dot(rb.linearVelocity, transform.up);

        // =========================
        // LIMITE PARA FRENTE
        // =========================

        if (signedForwardSpeed > maxSpeed)
        {
            forwardVelocity = transform.up * maxSpeed;
        }

        // =========================
        // LIMITE PARA RÉ
        // =========================

        if (signedForwardSpeed < -reverseSpeed)
        {
            forwardVelocity = -transform.up * reverseSpeed;
        }

        rb.linearVelocity = forwardVelocity + lateralVelocity;
    }

    private void ApplySteering()
    {
        float speed = rb.linearVelocity.magnitude;

        if (speed <= 0.1f)
        {
            return;
        }

        float speedFactor = Mathf.Clamp01(speed / maxSpeed);

        float steeringFactor = Mathf.Lerp(
            lowSpeedSteering,
            1f,
            speedFactor
        );

        float steeringDirection = steeringInput;

        // =========================
        // CORREÇÃO DA DIREÇÃO NA RÉ
        // =========================

        float forwardSpeed = Vector2.Dot(
            rb.linearVelocity,
            transform.up
        );

        if (forwardSpeed < -0.1f)
        {
            steeringDirection *= -1f;
        }

        float steeringAmount =
            steeringDirection *
            steering *
            steeringFactor *
            Time.fixedDeltaTime;

        rb.MoveRotation(rb.rotation + steeringAmount);
    }

    private void ApplyLateralGrip()
    {
        Vector2 forwardVelocity =
            transform.up *
            Vector2.Dot(rb.linearVelocity, transform.up);

        Vector2 lateralVelocity =
            transform.right *
            Vector2.Dot(rb.linearVelocity, transform.right);

        rb.linearVelocity =
            forwardVelocity +
            lateralVelocity /
            (1f + lateralGrip * Time.fixedDeltaTime);
    }
}