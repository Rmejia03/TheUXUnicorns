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
    //[SerializeField] Animator animate;
    [SerializeField] Transform headPosition;
    
    
    [SerializeField] int HP;
    [SerializeField] int animateSpeedTransition;
    [SerializeField] int ViewAngle;
    [SerializeField] int faceTargetSpeed;
    [SerializeField] float shootRate;
    [SerializeField] int roamDistance;
    [SerializeField] int roamTimer;
    bool isShooting;
    bool playerInRange;
    bool destinationChosen;
    float angleToPlayer;
    float stoppingDistanceOrigin;

    Vector3 startingPosition;
    Vector3 playerDirection;

    int HPOrigin;


    // Start is called before the first frame update
    void Start()
    {
        gameManager.instance.updateGameGoal(1);
        startingPosition = transform.position;
        stoppingDistanceOrigin = agent.stoppingDistance;
        HPOrigin = HP;
        UpdateEnemyUI();
    }

    // Update is called once per frame
    void Update()
    {
        //float animateSpeed = agent.velocity.normalized.magnitude;
        //animate.SetFloat("Speed", Mathf.Lerp(animate.GetFloat("Speed"), animateSpeed, Time.deltaTime * animateSpeedTransition));
        if(playerInRange && !CanSeePlayer())
        {
            StartCoroutine(Roam());
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
                if(!isShooting)
                {
                    StartCoroutine(shoot());
                }
                if(agent.remainingDistance <= agent.stoppingDistance)
                {
                    FaceTarget();
                }
                return true;
            }
        }
        agent.stoppingDistance = 0;
        return false;
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
        }
    }

    IEnumerator shoot()
    {
        isShooting = true;
        Instantiate(bullet, shootPOS.position, transform.rotation);
        yield return new WaitForSeconds(shootRate);
        isShooting = false;
    }

    public void takeDamage(int damage)
    {
        HP -= damage;
        UpdateEnemyUI();
        agent.SetDestination(gameManager.instance.player.transform.position);

        StartCoroutine(hitFlash());

        if (HP <= 0) 
        {
            Destroy(gameObject); 
            gameManager.instance.updateGameGoal(-1); 
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
