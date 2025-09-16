using System;
using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class genAi : MonoBehaviour
{
    [HideInInspector]public GameObject player;
    [HideInInspector] public Vector2 pPosition;
    public float speed = .5f, acceleration, range = 3;
    public float goalRange = 1.5f;
    [Range(1, 10)]
    public float attention = 1;
    [Range(0, 10)]
    public float stillness = 0;
    [HideInInspector] public bool inRange = false, attacking = false;
    [HideInInspector] public Coroutine MTG, attackingCor;
    [Range(0, 1)] public float likelyToAttack = .1f;
    [Range(0, 360)] public float coneOfAttack = 30;
    public bool leftToRight = true;
    [Space]
    [Space]
    public bulletStruct bulletStruct = new bulletStruct();
    [Space]
    [Space]
    public AttackStruct attackStruct = new AttackStruct();

    public void OnEnable()
    {
        player = GameObject.Find("player");
    }

    public void FixedUpdate()
    {
        pPosition = player.transform.position;
        if ((pPosition - (Vector2)transform.position).magnitude < range && !inRange)
        {
            StartCoroutine(TargetPlayer());
        }
        if (!attacking && inRange)
        {
            switch (attackStruct.shootType)
            {
                case ShootType.random:
                    if (UnityEngine.Random.Range(0f, 1f) < likelyToAttack * Time.fixedDeltaTime)
                    {
                        if (attackingCor != null) StopCoroutine(attackingCor);
                        attackingCor = StartCoroutine(AttackPlayer());
                    }
                    break;
                case ShootType.exact:
                    if (attackingCor != null) StopCoroutine(attackingCor);
                    attackingCor = StartCoroutine(AttackPlayer());
                    break;
            }
        }
    }

    public IEnumerator MoveTowardsGoal(Vector2 goal)
    {
        Vector2 pos = transform.position;
        float time = 0;
        float accel = acceleration * Mathf.Pow(time, 2);
        while(((Vector2)transform.position - goal).magnitude > .01)
        {
            time = ChangeVelocityProperties(time);
            transform.position = Vector2.Lerp(pos, goal, time);
            yield return new WaitForFixedUpdate();
        }
        transform.position = goal;
    }
    //override to change how the ai gets to the goal.
    //return the next step in time for lerping.
    public float ChangeVelocityProperties(float time)
    {
        float newTime = time + Time.fixedDeltaTime * speed;
        return acceleration * Mathf.Pow(time, 2) + newTime;
    }
    //override this to change how the ai finds its goal
    public virtual Vector2 FindNextGoal()
    {
        Vector2 playerThisVec = -pPosition + (Vector2)transform.position;
        Vector2 randomOffset = UnityEngine.Random.onUnitSphere;
        Vector2 nextGoal = pPosition + playerThisVec.normalized * goalRange + (randomOffset * 2 / attention) * (1 - GetNormalNumber(playerThisVec.magnitude, .4f, goalRange));
        nextGoal = Vector2.ClampMagnitude(nextGoal-(Vector2)transform.position, speed);
        return nextGoal;
    }
    //override this to change how long the ai waits
    public virtual float GetTimeToWait()
    {
        return UnityEngine.Random.Range(stillness * .5f, stillness);
    }

    public IEnumerator TargetPlayer()
    {
        inRange = true;
        while((pPosition - (Vector2)transform.position).magnitude < range)
        {
            if (MTG != null) StopCoroutine(MTG);
            MTG = StartCoroutine(MoveTowardsGoal(FindNextGoal() + (Vector2)transform.position));
            float timeUntilNextMove = GetTimeToWait();
            yield return new WaitForSeconds(timeUntilNextMove);
        }
        inRange = false;
    }

    public IEnumerator AttackPlayer()
    {
        attacking = true;
        int bulletNum = 0;
        while (bulletNum < attackStruct.amountOfBullets)
        {
            Vector2 dirToPlayer = (pPosition - (Vector2)transform.position).normalized;
            switch (attackStruct.targetingType)
            {
                case(TargetingType.random):
                    SpawnBullet(UnityEngine.Random.insideUnitCircle);
                    break;
                case (TargetingType.direct):
                    SpawnBullet(dirToPlayer);
                    break;
                case (TargetingType.radial):
                    float toPlayerAngle = Vector2.SignedAngle(Vector2.right, dirToPlayer) + 180;
                    float currBulletAngle = toPlayerAngle;
                    if (leftToRight)
                    {
                        currBulletAngle += coneOfAttack/2;
                        currBulletAngle -= bulletNum * coneOfAttack / attackStruct.amountOfBullets;
                    }
                    else
                    {
                        currBulletAngle -= coneOfAttack / 2;
                        currBulletAngle += bulletNum * coneOfAttack / attackStruct.amountOfBullets;
                    }
                    currBulletAngle += 180;
                    currBulletAngle *= Mathf.Deg2Rad;
                    Vector2 dir = new Vector2(Mathf.Cos(currBulletAngle), Mathf.Sin(currBulletAngle));
                    SpawnBullet(dir);
                    break;
            }
            yield return new WaitForSeconds(attackStruct.TimeBetweenBullets);
            bulletNum++;
        }
        yield return new WaitForSeconds(attackStruct.cooldown);
        attacking = false;
    }

    public void SpawnBullet(Vector2 dir)
    {
        GameObject go = Instantiate(bulletStruct.bulletPrefab, GameObject.Find("Bullets").transform);
        go.transform.position = transform.position;
        go.GetComponent<Rigidbody2D>().linearVelocity = dir.normalized * bulletStruct.speed;
        go.GetComponent<bullet>().velOverTime = bulletStruct.velocityOverTime;
    }



    public float GetNormalNumber(float x, float std, float mu)
    {
        float E = Mathf.Exp(-Mathf.Pow((x - mu) / std, 2) / 2);
        return E / (std * Mathf.Sqrt(2 * Mathf.PI));
    }

    public void OnDisable()
    {
        StopAllCoroutines();
    }
}

[Serializable]
public struct AttackStruct
{
    public TargetingType targetingType;
    public ShootType shootType;
    public int amountOfBullets;
    public float TimeBetweenBullets;
    public float cooldown;
}

[Serializable]
public struct bulletStruct
{
    public GameObject bulletPrefab;
    public Vector2 velocityOverTime;
    public float speed;
}

[Serializable]
public enum TargetingType
{
    radial,
    direct,
    random
}
[Serializable]
public enum ShootType
{
    random,
    exact
}
