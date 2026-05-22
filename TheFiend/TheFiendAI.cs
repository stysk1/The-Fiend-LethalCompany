using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

// NOTE: This type intentionally lives in the GLOBAL namespace. The prebuilt "thefiend"
// AssetBundle's enemy prefab references this script by assembly name + (empty) namespace +
// class name, and binds its public fields by name. Renaming the class, moving it into a
// namespace, or renaming any public field below will silently break the prefab binding.
public class TheFiendAI : EnemyAI
{
    public NetworkVariable<int> StateOfMind = new();
    public NetworkVariable<int> Funky = new(1);

    private Animator animator;

    public GameObject Main;
    public GameObject Neck;
    public GameObject Spine;
    public GameObject LeftHand;
    public GameObject RightHand;
    public MeshRenderer MapDot;
    public SkinnedMeshRenderer skinnedMesh;

    private float OldYScale;

    public System.Random enemyRandom;

    public AudioClip[] audioClips;
    public AudioClip StepClip;
    private AudioSource AS;
    private AudioSource AS2;

    private Vector3 FavSpot;
    private bool ResetNode;
    private bool EatingPlayer;
    private Coroutine rotateCoroutine;

    public NetworkVariable<bool> Seeking = new();
    public NetworkVariable<bool> Invis = new();
    public NetworkVariable<bool> RageMode = new();
    public NetworkVariable<bool> GlobalCD = new();
    public NetworkVariable<bool> StandingMode = new();
    public NetworkVariable<bool> IsDying = new(false);
    public NetworkVariable<bool> LungApparatusWillRage = new(TheFiend.TheFiend.WillRageAfterApparatus.Value);

    public Quaternion OldR;
    public bool Step;

    private Vector3 LastPos;
    private Vector3 Node;
    private int LightTriggerTimes;
    private NavMeshPath path;
    private GameObject Head;
    private GameObject breakerBox;
    private RoundManager roundManager;
    public TimeOfDay timeOfDay;
    private LungProp LungApparatus;
    public GameObject TargetLook;

    // Intentionally hides EnemyAI.Awake() (as the original mod did) — Unity invokes this most-derived
    // Awake; the Fiend does its own setup here and relies on base.Start() for the rest.
    public new void Awake()
    {
        path = new NavMeshPath();
        FavSpot = transform.position;
        Head = Neck.transform.Find("mixamorig:Head").gameObject;
        OldR = Neck.transform.localRotation;
        animator = GetComponent<Animator>();
        animator.Play("Idle");
        AS = GetComponent<AudioSource>();
        AS2 = Spine.GetComponent<AudioSource>();
        try
        {
            breakerBox = Object.FindObjectOfType<BreakerBox>().gameObject;
        }
        catch
        {
            breakerBox = null;
        }
        roundManager = Object.FindObjectOfType<RoundManager>();
        timeOfDay = Object.FindObjectOfType<TimeOfDay>();
        MapDot.material.color = Color.red;
        AudioMixerGroup outputAudioMixerGroup = SoundManager.Instance.diageticMixer.FindMatchingGroups("SFX")[0];
        AS.outputAudioMixerGroup = outputAudioMixerGroup;
    }

    public override void Start()
    {
        base.Start();
        OldYScale = Main.transform.position.y;
        enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
        AS.clip = audioClips[0];
        AS.loop = true;
        AS.Play();
        foreach (LungProp lung in Object.FindObjectsOfType<LungProp>())
        {
            if (lung.isLungDocked)
            {
                LungApparatus = lung;
                break;
            }
        }
    }

    public void FixedUpdate()
    {
        if (Step && !Invis.Value)
        {
            AS2.pitch = Random.Range(0.6f, 1f);
            AS2.PlayOneShot(StepClip);
        }
    }

