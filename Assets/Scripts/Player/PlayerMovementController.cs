﻿using System;
using System.Collections;
using UnityEngine;

public class PlayerMovementController : MonoBehaviour
{
    [Header("Movement Values")] 
    [SerializeField] private float minSpeed;
    [SerializeField] private float maxSpeed;
    [SerializeField] private float jumpSpeed;
    [SerializeField] private float speedMultiplierWhenJump;
    [SerializeField] private float gravityMultiplier;
    [SerializeField] private float joystickDeadZone;

    [Header("Edge Values")] 
    [SerializeField] private Vector3 edgeOffset;
    [SerializeField] private Vector3 edgeCompletedOffset;
    [SerializeField] private float lerpVelocity;
    [HideInInspector] public Vector3 edgePosition;
    [HideInInspector] public GameObject edgeGameObject;
    
    //Player Inputs
    private InputActions _input;
    private Vector2 movementVector;

    //Objects
    private PlayerSwordScanner _scannerSword;

    //Components
    private CharacterController _characterController;

    //Variables
    [HideInInspector] public bool _inputMoveObject;

    private bool _jump;
    private bool _onGround;
    private bool _onEdge;
    private bool _edgeAvailable;
    private bool _standing;
    private bool _inputToStand;
    private float _verticalSpeed;
    private Transform _cameraTransform;
    private PushPullObject _moveObject;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _cameraTransform = Camera.main.gameObject.transform;
        _input = new InputActions();
        _scannerSword = FindObjectOfType<PlayerSwordScanner>();

        _input.PlayerControls.Move.performed += callbackContext => movementVector = callbackContext.ReadValue<Vector2>();
        _input.PlayerControls.Jump.started += callbackContext => JumpInput();
        _input.PlayerControls.MoveObject.started += callbackContext => _inputMoveObject = true;
        _input.PlayerControls.MoveObject.canceled += callbackContext => _inputMoveObject = false;
    }

    private void OnEnable()
    {
        _input.Enable();
    }

    private void OnDisable()
    {
        _input.Disable();
    }

    private void Update()
    {
        var vector3D = PlayerUtils.RetargetVector(movementVector, _cameraTransform, joystickDeadZone);
        if(!_onEdge) RotateTowardsForward(vector3D);
        
        vector3D *= Mathf.Lerp(minSpeed, maxSpeed, movementVector.magnitude);

        #region Push & Pull
        
        if (_moveObject && _moveObject.canMove && _inputMoveObject && !_scannerSword.UsingScannerInHand() && vector3D.magnitude >= joystickDeadZone)
        {
            if (PlayerUtils.InputDirectionTolerance(_moveObject.moveVector, _moveObject.angleToAllowMovement, _cameraTransform, movementVector) && _moveObject.canPull)
            {
                _characterController.Move(_moveObject.moveVector * (_moveObject.speedWhenMove * Time.deltaTime));
                _moveObject.Pull();
            }

            if (PlayerUtils.InputDirectionTolerance(-_moveObject.moveVector, _moveObject.angleToAllowMovement, _cameraTransform, movementVector) && _moveObject.canPush)
            {
                _characterController.Move(-_moveObject.moveVector * (_moveObject.speedWhenMove * Time.deltaTime));
                _moveObject.Push();
            }

            if (!_moveObject.moving)
            {
                var scannerIntersect = FindObjectOfType<ScannerIntersectionManager>();
                
                if(_moveObject.swordStabbed) scannerIntersect.DeleteIntersections();
                else scannerIntersect.CheckIntersections(_moveObject.GetComponent<BoxCollider>());
                _moveObject.moving = true;
            }
            return;
        }
        else if (_moveObject && vector3D.magnitude < joystickDeadZone)
        {
            _moveObject.moving = false;
        }
        
        #endregion
        #region Jump

        if (_onGround && _jump)
        {
            _jump = false;
            
            if (_edgeAvailable)
            {
                _onEdge = true;
                _verticalSpeed = 0;
            }
            else _verticalSpeed = jumpSpeed;
        }

        if (_verticalSpeed < -0.2f && _edgeAvailable)
        {
            _onEdge = true;
            _verticalSpeed = 0;
        }
        

        if (!_onEdge)
        {
            _verticalSpeed += Physics.gravity.y * Time.deltaTime * gravityMultiplier;
            vector3D.y = _verticalSpeed;
            
            if (!_onGround) //Reduce Speed when jump
            {
                vector3D.x = vector3D.x * speedMultiplierWhenJump;
                vector3D.z = vector3D.z * speedMultiplierWhenJump;
            }
            
            var collisionFlags = _characterController.Move(vector3D * Time.deltaTime);

            if ((collisionFlags & CollisionFlags.Below) != 0)
            {
                _onGround = true;
                _verticalSpeed = 0;
            }
            else _onGround = false;

            if ((collisionFlags & CollisionFlags.Above) != 0 && _verticalSpeed > 0) _verticalSpeed = 0;
        }
        else
        {
            var position = transform.position;
            Vector3 moveVector = Vector3.zero;
            
            Vector3 lVector = Vector3.Lerp(position, edgePosition + edgeOffset, lerpVelocity);
            
            moveVector = new Vector3(0, (lVector - position).y, 0);
            
            RotateTowardsForward(-edgeGameObject.transform.forward);
            
            if(_characterController.enabled) _characterController.Move(moveVector);
        }
        

        #endregion
        #region Edge

        if (_onEdge)
        {
            var projectedVector = Vector3.ProjectOnPlane(transform.position - edgePosition, edgeGameObject.transform.forward);
            projectedVector = Vector3.ProjectOnPlane(projectedVector, transform.up);

            if (PlayerUtils.InputEqualVector(-edgeGameObject.transform.forward, _cameraTransform, movementVector) && !_standing || _inputToStand)
            {
                StartCoroutine(Co_StandEdge(projectedVector + edgePosition  + edgeCompletedOffset));
            }
            if (PlayerUtils.InputEqualVector(edgeGameObject.transform.forward, _cameraTransform, movementVector) && !_standing)
            {
                _onEdge = false;
                _edgeAvailable = false;
            }
        }
        
        #endregion
    }

    private void RotateTowardsForward(Vector3 forward)
    {
        transform.LookAt(transform.position + forward);
    }

    public void OnEdge()
    {
        _edgeAvailable = true;
    }

    public void OffEdge()
    {
        _edgeAvailable = false;
    }

    private IEnumerator Co_StandEdge(Vector3 finalPos)
    {
        _characterController.enabled = false;
        _standing = true;
        _inputToStand = false;
        var rb =gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        
        //Vertical Movement
        while (Math.Abs(finalPos.y - transform.position.y) > _characterController.height)
        {
            var lerpVector = Vector3.Lerp(transform.position, finalPos, lerpVelocity);
            var moveVector = new Vector3(0, (lerpVector - transform.position).y, 0);
            transform.Translate(moveVector);
            
            yield return null;
        }
        
        //Horizontal Movement
        while ((transform.position - finalPos).magnitude >  _characterController.height)
        {
            var moveVector = new Vector3(finalPos.x, transform.position.y, finalPos.z);
            transform.position = Vector3.MoveTowards(transform.position, moveVector, lerpVelocity);
            yield return null;
        }

        _standing = false;
        _onEdge = false;
        Destroy(rb);
        _characterController.enabled = true;
    }

    private void JumpInput()
    {
        if (!_onEdge && _onGround) _jump = true;
        if (!_standing && _onEdge) _inputToStand = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MoveObject") && other.transform.parent != null)
        {
            _moveObject = other.transform.parent.GetComponent<PushPullObject>();
        }
    }
}
