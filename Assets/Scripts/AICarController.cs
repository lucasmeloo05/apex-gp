using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class AICarController : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Física (compartilhada com o jogador)")]
    [Tooltip("Arraste o MESMO asset CarPhysicsProfile do jogador. Se vazio, usa valores padrão.")]
    [SerializeField] private CarPhysicsProfile physics;

    [Header("Waypoints")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float waypointReachDistance = 3f;
    [Tooltip("Distância do centro da pista até a borda (unidades do mundo).")]
    [SerializeField] private float trackHalfWidth = 6f;
    [Tooltip("Folga que o carro mantém das bordas.")]
    [SerializeField] private float edgeMargin = 1.5f;

    [Header("Engine")]
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float maxSpeed = 30f;
    [SerializeField] private float maxReverseSpeed = 8f;

    [Header("Braking")]
    [SerializeField] private float braking = 16f;
    [SerializeField] private float naturalDeceleration = 4f;

    [Header("Steering")]
    [Tooltip("Ângulo (graus) a partir do qual o volante vai ao máximo.")]
    [SerializeField] private float steerFullAngle = 35f;
    [Tooltip("Velocidade com que o volante vai de um lado ao outro.")]
    [SerializeField] private float steeringResponse = 6f;

    [Header("Cornering")]
    [Tooltip("Quanto do grip o piloto usa nas curvas (min = piloto cauteloso, max = no limite). Sorteado por carro segundo habilidade e agressividade.")]
    [SerializeField] private Vector2 cornerGripUseRange = new Vector2(0.72f, 0.92f);
    [SerializeField] private float minimumCornerSpeed = 10f;
    [Range(0f, 1f)]
    [SerializeField] private float racingLineStrength = 0.5f;
    [SerializeField] private float minLookAhead = 4f;
    [SerializeField] private float maxLookAhead = 14f;

    [Header("Sensores / Ultrapassagem")]
    [Tooltip("Coloque os carros numa layer 'Car' e selecione aqui. Se vazio, detecta qualquer Rigidbody2D.")]
    [SerializeField] private LayerMask carLayers;
    [SerializeField] private float carWidth = 2f;
    [SerializeField] private float carLength = 3.5f;
    [SerializeField] private float sensorRadius = 16f;
    [SerializeField] private float laneChangeSpeed = 4f;

    [Header("Personalidade (sorteada por carro)")]
    [SerializeField] private Vector2 skillRange = new Vector2(0.92f, 1f);
    [SerializeField] private Vector2 aggressionRange = new Vector2(0.2f, 1f);
    [SerializeField] private float maxStartReaction = 0.5f;
    [SerializeField] private bool enableMistakes = true;

    [Header("Largada")]
    [Tooltip("Gira o carro para a direção da pista ao iniciar a corrida.")]
    [SerializeField] private bool alignToTrackOnStart = false;

    // Use de fora para rubber banding
    public float SpeedMultiplier { get; set; } = 1f;

    // ---------------- ESTADO ----------------
    private int currentWaypoint;
    public int CurrentWaypoint => currentWaypoint;

    private bool canDrive;
    private float startDelay;

    // cache da pista
    private int n;
    private Vector2[] wpPos;
    private Vector2[] wpLeft;
    private float[] wpSegLen;
    private float[] wpCurvature;
    private float[] wpTurnSign;

    // personalidade
    private float skill = 1f;
    private float skillNorm;
    private float aggression = 0.5f;
    private float cornerGripUse = 0.8f;
    private float laneFraction;
    private float laneShuffleTimer;
    private float mistakeTimer;
    private float nextMistakeIn;
    private float mistakeOffset;

    // faixa / linha de corrida
    private float laneOffset;
    private float cornerStrength;
    private float cornerSign;
    private float overtakeTimer;
    private float overtakeSide;
    private float overtakeLane;

    // sensores
    private readonly Collider2D[] scanBuffer = new Collider2D[32];
    private ContactFilter2D scanFilter;
    private bool blockerFound;
    private float blockerFwd;
    private float blockerSide;
    private float blockerSpeed;
    private bool leftBlocked;
    private bool rightBlocked;

    // controle
    private float smoothedSteer;
    private float stuckTimer;
    private float reverseTimer;
    private float lastThrottle;
    private float lastBrake;
    private Vector2 debugLookPoint;

    // =========================================================
    // INICIALIZAÇÃO
    // =========================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (physics == null)
            physics = CarPhysicsProfile.CreateDefault();

        skill = Random.Range(skillRange.x, skillRange.y);
        skillNorm = Mathf.InverseLerp(skillRange.x, skillRange.y, skill);
        aggression = Random.Range(aggressionRange.x, aggressionRange.y);

        // quem é mais habilidoso e agressivo usa mais do limite do pneu nas curvas
        float boldness = Mathf.Clamp01(0.5f * aggression + 0.5f * skillNorm + Random.Range(-0.15f, 0.15f));
        cornerGripUse = Mathf.Lerp(cornerGripUseRange.x, cornerGripUseRange.y, boldness);

        laneFraction = Random.Range(-0.7f, 0.7f);
        laneShuffleTimer = Random.Range(3f, 10f);
        nextMistakeIn = Random.Range(8f, 25f);

        scanFilter = new ContactFilter2D();
        scanFilter.useTriggers = false;
        if (carLayers.value != 0)
            scanFilter.SetLayerMask(carLayers);

        CacheWaypoints();
    }

    private void Start()
    {
        FindStartingWaypoint();
    }

    public void SetWaypoints(Transform[] newWaypoints)
    {
        waypoints = newWaypoints;
        CacheWaypoints();
        FindStartingWaypoint();
    }

    public void SetRaceStarted(bool started)
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        canDrive = started;

        if (started)
        {
            FindStartingWaypoint();

            if (n > 0)
            {
                if (alignToTrackOnStart)
                    AlignToTrack();

                laneOffset = GetActualOffset();
            }

            startDelay = Random.Range(0f, maxStartReaction);
            smoothedSteer = 0f;
            stuckTimer = 0f;
            reverseTimer = 0f;
            overtakeTimer = 0f;
        }
        else
        {
            Stop();
        }
    }

    private void CacheWaypoints()
    {
        n = waypoints == null ? 0 : waypoints.Length;

        if (n < 2)
        {
            n = 0;
            return;
        }

        wpPos = new Vector2[n];
        wpLeft = new Vector2[n];
        wpSegLen = new float[n];
        wpCurvature = new float[n];
        wpTurnSign = new float[n];

        for (int i = 0; i < n; i++)
            wpPos[i] = waypoints[i].position;

        float[] rawCurv = new float[n];
        float[] rawSign = new float[n];

        for (int i = 0; i < n; i++)
        {
            Vector2 prev = wpPos[(i - 1 + n) % n];
            Vector2 cur = wpPos[i];
            Vector2 next = wpPos[(i + 1) % n];

            Vector2 tangent = next - prev;
            tangent = tangent.sqrMagnitude > 0.0001f ? tangent.normalized : Vector2.up;
            wpLeft[i] = new Vector2(-tangent.y, tangent.x);

            wpSegLen[i] = Vector2.Distance(cur, next);

            Vector2 v1 = cur - prev;
            Vector2 v2 = next - cur;
            float l1 = v1.magnitude;
            float l2 = v2.magnitude;

            if (l1 > 0.01f && l2 > 0.01f)
            {
                float angle = Vector2.SignedAngle(v1, v2); // + = esquerda
                rawCurv[i] = Mathf.Abs(angle) * Mathf.Deg2Rad / (0.5f * (l1 + l2));
                rawSign[i] = Mathf.Sign(angle);
            }
        }

        // suaviza: começa a frear um pouco antes e sai um pouco depois
        for (int i = 0; i < n; i++)
        {
            int p = (i - 1 + n) % n;
            int q = (i + 1) % n;

            float best = rawCurv[i];
            float sign = rawSign[i];

            if (rawCurv[p] * 0.6f > best) { best = rawCurv[p] * 0.6f; sign = rawSign[p]; }
            if (rawCurv[q] * 0.6f > best) { best = rawCurv[q] * 0.6f; sign = rawSign[q]; }

            wpCurvature[i] = best;
            wpTurnSign[i] = sign;
        }
    }

    private void FindStartingWaypoint()
    {
        if (n == 0)
            return;

        float best = Mathf.Infinity;
        int bestNext = 0;
        Vector2 pos = rb.position;

        for (int i = 0; i < n; i++)
        {
            int next = (i + 1) % n;
            Vector2 a = wpPos[i];
            Vector2 ab = wpPos[next] - a;
            float sqr = ab.sqrMagnitude;

            float t = sqr > 0.0001f
                ? Mathf.Clamp01(Vector2.Dot(pos - a, ab) / sqr)
                : 0f;

            float d = Vector2.Distance(pos, a + ab * t);

            if (d < best)
            {
                best = d;
                bestNext = next;
            }
        }

        currentWaypoint = bestNext;
    }

    private void AlignToTrack()
    {
        int prev = (currentWaypoint - 1 + n) % n;
        Vector2 dir = wpPos[currentWaypoint] - wpPos[prev];

        if (dir.sqrMagnitude < 0.0001f)
            return;

        dir.Normalize();

        // frente do carro = -transform.up
        rb.rotation = Mathf.Atan2(dir.x, -dir.y) * Mathf.Rad2Deg;
    }

    // =========================================================
    // LOOP PRINCIPAL
    // =========================================================

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        if (!canDrive)
        {
            Stop();
            return;
        }

        if (startDelay > 0f)
        {
            startDelay -= dt;
            Stop();
            return;
        }

        if (n == 0)
            return;

        Vector2 forward = GetForward();
        float speed = rb.linearVelocity.magnitude;
        float forwardSpeed = Vector2.Dot(rb.linearVelocity, forward);
        float personalMax = maxSpeed * skill * SpeedMultiplier;

        UpdateWaypoint();
        Scan();

        float actualOffset = GetActualOffset();

        float target = PlanSpeed(personalMax);

        UpdatePersonality(dt);
        UpdateLane(dt, actualOffset, target);

        target = ApplyTraffic(target, speed);

        if (mistakeTimer > 0f)
            target *= 0.88f;

        // Ponto de mira (pure pursuit)
        float look = Mathf.Lerp(minLookAhead, maxLookAhead, Mathf.Clamp01(speed / maxSpeed));
        Vector2 lookPoint = GetLookAheadPoint(look);
        debugLookPoint = lookPoint;

        float angle = Vector2.SignedAngle(forward, lookPoint - rb.position);
        float headingError = Mathf.Abs(angle);

        // Muito desalinhado da pista? Reduz a velocidade para se recuperar
        if (headingError > 25f)
        {
            float slow = Mathf.Min(target, minimumCornerSpeed);
            target = Mathf.Lerp(target, slow, Mathf.InverseLerp(25f, 80f, headingError));
        }

        UpdateStuck(dt, speed);

        Drive(target, forwardSpeed, forward);
        physics.ApplyDrag(rb, dt);
        ApplyGripAndLimit(personalMax, dt);
        Steer(angle, speed, dt);
    }

    private void Stop()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    private Vector2 GetForward()
    {
        return -transform.up;
    }

    // =========================================================
    // WAYPOINTS
    // =========================================================

    private void UpdateWaypoint()
    {
        Vector2 toTarget = wpPos[currentWaypoint] - rb.position;
        float dist = toTarget.magnitude;

        Vector2 left = wpLeft[currentWaypoint];
        Vector2 tangent = new Vector2(left.y, -left.x);

        bool passed = Vector2.Dot(toTarget, tangent) < 0f && dist < waypointReachDistance * 4f;

        if (dist <= waypointReachDistance || passed)
            currentWaypoint = (currentWaypoint + 1) % n;
    }

    private float GetActualOffset()
    {
        int prev = (currentWaypoint - 1 + n) % n;
        Vector2 a = wpPos[prev];
        Vector2 dir = wpPos[currentWaypoint] - a;
        float len = dir.magnitude;

        if (len < 0.01f)
            return 0f;

        dir /= len;
        Vector2 left = new Vector2(-dir.y, dir.x);

        return Vector2.Dot(rb.position - a, left);
    }

    private Vector2 GetLookAheadPoint(float lookAhead)
    {
        Vector2 from = rb.position;
        int idx = currentWaypoint;
        float remaining = lookAhead;

        for (int i = 0; i < 8; i++)
        {
            Vector2 p = wpPos[idx] + wpLeft[idx] * laneOffset;
            Vector2 seg = p - from;
            float d = seg.magnitude;

            if (d >= remaining)
                return from + seg / d * remaining;

            remaining -= d;
            from = p;
            idx = (idx + 1) % n;
        }

        return from;
    }

    // =========================================================
    // PLANEJAMENTO DE VELOCIDADE (frenagem antecipada, baseada no grip real)
    // =========================================================

    private float PlanSpeed(float personalMax)
    {
        // pilotos agressivos freiam mais tarde
        float brakeDecel = Mathf.Max(1f, braking / rb.mass) * Mathf.Lerp(0.6f, 0.9f, aggression);
        float planDistance = personalMax * personalMax / (2f * brakeDecel) + 10f;

        // aceleração lateral que ESTE piloto se permite usar (o carro só aguenta physics.GripAcceleration)
        float gripBudget = physics.GripAcceleration * cornerGripUse * skill;

        float allowedNow = personalMax;
        float dist = Vector2.Distance(rb.position, wpPos[currentWaypoint]);

        float bestScore = 0f;
        int bestIdx = -1;
        float bestDist = 0f;

        int idx = currentWaypoint;

        for (int i = 0; i < n && i < 40; i++)
        {
            float k = Mathf.Max(wpCurvature[idx], 0.0001f);

            float cornerSpeed = Mathf.Min(
                personalMax,
                Mathf.Max(minimumCornerSpeed, Mathf.Sqrt(gripBudget / k))
            );

            // velocidade máxima AGORA para chegar na curva a "cornerSpeed"
            float allowed = Mathf.Sqrt(cornerSpeed * cornerSpeed + 2f * brakeDecel * dist);

            if (allowed < allowedNow)
                allowedNow = allowed;

            float score = wpCurvature[idx] / (1f + dist * 0.05f);

            if (score > bestScore)
            {
                bestScore = score;
                bestIdx = idx;
                bestDist = dist;
            }

            dist += wpSegLen[idx];

            if (dist > planDistance)
                break;

            idx = (idx + 1) % n;
        }

        if (bestIdx >= 0)
        {
            cornerStrength = Mathf.Clamp01(wpCurvature[bestIdx] / 0.08f) *
                             Mathf.Clamp01(1f - bestDist / planDistance);
            cornerSign = wpTurnSign[bestIdx];
        }
        else
        {
            cornerStrength = 0f;
            cornerSign = 0f;
        }

        return allowedNow;
    }

    // =========================================================
    // PERSONALIDADE
    // =========================================================

    private void UpdatePersonality(float dt)
    {
        laneShuffleTimer -= dt;
        if (laneShuffleTimer <= 0f)
        {
            laneFraction = Random.Range(-0.7f, 0.7f);
            laneShuffleTimer = Random.Range(6f, 14f);
        }

        if (!enableMistakes)
            return;

        if (mistakeTimer > 0f)
        {
            mistakeTimer -= dt;
            return;
        }

        nextMistakeIn -= dt;
        if (nextMistakeIn <= 0f)
        {
            mistakeTimer = Random.Range(0.5f, 1.1f);
            mistakeOffset = Random.Range(-2.5f, 2.5f);
            nextMistakeIn = Random.Range(12f, 30f) * Mathf.Lerp(0.6f, 1.6f, skillNorm);
        }
    }

    // =========================================================
    // SENSORES
    // =========================================================

    private void Scan()
    {
        blockerFound = false;
        leftBlocked = false;
        rightBlocked = false;
        blockerFwd = float.MaxValue;

        Vector2 forward = GetForward();
        Vector2 left = new Vector2(-forward.y, forward.x);

        int count = Physics2D.OverlapCircle(rb.position, sensorRadius, scanFilter, scanBuffer);

        for (int i = 0; i < count; i++)
        {
            Rigidbody2D other = scanBuffer[i].attachedRigidbody;

            if (other == null || other == rb)
                continue;

            Vector2 rel = other.position - rb.position;
            float fwd = Vector2.Dot(rel, forward);
            float side = Vector2.Dot(rel, left);

            if (Mathf.Abs(fwd) < carLength * 0.9f && Mathf.Abs(side) < carWidth * 1.7f)
            {
                if (side > 0f) leftBlocked = true;
                else rightBlocked = true;
            }

            if (fwd > carLength * 0.5f && Mathf.Abs(side) < carWidth * 1.1f && fwd < blockerFwd)
            {
                blockerFound = true;
                blockerFwd = fwd;
                blockerSide = side;
                blockerSpeed = Vector2.Dot(other.linearVelocity, forward);
            }
        }
    }

    // =========================================================
    // FAIXA: linha de corrida + ultrapassagem
    // =========================================================

    private void UpdateLane(float dt, float actualOffset, float ourTarget)
    {
        float maxOffset = Mathf.Max(0.5f, trackHalfWidth - edgeMargin);

        float desired = Mathf.Lerp(
            laneFraction * maxOffset,
            cornerSign * maxOffset,
            cornerStrength * racingLineStrength
        );

        if (mistakeTimer > 0f)
            desired += mistakeOffset;

        overtakeTimer -= dt;
        float passGap = carWidth * 1.3f;

        if (blockerFound)
        {
            float blockerOffset = actualOffset + blockerSide;
            bool weAreFaster = ourTarget > blockerSpeed + 1f;
            float startDist = Mathf.Lerp(8f, 18f, aggression);

            if (overtakeTimer <= 0f && weAreFaster && blockerFwd < startDist)
            {
                bool canLeft = !leftBlocked && blockerOffset + passGap <= maxOffset + 0.5f;
                bool canRight = !rightBlocked && blockerOffset - passGap >= -maxOffset - 0.5f;

                float side = 0f;

                if (canLeft && canRight)
                    side = (maxOffset - blockerOffset) > (blockerOffset + maxOffset) ? 1f : -1f;
                else if (canLeft)
                    side = 1f;
                else if (canRight)
                    side = -1f;

                if (side != 0f)
                {
                    overtakeSide = side;
                    overtakeTimer = Random.Range(1.5f, 2.5f);
                }
            }

            if (overtakeTimer > 0f && overtakeSide != 0f)
                overtakeLane = Mathf.Clamp(blockerOffset + overtakeSide * passGap, -maxOffset, maxOffset);
        }

        if (overtakeTimer > 0f && overtakeSide != 0f)
            desired = overtakeLane;

        desired = Mathf.Clamp(desired, -maxOffset, maxOffset);
        laneOffset = Mathf.MoveTowards(laneOffset, desired, laneChangeSpeed * dt);

        if (leftBlocked) laneOffset = Mathf.Min(laneOffset, actualOffset);
        if (rightBlocked) laneOffset = Mathf.Max(laneOffset, actualOffset);
    }

    private float ApplyTraffic(float target, float speed)
    {
        if (!blockerFound)
            return target;

        float gap = blockerFwd - carLength;
        float desiredGap = Mathf.Lerp(3f, 1.2f, aggression) + speed * 0.12f;

        if (overtakeTimer > 0f)
            desiredGap *= 0.5f;

        if (gap < desiredGap * 2f)
        {
            float followSpeed = Mathf.Max(0f, blockerSpeed + (gap - desiredGap) * 1.5f);
            target = Mathf.Min(target, followSpeed);
        }

        return target;
    }

    // =========================================================
    // ANTI-TRAVAMENTO
    // =========================================================

    private void UpdateStuck(float dt, float speed)
    {
        if (reverseTimer > 0f)
        {
            reverseTimer -= dt;
            stuckTimer = 0f;
            return;
        }

        if (speed < 1f)
        {
            stuckTimer += dt;

            if (stuckTimer > 1.5f)
            {
                reverseTimer = 1f;
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    // =========================================================
    // MOTOR / FREIO
    // =========================================================

    private void Drive(float target, float forwardSpeed, Vector2 forward)
    {
        lastThrottle = 0f;
        lastBrake = 0f;

        if (reverseTimer > 0f)
        {
            rb.AddForce(-forward * acceleration * 0.7f);
            return;
        }

        float diff = target - forwardSpeed;

        if (diff > 0.3f)
        {
            lastThrottle = Mathf.Clamp01(diff / 2f);
            rb.AddForce(forward * acceleration * lastThrottle);
        }
        else if (diff < -0.3f)
        {
            lastBrake = Mathf.Clamp01(-diff / 1.5f);
            float brake = braking * lastBrake + naturalDeceleration;
            rb.AddForce(-forward * brake);
        }
    }

    private float GetGripMultiplier()
    {
        return physics.GripMultiplier(lastThrottle, lastBrake);
    }

    private void ApplyGripAndLimit(float personalMax, float dt)
    {
        Vector2 forward = GetForward();
        Vector2 left = new Vector2(-forward.y, forward.x);

        // grip limitado (o carro escorrega se passar do limite)
        physics.ApplyGrip(rb, forward, dt, GetGripMultiplier());

        float f = Vector2.Dot(rb.linearVelocity, forward);
        float l = Vector2.Dot(rb.linearVelocity, left);

        f = Mathf.Clamp(f, -maxReverseSpeed, personalMax);

        rb.linearVelocity = forward * f + left * l;
    }

    // =========================================================
    // DIREÇÃO
    // =========================================================

    private void Steer(float angle, float speed, float dt)
    {
        float targetSteer = Mathf.Clamp(angle / steerFullAngle, -1f, 1f);
        smoothedSteer = Mathf.MoveTowards(smoothedSteer, targetSteer, steeringResponse * dt);

        // mesma regra do jogador: limitado pelo grip (understeer)
        float yaw = physics.GetYawRate(smoothedSteer, speed, maxSpeed, GetGripMultiplier());

        rb.MoveRotation(rb.rotation + yaw * dt);
    }

    // =========================================================
    // DEBUG
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, debugLookPoint);
        Gizmos.DrawWireSphere(debugLookPoint, 0.4f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, sensorRadius);
    }
}