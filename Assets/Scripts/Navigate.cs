using UnityEngine;
using UnityEngine.AI;

// Helper for agent navigation
public class Navigate : MonoBehaviour
{
    private NavMeshAgent agent;

    // Assign desired postion to current_room for navigation
    public GameObject current_room = null;

    void Start()
    {
        // Surface the agent can walk on
        agent = GetComponent<NavMeshAgent>();

        // current_room must be assigned, so set it to agent's starting position
        current_room = transform.gameObject;
    }

    void Update()
    {
        // Agent navigates to current_room
        agent.SetDestination(current_room.transform.position);
    }
}