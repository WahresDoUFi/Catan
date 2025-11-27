using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    private static CameraController _instance;

    public static bool IsOverview => Mathf.Approximately(_instance.transform.position.y, _instance.heightLimit.y);

    private InputSystem_Actions _inputs;
    
    private float ZoomPercentage => Mathf.InverseLerp(heightLimit.x, heightLimit.y, _targetPosition.y);
    private float Speed => Mathf.Lerp(cameraSpeed.x, cameraSpeed.y, ZoomPercentage);
    private float MaxTilt => Mathf.Lerp(tiltLimit.x, tiltLimit.y, ZoomPercentage);
    private Vector3 TargetRotation => new Vector3(Mathf.Clamp(_targetTilt, MaxTilt, tiltLimit.y), _targetRotation, 0f);

    private Vector3 Forward => Quaternion.Euler(0f, _targetRotation, 0f) * Vector3.forward;
    private Vector3 Right => Quaternion.Euler(0f, 90f, 0f) * Forward;
    
    [SerializeField]
    private float maxDistance;
    [SerializeField] private Vector2 heightLimit;
    [SerializeField] private Vector2 tiltLimit;
    [SerializeField] private Vector2 cameraSpeed;

    [SerializeField] private float cameraLerpSpeed;
    private Vector3 _targetPosition;
    private float _targetTilt;
    private float _targetRotation;

    private Vector3 _overviewPosition;
    private Vector3 _previousPosition;

    private void Awake()
    {
        _instance = this;
        
        _targetPosition = _overviewPosition = transform.position;
        _targetTilt = transform.eulerAngles.x;
        _targetRotation = transform.eulerAngles.y;
        _inputs = new InputSystem_Actions();
        _inputs.Camera.Enable();
        
        _inputs.Camera.Move.performed += ctx => Move(ctx.ReadValue<Vector2>());
        _inputs.Camera.Zoom.performed += ctx => Zoom(ctx.ReadValue<Vector2>().y);
        _inputs.Camera.Overview.performed += ctx => EnterOverviewPosition();
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * cameraLerpSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(TargetRotation), Time.deltaTime * cameraLerpSpeed);
    }

    private void Move(Vector2 input)
    {
        if (Mouse.current.leftButton.isPressed)
        {
            _targetPosition -= Right * input.x * Speed;
            _targetPosition -= Forward * input.y * Speed;
            float height = _targetPosition.y;
            var clampedPosition =
                Vector3.ClampMagnitude(Vector3.ProjectOnPlane(_targetPosition, Vector3.up), maxDistance);
            clampedPosition.y = height;
            _targetPosition = clampedPosition;
            _previousPosition = _targetPosition;
        } else if (Mouse.current.rightButton.isPressed)
        {
            float tilt = _targetTilt - input.y;
            tilt = Mathf.Clamp(tilt, tiltLimit.x, tiltLimit.y);
            _targetTilt = tilt;

            float rotation = _targetRotation + input.x;
            rotation %= 360f;
            _targetRotation = rotation;
        }
    }

    private void Zoom(float input)
    {
        var targetHeight = _targetPosition.y - input;
        targetHeight = Mathf.Clamp(targetHeight, heightLimit.x, heightLimit.y);
        _targetPosition.y = targetHeight;
        _previousPosition = _targetPosition;
    }

    private void EnterOverviewPosition()
    {
        _targetPosition = _targetPosition == _overviewPosition ? _previousPosition : _overviewPosition;
    }
}
