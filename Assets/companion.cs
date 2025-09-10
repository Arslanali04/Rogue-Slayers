using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
         
    }

    // This function will be called when the button is clicked
    public void RunAnimation()
    {
        animator.SetBool("isRunning", true);
    }

    // Optional: stop running
    public void StopRunning()
    {
        animator.SetBool("isRunning", false);
    }
}
