using UnityEngine;

namespace CityBuilder.Roads
{
    /// <summary>
    /// Simple idle bob: floats an object up and down over time (retro-style hover).
    /// </summary>
    public class FloatBob : MonoBehaviour
    {
        [SerializeField] private float amplitude = 0.2f;
        [SerializeField] private float frequency = 1f;

        private Vector3 _startPos;

        private void Awake()
        {
            _startPos = transform.localPosition;
        }

        private void Update()
        {
            float offset = Mathf.Sin(Time.time * frequency) * amplitude;
            transform.localPosition = _startPos + Vector3.up * offset;
        }
    }
}
