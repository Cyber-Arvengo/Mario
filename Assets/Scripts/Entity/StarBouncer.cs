﻿using System.Collections;
using UnityEngine;

using Fusion;
using NSMB.Utils;

public class StarBouncer : BasicEntity, IPlayerInteractable {

    private static int ANY_GROUND_MASK = -1;

    [SerializeField] private float pulseAmount = 0.2f, pulseSpeed = 0.2f, moveSpeed = 3f, rotationSpeed = 30f, bounceAmount = 4f, deathBoostAmount = 20f, blinkingSpeed = 0.5f, lifespan = 15f;

    public Rigidbody2D body;
    public bool passthrough = true, fast = false;

    private SpriteRenderer sRenderer;
    private Transform graphicTransform;
    private PhysicsEntity physics;
    private BoxCollider2D worldCollider;
    private Animator animator;
    private float pulseEffectCounter;
    private bool canBounce;

    public bool Collected { get; set; }

    [Networked] public NetworkBool IsStationary { get; set; }
    [Networked] public NetworkBool DroppedByPit { get; set; }
    [Networked] public NetworkBool Collectable { get; set; }
    [Networked] public NetworkBool Fast { get; set; }
    [Networked] public TickTimer DespawnTimer { get; set; }

    public void Awake() {
        body = GetComponent<Rigidbody2D>();
        physics = GetComponent<PhysicsEntity>();
        sRenderer = GetComponentInChildren<SpriteRenderer>();
        worldCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
    }

    public void OnBeforeSpawned(byte direction, bool stationary, bool pit) {
        FacingRight = direction >= 2;
        Fast = direction == 0 || direction == 3;
        IsStationary = stationary;
        Collectable = stationary;
        DroppedByPit = pit;

        if (!stationary)
            DespawnTimer = TickTimer.CreateFromSeconds(Runner, lifespan);
    }

    public override void Spawned() {

        graphicTransform = transform.Find("Graphic");

        GameObject trackObject = Instantiate(UIUpdater.Instance.starTrackTemplate, UIUpdater.Instance.starTrackTemplate.transform.parent);
        TrackIcon icon = trackObject.GetComponent<TrackIcon>();
        icon.target = gameObject;
        trackObject.SetActive(true);

        if (IsStationary) {
            //main star
            animator.enabled = true;
            body.isKinematic = true;
            body.velocity = Vector2.zero;
            StartCoroutine(PulseEffect());

            //play star spawn sfx, only IF
            if (GameManager.Instance.musicEnabled)
                GameManager.Instance.sfx.PlayOneShot(Enums.Sounds.World_Star_Spawn.GetClip());

        } else {
            //player dropped star

            trackObject.transform.localScale = new(3f / 4f, 3f / 4f, 1f);
            passthrough = true;
            sRenderer.color = new(1, 1, 1, 0.55f);
            gameObject.layer = Layers.LayerHitsNothing;
            body.velocity = new(moveSpeed * (FacingRight ? 1 : -1) * (fast ? 2f : 1f), deathBoostAmount);

            //death via pit boost
            if (DroppedByPit)
                body.velocity += Vector2.up * 3;

            body.isKinematic = false;
            worldCollider.enabled = true;
        }

        if (ANY_GROUND_MASK == -1)
            ANY_GROUND_MASK = LayerMask.GetMask("Ground", "PassthroughInvalid");
    }

    public void Update() {
        if (IsStationary || (GameManager.Instance?.gameover ?? false))
            return;

        graphicTransform.Rotate(new(0, 0, rotationSpeed * 30 * (FacingRight ? -1 : 1) * Time.deltaTime), Space.Self);
    }

    public override void FixedUpdateNetwork() {
        if (IsStationary)
            return;

        if (GameManager.Instance?.gameover ?? false) {
            body.velocity = Vector2.zero;
            body.isKinematic = true;
            return;
        }

        if (DespawnTimer.Expired(Runner)) {
            Despawn();
            return;
        } else {
            float timeRemaining = DespawnTimer.RemainingTime(Runner) ?? 0;
            sRenderer.enabled = !(timeRemaining < 5 && timeRemaining * 2 % (blinkingSpeed * 2) < blinkingSpeed);
        }

        body.velocity = new(moveSpeed * (FacingRight ? 1 : -1) * (fast ? 2f : 1f), body.velocity.y);

        canBounce |= body.velocity.y < 0;
        Collectable |= body.velocity.y < 0;

        HandleCollision();

        if (passthrough && Collectable && body.velocity.y <= 0 && !Utils.IsAnyTileSolidBetweenWorldBox(body.position + worldCollider.offset, worldCollider.size * transform.lossyScale) && !Physics2D.OverlapBox(body.position, Vector2.one / 3, 0, ANY_GROUND_MASK)) {
            passthrough = false;
            gameObject.layer = Layers.LayerEntity;
            sRenderer.color = Color.white;
        }
        if (!passthrough) {
            if (Utils.IsAnyTileSolidBetweenWorldBox(body.position + worldCollider.offset, worldCollider.size * transform.lossyScale)) {
                gameObject.layer = Layers.LayerHitsNothing;
            } else {
                gameObject.layer = Layers.LayerEntity;
            }
        }

        if (!passthrough && body.position.y < GameManager.Instance.GetLevelMinY())
            Despawn();
    }

    public void InteractWithPlayer(PlayerController player) {
        if (player.IsDead)
            return;

        if (!Collectable || Collected)
            return;

        Collected = true;

        //we can collect
        player.Stars = (byte) Mathf.Min(player.Stars + 1, GameManager.Instance.starRequirement);
        Runner.Despawn(Object, true);

        //game mechanics
        if (IsStationary) {
            //TODO:
            //GameManager.Instance.SendAndExecuteEvent(Enums.NetEventIds.ResetTiles, null, SendOptions.SendReliable);
        }

        GameManager.Instance.CheckForWinner();

        //graphics / fx
        player.PlaySoundEverywhere(Object.HasInputAuthority ? Enums.Sounds.World_Star_Collect_Self : Enums.Sounds.World_Star_Collect_Enemy);
        Instantiate(Resources.Load("Prefabs/Particle/StarCollect"), transform.position, Quaternion.identity);
    }

    private IEnumerator PulseEffect() {
        while (true) {
            pulseEffectCounter += Time.deltaTime;
            float sin = Mathf.Sin(pulseEffectCounter * pulseSpeed) * pulseAmount;
            graphicTransform.localScale = Vector3.one * 3f + new Vector3(sin, sin, 0);

            yield return null;
        }
    }

    private void HandleCollision() {
        physics.UpdateCollisions();

        if (physics.hitLeft || physics.hitRight)
            Turnaround(physics.hitLeft);

        if (physics.onGround && canBounce) {
            body.velocity = new(body.velocity.x, bounceAmount);
            if (physics.hitRoof)
                Despawn();
        }
    }

    public void DisableAnimator() {
        animator.enabled = false;
    }

    public void Despawn() {
        Runner.Despawn(Object, true);
        Instantiate(Resources.Load("Prefabs/Particle/Puff"), transform.position, Quaternion.identity);
    }

    public void Turnaround(bool hitLeft) {
        FacingRight = hitLeft;
        body.velocity = new(moveSpeed * (FacingRight ? 1 : -1), body.velocity.y);
    }

}
