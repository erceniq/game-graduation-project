using UnityEngine;


public class PlayerInteraction : MonoBehaviour
{
    [Header("Essentials")]
    PlayerInputHandler inputHandler;
    CharacterController controller;

    [Header("OverlapSphere")]
    public float sphereRadius = 1f;
    public LayerMask interactableMask;
    private void Start()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Interact();
    }

    private void Interact()
    {

        Vector3 castPos = controller.transform.TransformPoint(controller.center);

        if (inputHandler.interacted)
        {
            Collider[] interactables = Physics.OverlapSphere(castPos,sphereRadius,interactableMask);

            float currentClosestInteractable = sphereRadius;
            Collider chosenInteraction = null;
            foreach (Collider interactable in interactables)
            {
                if (!interactable.TryGetComponent(out IInteractable _))continue;

                float closestInteractable = Vector3.Distance(castPos, interactable.transform.position);
                if (closestInteractable < currentClosestInteractable)
                {
                    chosenInteraction = interactable;
                    currentClosestInteractable = closestInteractable;
                }
            }
            if (chosenInteraction != null)
            {
                if (chosenInteraction.TryGetComponent(out IInteractable interactable) ||
                    (interactable = chosenInteraction.GetComponentInParent<IInteractable>()) != null)
                {
                    interactable.Interact();
                    inputHandler.interacted = false;
                }
            }
        }
    }
}