    public void LateUpdate()
    {
        if (TargetLook != null)
        {
            Neck.transform.LookAt(TargetLook.transform, Vector3.up);
        }
        else
        {
            Neck.transform.localRotation = OldR;
        }
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (stunnedByPlayer != null)
        {
            AS.clip = audioClips[2];
            AS.loop = false;
            AS.Play();
            IsDying.Value = true;
            Object.Destroy(gameObject, 4f);
            stunnedByPlayer = null;
            skinnedMesh.enabled = false;
        }
        if (IsDying.Value)
        {
            SyncPositionToClients();
            return;
        }

        if (timeOfDay.hour >= 15)
        {
            Funky.Value = 2;
        }
        if (Seeking.Value)
        {
            AS.volume = 0f;
        }
        else if (StateOfMind.Value != 3)
        {
            AS.volume = TheFiend.TheFiend.Volume.Value;
        }
        skinnedMesh.enabled = !Invis.Value;

        int funky = Mathf.Max(1, Funky.Value);
        if (Random.Range(1, 10000) == 1)
        {
            TeleportServerRpc();
        }
        if (Random.Range(1, 10000 / funky) == 1 && StateOfMind.Value == 3)
        {
            HideOnCellingServerRpc();
        }
        if (Random.Range(1, 10000 / funky) == 1 && !Seeking.Value)
        {
            ToggleSeekingServerRpc();
        }

        if (GlobalCD.Value)
        {
            SyncPositionToClients();
            return;
        }

        if (StateOfMind.Value < 3)
        {
            StateOfMind.Value = 0;
        }
        if (breakerBox != null && !Seeking.Value)
        {
            GameObject mesh = breakerBox.transform.Find("Mesh").gameObject;
            if (Vector3.Distance(Main.transform.position, mesh.transform.position) <= 5f)
            {
                if (Physics.Raycast(Neck.transform.position, mesh.transform.position - Neck.transform.position, out RaycastHit hit, float.PositiveInfinity, ~LayerMask.GetMask("Enemies")) && Vector3.Distance(hit.point, mesh.transform.position) < 2f)
                {
                    StateOfMind.Value = 4;
                    TargetLook = mesh;
                    if (Vector3.Distance(Main.transform.position, mesh.transform.position) <= 2f)
                    {
                        BreakerBoxBreakServerRpc();
                    }
                }
                else
                {
                    StateOfMind.Value = 0;
                }
            }
        }
        if (TargetClosestPlayer(100f, false, 70f))
        {
            TargetLook = targetPlayer.gameObject;
            if (targetPlayer.currentlyHeldObject != null && targetPlayer.currentlyHeldObject.gameObject.name.Contains("FlashlightItem"))
            {
                GameObject lightObject = targetPlayer.currentlyHeldObject.gameObject.transform.Find("Light").gameObject;
                Light light = lightObject.GetComponent<Light>();
                if (light.enabled && Vector3.Distance(Head.transform.position, lightObject.transform.position) <= 2.5f)
                {
                    FearedServerRpc(false, true);
                    LightTriggerTimes++;
                }
            }
        }
        if (StateOfMind.Value == 3 && !GlobalCD.Value && !Seeking.Value && TargetClosestPlayer(100f, false, 70f))
        {
            TargetLook = targetPlayer.gameObject;
            if (Vector3.Distance(transform.position, targetPlayer.gameObject.transform.position) <= 4f)
            {
                HideOnCellingServerRpc();
            }
        }
        if (!EatingPlayer && StateOfMind.Value <= 2 && !GlobalCD.Value && !StandingMode.Value)
        {
            if (TargetClosestPlayer(100f, false, 70f))
            {
                TargetLook = targetPlayer.gameObject;
                ResetNode = true;
                if (agent.remainingDistance > 10f && !RageMode.Value)
                {
                    OldYScale = Main.transform.position.y;
                    if (!Seeking.Value)
                    {
                        StateOfMind.Value = 1;
                        agent.speed = 3 + (Funky.Value - 1);
                        animator.Play("Walk");
                        if (CheckDoor())
                        {
                            if (Random.Range(1, 100) == 1 && StateOfMind.Value != 3)
                            {
                                HideOnCellingServerRpc();
                            }
                        }
                        else if (Random.Range(1, 1000) == 1 && StateOfMind.Value != 3)
                        {
                            HideOnCellingServerRpc();
                        }
                    }
                    else
                    {
                        agent.speed = 1f;
                        animator.Play("Seeking");
                        BreakDoorServerRpc();
                    }
                    if (Random.Range(1, TheFiend.TheFiend.FlickerRngChance.Value) == 1)
                    {
                        roundManager.FlickerLights(true, true);
                    }
                }
                else if (!Seeking.Value)
                {
                    StateOfMind.Value = 2;
                    if (CheckLineOfSightForPlayer(45f, 60, -1) != null)
                    {
                        targetPlayer.JumpToFearLevel(0.9f, true);
                    }
                    if (!RageMode.Value)
                    {
                        agent.speed = 9 * Funky.Value;
                    }
                    else
                    {
                        agent.speed = 20 * Funky.Value;
                    }
                    animator.Play("Run");
                    BreakDoorServerRpc();
                }
                SetDestinationToPosition(targetPlayer.transform.position, false);
                if (Seeking.Value)
                {
                    PlayerControllerB[] players = Object.FindObjectsOfType<PlayerControllerB>();
                    foreach (PlayerControllerB player in players)
                    {
                        if (player.HasLineOfSightToPosition(Neck.transform.position, 45f, 60, -1f))
                        {
                            ToggleSeekingServerRpc();
                            FearedServerRpc(true);
                            break;
                        }
                    }
                }
            }
            else
            {
                agent.speed = 3f;
                if (ResetNode)
                {
                    WonderVectorServerRpc(60f);
                    ResetNode = false;
                }
                if (Node != Vector3.zero)
                {
                    SetDestinationToPosition(Node, false);
                }
                else
                {
                    ResetNode = true;
                }
                if (agent.remainingDistance == 0f)
                {
                    ResetNode = true;
                }
                if (Random.Range(1, 100) == 1)
                {
                    ResetNode = true;
                }
                TargetLook = null;
            }
            if (!GlobalCD.Value)
            {
                if (agent.remainingDistance == 0f && StateOfMind.Value == 0 && !RageMode.Value)
                {
                    animator.Play("Idle");
                }
                else if (!Seeking.Value && StateOfMind.Value == 1)
                {
                    animator.Play("Walk");
                }
            }
        }
        if (StateOfMind.Value == 4 && TargetLook != null)
        {
            animator.Play("Walk");
            SetDestinationToPosition(TargetLook.transform.position, false);
        }
        if (LungApparatus != null && LungApparatusWillRage.Value && !Invis.Value && !LungApparatus.isLungDocked)
        {
            LungApparatus.transform.Find("Point Light").gameObject.GetComponent<Light>().color = Color.red;
            LungApparatus.scrapValue = 300;
            LungApparatus = null;
            StartCoroutine(Rage());
        }

        SyncPositionToClients();
    }

