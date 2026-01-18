using UnityEngine;
using UnityEngine.InputSystem;

public class DrawingInputHandler : MonoBehaviour
{
    [SerializeField] private DrawingController _drawingController;

    public void HandleDrawInput(InputAction.CallbackContext context)
    {
        if(context.performed)
        {
            _drawingController.IsDrawing = true;
        }
        else if(context.canceled)
        {
            _drawingController.IsDrawing = false;
        }
    }

    public void HandleChangeDrawStateInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _drawingController.EnableDrawing = !_drawingController.EnableDrawing;
        }
    }

    public void HandleSaveDrawInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _drawingController.SaveDrawingToFile();
        }
    }

    public void HandleClearDrawInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _drawingController.ClearCanvas();
        }
    }

    public void HandleMousePositionInput(InputAction.CallbackContext context)
    {
        Vector2 mousePosition = context.ReadValue<Vector2>();
        _drawingController.MousePosition = mousePosition;
    }
}
