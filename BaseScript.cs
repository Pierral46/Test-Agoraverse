using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BaseScript : MonoBehaviour
{
    private Rigidbody rbBall1, rbBall2;
    private Camera Camera;
    private List<GameObject> primitives = new List<GameObject>();
    private GameObject[] cubesInChamboultou = new GameObject[6];
    private GameObject currentHeldObject = null, canvasUI, heldBall, ball1, ball2;
    private Image imageRotate, imageScale, imageCube, imageSphere, imageCylinder, imageRadio;
    private Color[] colorArray = new Color[] { Color.white, Color.black, Color.gray, Color.magenta, Color.green, Color.blue, Color.yellow };
    private Color primitiveColor;
    RaycastHit RayHitInfo;
    private int colorArrayNumber = 0, cylinderScale = 1, clipNumber = 0, ButtonEventNumber = 0, thrownBalls = 0;
    private bool movedCam = false, trueRotateFalseScale = true, primitiveColliding = false;
    private float primitiveHeight = 0.5f;
    private AudioSource audiosource;
    private Canvas myCanvas;

    public AudioClip[] clip;
    void Start()
    {
        CreateCanvas();
        CreateChamboultou();
        Camera = FindObjectOfType<Camera>();
        GameObject basePlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        basePlane.transform.localScale = new Vector3(10, 10, 10);
        basePlane.GetComponent<Renderer>().material.color = Color.gray;
    }


    void Update()
    {
        BaseMovements();
        PrimitiveController();
    }


    private void BaseMovements()
    {
        //Camera.transform.eulerAngles
        float horizontal, vertical;
        float mouseX = Input.GetAxis("Mouse X") * 400 * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * 500 * Time.deltaTime;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.Q))
        {
            horizontal = -1;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            horizontal = 1;
        }
        else horizontal = 0;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Z))
        {
            vertical = 1;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            vertical = -1;
        }
        else vertical = 0;
        horizontal *= 20 * Time.deltaTime;
        vertical *= 20 * Time.deltaTime;
        if (Input.GetButton("Fire2"))
        {
            Camera.transform.eulerAngles += new Vector3(-mouseY, mouseX, 0);
            Camera.transform.Translate(new Vector3(horizontal, 0, vertical));
            if (mouseX != 0 || mouseY != 0) movedCam = true;
        }
    }

    private void PrimitiveController()
    {
        //Raycast
        bool RayHit;
        Rigidbody ballRB = null;
        RayHit = Physics.Raycast(Camera.ScreenPointToRay(Input.mousePosition), out RayHitInfo, 15);
        //Position to place primitives so they are always above ground
        if (currentHeldObject != null)
        {
            primitiveHeight = currentHeldObject.transform.localScale.y / (2 - cylinderScale);
        }
        //Position to hold chamboultou ball
        if (heldBall != null)
        {
            ballRB = heldBall.GetComponent<Rigidbody>();
            ballRB.transform.position = Camera.transform.position + Camera.transform.forward * 1;
        }
        //Color of primitive depending on collision with objects
        if (RayHit && currentHeldObject != null && !primitiveColliding)
        {
            currentHeldObject.transform.position = new Vector3(RayHitInfo.point.x, RayHitInfo.point.y + primitiveHeight, RayHitInfo.point.z);
            currentHeldObject.GetComponent<Renderer>().material.color = Color.green;
        }
        else if (RayHit && currentHeldObject != null && primitiveColliding)
        {
            currentHeldObject.transform.position = new Vector3(RayHitInfo.point.x, RayHitInfo.point.y + primitiveHeight, RayHitInfo.point.z);
            currentHeldObject.GetComponent<Renderer>().material.color = Color.red;
        }
        else if (!RayHit && currentHeldObject != null)
        {
            Vector3 primitivePosition = Camera.transform.position + Camera.ScreenPointToRay(Input.mousePosition).direction * 15;
            currentHeldObject.transform.position = new Vector3(primitivePosition.x, primitivePosition.y + primitiveHeight, primitivePosition.z);
            currentHeldObject.GetComponent<Renderer>().material.color = Color.red;
        }

        //Collision check
        if (currentHeldObject != null)
        {
            Vector3 position = new Vector3(currentHeldObject.transform.position.x, currentHeldObject.transform.position.y + 0.01f, currentHeldObject.transform.position.z);
            primitiveColliding = Physics.CheckBox(position, currentHeldObject.transform.localScale / (2 - cylinderScale), currentHeldObject.transform.rotation);
        }

        //Destroy Primitive
        if (currentHeldObject != null && Input.GetButtonDown("Cancel"))
        {
            DestroyPrimitive();
        }

        //Place and recover Primitive or throw chamboultou ball
        if (Input.GetButtonDown("Fire1"))
        {
            if (currentHeldObject != null && RayHit && !primitiveColliding)
            {
                currentHeldObject.layer = 0;
                currentHeldObject.GetComponent<Renderer>().material.color = primitiveColor;
                currentHeldObject = null;
            }
            else if (currentHeldObject == null && heldBall == null && RayHit && primitives.Contains(RayHitInfo.collider.gameObject))
            {
                primitiveColor = RayHitInfo.collider.gameObject.GetComponent<Renderer>().material.color;
                currentHeldObject = RayHitInfo.collider.gameObject;
                currentHeldObject.layer = 2;
            }
            if(currentHeldObject == null && heldBall == null && RayHit && RayHitInfo.collider.CompareTag("Respawn"))
            {
                heldBall = RayHitInfo.collider.gameObject;
                heldBall.layer = 2;
            }
            else if(currentHeldObject == null && heldBall != null)
            {
                ballRB.AddForce((Camera.transform.forward + new Vector3(0,0.1f,0)) * 40, ForceMode.Impulse);
                thrownBalls++;
                heldBall.layer = 0;
                heldBall = null;
                if (thrownBalls == 2) Invoke("RespawnChamboultou", 1f);
            }
        }

        //Change primitive color
        if (Input.GetButtonUp("Fire2")&& RayHit && currentHeldObject == null)
        {
            if (!movedCam)
            {   //radio music change
                if (RayHitInfo.collider.CompareTag("Player"))
                {
                    clipNumber++;
                    if (clipNumber == 3) clipNumber = 0;
                    audiosource.clip = clip[clipNumber];
                    audiosource.Play();
                }
                else
                {       //color change
                    RayHitInfo.collider.gameObject.GetComponent<Renderer>().material.color = colorArray[colorArrayNumber];
                    colorArrayNumber++;
                    if (colorArrayNumber == 7) colorArrayNumber = 0;
                }
            }
            else
            {
                movedCam = false;
            }
        }

        //Create different primitives
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            CreatePrimitive(PrimitiveType.Cube);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            CreatePrimitive(PrimitiveType.Sphere);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            CreatePrimitive(PrimitiveType.Cylinder);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            CreateRadio();
        }

        //Rotate and Scale Primitives
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            RotateOrScale();
        }
        if (currentHeldObject != null && trueRotateFalseScale)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel") * 10000 * Time.deltaTime;
            currentHeldObject.transform.Rotate(new Vector3(0, scroll, 0), Space.World);
        }
        else if (currentHeldObject != null && !trueRotateFalseScale)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel") * 500 * Time.deltaTime;
            currentHeldObject.transform.localScale += new Vector3(scroll, scroll, scroll);
        }
    }

    private void CreatePrimitive(PrimitiveType primitive)
    {
        if (currentHeldObject != null)
        {
            DestroyPrimitive();
        }
        currentHeldObject = GameObject.CreatePrimitive(primitive);
        primitives.Add(currentHeldObject);
        currentHeldObject.layer = 2;
        if (primitive == PrimitiveType.Cube)
        {
            imagesToWhite();
            imageCube.color = Color.yellow;
        }
        if (primitive == PrimitiveType.Sphere)
        {
            imagesToWhite();
            imageSphere.color = Color.yellow;
        }
        if (primitive == PrimitiveType.Cylinder)
        {
            currentHeldObject.transform.localScale = new Vector3(1, 0.5f, 1);
            cylinderScale = 1;
            imagesToWhite();
            imageCylinder.color = Color.yellow;
        }
        else
        {
            cylinderScale = 0;
        }
    }

    private void CreateRadio()
    {
        cylinderScale = 0;
        GameObject radioPart;
        if (currentHeldObject != null)
        {
            DestroyPrimitive();
        }
        imagesToWhite();
        imageRadio.color = Color.yellow;
        currentHeldObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        primitives.Add(currentHeldObject);
        currentHeldObject.layer = 2;
        currentHeldObject.transform.localScale = new Vector3(0.8f, 1, 2);
        currentHeldObject.GetComponent<Renderer>().material.color = Color.gray;
        currentHeldObject.tag = "Player";
        audiosource = currentHeldObject.AddComponent<AudioSource>();
        audiosource.clip = clip[0];
        audiosource.Play();
        audiosource.spatialBlend = 1;
        audiosource.maxDistance = 10;

        radioPart = GameObject.CreatePrimitive(PrimitiveType.Cube);
        radioPart.transform.localScale = new Vector3(0.1f, 0.1f, 0.2f);
        radioPart.transform.position = new Vector3(-0.367f, 0, 0);
        radioPart.transform.SetParent(currentHeldObject.transform);
        radioPart.layer = 2;

        radioPart = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        radioPart.transform.localScale = new Vector3(0.8f, 0.1f, 0.7f);
        radioPart.transform.position = new Vector3(-0.35f, 0, 0.6f);
        radioPart.transform.eulerAngles = new Vector3(0, 0, 90);
        radioPart.transform.SetParent(currentHeldObject.transform);
        radioPart.layer = 2;

        radioPart = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        radioPart.transform.localScale = new Vector3(0.8f, 0.1f, 0.7f);
        radioPart.transform.position = new Vector3(-0.35f, 0, -0.6f);
        radioPart.transform.eulerAngles = new Vector3(0, 0, 90);
        radioPart.transform.SetParent(currentHeldObject.transform);
        radioPart.layer = 2;
    }

    private void CreateChamboultou()
    {
        GameObject socle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        socle.transform.position = new Vector3(0, 0.5f, 0);
        socle.transform.localScale = new Vector3(2, 1, 1);
        socle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        socle.transform.position = new Vector3(0, 0.5f, 5.5f);
        socle.transform.localScale = new Vector3(2, 1, 1);
        ball1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball1.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        ball1.transform.position = new Vector3(-0.42f, 1.15f, 0);
        rbBall1 = ball1.AddComponent<Rigidbody>();
        rbBall1.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        ball1.transform.tag = "Respawn";
        ball2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball2.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        ball2.transform.position = new Vector3(0, 1.15f, 0);
        rbBall2 = ball2.AddComponent<Rigidbody>();
        rbBall2.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        ball2.transform.tag = "Respawn";
        int nbrOfCubeToBeGen = 3, nbrOfCubesTotal = 0;
        float x = -0.3f, y = 1.15f;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < nbrOfCubeToBeGen; j++)
            {
                cubesInChamboultou[nbrOfCubesTotal] = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cubesInChamboultou[nbrOfCubesTotal].transform.localScale = new Vector3(0.2f, 0.3f, 0.2f);
                cubesInChamboultou[nbrOfCubesTotal].transform.position = new Vector3(x, y, 5.49f);
                cubesInChamboultou[nbrOfCubesTotal].AddComponent<Rigidbody>();
                x += 0.3f;
                nbrOfCubesTotal++;
            }
            nbrOfCubeToBeGen--;
            y += 0.3f;
            x -= 0.45f;
            x *= -1;
        }
    }

    private void RespawnChamboultou()
    {
        rbBall1.isKinematic = true;
        ball1.transform.position = new Vector3(-0.42f, 1.15f, 0);
        rbBall1.isKinematic = false;
        rbBall2.isKinematic = true;
        ball2.transform.position = new Vector3(0, 1.15f, 0);
        rbBall2.isKinematic = false;
        thrownBalls = 0;
        int nbrOfCubeToBeGen = 3, nbrOfCubesTotal = 0;
        float x = -0.3f, y = 1.15f;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < nbrOfCubeToBeGen; j++)
            {
                cubesInChamboultou[nbrOfCubesTotal].GetComponent<Rigidbody>().isKinematic = true;
                cubesInChamboultou[nbrOfCubesTotal].transform.position = new Vector3(x, y, 5.49f);
                cubesInChamboultou[nbrOfCubesTotal].transform.eulerAngles = Vector3.zero;
                cubesInChamboultou[nbrOfCubesTotal].GetComponent<Rigidbody>().isKinematic = false;
                nbrOfCubesTotal++;
                x += 0.3f;
            }
            nbrOfCubeToBeGen--;
            y += 0.3f;
            x -= 0.45f;
            x *= -1;
        }
    }

    private void DestroyPrimitive()
    {
        primitives.Remove(currentHeldObject);
        Destroy(currentHeldObject);
    }

    private void RotateOrScale()
    {
        if (trueRotateFalseScale)
        {
            trueRotateFalseScale = false;
            imageRotate.color = Color.white;
            imageScale.color = Color.yellow;
        }
        else
        {
            trueRotateFalseScale = true;
            imageRotate.color = Color.yellow;
            imageScale.color = Color.white;
        }
    }

    private void imagesToWhite()
    {
        imageCube.color = Color.white;
        imageSphere.color = Color.white;
        imageCylinder.color = Color.white;
        imageRadio.color = Color.white;
    }

    private void CreateCanvas()
    {
        canvasUI = new GameObject();
        canvasUI.name = "Canvas";
        myCanvas = canvasUI.AddComponent<Canvas>();
        myCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasUI.AddComponent<CanvasScaler>();
        canvasUI.AddComponent<GraphicRaycaster>();
        imageRotate = CreateImageFeedback("imageRotate", new Vector3(-750, 400, 0), "Rotate", new Vector3(11, -35, 0), true);
        imageScale = CreateImageFeedback("imageScale", new Vector3(-640, 400, 0), "Scale", new Vector3(15, -35, 0), false);
        imageCube = CreateImageFeedback("Cube", new Vector3(-165, 400, 0), "Cube", new Vector3(17, -35, 0), false);
        imageSphere = CreateImageFeedback("Sphere", new Vector3(-55, 400, 0), "Sphere", new Vector3(10, -35, 0), false);
        imageCylinder = CreateImageFeedback("Cylinder", new Vector3(55, 400, 0), "Cylinder", new Vector3(4, -35, 0), false);
        imageRadio = CreateImageFeedback("Radio", new Vector3(165, 400, 0), "Radio", new Vector3(15, -35, 0), false);
        GameObject eventSystem = new GameObject();
        eventSystem.name = "EventSystem";
        eventSystem.AddComponent<EventSystem>();
        StandaloneInputModule inputs = eventSystem.AddComponent<StandaloneInputModule>();
    }
    private Image CreateImageFeedback(string name, Vector3 objectPosition, string text, Vector3 textPosition, bool yellow)
    {
        GameObject imageObj = new GameObject();
        imageObj.name = name;
        imageObj.transform.SetParent(canvasUI.transform);
        Image image = imageObj.AddComponent<Image>();
        image.transform.localPosition = objectPosition;
        image.rectTransform.sizeDelta = new Vector2(100, 75);
        if (yellow) image.color = Color.yellow;
        Button button = imageObj.AddComponent<Button>();
        if (ButtonEventNumber == 0 || ButtonEventNumber == 1)
            button.onClick.AddListener(delegate { RotateOrScale(); });
        else if (ButtonEventNumber == 2)
            button.onClick.AddListener(delegate { CreatePrimitive(PrimitiveType.Cube); });
        else if (ButtonEventNumber == 3)
            button.onClick.AddListener(delegate { CreatePrimitive(PrimitiveType.Sphere); });
        else if (ButtonEventNumber == 4)
            button.onClick.AddListener(delegate { CreatePrimitive(PrimitiveType.Cylinder); });
        else if (ButtonEventNumber == 5)
            button.onClick.AddListener(delegate { CreateRadio(); });
        ButtonEventNumber++;
        GameObject newTextObj = new GameObject();
        newTextObj.transform.SetParent(imageObj.transform);
        Text imageText = newTextObj.AddComponent<Text>();
        imageText.text = text;
        imageText.fontSize = 25;
        imageText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        imageText.color = Color.black;
        newTextObj.transform.localPosition = textPosition;

        return image;
    }
}
