using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class Menu : MonoBehaviour
{
  [Tooltip("Pannello UI del menu pausa")]
  public GameObject pauseMenuPanel;
  static bool isPaused = false; // Statico per condividere lo stato tra istanze

  void Update()
  {
    // === GESTIONE PAUSA (ESC) ===
    if (Input.GetKeyDown(KeyCode.Escape) && pauseMenuPanel)
    {
      if (isPaused) Riprendi();
      else Pausa();
      return;
    }

    // Se in pausa, blocca tutte le altre interazioni
    if (isPaused) return;
  }

  // === MENU PAUSA ===
  void Pausa()
  {
    isPaused = true;
    Time.timeScale = 0f;
    if (pauseMenuPanel) pauseMenuPanel.SetActive(true);
    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;
  }

  public void Riprendi()
  {
    isPaused = false;
    Time.timeScale = 1f;
    if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
  }

  public void Ricomincia()
  {
    Time.timeScale = 1f;
    isPaused = false;
    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
  }

  public void Esci()
  {
    Time.timeScale = 1f;
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
  }
}
