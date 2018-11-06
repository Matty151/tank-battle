using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using System.Linq;

namespace Red
{
    public class SimpleFSM : FSM
    {
        public enum FSMState
        {
            None,
            Patrol,
            Chase,
            Flee,
            Attack,
            Evade,
            Dead,
        }

        //Current state that the NPC is reaching
        public FSMState curState;

        //Tank Rotation Speed
        public float curRotSpeed = 10;
        public float shootRate = 3.0f;

        //Bullet
        public GameObject Bullet;

        private float initialTankSpeed;

        public float fleeDistance;
        public float tooCloseForComfortDistance;
        public float evasionDistance;
        public float spottingRange = 500;
        private List<Transform> tanks = new List<Transform>();
        private GameObject closestTank;

        //Whether the NPC is destroyed or not
        private bool bDead;
        private int health;

        private NavMeshAgent navMeshAgent;

        public Transform[] friendlyTransforms;
        private Transform[] enemyTransforms;


        //Initialize the Finite state machine for the NPC tank
        protected override void Initialize()
        {
            curState = FSMState.Patrol;
            bDead = false;
            elapsedTime = 0.0f;
            health = 100;

            foreach (Transform tank in transform.parent)
            {
                if (tank != transform)
                {
                    tanks.Add(tank);
                }
            }

            navMeshAgent = GetComponent<NavMeshAgent>();
            initialTankSpeed = navMeshAgent.speed;

            enemyTransforms = GameObject.FindGameObjectsWithTag("Tank").
                    Where((o) => !friendlyTransforms.Contains(o.transform)).
                    Select((o) => o.transform).
                    ToArray();            

            //Get the turret of the tank
            turret = gameObject.transform.GetChild(0).transform;
            bulletSpawnPoint = turret.GetChild(0).transform;
            bulletSpawnPoint.Translate(new Vector3(0, 0, 3));
        }

        //Update each frame
        protected override void FSMUpdate()
        {
            switch (curState)
            {
                case FSMState.Patrol: UpdatePatrolState(); break;
                case FSMState.Chase: UpdateChaseState(); break;
                case FSMState.Attack: UpdateAttackState(); break;
                case FSMState.Dead: UpdateDeadState(); break;
                case FSMState.Flee: UpdateFleeState(); break;
                case FSMState.Evade: UpdateEvadeState(closestTank); break;
            }

            //Update the time
            elapsedTime += Time.deltaTime;

            //Go to dead state is no health left
            if (health <= 0)
                curState = FSMState.Dead;
        }

        /// <summary>
        /// Patrol state
        /// </summary>
        protected void UpdatePatrolState()
        {
            float distanceToDestination = Vector3.Distance(transform.position, navMeshAgent.destination);
            if (distanceToDestination < 100)
            {
                navMeshAgent.destination = new Vector3(Random.Range(500, 2500), 0, Random.Range(500, 2500));
            }

            foreach (Transform curEnemy in enemyTransforms)
            {
                RaycastHit hit;
                Vector3 direction = curEnemy.transform.position - transform.position;
                if (Physics.Raycast(turret.transform.position, direction, out hit, spottingRange * 5))
                {
                    if (hit.transform.CompareTag(curEnemy.tag))
                    {
                        closestTank = hit.transform.gameObject;
                        curState = FSMState.Chase;
                    }
                    else
                    {
                        curState = FSMState.Patrol;
                    }
                }
            }
        }

        /// <summary>
        /// Chase state
        /// </summary>
        protected void UpdateChaseState()
        {
            if (closestTank != null)
            {
                navMeshAgent.destination = closestTank.transform.position;

                Quaternion targetRotation = Quaternion.LookRotation(closestTank.transform.position - transform.position);
                turret.transform.rotation = Quaternion.Slerp(turret.transform.rotation, targetRotation, Time.deltaTime * curRotSpeed);

                float distanceToTank = Vector3.Distance(transform.position, closestTank.transform.position);

                if (distanceToTank >= 200 && distanceToTank <= 300 && turret.transform.rotation == targetRotation)
                {
                    ShootBullet();
                }
                else if (distanceToTank < 200)
                {
                    navMeshAgent.speed = initialTankSpeed / 4;
                }
                else
                {
                    navMeshAgent.speed = initialTankSpeed;
                }

                if (distanceToTank > spottingRange * 5)
                {
                    curState = FSMState.Patrol;
                }

                if (distanceToTank < 100)
                {
                    navMeshAgent.speed = initialTankSpeed;
                    curState = FSMState.Flee;
                }
            }
            else
            {
                navMeshAgent.speed = initialTankSpeed;
                curState = FSMState.Patrol;
            }
        }

        /// <summary>
        /// Attack state
        /// </summary>
        protected void UpdateAttackState()
        {

        }

        /// <summary>
        /// Dead state
        /// </summary>
        protected void UpdateDeadState()
        {
            //Show the dead animation with some physics effects
            if (!bDead)
            {
                bDead = true;
                Explode();
            }
        }

        /// <summary>
        /// Fleeing state
        /// </summary>
        protected void UpdateFleeState()
        {
            float distanceToDestination = Vector3.Distance(transform.position, navMeshAgent.destination);
            if (distanceToDestination < 100)
            {
                navMeshAgent.destination = new Vector3(Random.Range(500, 2500), 0, Random.Range(500, 2500));
            }

            if (closestTank != null)
            {
                float distanceToClosestTank = Vector3.Distance(transform.position, closestTank.transform.position);
                if (distanceToClosestTank > 200)
                {
                    curState = FSMState.Patrol;
                }
            } else
            {
                navMeshAgent.speed = initialTankSpeed;
                curState = FSMState.Patrol;
            }
        }

        /// <summary>
        /// Evading state
        /// </summary>
        protected void UpdateEvadeState(GameObject otherTank)
        {

        }

        /// <summary>
        /// Shoot the bullet from the turret
        /// </summary>
        private void ShootBullet()
        {
            if (elapsedTime >= shootRate)
            {
                //Shoot the bullet
                Instantiate(Bullet, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
                elapsedTime = 0.0f;
            }
        }

        /// <summary>
        /// Check the collision with the bullet
        /// </summary>
        /// <param name="collision"></param>
        void OnCollisionEnter(Collision collision)
        {
            //Reduce health
            if (collision.gameObject.tag == "Bullet")
            {
                //health -= collision.gameObject.GetComponent<Bullet>().damage;
                health -= 25;

                if (health <= 50)
                {
                    navMeshAgent.speed = initialTankSpeed / 2;
                }
            }
        }

        /// <summary>
        /// Check whether the next random position is the same as current tank position
        /// </summary>
        /// <param name="pos">position to check</param>
        protected bool IsInCurrentRange(Vector3 pos)
        {
            float xPos = Mathf.Abs(pos.x - transform.position.x);
            float zPos = Mathf.Abs(pos.z - transform.position.z);

            if (xPos <= 50 && zPos <= 50)
                return true;

            return false;
        }

        protected void Explode()
        {
            Destroy(transform.gameObject);
            enemyTransforms = GameObject.FindGameObjectsWithTag("Tank").
                    Where((o) => !friendlyTransforms.Contains(o.transform)).
                    Select((o) => o.transform).
                    ToArray();
        }

    }
}