using UnityEngine;

namespace Mirage.Examples.Additive
{
    [RequireComponent(typeof(CharacterController))]
    //[RequireComponent(typeof(NetworkTransform))]
    [RequireComponent(typeof(JamesFrowen.PositionSync.SyncPositionBehaviour))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : NetworkBehaviour
    {
        public CharacterController characterController;
        public CapsuleCollider capsuleCollider;

        private void Awake()
        {
            Identity.OnStartLocalPlayer.AddListener(OnStartLocalPlayer);
        }

        private void OnValidate()
        {
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
            if (capsuleCollider == null)
                capsuleCollider = GetComponent<CapsuleCollider>();
        }

        private void Start()
        {
            capsuleCollider.enabled = IsServer;
        }

        public void OnStartLocalPlayer()
        {
            characterController.enabled = true;

            Camera.main.orthographic = false;
            Camera.main.transform.SetParent(transform);
            Camera.main.transform.localPosition = new Vector3(0f, 3f, -8f);
            Camera.main.transform.localEulerAngles = new Vector3(10f, 0f, 0f);
        }

        private void OnDisable()
        {
            if (IsLocalPlayer && Camera.main != null)
            {
                Camera.main.orthographic = true;
                Camera.main.transform.SetParent(null);
                Camera.main.transform.localPosition = new Vector3(0f, 70f, 0f);
                Camera.main.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            }
        }

        [Header("Movement Settings")]
        public float moveSpeed = 8f;
        public float turnSensitivity = 5f;
        public float maxTurnSpeed = 150f;

        [Header("Diagnostics")]
        public float horizontal;
        public float vertical;
        public float turn;
        public float jumpSpeed;
        public bool isGrounded = true;
        public bool isFalling;
        public Vector3 velocity;

        private void Update()
        {
            if (!IsLocalPlayer || !characterController.enabled)
                return;

            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");

            // Q and E cancel each other out, reducing the turn to zero
            if (Input.GetKey(KeyCode.Q))
                turn = Mathf.MoveTowards(turn, -maxTurnSpeed, turnSensitivity);
            if (Input.GetKey(KeyCode.E))
                turn = Mathf.MoveTowards(turn, maxTurnSpeed, turnSensitivity);
            if (Input.GetKey(KeyCode.Q) && Input.GetKey(KeyCode.E))
                turn = Mathf.MoveTowards(turn, 0, turnSensitivity);
            if (!Input.GetKey(KeyCode.Q) && !Input.GetKey(KeyCode.E))
                turn = Mathf.MoveTowards(turn, 0, turnSensitivity);

            if (isGrounded)
                isFalling = false;

            if ((isGrounded || !isFalling) && jumpSpeed < 1f && Input.GetKey(KeyCode.Space))
            {
                jumpSpeed = Mathf.Lerp(jumpSpeed, 1f, 0.5f);
            }
            else if (!isGrounded)
            {
                isFalling = true;
                jumpSpeed = 0;
            }
        }

        private void FixedUpdate()
        {
            if (!IsLocalPlayer || characterController == null)
                return;

            transform.Rotate(0f, turn * Time.fixedDeltaTime, 0f);

            var direction = new Vector3(horizontal, jumpSpeed, vertical);
            direction = Vector3.ClampMagnitude(direction, 1f);
            direction = transform.TransformDirection(direction);
            direction *= moveSpeed;

            if (jumpSpeed > 0)
                characterController.Move(direction * Time.fixedDeltaTime);
            else
                characterController.SimpleMove(direction);

            isGrounded = characterController.isGrounded;
            velocity = characterController.velocity;
        }
    }
}
