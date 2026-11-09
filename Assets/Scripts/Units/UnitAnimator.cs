using UnityEngine;

// Pequeño envoltorio sobre Animator: reproduce estados por nombre ("Idle", "Run", "Attack"...)
// y voltea el sprite según la dirección. Los Animator Controllers los genera
// el menú "Tiny Swords > 1. Configurar sprites y animaciones".
public class UnitAnimator : MonoBehaviour
{
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    string current;

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public string Current => current;

    public void Play(string state, bool restart = false)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        if (!restart && state == current) return;

        int hash = Animator.StringToHash(state);
        if (!animator.HasState(0, hash)) return;

        animator.Play(hash, 0, 0f);
        current = state;
    }

    public bool HasState(string state)
    {
        return animator != null && animator.runtimeAnimatorController != null
            && animator.HasState(0, Animator.StringToHash(state));
    }

    public void Face(float directionX)
    {
        if (spriteRenderer == null || Mathf.Abs(directionX) < 0.01f) return;
        spriteRenderer.flipX = directionX < 0;
    }

    public void SetVisible(bool visible)
    {
        if (spriteRenderer != null) spriteRenderer.enabled = visible;
    }
}
