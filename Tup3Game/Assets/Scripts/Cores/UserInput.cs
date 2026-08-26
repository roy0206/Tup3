using System;
using System.Collections.Generic;
using UnityEngine;

public enum KeyPhase
{
    Down,
    Up,
    Held 
}

public enum MouseButton
{
    Left = 0,
    Right = 1,
    Middle = 2
}

public class UserInput : Singleton<UserInput>
{
    private class KeyCallbacks
    {
        public Action Down;
        public Action Up;
        public Action Held;
    }

    private readonly Dictionary<KeyCode, KeyCallbacks> keyCallbacks = new();

    private readonly KeyCallbacks[] mouseCallbacks =
    {
        new KeyCallbacks(), // Left
        new KeyCallbacks(), // Right
        new KeyCallbacks()  // Middle
    };
    
    public Vector2 MousePosition { get; private set; }
    
    public Vector2 MouseDelta { get; private set; }
    
    public Vector2 ScrollDelta { get; private set; }

    private Vector2 prevMousePosition;
    
    private readonly bool[] dragging = new bool[3];
    private readonly Vector2[] dragOrigin = new Vector2[3];

    protected override void OnAwake()
    {
        prevMousePosition = Input.mousePosition;
        MousePosition = prevMousePosition;
    }

    private void Update()
    {
        if (PauseManager.IsPaused || DialogueManager.IsDialogueActive) return;

        UpdateMouse();
        DispatchKeyListeners();
        DispatchMouseListeners();
    }
    
    public bool GetKey(KeyCode key) => Input.GetKey(key);
    public bool GetKeyDown(KeyCode key) => Input.GetKeyDown(key);
    public bool GetKeyUp(KeyCode key) => Input.GetKeyUp(key);
    
    public bool GetMouseButton(MouseButton b) => Input.GetMouseButton((int)b);
    public bool GetMouseButtonDown(MouseButton b) => Input.GetMouseButtonDown((int)b);
    public bool GetMouseButtonUp(MouseButton b) => Input.GetMouseButtonUp((int)b);
    
    public bool IsDragging(MouseButton b) => dragging[(int)b];
    
    public Vector2 GetDragOrigin(MouseButton b) => dragging[(int)b] ? dragOrigin[(int)b] : Vector2.zero;
    
    public Vector2 GetDragDelta(MouseButton b) =>
        dragging[(int)b] ? MousePosition - dragOrigin[(int)b] : Vector2.zero;
    
    public void AddKeyListener(KeyCode key, KeyPhase phase, Action callback)
    {
        if (callback == null) return;
        if (!keyCallbacks.TryGetValue(key, out var cb))
        {
            cb = new KeyCallbacks();
            keyCallbacks[key] = cb;
        }
        AddTo(cb, phase, callback);
    }

    public void RemoveKeyListener(KeyCode key, KeyPhase phase, Action callback)
    {
        if (callback == null) return;
        if (keyCallbacks.TryGetValue(key, out var cb))
            RemoveFrom(cb, phase, callback);
    }
    
    public void AddMouseListener(MouseButton b, KeyPhase phase, Action callback)
    {
        if (callback == null) return;
        AddTo(mouseCallbacks[(int)b], phase, callback);
    }

    public void RemoveMouseListener(MouseButton b, KeyPhase phase, Action callback)
    {
        if (callback == null) return;
        RemoveFrom(mouseCallbacks[(int)b], phase, callback);
    }
    
    private static void AddTo(KeyCallbacks cb, KeyPhase phase, Action callback)
    {
        switch (phase)
        {
            case KeyPhase.Down: cb.Down += callback; break;
            case KeyPhase.Up: cb.Up += callback; break;
            case KeyPhase.Held: cb.Held += callback; break;
        }
    }

    private static void RemoveFrom(KeyCallbacks cb, KeyPhase phase, Action callback)
    {
        switch (phase)
        {
            case KeyPhase.Down: cb.Down -= callback; break;
            case KeyPhase.Up: cb.Up -= callback; break;
            case KeyPhase.Held: cb.Held -= callback; break;
        }
    }

    private void UpdateMouse()
    {
        Vector2 cur = Input.mousePosition;
        MouseDelta = cur - prevMousePosition;
        MousePosition = cur;
        prevMousePosition = cur;
        ScrollDelta = Input.mouseScrollDelta;

        for (int i = 0; i < 3; i++)
        {
            if (Input.GetMouseButtonDown(i))
            {
                dragging[i] = true;
                dragOrigin[i] = cur;
            }
            else if (Input.GetMouseButtonUp(i))
            {
                dragging[i] = false;
            }
        }
    }

    private void DispatchKeyListeners()
    {
        foreach (var pair in keyCallbacks)
        {
            KeyCode key = pair.Key;
            KeyCallbacks cb = pair.Value;

            if (cb.Down != null && Input.GetKeyDown(key)) cb.Down.Invoke();
            if (cb.Up != null && Input.GetKeyUp(key)) cb.Up.Invoke();
            if (cb.Held != null && Input.GetKey(key)) cb.Held.Invoke();
        }
    }

    private void DispatchMouseListeners()
    {
        for (int i = 0; i < 3; i++)
        {
            KeyCallbacks cb = mouseCallbacks[i];
            if (cb.Down != null && Input.GetMouseButtonDown(i)) cb.Down.Invoke();
            if (cb.Up != null && Input.GetMouseButtonUp(i)) cb.Up.Invoke();
            if (cb.Held != null && Input.GetMouseButton(i)) cb.Held.Invoke();
        }
    }
}

/* [파일 노트]
 * Update 첫 줄의 PauseManager.IsPaused 게이트 : 일시정지 중에는 등록된 키/마우스 리스너를
 * 일절 디스패치하지 않는다(예: V 홀드 상호작용으로 보스방 입장 씬 전환이 일어나는 사고 방지).
 * 일시정지 UI 는 uGUI 이벤트(EventSystem)와 PauseManager 자체의 ESC 처리로만 동작하므로
 * 이 게이트의 영향을 받지 않는다.
 */
