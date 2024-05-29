using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;


public class EnemyAI : MonoBehaviour, IDamage
{
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Renderer model;
    [SerializeField] Transform shootPOS;
    [SerializeField] GameObject bullet;
    [SerializeField] Material enemyType;
    [SerializeField] Animator animate;
    [SerializeField] Transform headPosition;
    [SerializeField] Transform[] patrolPoints;
    
    
    [SerializeField] int HP;
    [SerializeField] int animateSpeedTransition;
    [SerializeField] int ViewAngle;
    [SerializeField] int faceTargetSpeed;
    [SerializeField] float attackRate;
    [SerializeField] int roamDistance;
    [SerializeField] int roamTimer;
    [SerializeField] bool isMelee;
    [SerializeField] float patrolSpeed;
    [SerializeField] int patrolDelay;
    [SerializeField] float meleeRange;
    [SerializeField] int meleeDamage;
    [SerializeField] int meleeAnimDur;
    [SerializeField] int shootRange;

    bool isAttacking;
    bool playerInRange;
    bool destinationChosen;
    float angleToPlayer;
    float stoppingDistanceOrigin;
    bool isPatrolling;
    bool hasPatrolPoints;
    

    Vector3 startingPosition;
    Vector3 playerDirection;

    int HPOrigin;
    int currentPatrolPoint = 0;

    // Start is called before the first frame update
    void Start()
    {
       
        startingPosition = transform.position;
        stoppingDistanceOrigin = agent.stoppingDistance;
        HPOrigin = HP;
        UpdateEnemyUI();

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            hasPatrolPoints = true;
            StartCoroutine(Patrol());
        }
        else
        {
            hasPatrolPoints = false;
            StartCoroutine(Roam());
        }
      
    }

    // Update is called once per frame
    void Update()
    {
       float animateSpeed = agent.velocity.normalized.magnitude;
       animate.SetFloat("Speed", Mathf.Lerp(animate.GetFloat("Speed"), animateSpeed, Time.deltaTime * animateSpeedTransition));
        //if (playerInRange && !CanSeePlayer())
        //{
        //    StartCoroutine(Roam());
        //}
        //else if (!playerInRange)
        //{
        //    StartCoroutine(Roam());
        //}
        if(playerInRange && !isAttacking && CanSeePlayer())
        {
            float distanceToPlayer = Vector3.Distance(transform.position, gameManager.instance.player.transform.position);

            if(isMelee && distanceToPlayer <= meleeRange)
            {
                StartCoroutine(MeleeAttack());
            }
            else if(!isMelee && distanceToPlayer <= shootRange)
            {
                StartCoroutine(Shoot());
            }
            else
                agent.SetDestination(gameManager.instance.player.transform.position);
        }
        else if(!playerInRange)
        {
            StartCoroutine(Roam());
        }
     
    }
    
    //Roaming Enemy
    IEnumerator Roam()
    {
        if(!destinationChosen && agent.remainingDistance < 0.05f)
        {
            destinationChosen = true;
            agent.stoppingDistance = 0;

            yield return new WaitForSeconds(roamTimer);

            Vector3 randomPosition = Random.insideUnitSphere * roamDistance;
            randomPosition += startingPosition;

            NavMeshHit hit;
            NavMesh.SamplePosition(randomPosition, out hit, roamDistance, 1);
            agent.SetDestination(hit.position);

            destinationChosen = false;
        }
    }

    //Patrolling enemy 
    IEnumerator Patrol()
    {
        isPatrolling = true;
        agent.stoppingDistance = 0;
        while(true)
        {
            Vector3 targetPosition = patrolPoints[currentPatrolPoint].position;
            agent.SetDestination(targetPosition);

            while(agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
            {
                yield return null;
            }

            yield return new WaitForSeconds(patrolDelay);

            currentPatrolPoint = (currentPatrolPoint + 1) % patrolPoints.Length;
            yield return null;

        }
    }

    

    //See player within range
    bool CanSeePlayer()
    {
        playerDirection = gameManager.instance.player.transform.position - headPosition.position;
        angleToPlayer = Vector3.Angle(new Vector3(playerDirection.x, playerDirection.y + 1, playerDirection.z), transform.forward);

        Debug.Log(angleToPlayer);
        Debug.DrawRay(headPosition.position, playerDirection);

        RaycastHit hit;

        if(Physics.Raycast(headPosition.position, playerDirection, out hit))
        {
            if(hit.collider.CompareTag("Player") && angleToPlayer <= ViewAngle)
            {
                agent.stoppingDistance = stoppingDistanceOrigin;
                agent.SetDestination(gameManager.instance.player.transform.position);

                if (!isAttacking)
                {
                    StartCoroutine(MeleeAttack());
                }
                if (agent.remainingDistance <= agent.stoppingDistance)
                {
                    FaceTarget();
                    if (isMelee)
                    {
                        StartCoroutine(attack());
                    }
                }
                return true;
            }
        }
        agent.stoppingDistance = 0;
        return false;
    }
    
    //Check if in melee range
    bool IsInRangeForMelee()
    {
        return Vector3.Distance(transform.position, gameManager.instance.player.transform.position) <= meleeRange;
    }

    //Rotation To Face Player
    void FaceTarget()
    {
        Quaternion rotation = Quaternion.LookRotation(new Vector3(playerDirection.x, 0, playerDirection.z));
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, Time.deltaTime * faceTargetSpeed);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            agent.stoppingDistance = 0;
        }
    }

    IEnumerator Shoot()
    {
        isAttacking = true;
        animate.SetTrigger("Attack");

        yield return new WaitForSeconds(attackRate);

        Instantiate(bullet, shootPOS.position, transform.rotation);
        
        isAttacking = false;
    }

    public void meleeHit()
    {
        if (IsInRangeForMelee())
        {
            gameManager.instance.player.GetComponent<IDamage>().takeDamage(meleeDamage);
        }
    }
    IEnumerator MeleeAttack()
    {
        if (!isAttacking)
        {
            isAttacking = true;
            animate.SetTrigger("Attack");

            yield return new WaitForSeconds(meleeAnimDur);            
            
            isAttacking = false;
        }
    }

    public void takeDamage(int damage)
    {
        HP -= damage;
        UpdateEnemyUI();
        agent.SetDestination(gameManager.instance.player.transform.position);

        StartCoroutine(hitFlash());

        if (HP <= 0) 
        {
            gameManager.instance.updateGameGoal(-1); 
            Destroy(gameObject); 
        }
    }

    IEnumerator hitFlash()
    {
        model.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        model.material.color = enemyType.color;
    }

    //Enemy HP
    void UpdateEnemyUI()
    {
        gameManager.instance.enemyHPBar.fillAmount = (float)HP / HPOrigin;
    }
}
