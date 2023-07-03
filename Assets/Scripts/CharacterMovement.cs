using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterMovement : MonoBehaviour {
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private float _speed;
    [SerializeField] private float _jumpHeight;
    [SerializeField] private float _jumpPeakDuration = 1f;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private InputActionAsset _actionAssets;
    private List<GameObject> _groundPoints = new List<GameObject>(3);
    private bool _grounded => _groundPoints.Count > 0;
    private InputActionMap _gameplayActionMap;
    private InputAction _moveAction;
    private float _jumpSpeed;

    private void Awake() {
        _actionAssets.Enable();
        _gameplayActionMap = _actionAssets.FindActionMap("Gameplay");
        _moveAction = _gameplayActionMap.FindAction("Movement");
        _gameplayActionMap.FindAction("Jump").performed += Jump;
        Debug.Assert(_jumpPeakDuration > 0f, "Jump peak duration should be greater than 0");
        _jumpSpeed = (_jumpHeight - (0.5f * (_rb.gravityScale * Physics2D.gravity.y) * (_jumpPeakDuration * _jumpPeakDuration))) / _jumpPeakDuration;
    }

    private void OnDestroy() {
        _gameplayActionMap.FindAction("Jump").performed -= Jump;
    }

    private void Jump(InputAction.CallbackContext context) {
        if (_grounded) {
            _rb.velocity = new Vector2(_rb.velocity.x, _jumpSpeed);
        }
    }

    private void FixedUpdate() {
        float horizontal = _moveAction.ReadValue<float>();

        _rb.velocity = new Vector2(horizontal * _speed, _rb.velocity.y);
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        if ((1 << collision.gameObject.layer) == _groundLayer.value) {
            _groundPoints.Add(collision.gameObject);
        }
    }
    
    private void OnCollisionExit2D(Collision2D collision) {
        if ((1 << collision.gameObject.layer) == _groundLayer.value) {
            _groundPoints.Remove(collision.gameObject);
        }
    }
}
