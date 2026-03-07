using UnityEngine;

namespace CityBuilder.Roads
{
    /// <summary>
    /// Periodically triggers a RoadUserSpawner using InvokeRepeating.
    /// </summary>
    public class RandomSpawner : MonoBehaviour
    {
        [SerializeField] private RoadUserSpawner spawner;
        [Header("Timing (seconds)")]
        [SerializeField] private Vector2 initialDelayRange = new Vector2(0.5f, 1.5f);
        [SerializeField] private Vector2 repeatIntervalRange = new Vector2(2f, 5f);

        private void OnEnable()
        {
            if (spawner == null)
            {
                Debug.LogWarning($"{nameof(RandomSpawner)} missing spawner reference.");
                return;
            }

            float initialDelay = Random.Range(Mathf.Min(initialDelayRange.x, initialDelayRange.y), Mathf.Max(initialDelayRange.x, initialDelayRange.y));
            float repeatInterval = Random.Range(Mathf.Min(repeatIntervalRange.x, repeatIntervalRange.y), Mathf.Max(repeatIntervalRange.x, repeatIntervalRange.y));
            InvokeRepeating(nameof(SpawnAgent), initialDelay, repeatInterval);
        }

        private void OnDisable()
        {
            CancelInvoke();
        }

        private void SpawnAgent()
        {
            spawner.SpawnAgent();
        }
    }
}
