namespace player2_sdk
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AI;
    using Newtonsoft.Json.Linq;

    public class NpcAIHandler : MonoBehaviour
    {
        [Header("References")]
        public NpcManager npcManager;
        public NavMeshAgent agent;
        public Animator animator;
        public Transform coverPoint;
        public GameObject attackTargetObject;

        [Header("Settings")]
        public string npcName = "CubeNPC";
        public float attackRange = 15f;
        private PlayerController PlayerController;
        private bool isMoving = false;

        void Awake()
        {
            if (npcManager != null)
            {
                npcManager.RegisterNpc(npcName, null, gameObject);
                Debug.Log($"{npcName} registered with NpcManager");
            }
        }

        void Start()
        {
            if (npcManager != null)
            {
                npcManager.functionHandler.AddListener(OnFunctionCalled);
                Debug.Log($"{npcName} subscribed to functionHandler");
            }
        }

        void Update()
        {
            if (isMoving && agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh && !agent.pathPending)
            {
                if (agent.remainingDistance <= agent.stoppingDistance)
                {
                    if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                    {
                        animator?.SetBool("isWalking", false);
                        isMoving = false;
                    }
                }
            }
        }

        // ============ PLAYER2 CALLBACK ============
        private void OnFunctionCalled(FunctionCall functionCall)
        {
            if (functionCall == null) return;

            Debug.Log($"{npcName} received function call: {functionCall.name} with args: {functionCall.arguments}");
            var args = JObjectToDictionary(functionCall.arguments);
            ExecuteCommand(functionCall.name, args);
        }

        private Dictionary<string, object> JObjectToDictionary(JObject jObject)
        {
            var dict = new Dictionary<string, object>();
            if (jObject == null) return dict;

            foreach (var pair in jObject)
                dict[pair.Key] = pair.Value.ToObject<object>();

            return dict;
        }

        // ============ MANUAL INPUT SUPPORT ============
        public void ProcessCommand(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;

            Debug.Log($"{npcName} received manual input: {input}");

            string[] parts = input.Trim().Split(' ');
            string command = parts[0].ToLower();
            var args = new Dictionary<string, object>();

            switch (command)
            {
                case "move_to":
                    if (parts.Length >= 3)
                    {
                        args["x"] = float.Parse(parts[1]);
                        args["z"] = float.Parse(parts[2]);
                    }
                    break;

                case "move":
                    args["distance"] = (parts.Length >= 2) ? float.Parse(parts[1]) : 5f;
                    break;

                case "stop":
                    break;

                case "attack_target":
                    if (parts.Length >= 2)
                        args["targetName"] = parts[1];
                    break;

                case "take_cover":
                    //  anim.SetFloat("walkSpeed", smoothedSpeed, 0.1f, Time.deltaTime);
                    PlayerController PlayerControllerinstance = FindObjectOfType<PlayerController>();
                    if (PlayerControllerinstance != null)
                    {
                        PlayerControllerinstance.RunAnimation();
                        
                    }
                    break;

                case "flame":
                    if (parts.Length >= 2)
                        args["radius"] = float.Parse(parts[1]);
                    break;
            }

            ExecuteCommand(command, args);
        }
        public Animator anim;
        // ============ EXECUTION ============
        private void ExecuteCommand(string command, Dictionary<string, object> args)
        {
            switch (command)
            {
                case "move_to": MoveTo(args); break;
                case "move": MoveForward(args); break;
                case "stop": StopMoving(); break;
                case "attack_target": AttackTarget(args); break;
                case "take_cover": TakeCover(args); break;
                case "flame": Flame(args); break;
                default:
                    Debug.LogWarning($"{npcName} unknown command: {command}");
                    break;
            }
        }

        // -------- AI Actions --------
        public void MoveTo(Dictionary<string, object> args)
        {
            if (agent == null || !agent.isOnNavMesh) return;
            if (!args.ContainsKey("x") || !args.ContainsKey("z")) return;

            float x = Convert.ToSingle(args["x"]);
            float y = args.ContainsKey("y") ? Convert.ToSingle(args["y"]) : transform.position.y;
            float z = Convert.ToSingle(args["z"]);

            Vector3 targetPos = new Vector3(x, y, z);
            if (NavMesh.SamplePosition(targetPos, out var hit, 5.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                animator?.SetBool("isWalking", true);
                isMoving = true;
                Debug.Log($"{npcName} moving to {hit.position}");
            }
            else
            {
                Debug.LogError($"{npcName} move_to target {targetPos} not on NavMesh!");
            }
        }

        public void MoveForward(Dictionary<string, object> args)
        {
            if (agent == null || !agent.isOnNavMesh) return;

            float distance = args.ContainsKey("distance") ? Convert.ToSingle(args["distance"]) : 5f;
            Vector3 forwardTarget = transform.position + transform.forward * distance;

            if (NavMesh.SamplePosition(forwardTarget, out var hit, 5.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                animator?.SetBool("isWalking", true);
                isMoving = true;
                Debug.Log($"{npcName} moving forward {distance} units to {hit.position}");
            }
            else
            {
                Debug.LogError($"{npcName} cannot move forward, no NavMesh near {forwardTarget}");
            }
        }

        public void StopMoving()
        {
            if (agent == null || !agent.isOnNavMesh) return;

            agent.ResetPath();
            animator?.SetBool("isWalking", false);
            isMoving = false;

            Debug.Log($"{npcName} stopped moving");
        }

        public void AttackTarget(Dictionary<string, object> args)
        {
            if (agent == null || !agent.isOnNavMesh) return;

            GameObject target = attackTargetObject;

            // If specific target name given
            if (args.ContainsKey("targetName"))
            {
                string targetName = args["targetName"].ToString();
                target = GameObject.Find(targetName);
            }

            // If still null, auto-find nearest enemy
            if (target == null)
            {
                GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
                if (enemies.Length > 0)
                {
                    GameObject nearest = null;
                    float minDist = Mathf.Infinity;
                    foreach (var enemy in enemies)
                    {
                        float dist = Vector3.Distance(transform.position, enemy.transform.position);
                        if (dist < minDist)
                        {
                            nearest = enemy;
                            minDist = dist;
                        }
                    }
                    target = nearest;
                }
            }

            if (target != null)
            {
                agent.ResetPath();
                isMoving = false;

                transform.LookAt(target.transform);
                animator?.SetTrigger("shoot");

                Debug.Log($"{npcName} attacking {target.name}");
            }
            else
            {
                Debug.LogWarning($"{npcName} no valid enemy found to attack.");
            }
        }

        public void TakeCover(Dictionary<string, object> args)
{
    if (agent == null || !agent.isOnNavMesh) return;

    Vector3 destination = transform.position;

    // Find nearest cover point (tagged "Cube")
    GameObject[] covers = GameObject.FindGameObjectsWithTag("cube");
    if (covers.Length > 0)
    {
        GameObject nearest = null;
        float minDist = Mathf.Infinity;

        foreach (var cover in covers)
        {
            float dist = Vector3.Distance(transform.position, cover.transform.position);
            if (dist < minDist)
            {
                nearest = cover;
                minDist = dist;
            }
        }

        if (nearest != null)
        {
            // Direction from NPC -> cover
            Vector3 dir = (nearest.transform.position - transform.position).normalized;

            // Instead of moving into the cube, stop *before it*
            float coverOffset = 1.5f; // adjust for how thick your cover is
            destination = nearest.transform.position - dir * coverOffset;
        }
    }

    if (NavMesh.SamplePosition(destination, out var hit, 5.0f, NavMesh.AllAreas))
    {
        agent.SetDestination(hit.position);
        animator?.SetBool("isWalking", true);
        isMoving = true;
        Debug.Log($"{npcName} taking cover near {hit.position}");
    }
    else
    {
        Debug.LogError($"{npcName} cover point {destination} is NOT on NavMesh!");
    }
}

        private void Flame(Dictionary<string, object> args)
        {
            float radius = args.ContainsKey("radius") ? Convert.ToSingle(args["radius"]) : 5f;
            Debug.Log($"{npcName} casting flame with radius {radius}");
        }
    }
}
