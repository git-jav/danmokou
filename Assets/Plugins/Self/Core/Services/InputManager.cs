﻿using System;
using System.Linq;
using UnityEngine;
using KC = UnityEngine.KeyCode;

public enum InputTriggerMethod {
    ONCE,
    ONCE_TOGGLE,
    PERSISTENT
}

public readonly struct InputChecker {
    public readonly Func<bool> checker;
    public readonly string keyDescr;

    public InputChecker(Func<bool> f, string k) {
        checker = f;
        keyDescr = k;
    }
    
    public InputChecker Or(InputChecker other) => 
        new InputChecker(checker.Or(other.checker), $"{keyDescr} or {other.keyDescr}");
}

public abstract class InputChecker2 {
    public abstract bool Check(InputTriggerMethod method);
}
public class InputHandler {
    private bool refractory;
    private readonly InputTriggerMethod trigger;
    private bool toggledValue;
    public bool Active { get; private set; }

    public bool ClaimActive() {
        if (Active) {
            Active = false;
            return true;
        }
        return false;
    }
    public InputChecker checker;
    public string Desc => checker.keyDescr;

    private InputHandler(InputTriggerMethod method, InputChecker check) {
        refractory = false;
        trigger = method;
        checker = check;
    }
    
    public static InputHandler Toggle(InputChecker check) => new InputHandler(InputTriggerMethod.ONCE_TOGGLE, check);
    public static InputHandler Hold(InputChecker check) => new InputHandler(InputTriggerMethod.PERSISTENT, check);
    public static InputHandler Trigger(InputChecker check) => new InputHandler(InputTriggerMethod.ONCE, check);

    public void Update() {
        var isActive = checker.checker();
        if (!refractory && isActive) {
            refractory = trigger == InputTriggerMethod.ONCE || trigger == InputTriggerMethod.ONCE_TOGGLE;
            if (trigger == InputTriggerMethod.ONCE_TOGGLE) Active = toggledValue = !toggledValue;
            else Active = true;
        } else {
            if (refractory && !isActive) refractory = false;
            Active = (trigger == InputTriggerMethod.ONCE_TOGGLE) ? toggledValue : false;
        }
    }
}
public static class InputManager {
    private const string aHoriz = "Horizontal";
    private const string aVert = "Vertical";
    private const string aCRightX = "ControllerRightX";
    private const string aCRightY = "ControllerRightY";
    private const string aCDPadX = "DPadX";
    private const string aCDPadY = "DPadY";
    private const string aCLeftTrigger = "ControllerLTrigger";
    private const string aCRightTrigger = "ControllerRTrigger";

    private const KC cLeftShoulder = KC.JoystickButton4;
    private const KC cRightShoulder = KC.JoystickButton5;
    private const KC cA = KC.JoystickButton0;
    private const KC cB = KC.JoystickButton1;
    private const KC cX = KC.JoystickButton2;
    private const KC cY = KC.JoystickButton3;
    private const KC cSelect = KC.JoystickButton6;
    private const KC cStart = KC.JoystickButton7;
    private static InputChecker Key(KC key) => new InputChecker(() => Input.GetKey(key), key.ToString());
    private static InputChecker AxisL0(string axis) => new InputChecker(() => Input.GetAxisRaw(axis) < -0.1f, axis);
    private static InputChecker AxisG0(string axis) => new InputChecker(() => Input.GetAxisRaw(axis) > 0.1f, axis);
    
    
    //public static readonly InputHandler FocusToggle = InputHandler.Toggle(Key(KC.Space).Or(Key(cRightShoulder)));
    public static readonly InputHandler FocusHold = InputHandler.Hold(Key(KC.LeftShift).Or(AxisG0(aCRightTrigger)));
    public static readonly InputHandler AimLeft = InputHandler.Trigger(Key(KC.A).Or(AxisL0(aCRightX)));
    public static readonly InputHandler AimRight = InputHandler.Trigger(Key(KC.D).Or(AxisG0(aCRightX)));
    public static readonly InputHandler AimUp = InputHandler.Trigger(Key(KC.W).Or(AxisG0(aCRightY)));
    public static readonly InputHandler AimDown = InputHandler.Trigger(Key(KC.S).Or(AxisL0(aCRightY)));
    public static readonly InputHandler ShootToggle = InputHandler.Toggle(Key(KC.X).Or(Key(cLeftShoulder)));
    public static readonly InputHandler ShootHold = InputHandler.Hold(Key(KC.Z).Or(AxisG0(aCLeftTrigger)));
    
