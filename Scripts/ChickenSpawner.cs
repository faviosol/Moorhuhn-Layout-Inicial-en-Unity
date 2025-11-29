using UnityEngine;

public class ChickenSpawner : MonoBehaviour
{
    [Header("Prefab del pollo")]
    public GameObject chickenPrefab;

    [Header("Spawn (controlado por la wave actual)")]
    public float spawnInterval = 1.5f;
    public int maxChickens = 10;

    [Header("Rango Y para cruces horizontales")]
    public float minY = -2f;
    public float maxY = 4f;

    [Header("Limites del mundo (ajusta a tu camara)")]
    public float xLeft = -12f;
    public float xRight = 12f;
    public float yTop = 5f;
    public float yBottom = -5f;

    [Header("Nombres posibles de pollos")]
    public string[] nombresPollo = new string[5] { "Pepe", "Lolo", "Chimuelo", "Nugget", "Paco" };

    // MATRIZ 2D de tipos por zona (3x3)
    public int[,] mapaSpawn = new int[3, 3]
    {
        { 0, 1, 0 }, // arriba
        { 1, 2, 1 }, // medio
        { 3, 4, 3 }  // abajo
    };

    // MATRIZ 2D de conteo actual de pollos (3x3)
    private int[,] contadorZonas = new int[3, 3];

    // Limite de cuantos pollos puede haber en una zona
    public int limitePorZona = 3;

    // ---------- LISTA ENLAZADA DE WAVES ----------

    public class WaveNode
    {
        public float spawnInterval;  // intervalo entre spawns
        public int maxChickens;      // maximo de enemigos en escena
        public float waveDuration;   // duracion de la wave en segundos (0 = infinita)
        public Color colorWave;      // color de los pollos en esta wave
        public WaveNode next;        // siguiente wave

        public WaveNode(float spawnInterval, int maxChickens, float waveDuration, Color colorWave)
        {
            this.spawnInterval = spawnInterval;
            this.maxChickens = maxChickens;
            this.waveDuration = waveDuration;
            this.colorWave = colorWave;
            this.next = null;
        }
    }

    private WaveNode firstWave;     // inicio de la lista enlazada
    private WaveNode currentWave;   // wave actual
    private float waveTime = 0f;    // tiempo acumulado en la wave actual

    // ---------------------------------------------

    private float timer = 0f;

