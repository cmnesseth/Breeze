using UnityEngine;
using UnityEngine.Events;


// Abstract class allows this script to become a template for different objects to have different interaction results
public abstract class Interactable : MonoBehaviour
{

    public string promptMessage;

    public UnityEvent onInteraction;

    // Interaction() is called by the player
    public void Interaction()
    {
        Interact();
    }

    // Template method to be overridden by subclasses
    protected virtual void Interact()
    {

    }
}
