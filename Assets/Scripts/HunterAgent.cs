using System.Collections.Generic;
using UnityEngine;
namespace HuntingSimulation
{
    public sealed class HunterAgent : AgentMotor
    {
        [Header("Ataques y percepcion")]
        public float TBA = 1.8f;
        public float RangeAttackRadius = 6f;
        public float MeleeAttackRadius = 2.4f;
        public float VisionRadius = 10f;
        public float sensorInterval = 0.1f;
        [Header("Energia")]
        public float Energy = 100f;
        public float MaxEnergy = 100f;
        public float energyDrain = 12f;
        public float energyRecharge = 20f;
        [Header("Patrulla y recoleccion")]
        public float gatherDuration = 1.5f;
        public float gatherArrivalRadius = 1.45f;
        public float interestInterval = 5f;
        public float waypointArrivalRadius = 0.8f;
        public float arrivalSlowingDistance = 2f;
        public Transform[] waypoints;
        public Transform interestContainer;
        public InterestObject interestPrefab;
        [Header("Feedback")]
        public float shotDuration = 0.22f;
        public float shotWidth = 0.1f;
        public float shotHeight = 0.4f;
        public BoidAgent Target { get; internal set; }
        public HunterFSM FSM { get; private set; }
        public Vector3 LastKnownPosition { get; private set; }
        public float NextAttackTime { get; private set; }
        public float Cooldown => Mathf.Max(0, NextAttackTime - Time.time);
        public string LastAction { get; private set; } = "Iniciando patrulla";
        public int Attacks { get; private set; }
        public int Gathered { get; private set; }
        public List<BoidAgent> Detected { get; } = new List<BoidAgent>();
        float nextInterest, nextSense, shotUntil;
        int waypointIndex;
        LineRenderer shot;
        protected override void Awake()
        {
            base.Awake();
            sensor = LocalSensor.Create(transform, VisionRadius);
            ConfigureFeedback();
            Energy = MaxEnergy;
            LastKnownPosition = transform.position;
            FSM = new HunterFSM(this);
        }
        void ConfigureFeedback()
        {
            shot = gameObject.AddComponent<LineRenderer>();
            shot.material = new Material(Shader.Find("Sprites/Default"));
            shot.startColor = shot.endColor = Color.red;
            shot.startWidth = shot.endWidth = shotWidth;
            shot.positionCount = 2; shot.enabled = false;
        }
        void Update()
        {
            Sense();
            FSM.Tick(Time.deltaTime);
            shot.enabled = Time.time < shotUntil;
        }
        void Sense()
        {
            if (Time.time < nextSense) return;
            nextSense = Time.time + sensorInterval;
            sensor.SetRadius(VisionRadius);
            Detected.Clear();
            foreach (var hit in sensor.Candidates)
                if (hit != null && hit.enabled && hit.TryGetComponent(out BoidAgent boid) && !boid.Collected &&
                    Vector3.Distance(transform.position, boid.transform.position) <= VisionRadius) Detected.Add(boid);
        }
        public bool Visible(BoidAgent boid) => boid != null && !boid.Collected && Detected.Contains(boid) && Vector3.Distance(transform.position, boid.transform.position) <= VisionRadius;
        public BoidAgent Nearest(bool dead)
        {
            BoidAgent result = null; float best = float.PositiveInfinity;
            foreach (var boid in Detected)
            {
                if (!Visible(boid) || boid.CollectionRequested || boid.Dead != dead) continue;
                float distance = Vector3.SqrMagnitude(boid.transform.position - transform.position);
                if (distance < best) { result = boid; best = distance; }
            }
            return result;
        }
        public float TargetDistance => Target == null ? float.PositiveInfinity : Vector3.Distance(transform.position, Target.transform.position);
        public void RememberTarget() { if (Target != null && !Target.Dead) LastKnownPosition = Target.transform.position; }
        public void Pursue() => Seek(Steering.Predict(transform.position, Target.transform.position, Target.Velocity, speed, predictionHorizon));
        public void Patrol()
        {
            if (waypoints == null || waypoints.Length == 0) { Stop(); return; }
            Vector3 point = waypoints[waypointIndex].position; point.y = transform.position.y;
            if (Vector3.Distance(transform.position, point) <= waypointArrivalRadius) waypointIndex = (waypointIndex + 1) % waypoints.Length;
            Seek(point);
        }
        public void SpawnInterest()
        {
            if (Time.time < nextInterest) return;
            nextInterest = Time.time + interestInterval;
            if (InterestObject.ActiveCount < InterestObject.MaximumActive && interestPrefab != null && TrySpawnPosition(out Vector3 point))
                Instantiate(interestPrefab, point, Quaternion.identity, interestContainer);
        }
        public void PerformAttack(bool melee)
        {
            if (Cooldown > 0 || Target == null || Target.Dead) return;
            Target.Damage(melee ? BoidAgent.MaximumHits : 1);
            NextAttackTime = Time.time + TBA; Attacks++;
            LastAction = (melee ? "Cuerpo a cuerpo (2 hits): " : "Distancia (1 hit): ") + Target.name;
            shot.SetPosition(0, transform.position + Vector3.up * shotHeight);
            shot.SetPosition(1, Target.transform.position + Vector3.up * shotHeight);
            shotUntil = Time.time + shotDuration;
        }
        public void FinishGather()
        {
            LastAction = "Recolectado: " + Target.name;
            Target.Collect(); Gathered++;
        }
        void OnValidate()
        {
            RangeAttackRadius = Mathf.Max(0.02f, RangeAttackRadius);
            MeleeAttackRadius = Mathf.Clamp(MeleeAttackRadius, 0.01f, RangeAttackRadius * 0.9f);
            MaxEnergy = Mathf.Max(1, MaxEnergy);
            sensorInterval = Mathf.Max(0.02f, sensorInterval);
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(transform.position, VisionRadius);
            Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, RangeAttackRadius);
            Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, MeleeAttackRadius);
            if (Target != null) Gizmos.DrawLine(transform.position, Target.transform.position);
        }
    }
}
