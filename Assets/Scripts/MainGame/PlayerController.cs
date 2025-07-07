using Fusion;
using Fusion.Addons.Physics;
using TMPro;
using UnityEngine;

public class PlayerController : NetworkBehaviour, IBeforeUpdate
{
    public bool AcceptAnyInput => PlayerIsAlive && !GameManager.MatchIsOver && !playerChatController.IsTyping;
    
    [SerializeField] private PlayerChatController playerChatController;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private GameObject cam;
    [SerializeField] private float moveSpeed = 6;
    [SerializeField] private float jumpForce = 1000;

    [Header("Grounded Vars")] 
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundDetectionObj;

    [Networked] public TickTimer RespawnTimer { get; private set; }
    [Networked] public NetworkBool PlayerIsAlive { get; private set; }

    [Networked]
    private NetworkString<_8> playerName { get; set; }

    [Networked] private NetworkButtons buttonsPrev { get; set; }
    [Networked] private Vector2 serverNextSpawnPoint { get; set; }
    [Networked] private NetworkBool isGrounded { get; set; }
    [Networked] private TickTimer respawnToNewPointTimer { get; set; }

    private float horizontal;
    private Rigidbody2D rigid;
    private PlayerWeaponController playerWeaponController;
    private PlayerVisualController playerVisualController;
    private PlayerHealthController playerHealthController;
    private ChangeDetector _changeDetector;

    public enum PlayerInputButtons
    {
        None,
        Jump,
        Shoot
    }

    public override void Spawned()
    {
        Runner.SetIsSimulated(Object, true);
        
        rigid = GetComponent<Rigidbody2D>();
        playerWeaponController = GetComponent<PlayerWeaponController>();
        playerVisualController = GetComponent<PlayerVisualController>();
        playerHealthController = GetComponent<PlayerHealthController>();
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState, false);
        
        GlobalManagers.Instance.PlayerSpawnerController.AddToEntry(base.Object.InputAuthority, base.Object);
        
