namespace Mantega.Drawer
{
    using Mantega.Core.Diagnostics;
    using UnityEngine;
    using UnityEngine.InputSystem;

    /// <summary>
    /// Handles user input for drawing operations.
    /// </summary>
    /// <remarks>This class is responsible for managing input actions related to drawing functionality. It
    /// binds input actions to their respective event handlers, ensuring that user interactions are processed and
    /// forwarded to the <see cref="DrawingController"/>. The input actions include drawing, toggling the drawing state,
    /// saving the drawing, clearing the canvas, and updating the pointer position. To use this class, ensure that all
    /// required dependencies, such as the <see cref="DrawingController"/> and input action references, are properly
    /// assigned in the Unity Inspector.</remarks>
    public class DrawingInputHandler : MonoBehaviour
    {
        /// <summary>
        /// The drawing controller that manages the drawing operations.
        /// </summary>
        [Header("Components")]
        [SerializeField] private DrawingController _drawingController;

        /// <summary>
        /// The input action reference for drawing.
        /// </summary>
        [Header("Input Actions")]
        [Tooltip("Button action called to draw")]
        [SerializeField] private InputActionReference _drawAction;

        /// <summary>
        /// Represents the input action used to toggle the activation or deactivation of the drawer.
        /// </summary>
        [Tooltip("Button action called to active/deactive the drawer")]
        [SerializeField] private InputActionReference _toggleDrawingAction;

        /// <summary>
        /// Represents the input action used to save the current drawing.
        /// </summary>
        [Tooltip("Button action called to save the draw")]
        [SerializeField] private InputActionReference _saveDrawAction;

        /// <summary>
        /// Represents the input action used to clear the drawing.
        /// </summary>
        [Tooltip("Button action called to clean the draw")]
        [SerializeField] private InputActionReference _clearDrawAction;

        /// <summary>
        /// Represents an input action used to retrieve the pointer position.
        /// </summary>
        [Tooltip("Value action called to pass the pointer position")]
        [SerializeField] private InputActionReference _pointerPositionAction;

        /// <summary>
        /// Validates required dependencies.
        /// </summary>
        /// <remarks>This method ensures that all required dependencies are properly set before the
        /// component is used.</remarks>
        /// <exception cref="System.ArgumentNullException">Thrown if the required dependency <see cref="_drawingController"/> is not assigned.</exception>
        private void Awake()
        {
            // Validate references
            if (_drawingController == null)
            {
                throw new System.ArgumentNullException(nameof(_drawingController));
            }
        }

        /// <summary>
        /// Initializes and binds input actions to their respective event handlers.
        /// </summary>
        /// <remarks>This method is called when the component is enabled. It sets up the necessary input
        /// actions to handle drawing, toggling drawing states, saving, clearing, and pointer position updates. Ensure
        /// that the input actions are properly configured before enabling the component.</remarks>
        private void OnEnable()
        {
            // Bind actions
            if(_drawAction != null) BindAction(_drawAction, OnDrawPerformed, OnDrawCanceled);
            if(_toggleDrawingAction != null) BindAction(_toggleDrawingAction, OnToggleDrawPerformed);
            if(_saveDrawAction != null) BindAction(_saveDrawAction, OnSavePerformed);
            if(_clearDrawAction != null) BindAction(_clearDrawAction, OnClearPerformed);
            if(_pointerPositionAction != null) BindAction(_pointerPositionAction, OnPointerPositionPerformed);
        }

        /// <summary>
        /// Unbinds all associated input actions to prevent further interaction.
        /// </summary>
        /// <remarks>This method ensures that all input actions previously bound to the object are
        /// properly unbound, preventing unintended behavior or resource leaks when the object is no longer
        /// active.</remarks>
        private void OnDisable()
        {
            // Unbind actions
            if (_drawAction != null) UnbindAction(_drawAction, OnDrawPerformed, OnDrawCanceled);
            if (_toggleDrawingAction != null) UnbindAction(_toggleDrawingAction, OnToggleDrawPerformed);
            if (_saveDrawAction != null) UnbindAction(_saveDrawAction, OnSavePerformed);
            if (_clearDrawAction != null) UnbindAction(_clearDrawAction, OnClearPerformed);
            if (_pointerPositionAction != null) UnbindAction(_pointerPositionAction, OnPointerPositionPerformed);
        }

