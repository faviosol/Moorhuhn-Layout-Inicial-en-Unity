using UnityEngine;

public class EnemyChicken : MonoBehaviour
{
    public enum MovementType
    {
        LeftToRight,
        RightToLeft,
        FromTop,
        Background,
        Tank,
        GraphPath
    }

    [Header("Config general")]
    public MovementType movementType = MovementType.LeftToRight;
    public int vida = 1;
    public int puntos = 10;
    public string nombrePollo = "Pollo";

    [Header("Velocidades")]
    public float speedX = 3f;
    public float speedY = -1f;
    public float amplitudSeno = 0.5f;
    public float frecuenciaSeno = 2f;

    [Header("Limites de destruccion")]
    public float limiteX = 12f;
    public float limiteYAbajo = -7f;

    private float yBase;

    // -------- GRAFO: nodos y vecinos ----------
    [System.Serializable]
    public struct GraphNode
    {
        public Vector2 position;   // posicion del nodo en el mundo
        public int[] neighbors;    // indices de los nodos vecinos
    }

    [Header("Movimiento por grafo")]
    public GraphNode[] graphNodes;
    public int startNodeIndex = 0;
    public float graphSpeed = 3f;
    public float reachDistance = 0.1f;

    private int currentNodeIndex = -1;
    private int targetNodeIndex = -1;
    // ------------------------------------------

    void Start()
    {
        yBase = transform.position.y;

        if (movementType == MovementType.Background)
        {
            transform.localScale *= 0.5f;
            puntos *= 2;
            speedX *= 0.7f;
        }

        if (movementType == MovementType.Tank)
        {
            vida = Mathf.Max(vida, 3);
            puntos *= 3;
        }

        if (movementType == MovementType.GraphPath)
        {
            InitGraphMovement();
        }
    }

    void Update()
    {
        Vector3 pos = transform.position;

        switch (movementType)
        {
            case MovementType.LeftToRight:
                pos.x += speedX * Time.deltaTime;
                break;

            case MovementType.RightToLeft:
                pos.x -= speedX * Time.deltaTime;
                break;

            case MovementType.FromTop:
                pos.y += speedY * Time.deltaTime;
                pos.x += speedX * Time.deltaTime;
                break;

            case MovementType.Background:
                pos.x += speedX * Time.deltaTime;
                pos.y = yBase + Mathf.Sin(Time.time * frecuenciaSeno) * amplitudSeno;
                break;

            case MovementType.Tank:
                pos.x += speedX * Time.deltaTime;
                break;

            case MovementType.GraphPath:
                UpdateGraphMovement(ref pos);
                break;
        }

        transform.position = pos;

        // si sale de los limites, se destruye y avisa al spawner
        if (Mathf.Abs(pos.x) > limiteX || pos.y < limiteYAbajo)
        {
            if (GameManager.Instance != null && GameManager.Instance.chickenSpawner != null)
            {
                GameManager.Instance.chickenSpawner.ReducirContadorZona(transform.position);
            }

            Destroy(gameObject);
        }
    }

    // metodo de dano sin caracteres especiales
    public void RecibirDano(int dano)
    {
        vida -= dano;
        if (vida <= 0)
        {
            Morir();
        }
    }

    void Morir()
    {
        Debug.Log("Mataste un pollo, +" + puntos + " puntos");

        if (GameManager.Instance != null)
        {
            // suma al score total y lo inserta en el arbol
            GameManager.Instance.AddScore(puntos);
            // registra la muerte en la tabla hash por nombre
            GameManager.Instance.RegisterKill(nombrePollo, puntos);

            // avisar al spawner para liberar la zona
            if (GameManager.Instance.chickenSpawner != null)
            {
                GameManager.Instance.chickenSpawner.ReducirContadorZona(transform.position);
            }
        }

        Destroy(gameObject);
    }

    // ----------------- GRAFO -------------------

    void InitGraphMovement()
    {
        if (graphNodes == null || graphNodes.Length == 0)
            return;

        if (startNodeIndex < 0 || startNodeIndex >= graphNodes.Length)
            startNodeIndex = 0;

        currentNodeIndex = startNodeIndex;
        transform.position = new Vector3(
            graphNodes[currentNodeIndex].position.x,
            graphNodes[currentNodeIndex].position.y,
            transform.position.z
        );

        ChooseNextNode();
    }

    void ChooseNextNode()
    {
        if (graphNodes == null || graphNodes.Length == 0)
            return;

        GraphNode node = graphNodes[currentNodeIndex];

        if (node.neighbors != null && node.neighbors.Length > 0)
        {
            int idx = Random.Range(0, node.neighbors.Length);
            int nextIndex = node.neighbors[idx];

            if (nextIndex < 0 || nextIndex >= graphNodes.Length)
            {
                targetNodeIndex = currentNodeIndex;
            }
            else
            {
                targetNodeIndex = nextIndex;
            }
        }
        else
        {
            targetNodeIndex = currentNodeIndex;
        }
    }

    void UpdateGraphMovement(ref Vector3 pos)
    {
        if (graphNodes == null || graphNodes.Length == 0)
            return;

        if (currentNodeIndex < 0 || targetNodeIndex < 0)
        {
            InitGraphMovement();
        }

        Vector2 target2 = graphNodes[targetNodeIndex].position;
        Vector3 target3 = new Vector3(target2.x, target2.y, pos.z);

        pos = Vector3.MoveTowards(pos, target3, graphSpeed * Time.deltaTime);

        float dist = Vector3.Distance(pos, target3);
        if (dist <= reachDistance)
        {
            currentNodeIndex = targetNodeIndex;
            ChooseNextNode();
        }
    }

    // ------------------------------------------
}