using UnityEngine;

namespace toio.Samples.Sample_Sensor
{
    public class ToioWasdDemoMover : MonoBehaviour
    {
        [SerializeField] private ToioWasdInput inputSource;
        [SerializeField] private float moveSpeed = 2.0f;
        [SerializeField] private float rotateSpeedDegPerSec = 120.0f;

        private void Reset()
        {
            inputSource = FindObjectOfType<ToioWasdInput>();
        }

        private void Update()
        {
            if (inputSource == null)
            {
                return;
            }

            transform.Rotate(0f, inputSource.Horizontal * rotateSpeedDegPerSec * Time.deltaTime, 0f, Space.Self);
            transform.Translate(Vector3.forward * inputSource.Vertical * moveSpeed * Time.deltaTime, Space.Self);
        }
    }
}