    [ServerRpc]
    public void ToggleSeekingServerRpc()
    {
        Seeking.Value = !Seeking.Value;
    }

    private void OnTriggerStay(Collider collision)
    {
        PlayerControllerB player = collision.gameObject.GetComponent<PlayerControllerB>();
        if (player != null && StateOfMind.Value != 3 && !GlobalCD.Value && !Invis.Value && !IsDying.Value && !EatingPlayer && Vector3.Distance(transform.position, collision.gameObject.transform.position) < 4f)
        {
            GrabServerRpc(player);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SceamServerRpc()
    {
        AS.Stop();
        AS.clip = audioClips[Random.Range(1, 2)];
        AS.loop = false;
        AS.Play();
        SceamClientRpc();
    }

    [ClientRpc]
    public void SceamClientRpc()
    {
        AS.Stop();
        AS.clip = audioClips[Random.Range(1, 2)];
        AS.loop = false;
        AS.Play();
    }

    [ServerRpc(RequireOwnership = false)]
    public void IdleSoundServerRpc()
    {
        AS.Stop();
        AS.clip = audioClips[0];
        AS.loop = true;
        AS.Play();
        IdleSoundClientRpc();
    }

    [ClientRpc]
    public void IdleSoundClientRpc()
    {
        AS.Stop();
        AS.clip = audioClips[0];
        AS.loop = true;
        AS.Play();
    }

    [ServerRpc(RequireOwnership = false)]
    public void GrabServerRpc(NetworkBehaviourReference PlayerControllerBRef)
    {
        if (PlayerControllerBRef.TryGet(out PlayerControllerB player))
        {
            GrabClientRpc(player.gameObject);
        }
    }

    [ClientRpc]
    public void GrabClientRpc(NetworkObjectReference networkObject)
    {
        if (networkObject.TryGet(out NetworkObject netObj))
        {
            StartCoroutine(Grabbing(netObj.gameObject));
        }
    }

    public IEnumerator Grabbing(GameObject Player)
    {
        if (EatingPlayer || Player == null)
        {
            yield break;
        }
        PlayerControllerB PCB = Player.GetComponent<PlayerControllerB>();
        if (PCB.health <= 50)
        {
            EatingPlayer = true;
            TargetLook = null;
            agent.speed = 0f;
            SetDestinationToPosition(agent.transform.position, false);
            float oldspeed = PCB.movementSpeed;
            PCB.movementSpeed = 0f;
            animator.Play("Grab");
            transform.LookAt(Player.transform.position, Vector3.up);
            rotateCoroutine = StartCoroutine(RotatePlayerToMe(PCB));
            SceamServerRpc();
            yield return new WaitForSeconds(1.7f);
            PCB.KillPlayer(Main.transform.forward * 30f, true, (CauseOfDeath)6, 1, default);
            if (PCB.IsOwner)
            {
                PCB.movementSpeed = oldspeed;
            }
            if (rotateCoroutine != null)
            {
                StopCoroutine(rotateCoroutine);
                rotateCoroutine = null;
            }
            yield return new WaitForSeconds(1f);
            IdleSoundServerRpc();
            animator.Play("Idle");
            yield return new WaitForSeconds(3f);
            yield return new WaitForSeconds(2f);
            RageMode.Value = false;
            if (Random.Range(1, 30) == 1)
            {
                HideOnCellingServerRpc();
            }
            EatingPlayer = false;
        }
        else
        {
            PCB.DamagePlayer(50, true, true, (CauseOfDeath)0, 0, false, default);
            PCB.externalForceAutoFade += Main.transform.forward * 30f;
            animator.Play("Craw");
            GlobalCD.Value = true;
            PCB.movementAudio.PlayOneShot(audioClips[6], 1f);
            StartCooldown(1f);
        }
    }

    private IEnumerator RotatePlayerToMe(PlayerControllerB PCB)
    {
        if (PCB != null)
        {
            Vector3 Position = transform.position - PCB.gameObject.transform.position;
            while (PCB != null && PCB.health > 0)
            {
                PlayerSmoothLookAt(Position, PCB);
                yield return null;
            }
        }
    }

    private void PlayerSmoothLookAt(Vector3 newDirection, PlayerControllerB PCB)
    {
        PCB.gameObject.transform.rotation = Quaternion.Lerp(PCB.gameObject.transform.rotation, Quaternion.LookRotation(newDirection), Time.deltaTime * 5f);
    }

    [ServerRpc(RequireOwnership = false)]
    public void HideOnCellingServerRpc()
    {
        if (StateOfMind.Value != 3)
        {
            if (StateOfMind.Value <= 3)
            {
                StateOfMind.Value = 3;
                LastPos = Main.transform.position;
                OldYScale = Main.transform.position.y;
                Physics.Raycast(Main.transform.position, transform.TransformDirection(Vector3.up), out RaycastHit hit, float.PositiveInfinity, ~LayerMask.GetMask("Enemies"));
                animator.Play("Hide");
                AS.Stop();
                agent.speed = 0f;
                SetYLevelClientRpc(hit.point.y);
                MapDot.enabled = false;
            }
        }
        else if (!StandingMode.Value)
        {
            StartCoroutine(Stand());
        }
    }

    [ClientRpc]
    public void SetYLevelClientRpc(float y)
    {
        Main.transform.position = new Vector3(Main.transform.position.x, y, Main.transform.position.z);
    }

    public IEnumerator Stand()
    {
        StandingMode.Value = true;
        MapDot.enabled = true;
        Rigidbody rig = Main.AddComponent<Rigidbody>();
        rig.detectCollisions = false;
        while (Vector3.Distance(Main.transform.position, LastPos) > 1.5f)
        {
            yield return null;
        }
        Object.Destroy(rig);
        animator.Play("UnHide");
        yield return new WaitForSeconds(0.2f);
        SetYLevelClientRpc(OldYScale);
        animator.Play("Idle");
        SceamServerRpc();
        yield return new WaitForSeconds(2f);
        IdleSoundServerRpc();
        BreakDoorServerRpc();
        StateOfMind.Value = 1;
        StandingMode.Value = false;
    }

    [ServerRpc]
    public void BreakDoorServerRpc()
    {
        try
        {
            DoorLock[] doors = Object.FindObjectsOfType<DoorLock>();
            foreach (DoorLock door in doors)
            {
                GameObject doorObject = door.transform.parent.parent.parent.gameObject;
                if (doorObject.GetComponent<Rigidbody>() == null && Vector3.Distance(transform.position, doorObject.transform.position) <= 4f)
                {
                    NetworkObjectReference netObjRef = doorObject;
                    Vector3 direction = targetPlayer.transform.position - transform.position;
                    BashDoorClientRpc(netObjRef, direction.normalized * 20f);
                }
            }
        }
        catch
        {
        }
    }

    [ClientRpc]
    public void BashDoorClientRpc(NetworkObjectReference netObjRef, Vector3 Position)
    {
        if (netObjRef.TryGet(out NetworkObject netObj))
        {
            GameObject doorObject = netObj.gameObject;
            Rigidbody rb = doorObject.AddComponent<Rigidbody>();
            AudioSource audio = doorObject.AddComponent<AudioSource>();
            audio.spatialBlend = 1f;
            audio.maxDistance = 60f;
            audio.rolloffMode = AudioRolloffMode.Linear;
            audio.volume = 3f;
            StartCoroutine(TurnOffC(rb, 0.12f));
            rb.AddForce(Position, ForceMode.Impulse);
            audio.PlayOneShot(audioClips[3]);
        }
    }

    public bool CheckDoor()
    {
        DoorLock[] doors = Object.FindObjectsOfType<DoorLock>();
        foreach (DoorLock door in doors)
        {
            GameObject doorObject = door.transform.parent.parent.gameObject;
            if (Vector3.Distance(transform.position, doorObject.transform.position) <= 4f)
            {
                return true;
            }
        }
        return false;
    }

    private IEnumerator TurnOffC(Rigidbody rigidbody, float time)
    {
        rigidbody.detectCollisions = false;
        yield return new WaitForSeconds(time);
        rigidbody.detectCollisions = true;
        Object.Destroy(rigidbody.gameObject, 5f);
    }

    [ServerRpc(RequireOwnership = false)]
    public void FearedServerRpc(bool TempRage, bool uselight = false)
    {
        if (EatingPlayer || StandingMode.Value)
        {
            return;
        }
        GlobalCD.Value = true;
        FearedClientRpc();
        StartCoroutine(CD(5f));
        float tempRage = 3f;
        if (uselight)
        {
            tempRage = LightTriggerTimes * 2;
        }
        if (TempRage)
        {
            StartCoroutine(SetTempRage(tempRage));
        }
    }

    public IEnumerator Rage()
    {
        GlobalCD.Value = true;
        yield return new WaitForSeconds(0.2f);
        animator.Play("Rage");
        PlayerControllerB[] players = Object.FindObjectsOfType<PlayerControllerB>();
        foreach (PlayerControllerB player in players)
        {
            player.JumpToFearLevel(0.9f, true);
        }
        AS.maxDistance = 500f;
        AS.Stop();
        AS.clip = audioClips[5];
        AS.loop = false;
        AS.Play();
        yield return new WaitForSeconds(9f);
        ToggleRageServerRpc(true);
        AS.maxDistance = 30f;
        GlobalCD.Value = false;
        yield return new WaitForSeconds(20f);
        ToggleRageServerRpc(false);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleRageServerRpc(bool TheRageValue)
    {
        RageMode.Value = TheRageValue;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TeleportServerRpc()
    {
        if (Invis.Value)
        {
            return;
        }
        Invis.Value = true;
        List<PlayerControllerB> insidePlayers = new List<PlayerControllerB>();
        PlayerControllerB[] players = Object.FindObjectsOfType<PlayerControllerB>();
        foreach (PlayerControllerB player in players)
        {
            if (player.isInsideFactory)
            {
                insidePlayers.Add(player);
            }
        }
        if (insidePlayers.Count > 0)
        {
            transform.position = insidePlayers[Random.Range(1, insidePlayers.Count)].gameObject.transform.position;
        }
        GlobalCD.Value = true;
        StartCoroutine(CD(25f, true));
    }

    [ClientRpc]
    public void FearedClientRpc()
    {
        animator.Play("CoverFace");
        agent.speed = 0f;
        AS.Stop();
        AS.clip = audioClips[4];
        AS.loop = false;
        AS.Play();
    }

    public void StartCooldown(float time, bool UnInvis = false)
    {
        StartCoroutine(CD(time, UnInvis));
    }

    private IEnumerator CD(float time, bool UnInvis = false)
    {
        agent.speed = 0f;
        yield return new WaitForSeconds(time);
        GlobalCD.Value = false;
        if (UnInvis)
        {
            Invis.Value = false;
        }
    }

    private IEnumerator StateMindCD(float time, int typenow)
    {
        yield return new WaitForSeconds(time);
        StateOfMind.Value = typenow;
    }

    private IEnumerator SetTempRage(float time)
    {
        ToggleRageServerRpc(true);
        yield return new WaitForSeconds(time);
        ToggleRageServerRpc(false);
    }

    [ServerRpc(RequireOwnership = false)]
    public void WonderVectorServerRpc(float Range)
    {
        Vector3 candidate = transform.position + new Vector3(Random.Range(0f - Range, Range), 0f, Random.Range(0f - Range, Range));
        if (agent.CalculatePath(candidate, path))
        {
            Node = candidate;
        }
        else
        {
            Node = Vector3.zero;
        }
    }

    [ServerRpc]
    public void BreakerBoxBreakServerRpc()
    {
        if (breakerBox.GetComponent<Rigidbody>() == null)
        {
            BreakerBoxBreakClientRpc(breakerBox);
        }
    }

    [ClientRpc]
    public void BreakerBoxBreakClientRpc(NetworkObjectReference networkObjectReference)
    {
        if (networkObjectReference.TryGet(out NetworkObject netObj))
        {
            GameObject boxObject = netObj.gameObject;
            boxObject.transform.Find("Mesh").Find("PowerBoxDoor").gameObject.AddComponent<Rigidbody>();
            Rigidbody rb = boxObject.AddComponent<Rigidbody>();
            StartCoroutine(TurnOffC(rb, 0.1f));
            Vector3 direction = Neck.transform.position - transform.position;
            rb.AddForce(direction.normalized * 15f, ForceMode.Impulse);
            boxObject.GetComponent<AudioSource>().PlayOneShot(audioClips[3]);
            Object.Destroy(boxObject, 5f);
            roundManager.PowerSwitchOffClientRpc();
            StartCoroutine(StateMindCD(1f, 0));
            animator.Play("Grab");
        }
    }
}
