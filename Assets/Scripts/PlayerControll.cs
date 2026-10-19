using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControll : MonoBehaviour
{
    public float moveSpeed;

    private bool isMoving;

    private Vector2 input;

    private Animator animator;

    public LayerMask solidObjectsLayer;

    public LayerMask interactableLayer;

    private Vector2 gridOffset;


    private void Awake()
    {
        animator = GetComponent<Animator>();

        Vector3 p = transform.position;
        gridOffset = new Vector2(
            p.x - (Mathf.Floor(p.x) + 0.5f),
            p.y - (Mathf.Floor(p.y) + 0.5f));

    }

    public void HandleUpdate()
    {
        if (!isMoving)
        {
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");

            if (Mathf.Abs(input.x) > 0)
                input.y = 0;

            if (input != Vector2.zero)
            {
                animator.SetFloat("MoveX", input.x);
                animator.SetFloat("MoveY", input.y);

                var targetPos = transform.position;
                targetPos.x += input.x;
                targetPos.y += input.y;

                if(IsWalkable(targetPos))
                {
                    StartCoroutine(Move(targetPos));
                }

            }
        }
        animator.SetBool("isMoving", isMoving);

        if (Input.GetKeyDown(KeyCode.Z))
            Interact();
    }

    void Interact()
    {
        var facingDir = new Vector3(animator.GetFloat("MoveX"), animator.GetFloat("MoveY"));
        var interactPos = transform.position + facingDir;

        var collider = Physics2D.OverlapCircle(interactPos, 0.2f, interactableLayer);

        if(collider != null)
        {
            collider.GetComponent<Interacao>()?.Interact();
        }
    }

    public void ResetPlayer()
    {
        isMoving = false;
        input = Vector2.zero;

        if (animator != null)
        {
            animator.SetBool("isMoving", false);
            animator.SetFloat("MoveX", 0);
            animator.SetFloat("MoveY", 0);
        }

        StopAllCoroutines(); // para qualquer movimento que estiver acontecendo
    }

    public void TeleportTo(Vector3 worldPosition, Vector2 facing)
    {
        StopAllCoroutines();
        isMoving = false;
        input = Vector2.zero;

        transform.position = new Vector3(
            Mathf.Floor(worldPosition.x) + 0.5f + gridOffset.x,
            Mathf.Floor(worldPosition.y) + 0.5f + gridOffset.y,
            transform.position.z);

        if (animator != null)
        {
            animator.SetBool("isMoving", false);

            if (facing != Vector2.zero)
            {
                animator.SetFloat("MoveX", facing.x);
                animator.SetFloat("MoveY", facing.y);
            }
        }
    }
    IEnumerator Move(Vector3 targetPos)
    {
        isMoving = true;
        while ((targetPos - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;

        isMoving = false;
    }

    private bool IsWalkable(Vector3 targetPos)
    {
        if(Physics2D.OverlapCircle(targetPos, 0.2f, solidObjectsLayer | interactableLayer) != null)
        {
            return false;
        }
        return true;
    }

}