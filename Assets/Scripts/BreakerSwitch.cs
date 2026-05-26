using UnityEngine;

public class BreakerSwitch : MonoBehaviour
{
    [Header("Identité")]
    public int switchIndex;
    public string switchName = "Red";
    public Color nameColor = Color.red;

    [Header("Direction correcte")]
    public SwitchDirection correctDirection = SwitchDirection.UP;

    [Header("Animation")]
    public Animator switchAnimator;

    [Header("Interaction")]
    public float interactRadius = 2.5f; // Distance max pour regarder le switch
    public KeyCode upKey   = KeyCode.E;
    public KeyCode downKey = KeyCode.Q;

    public enum SwitchDirection { UP, DOWN }
    public enum SwitchState     { Idle, Up, Down }

    private SwitchState currentState = SwitchState.Idle;
    private bool locked   = false;
    private bool isLooked = false; // Le joueur regarde CE switch
    private Transform player;
    private Camera playerCamera;

    private GUIStyle boxStyle;
    private GUIStyle nameStyle;
    private GUIStyle textStyle;
    private bool stylesInitialized = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerCamera = Camera.main;
        if (switchAnimator == null)
            switchAnimator = GetComponent<Animator>();
        SetState(SwitchState.Idle);

        // Assurer qu'il y a un collider sur ce switch pour le Raycast
        if (GetComponent<Collider>() == null)
        {
            BoxCollider bc = gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(0.3f, 0.3f, 0.1f);
        }
    }

    void Update()
    {
        if (locked) return;

        isLooked = false;

        // Raycast depuis le centre de la caméra (là où le joueur regarde)
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= interactRadius)
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactRadius))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    isLooked = true;

                    // Toggle UP
                    if (Input.GetKeyDown(upKey))
                    {
                        if (currentState != SwitchState.Up)
                            SetState(SwitchState.Up);
                        else
                            SetState(SwitchState.Idle);
                        BreakerPanelManager.Instance.OnSwitchChanged(switchIndex, currentState);
                    }

                    // Toggle DOWN
                    else if (Input.GetKeyDown(downKey))
                    {
                        if (currentState != SwitchState.Down)
                            SetState(SwitchState.Down);
                        else
                            SetState(SwitchState.Idle);
                        BreakerPanelManager.Instance.OnSwitchChanged(switchIndex, currentState);
                    }
                }
            }
        }
    }

    void OnGUI()
    {
        if (!isLooked || locked) return;

        if (!stylesInitialized)
        {
            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTexture(2, 2, new Color(0f, 0f, 0f, 0.80f));

            nameStyle = new GUIStyle(GUI.skin.label);
            nameStyle.fontSize  = 20;
            nameStyle.alignment = TextAnchor.MiddleCenter;
            nameStyle.fontStyle = FontStyle.Bold;
            nameStyle.normal.textColor = nameColor;

            textStyle = new GUIStyle(GUI.skin.label);
            textStyle.fontSize  = 16;
            textStyle.alignment = TextAnchor.MiddleCenter;
            textStyle.normal.textColor = Color.white;

            stylesInitialized = true;
        }

        float boxW = 380f;
        float boxH = 90f;
        float x = (Screen.width  - boxW) / 2f;
        float y =  Screen.height - boxH - 40f;

        GUI.Box(new Rect(x, y, boxW, boxH), "", boxStyle);

        GUI.Label(new Rect(x, y + 5f, boxW, 30f),
                  $"— {switchName} Switch —", nameStyle);

        string upLabel   = currentState == SwitchState.Up   
            ? "[ E ] Revenir au milieu ↕" 
            : "[ E ] Pousser vers le HAUT ⬆";
        string downLabel = currentState == SwitchState.Down 
            ? "[ Q ] Revenir au milieu ↕" 
            : "[ Q ] Pousser vers le BAS ⬇";

        GUI.Label(new Rect(x, y + 35f, boxW, 25f), upLabel,   textStyle);
        GUI.Label(new Rect(x, y + 60f, boxW, 25f), downLabel, textStyle);
    }

    Texture2D MakeTexture(int w, int h, Color col)
    {
        Color[] pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = col;
        Texture2D tex = new Texture2D(w, h);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    void SetState(SwitchState state)
    {
        currentState = state;
        if (switchAnimator == null) return;
        switch (state)
        {
            case SwitchState.Idle: switchAnimator.SetInteger("SwitchState", 0);  break;
            case SwitchState.Up:   switchAnimator.SetInteger("SwitchState", 1);  break;
            case SwitchState.Down: switchAnimator.SetInteger("SwitchState", -1); break;
        }
    }

    public SwitchState GetState() => currentState;

    public void ResetSwitch()
    {
        SetState(SwitchState.Idle);
    }

    public void Lock(bool state)
    {
        locked = state;
        if (locked) isLooked = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}