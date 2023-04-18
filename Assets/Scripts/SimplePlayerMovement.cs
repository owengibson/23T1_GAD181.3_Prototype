using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace MeadowMateys
{
    public class SimplePlayerMovement : MonoBehaviour
    {
        [SerializeField] private KeyCode leftKey, rightKey, jumpKey, crouchKey, increaseRopeKey, decreaseRopeKey, attachRopeKey;
        [SerializeField] private float moveSpeed;
        [SerializeField] private float jumpStrength;
        [SerializeField] private float climbSpeed;
        [SerializeField] private float maxDistance;
        [SerializeField] private LayerMask levelLayerMask;
        [SerializeField] private Transform otherPlayer;
        [SerializeField] private bool isRopeAttached = false;

        private bool _isLadder;
        private bool _isClimbing;
        private bool _isRopeAttachOnCooldown = false;
        private float _distance;

        private Rigidbody2D _rigidbody;
        private SpriteRenderer _spriteRenderer;
        private BoxCollider2D _boxCollider2D;
        private Animator _animator;
        private LineRenderer _lineRenderer;

        private void Start()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _boxCollider2D = GetComponent<BoxCollider2D>();
            _animator = GetComponent<Animator>();
            _lineRenderer = GetComponentInChildren<LineRenderer>();
        }
        private void Update()
        {
            if (_isLadder && (Input.GetKey(jumpKey) || Input.GetKey(crouchKey)))
            {
                _isClimbing = true;
            }

            _distance = Vector2.Distance(transform.position, otherPlayer.position);
            if (Input.GetKeyDown(attachRopeKey) && !_isRopeAttachOnCooldown && _distance < 5f && _lineRenderer != null)
            {
                StartCoroutine(CooldownRopeAttach());
                Debug.Log("Attaching rope");
                maxDistance = 5f;
                isRopeAttached = !isRopeAttached;
                _lineRenderer.enabled = !_lineRenderer.enabled;
            }
        }
        private void FixedUpdate()
        {
            Vector3 currentMove = Vector3.zero;

            float lastXPos;
            lastXPos = transform.position.x;

            if (Input.GetKey(rightKey)) // Move right
            {
                currentMove = new Vector3(moveSpeed, 0, 0);
                _spriteRenderer.flipX = false;
            }
            else if (Input.GetKey(leftKey)) // Move left
            {
                currentMove = new Vector3(-moveSpeed, 0, 0);
                _spriteRenderer.flipX = true;
            }

            //--- Rope control ---//
            if (isRopeAttached)
            {
                transform.position += currentMove;
                _distance = Vector2.Distance(transform.position, otherPlayer.position);
                if (_distance >= maxDistance)
                {
                    otherPlayer.position += currentMove;
                }
                _distance = Vector2.Distance(transform.position, otherPlayer.position);
                while (_distance >= maxDistance)
                {
                    transform.position = Vector3.MoveTowards(transform.position, otherPlayer.position, moveSpeed);
                    _distance = Vector2.Distance(transform.position, otherPlayer.position);
                }

                if(Input.GetKey(increaseRopeKey) && maxDistance <= 5f)
                {
                    maxDistance += 0.1f;
                    Debug.Log(maxDistance);
                }
                else if (Input.GetKey(decreaseRopeKey) && maxDistance >= 2f)
                {
                    maxDistance -= 0.1f;
                    Debug.Log(maxDistance);
                }
            }
            else transform.position += currentMove;

            //--- Jump and climb ---//
            if (Input.GetKey(jumpKey) && IsGrounded() && !_isClimbing) // Jump
            {
                _rigidbody.velocity = new Vector2(_rigidbody.velocity.x, jumpStrength);
            }
            else if (Input.GetKey(jumpKey) && _isClimbing) // Climb up
            {
                _rigidbody.gravityScale = 0f;
                _rigidbody.velocity = new Vector2(_rigidbody.velocity.x, climbSpeed);
            }
            else if (Input.GetKey(crouchKey) && _isClimbing) // Climb down
            {
                _rigidbody.gravityScale = 0f;
                _rigidbody.velocity = new Vector2(_rigidbody.velocity.x, -climbSpeed);
            }

            //---------ANIMATION CONTROL-----------//

            //Walk animation
            if (transform.position.x != lastXPos)
            {
                _animator.SetFloat("Speed", 1);
            }
            else _animator.SetFloat("Speed", 0);

            //Jump animation
            if (Mathf.Abs(_rigidbody.velocity.y) > 0.01f && !_isClimbing)
            {
                _animator.SetBool("isJumping", true);
            }
            else _animator.SetBool("isJumping", false);

            //Climb animation
            if (Mathf.Abs(_rigidbody.velocity.y) > 0.01f && _isClimbing)
            {
                _animator.SetBool("isClimbing", true);
            }
            else _animator.SetBool("isClimbing", false);
        }

        private bool IsGrounded()
        {
            RaycastHit2D hit = Physics2D.Raycast(_boxCollider2D.bounds.center, Vector2.down, _boxCollider2D.bounds.extents.y + 0.02f, levelLayerMask);
            return hit.collider != null;
        }
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Ladder"))
            {
                _isLadder = true;
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.CompareTag("Ladder"))
            {
                _isLadder = false;
                _isClimbing = false;
                _rigidbody.gravityScale = 3f;
            }
        }

        private IEnumerator CooldownRopeAttach()
        {
            _isRopeAttachOnCooldown = true;
            yield return new WaitForSecondsRealtime(2f);
            _isRopeAttachOnCooldown = false;
            yield break;
        }
    }
}