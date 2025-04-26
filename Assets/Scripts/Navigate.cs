using UnityEngine;
using UnityEngine.AI;

// Helper for agent navigation
public class Navigate : MonoBehaviour
{
    private NavMeshAgent agent;

    private GameObject _current_room = null;
    public GameObject current_room
    {
        get => _current_room;
        set
        {
            if (_current_room != value)
            {
                _current_room = value;
                hasReached = false; // Reset so arrival event can fire
            }
        }
    }

    private bool hasReached = false;
    private AgentDialog agentDialog;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agentDialog = GetComponent<AgentDialog>();
        current_room = transform.gameObject;
    }

    void Update()
    {
        if (current_room != null)
        {
            agent.SetDestination(current_room.transform.position);

            // Check if agent has reached the destination
            if (!agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance &&
                (!agent.hasPath || agent.velocity.sqrMagnitude == 0f))
            {
                if (!hasReached)
                {
                    hasReached = true;
                    if (agentDialog != null)
                        agentDialog.OnAgentReachedDestination();
                }
            }
            else
            {
                hasReached = false;
            }
        }
    }
}
