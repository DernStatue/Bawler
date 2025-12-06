using UnityEngine;
using System.Collections;
using TMPro;

public class GunSystem : MonoBehaviour
{
    [Header("Gun Stats")]
    public int damage = 25;
    public float timeBetweenShooting = 0.25f;
    public float spread = 0.1f;
    public float range = 100f;
    public float reloadTime = 2f;
    public float timeBetweenShots = 0.1f;
    public int magazineSize = 30;
    public int bulletsPerTap = 1;
    public bool allowButtonHold = true;

    [Header("Weapon State")]
    [SerializeField] private int bulletsLeft;
    [SerializeField] private int bulletsShot;
    [SerializeField] private bool shooting;
    [SerializeField] private bool readyToShoot = true;
    [SerializeField] private bool reloading;
    [SerializeField] private bool isAiming;

    [Header("References")]
    public Camera fpsCam;
    public Transform attackPoint;
    public LayerMask whatIsEnemy;
    public PlayerMovement playerMovement;

    [Header("Graphics")]
    public GameObject muzzleFlash;
    public GameObject bulletHoleGraphic;
    public CamShake camShake;
    public float camShakeMagnitude = 0.5f;
    public float camShakeDuration = 0.1f;
    public TextMeshProUGUI ammoText;

    [Header("Bullet Trail System")]
    public GameObject bulletTrailPrefab;
    public float trailLifetime = 0.3f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip gunshotClip;
    public AudioClip reloadSoundClip;
    public AudioClip emptyClip;

    [Header("Hybrid System")]
    public bool useRaycastDamage = true; // Primary damage system
    public bool useVisualBullets = true; // Visual bullets for effect
    public GameObject bulletPrefab;
    public float bulletSpeed = 50f;
    public LayerMask bulletCollisionLayers = -1; // What bullets can hit visually

    [Header("Aiming")]
    public float aimSpreadReduction = 0.5f;
    public float aimDamageBonus = 1.2f;

    // Private variables
    private RaycastHit rayHit;
    private int currentDamage;

    private void Awake()
    {
        bulletsLeft = magazineSize;
        readyToShoot = true;

        if (playerMovement == null)
            playerMovement = GetComponentInParent<PlayerMovement>();
    }

    private void Update()
    {
        HandleInput();
        UpdateUI();
        UpdateAimingState();
    }

    private void HandleInput()
    {
        if (playerMovement != null && playerMovement.isDead)
            return;

        if (allowButtonHold)
            shooting = Input.GetKey(KeyCode.Mouse0);
        else
            shooting = Input.GetKeyDown(KeyCode.Mouse0);

        if (Input.GetKeyDown(KeyCode.R) && bulletsLeft < magazineSize && !reloading)
            Reload();

        if (shooting && bulletsLeft <= 0 && !reloading)
        {
            if (audioSource != null && emptyClip != null)
                audioSource.PlayOneShot(emptyClip);

            Reload();
            return;
        }

        if (readyToShoot && shooting && !reloading && bulletsLeft > 0)
        {
            bulletsShot = bulletsPerTap;
            Shoot();
        }
    }

    private void UpdateAimingState()
    {
        isAiming = Input.GetKey(KeyCode.Mouse1);
    }

    private void UpdateUI()
    {
        if (ammoText != null)
        {
            ammoText.SetText($"{bulletsLeft} / {magazineSize}");

            if (bulletsLeft <= 0)
                ammoText.color = Color.red;
            else if (bulletsLeft <= magazineSize * 0.25f)
                ammoText.color = Color.yellow;
            else
                ammoText.color = Color.white;
        }
    }

    private void Shoot()
    {
        readyToShoot = false;

        // Calculate spread and damage
        float currentSpread = isAiming ? spread * aimSpreadReduction : spread;
        float x = Random.Range(-currentSpread, currentSpread);
        float y = Random.Range(-currentSpread, currentSpread);
        currentDamage = isAiming ? Mathf.RoundToInt(damage * aimDamageBonus) : damage;

        Vector3 direction = fpsCam.transform.forward + new Vector3(x, y, 0);

        // RAYCAST DAMAGE (Instant hit)
        if (useRaycastDamage)
        {
            ProcessRaycastHit(direction);
        }

        // VISUAL BULLET (For effect)
        if (useVisualBullets)
        {
            FireVisualBullet(direction);
        }

        // Common effects
        CreateMuzzleFlash();
        PlayGunshotSound();
        ApplyCameraShake();

        // Update ammo and timing
        bulletsLeft--;
        bulletsShot--;

        Invoke(nameof(ResetShot), timeBetweenShooting);

        if (bulletsShot > 0 && bulletsLeft > 0)
            Invoke(nameof(Shoot), timeBetweenShots);
    }