    public static readonly InputHandler UILeft = InputHandler.Trigger(AxisL0(aHoriz).Or(AxisL0(aCDPadX)));
    public static readonly InputHandler UIRight = InputHandler.Trigger(AxisG0(aHoriz).Or(AxisG0(aCDPadX)));
    public static readonly InputHandler UIUp = InputHandler.Trigger(AxisG0(aVert).Or(AxisG0(aCDPadY)));
    public static readonly InputHandler UIDown = InputHandler.Trigger(AxisL0(aVert).Or(AxisL0(aCDPadY)));
    
    public static readonly InputHandler UIConfirm = InputHandler.Trigger(Key(KC.Z).Or(Key(cA)));
    public static readonly InputHandler UIBack = InputHandler.Trigger(Key(KC.X).Or(Key(cB)));
    public static readonly InputHandler UISkipDialogue = InputHandler.Trigger(Key(KC.LeftControl));
    
    public static readonly InputHandler Pause = InputHandler.Trigger(Key(KC.Escape).Or(Key(cStart)));
    

    private static readonly InputHandler[] Updaters = {
        //FocusToggle, 
        FocusHold, AimLeft, AimRight, AimUp, AimDown, ShootToggle, ShootHold,
        UIDown, UIUp, UILeft, UIRight, UIConfirm, UIBack, UISkipDialogue, Pause
    };

    //TODO adding joystick support is not too hard.
    //AxisRaw already includes joystick movement.
    //And you can access joystick keys via eg. KeyCode.
    //The order on my stick is:
    // 2 3 5 4
    // 0 1 z- z+

    private static KeyCode editorReloadHook = KeyCode.R;
    public static float HorizontalSpeed => Input.GetAxisRaw(aHoriz) + Input.GetAxisRaw(aCDPadX);
        /*if (Input.GetKey(left)) {
            return -1;
        } else if (Input.GetKey(right)) {
            return 1;
        }
        return 0;*/
    public static float VerticalSpeed => Input.GetAxisRaw(aVert) + Input.GetAxisRaw(aCDPadY);
        /*if (Input.GetKey(up)) {
            return 1;
        } else if (Input.GetKey(down)) {
            return -1;
        }
        return 0;*/
        

    //Called by GameManagement
    public static void OncePerFrameToggleControls() {
        foreach (var u in Updaters) u.Update();
        //Debug.Log(UIDown.Active);
/*
        foreach (var v in Enum.GetValues(typeof(KeyCode)).Cast<KeyCode>()) {
            if (Input.GetKey(v)) Debug.Log($"Keypress {v}");
        }*/
        //foreach (var axis in new[] {"ControllerRTrigger", "ControllerLTrigger"}) Debug.Log($"Axis {axis}: {Input.GetAxis(axis)}");

    }

    public static bool IsFocus => FocusHold.Active;
    public static ShootDirection? FiringDir { get {
        if (AimUp.Active) return ShootDirection.UP;
        if (AimRight.Active) return ShootDirection.RIGHT;
        if (AimLeft.Active) return ShootDirection.LEFT;
        if (AimDown.Active) return ShootDirection.DOWN;
        return null;
    }}
    public static float? FiringAngle => FiringDir?.ToAngle();

    public static bool IsFiring => ShootHold.Active || ShootToggle.Active;

    public static bool EditorReloadActivated() {
        return Input.GetKeyDown(editorReloadHook);
    }

}
