using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks
{
    public Transform viewPoint;
    public float mouseSensitivity = 1f;
    private float verticalRotStore;
    private Vector2 mouseInput;
    public bool invertLook;
    public CharacterController charCon;

    public float moveSpeed = 5f, runSpeed = 8f;
    private float activeMoveSpeed;
    
    private Vector3 moveDir, movement;

    private Camera cam;

    public float jumpForce = 12f,gravityMod =2.5f;

    public Transform groundCheckPoint;
    private bool isGrounded;
    public LayerMask groundLayers;

    public GameObject bulletImpact;

    //public float timeBetweenShots = .1f;
    private float shotCounter;

    public float maxHeat = 10f, /*heatPerShot = 1f,*/ coolRate = 4f, overheatCoolRate = 5f;
    private float heatCounter = 0;
    private bool overHeated;

    public Gun[] allGuns;
    private int selectedGun;

    public GameObject playerHitImpact;
    public int maxHealth = 100;
    private int currentHealth;

    public Animator anim;

    public GameObject playerModel;
    public Transform modelGunPoint;
    public Transform gunHolder;
    
    // Start is called before the first frame update
    void Start()
    {

        // SwitchGun();
        photonView.RPC("SetGun", RpcTarget.All, selectedGun);
        if (photonView.IsMine)
        {
            Cursor.lockState = CursorLockMode.Locked;

            cam = Camera.main;


            playerModel.SetActive(false);

            currentHealth = maxHealth;
            UIController.instance.weaponTempSlider.maxValue = maxHeat;

            UIController.instance.healthSlider.maxValue = maxHealth;
            UIController.instance.healthSlider.value = maxHealth;
        }
        else{
            gunHolder.parent = modelGunPoint;
            gunHolder.localPosition = Vector3.zero;
            gunHolder.localRotation = Quaternion.identity;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!photonView.IsMine) return;

        mouseInput = new Vector2(
            Input.GetAxisRaw("Mouse X"), 
            Input.GetAxisRaw("Mouse Y")
            )*mouseSensitivity;

        transform.rotation = Quaternion.Euler(
            transform.rotation.eulerAngles.x,
            transform.rotation.eulerAngles.y + mouseInput.x,
            transform.rotation.eulerAngles.z
        );

        verticalRotStore += mouseInput.y;
        verticalRotStore = Mathf.Clamp(verticalRotStore,-60f,60f);
        
        if (invertLook)
        {
            viewPoint.rotation = Quaternion.Euler(
                verticalRotStore,
                viewPoint.rotation.eulerAngles.y,
                viewPoint.rotation.eulerAngles.z
                );
        }
        else
        {
            viewPoint.rotation = Quaternion.Euler(
                -verticalRotStore,
                viewPoint.rotation.eulerAngles.y,
                viewPoint.rotation.eulerAngles.z
                );
        }

        moveDir = new Vector3(Input.GetAxisRaw("Horizontal"),0f,Input.GetAxisRaw("Vertical"));


        if (Input.GetKey(KeyCode.LeftShift))
        {
            activeMoveSpeed = runSpeed;
        }
        else
        {
            activeMoveSpeed = moveSpeed;
        }

        float yVel = movement.y;
        movement = ((transform.forward * moveDir.z) + (transform.right * moveDir.x)).normalized * activeMoveSpeed;
        movement.y = yVel;

        if (charCon.isGrounded){
            movement.y = 0f;
        }

        isGrounded = Physics.Raycast(groundCheckPoint.position,Vector3.down, 0.25f, groundLayers);

        if (Input.GetButton("Jump") && isGrounded)
        {
            movement.y = jumpForce;
        }
        movement.y += Physics.gravity.y * Time.deltaTime*gravityMod;
        charCon.Move(movement  * Time.deltaTime);

        allGuns[selectedGun].muzzleFlash.gameObject.SetActive(false);

        if (!overHeated)
        {
            if (Input.GetMouseButtonDown(0)){
                Shoot();
            }

            if (Input.GetMouseButton(0) && allGuns[selectedGun].isAutomatic){
                shotCounter -= Time.deltaTime;

                if(shotCounter <= 0){
                    Shoot();
                }
            }
            heatCounter -= coolRate*Time.deltaTime;
        }
        else
        {
            heatCounter -= overheatCoolRate*Time.deltaTime;
            if(heatCounter <= 0){
                overHeated = false;
                UIController.instance.overheatedMessage.gameObject.SetActive(false);
            }
        }

        if(heatCounter <= 0){
            heatCounter = 0;
        }

        UIController.instance.weaponTempSlider.value = heatCounter;

        if(Input.GetAxisRaw("Mouse ScrollWheel")>0f){
            selectedGun++;
            if(selectedGun >= allGuns.Length){
                selectedGun = 0;
            }
            photonView.RPC("SetGun", RpcTarget.All, selectedGun);
        }
        else if(Input.GetAxisRaw("Mouse ScrollWheel")<0f){
            selectedGun--;
            if(selectedGun <= 0){
                selectedGun = allGuns.Length-1;
            }
            photonView.RPC("SetGun", RpcTarget.All, selectedGun);
        }

        for(int i = 0; i < allGuns.Length; i++)
        {
            if(Input.GetKeyDown((i+1).ToString())){
                selectedGun = i;
                photonView.RPC("SetGun", RpcTarget.All, selectedGun);
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape)){
            Cursor.lockState = CursorLockMode.None;
        }
        else if (Cursor.lockState == CursorLockMode.None)
        {
            if (Input.GetMouseButtonDown(0)){
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        anim.SetBool("grounded", isGrounded);
        anim.SetFloat("speed", moveDir.magnitude);
    }

    private void Shoot(){
        Ray ray = cam.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
        ray.origin = cam.transform.position;
        if (Physics.Raycast(ray, out RaycastHit hit))
        {   
            if (hit.collider.gameObject.tag == "Player")
            {
                // playerHitImpact
                Debug.Log("Hit "+ hit.collider.gameObject.GetPhotonView().Owner.NickName);
                PhotonNetwork.Instantiate(playerHitImpact.name, hit.point, Quaternion.identity);

                hit.collider.gameObject.GetPhotonView().RPC(
                    "DealDamage", 
                    RpcTarget.All, 
                    photonView.Owner.NickName, 
                    allGuns[selectedGun].shotDamage,
                    PhotonNetwork.LocalPlayer.ActorNumber
                    );
            }
            else
            {
                GameObject bulletImpactObject = Instantiate(bulletImpact,hit.point+(hit.normal*.002f), Quaternion.LookRotation(hit.normal,Vector3.up));
                Destroy(bulletImpactObject, 10f);
            }
        }
        shotCounter = allGuns[selectedGun].timeBetweenShots;

        heatCounter += allGuns[selectedGun].heatPerShot;

        if (heatCounter >= maxHeat) 
        {
            heatCounter = maxHeat;
            overHeated = true;
            UIController.instance.overheatedMessage.gameObject.SetActive(true);
        }

        allGuns[selectedGun].muzzleFlash.gameObject.SetActive(true);
    }
    
    [PunRPC]
    public void DealDamage(string damager, int damageAmount, int actor){
        TakeDamage(damager, damageAmount,actor);
    }

    public void TakeDamage(string damager, int damageAmount, int actor){
        if (photonView.IsMine)
        {
            // Debug.Log(photonView.Owner.NickName + " has been hit by "+ damager);
            currentHealth -= damageAmount;
            UIController.instance.healthSlider.value = currentHealth;
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                PlayerSpawner.instance.Die(damager);

                MatchManager.instance.UpdateStatsSend(actor, 0, 1);
            }

        }
    }

    private void LateUpdate(){
        if (!photonView.IsMine) return;

        cam.transform.position = viewPoint.position;
        cam.transform.transform.rotation = viewPoint.rotation;
    }
    
    void SwitchGun(){
        foreach(Gun gun in allGuns){
            gun.gameObject.SetActive(false);
        }
        allGuns[selectedGun].gameObject.SetActive(true);
        allGuns[selectedGun].muzzleFlash.SetActive(false);
    }

    [PunRPC]
    public void SetGun(int gunToSwitchTo){
        if (gunToSwitchTo < allGuns.Length)
        {
            selectedGun = gunToSwitchTo;
            SwitchGun();
        }
    }
}
