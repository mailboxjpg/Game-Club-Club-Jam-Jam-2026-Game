using UnityEngine;

[RequireComponent(typeof(CharacterController2D))]
public class CharacterAnimator : MonoBehaviour
{
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");
    private static readonly int IsDashingHash = Animator.StringToHash("IsDashing");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    [SerializeField] private Animator animator;
    [SerializeField] private float walkAnimationSpeed = 1f;
    [SerializeField] private float crouchWalkAnimationSpeed = 0.5f;
    [SerializeField] private float runAnimationSpeed = 2f;

    private CharacterController2D _characterController;

    private void Start()
    {
        _characterController = GetComponent<CharacterController2D>();
        _characterController.OnJumped += PlayJump;
    }

    private void OnDestroy()
    {
        _characterController.OnJumped -= PlayJump;
    }

    private void Update()
    {
        float characterMoveSpeed = Mathf.Abs(_characterController.Velocity.x);
        bool isMoving = characterMoveSpeed > 0.1f;
        float animateMoveSpeed = 1f;
        if (isMoving)
        {
            if (_characterController.IsRunning)
            {
                animateMoveSpeed = runAnimationSpeed;
            }
            else if (_characterController.IsCrouching)
            {
                animateMoveSpeed = crouchWalkAnimationSpeed;
            }
            else
            {
                animateMoveSpeed = walkAnimationSpeed;
            }
        }
        
        animator.SetBool(IsMovingHash, isMoving);
        animator.SetFloat(MoveSpeedHash, animateMoveSpeed);
        animator.SetBool(IsCrouchingHash, _characterController.IsCrouching);
        animator.SetBool(IsDashingHash, _characterController.IsDashing);
    }

    private void PlayJump()
    {
        animator.SetTrigger(JumpHash);
    }
}
