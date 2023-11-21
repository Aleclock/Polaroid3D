using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// https://www.youtube.com/watch?v=u1Zel20rwOk (https://github.com/UnityTechnologies/open-project-1)
[CreateAssetMenu(fileName = "InputReader", menuName = "SO/Input Reader")]
public class InputReader : ScriptableObject, CustomInputActions.IXRIHeadActions, CustomInputActions.IXRILeftHandCustomActions, CustomInputActions.IXRIRightHandCustomActions
{

    public event UnityAction TriggerLeftHandActivateEvent = delegate{};
    public event UnityAction TriggerLeftHandCancelEvent = delegate{};
    public event UnityAction TriggerRightHandActivateEvent = delegate{};
    public event UnityAction TriggerRightHandCancelEvent = delegate{};
    public event UnityAction GripLeftHandActivateEvent = delegate{};
    public event UnityAction GripLeftHandCanceledEvent = delegate{};
    public event UnityAction<float> GripLeftHandValueEvent = delegate{};
    public event UnityAction GripRightHandActivateEvent = delegate{};
    public event UnityAction GripRightHandCanceledEvent = delegate{};
    public event UnityAction<float> GripRightHandValueEvent = delegate{};
    public event UnityAction<float> TriggerLeftHandValueEvent = delegate{};
    public event UnityAction<float> TriggerRightHandValueEvent = delegate{};

    private CustomInputActions customInputActions;

    public void EnableInputs() 
    {
        if (customInputActions == null)
        {
            customInputActions = new CustomInputActions();
            customInputActions.XRIHead.Enable();
            customInputActions.XRILeftHandCustom.Enable();
            customInputActions.XRIRightHandCustom.Enable();

            customInputActions.XRIHead.SetCallbacks(this);
            customInputActions.XRILeftHandCustom.SetCallbacks(this);
            customInputActions.XRIRightHandCustom.SetCallbacks(this);
        }
    }

    public void DisableInputs()
    {
        customInputActions.XRIHead.Disable();
        customInputActions.XRILeftHandCustom.Disable();
        customInputActions.XRIRightHandCustom.Disable();
    }

    // ------------------------------------------------------------
    // ----- HEAD
    // ------------------------------------------------------------
    public void OnHeadPosition(InputAction.CallbackContext context)
    {

    }

    public void OnHeadRotation(InputAction.CallbackContext context)
    {

    }

    public Vector3 GetHeadPosition()
    {
        return customInputActions.XRIHead.HeadPosition.ReadValue<Vector3>();
    }

    public Quaternion GetHeadRotation()
    {
        return customInputActions.XRIHead.HeadRotation.ReadValue<Quaternion>();
    }

    // ------------------------------------------------------------
    // ----- LEFT HAND
    // ------------------------------------------------------------
    public void OnLeftHandPosition(InputAction.CallbackContext context)
    {

    }

    public void  OnLeftHandRotation(InputAction.CallbackContext context)
    {

    }
    
    public void OnLeftHandTrigger(InputAction.CallbackContext context)
    {
        switch(context.phase)
        {
            case InputActionPhase.Performed:
                TriggerLeftHandActivateEvent.Invoke();
                break;
            case InputActionPhase.Canceled:
                TriggerLeftHandCancelEvent.Invoke();
                break;
            default:
                break;
        }
    }

    public Vector3 GetLeftHandPosition()
    {
        return customInputActions.XRILeftHandCustom.LeftHandPosition.ReadValue<Vector3>();
    }

    public Quaternion GetLeftHandRotation()
    {
        return customInputActions.XRILeftHandCustom.LeftHandRotation.ReadValue<Quaternion>();
    }

    public void OnLeftHandGripValue(InputAction.CallbackContext context)
    {
        float value = context.ReadValue<float>();
        if (value > 0.01 && value < 0.99)
            GripLeftHandValueEvent.Invoke(context.ReadValue<float>());
    }

    public void OnLeftHandTriggerValue(InputAction.CallbackContext context)
    {
        float value = context.ReadValue<float>();
        if (value > 0.01 && value < 0.9)
            TriggerLeftHandValueEvent.Invoke(context.ReadValue<float>());
    }

    public void OnLeftHandGrip(InputAction.CallbackContext context)
    {
        switch(context.phase)
        {
            case InputActionPhase.Performed:
                GripLeftHandActivateEvent.Invoke();
                break;
            case InputActionPhase.Canceled:
                GripLeftHandCanceledEvent.Invoke();
                break;
            default:
                break;
        }
    }

    // ------------------------------------------------------------
    // ----- RIGHT HAND
    // ------------------------------------------------------------

    public void OnRightHandPosition(InputAction.CallbackContext context)
    {

    }

    public void OnRightHandRotation(InputAction.CallbackContext context)
    {

    }

    public Vector3 GetRightHandPosition()
    {
        return customInputActions.XRIRightHandCustom.RightHandPosition.ReadValue<Vector3>();
    }

    public Quaternion GetRightHandRotation()
    {
        return customInputActions.XRIRightHandCustom.RightHandRotation.ReadValue<Quaternion>();
    }

    public void OnRightHandTrigger(InputAction.CallbackContext context)
    {
        switch(context.phase)
        {
            case InputActionPhase.Performed:
                TriggerRightHandActivateEvent.Invoke();
                break;
            case InputActionPhase.Canceled:
                TriggerRightHandCancelEvent.Invoke();
                break;
            default:
                break;
        }
    }

    public void OnRightHandGripValue(InputAction.CallbackContext context)
    {
        float value = context.ReadValue<float>();
        if (value > 0.01 && value < 0.99)
            GripRightHandValueEvent.Invoke(context.ReadValue<float>());
    }

    public void OnRightHandTriggerValue(InputAction.CallbackContext context)
    {
        float value = context.ReadValue<float>();
        if (value > 0.01 && value < 0.99)
            TriggerRightHandValueEvent.Invoke(context.ReadValue<float>());
    }
    
    public void OnRightHandGrip(InputAction.CallbackContext context)
    {
        switch(context.phase)
        {
            case InputActionPhase.Performed:
                GripRightHandActivateEvent.Invoke();
                break;
            case InputActionPhase.Canceled:
                GripLeftHandCanceledEvent.Invoke();
                break;
            default:
                break;
        }
    }

    public void OnLeftHandHapticDevice(InputAction.CallbackContext context)
    {
    
    }

    public void OnLeftHandTrackingState(InputAction.CallbackContext context)
    {
        
    }

    public void OnRightHandHapticDevice(InputAction.CallbackContext context)
    {
    
    }

    public void OnRightHandTrackingState(InputAction.CallbackContext context)
    {
    
    }
}
