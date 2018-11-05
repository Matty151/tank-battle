using UnityEngine;
using System.Collections;

public class Target : MonoBehaviour
{
    private UnityEngine.AI.NavMeshAgent[] navAgents;
    public Transform targetMarker;

    void Start ()
    {
	    navAgents = FindObjectsOfType(typeof(UnityEngine.AI.NavMeshAgent)) as UnityEngine.AI.NavMeshAgent[];
		UpdateTargets (targetMarker.position);
    }

    void UpdateTargets ( Vector3 targetPosition  )
    {
	    foreach(UnityEngine.AI.NavMeshAgent agent in navAgents) 
        {
		    agent.destination = targetPosition;
	    }
    }

    void Update ()
    {
        
    }
}