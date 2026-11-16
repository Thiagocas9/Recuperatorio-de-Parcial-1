using UnityEngine;
namespace HuntingSimulation
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class InterestObject : MonoBehaviour
    {
        public const int MaximumActive = 5;
        public float health = 45f;
        public float Radius => GetComponent<SphereCollider>().radius * transform.lossyScale.x;
        public static int ActiveCount { get; private set; }
        public static int DestroyedCount { get; private set; }
        public bool Available => health > 0 && isActiveAndEnabled;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCounters() { ActiveCount = 0; DestroyedCount = 0; }
        void OnEnable() => ActiveCount++;
        void OnDisable() => ActiveCount--;
        public void Damage(float amount)
        {
            if (!Available) return;
            health = Mathf.Max(0, health - amount);
            if (health <= 0) { DestroyedCount++; gameObject.SetActive(false); Destroy(gameObject); }
        }
    }
}
