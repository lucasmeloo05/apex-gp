using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCarController : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Engine")]
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float maxSpeed = 30f;
    [SerializeField] private float reverseSpeed = 8f;

    [Header("Braking")]
    [SerializeField] private float braking = 30f;
    [SerializeField] private float naturalDeceleration = 4f;

    [Header("Steering")]
    [SerializeField] private float steering = 180f;
    [SerializeField] private float lowSpeedSteering = 0.5f;

    [Header("Tire Grip")]
    [SerializeField] private float lateralGrip = 5f;

    [Header("Engine Sound")]
    [SerializeField] private AudioSource engineAudio;

    [Header("Acceleration Sound")]
    [SerializeField] private AudioSource accelerationAudio;

    [Header("Brake Sound")]
    [SerializeField] private AudioSource brakeAudio;

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
        // ACELERAÇÃO
        // =========================

        if (Keyboard.current.wKey.isPressed)
        {
            throttleInput = 1f;

            HandleAccelerationSound();

            // Som de partida
            // Toca somente quando W é pressionado
            if (Keyboard.current.wKey.wasPressedThisFrame)
            {
                PlayEngineStartSound();
            }
        }

        // =========================
        // FREIO / RÉ
        // =========================

        else if (Keyboard.current.sKey.isPressed)
        {
            throttleInput = -1f;

            Vector2 forward = -transform.up;

            float forwardSpeed = Vector2.Dot(
                rb.linearVelocity,
                forward
            );

            // Só toca o som de freio se o carro
            // estiver andando para frente
            if (forwardSpeed > 0.1f &&
                Keyboard.current.sKey.wasPressedThisFrame)
            {
                PlayBrakeSound();
            }

            // Para o som de aceleração
            StopAccelerationSound();
        }

        // =========================
        // NENHUM COMANDO
        // =========================

        else
        {
            StopAccelerationSound();
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

    // =========================================================
    // SOM DE PARTIDA
    // =========================================================

    private void PlayEngineStartSound()
    {
        if (engineAudio == null)
            return;

        engineAudio.loop = false;
        engineAudio.Play();
    }

    // =========================================================
    // SOM DE ACELERAÇÃO
    // =========================================================

    private void HandleAccelerationSound()
    {
        if (accelerationAudio == null)
            return;

        accelerationAudio.loop = true;

        if (!accelerationAudio.isPlaying)
        {
            accelerationAudio.Play();
        }
    }

    private void StopAccelerationSound()
    {
        if (accelerationAudio == null)
            return;

        if (accelerationAudio.isPlaying)
        {
            accelerationAudio.Stop();
        }
    }

    // =========================================================
    // SOM DE FREIO
    // =========================================================

    private void PlayBrakeSound()
    {
        if (brakeAudio == null)
            return;

        brakeAudio.loop = false;
        brakeAudio.Stop();
        brakeAudio.Play();
    }

    // =========================================================
    // MOTOR / FÍSICA
    // =========================================================

    private void HandleEngine()
    {
        // A frente REAL da sprite é para baixo
        Vector2 forward = -transform.up;

        float forwardSpeed = Vector2.Dot(
            rb.linearVelocity,
            forward
        );

        // =========================
        // ACELERANDO
        // =========================

        if (throttleInput > 0f)
        {
            if (forwardSpeed < maxSpeed)
            {
                rb.AddForce(forward * acceleration);
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
                rb.AddForce(-forward * braking);
            }
            // Parado ou andando para trás -> ré
            else
            {
                if (forwardSpeed > -reverseSpeed)
                {
                    rb.AddForce(-forward * acceleration);
                }
            }
        }
    }

    // =========================================================
    // DESACELERAÇÃO NATURAL
    // =========================================================

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

    // =========================================================
    // LIMITE DE VELOCIDADE
    // =========================================================

    private void LimitSpeed()
    {
        Vector2 forward = -transform.up;

        Vector2 forwardVelocity =
            forward *
            Vector2.Dot(rb.linearVelocity, forward);

        Vector2 lateralVelocity =
            transform.right *
            Vector2.Dot(rb.linearVelocity, transform.right);

        float signedForwardSpeed =
            Vector2.Dot(rb.linearVelocity, forward);

        // =========================
        // LIMITE PARA FRENTE
        // =========================

        if (signedForwardSpeed > maxSpeed)
        {
            forwardVelocity = forward * maxSpeed;
        }

        // =========================
        // LIMITE PARA RÉ
        // =========================

        if (signedForwardSpeed < -reverseSpeed)
        {
            forwardVelocity = -forward * reverseSpeed;
        }

        rb.linearVelocity =
            forwardVelocity +
            lateralVelocity;
    }

    // =========================================================
    // DIREÇÃO
    // =========================================================

    private void ApplySteering()
    {
        float speed = rb.linearVelocity.magnitude;

        if (speed <= 0.1f)
        {
            return;
        }

        float speedFactor =
            Mathf.Clamp01(speed / maxSpeed);

        float steeringFactor =
            Mathf.Lerp(
                lowSpeedSteering,
                1f,
                speedFactor
            );

        float steeringDirection =
            steeringInput;

        // =========================
        // CORREÇÃO DA DIREÇÃO NA RÉ
        // =========================

        Vector2 forward = -transform.up;

        float forwardSpeed =
            Vector2.Dot(
                rb.linearVelocity,
                forward
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
}