using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Gestisce il sistema di raccolta benzina nel gioco.
/// Spawna taniche in posizioni casuali, gestisce il timer e determina vittoria/sconfitta.
/// </summary>
public class Benzina : MonoBehaviour
{
  [Header("Impostazioni Taniche")]
  [Tooltip("Prefab della tanica di benzina da istanziare")]
  public GameObject tanicaPrefab;

  [Tooltip("AudioSource usato per riprodurre il suono di raccolta")]
  public AudioSource audioSource;

  [Tooltip("Numero totale di taniche da raccogliere per vincere")]
  public int numeroTaniche = 10;

  [Header("Punteggio e timer")]
  [Tooltip("Riferimento al testo UI che mostra il punteggio corrente")]
  public Text punteggioText;

  [Tooltip("Riferimento al testo UI che mostra il tempo rimanente")]
  public Text timerText;

  [Tooltip("Riferimento al testo UI che mostra il tempo aggiunto dopo la raccolta")]
  public Text timeAddedText;

  [Tooltip("Tempo iniziale di gioco in secondi (default: 5 minuti)")]
  public float tempoLimite = 300f;

  [Header("Immagini Finali")]
  [Tooltip("Immagine mostrata se si raccolgono tutte le taniche")]
  public RawImage immagineVittoria;

  [Tooltip("Immagine mostrata se il tempo scade")]
  public RawImage immagineGameOver;

  // === VARIABILI PRIVATE ===
  private GameObject[] taniche;        // Array delle taniche istanziate
  private Vector3[] posizioniIniziali; // Posizioni base per l'animazione fluttuante
  private float tempoRimanente;        // Countdown del timer
  private bool giocoFinito;            // Flag per bloccare il gioco a fine partita
  private GameObject player;           // Cache del riferimento al giocatore
  private int punteggio;               // Contatore taniche raccolte
  private float timeAddedTimer = 0f;   // timer per +10s UI

  /// <summary>Inizializza il gioco: spawna le taniche, configura timer e UI.</summary>
  void Start()
  {
    tempoRimanente = tempoLimite;
    player = GameObject.FindGameObjectWithTag("Car");

    // Trova tutti i punti di spawn con tag "FuelTank"
    GameObject[] nodes = GameObject.FindGameObjectsWithTag("FuelTank");

    if (nodes.Length == 0) { Debug.LogError("Benzina: Nessun FuelTank trovato!"); return; }
    if (tanicaPrefab == null) { Debug.LogError("Benzina: Prefab tanica non assegnato!"); return; }

    // Inizializza gli array per le taniche
    taniche = new GameObject[numeroTaniche];
    posizioniIniziali = new Vector3[numeroTaniche];

    // Mescola i nodi (le taniche di benzina) per una distribuzione casuale
    for (int i = nodes.Length - 1; i > 0; i--)
    {
      int j = Random.Range(0, i + 1);
      (nodes[i], nodes[j]) = (nodes[j], nodes[i]);
    }

    // Crea ogni tanica in una posizione casuale
    for (int i = 0; i < numeroTaniche; i++)
    {
      // Posiziona le taniche leggermente sopra il terreno
      Vector3 pos = nodes[i].transform.position + Vector3.up * 0.8f;
      taniche[i] = Instantiate(tanicaPrefab, pos, Quaternion.identity);
      taniche[i].name = $"Tanica_Benzina_{i}";
      taniche[i].transform.localScale *= 5f; // scala per visibilità
      posizioniIniziali[i] = pos;
    }

    AggiornaUI(false);
  }

