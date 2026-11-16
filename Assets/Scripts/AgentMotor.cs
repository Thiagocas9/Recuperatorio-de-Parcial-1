using UnityEngine;

namespace HuntingSimulation
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class AgentMotor : MonoBehaviour
    {
        [Header("Movimiento y steering")]
        [Min(0.1f)] public float speed = 4f;
        [Min(0.1f)] public float acceleration = 12f;
        [Min(0.01f)] public float responseTime = 0.4f;
        public float rotationSpeedThreshold = 0.01f;
        [Min(0)] public float predictionHorizon = 1.2f;
        [Header("Limites y seguridad")]
        public float arenaHalfSize = 19f;
        public float boundaryMargin = 3f;
        public float boundaryWeight = 2f;
        public float obstacleMargin = 1.4f;
        public float obstacleWeight = 2.5f;
        public float obstacleSideWeight = 1.2f;
        [Range(0, 1)] public float obstacleForwardThreshold = 0.25f;
        public float contactPadding = 0.08f;
        public float spawnMargin = 3f;
        public float groundHeight = 0.6f;
        public int spawnAttempts = 200;
        public float spawnClearance = 1.4f;
        protected Rigidbody body;
        protected Renderer visual;
        protected LocalSensor sensor;
        Vector3 steering;
        bool stopped = true;
        public Vector3 Velocity => body == null || body.isKinematic ? Vector3.zero : body.linearVelocity;
        public float Radius => GetComponent<SphereCollider>().radius * transform.lossyScale.x;
        public bool WithinBounds(Vector3 point) => Mathf.Abs(point.x) <= arenaHalfSize && Mathf.Abs(point.z) <= arenaHalfSize;
        protected virtual void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            visual = GetComponent<Renderer>();
            var frictionless = new PhysicsMaterial("Agentes sin friccion") { dynamicFriction = 0, staticFriction = 0, bounciness = 0 };
            GetComponent<Collider>().material = frictionless;
        }
        protected void FixedUpdate()
        {
            if (body.isKinematic) return;
            if (stopped) { body.linearVelocity = Vector3.zero; return; }
            Vector3 velocity = Vector3.ClampMagnitude(body.linearVelocity + steering * Time.fixedDeltaTime, speed);
            velocity.y = 0;
            body.linearVelocity = velocity;
            if (velocity.sqrMagnitude > rotationSpeedThreshold * rotationSpeedThreshold) body.MoveRotation(Quaternion.LookRotation(velocity));
        }
        public Vector3 Force(Vector3 desired) => Steering.Force(desired, Velocity, responseTime, acceleration);
        public void Drive(Vector3 force)
        {
            Vector3 boundary = Vector3.zero;
            Vector3 p = transform.position;
            if (Mathf.Abs(p.x) > arenaHalfSize - boundaryMargin) boundary.x = -Mathf.Sign(p.x) * speed;
            if (Mathf.Abs(p.z) > arenaHalfSize - boundaryMargin) boundary.z = -Mathf.Sign(p.z) * speed;
            if (boundary != Vector3.zero) force += Force(boundary) * boundaryWeight;
            force += ObstacleAvoidance(force) * obstacleWeight;
            steering = Vector3.ClampMagnitude(force, acceleration);
            steering.y = 0; stopped = false;
        }
        Vector3 ObstacleAvoidance(Vector3 intention)
        {
            Vector3 away = Vector3.zero;
            if (sensor == null) return away;
            foreach (var hit in sensor.Candidates)
            {
                if (hit == null || !hit.enabled || !hit.TryGetComponent(out InterestObject item) || !item.Available) continue;
                Vector3 offset = transform.position - item.transform.position; offset.y = 0;
                float clearance = Radius + item.Radius + contactPadding;
                if (offset.magnitude < clearance + obstacleMargin)
                    {
                    float proximity = Mathf.Clamp01((clearance + obstacleMargin - offset.magnitude) / obstacleMargin);
                    away += offset.normalized * proximity;
                    if (Vector3.Dot(intention.normalized, -offset.normalized) > obstacleForwardThreshold)
                    {
                        Vector3 tangent = Vector3.Cross(Vector3.up, offset.normalized);
                        if (Vector3.Dot(tangent, intention) < 0) tangent = -tangent;
                        away += tangent * proximity * obstacleSideWeight;
                    }
                }
            }
            return away == Vector3.zero ? away : Force(Vector3.ClampMagnitude(away, 1) * speed);
        }
        public void Seek(Vector3 point) => Drive(Force(Steering.Seek(transform.position, point, speed)));
        public void Arrive(Vector3 point, float stop, float slowing) => Drive(Force(Steering.Arrive(transform.position, point, speed, stop, slowing)));
        public void Stop() { stopped = true; steering = Vector3.zero; }
        public void SetTint(Color color) => visual.material.color = color;
        public bool TrySpawnPosition(out Vector3 point)
        {
            for (int i = 0; i < spawnAttempts; i++)
            {
                float extent = arenaHalfSize - spawnMargin;
                point = new Vector3(Random.Range(-extent, extent), groundHeight, Random.Range(-extent, extent));
                if (!Physics.CheckSphere(point, spawnClearance, 1 << 8, QueryTriggerInteraction.Ignore)) return true;
            }
            point = default; return false;
        }
    }
}


