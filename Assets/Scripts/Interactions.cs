using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>Gestisce interazioni: raccolta oggetti o apertura porte garage.</summary>
public class Interactions : MonoBehaviour
{
  // Tipo di interazione: oggetto raccoglibile o porta controllabile
  public enum TipoInterazione { Raccoglibile, PortaGarage }

  [Header("Configurazione")]
  [Tooltip("Tipo di interazione")]
  public TipoInterazione tipo = TipoInterazione.Raccoglibile;

  [Tooltip("Testo UI per il prompt")]
  public Text promptText;

  [Tooltip("Riferimento al player")]
  public Transform playerTransform;

  // Variabili di stato e posizioni per il movimento della porta
  bool playerInRange, doorIsOpen, moving = false;
  AudioSource audioSource;
  Vector3 doorClosedPos, doorOpenPos, targetPos;

  void Start()
  {
    // Prende l'AudioSource dal prefab stesso
    audioSource = GetComponent<AudioSource>();

    // Calcola le posizioni se l'oggetto è una porta
    if (tipo == TipoInterazione.PortaGarage)
    {
      doorClosedPos = transform.position;
      doorOpenPos = doorClosedPos + Vector3.up * 3f;
    }
  }

  // === Trigger per quando il player entra o esce dall'area di interazione ===
  void OnTriggerEnter(Collider c) { if (c.transform == playerTransform) SetPlayerInRange(true); }
  void OnTriggerExit(Collider c) { if (c.transform == playerTransform) SetPlayerInRange(false); }

  void SetPlayerInRange(bool inRange)
  {
    playerInRange = inRange;
    if (playerInRange) MostraPrompt();
    else NascondiPrompt();
  }

  void Update()
  {
    // Se la porta è in movimento, spostala verso `targetPos`
    if (moving)
    {
      transform.position = Vector3.MoveTowards(transform.position, targetPos, 3f * Time.deltaTime);
      // Quando la distanza rimanente è minore della soglia, assicuriamo la posizione finale evitando oscillazioni e micro-scatti
      if (Vector3.Distance(transform.position, targetPos) < 0.001f)
      {
        transform.position = targetPos;
        moving = false;
      }
    }

    if (!playerInRange || !Input.GetKeyDown(KeyCode.E)) return;

    // Se è un oggetto raccoglibile, riproduci audio, aggiungilo all'inventario e distruggilo
    if (tipo == TipoInterazione.Raccoglibile)
    {
      audioSource?.Play();
      PlayerController.Instance?.Add(gameObject.name);
      NascondiPrompt();
      Destroy(gameObject, audioSource ? audioSource.clip.length - .8f : 0f);
      return;
    }

    // Altrimenti è una porta: apri/chiudi in base allo stato attuale
    else if (!doorIsOpen)
    {
      SetDoorOpen(true);
      StartCoroutine(ChiusuraAutomatica());
    }
  }

  // === Gestione porta ===
  public void SetDoorOpen(bool open)
  {
    audioSource?.Play();
    doorIsOpen = open;
    targetPos = open ? doorOpenPos : doorClosedPos;
    moving = true;
    if (!open && playerInRange) MostraPrompt();
  }

  // Coroutine per chiudere automaticamente la porta dopo 3 secondi se il player è lontano
  IEnumerator ChiusuraAutomatica()
  {
    yield return new WaitForSeconds(3f);
    if (!doorIsOpen || playerTransform == null) yield break;
    // distanza 2D fra porta e player (ignoriamo la Y)
    float distanza = Vector2.Distance(
        new Vector2(transform.position.x, transform.position.z),
        new Vector2(playerTransform.position.x, playerTransform.position.z));

    // Se il player è fuori dal raggio di sicurezza, richiudi la porta
    if (distanza > 3f) SetDoorOpen(false);
    else StartCoroutine(ChiusuraAutomatica());
  }

  // === UI ===
  void MostraPrompt()
  {
    if (!promptText) return;
    // Se c'è un messaggio, mostralo; altrimenti nascondi il prompt
    string msg = tipo == TipoInterazione.Raccoglibile
        ? $"Premi E per raccogliere {gameObject.name}"
        : (doorIsOpen ? "" : "Premi E per aprire la porta");
    promptText.text = msg;
    promptText.gameObject.SetActive(!string.IsNullOrEmpty(msg));
  }

  void NascondiPrompt() => promptText?.gameObject.SetActive(false);
}
