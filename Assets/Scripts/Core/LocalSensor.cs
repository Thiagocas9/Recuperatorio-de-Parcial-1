using System.Collections.Generic;
using UnityEngine;

namespace HuntingSimulation
{
    // El trigger registra candidatos locales; las decisiones filtran por distancia al centro.
    public sealed class LocalSensor : MonoBehaviour
    {
        public readonly HashSet<Collider> Candidates = new HashSet<Collider>();
        SphereCollider area;
        public static LocalSensor Create(Transform owner, float radius)
        {
            var go = new GameObject("Sensor local");
            go.layer = 9;
            go.transform.SetParent(owner, false);
            var sensor = go.AddComponent<LocalSensor>();
            sensor.area = go.AddComponent<SphereCollider>();
            sensor.area.isTrigger = true;
            sensor.SetRadius(radius);
            return sensor;
        }
        public void SetRadius(float radius) => area.radius = radius / transform.lossyScale.x;
        void OnTriggerEnter(Collider other) { if (other.gameObject.layer == 8 && !other.isTrigger) Candidates.Add(other); }
        void OnTriggerExit(Collider other) => Candidates.Remove(other);
        void OnDisable() => Candidates.Clear();
    }
}
