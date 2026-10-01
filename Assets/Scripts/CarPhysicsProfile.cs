using UnityEngine;

/// <summary>
/// Física compartilhada entre o jogador e a IA.
/// Crie o asset em: botão direito no Project > Create > Racing > Car Physics Profile
/// e arraste no campo "Physics" do PlayerCarController e de todos os AICarController.
/// </summary>
[CreateAssetMenu(fileName = "CarPhysicsProfile", menuName = "Racing/Car Physics Profile")]
public class CarPhysicsProfile : ScriptableObject
{
    [Header("Aderência (o coração da mudança)")]
    [Tooltip("Aceleração lateral máxima que o pneu aguenta. Menor = curvas exigem mais freio. Velocidade máxima numa curva = sqrt(grip * raio).")]
    [SerializeField] private float gripAcceleration = 22f;

    [Tooltip("Quanto do grip se perde ao frear forte (círculo de atrito).")]
    [Range(0f, 1f)]
    [SerializeField] private float brakeGripLoss = 0.25f;

    [Tooltip("Quanto do grip se perde ao acelerar a fundo.")]
    [Range(0f, 1f)]
    [SerializeField] private float throttleGripLoss = 0.1f;

    [Header("Direção")]
    [Tooltip("Giro máximo do carro em graus/s (antes dos limites de grip).")]
    [SerializeField] private float steeringRate = 180f;

    [Tooltip("Abaixo dessa velocidade o carro quase não gira (não roda parado).")]
    [SerializeField] private float minSteerSpeed = 3f;

    [Tooltip("Fração do esterço que sobra na velocidade máxima. Menor = volante mais 'preguiçoso' em alta.")]
    [Range(0.2f, 1f)]
    [SerializeField] private float highSpeedSteering = 0.6f;

    [Tooltip("1 = o carro nunca passa do limite do pneu (só abre a trajetória). Maior = permite passar do limite e deslizar.")]
    [SerializeField] private float understeerTolerance = 1.1f;

    [Header("Resistências")]
    [Tooltip("Resistência do ar (quadrática). Maior = tirar o pé desacelera mais.")]
    [SerializeField] private float dragCoefficient = 0.012f;

    [Tooltip("Perda de velocidade ao deslizar de lado (por unidade de velocidade lateral).")]
    [SerializeField] private float slideScrub = 1f;

    public float GripAcceleration => gripAcceleration;

    public static CarPhysicsProfile CreateDefault()
    {
        var p = CreateInstance<CarPhysicsProfile>();
        p.hideFlags = HideFlags.HideAndDontSave;
        return p;
    }

    /// <summary>Multiplicador de grip (0..1) conforme o uso de acelerador e freio.</summary>
    public float GripMultiplier(float throttle01, float brake01)
    {
        float m = 1f - brakeGripLoss * Mathf.Clamp01(brake01) - throttleGripLoss * Mathf.Clamp01(throttle01);
        return Mathf.Clamp(m, 0.2f, 1f);
    }

    /// <summary>Resistência do ar.</summary>
    public void ApplyDrag(Rigidbody2D rb, float dt)
    {
        Vector2 v = rb.linearVelocity;
        float s = v.magnitude;

        if (s < 0.01f)
            return;

        float newSpeed = Mathf.Max(0f, s - dragCoefficient * s * s * dt);
        rb.linearVelocity = v / s * newSpeed;
    }

    /// <summary>
    /// Grip lateral com LIMITE de força: só remove velocidade lateral até o que o pneu aguenta por frame.
    /// O que passar disso vira deslize, e deslizar custa velocidade.
    /// </summary>
    public void ApplyGrip(Rigidbody2D rb, Vector2 forward, float dt, float gripMultiplier)
    {
        Vector2 left = new Vector2(-forward.y, forward.x);
        Vector2 vel = rb.linearVelocity;

        float f = Vector2.Dot(vel, forward);
        float l = Vector2.Dot(vel, left);

        float absL = Mathf.Abs(l);
        float removal = Mathf.Min(absL, gripAcceleration * gripMultiplier * dt);

        l -= Mathf.Sign(l) * removal;

        // pneu raspando: perde velocidade para frente
        if (absL > 0.5f)
            f = Mathf.MoveTowards(f, 0f, slideScrub * Mathf.Min(absL, 12f) * dt);

        rb.linearVelocity = forward * f + left * l;
    }

    /// <summary>
    /// Giro (graus/s) para uma entrada de volante de -1 a 1.
    /// Limitado pelo grip: quanto mais rápido, menos o carro consegue virar (understeer).
    /// </summary>
    public float GetYawRate(float steerInput, float speed, float maxSpeed, float gripMultiplier)
    {
        if (speed < 0.01f)
            return 0f;

        float authority = Mathf.Clamp01(speed / minSteerSpeed);
        float speedFactor = Mathf.Clamp01(speed / Mathf.Max(0.01f, maxSpeed));

        float rate = steerInput * steeringRate * authority * Mathf.Lerp(1f, highSpeedSteering, speedFactor);

        // giro máximo que o grip permite: omega = a / v
        float maxRate = understeerTolerance * gripAcceleration * gripMultiplier / speed * Mathf.Rad2Deg;

        return Mathf.Clamp(rate, -maxRate, maxRate);
    }
}