        #region Bind/Unbind Input Actions
        /// <summary>
        /// Binds the specified input action to the provided callback methods for performed and canceled events.
        /// </summary>
        /// <remarks>This method ensures that the input action reference is valid before binding the
        /// callbacks. The caller is responsible for ensuring that the provided callbacks are thread-safe if used in a
        /// multi-threaded context.</remarks>
        /// <param name="actionRef">The <see cref="InputActionReference"/> representing the input action to bind.</param>
        /// <param name="onPerformed">The callback to invoke when the input action is performed.</param>
        /// <param name="onCanceled">The optional callback to invoke when the input action is canceled. If null, no binding is made for the
        /// canceled event.</param>
        private void BindAction(InputActionReference actionRef, System.Action<InputAction.CallbackContext> onPerformed, System.Action<InputAction.CallbackContext> onCanceled = null)
        {
            ValidateActionReference(actionRef);

            actionRef.action.performed += onPerformed;
            if (onCanceled != null) actionRef.action.canceled += onCanceled;
        }

        /// <summary>
        /// Unbinds the specified callbacks from the performed and canceled events of the given input action.
        /// </summary>
        /// <remarks>This method removes the specified callbacks from the input action's event handlers.
        /// Ensure that the callbacks were previously bound to avoid unexpected behavior.</remarks>
        /// <param name="actionRef">The <see cref="InputActionReference"/> representing the input action to unbind from.</param>
        /// <param name="onPerformed">The callback to unbind from the performed event of the input action.</param>
        /// <param name="onCanceled">The optional callback to unbind from the canceled event of the input action. If null, no action is taken for
        /// the canceled event.</param>
        private void UnbindAction(InputActionReference actionRef, System.Action<InputAction.CallbackContext> onPerformed, System.Action<InputAction.CallbackContext> onCanceled = null)
        {
            ValidateActionReference(actionRef);

            actionRef.action.performed -= onPerformed;
            if (onCanceled != null) actionRef.action.canceled -= onCanceled;
        }

        /// <summary>
        /// Validates the specified <see cref="InputActionReference"/> to ensure it is not null  and contains a valid
        /// action.
        /// </summary>
        /// <param name="actionRef">The <see cref="InputActionReference"/> to validate.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="actionRef"/> is null or if its <see cref="InputActionReference.action"/> property
        /// is null.</exception>
        private void ValidateActionReference(InputActionReference actionRef)
        {
            Validations.ValidateNotNull(actionRef, this);
            Validations.ValidateNotNull(actionRef.action, this);
        }

        #endregion

        #region Handle Input Actions
        /// <summary>
        /// Handles the "Draw" input action when it is performed.
        /// </summary>
        /// <param name="ctx">The context of the input action, containing information about the performed action.</param>
        private void OnDrawPerformed(InputAction.CallbackContext ctx) => _drawingController.IsDrawing = true;

        /// <summary>
        /// Handles the "Draw" input action when it is canceled.
        /// </summary>
        /// <param name="ctx">The context of the input action that triggered the cancellation.</param>
        private void OnDrawCanceled(InputAction.CallbackContext ctx) => _drawingController.IsDrawing = false;

        /// <summary>
        /// Handles the toggle drawing action triggered by the user.
        /// </summary>
        /// <param name="ctx">The context of the input action that triggered this method.</param>
        private void OnToggleDrawPerformed(InputAction.CallbackContext ctx)
        {
            _drawingController.EnableDrawing = !_drawingController.EnableDrawing;
        }

        /// <summary>
        /// Handles the save action triggered by the user.
        /// </summary>
        /// <param name="ctx">The context of the input action, containing information about the trigger event.</param>
        private void OnSavePerformed(InputAction.CallbackContext ctx)
        {
            _drawingController.SaveDrawingToFile();
        }

        /// <summary>
        /// Handles the clear action performed by the user.
        /// </summary>
        /// <param name="ctx">The context of the input action, providing details about the performed action.</param>
        private void OnClearPerformed(InputAction.CallbackContext ctx)
        {
            _drawingController.ClearCanvas();
        }

        /// <summary>
        /// Handles the pointer position input action and updates the mouse position in the drawing controller.
        /// </summary>
        /// <param name="ctx">The context of the input action, containing the pointer position as a <see cref="Vector2"/> value.</param>
        private void OnPointerPositionPerformed(InputAction.CallbackContext ctx)
        {
            _drawingController.MousePosition = ctx.ReadValue<Vector2>();
        }
        #endregion
    }
}