        SetLocalObjects();
        PlayerIsAlive = true;
    }

    private void SetLocalObjects()
    {
        if (Utils.IsLocalPlayer(Object))
        {
            cam.transform.SetParent(null);
            cam.SetActive(true);

            var nickName = GlobalManagers.Instance.NetworkRunnerController.LocalPlayerNickname;
            RpcSetNickName(nickName);
        }
        else
        {
            //If this is not our InputAuthority player (aka a proxy)
            //We want to make sure to set this nrb's InterpolationDataSource to Snapshots
            //As it will automatically set all nrb's to be predicted regardless if it's a proxy or not, as we are doing full physics prediction.
            //And setting it back to snapshots for proxies, will also make sure that lag compensation will work properly + be more cost efficient
            base.Object.RenderSource = RenderSource.Interpolated;
            base.Object.ForceRemoteRenderTimeframe = true;
        }
    }

    // Sends RPC to the HOST (from a client)
    //"sources" define which PEER can send the rpc
    //The RpcTargets defines on which it is executed!
    [Rpc(sources: RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RpcSetNickName(NetworkString<_8> nickName)
    {
        playerName = nickName;
    }

    //For example -
    //if i set on spawned method a name called "banana"
    // and then on fun i change another name which is again "banana"
    private void OnNicknameChanged( NetworkString<_8> nickName)
    {
        SetPlayerNickname(nickName);
    }

    private void SetPlayerNickname(NetworkString<_8> nickName)
    {
        playerNameText.text = nickName + " " + Object.InputAuthority.PlayerId;
    }

    public void KillPlayer()
    {
        const int RESPAWN_AMOUNT = 5;
        if (Runner.IsServer)
        {
            serverNextSpawnPoint = GlobalManagers.Instance.PlayerSpawnerController.GetRandomSpawnPoint();
            respawnToNewPointTimer = TickTimer.CreateFromSeconds(Runner, RESPAWN_AMOUNT - 1);
        }
        
        PlayerIsAlive = false;
        rigid.simulated = false;
        playerVisualController.TriggerDieAnimation();
        RespawnTimer = TickTimer.CreateFromSeconds(Runner, RESPAWN_AMOUNT);
    }

    //Happens before anything else Fusion does, network application, reconlation etc 
    //Called at the start of the Fusion Update loop, before the Fusion simulation loop.
    //It fires before Fusion does ANY work, every screen refresh.
    public void BeforeUpdate()
    {
        //We are the local machine
        if (Utils.IsLocalPlayer(Object) && AcceptAnyInput)
        {
            const string HORIZONTAL = "Horizontal";
            horizontal = Input.GetAxisRaw(HORIZONTAL);
        }
    }

    //FUN
    public override void FixedUpdateNetwork()
    {
        CheckRespawnTimer();

        // will return false if:
        //the client does not have State Authority or Input Authority
        // the requested type of input does not exist in the simulation
        if (Runner.TryGetInputForPlayer<PlayerData>(Object.InputAuthority, out var input))
        {
            if (AcceptAnyInput)
            {
                rigid.linearVelocity = new Vector2(input.HorizontalInput * moveSpeed, rigid.linearVelocity.y);

                CheckJumpInput(input);

                buttonsPrev = input.NetworkButtons;
            }
            else
            {
                rigid.linearVelocity = Vector2.zero;
            }
        }

        playerVisualController.UpdateScaleTransforms(rigid.linearVelocity);
    }

    private void CheckRespawnTimer()
    {
        if (PlayerIsAlive) return;

        //Will only run on the server
        if (respawnToNewPointTimer.Expired(Runner))
        {
            GetComponent<NetworkRigidbody2D>().Teleport(serverNextSpawnPoint);
            respawnToNewPointTimer = TickTimer.None;
        }
        
        if (RespawnTimer.Expired(Runner))
        {
            RespawnTimer = TickTimer.None;
            RespawnPlayer();
        }
    }

    private void RespawnPlayer()
    {
        PlayerIsAlive = true;
        rigid.simulated = true;
        playerVisualController.TriggerRespawnAnimation();
        playerHealthController.ResetHealthAmountToMax();
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this, out var prev, out var current))
        {
            switch (change)
            {
                case nameof(playerName):
                    var reader = GetPropertyReader<NetworkString<_8>>(nameof(playerName));
                    var (oldName, currentName) = reader.Read(prev, current);
                    OnNicknameChanged(currentName);
                    break;
            }
        }
        
        playerVisualController.RendererVisuals(rigid.linearVelocity, playerWeaponController.IsHoldingShootingKey);
    }

    private void CheckJumpInput(PlayerData input)
    {
        var transform1 = groundDetectionObj.transform;
        isGrounded = (bool)Runner.GetPhysicsScene2D().OverlapBox(transform1.position,
            transform1.localScale, 0, groundLayer);

        if (isGrounded)
        {
            var pressed = input.NetworkButtons.GetPressed(buttonsPrev);
            if (pressed.WasPressed(buttonsPrev, PlayerInputButtons.Jump))
            {
                rigid.AddForce(Vector2.up * jumpForce, ForceMode2D.Force);
            }
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        GlobalManagers.Instance.ObjectPoolingManager.RemoveNetworkObjectFromDic(Object);
        Destroy(gameObject);
    }

    public PlayerData GetPlayerNetworkInput()
    {
        PlayerData data = new PlayerData();
        data.HorizontalInput = horizontal;
        data.GunPivotRotation = playerWeaponController.LocalQuaternionPivotRot;
        data.NetworkButtons.Set(PlayerInputButtons.Jump, Input.GetKey(KeyCode.Space));
        data.NetworkButtons.Set(PlayerInputButtons.Shoot, Input.GetButton("Fire1"));
        return data;
    }
}