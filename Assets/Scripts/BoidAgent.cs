using System.Collections.Generic;
using UnityEngine;

namespace HuntingSimulation
{
    public sealed class BoidAgent : AgentMotor
    {
        [Header("Sensores locales")]
        public float perceptionRadius = 6f;
        public float separationRadius = 1.8f;
        public float threatRadius = 7f;
        public float sensorInterval = 0.1f;
        [Header("Pesos de fuerzas limitadas individualmente")]
        public float separationWeight = 2.6f;
        public float alignmentWeight = 1f;
        public float cohesionWeight = 0.8f;
        public float cruiseWeight = 0.65f;
        [Header("Interaccion y ciclo de vida")]
        public float interactionInterval = 0.7f;
        public float consumptionDamage = 10f;
        public float respawnDelay = 5f;
        public float arrivalSlowingDistance = 3f;
        public float arrivalTolerance = 0.25f;
        public const int MaximumHits = 2;
        public int Health { get; private set; } = MaximumHits;
        public bool Dead => Health <= 0;
        public bool Collected { get; private set; }
        public bool CollectionRequested { get; private set; }
        public string Behaviour => FSM?.Current.Name ?? "Flocking";
        public StateMachine FSM { get; private set; }
        public int Neighbours => neighbours.Count;
        public static int Respawns { get; private set; }
        public HunterAgent Threat { get; private set; }
        public InterestObject Interest { get; private set; }
        public float ObjectDistance => Interest == null ? float.PositiveInfinity : Vector3.Distance(transform.position, Interest.transform.position);
        public float ObjectStopDistance => Interest == null ? 0 : Radius + Interest.Radius + contactPadding + obstacleMargin;
        readonly List<BoidAgent> neighbours = new List<BoidAgent>();
        Vector3 heading;
        float nextSense;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCounters() => Respawns = 0;
        protected override void Awake()
        {
            base.Awake();
            heading = Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward;
            sensor = LocalSensor.Create(transform, Mathf.Max(perceptionRadius, threatRadius));
            FSM = new StateMachine(new FlockingState(this));
            nextSense = Time.time + Random.value * sensorInterval;
        }
        void Update()
        {
            RefreshPerception();
            FSM.Tick(Time.deltaTime);
        }
        void RefreshPerception()
        {
            if (Time.time < nextSense) return;
            nextSense = Time.time + sensorInterval;
            sensor.SetRadius(Mathf.Max(perceptionRadius, threatRadius));
            neighbours.Clear(); Threat = null; Interest = null;
            if (Dead || Collected) return;
            float bestInterest = float.PositiveInfinity;
            foreach (var hit in sensor.Candidates)
            {
                if (hit == null || !hit.enabled || hit.gameObject == gameObject) continue;
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance <= threatRadius && hit.TryGetComponent(out HunterAgent hunter)) Threat = hunter;
                if (distance <= perceptionRadius && hit.TryGetComponent(out BoidAgent other) && !other.Collected) neighbours.Add(other);
                if (distance <= perceptionRadius && distance < bestInterest && hit.TryGetComponent(out InterestObject item) && item.Available)
                { Interest = item; bestInterest = distance; }
            }
        }
        public bool SeesThreat => Threat != null && Vector3.Distance(transform.position, Threat.transform.position) <= threatRadius;
        public bool SeesInterest => Interest != null && Interest.Available && ObjectDistance <= perceptionRadius;
        public bool AtObject => SeesInterest && ObjectDistance <= ObjectStopDistance + arrivalTolerance;
        public Vector3 Separation()
        {
            Vector3 away = Vector3.zero;
            foreach (var other in neighbours)
            {
                if (other == null || other.Collected) continue;
                Vector3 offset = transform.position - other.transform.position;
                float distance = offset.magnitude;
                if (distance > 0 && distance < separationRadius) away += offset.normalized * (1 - distance / separationRadius);
            }
            return away == Vector3.zero ? Vector3.zero : Force(Vector3.ClampMagnitude(away, 1) * speed);
        }
        public void Flock()
        {
            Vector3 alignment = Vector3.zero, center = Vector3.zero; int count = 0;
            foreach (var other in neighbours)
            {
                if (other == null || other.Dead || other.Collected || Vector3.Distance(transform.position, other.transform.position) > perceptionRadius) continue;
                alignment += other.Velocity; center += other.transform.position; count++;
            }
            Vector3 force = Separation() * separationWeight;
            if (count > 0)
            {
                force += Force(alignment / count) * alignmentWeight;
                force += Force(Steering.Seek(transform.position, center / count, speed)) * cohesionWeight;
            }
            else force += Force(heading * speed) * cruiseWeight;
            if (Velocity.sqrMagnitude > Mathf.Epsilon) heading = Velocity.normalized;
            Drive(force);
        }
        public void Evade()
        {
            Vector3 future = Steering.Predict(transform.position, Threat.transform.position, Threat.Velocity, speed + Threat.speed, predictionHorizon);
            Vector3 escape = transform.position - future;
            if (escape.sqrMagnitude <= Mathf.Epsilon) escape = heading;
            Drive(Force(escape.normalized * speed) + Separation() * separationWeight);
        }
        public void SeekObject() => Drive(Force(Steering.Arrive(transform.position, Interest.transform.position, speed, ObjectStopDistance, arrivalSlowingDistance)) + Separation() * separationWeight);
        public void Damage(int hits) { if (!Dead && !Collected) Health = Mathf.Max(0, Health - Mathf.Max(0, hits)); }
        public void Collect() { if (Dead && !Collected) CollectionRequested = true; }
        public void EnterDead()
        {
            Stop(); body.linearVelocity = Vector3.zero; body.isKinematic = true;
            SetTint(Color.gray);
        }
        public void HideForRespawn()
        {
            Collected = true; visual.enabled = false;
            GetComponent<Collider>().enabled = false;
            sensor.gameObject.SetActive(false);
        }
        public bool TryRespawn()
        {
            if (!TrySpawnPosition(out Vector3 point)) return false;
            body.position = point; transform.position = point;
            body.isKinematic = false;
            Health = MaximumHits; Collected = false; CollectionRequested = false;
            visual.enabled = true; GetComponent<Collider>().enabled = true;
            sensor.gameObject.SetActive(true); nextSense = 0; Respawns++;
            return true;
        }
        void OnValidate()
        {
            perceptionRadius = Mathf.Max(1, perceptionRadius);
            separationRadius = Mathf.Clamp(separationRadius, 0.01f, perceptionRadius * 0.9f);
            sensorInterval = Mathf.Max(0.02f, sensorInterval);
            arrivalSlowingDistance = Mathf.Max(0.01f, arrivalSlowingDistance);
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, perceptionRadius);
            Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, separationRadius);
            Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, threatRadius);
        }
    }
}
