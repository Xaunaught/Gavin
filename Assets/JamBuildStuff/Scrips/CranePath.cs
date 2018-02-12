using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class CranePath : MonoBehaviour
{

    public List<CraneNode> nodes = new List<CraneNode>();
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
[System.Serializable]
public class CraneNode
{
    public Vector3 position;
    public List<Connection> connections = new List<Connection>();
    public void Connect(CraneNode connection)
    {
        Vector3 connectDirection = (connection.position - position).normalized;
        connections.Add(new Connection() { connected = connection, direction = connectDirection });
        connection.connections.Add(new Connection() { connected = this, direction = -connectDirection });
    }
}
[System.Serializable]
public class Connection
{
    public Vector3 direction;
    public CraneNode connected;
}
