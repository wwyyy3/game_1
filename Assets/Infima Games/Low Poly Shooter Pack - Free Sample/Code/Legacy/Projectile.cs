using System;
using UnityEngine;
using System.Collections;
using InfimaGames.LowPolyShooterPack;
using Random = UnityEngine.Random;

public class Projectile : MonoBehaviour
{

    [Range(5, 100)]
    [Tooltip("After how long time should the bullet prefab be destroyed?")]
    public float destroyAfter;
    [Tooltip("If enabled the bullet destroys on impact")]
    public bool destroyOnImpact = false;
    [Tooltip("Minimum time after impact that the bullet is destroyed")]
    public float minDestroyTime;
    [Tooltip("Maximum time after impact that the bullet is destroyed")]
    public float maxDestroyTime;

    [Header("Impact Effect Prefabs")]
    public Transform[] bloodImpactPrefabs;
    public Transform[] metalImpactPrefabs;
    public Transform[] dirtImpactPrefabs;
    public Transform[] concreteImpactPrefabs;

    private void Start()
    {
        //Grab the game mode service, we need it to access the player character!
        var gameModeService = ServiceLocator.Current.Get<IGameModeService>();
        //Ignore the main player character's collision. A little hacky, but it should work.
        Physics.IgnoreCollision(gameModeService.GetPlayerCharacter().GetComponent<Collider>(), GetComponent<Collider>());

        //Start destroy timer
        StartCoroutine(DestroyAfter());
    }

    //If the bullet collides with anything
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Bullet hit: " + collision.gameObject.name);
        //Ignore collisions with other projectiles.
        if (collision.gameObject.GetComponent<Projectile>() != null)
            return;

        // //Ignore collision if bullet collides with "Player" tag
        if (collision.gameObject.CompareTag("Player"))
        {

            Debug.LogWarning("Collides with player");
            Physics.IgnoreCollision(GetComponent<Collider>(), GetComponent<Collider>());

            //Ignore player character collision, otherwise this moves it, which is quite odd, and other weird stuff happens!
            Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());

            // 	//Return, otherwise we will destroy with this hit, which we don't want!
            return;
        }
        //
        //If destroy on impact is false, start 
        //coroutine with random destroy timer
        if (!destroyOnImpact)
        {
            StartCoroutine(DestroyTimer());
        }
        //Otherwise, destroy bullet on impact
        else
        {
            Destroy(gameObject);
        }

        //If bullet collides with "Blood" tag
        if (collision.transform.tag == "Blood")
        {
            //Instantiate random impact prefab from array
            Instantiate(bloodImpactPrefabs[Random.Range
                (0, bloodImpactPrefabs.Length)], transform.position,
                Quaternion.LookRotation(collision.contacts[0].normal));
            //Destroy bullet object
            Destroy(gameObject);
        }

        //If bullet collides with "Metal" tag
        if (collision.transform.tag == "Metal")
        {
            //Instantiate random impact prefab from array
            Instantiate(metalImpactPrefabs[Random.Range
                (0, bloodImpactPrefabs.Length)], transform.position,
                Quaternion.LookRotation(collision.contacts[0].normal));
            //Destroy bullet object
            Destroy(gameObject);
        }

        //If bullet collides with "Dirt" tag
        if (collision.transform.tag == "Dirt")
        {
            //Instantiate random impact prefab from array
            Instantiate(dirtImpactPrefabs[Random.Range
                (0, bloodImpactPrefabs.Length)], transform.position,
                Quaternion.LookRotation(collision.contacts[0].normal));
            //Destroy bullet object
            Destroy(gameObject);
        }

        //If bullet collides with "Concrete" tag
        if (collision.transform.tag == "Concrete")
        {
            //Instantiate random impact prefab from array
            Instantiate(concreteImpactPrefabs[Random.Range
                (0, bloodImpactPrefabs.Length)], transform.position,
                Quaternion.LookRotation(collision.contacts[0].normal));
            //Destroy bullet object
            Destroy(gameObject);
        }


        if (collision.transform.tag == "Monster")
        {
            MonsterController monster = collision.transform.gameObject.GetComponent<MonsterController>();
            if (monster != null)
            {
                Vector3 hitDirection = collision.transform.position - transform.position;
                monster.TakeDamage(1, hitDirection); // 传递受击方向
                                                     // 对怪物造成1点伤害

                Debug.Log("monster hit");
            }
            //Destroy bullet object
            Destroy(gameObject);
        }
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.CompareTag("Monster"))
    //    {
    //        MonsterController monster = other.GetComponent<MonsterController>();
    //        if (monster != null)
    //        {
    //            Debug.Log("Bullet hit: " + other.gameObject.name);
    //            monster.TakeDamage(1);
    //            Debug.Log("Monster hit!");
    //        }
    //        Destroy(gameObject);
    //    }
    //}

    private IEnumerator DestroyTimer()
    {
        //Wait random time based on min and max values
        yield return new WaitForSeconds
            (Random.Range(minDestroyTime, maxDestroyTime));
        //Destroy bullet object
        Destroy(gameObject);
    }

    private IEnumerator DestroyAfter()
    {
        //Wait for set amount of time
        yield return new WaitForSeconds(destroyAfter);
        //Destroy bullet object
        Destroy(gameObject);
    }
}