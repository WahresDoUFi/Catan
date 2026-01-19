using GamePlay;
using Misc;
using UI;
using UI.DevelopmentCards;
using UI.Trade;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

namespace User
{
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance;

        public static bool IsOverview => Mathf.Approximately(Instance._targetPosition.y, Instance.heightLimit.y);
        private float ZoomPercentage => Mathf.InverseLerp(heightLimit.x, heightLimit.y, _targetPosition.y);
        private float Speed => Mathf.Lerp(cameraSpeed.x, cameraSpeed.y, ZoomPercentage);
        private float MaxTilt => Mathf.Lerp(tiltLimit.x, tiltLimit.y, ZoomPercentage);

        private Vector3 TargetRotation =>
            new Vector3(Mathf.Clamp(_targetTilt, MaxTilt, tiltLimit.y), _targetRotation, 0f);

        private Vector3 Forward => Quaternion.Euler(0f, _targetRotation, 0f) * Vector3.forward;
        private Vector3 Right => Quaternion.Euler(0f, 90f, 0f) * Forward;

        [SerializeField] private float maxDistance;
        [SerializeField] private Vector2 heightLimit;
        [SerializeField] private Vector2 tiltLimit;
        [SerializeField] private Vector2 cameraSpeed;
        [SerializeField] private float cameraLerpSpeed;

        private Vector3 _targetPosition;
        private float _targetTilt;
        private float _targetRotation;
        private Vector3 _overviewPosition;
        private Vector3 _previousPosition;
        private Camera _camera;

        //  input data
        private Vector2 _moveInput;
        private float _scrollInput;

        private void Awake()
        {
            Instance = this;

            _targetPosition = _overviewPosition = transform.position;
            _targetTilt = transform.eulerAngles.x;
            _targetRotation = transform.eulerAngles.y;
            _camera = GetComponent<Camera>();
        }

        private void Update()
        {
            PerformMove();
            PerformZoom();
            UpdatePosition();
        }

        public void EnterOverview(bool isToggle = false)
        {
            _targetPosition = _targetPosition == _overviewPosition && isToggle ? _previousPosition : _overviewPosition;
        }

        public void Move(Vector2 input)
        {
            _moveInput = input;
        }

        public void Zoom(float input)
        {
            _scrollInput = input;
        }

        private void PerformMove()
        {
            if (CanMove())
            {
                if (Mouse.current.leftButton.isPressed)
                {
                    _targetPosition -= Right * (_moveInput.x * Speed * Time.deltaTime);
                    _targetPosition -= Forward * (_moveInput.y * Speed * Time.deltaTime);
                    float height = _targetPosition.y;
                    var clampedPosition =
                        Vector3.ClampMagnitude(Vector3.ProjectOnPlane(_targetPosition, Vector3.up), maxDistance);
                    clampedPosition.y = height;
                    _targetPosition = clampedPosition;
                    _previousPosition = _targetPosition;
                }
                else if (Mouse.current.rightButton.isPressed)
                {
                    float tilt = _targetTilt - _moveInput.y;
                    tilt = Mathf.Clamp(tilt, tiltLimit.x, tiltLimit.y);
                    _targetTilt = tilt;

                    float rotation = _targetRotation + _moveInput.x;
                    rotation %= 360f;
                    _targetRotation = rotation;
                }
            }
            _moveInput = Vector2.zero;
        }

        private void UpdatePosition()
        {
            transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * cameraLerpSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(TargetRotation),
                Time.deltaTime * cameraLerpSpeed);
        }

        private void PerformZoom()
        {
            if (CanMove())
            {
                var targetHeight = _targetPosition.y - _scrollInput;
                targetHeight = Mathf.Clamp(targetHeight, heightLimit.x, heightLimit.y);
                _targetPosition.y = targetHeight;
                _previousPosition = _targetPosition;
            }
            _scrollInput = 0f;
        }

        public Vector3 MouseWorldPosition(float offset = 0)
        {
            Vector3 mousePos = Mouse.current.position.ReadValue();
            mousePos.z = _camera.transform.position.y - offset;
            return _camera.ScreenToWorldPoint(mousePos);
        }

        public GameObject Raycast(LayerMask mask, float offset = 0f)
        {
            var targetPos = MouseWorldPosition(offset);
            var direction = targetPos - transform.position;
            if (Physics.Raycast(transform.position, direction.normalized, out RaycastHit hit, float.MaxValue, mask))
            {
                return hit.collider.gameObject;
            }
            return null;
        }

        private bool CanMove()
        {
            if (BuildManager.BuildModeActive) return false;
            if (GameManager.Instance.IsMyTurn() && !(DiceController.Instance.HasThrown || GameManager.Instance.DiceThrown)) return false;
            if (TradeMenu.Instance.IsOpen) return false;
            if (TradeWindow.IsOpen) return false;
            if (PauseMenu.IsOpen) return false;
            if (GameManager.Instance.IsGameOver) return false;
            if (DevelopmentCardsDisplay.HasToRevealCard) return false;
            if (DevelopmentCardsMenu.IsOpen) return false;
            if (GameManager.Instance.RepositionBandit) return false;
            return true;
        }
    }
}