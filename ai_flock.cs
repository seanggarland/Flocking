using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ai_flock : MonoBehaviour
{
    public GameObject AgentToInstantiate;
    public bool InitVelocityZero = false;
    public int flockSize;
    public float maxSpeed = 10.0f;
    public float safeRadius = 1.0f;
    public float flockRadius = 5.0f;

    [Range(0, 1)]
    public float AlignmentStrength;
    [Range(0, 1)]
    public float CohesionStrength;
    [Range(0, 1)]
    public float SeparationStrength;

    public List<ai_flock_agent> Agents;
    public List<GameObject> AgentObjs;
    public Vector3 AveragePosition;
    public Vector3 TargetPosition;
    public Vector3 AverageForward;

    // Initialization of members.
    void Start()
    {
        TargetPosition = Vector3.zero;
        AverageForward = Vector3.zero;
        AveragePosition = Vector3.zero;
        Agents = new List<ai_flock_agent>();
        AgentObjs = new List<GameObject>();
    }

    // Destroy all agents in the flock.
    public void DestroyAll()
    {
        Agents.Clear();

        for (int i = 0; i < AgentObjs.Count; i++)
        {
            if (AgentObjs[i])
            {
                if (AgentObjs[i].GetComponent<ai_lifeControl>())
                    AgentObjs[i].GetComponent<ai_lifeControl>().Kill();
                else
                    Destroy(AgentObjs[i]);
            }
        }

        AgentObjs.Clear();

        AveragePosition = TargetPosition;
    }

    // Add a new set of agents to the flock based on "flockSize."
    public void AddToFlock()
    {
        for (int i = 0; i < flockSize; i++)
        {
            GameObject _agent = (GameObject)Instantiate(AgentToInstantiate, transform.position, transform.rotation);
            AgentObjs.Add(_agent);
            ai_flock_agent _ai = _agent.GetComponent<ai_flock_agent>();
            _ai.maxSpeed = maxSpeed;
            _ai.safeRadius = safeRadius;
            _agent.transform.position = transform.position + new Vector3(Random.Range(-flockRadius, flockRadius), Random.Range(-flockRadius, flockRadius), Random.Range(-flockRadius, flockRadius));
            if (!InitVelocityZero)
                _ai.GetComponent<Rigidbody>().velocity = new Vector3(Random.Range(-maxSpeed, maxSpeed), Random.Range(-maxSpeed, maxSpeed), Random.Range(-maxSpeed, maxSpeed));
            else
                _ai.GetComponent<Rigidbody>().velocity = Vector3.zero;
            _ai.Update();

            Agents.Add(_ai);
        }
    }

    void CalculateAverages()
    {
        // Zero out our persistent averages before we re-calculate.
        AverageForward = AveragePosition = Vector3.zero;

        int size = Agents.Count;

        // Add up the values.
        for (int i = 0; i < size; i++)
        {
            if (Agents[i])
            {
                AverageForward += Agents[i]._rigidbody.velocity;
                AveragePosition += Agents[i].transform.position;
            }
        }

        // Divide by the number to get the average.
        AverageForward /= size;
        AveragePosition /= size;
    }

    // Returns an acceleration Vector based upon the average forward of the flock,
    // the current strength of Allignment, and the max speed of the agent.
    Vector3 CalculateAlignmentAcceleration(ai_flock_agent _agent)
    {
        Vector3 _forward = AverageForward;

        _forward /= _agent.maxSpeed;
        if (_forward.magnitude > 1)
            _forward = _forward.normalized;

        return _forward * AlignmentStrength;
    }

    // Returns a target direction Vector based upon the desired cohesion point of the flock,
    // and the strength of Cohesion.
    //      *In flocking, the CohesionPoint is usually the "AveragePosition" of the flock.
    //          However, in this example, we use the "Mother Ship" as our cohesion point.*
    Vector3 CalculateCohesionAcceleration(ai_flock_agent _agent, Vector3 CohesionPoint)
    {
        Vector3 _toTarget = CohesionPoint - _agent.transform.position;
        float _distance = _toTarget.magnitude;

        _toTarget = _toTarget.normalized;

        if (_distance < flockRadius)
            _toTarget *= (_distance / flockRadius);

        return _toTarget * CohesionStrength;
    }

    // Returns a separation Vector based upon the distance between the current agent and each agent in the flock,
    // and the current strength of Separation.
    Vector3 CalculateSeparationAcceleration(ai_flock_agent _agent)
    {
        Vector3 _sum = Vector3.zero;

        for (int i = 0; i < Agents.Count; i++)
        {
            // if we are not looking at ourself:
            if (Agents[i] != _agent && Agents[i])
            {
                Vector3 _toAgent = _agent.transform.position - Agents[i].transform.position;
                float _distance = _toAgent.magnitude;
                float _safeDistance = _agent.safeRadius + Agents[i].safeRadius;

                // If we're too close to this agent,
                // we need to modify the outgoing vector by a ratio based upon our desired distances.
                if (_distance < _safeDistance)
                {
                    _toAgent = _toAgent.normalized;
                    _toAgent *= ((_safeDistance - _distance) / _safeDistance);
                    _sum += _toAgent;
                }
            }
        }

        if (_sum.magnitude > 1.0f)
            _sum = _sum.normalized;

        return _sum * SeparationStrength;
    }

    // Update is called once per frame
    void Update()
    {
        if (Agents.Count > 0)
        {
            // Calculates averages for the position and forward vector of the Flock.
            CalculateAverages();

            // Each agent's rigidbody needs to be given the updated average information.
            for (int i = 0; i < Agents.Count; i++)
            {
                ai_flock_agent _agent = Agents[i];
                // Paranoia check to remove any unwanted null references.
                if (!_agent)
                {
                    Agents.Remove(_agent);
                    continue;
                }

                // Acceleration for each agent is calculated based upon 3 values:
                // Alignment, Cohesion, and Separation.
                // These are calculated using the averages found above.
                Vector3 _acceleration = CalculateAlignmentAcceleration(_agent);
                _acceleration += CalculateCohesionAcceleration(_agent, TargetPosition);
                _acceleration += CalculateSeparationAcceleration(_agent);

                float _accelerationMultiplier = _agent.maxSpeed;

                _acceleration *= _accelerationMultiplier * Time.deltaTime;

                // Update the velocity of the agent, and lerp their forward towards the "target" (Gives an organic feel to the flying agent).
                _agent._rigidbody.velocity += _acceleration;

                Vector3 _forward = _agent.transform.forward;
                _forward = Vector3.Lerp(_forward, (TargetPosition - _agent.transform.position).normalized, 5 * Time.deltaTime);
                _agent.transform.forward = _forward;
            }
        }
    }

    void OnGUI()
    {
        float screenX = 1.0f * Screen.width / 1920;
        float screenY = 1.0f * Screen.height / 1080;

        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(screenX, screenY, 1.0f));

        if (GUI.Button(new Rect(192, 138, 200, 60), "SWARM"))
        {
            AlignmentStrength = 0.0f;
            SeparationStrength = 0.0f;
            for (int i = 0; i < Agents.Count; i++)
            {
                if (Agents[i])
                {
                    Agents[i]._rigidbody.velocity = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f)) * Agents[i].maxSpeed;
                }
            }
        }

        if (GUI.Button(new Rect(192, 138 + 80, 200, 60), "KITE"))
        {
            AlignmentStrength = 1.0f;
            SeparationStrength = 1.0f;
        }

        flockSize = int.Parse(GUI.TextField(new Rect(192, 288 + 50, 200, 20), flockSize.ToString(), 3));

        if (GUI.Button(new Rect(192, 288 + 80, 200, 60), "ADD TO FLEET"))
        {
            AddToFlock();
        }

        if (GUI.Button(new Rect(192, 288 + 80 + 80, 200, 60), "DESTROY ALL"))
        {
            DestroyAll();
        }
    }
}
