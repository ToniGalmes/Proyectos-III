﻿using System.Diagnostics.CodeAnalysis;
using UnityEngine;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class PushPullObject : MonoBehaviour
{
    public float speedWhenMove;
    public float narrowAngleToAllowMovement;
    public float wideAngleToAllowMovement;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private LayerMask _layersToDetectCollision;
    [HideInInspector] public bool canMove;
    [HideInInspector] public bool canPush;
    [HideInInspector] public bool canPull;
    [HideInInspector] public bool swordStabbed;
    [HideInInspector] public bool moving;
    [HideInInspector] public Vector3 moveVector; //This vector can be negative, it depends if it's pushing or pulling
    private BoxCollider _boxCollider;
    private Rigidbody _rb;
    private InputActions _input;
    
    private void Awake()
    {
        _boxCollider = GetComponent<BoxCollider>();
        _rb = GetComponent<Rigidbody>();
        _input = new InputActions();
        _input.Enable();
        _input.PlayerControls.Sword.started += ctx => SwordInput();
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (GetAngleBetweenForwardAndPlayer() <= GetAngleToAllowMovement())
            {
                canMove = true;
                moveVector = GetClosestVector();
                
                canPush = !PushCollision();
                canPull = !PullCollision();
            }
            else canMove = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canMove = false;
        }
    }
    
    //Moves this GameObject when the player pulls it.
    public void Pull()
    {
        _rb.velocity = moveVector.normalized * 1.15f * speedWhenMove;
    }
    
    //Moves this GameObject when the player pushes it.
    public void Push()
    {
        _rb.velocity = -moveVector.normalized * speedWhenMove;
    }

    //Gets the angle between the closest vector and the director vector
    //This is used to check if the player is in the safe face of the cube as the movement axis.
    private float GetAngleBetweenForwardAndPlayer()
    {
        Vector3 l_directionVector = GetClosestVector();
        Vector3 playerForward = GetDirectionVector();

        return Mathf.Abs(Vector3.Angle(l_directionVector, playerForward));
    }
    
    //Gets the Projected vector between the player and this object.
    private Vector3 GetDirectionVector()
    {
        Vector3 playerVector = playerTransform.position - transform.position;
        Vector3 l_directionVector = Vector3.ProjectOnPlane(playerVector, Vector3.up).normalized;

        return l_directionVector;
    }
    
    //Checks if the object is colliding when pushing
    private bool PushCollision()
    {
        return Physics.Raycast(transform.position, -GetDirectionVector(), GetColliderSize(), _layersToDetectCollision);
    }
    
    //Checks if the player is colliding when pulling
    private bool PullCollision()
    {
        return Physics.Raycast(playerTransform.position, GetDirectionVector(), 1, _layersToDetectCollision);
    }
    
    //Returns the closest vector to the player
    //This is used to set the axis of movement
    public Vector3 GetClosestVector()
    {
        Vector3[] vectors = Generate3DVectors();
        Vector3 closestVector = vectors[0];
        float closestVectorAngle = 360;

        foreach (var v in vectors)
        {
            var angle = Mathf.Abs(Vector3.Angle(v, GetDirectionVector()));
            if (angle < closestVectorAngle)
            {
                closestVector = v;
                closestVectorAngle = angle;
            }
        }

        return closestVector;
    }

    //Generates the 4 vectors of movement of the cube. 
    //Its used because the cube can be rotated in any angle.
    private Vector3[] Generate3DVectors()
    {
        Vector3[] vectors = new Vector3[4];
        
        vectors[0] = transform.forward;
        vectors[1] = -transform.forward;
        vectors[2] = transform.right;
        vectors[3] = -transform.right;

        return vectors;
    }

    //Gets the collider size from the center. This is used by the Raycast to get the distance of detection
    private float GetColliderSize()
    {
        if (GetClosestVector() == transform.forward || GetClosestVector() == -transform.forward)
        {
            return _boxCollider.size.z / 2 + 0.01f;
        }
        return _boxCollider.size.x / 2 + 0.01f;
    }

    private void SwordInput()
    {
        if (swordStabbed) swordStabbed = false;
    }

    public float GetAngleToAllowMovement()
    {
        var z = _boxCollider.size.z;
        var x = _boxCollider.size.x;
        var actualPlayerVector = GetClosestVector();


        if (z >= x)
        {
            //Its more wide in the Z axis
            if (actualPlayerVector == transform.right || actualPlayerVector == -transform.right)
            {
                //The player is in the X axis
                return wideAngleToAllowMovement;
            }
            //The player is in the Z axis
            return narrowAngleToAllowMovement;
        }
        
        //It's more wide in the X axis
        if (actualPlayerVector == transform.forward || actualPlayerVector == -transform.forward)
        {
            //The player is in the Z axis
            return wideAngleToAllowMovement;
        }
        //The player is in the X axis
        return narrowAngleToAllowMovement;
    }

    public void LockAllConstraints()
    {
        if (_rb.constraints == RigidbodyConstraints.None)
        {
            _rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    public void UnlockAllConstraints()
    {
        if (_rb.constraints == RigidbodyConstraints.FreezeAll)
        {
            _rb.constraints = RigidbodyConstraints.None;
        }
    }
}
