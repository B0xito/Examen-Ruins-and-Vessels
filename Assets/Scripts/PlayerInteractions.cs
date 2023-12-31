using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInteractions : MonoBehaviour
{
    #region Movement Variables
    [Header("Movement Variables")]
    [SerializeField] float walkSpeed = 6f;
    [SerializeField] float ogSpeed;
    [SerializeField] float runSpeed = 9f;
    #endregion

    #region Stamina System
    [Header("Stamina System")]
    public Image staminaBar;
    [SerializeField] TMP_Text staminaAmountText;
    [SerializeField] float currentStamina;
    [SerializeField] float maxStamina;
    [SerializeField] float runCost;
    [SerializeField] float chargeRate;
    [SerializeField] bool fullStamina;
    bool isRecharging;
    #endregion

    #region Mining Variables
    [Header("Mining Variables")]
    [Range(1f, 5f)]
    [SerializeField] float rayDistance;
    [SerializeField] LayerMask minableMask;
    [SerializeField] SpawnItem spawnItem;
    [SerializeField] Animator riftAnim;
    [SerializeField] GameManager gameManager;

    [Header("Collapse")]
    [SerializeField] Collapse collapse;
    #endregion

    #region Consumable Variables
    [Header("Consumable Variables")]
    [SerializeField] List<Consumable> consumables = new List<Consumable>();
    Consumable currentItem;
    [SerializeField] int itemAmount;
    [SerializeField] TMP_Text itemAmountText;
    [SerializeField] Image itemUI;
    [SerializeField] Sprite notItemSprite;
    [SerializeField] GameObject regenAmount;
    #endregion

    #region Pieces Variables
    [Header("Pieces Variables")]
    //[SerializeField] List<PieceData> pieces = new List<PieceData>();
    public bool addingPiece = false;
    private Coroutine adding;

    [Header("Pieces Count")]
    //Red pieces
    public int redCount;
    [SerializeField] TMP_Text redCountText;

    //Blue pieces
    public int blueCount;
    [SerializeField] TMP_Text blueCountText;

    //Green pieces
    public int greenCount;
    [SerializeField] TMP_Text greenCountText;

    //Golden pieces
    public int goldenCount;
    [SerializeField] TMP_Text goldenCountText;
    #endregion

    #region Audio
    [Header("Audio Sources")]
    public AudioSource effectPlayer;
    public AudioSource effectsObjects;
    public AudioSource BGM;

    [Header("Audo Clips")]
    public AudioClip playerSteps;
    public AudioClip consumableSound;
    public AudioClip BGMusic;

    public AudioClip riftMined;
    #endregion
    private void Awake()
    {
        BGM.clip = BGMusic;
    }

    private void Start()
    {
        BGM.Play();
        maxStamina = 100f;
        currentStamina = maxStamina;
        ogSpeed = walkSpeed;
        gameManager = FindObjectOfType<GameManager>();
    }

    void Update()
    {
        // Dependiendo del tipo de camera es el tipo de movimiento
        // FirstPersonMovement();

        ThirdPersonMovement();

        Mining();

        #region Stamina System
        CheckStamina();
        staminaBar.fillAmount = currentStamina / maxStamina;
        staminaAmountText.text = currentStamina.ToString("F0") + " " + "/" + " " + maxStamina.ToString();
        #endregion

        #region Consumable
        if (Input.GetKeyDown(KeyCode.C) && fullStamina == false)
        {
            effectPlayer.clip = consumableSound;
            ConsumeItem(currentItem);
            effectPlayer.Play();
        }

        if (consumables.Count > 0) { regenAmount.SetActive(true); }
        if (consumables.Count <= 0) { itemUI.sprite = notItemSprite; regenAmount.SetActive(false);  }
        else { itemUI.sprite = currentItem.consumableSprite; }
        #endregion
    }

    void FirstPersonMovement()
    {
        float h = Input.GetAxis("Horizontal") * walkSpeed * Time.deltaTime;
        float v = Input.GetAxis("Vertical") * walkSpeed * Time.deltaTime;
        transform.Translate(h, 0, v);
    }

    void ThirdPersonMovement()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);

        // Esto hace que el personaje se mueva
        transform.position = transform.position + movement * walkSpeed * Time.deltaTime;

        // Esto hace que el personaje mire hacia la direcci�n de movimiento
        if (movement != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(movement), 0.15F);
        }      

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S) || 
            Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D))
        {
            effectPlayer.clip = playerSteps;
            effectPlayer.Play();
        }
    }

    void CheckStamina()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (currentStamina > 0)
            {
                currentStamina -= runCost * Time.deltaTime;
                walkSpeed = runSpeed;
            }
            else
            {
                currentStamina = 0;               
                walkSpeed = ogSpeed;
            }
        }
        else
        {
            walkSpeed = ogSpeed;
            if (currentStamina < maxStamina)
            {
                if (!isRecharging) StartCoroutine(RechargeStamina());
            }
        }

        if (currentStamina >= maxStamina)
        {
            currentStamina = maxStamina;
            fullStamina = true;
        }
        else
        { 
            fullStamina = false;
        }
    }

    void Mining()
    {
        RaycastHit hit;
        Debug.DrawRay(transform.position, transform.forward * rayDistance, Color.red);

        //Revisa con un rayo desde la posicion 0,0 del player hacia adelante si hay un gameobject
        //con el layer minable, si es asi entra el if
        if (Physics.Raycast(transform.position, transform.forward, out hit, rayDistance, minableMask))
        {
            spawnItem = hit.transform.gameObject.GetComponent<SpawnItem>();
            riftAnim = hit.transform.gameObject.GetComponentInParent<Animator>();

            if (Input.GetKeyDown(KeyCode.F))
            {
                Debug.Log("Mining");
                if (hit.collider.CompareTag("Rift"))
                {
                    effectsObjects.clip = riftMined;
                    effectsObjects.Play();
                    riftAnim.SetBool("mining", true);
                    gameManager.GetComponent<GameManager>().totalRifts--;

                }
                else
                {
                    Debug.Log("Not rift");
                }
            }
        }
    }

    public void SpawnItemsFromRift()
    {
        collapse.CollapseProb();
        Debug.Log("GameObject spawned");
        int rand = Random.Range(5, 10);
        spawnItem.Mined(rand);
        riftAnim.SetBool("mining", false);
    }

    IEnumerator RechargeStamina()
    {
        isRecharging = true;
        yield return new WaitForSeconds(1f);
        while (currentStamina < maxStamina)
        {
            currentStamina += chargeRate / 10f;
            if (currentStamina > maxStamina) currentStamina = maxStamina;
            yield return new WaitForSeconds(.1f);
        }
        isRecharging = false;
    }

    #region Consumable
    public void AddConsumable(Consumable item)
    {
        consumables.Add(item);
        Debug.Log(item.GetComponent<Consumable>().consumableName + " " + "added to consumables");
        itemUI.sprite = item.GetComponent<Consumable>().consumableSprite;
    }

    public void ConsumeItem(Consumable item)
    {
        if (consumables.Contains(item))
        {
            Debug.Log(item.GetComponent<Consumable>().consumableName + " " + "removed from consumables");
            currentStamina += item.GetComponent<Consumable>().regenerationAmount;
            Destroy(item.transform.gameObject);
            consumables.Remove(item);
            itemUI.sprite = notItemSprite;
            itemAmount--;
            itemAmountText.text = itemAmount.ToString();          
        }
    }
    #endregion

    #region Pieces
    private IEnumerator AddingPieceTimer()
    {
       yield return new WaitForSeconds(1.7f);

       addingPiece = false;
    }

    //public void AddPiece(PieceData fragment)
    //{
    //    if (fragment.CompareTag("RedPiece") || fragment.CompareTag("BluePiece") || 
    //        fragment.CompareTag("GreenPiece") || fragment.CompareTag("GoldenPiece"))
    //    {
    //        pieces.Add(fragment);
    //        addingPiece = true;
    //        if (adding != null) StopCoroutine(adding);
    //        adding = StartCoroutine(AddingPieceTimer());
    //    }
    //}

    //public void RemovePiece(PieceData fragment)
    //{
    //    if (pieces.Contains(fragment))
    //    {
    //        pieces.Remove(fragment);
    //    }
    //}
    #endregion

    private void OnTriggerEnter(Collider other)
    {
        #region Consumable
        if (other.GetComponent<Consumable>())
        {
            Debug.Log("Trigger Consumable");
            Consumable thisConsumable = other.GetComponent<Consumable>();
            AddConsumable(thisConsumable);
            currentItem = thisConsumable;
            itemAmount++;
            itemAmountText.text = itemAmount.ToString();
            if (other.CompareTag("Item"))
            {
                other.gameObject.SetActive(false);
            }
        }
        #endregion

        #region Pieces
        if (other.gameObject.GetComponent<PieceData>())
        {
            //AddPiece(other.GetComponent<PieceData>());

            // La pieza se destruye, pero el script no se guarda65
            if (other.CompareTag("RedPiece")) { redCount++; redCountText.text = redCount.ToString(); spawnItem.spawnedItems.Remove(other.gameObject); Destroy(other.gameObject); addingPiece = true; if (adding != null) StopCoroutine(adding); adding = StartCoroutine(AddingPieceTimer()); }
            if (other.CompareTag("BluePiece")) { blueCount++; blueCountText.text = blueCount.ToString(); spawnItem.spawnedItems.Remove(other.gameObject); Destroy(other.gameObject); addingPiece = true; if (adding != null) StopCoroutine(adding); adding = StartCoroutine(AddingPieceTimer()); }
            if (other.CompareTag("GreenPiece")) { greenCount++; greenCountText.text = greenCount.ToString(); spawnItem.spawnedItems.Remove(other.gameObject); Destroy(other.gameObject); addingPiece = true; if (adding != null) StopCoroutine(adding); adding = StartCoroutine(AddingPieceTimer()); }
            if (other.CompareTag("GoldenPiece")) { goldenCount++; goldenCountText.text = goldenCount.ToString(); spawnItem.spawnedItems.Remove(other.gameObject); Destroy(other.gameObject); addingPiece = true; if (adding != null) StopCoroutine(adding); adding = StartCoroutine(AddingPieceTimer()); }
        }
        #endregion
    }
}