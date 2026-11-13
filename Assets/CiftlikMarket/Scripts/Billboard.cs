using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>Nesneyi her kare kameraya döndürür (sabır barı gibi).</summary>
    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam) transform.rotation = cam.transform.rotation;
        }
    }
}
