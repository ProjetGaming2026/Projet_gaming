using UnityEngine;

public class BreakerSwitch : MonoBehaviour
{
    [Header("Identite")]
    public int switchIndex;
    public string switchName = "Rouge";
    public Color nameColor = Color.red;

    [Header("Direction correcte")]
    public SwitchDirection correctDirection = SwitchDirection.UP;

    [Header("Animation")]
    public Animator switchAnimator;

    [Header("Interaction")]
    public float interactRadius = 2.5f;
    public KeyCode upKey = KeyCode.E;
    public KeyCode downKey = KeyCode.Q;

    public enum SwitchDirection { UP, DOWN }
    public enum SwitchState { Idle, Up, Down }

    private SwitchState currentState = SwitchState.Idle;
    private bool locked;
    private bool isLooked;
    private Transform player;
    private Camera playerCamera;

    private GUIStyle boxStyle;
    private GUIStyle nameStyle;
    private GUIStyle textStyle;
    private bool stylesInitialized;

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        playerCamera = Camera.main;
        if (switchAnimator == null)
        {
            switchAnimator = GetComponent<Animator>();
        }

        SetState(SwitchState.Idle);

        if (GetComponent<Collider>() == null)
        {
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(0.3f, 0.3f, 0.1f);
        }
    }

    private void Update()
    {
        if (locked || player == null || playerCamera == null)
        {
            return;
        }

        isLooked = false;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > interactRadius)
        {
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactRadius))
        {
            return;
        }

        if (hit.transform != transform && !hit.transform.IsChildOf(transform))
        {
            return;
        }

        isLooked = true;

        if (Input.GetKeyDown(upKey))
        {
            if (currentState != SwitchState.Up)
            {
                SetState(SwitchState.Up);
            }
            else
            {
                SetState(SwitchState.Idle);
            }

            BreakerPanelManager.Instance.OnSwitchChanged(switchIndex, currentState);
            return;
        }

        if (Input.GetKeyDown(downKey))
        {
            if (currentState != SwitchState.Down)
            {
                SetState(SwitchState.Down);
            }
            else
            {
                SetState(SwitchState.Idle);
            }

            BreakerPanelManager.Instance.OnSwitchChanged(switchIndex, currentState);
        }
    }

    private void OnGUI()
    {
        if (!isLooked || locked)
        {
            return;
        }

        EnsureStyles();

        float boxWidth = 420f;
        float boxHeight = 96f;
        float x = (Screen.width - boxWidth) / 2f;
        float y = Screen.height - boxHeight - 40f;

        GUI.Box(new Rect(x, y, boxWidth, boxHeight), string.Empty, boxStyle);
        GUI.Label(new Rect(x, y + 6f, boxWidth, 30f), "Interrupteur " + switchName, nameStyle);

        string upLabel = currentState == SwitchState.Up
            ? "[ E ] Remettre au centre"
            : "[ E ] Basculer vers le haut";

        string downLabel = currentState == SwitchState.Down
            ? "[ Q ] Remettre au centre"
            : "[ Q ] Basculer vers le bas";

        GUI.Label(new Rect(x, y + 38f, boxWidth, 24f), upLabel, textStyle);
        GUI.Label(new Rect(x, y + 62f, boxWidth, 24f), downLabel, textStyle);
    }

    private void EnsureStyles()
    {
        if (stylesInitialized)
        {
            return;
        }

        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTexture(2, 2, new Color(0f, 0f, 0f, 0.8f));

        nameStyle = new GUIStyle(GUI.skin.label);
        nameStyle.fontSize = 20;
        nameStyle.alignment = TextAnchor.MiddleCenter;
        nameStyle.fontStyle = FontStyle.Bold;
        nameStyle.normal.textColor = nameColor;

        textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 16;
        textStyle.alignment = TextAnchor.MiddleCenter;
        textStyle.normal.textColor = Color.white;

        stylesInitialized = true;
    }

    private Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private void SetState(SwitchState state)
    {
        currentState = state;
        if (switchAnimator == null)
        {
            return;
        }

        switch (state)
        {
            case SwitchState.Idle:
                switchAnimator.SetInteger("SwitchState", 0);
                break;
            case SwitchState.Up:
                switchAnimator.SetInteger("SwitchState", 1);
                break;
            case SwitchState.Down:
                switchAnimator.SetInteger("SwitchState", -1);
                break;
        }
    }

    public SwitchState GetState()
    {
        return currentState;
    }

    public void ResetSwitch()
    {
        SetState(SwitchState.Idle);
    }

    public void Lock(bool state)
    {
        locked = state;
        if (locked)
        {
            isLooked = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
