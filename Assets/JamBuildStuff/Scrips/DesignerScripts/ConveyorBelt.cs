using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConveyorBelt : MonoBehaviour {
    public BoxCollider moveArea;
    public Vector3 direction;
    public float speed;
    private Renderer rend;
    private Vector2 offset;
    private Vector2 tiling;

    private void OnTriggerStay(Collider other)
    {
        if (other.tag=="Robot" || other.tag == "Moveable")
        {
            other.gameObject.transform.Translate(direction*Time.deltaTime*speed,Space.World);
        }
    }

    private void Start()
    {
        rend = GetComponent<Renderer>();
        tiling = rend.material.GetTextureScale("_MainTex");
        Debug.Log(tiling);
    }

    private void Update()
    {
        offset.y -= Time.deltaTime * speed * direction.x / tiling.x / Vector3.Project(rend.bounds.extents, direction.normalized).magnitude /*the half is because the belt indicator's tiling is x2 */;
        offset.x -= Time.deltaTime * speed * direction.z / tiling.y/ Vector3.Project(rend.bounds.extents, direction.normalized).magnitude;
        rend.material.SetTextureOffset("_MainTex", new Vector2(offset.x, offset.y));
    }
    public void Trigger()
    {
        speed *= -1f;
    }

}
