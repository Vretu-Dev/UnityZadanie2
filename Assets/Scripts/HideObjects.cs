using System.Collections.Generic;
using UnityEngine;

public class HideObjects : MonoBehaviour
{
    public GameObject[] signs;

    void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            SetSignsActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            SetSignsActive(false);
        }
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player");
    }

    private void SetSignsActive(bool active)
    {
        if (signs == null) return;

        foreach (GameObject sign in signs)
        {
            if (sign != null)
            {
                sign.SetActive(active);
            }
        }
    }
}
