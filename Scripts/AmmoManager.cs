using UnityEngine;
using TMPro;

public class AmmoManager : MonoBehaviour
{
    public static AmmoManager Instance { get; private set; }

    // Cola circular simple para las balas
    [System.Serializable]
    public struct BulletQueue
    {
        public int[] datos;
        public int frente;
        public int fin;
        public int cantidad;

        public void Init(int capacidad)
        {
            datos = new int[capacidad];
            frente = 0;
            fin = 0;
            cantidad = 0;
        }

        public void Clear()
        {
            frente = 0;
            fin = 0;
            cantidad = 0;
        }

        public bool EstaVacia()
        {
            return cantidad == 0;
        }

        public bool EstaLlena()
        {
            return cantidad == datos.Length;
        }

        public void Enqueue(int valor)
        {
            if (EstaLlena()) return;

            datos[fin] = valor;
            fin = (fin + 1) % datos.Length; // avance circular
            cantidad++;
        }

        public int Dequeue()
        {
            if (EstaVacia()) return 0;

            int valor = datos[frente];
            frente = (frente + 1) % datos.Length; // avance circular
            cantidad--;
            return valor;
        }

        public int Count { get { return cantidad; } }
    }

    [Header("Munición")]
    public int clipSize = 10;
    public float reloadTime = 2f;

    public BulletQueue bullets;
    private bool isReloading = false;

    [Header("UI")]
    public TMP_Text ammoText;

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
        bullets.Init(clipSize);
        ReloadInstant();
    }

    public bool CanShoot()
    {
        return bullets.Count > 0 && !isReloading;
    }

    public void UseBullet()
    {
        if (bullets.Count > 0)
        {
            bullets.Dequeue();
            UpdateAmmoText();
        }
    }

    public void StartReload()
    {
        if (!isReloading && bullets.Count < clipSize)
            StartCoroutine(ReloadCoroutine());
    }

    private System.Collections.IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        yield return new WaitForSeconds(reloadTime);
        ReloadInstant();
        isReloading = false;
    }

    private void ReloadInstant()
    {
        bullets.Clear();
        for (int i = 0; i < clipSize; i++)
            bullets.Enqueue(1);

        UpdateAmmoText();
    }

    private void UpdateAmmoText()
    {
        if (ammoText != null)
            ammoText.text = "Balas: " + bullets.Count + "/" + clipSize;
    }
}