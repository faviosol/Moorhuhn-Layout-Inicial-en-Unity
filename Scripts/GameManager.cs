using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ARRAY 1D: puntos por tipo de pollo
    // 0..4 = tipos de pollo
    [Header("Score por tipo de pollo")]
    public int[] puntosPorTipo = new int[5] { 10, 15, 20, 30, 50 };

    [Header("Score total")]
    public int score = 0;
    public TMP_Text scoreText;

    [Header("Timer")]
    public float matchDuration = 60f; // duracion de la partida en segundos
    private float timeLeft;
    public TMP_Text timerText;

    [Header("Sistemas a desactivar al final")]
    public ChickenSpawner chickenSpawner;   // arrastralo en el Inspector
    public MonoBehaviour shootScript;       // aqui arrastras tu MouseShooter

    [Header("UI Final (opcional)")]
    public GameObject gameOverPanel;        // panel de Game Over
    public TMP_Text rankingText;            // texto para mostrar el ranking del arbol

    [Header("Hash table de muertes por nombre")]
    public int hashTableSize = 31;          // tamano de la tabla hash
    public TMP_Text killInfoText;           // texto para mostrar info de kills / resumen

    public bool IsGameOver { get; private set; }

    // -------- ARBOL BINARIO DE PUNTAJES --------
    [System.Serializable]
    public class ScoreTreeNode
    {
        public int value;             // puntaje de ese enemigo
        public int count;             // cuantas veces aparecio ese valor
        public ScoreTreeNode left;    // menor
        public ScoreTreeNode right;   // mayor

        public ScoreTreeNode(int v)
        {
            value = v;
            count = 1;
            left = null;
            right = null;
        }
    }

    private ScoreTreeNode scoreRoot = null;
    // -------------------------------------------

    // -------- TABLA HASH DE MUERTES POR NOMBRE --------
    [System.Serializable]
    public class KillHashTable
    {
        [System.Serializable]
        public struct Entry
        {
            public string key;   // nombre del pollo
            public int value;    // cantidad de muertes
            public bool used;    // indica si esta celda fue usada
        }

        public Entry[] entries;
        public int size;

        public KillHashTable(int size)
        {
            this.size = size;
            entries = new Entry[size];
        }

        private int Hash(string key)
        {
            int h = 0;
            for (int i = 0; i < key.Length; i++)
            {
                h = (h * 31 + key[i]) % size;
            }
            if (h < 0) h = -h;
            return h;
        }

        private int FindIndex(string key)
        {
            int index = Hash(key);
            int startIndex = index;

            while (true)
            {
                if (!entries[index].used || entries[index].key == key)
                {
                    return index;
                }

                index = (index + 1) % size;
                if (index == startIndex)
                {
                    // tabla llena
                    return -1;
                }
            }
        }

        // Suma una muerte para "name" y devuelve el total acumulado
        public int AddKill(string name)
        {
            if (size == 0) return 0;

            int index = FindIndex(name);
            if (index == -1) return 0;

            if (!entries[index].used)
            {
                entries[index].used = true;
                entries[index].key = name;
                entries[index].value = 1;
            }
            else
            {
                entries[index].value++;
            }

            return entries[index].value;
        }

        // Devuelve un resumen en texto de todas las entradas usadas
        public string GetSummaryText()
        {
            string result = "";
            for (int i = 0; i < size; i++)
            {
                if (entries[i].used)
                {
                    result += "Nombre " + entries[i].key + " x" + entries[i].value + "\n";
                }
            }
            return result;
        }
    }

    private KillHashTable killTable;
    // ---------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        timeLeft = matchDuration;
        IsGameOver = false;

        UpdateScoreText();
        UpdateTimerText();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // limpiar arbol e inicializar hash table al inicio
        scoreRoot = null;
        killTable = new KillHashTable(hashTableSize);
    }

    private void Update()
    {
        if (IsGameOver) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft < 0f) timeLeft = 0f;

        UpdateTimerText();

        if (timeLeft <= 0f && !IsGameOver)
        {
            EndGame();
        }
    }

    // Se llama cuando un enemigo da puntos (desde EnemyChicken.Morir)
    public void AddScore(int amount)
    {
        if (IsGameOver) return;

        score += amount;
        InsertScore(amount);  // insertar en el arbol
        UpdateScoreText();
    }

    // Registro en tabla hash: se llama tambien desde EnemyChicken.Morir
    public void RegisterKill(string nombre, int puntosBase)
    {
        if (IsGameOver) return;
        if (killTable == null || string.IsNullOrEmpty(nombre))
            return;

        int total = killTable.AddKill(nombre);

        // texto informativo simple
        if (killInfoText != null)
        {
            killInfoText.text = "Mataste a " + nombre + " (" + total + ")";
        }

        // bonus cada 3 muertes del mismo nombre
        if (total % 3 == 0)
        {
            int bonus = puntosBase; // mismo valor que puntos base
            score += bonus;
            UpdateScoreText();

            if (killInfoText != null)
            {
                killInfoText.text += "\nBONUS +" + bonus;
            }
            else
            {
                Debug.Log("BONUS por " + nombre + " x" + total + " +" + bonus);
            }
        }
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
            scoreText.text = score.ToString();
    }

    private void UpdateTimerText()
    {
        if (timerText == null) return;

        int totalSeconds = Mathf.CeilToInt(timeLeft);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void EndGame()
    {
        IsGameOver = true;

        // apagar spawner
        if (chickenSpawner != null)
            chickenSpawner.enabled = false;

        // apagar script de disparo
        if (shootScript != null)
            shootScript.enabled = false;

        // mostrar panel final
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        // mostrar ranking del arbol
        ShowScoreRanking();

        // mostrar resumen de la tabla hash
        ShowKillSummary();
    }

    // ----------------- METODOS DEL ARBOL -----------------

    // Inserta un valor de puntaje en el arbol
    private void InsertScore(int value)
    {
        scoreRoot = InsertScoreRec(scoreRoot, value);
    }

    private ScoreTreeNode InsertScoreRec(ScoreTreeNode node, int value)
    {
        if (node == null)
        {
            return new ScoreTreeNode(value);
        }

        if (value < node.value)
        {
            node.left = InsertScoreRec(node.left, value);
        }
        else if (value > node.value)
        {
            node.right = InsertScoreRec(node.right, value);
        }
        else
        {
            // valor repetido, solo aumentamos contador
            node.count++;
        }

        return node;
    }

    // Llama al recorrido en orden y lo muestra en UI o consola
    private void ShowScoreRanking()
    {
        if (rankingText != null)
        {
            rankingText.text = "Ranking de puntajes individuales:\n";
            BuildRankingText(scoreRoot);
        }
        else
        {
            Debug.Log("Ranking de puntajes individuales (arbol en orden):");
            PrintRankingToConsole(scoreRoot);
        }
    }

    // Construye un string en orden (izquierda - raiz - derecha)
    private void BuildRankingText(ScoreTreeNode node)
    {
        if (node == null) return;

        BuildRankingText(node.left);
        rankingText.text += "Puntaje " + node.value + " x" + node.count + "\n";
        BuildRankingText(node.right);
    }

    // Version para consola
    private void PrintRankingToConsole(ScoreTreeNode node)
    {
        if (node == null) return;

        PrintRankingToConsole(node.left);
        Debug.Log("Puntaje " + node.value + " x" + node.count);
        PrintRankingToConsole(node.right);
    }

    // ----------------- METODOS DE TABLA HASH -----------------

    private void ShowKillSummary()
    {
        if (killTable == null) return;

        string resumen = killTable.GetSummaryText();

        if (killInfoText != null)
        {
            killInfoText.text = "Registro de muertes (hash):\n" + resumen;
        }
        else
        {
            Debug.Log("Registro de muertes (hash):\n" + resumen);
        }
    }

    // ------------------------------------------------------
}
