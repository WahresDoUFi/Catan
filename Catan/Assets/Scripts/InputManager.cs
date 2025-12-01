using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    private InputSystem_Actions _input;

    private void Awake()
    {
        _input = new InputSystem_Actions();
        SetupCameraInputs();
        SetupClickInputs();
    }

    private void OnDisable()
    {
        _input.Dispose();
    }

    private void SetupCameraInputs()
    {
        _input.Camera.Enable();
        _input.Camera.Move.performed += ctx => CameraController.Instance.Move(ctx.ReadValue<Vector2>());
        _input.Camera.Zoom.performed += ctx => CameraController.Instance.Zoom(ctx.ReadValue<Vector2>().y);
        _input.Camera.Overview.performed += _ => CameraController.Instance.EnterOverview(true);
    }

    private void SetupClickInputs()
    {
        _input.UI.Enable();
        _input.UI.Click.performed += _ => BuildManager.ConfirmPosition();
    }
}
