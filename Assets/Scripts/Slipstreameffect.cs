using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistema de vácuo (slipstream).
/// Quem anda colado atrás de outro carro ganha velocidade máxima e aceleração extras.
/// É adicionado automaticamente pelos controladores (jogador e IA).
/// Para ajustar os valores no Inspector, adicione este componente manualmente nas prefabs.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SlipstreamEffect : MonoBehaviour
{
    [Header("Detecção (cone atrás do carro da frente)")]
    [Tooltip("Alcance máximo do vácuo.")]
    [SerializeField] private float maxDistance = 22f;
    [Tooltip("Distância mínima (carro quase colado, ainda vale).")]
    [SerializeField] private float minDistance = 2.5f;
    [Tooltip("Meia-largura do cone perto do carro da frente.")]
    [SerializeField] private float coneWidthNear = 1.4f;
    [Tooltip("Meia-largura do cone no alcance máximo.")]
    [SerializeField] private float coneWidthFar = 2.6f;
    [Tooltip("O carro da frente precisa estar acima dessa velocidade.")]
    [SerializeField] private float minLeaderSpeed = 12f;
    [Tooltip("Você precisa estar acima dessa velocidade.")]
    [SerializeField] private float minMySpeed = 8f;
    [Tooltip("Quão alinhados os dois carros precisam estar (1 = mesma direção exata).")]
    [Range(0.5f, 1f)]
    [SerializeField] private float minAlignment = 0.85f;

    [Header("Carga")]
    [Tooltip("Segundos dentro do vácuo para chegar na carga máxima.")]
    [SerializeField] private float chargeTime = 1.5f;
    [Tooltip("Segundos para perder a carga ao sair do vácuo.")]
    [SerializeField] private float decayTime = 0.8f;

    [Header("Efeitos (com carga máxima)")]
    [Tooltip("Velocidade máxima extra (u/s).")]
    [SerializeField] private float topSpeedBonus = 3.5f;
    [Tooltip("Aceleração extra (0.25 = +25%).")]
    [Range(0f, 1f)]
    [SerializeField] private float accelerationBonus = 0.25f;

    // ---------------- registro de todos os carros ----------------
    private static readonly List<SlipstreamEffect> all = new List<SlipstreamEffect>();

    private Rigidbody2D rb;
    private float charge;

    // ---------------- acesso externo ----------------

    /// <summary>0 = sem vácuo, 1 = vácuo completo.</summary>
    public float Charge => charge;

    public bool IsActive => charge > 0.05f;

    /// <summary>Velocidade máxima extra atual (somar à velocidade máxima).</summary>
    public float SpeedBonus => topSpeedBonus * charge;

    /// <summary>Multiplicador de aceleração atual (1 = normal).</summary>
    public float AccelMultiplier => 1f + accelerationBonus * charge;

    private Vector2 Forward => -transform.up;

    // =========================================================
    // CICLO DE VIDA
    // =========================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        if (!all.Contains(this))
            all.Add(this);
    }

    private void OnDisable()
    {
        all.Remove(this);
        charge = 0f;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        float target = ComputeDraftStrength();

        // Sobe devagar dentro do vácuo, cai um pouco mais rápido ao sair
        float rate = target > charge ? 1f / chargeTime : 1f / decayTime;

        charge = Mathf.MoveTowards(charge, target, rate * dt);
    }

    // =========================================================
    // DETECÇÃO
    // =========================================================

    // Retorna a força do vácuo (0 a 1) considerando o melhor carro à frente
    private float ComputeDraftStrength()
    {
        Vector2 fwd = Forward;
        Vector2 left = new Vector2(-fwd.y, fwd.x);

        float mySpeed = Vector2.Dot(rb.linearVelocity, fwd);

        if (mySpeed < minMySpeed)
            return 0f;

        float best = 0f;

        for (int i = 0; i < all.Count; i++)
        {
            SlipstreamEffect other = all[i];

            if (other == this || other == null || other.rb == null)
                continue;

            Vector2 rel = other.rb.position - rb.position;

            // distância à frente
            float ahead = Vector2.Dot(rel, fwd);

            if (ahead < minDistance || ahead > maxDistance)
                continue;

            // dentro do cone?
            float side = Mathf.Abs(Vector2.Dot(rel, left));
            float cone = Mathf.Lerp(coneWidthNear, coneWidthFar, ahead / maxDistance);

            if (side > cone)
                continue;

            // andando no mesmo sentido?
            Vector2 otherFwd = other.Forward;

            if (Vector2.Dot(otherFwd, fwd) < minAlignment)
                continue;

            // o líder precisa estar rápido o suficiente para "abrir" o ar
            float otherSpeed = Vector2.Dot(other.rb.linearVelocity, otherFwd);

            if (otherSpeed < minLeaderSpeed)
                continue;

            float distFactor = 1f - ahead / maxDistance;   // mais perto = mais forte
            float sideFactor = 1f - side / cone;           // mais centralizado = mais forte

            float strength = Mathf.Sqrt(distFactor) * Mathf.Lerp(0.6f, 1f, sideFactor);

            if (strength > best)
                best = strength;
        }

        return Mathf.Clamp01(best);
    }

    // =========================================================
    // DEBUG
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position;
        Vector3 f = -transform.up;
        Vector3 l = new Vector3(-f.y, f.x, 0f);

        Vector3 nearPoint = origin + f * minDistance;
        Vector3 farPoint = origin + f * maxDistance;

        Gizmos.color = IsActive ? Color.green : new Color(1f, 1f, 1f, 0.35f);

        Gizmos.DrawLine(nearPoint + l * coneWidthNear, farPoint + l * coneWidthFar);
        Gizmos.DrawLine(nearPoint - l * coneWidthNear, farPoint - l * coneWidthFar);
        Gizmos.DrawLine(farPoint + l * coneWidthFar, farPoint - l * coneWidthFar);
        Gizmos.DrawLine(nearPoint + l * coneWidthNear, nearPoint - l * coneWidthNear);
    }
}