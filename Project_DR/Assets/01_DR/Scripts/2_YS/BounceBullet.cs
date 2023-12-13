using BNG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BounceBullet : MonoBehaviour
{
    public Rigidbody rigid;
    public DamageCollider damageCollider;

    public int BounceTableId;

    [Header("테이블 관련")]
    public float speed = default;
    public float damage = default;
    public float destoryTime = default;



    // Start is called before the first frame update
    void Start()
    {
        GetData(BounceTableId);

        rigid = GetComponent<Rigidbody>();
        rigid.velocity = transform.forward * speed;

        damageCollider.Damage = damage;
    }

    public virtual void GetData(int BounceTableId)
    {
        //6912
        speed = (float)DataManager.instance.GetData(BounceTableId, "Speed", typeof(float));
        damage = (float)DataManager.instance.GetData(BounceTableId, "Damage", typeof(float));
        destoryTime = (float)DataManager.instance.GetData(BounceTableId, "DesTime", typeof(float));
    }

    public virtual void OnCollisionEnter(Collision collision)
    {

        if (collision.collider.CompareTag("Player") || collision.collider.CompareTag("Wall"))
        {
            Destroy(this.gameObject);
        }
    }
}
