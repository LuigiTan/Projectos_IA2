using UnityEngine;
using UnityEngine.InputSystem;

public class ElCanelo : MonoBehaviour
{
    private BasicScav bob;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameObject.TryGetComponent(out bob);
    }

    // Update is called once per frame
    void Update()
    {
        if(Mouse.current.rightButton.wasPressedThisFrame)
        {
            bob.GetHit();
        }
    }
}
