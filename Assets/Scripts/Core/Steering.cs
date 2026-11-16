using UnityEngine;

namespace HuntingSimulation
{
    public static class Steering
    {
        // Reynolds: fuerza = velocidad deseada - velocidad actual / tiempo de respuesta.
        // Cada contribución tiene el mismo límite antes de aplicar los pesos.
        public static Vector3 Force(Vector3 desired, Vector3 velocity, float response, float limit)
            => Vector3.ClampMagnitude((desired - velocity) / Mathf.Max(response, Mathf.Epsilon), limit);
        public static Vector3 Seek(Vector3 position, Vector3 target, float speed)
            => (target - position).normalized * speed;
        public static Vector3 Arrive(Vector3 position, Vector3 target, float speed, float stop, float slowing)
        {
            Vector3 offset = target - position; offset.y = 0;
            return offset.normalized * speed * Mathf.Clamp01((offset.magnitude - stop) / slowing);
        }
        public static Vector3 Predict(Vector3 position, Vector3 target, Vector3 velocity, float speed, float horizon)
            => target + velocity * Mathf.Min(horizon, Vector3.Distance(position, target) / Mathf.Max(speed, Mathf.Epsilon));
    }
}