    private void ProcessRaycastHit(Vector3 direction)
    {
        bool hitSomething = Physics.Raycast(fpsCam.transform.position, direction, out rayHit, range, whatIsEnemy);

        if (hitSomething)
        {
            Debug.Log($"Raycast Hit: {rayHit.collider.name} for {currentDamage} damage");

            // Apply damage instantly via raycast
            Health healthComponent = rayHit.collider.GetComponent<Health>();
            if (healthComponent != null)
            {
                healthComponent.TakeDamage(currentDamage);
            }

            // Visual effects at hit point
            CreateBulletTrail(rayHit.point);
            CreateBulletHole(rayHit.point, rayHit.normal);
        }
        else
        {
            // Create trail to max range for missed shots
            Vector3 endPoint = fpsCam.transform.position + direction * range;
            CreateBulletTrail(endPoint);
        }
    }

    private void FireVisualBullet(Vector3 direction)
    {
        if (bulletPrefab == null || attackPoint == null) return;

        GameObject bullet = Instantiate(bulletPrefab, attackPoint.position, Quaternion.LookRotation(direction));
        Rigidbody rb = bullet.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = direction.normalized * bulletSpeed;
        }

        // Setup visual bullet (no damage, just for show)
        VisualBullet visualBullet = bullet.GetComponent<VisualBullet>();
        if (visualBullet == null)
            visualBullet = bullet.AddComponent<VisualBullet>();

        visualBullet.Initialize(bulletCollisionLayers);

        Destroy(bullet, 5f);
    }

    private void CreateMuzzleFlash()
    {
        if (muzzleFlash != null && attackPoint != null)
        {
            GameObject flash = Instantiate(muzzleFlash, attackPoint.position, attackPoint.rotation);
            flash.transform.SetParent(attackPoint);
            Destroy(flash, 0.2f);
        }
    }

    private void PlayGunshotSound()
    {
        if (audioSource != null && gunshotClip != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(gunshotClip);
        }
    }

    private void ApplyCameraShake()
    {
        if (camShake != null)
        {
            float shakeAmount = isAiming ? camShakeMagnitude * 0.5f : camShakeMagnitude;
            camShake.Shake(camShakeDuration, shakeAmount);
        }
    }

    private void CreateBulletTrail(Vector3 hitPoint)
    {
        if (bulletTrailPrefab == null || attackPoint == null) return;

        GameObject trail = Instantiate(bulletTrailPrefab, attackPoint.position, Quaternion.identity);
        LineRenderer lr = trail.GetComponent<LineRenderer>();

        if (lr != null)
        {
            lr.SetPosition(0, attackPoint.position);
            lr.SetPosition(1, hitPoint);
        }

        Destroy(trail, trailLifetime);
    }

    private void CreateBulletHole(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (bulletHoleGraphic != null)
        {
            GameObject hole = Instantiate(bulletHoleGraphic, hitPoint, Quaternion.LookRotation(hitNormal));
            Destroy(hole, 10f);
        }
    }

    private void ResetShot()
    {
        readyToShoot = true;
    }

    private void Reload()
    {
        if (reloading) return;

        reloading = true;

        if (audioSource != null && reloadSoundClip != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(reloadSoundClip);
        }

        Invoke(nameof(ReloadFinished), reloadTime);
    }

    private void ReloadFinished()
    {
        bulletsLeft = magazineSize;
        reloading = false;
    }

    // Public methods
    public bool IsReloading() => reloading;
    public bool IsAiming() => isAiming;
    public float GetAmmoPercentage() => (float)bulletsLeft / magazineSize;

    public void AddAmmo(int amount)
    {
        bulletsLeft = Mathf.Min(bulletsLeft + amount, magazineSize);
    }

    // Toggle between systems
    public void SetRaycastDamage(bool enabled)
    {
        useRaycastDamage = enabled;
    }

    public void SetVisualBullets(bool enabled)
    {
        useVisualBullets = enabled;
    }
}

// Visual bullet component (no damage, just for show)
public class VisualBullet : MonoBehaviour
{
    private LayerMask collisionLayers;
    private bool hasInitialized = false;

    public void Initialize(LayerMask layers)
    {
        collisionLayers = layers;
        hasInitialized = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasInitialized) return;

        // Check if we hit something in our collision layers
        if ((collisionLayers.value & (1 << other.gameObject.layer)) > 0)
        {
            // Create impact effect (optional)
            CreateImpactEffect(other);

            // Destroy the visual bullet
            Destroy(gameObject);
        }
    }

    private void CreateImpactEffect(Collider hitCollider)
    {
        // You can add particle effects, sound, etc. here
        // This is purely visual - no damage dealt
        Debug.Log($"Visual bullet hit: {hitCollider.name}");
    }
}