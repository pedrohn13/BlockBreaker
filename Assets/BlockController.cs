using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockController : MonoBehaviour
{

    PhotonController Controller;

    void Start()
    {
        Controller = GameObject.Find("PhotonController").GetComponent<PhotonController>();
    }

    void OnMouseDown()
    {
        Controller.Point();
        Destroy(this.gameObject);
    }

    void Update()
    {
       
    }
}