  /// <summary>Aggiorna timer, animazioni e controlla raccolta.</summary>
  void Update()
  {
    if (giocoFinito) return;

    // Decrementa il timer e controlla se è scaduto
    tempoRimanente -= Time.deltaTime;
    if (tempoRimanente <= 0f) { TerminaGioco(false); return; } // Game Over

    // Flag per indicare se abbiamo aggiunto secondi in questo frame
    bool aggiungiSecondi = false;

    // Mostra UI (timer/punteggio) alla fine dell'update, dopo eventuali raccolte
    if (taniche == null || player == null) { AggiornaUI(false); return; }

    for (int i = 0; i < taniche.Length; i++)
    {
      if (taniche[i] == null) continue;

      Transform t = taniche[i].transform;

      // Animazione: rotazione continua sull'asse Y
      t.Rotate(0, 50f * Time.deltaTime, 0);

      // Animazione: fluttuazione sinusoidale verticale
      float fluttuazione = Mathf.Sin(Time.time * 2f + i) * 0.15f;
      t.position = posizioniIniziali[i] + Vector3.up * fluttuazione;

      // Se il player è abbastanza vicino ad una tanica, allora raccoglila
      if (Vector3.Distance(player.transform.position, t.position) <= 3f)
      {
        // Incrementa punteggio, aggiunge tempo bonus, riproduce suono e distrugge la tanica raccolta
        punteggio++; tempoRimanente += 10f; aggiungiSecondi = true;
        audioSource?.PlayOneShot(audioSource.clip, 3f);
        Destroy(taniche[i]);
        taniche[i] = null;

        // Controlla condizione di vittoria
        if (punteggio >= numeroTaniche) TerminaGioco(true); // Vittoria!
      }
    }

    // Aggiorna UI dopo aver processato eventuali raccolte
    AggiornaUI(aggiungiSecondi);

    // Dopo aver mostrato il testo "+10s", gestisci il fade out
    if (timeAddedTimer > 0f && timeAddedText)
    {
      timeAddedTimer -= Time.deltaTime;
      float alpha = Mathf.Clamp01(timeAddedTimer / 2f);
      timeAddedText.color = new Color(timeAddedText.color.r, timeAddedText.color.g, timeAddedText.color.b, alpha);
    }
    else timeAddedText.text = "";
  }

  /// <summary>Aggiorna tutti gli elementi UI (punteggio e timer).</summary>
  void AggiornaUI(bool aggiungiSecondi)
  {
    // Aggiorna il contatore punteggio
    if (punteggioText != null) punteggioText.text = $"{punteggio}/{numeroTaniche}";

    // Aggiorna il timer con formato MM:SS
    if (timerText != null)
    {
      float tempo = Mathf.Max(0f, tempoRimanente);
      int minuti = Mathf.FloorToInt(tempo / 60);
      int secondi = Mathf.FloorToInt(tempo % 60);
      timerText.text = $"Tempo rimanente: {minuti:00}:{secondi:00}";

      // Colore dinamico basato sul tempo rimanente (rosso se <30s, giallo se <60s, bianco altrimenti)
      timerText.color = tempo <= 30f ? Color.red :
                        tempo <= 60f ? Color.yellow : Color.white;
    }

    // Aggiungi e mostra il testo "+10s" se abbiamo raccolto una tanica
    if (timeAddedText && aggiungiSecondi)
    {
      timeAddedText.text = "+10s";
      timeAddedTimer = 2f;
      // imposta alpha a 1 immediatamente
      timeAddedText.color = new Color(timeAddedText.color.r, timeAddedText.color.g, timeAddedText.color.b, 1f);
    }
  }

  /// <summary>Termina il gioco mostrando la schermata appropriata</summary>
  /// <param name="vittoria">True se il giocatore ha vinto, False per game over</param>
  void TerminaGioco(bool vittoria)
  {
    if (giocoFinito) return; // Previene chiamate multiple
    giocoFinito = true;

    AggiornaUI(false); // Aggiorna UI una volta con valori finali

    // Ferma tutti gli audio nella scena
    foreach (var audio in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
      if (audio.isPlaying) audio.Pause();
    AudioListener.volume = 0f;

    // Disabilita i controlli del giocatore
    if (player != null)
    {
      var car = player.GetComponent<CarController>();
      if (car != null) car.enabled = false;

      var rb = player.GetComponent<Rigidbody>();
      if (rb != null)
      {
        // Azzeriamo la velocità prima di rendere il corpo kinematic
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
      }
    }

    // Mostra la schermata finale appropriata
    if (vittoria) immagineVittoria?.gameObject.SetActive(true);
    else if (immagineGameOver != null) immagineGameOver.gameObject.SetActive(true);
    else Debug.LogError("Immagine Game Over non assegnata!");

    // Ferma il tempo di gioco con un piccolo ritardo per permettere all'UI di aggiornarsi
    StartCoroutine(FermaTempoConRitardo());
  }

  /// <summary>Ferma il tempo dopo breve ritardo (per permettere aggiornamento UI).</summary>
  IEnumerator FermaTempoConRitardo()
  {
    yield return null; // Aspetta un frame
    yield return new WaitForSecondsRealtime(0.1f); // Piccolo ritardo aggiuntivo
    Time.timeScale = 0f; // Pausa il gioco
  }
}
