using UnityEngine;

public class MouseShooter : MonoBehaviour
{
    public Camera cam;
    public int damagePerShot = 1;

    void Start()
    {
        if (cam == null)
            cam = Camera.main;

        Cursor.visible = false;  // opcional, solo visual
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        // Mover la mira al mouse
        Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;
        transform.position = mousePos;

        // Disparar con clic izquierdo
        if (Input.GetMouseButtonDown(0))
        {
            if (!AmmoManager.Instance.CanShoot())
                return;

            AmmoManager.Instance.UseBullet();

            Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);
            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

            if (hit.collider != null)
            {
                EnemyChicken chicken = hit.collider.GetComponent<EnemyChicken>();
                if (chicken != null)
                {
                    chicken.RecibirDano(damagePerShot);
                }
            }
        }

        // Recargar con tecla R
        if (Input.GetKeyDown(KeyCode.R))
        {
            AmmoManager.Instance.StartReload();
        }
    }
}
