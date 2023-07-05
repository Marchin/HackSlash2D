using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;

public class CharacterMovement : MonoBehaviour {
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private float _speed;
    [SerializeField] private float _airDrag;
    [SerializeField] private float _airAcceleration;
    [SerializeField] private float _maxAirSpeed;
    [SerializeField] private float _slopAngle;
    [SerializeField] private float _jumpHeight;
    [SerializeField] private float _jumpPeakDuration = 1f;
    [SerializeField] private float _fallMultiplier = 1f;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private InputActionAsset _actionAssets;
    private List<GameObject> _groundPoints = new List<GameObject>(3);
    private bool _grounded => _groundPoints.Count > 0;
    private InputActionMap _gameplayActionMap;
    private InputAction _moveAction;
    private float _jumpSpeed;
    private float _minYSlope;

    private void Awake() {
        _actionAssets.Enable();
        _gameplayActionMap = _actionAssets.FindActionMap("Gameplay");
        _moveAction = _gameplayActionMap.FindAction("Movement");
        _gameplayActionMap.FindAction("Jump").performed += Jump;
        RefreshJumpSpeed();
    }

    private void OnValidate() {
        RefreshJumpSpeed();
        _minYSlope = Mathf.Cos(_slopAngle);
    }

    private void RefreshJumpSpeed() {
        Debug.Assert(_jumpPeakDuration > 0f, "Jump peak duration should be greater than 0");
        _jumpSpeed = _jumpHeight / _jumpPeakDuration;
    }

    private void OnDestroy() {
        _gameplayActionMap.FindAction("Jump").performed -= Jump;
    }

    private async void Jump(InputAction.CallbackContext context) {
        if (_grounded) {
            _rb.velocity = new Vector2(_rb.velocity.x, _jumpSpeed);
            await UniTask.Delay((int)(_jumpPeakDuration * 1000f));
            _rb.velocity = new Vector2(_rb.velocity.x, 0f);
        }
    }

    private void Update() {
        _rb.gravityScale = (_rb.velocity.y > 0.5f) ? 0f : _fallMultiplier;
    }

    private void FixedUpdate() {
        float horizontal = _moveAction.ReadValue<float>();

        if (_grounded) {
            _rb.velocity = new Vector2(horizontal * _speed, _rb.velocity.y);
        } else if ((Mathf.Abs(_rb.velocity.x) > _maxAirSpeed) && 
            (Mathf.Sign(horizontal) == Mathf.Sign(_rb.velocity.x))
        ) {
            var velocity = _rb.velocity;
            velocity.x -= _airDrag * Mathf.Sign(velocity.x) * Time.fixedDeltaTime;
            if (velocity.x > 0) {
                Mathf.Max(velocity.x, _maxAirSpeed);
            } else {
                Mathf.Min(velocity.x, _maxAirSpeed);
            }
            _rb.velocity = velocity;
        } else if (horizontal != 0f) {
            var velocity = _rb.velocity;
            velocity.x += _airAcceleration * Mathf.Sign(horizontal) * Time.fixedDeltaTime;
            velocity.x = Mathf.Clamp(velocity.x, -_maxAirSpeed, _maxAirSpeed);
            _rb.velocity = velocity;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        if ((1 << collision.gameObject.layer) == _groundLayer.value) {
            bool fromAbove = false;
            foreach (var contactPoint in collision.contacts) {
                if (contactPoint.normal.y >= _minYSlope) {
                    fromAbove = true;
                    break;
                }
            }
            if (fromAbove) {
                _groundPoints.Add(collision.gameObject);
            }
        }
    }
    
    private void OnCollisionExit2D(Collision2D collision) {
        if (_groundPoints.Contains(collision.gameObject)) {
            _groundPoints.Remove(collision.gameObject);
        }
    }
}