    void Start()
    {
        // crear waves segun la duracion total de la partida
        SetupWavesByMatchTime();

        currentWave = firstWave;
        ApplyCurrentWaveSettings();
        waveTime = 0f;
        timer = 0f;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        // actualizar wave (cambio de nodo en la lista enlazada)
        UpdateWave();

        // logica normal de spawn
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;

            int current = FindObjectsOfType<EnemyChicken>().Length;
            if (current < maxChickens)
            {
                SpawnChicken();
            }
        }
    }

    // Crea las waves usando el tiempo total de la partida
    void SetupWavesByMatchTime()
    {
        float total = 60f;

        if (GameManager.Instance != null)
        {
            total = GameManager.Instance.matchDuration;
        }

        // primera y segunda wave de 20 segundos
        float w1Dur = 20f;
        float w2Dur = 20f;
        float w3Dur = total - (w1Dur + w2Dur);

        if (w3Dur < 0f)
            w3Dur = 0f; // si la partida dura menos de 40, sobrante = 0

        // Wave 1: color blanco
        WaveNode w1 = new WaveNode(1.5f, 8, w1Dur, Color.white);
        // Wave 2: color amarillo
        WaveNode w2 = new WaveNode(1.0f, 12, w2Dur, Color.yellow);
        // Wave 3: color rojo, dura "lo que sobre"
        // si w3Dur == 0, esta wave sera infinita en la practica (el GameManager cortara la partida)
        WaveNode w3 = new WaveNode(0.7f, 16, w3Dur, Color.red);

        w1.next = w2;
        w2.next = w3;

        firstWave = w1;
    }

    // Cambia a la siguiente wave cuando se acaba el tiempo, avanzando en la lista enlazada.
    void UpdateWave()
    {
        if (currentWave == null) return;
        if (currentWave.waveDuration <= 0f) return;

        waveTime += Time.deltaTime;

        if (waveTime >= currentWave.waveDuration)
        {
            if (currentWave.next != null)
            {
                currentWave = currentWave.next;
                ApplyCurrentWaveSettings();
                waveTime = 0f;
            }
            else
            {
                currentWave.waveDuration = 0f;
            }
        }
    }

    void ApplyCurrentWaveSettings()
    {
        if (currentWave == null) return;

        spawnInterval = currentWave.spawnInterval;
        maxChickens = currentWave.maxChickens;
    }

    // Convierte una posición del mundo en una zona (fila, columna) de la matriz 3x3
    private void CalcularZonaDesdePos(Vector3 pos, out int fila, out int columna)
    {
        float altoTotal = yTop - yBottom;
        float altoTercio = altoTotal / 3f;
        fila = (int)((pos.y - yBottom) / altoTercio);
        if (fila < 0) fila = 0;
        if (fila > 2) fila = 2;

        float anchoTotal = xRight - xLeft;
        float anchoTercio = anchoTotal / 3f;
        columna = (int)((pos.x - xLeft) / anchoTercio);
        if (columna < 0) columna = 0;
        if (columna > 2) columna = 2;
    }

    // Crea e instancia un pollo según el tipo indicado
    private EnemyChicken CrearPolloPorTipo(int tipo)
    {
        Vector3 pos = Vector3.zero;
        GameObject go = null;
        EnemyChicken ch = null;

        switch (tipo)
        {
            case 0: // izquierda -> derecha
                pos = new Vector3(xLeft, Random.Range(minY, maxY), 0f);
                go = Instantiate(chickenPrefab, pos, Quaternion.identity);
                ch = go.GetComponent<EnemyChicken>();
                if (ch != null)
                {
                    ch.movementType = EnemyChicken.MovementType.LeftToRight;
                    ch.speedX = Random.Range(3f, 6f);
                }
                break;

            case 1: // derecha -> izquierda
                pos = new Vector3(xRight, Random.Range(minY, maxY), 0f);
                go = Instantiate(chickenPrefab, pos, Quaternion.identity);
                ch = go.GetComponent<EnemyChicken>();
                if (ch != null)
                {
                    ch.movementType = EnemyChicken.MovementType.RightToLeft;
                    ch.speedX = Random.Range(3f, 6f);
                }
                break;

            case 2: // desde arriba
                pos = new Vector3(Random.Range(xLeft + 2f, xRight - 2f), yTop, 0f);
                go = Instantiate(chickenPrefab, pos, Quaternion.identity);
                ch = go.GetComponent<EnemyChicken>();
                if (ch != null)
                {
                    ch.movementType = EnemyChicken.MovementType.FromTop;
                    ch.speedY = Random.Range(-4f, -2f);
                    ch.speedX = Random.Range(-1.5f, 1.5f);
                }
                break;

            case 3: // fondo
                pos = new Vector3(xLeft, Random.Range(minY, maxY), 0f);
                go = Instantiate(chickenPrefab, pos, Quaternion.identity);
                ch = go.GetComponent<EnemyChicken>();
                if (ch != null)
                {
                    ch.movementType = EnemyChicken.MovementType.Background;
                    ch.speedX = Random.Range(1.5f, 3f);
                    ch.puntos = 20;
                }
                break;

            case 4: // tanque / graph path
                pos = new Vector3(xLeft, Random.Range(minY, maxY), 0f);
                go = Instantiate(chickenPrefab, pos, Quaternion.identity);
                ch = go.GetComponent<EnemyChicken>();
                if (ch != null)
                {
                    ch.movementType = EnemyChicken.MovementType.GraphPath;
                    ch.graphSpeed = Random.Range(2f, 4f);
                    ch.vida = 3;
                    ch.puntos = 30;
                }
                break;
        }

        return ch;
    }

    void SpawnChicken()
    {
        if (chickenPrefab == null)
        {
            Debug.LogWarning("No asignaste chickenPrefab en ChickenSpawner.");
            return;
        }

        // POSICION BASE ALEATORIA PARA DECIDIR LA ZONA
        Vector3 posRandom = new Vector3(
            Random.Range(xLeft, xRight),
            Random.Range(yBottom, yTop),
            0f
        );

        // Convertir la posición en zona (fila, columna)
        int fila, columna;
        CalcularZonaDesdePos(posRandom, out fila, out columna);

        // SI LA ZONA ESTA LLENA, BUSCAR UNA LIBRE
        if (contadorZonas[fila, columna] >= limitePorZona)
        {
            bool encontrada = false;

            for (int f = 0; f < 3 && !encontrada; f++)
            {
                for (int c = 0; c < 3 && !encontrada; c++)
                {
                    if (contadorZonas[f, c] < limitePorZona)
                    {
                        fila = f;
                        columna = c;
                        encontrada = true;
                    }
                }
            }

            if (!encontrada)
            {
                // Todas las zonas estan llenas
                return;
            }
        }

        // OBTENER TIPO DESDE LA MATRIZ 2D
        int tipo = mapaSpawn[fila, columna];

        // Crear pollo según tipo
        EnemyChicken ch = CrearPolloPorTipo(tipo);

        // SI SE CREO EL POLLO, AUMENTAR EL CONTADOR DE LA ZONA, NOMBRE Y COLOR
        if (ch != null)
        {
            // asignar nombre aleatorio
            if (nombresPollo != null && nombresPollo.Length > 0)
            {
                int idxNombre = Random.Range(0, nombresPollo.Length);
                ch.nombrePollo = nombresPollo[idxNombre];
            }

            contadorZonas[fila, columna]++;

            ch.limiteX = Mathf.Max(Mathf.Abs(xLeft), Mathf.Abs(xRight)) + 1f;
            ch.limiteYAbajo = yBottom - 1f;

            // cambiar color segun la wave actual
            SpriteRenderer sr = ch.GetComponent<SpriteRenderer>();
            if (sr != null && currentWave != null)
            {
                sr.color = currentWave.colorWave;
            }
        }
    }

    // Metodo para reducir contador cuando un pollo muere
    public void ReducirContadorZona(Vector3 pos)
    {
        int fila, columna;
        CalcularZonaDesdePos(pos, out fila, out columna);

        contadorZonas[fila, columna]--;
        if (contadorZonas[fila, columna] < 0)
            contadorZonas[fila, columna] = 0;
    }
}