using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCarController : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Física (compartilhada com a IA)")]
    [Tooltip("Arraste o asset CarPhysicsProfile. Se vazio, usa valores padrão.")]
    [SerializeField] private CarPhysicsProfile physics;

    [Header("Engine")]
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float maxSpeed = 30f;
    [SerializeField] private float reverseSpeed = 8f;

    [Header("Braking")]
    [SerializeField] private float braking = 16f;
    [SerializeField] private float naturalDeceleration = 4f;
    [Tooltip("Tempo para o freio chegar na força total ao apertar S. Maior = mais progressivo.")]
    [SerializeField] private float brakeRampTime = 0.35f;
    [Tooltip("Abaixo dessa velocidade o freio perde força aos poucos, evitando a parada seca.")]
    [SerializeField] private float brakeSoftStopSpeed = 4f;

    private float brakePedal;

    [Header("Steering")]
    [Tooltip("Velocidade com que o volante vira (teclado). Maior = mais responsivo. 100 = instantâneo.")]
    [SerializeField] private float steeringResponse = 10f;

    [Header("Launch Slip (derrapada na largada / arrancada)")]
    [SerializeField] private bool enableLaunchSlip = true;
    [Tooltip("Só derrapa se a velocidade estiver abaixo disso ao acelerar.")]
    [SerializeField] private float slipTriggerSpeed = 2f;
    [Tooltip("O carro precisa estar abaixo disso (sem acelerar) para 'armar' a derrapada.")]
    [SerializeField] private float slipRearmSpeed = 1f;
    [Tooltip("Força da rabeada (graus/s de pico).")]
    [SerializeField] private float slipYawKick = 140f;
    [Tooltip("Rapidez do balanço da traseira (rad/s).")]
    [SerializeField] private float slipFrequency = 9f;
    [Tooltip("Rapidez com que a rabeada se acalma.")]
    [SerializeField] private float slipDamping = 2.5f;
    [SerializeField] private float slipDuration = 1.4f;
    [Tooltip("Fração do grip que sobra no auge da derrapada (menor = desliza mais).")]
    [Range(0.05f, 1f)]
    [SerializeField] private float slipGripFraction = 0.3f;
    [Tooltip("Quanto da aceleração se perde patinando (0 a 1).")]
    [Range(0f, 1f)]
    [SerializeField] private float wheelspinTractionLoss = 0.3f;
    [Tooltip("Tempo mínimo entre duas derrapadas.")]
    [SerializeField] private float slipCooldown = 2f;
    [Tooltip("Quanto esterçar contra a rabeada ajuda a controlar (0 a 1).")]
    [Range(0f, 1f)]
    [SerializeField] private float counterSteerAssist = 0.5f;

    [Header("Engine Sound")]
    [SerializeField] private AudioSource engineAudio;

    [Header("Acceleration Sound")]
    [SerializeField] private AudioSource accelerationAudio;

    [Header("Brake Sound")]
    [SerializeField] private AudioSource brakeAudio;

    [Header("Skid Sound (opcional)")]
    [SerializeField] private AudioSource skidAudio;

    private float throttleInput;
    private float steeringInput;
    private float smoothedSteeringInput;

    // ---------------- ESTADO DA DERRAPADA ----------------

    private bool slipArmed = true;
    private bool slipActive = false;
    private float slipTime;
    private float slipDir = 1f;
    private float slipPower = 1f;
    private float slipCooldownTimer;
    private float slipEnvelope;

    // ---------------- CONTROLE DA LARGADA ----------------

    private bool raceStarted = false;

    public void SetRaceStarted(bool started)
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        raceStarted = started;

        if (!raceStarted)
        {
            throttleInput = 0f;
            steeringInput = 0f;
            smoothedSteeringInput = 0f;

            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            StopAccelerationSound();
            EndSlip();
        }

        slipArmed = true;
        slipCooldownTimer = 0f;
    }

    public bool IsRaceStarted()
    {
        return raceStarted;
    }

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (physics == null)
            physics = CarPhysicsProfile.CreateDefault();
    }

    // =========================================================
    // INPUT
    // =========================================================

    private void Update()
    {
        throttleInput = 0f;
        steeringInput = 0f;

        if (!raceStarted)
        {
            StopAccelerationSound();
            return;
        }

        var kb = Keyboard.current;
        if (kb == null)
            return;

        if (kb.wKey.isPressed)
        {
            throttleInput = 1f;

            HandleAccelerationSound();

            if (kb.wKey.wasPressedThisFrame &&
                rb.linearVelocity.magnitude < 0.5f)
            {
                PlayEngineStartSound();
            }
        }
        else if (kb.sKey.isPressed)
        {
            throttleInput = -1f;

            Vector2 forward = -transform.up;
            float forwardSpeed = Vector2.Dot(rb.linearVelocity, forward);

            if (forwardSpeed > 0.1f && kb.sKey.wasPressedThisFrame)
                PlayBrakeSound();

            StopAccelerationSound();
        }
        else
        {
            StopAccelerationSound();
        }

        if (kb.aKey.isPressed)
            steeringInput = 1f;
        else if (kb.dKey.isPressed)
            steeringInput = -1f;
    }

    // =========================================================
    // FÍSICA
    // =========================================================

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        if (!raceStarted)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            return;
        }

        smoothedSteeringInput = Mathf.MoveTowards(
            smoothedSteeringInput,
            steeringInput,
            steeringResponse * dt
        );

        UpdateLaunchSlip(dt);

        HandleEngine();
        ApplyNaturalDeceleration();
        physics.ApplyDrag(rb, dt);
        ApplyLateralGrip(dt);
        LimitSpeed();
        ApplySteering(dt);
    }

    // Grip atual: cai ao frear forte, ao acelerar a fundo e durante a derrapada da largada
    private float GetGripMultiplier()
    {
        Vector2 forward = -transform.up;
        float forwardSpeed = Vector2.Dot(rb.linearVelocity, forward);

        float brake = (throttleInput < 0f && forwardSpeed > 0.1f) ? 1f : 0f;
        float throttle = throttleInput > 0f ? 1f : 0f;

        float m = physics.GripMultiplier(throttle, brake);

        return m * Mathf.Lerp(1f, slipGripFraction, slipEnvelope);
    }

    // =========================================================
    // DERRAPADA NA LARGADA / ARRANCADA
    // =========================================================

    private void UpdateLaunchSlip(float dt)
    {
        if (slipCooldownTimer > 0f)
            slipCooldownTimer -= dt;

        float speed = rb.linearVelocity.magnitude;

        if (throttleInput <= 0f && speed < slipRearmSpeed)
            slipArmed = true;

        if (enableLaunchSlip &&
            !slipActive &&
            slipArmed &&
            slipCooldownTimer <= 0f &&
            throttleInput > 0f &&
            speed < slipTriggerSpeed)
        {
            StartSlip();
        }

        if (slipActive)
        {
            slipTime += dt;

            if (slipTime >= slipDuration)
                EndSlip();
            else
                slipEnvelope = Mathf.Exp(-slipTime * slipDamping);
        }
    }

    private void StartSlip()
    {
        slipActive = true;
        slipArmed = false;
        slipTime = 0f;
        slipEnvelope = 1f;
        slipDir = Random.value < 0.5f ? -1f : 1f;
        slipPower = Random.Range(0.8f, 1.2f);
        slipCooldownTimer = slipCooldown;

        if (skidAudio != null)
        {
            skidAudio.loop = false;
            skidAudio.Stop();
            skidAudio.Play();
        }
    }

    private void EndSlip()
    {
        slipActive = false;
        slipEnvelope = 0f;
        slipTime = 0f;
    }

    private float GetSlipYawRate()
    {
        if (!slipActive)
            return 0f;

        float rate = slipDir * slipPower * slipYawKick * slipEnvelope *
                     Mathf.Sin(slipTime * slipFrequency);

        if (smoothedSteeringInput * rate < 0f)
            rate *= 1f - counterSteerAssist * Mathf.Abs(smoothedSteeringInput);

        return rate;
    }

    // =========================================================
    // SONS
    // =========================================================

    private void PlayEngineStartSound()
    {
        if (engineAudio == null)
            return;

        engineAudio.loop = false;
        engineAudio.Play();
    }

    private void HandleAccelerationSound()
    {
        if (accelerationAudio == null)
            return;

        accelerationAudio.loop = true;

        if (!accelerationAudio.isPlaying)
            accelerationAudio.Play();
    }

    private void StopAccelerationSound()
    {
        if (accelerationAudio == null)
            return;

        if (accelerationAudio.isPlaying)
            accelerationAudio.Stop();
    }

    private void PlayBrakeSound()
    {
        if (brakeAudio == null)
            return;

        brakeAudio.loop = false;
        brakeAudio.Stop();
        brakeAudio.Play();
    }

    // =========================================================
    // MOTOR / FREIO
    // =========================================================

    private void HandleEngine()
    {
        Vector2 forward = -transform.up;
        float forwardSpeed = Vector2.Dot(rb.linearVelocity, forward);

        // Pedal de freio progressivo (sobe ao apertar S, solta ao largar)
        bool isBraking = throttleInput < 0f && forwardSpeed > 0.1f;
        brakePedal = Mathf.MoveTowards(
            brakePedal,
            isBraking ? 1f : 0f,
            Time.fixedDeltaTime / Mathf.Max(0.01f, brakeRampTime)
        );

        if (throttleInput > 0f)
        {
            if (forwardSpeed < maxSpeed)
            {
                float traction = 1f - wheelspinTractionLoss * slipEnvelope;
                rb.AddForce(forward * acceleration * traction);
            }

            return;
        }

        if (throttleInput < 0f)
        {
            if (forwardSpeed > 0.1f)
            {
                // Perde força nos últimos metros, sem "trancar" o carro de uma vez
                float softStop = Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(forwardSpeed / brakeSoftStopSpeed));
                rb.AddForce(-forward * braking * brakePedal * softStop);
            }
            else
            {
                if (forwardSpeed > -reverseSpeed)
                    rb.AddForce(-forward * acceleration);
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
        Vector2 forward = -transform.up;
        Vector2 left = new Vector2(-forward.y, forward.x);

        float f = Vector2.Dot(rb.linearVelocity, forward);
        float l = Vector2.Dot(rb.linearVelocity, left);

        f = Mathf.Clamp(f, -reverseSpeed, maxSpeed);

        rb.linearVelocity = forward * f + left * l;
    }

    // =========================================================
    // DIREÇÃO
    // =========================================================

    private void ApplySteering(float dt)
    {
        float speed = rb.linearVelocity.magnitude;
        float yaw = 0f;

        if (speed > 0.1f)
        {
            float dir = smoothedSteeringInput;

            Vector2 forward = -transform.up;
            float forwardSpeed = Vector2.Dot(rb.linearVelocity, forward);

            // Correção da direção na ré
            if (forwardSpeed < -0.1f)
                dir *= -1f;

            yaw = physics.GetYawRate(dir, speed, maxSpeed, GetGripMultiplier());
        }

        // A rabeada é somada no mesmo MoveRotation (funciona mesmo parado)
        yaw += GetSlipYawRate();

        rb.MoveRotation(rb.rotation + yaw * dt);
    }

    // =========================================================
    // ADERÊNCIA LATERAL
    // =========================================================

    private void ApplyLateralGrip(float dt)
    {
        physics.ApplyGrip(rb, -transform.up, dt, GetGripMultiplier());
    }
}