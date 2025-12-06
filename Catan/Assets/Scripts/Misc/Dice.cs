using UnityEngine;

namespace Misc
{
    [RequireComponent(typeof(Rigidbody))]
    public class Dice : MonoBehaviour
    {
        private static readonly Vector3[] FaceDirections = new[]
        {
            Vector3.zero,
            new Vector3(0, 0, -90),
            new Vector3(90, 0, 0),
            new Vector3(-90, 0, 0),
            new Vector3(0, 0, 90),
            new Vector3(180, 0, 0)
        };

        public Vector3 Position => _rigidbody.position;
        public Vector3 Rotation => _rigidbody.rotation.eulerAngles;
        public Vector3 Velocity => _rigidbody.linearVelocity;
        public bool Stable => _realVelocity.sqrMagnitude < 0.01f;
        public bool Active => _rigidbody.isKinematic == false;
        
        [SerializeField] private float gravity;
        [SerializeField] private float correctionThreshold;
        [SerializeField] private float drag;
        [SerializeField] private AudioClip[] collisionSounds;

        private Vector3 _velocity;
        private Vector3 _angularVelocity;
        private Rigidbody _rigidbody;
        private AudioSource _audioSource;
        private Vector3 _realVelocity;
        private Vector3 _lastPosition;
        private int _targetNumber;

        private void Awake()
        {
            _lastPosition = transform.position;
            _rigidbody = GetComponent<Rigidbody>();
            _audioSource = GetComponent<AudioSource>();
        }

        private void FixedUpdate()
        {
            CalculateRealVelocity();
            ApplyGravity();
            _rigidbody.linearVelocity = _velocity;
            _rigidbody.angularVelocity = _angularVelocity;
        }

        public void SetThrowPosition(Vector3 position, Vector3 rotation, Vector3 velocity)
        {
            _rigidbody.position = position;
            _rigidbody.rotation = Quaternion.Euler(rotation);
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = velocity;
            _realVelocity = velocity;
        }

        public void SetPosition(Vector3 position)
        {
            enabled = false;
            _rigidbody.isKinematic = true;
            _rigidbody.position = position;
        }

        public void SetTarget(Vector3 position, float speed, float smoothing)
        {
            _rigidbody.isKinematic = false;
            var direction = position - _rigidbody.position;
            var velocity = Vector3.ClampMagnitude(direction * speed, speed);
            _rigidbody.linearVelocity = Vector3.Lerp(_rigidbody.linearVelocity, velocity, smoothing * Time.fixedDeltaTime);
            _rigidbody.angularVelocity = -Vector3.Cross(velocity.normalized, Vector3.up);
        }

        public void Release(float maxForce)
        {
            enabled = true;
            _velocity = _rigidbody.linearVelocity;
            _velocity = Vector3.ClampMagnitude(_velocity, maxForce);
            _velocity.y = -gravity;
            _angularVelocity = -Vector3.Cross(_velocity, Vector3.up);
        }

        public void SetTargetNumber(int number)
        {
            _targetNumber = number;
        }

        private void CalculateRealVelocity()
        {
            _realVelocity = _rigidbody.position - _lastPosition;
            _realVelocity /= Time.fixedDeltaTime;
            _lastPosition = _rigidbody.position;
        }

        private void ApplyGravity()
        {
            _velocity += Vector3.down * (gravity * Time.fixedDeltaTime);
            _velocity = Vector3.Lerp(_velocity, Vector3.zero, Time.fixedDeltaTime * drag);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!enabled) return;
            var contactPoint = Vector3.zero;
            foreach (var point in collision.contacts)
            {
                contactPoint += point.point;
            }
            contactPoint /= collision.contacts.Length;
            var direction = transform.position - contactPoint;
            if (_velocity.magnitude > 0.3f)
                _velocity = direction.normalized * _velocity.magnitude * 0.75f;
            _angularVelocity = -Vector3.Cross(direction, Vector3.up);

            PlayCollisionSound(collision);
            RotateTowardsTarget();
        }

        private void PlayCollisionSound(Collision collision)
        {
            var impactForce = collision.relativeVelocity.magnitude;
            if (impactForce > 0.5f && collisionSounds.Length > 0 && !_audioSource.isPlaying)
            {
                var randomClip = collisionSounds[Random.Range(0, collisionSounds.Length)];
                _audioSource.pitch = Random.Range(0.9f, 1.1f);
                _audioSource.volume = Mathf.Clamp01(impactForce / 15f);
                _audioSource.PlayOneShot(randomClip);
            }
        }

        private void RotateTowardsTarget()
        {
            if (_velocity.y > correctionThreshold) return;
            var targetRotation = Quaternion.Euler(FaceDirections[_targetNumber - 1]);
            var currentRotation = transform.rotation;

            var delta = targetRotation * Quaternion.Inverse(currentRotation);
            delta.ToAngleAxis(out float angleDeg, out Vector3 axis);

            if (angleDeg < 0.1f)
            {
                _angularVelocity = Vector3.zero;
                return;
            }

            _angularVelocity = axis.normalized * (angleDeg * Mathf.Deg2Rad);
            float desiredUpForce = angleDeg / 10f;
            _velocity.y = Mathf.Max(desiredUpForce, _velocity.y);
        }
    }
}