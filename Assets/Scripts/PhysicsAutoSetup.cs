using UnityEngine;

public class PhysicsAutoSetup : MonoBehaviour
{
    public string[] targetTags = { "Grab", "HeavyGrab" };

    void Start()
    {
        foreach (string tag in targetTags)
        {
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in taggedObjects)
            {
                // add Collider if missing
                if (obj.GetComponent<Collider>() == null)
                {
                    if (obj.GetComponent<MeshFilter>() != null)
                    {
                        MeshCollider meshCol = obj.AddComponent<MeshCollider>();
                        meshCol.convex = true;
                    }
                    else
                    {
                        obj.AddComponent<BoxCollider>();
                    }

                    // Debug.Log($"Added Collider to {obj.name}");
                }

                // add Rigidbody if missing
                if (obj.GetComponent<Rigidbody>() == null)
                {
                    Rigidbody rb = obj.AddComponent<Rigidbody>();
                    rb.useGravity = true;
                    rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;

                    // Debug.Log($"Added Rigidbody to {obj.name}");
                }
            }
        }
    }
}
