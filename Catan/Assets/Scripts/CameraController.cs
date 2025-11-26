using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{

    private InputSystem_Actions _inputs;

    private void Awake()
    {
        _inputs = new InputSystem_Actions();
        _inputs.Camera.Enable();
        
        _inputs.Camera.Move.performed += ctx => Move(ctx.ReadValue<Vector2>());
        _inputs.Camera.Zoom.performed += ctx => Zoom(ctx.ReadValue<Vector2>().y);
    }

    private void Move(Vector2 input)
    {
        if (Mouse.current.leftButton.isPressed)
        {
            transform.position -= new Vector3(input.x, 0f, input.y);
        }
    }

    private void Zoom(float input)
    {
        transform.position -= new Vector3(0f, input);
    }
}
