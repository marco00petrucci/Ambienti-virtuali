using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CONTROLLER DELLA MACCHINA
/// ============================
/// 
/// CONTROLLI:
/// - W = Accelera (vai avanti)
/// - S = Retromarcia (vai indietro)
/// - A = Sterza a sinistra
/// - D = Sterza a destra
/// - SPAZIO = Freno a mano (per driftare)
/// - R = Raddrizza la macchina se si ribalta
/// - C = Guarda dietro (tenere premuto)
/// - V = Cambia inquadratura della camera
/// - TAB = Mostra/nascondi controlli
/// 
/// FUNZIONALITÀ:
/// - Movimento realistico con fisica delle ruote
/// - Drift con il freno a mano
/// - Camera che segue la macchina stile GTA
/// - Respawn automatico se cadi nell'acqua
/// - Effetti visivi (fumo gomme, segni sull'asfalto)
/// - Suoni del motore e delle gomme
/// </summary>
public class CarController : MonoBehaviour
{
  // =====================================================================
  // IMPOSTAZIONI VELOCITÀ
  // =====================================================================
  [Header("Velocità e Accelerazione")]

  [Range(20, 300)]
  [Tooltip("Velocità massima in km/h")]
  public int maxSpeed = 300;

  [Range(10, 120)]
  [Tooltip("Velocità massima in retromarcia")]
  public int maxReverseSpeed = 45;

  [Range(1, 10)]
  [Tooltip("Potenza accelerazione: 1=lenta, 10=velocissima")]
  public int accelerationMultiplier = 2;

  [Range(1, 10)]
  [Tooltip("Quanto velocemente rallenta quando non acceleri")]
  public int decelerationMultiplier = 2;

  [Range(100, 600)]
  [Tooltip("Potenza dei freni")]
  public int brakeForce = 350;

  [Range(1, 10)]
  [Tooltip("Quanto le ruote diventano scivolose quando tiri il freno a mano (1=debole, 10=molto)")]
  public float handbrakeDriftMultiplier = 6f;

  // =====================================================================
  // COSTANTI DI GUIDA (valori fissi che non cambiano mai)
  // =====================================================================

  public const float steeringSpeed = 0.5f; // Quanto è reattivo lo sterzo

  // =====================================================================
  // RUOTE - Collega qui le ruote della macchina nell'Inspector
  // =====================================================================
  [Header("Ruote - Modelli 3D visibili")]
  [Tooltip("Ruota anteriore sinistra (il modello 3D che vedi)")]
  public GameObject frontLeftMesh;
  [Tooltip("Ruota anteriore destra")]
  public GameObject frontRightMesh;
  [Tooltip("Ruota posteriore sinistra")]
  public GameObject rearLeftMesh;
  [Tooltip("Ruota posteriore destra")]
  public GameObject rearRightMesh;

  [Header("Ruote - Collider per la fisica")]
  [Tooltip("WheelCollider anteriore sinistro (gestisce la fisica)")]
  public WheelCollider frontLeftCollider;
  [Tooltip("WheelCollider anteriore destro")]
  public WheelCollider frontRightCollider;
  [Tooltip("WheelCollider posteriore sinistro")]
  public WheelCollider rearLeftCollider;
  [Tooltip("WheelCollider posteriore destro")]
  public WheelCollider rearRightCollider;

  // =====================================================================
  // EFFETTI VISIVI
  // =====================================================================
  [Header("Effetti Visivi")]
  [Tooltip("Particelle di fumo dalla ruota posteriore sinistra")]
  public ParticleSystem RLWParticleSystem;
  [Tooltip("Particelle di fumo dalla ruota posteriore destra")]
  public ParticleSystem RRWParticleSystem;

  [Tooltip("Segno nero lasciato dalla ruota posteriore sinistra")]
  public TrailRenderer RLWTireSkid;
  [Tooltip("Segno nero lasciato dalla ruota posteriore destra")]
  public TrailRenderer RRWTireSkid;

  // =====================================================================
  // INTERFACCIA UTENTE
  // =====================================================================
  [Header("Interfaccia")]
  [Tooltip("Testo che mostra la velocità (es: '120')")]
  public Text carSpeedText;
  [Tooltip("Pannello con la lista dei controlli")]
  public GameObject controlsUI;

  // =====================================================================
  // AUDIO
  // =====================================================================
  [Header("Audio")]
  [Tooltip("Suono del motore della macchina")]
  public AudioSource carEngineSound;
  [Tooltip("Suono delle gomme che strisciano")]
  public AudioSource tireScreechSound;

  // =====================================================================
  // CAMERA
  // =====================================================================
  [Header("Camera")]
  [Tooltip("La camera che segue la macchina")]
  public Camera playerCamera;

  [Tooltip("Sensibilità del mouse per ruotare la camera")]
  public float mouseSensitivity = 3f;

  [Tooltip("Secondi prima che la camera torni dietro la macchina")]
  public float cameraReturnDelay = 2f;

  [Tooltip("Velocità con cui la camera torna in posizione")]
  public float cameraReturnSpeed = 3f;

  // =====================================================================
  // VARIABILI DI STATO (tengono traccia di cosa sta succedendo)
  // =====================================================================
  private float carSpeed; // Velocità attuale della macchina in km/h
  private bool isDrifting; // La macchina sta driftando? (scivolando di lato)
  private bool isTractionLocked; // Il freno a mano è tirato?
  private bool useGTACamera; // La camera stile GTA è attiva?
  private Rigidbody carRigidbody; // Componente Rigidbody che gestisce la fisica della macchina

  // Valori dei controlli (da -1 a 1)
  private float steeringAxis;   // Sterzo: -1=tutto sinistra, 1=tutto destra
  private float throttleAxis;   // Acceleratore: -1=retromarcia, 1=avanti
  private float driftingAxis;   // Quanto stiamo driftando (0=niente, 1=massimo)

  // Velocità della macchina nelle direzioni locali
  private float localVelocityX; // Velocità laterale (positivo=destra)
  private float localVelocityZ; // Velocità avanti/indietro (positivo=avanti)
  private bool deceleratingCar; // La macchina sta rallentando automaticamente?
  private bool isInitialized; // Lo script ha finito di inizializzarsi?

  // Array che contengono tutte e 4 le ruote (per gestirle con un ciclo)
  private WheelCollider[] wheelColliders;
  private GameObject[] wheelMeshes;

  // Dati sulla frizione delle ruote (quanto "grip" hanno)
  private WheelFrictionCurve[] wheelFrictions;
  private float[] originalExtremumSlips;

  private float initialCarEngineSoundPitch; // Tonalità originale del suono del motore

  // Ultima posizione dove la macchina era al sicuro
  private Vector3 lastSafePosition;
  private Quaternion lastSafeRotation;
  private float lastSafePositionUpdateTime; // Quando abbiamo salvato l'ultima posizione sicura
  private float lastRespawnTime; // Quando è avvenuto l'ultimo respawn

  // =====================================================================
  // VARIABILI PER LA CAMERA
  // =====================================================================

  private float cameraYaw; // Rotazione orizzontale della camera (sinistra/destra)
  private float cameraPitch = 15f; // Rotazione verticale della camera (alto/basso)
  private float lastMouseMoveTime; // Quando il mouse si è mosso l'ultima volta
  private bool isRearViewActive; // Stiamo guardando dietro? (tasto C)
  private Vector3 cameraVelocity; //Usato per il movimento fluido della camera
  private int cameraMode; // Modalità camera attuale (0=normale, 1=vicina, 2=dall'alto)

  void Start()
  {
    // Prendi il componente Rigidbody (gestisce la fisica della macchina)
    carRigidbody = GetComponent<Rigidbody>();

    // Metti tutte le ruote in array per gestirle con un ciclo
    wheelColliders = new WheelCollider[] { frontLeftCollider, frontRightCollider, rearLeftCollider, rearRightCollider };
    wheelMeshes = new GameObject[] { frontLeftMesh, frontRightMesh, rearLeftMesh, rearRightMesh };

    // Inizializza gli array per la frizione delle ruote
    wheelFrictions = new WheelFrictionCurve[4];
    originalExtremumSlips = new float[4];

    // Salva i valori originali di frizione di ogni ruota (serviranno per il sistema di drift)
    for (int i = 0; i < 4; i++)
    {
      // Prendi la frizione laterale della ruota
      WheelFrictionCurve frizione = wheelColliders[i].sidewaysFriction;

      // Copia tutti i valori in una nuova struttura
      wheelFrictions[i] = new WheelFrictionCurve
      {
        extremumSlip = frizione.extremumSlip,
        extremumValue = frizione.extremumValue,
        asymptoteSlip = frizione.asymptoteSlip,
        asymptoteValue = frizione.asymptoteValue,
        stiffness = frizione.stiffness
      };

      // Salva il valore originale di extremumSlip
      originalExtremumSlips[i] = frizione.extremumSlip;
    }

    // Salva la tonalità originale del suono del motore
    if (carEngineSound != null) initialCarEngineSoundPitch = carEngineSound.pitch;

    // Avvia funzioni che si ripetono ogni 0.1 secondi
    InvokeRepeating(nameof(UpdateSpeedUI), 0f, 0.1f);
    InvokeRepeating(nameof(UpdateSounds), 0f, 0.1f);

    // Mostra i controlli all'inizio
    controlsUI?.SetActive(true);

    // Salva la posizione iniziale come "sicura" per il respawn
    lastSafePosition = transform.position;
    lastSafeRotation = transform.rotation;
    lastSafePositionUpdateTime = Time.time;

    // Ora lo script è pronto!
    isInitialized = true;
  }

  void Update()
  {
    // Se lo script non ha finito di inizializzarsi, non fare nulla
    if (!isInitialized) return;

    // ---------------------------------------------------------------------
    // CALCOLA LA VELOCITÀ ATTUALE
    // ---------------------------------------------------------------------
    // Formula: circonferenza ruota × giri al minuto × 60 minuti / 1000 = km/h
    // La circonferenza è 2 × pi greco × raggio
    float circonferenzaRuota = 2 * Mathf.PI * wheelColliders[0].radius;
    float giriAlMinuto = wheelColliders[0].rpm;
    carSpeed = (circonferenzaRuota * giriAlMinuto * 60) / 1000;

    // ---------------------------------------------------------------------
    // CALCOLA LA DIREZIONE DEL MOVIMENTO
    // ---------------------------------------------------------------------
    // Converti la velocità globale in velocità locale (rispetto alla macchina)
    // Questo ci dice se ci muoviamo avanti, indietro o di lato
    Vector3 velocitaLocale = transform.InverseTransformDirection(carRigidbody.linearVelocity);
    localVelocityX = velocitaLocale.x; // Positivo = ci muoviamo a destra
    localVelocityZ = velocitaLocale.z; // Positivo = ci muoviamo in avanti

    // ---------------------------------------------------------------------
    // GESTISCI I CONTROLLI
    // ---------------------------------------------------------------------
    HandleDrivingInput();

    // TAB = mostra/nascondi pannello controlli
    if (Input.GetKeyDown(KeyCode.Tab) && controlsUI != null) controlsUI.SetActive(!controlsUI.activeSelf);

    // C = guarda dietro (mentre tieni premuto)
    isRearViewActive = Input.GetKey(KeyCode.C);

    // V = cambia modalità camera (normale → vicina → dall'alto → normale...)
    if (Input.GetKeyDown(KeyCode.V)) cameraMode = (cameraMode + 1) % 3; // Cicla tra 0, 1, 2

    // R = raddrizza la macchina
    if (Input.GetKeyDown(KeyCode.R))
    {
      // Ferma completamente la macchina
      ResetStatoMacchina();

      // Solleva la macchina di 1.5 metri e raddrizzala
      Vector3 nuovaPosizione = transform.position + Vector3.up * 1.5f;

      // Mantieni solo la rotazione orizzontale (yaw), azzera inclinazione
      Quaternion nuovaRotazione = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

      transform.position = nuovaPosizione;
      transform.rotation = nuovaRotazione;
    }

    // ---------------------------------------------------------------------
    // SALVA LA POSIZIONE SICURA (ogni secondo)
    // ---------------------------------------------------------------------
    if (Time.time - lastSafePositionUpdateTime >= 1f)
    {
      // Controlla che TUTTE e 4 le ruote tocchino terra
      bool tutteLeRuoteATerra = wheelColliders[0].isGrounded && wheelColliders[1].isGrounded && wheelColliders[2].isGrounded && wheelColliders[3].isGrounded;

      // Controlla che la macchina sia dritta
      // Vector3.Dot confronta due direzioni: se > 0.7, sono abbastanza allineate
      // transform.up = direzione "sopra" della macchina
      // Vector3.up = direzione "sopra" del mondo
      bool macchinaInPiedi = Vector3.Dot(transform.up, Vector3.up) > 0.7f;

      // Salva la posizione solo se la macchina è al sicuro
      if (tutteLeRuoteATerra && macchinaInPiedi)
      {
        lastSafePosition = transform.position;
        lastSafeRotation = transform.rotation;
      }

      lastSafePositionUpdateTime = Time.time;
    }

    // ---------------------------------------------------------------------
    // RESPAWN SE CADE NELL'ACQUA
    // ---------------------------------------------------------------------
    if (transform.position.y < -10f)
    {
      // Evita respawn troppo frequenti (minimo 2 secondi tra uno e l'altro)
      if (Time.time - lastRespawnTime < 2f) return;
      lastRespawnTime = Time.time;

      // Ferma la macchina
      ResetStatoMacchina();

      // Teletrasporta all'ultima posizione sicura, 2 metri più in alto
      transform.position = lastSafePosition + Vector3.up * 2f;
      transform.rotation = lastSafeRotation;
    }

    // ---------------------------------------------------------------------
    // MUOVI I MODELLI 3D DELLE RUOTE PER FARLI CORRISPONDERE AI WHEELCOLLIDER
    // ---------------------------------------------------------------------
    for (int i = 0; i < 4; i++)
    {
      // Prendi la posizione e rotazione attuali della ruota fisica
      wheelColliders[i].GetWorldPose(out Vector3 posizione, out Quaternion rotazione);

      // Applica posizione e rotazione al modello 3D
      wheelMeshes[i].transform.position = posizione;
      wheelMeshes[i].transform.rotation = rotazione;
    }
  }

  /// <summary>GESTIONE INPUT - Legge i tasti premuti e chiama le funzioni giuste</summary>
  void HandleDrivingInput()
  {
    // ----- ACCELERAZIONE -----
    if (Input.GetKey(KeyCode.W))
    {
      StopDeceleration();
      UpdateDriftState(); // Controlla se stiamo driftando

      // Aumenta gradualmente l'acceleratore (da 0 a 1)
      throttleAxis += Time.deltaTime * 3f;
      if (throttleAxis > 1f) throttleAxis = 1f;

      // Se ci stiamo muovendo all'indietro, frena prima di accelerare
      if (localVelocityZ < -1f) SetWheelBrakeTorque(brakeForce);

      // Se non abbiamo raggiunto la velocità massima, accelera
      else if (Mathf.RoundToInt(carSpeed) < maxSpeed)
      {
        float forzaMotore = accelerationMultiplier * 50f * throttleAxis;
        SetWheelMotorTorque(forzaMotore);
        SetWheelBrakeTorque(0);
      }
      // Se siamo alla velocità massima, smetti di accelerare
      else SetWheelMotorTorque(0);
    }

    // ----- RETROMARCIA -----
    if (Input.GetKey(KeyCode.S))
    {
      StopDeceleration();
      UpdateDriftState(); // Controlla se stiamo driftando

      // Diminuisci gradualmente l'acceleratore (da 0 a -1)
      throttleAxis -= Time.deltaTime * 3f;
      if (throttleAxis < -1f) throttleAxis = -1f;

      // Se ci stiamo muovendo in avanti, frena prima
      if (localVelocityZ > 1f) SetWheelBrakeTorque(brakeForce);

      // Se non abbiamo raggiunto la velocità massima di retromarcia
      else if (Mathf.Abs(Mathf.RoundToInt(carSpeed)) < maxReverseSpeed)
      {
        float forzaMotore = accelerationMultiplier * 50f * throttleAxis;
        SetWheelMotorTorque(forzaMotore);
        SetWheelBrakeTorque(0);
      }
      else SetWheelMotorTorque(0);
    }

    // ----- STERZO -----
    if (Input.GetKey(KeyCode.A)) Steer(-1); // Sterza a sinistra
    if (Input.GetKey(KeyCode.D)) Steer(1);  // Sterza a destra

    // ----- FRENO A MANO (DRIFT) -----
    // Il drift funziona così:
    // 1. Quando premi SPAZIO, le ruote diventano più "scivolose"
    // 2. Questo fa perdere aderenza alla macchina, che inizia a scivolare
    // 3. Quando rilasci SPAZIO, le ruote tornano normali gradualmente
    if (Input.GetKey(KeyCode.Space))
    {
      StopDeceleration();
      // Ferma eventuali recuperi di trazione in corso
      CancelInvoke(nameof(RecoverTraction));

      // Aumenta gradualmente l'effetto drift
      driftingAxis += Time.deltaTime;
      if (driftingAxis > 1f) driftingAxis = 1f;

      // Calcola quanto devono scivolare le ruote (più scivolano, più è facile driftare)
      // Assicuriamoci di avere un valore minimo di driftingAxis così lo slip parte subito
      if (driftingAxis <= 0f) driftingAxis = 0.25f;

      // Moltiplicatore finale applicato alle extremumSlip originali
      float moltiplicatore = handbrakeDriftMultiplier * driftingAxis;

      // Applica lo scivolamento alle ruote (sempre mentre tieni il freno a mano)
      RendiRuoteScivolose(moltiplicatore);

      // Controlla se stiamo scivolando di lato
      isDrifting = Mathf.Abs(localVelocityX) > 2.5f;

      // Segna che il freno a mano è attivo
      isTractionLocked = true;

      // Aggiorna effetti visivi
      UpdateDriftEffects();
    }

    // Quando rilasci lo spazio, recupera la trazione
    if (Input.GetKeyUp(KeyCode.Space)) RecoverTraction();

    // ----- RALLENTAMENTO AUTOMATICO -----
    // Se non premi né W né S, la macchina rallenta da sola
    if (!Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.W))
    {
      // Ferma il motore
      SetWheelMotorTorque(0);
      SetWheelBrakeTorque(0);

      // Se non stiamo già rallentando e non usiamo il freno a mano
      if (!Input.GetKey(KeyCode.Space) && !deceleratingCar)
      {
        // Avvia il rallentamento automatico
        InvokeRepeating(nameof(DecelerateCar), 0f, 0.1f);
        deceleratingCar = true;
      }
    }

    // ----- STERZO AUTOMATICO AL CENTRO -----
    // Se non sterzi, le ruote tornano dritte piano piano
    if (!Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D) && steeringAxis != 0f)
    {
      // Muovi steeringAxis verso 0
      steeringAxis = Mathf.MoveTowards(steeringAxis, 0f, Time.deltaTime * 10f * steeringSpeed);

      // Se le ruote sono quasi dritte, mettile perfettamente dritte
      if (Mathf.Abs(wheelColliders[0].steerAngle) < 1f) steeringAxis = 0f;

      ApplySteering();
    }
  }

  /// <summary>Ferma il rallentamento automatico della macchina</summary>
  void StopDeceleration()
  {
    CancelInvoke(nameof(DecelerateCar));
    deceleratingCar = false;
  }

  /// <summary>CAMERA STILE GTA - Viene chiamato dopo Update, perfetto per la camera
  /// La camera orbita intorno alla macchina come in GTA:
  /// - Puoi ruotarla con il mouse
  /// - Torna automaticamente dietro la macchina se non muovi il mouse
  /// - Premendo C guardi dietro
  /// </summary>
  void LateUpdate()
  {
    if (playerCamera != null && useGTACamera)
    {
      // Prendi i parametri della camera in base alla modalità selezionata
      float distanzaDallaMacchina, altezzaCamera, inclinazioneDefault;

      // Modalità 0 = normale, 1 = vicina, 2 = dall'alto
      switch (cameraMode)
      {
        case 1: // Modalità VICINA (quasi dentro la macchina)
          distanzaDallaMacchina = 0.5f;
          altezzaCamera = 1.6f;
          inclinazioneDefault = 2f;
          break;
        case 2: // Modalità DALL'ALTO (visuale strategica)
          distanzaDallaMacchina = 25f;
          altezzaCamera = 5f;
          inclinazioneDefault = 20f;
          break;
        default: // Modalità NORMALE (default)
          distanzaDallaMacchina = 7f;
          altezzaCamera = 2f;
          inclinazioneDefault = 15f;
          break;
      }

      // ----- LEGGI IL MOVIMENTO DEL MOUSE -----
      float movimentoMouseX = Input.GetAxis("Mouse X");
      float movimentoMouseY = Input.GetAxis("Mouse Y");

      // Se il mouse si è mosso, aggiorna la rotazione della camera
      if (Mathf.Abs(movimentoMouseX) > 0.01f || Mathf.Abs(movimentoMouseY) > 0.01f)
      {
        lastMouseMoveTime = Time.time;

        // Aggiungi la rotazione orizzontale
        cameraYaw += movimentoMouseX * mouseSensitivity;

        // Aggiungi la rotazione verticale (con limiti)
        cameraPitch -= movimentoMouseY * mouseSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch, -10f, 60f);
      }

      // Se il mouse è fermo da un po' e non stiamo guardando dietro, riporta la camera dietro la macchina
      else if (Time.time - lastMouseMoveTime > cameraReturnDelay && !isRearViewActive)
      {
        cameraYaw = Mathf.LerpAngle(cameraYaw, 0f, cameraReturnSpeed * Time.deltaTime);
        cameraPitch = Mathf.Lerp(cameraPitch, inclinazioneDefault, cameraReturnSpeed * Time.deltaTime);
      }

      // Se premi C, guarda dietro (aggiungi 180 gradi), altrimenti usa la rotazione calcolata col mouse
      float rotazioneOrizzontale;
      if (isRearViewActive) rotazioneOrizzontale = 180f;
      else rotazioneOrizzontale = cameraYaw;

      // ----- CALCOLA LA POSIZIONE DELLA CAMERA -----
      // La camera si trova su una sfera immaginaria intorno alla macchina
      // Usiamo angoli in radianti per la trigonometria

      // Angolo totale = rotazione macchina + rotazione camera + 180° (per stare dietro)
      float angoloTotale = (transform.eulerAngles.y + rotazioneOrizzontale + 180f) * Mathf.Deg2Rad;
      float angoloVerticale = cameraPitch * Mathf.Deg2Rad;

      // Calcola l'offset (spostamento) rispetto al centro
      // Queste sono le formule per trovare un punto su una sfera
      float offsetX = Mathf.Sin(angoloTotale) * Mathf.Cos(angoloVerticale) * distanzaDallaMacchina;
      float offsetY = Mathf.Sin(angoloVerticale) * distanzaDallaMacchina;
      float offsetZ = Mathf.Cos(angoloTotale) * Mathf.Cos(angoloVerticale) * distanzaDallaMacchina;
      Vector3 offset = new Vector3(offsetX, offsetY, offsetZ);

      // Il centro intorno a cui ruota la camera
      Vector3 centroMacchina = transform.position + Vector3.up * altezzaCamera;

      Vector3 posizioneDesiderata = centroMacchina + offset;

      // ----- MUOVI LA CAMERA FLUIDAMENTE -----
      // SmoothDamp fa un movimento morbido, non a scatti
      playerCamera.transform.position = Vector3.SmoothDamp(
          playerCamera.transform.position,  // Posizione attuale
          posizioneDesiderata,              // Dove vogliamo andare
          ref cameraVelocity,               // Velocità attuale (gestita automaticamente)
          0.1f                              // Tempo per raggiungere la destinazione
      );

      // Fai guardare la camera verso la macchina
      playerCamera.transform.LookAt(centroMacchina);
    }
  }

  /// <summary>Attiva la camera stile GTA che ruota intorno alla macchina. Chiamata quando la macchina esce dal garage</summary>
  public void EnableGTACamera()
  {
    useGTACamera = true;
    cameraYaw = 0f;      // Resetta rotazione orizzontale
    cameraPitch = 15f;   // Resetta rotazione verticale
    lastMouseMoveTime = Time.time;
  }

  // =====================================================================
  // INTERFACCIA UTENTE E SUONI
  // =====================================================================

  /// <summary>Aggiorna il testo della velocità mostrato sullo schermo, prendendo il valore assoluto e arrotondando</summary>
  void UpdateSpeedUI()
  {
    if (carSpeedText != null) carSpeedText.text = Mathf.RoundToInt(Mathf.Abs(carSpeed)).ToString();
  }

  /// <summary>Aggiorna i suoni del motore e delle gomme</summary>
  void UpdateSounds()
  {
    // ----- SUONO MOTORE -----
    // Più vai veloce, più il motore fa un suono acuto (pitch alto)
    if (carEngineSound != null)
    {
      float velocitaAttuale = Mathf.Abs(carRigidbody.linearVelocity.magnitude);
      carEngineSound.pitch = initialCarEngineSoundPitch + (velocitaAttuale / 25f);
    }

    // ----- SUONO GOMME -----
    if (tireScreechSound != null)
    {
      // Le gomme strisciano se stiamo driftando O se il freno a mano è tirato ad alta velocità
      bool deveStrisciare = isDrifting || (isTractionLocked && Mathf.Abs(carSpeed) > 12f);

      if (deveStrisciare && !tireScreechSound.isPlaying) tireScreechSound.Play();
      else if (!deveStrisciare && tireScreechSound.isPlaying) tireScreechSound.Stop();
    }
  }

  /// <summary>Sterza nella direzione specificata</summary>
  /// <param name="direction">-1 = sinistra, +1 = destra</param>
  void Steer(int direction)
  {
    // Aumenta/diminuisci il valore dello sterzo
    steeringAxis += direction * Time.deltaTime * 10f * steeringSpeed;

    // Limita il valore tra -1 e 1
    steeringAxis = Mathf.Clamp(steeringAxis, -1f, 1f);

    ApplySteering();
  }

  /// <summary>Applica l'angolo di sterzo alle ruote anteriori</summary>
  void ApplySteering()
  {
    // Applica l'angolo gradualmente (per un movimento fluido)
    float angoloAttuale = wheelColliders[0].steerAngle;
    float nuovoAngolo = Mathf.Lerp(angoloAttuale, steeringAxis * 40, steeringSpeed);

    // Applica lo stesso angolo a entrambe le ruote anteriori
    wheelColliders[0].steerAngle = nuovoAngolo;
    wheelColliders[1].steerAngle = nuovoAngolo;
  }

  // =====================================================================
  // MOTORE E FRENI
  // =====================================================================

  /// <summary>Rallenta automaticamente quando non si preme nessun tasto</summary>
  public void DecelerateCar()
  {
    UpdateDriftState();

    // Riporta l'acceleratore verso 0
    throttleAxis = Mathf.MoveTowards(throttleAxis, 0f, Time.deltaTime * 10f);
    if (Mathf.Abs(throttleAxis) < 0.15f) throttleAxis = 0f;

    // Rallenta la macchina moltiplicando la velocità per un valore < 1
    // Più alto è decelerationMultiplier, più velocemente rallenta
    float fattoreRallentamento = 1f / (1f + 0.025f * decelerationMultiplier);
    carRigidbody.linearVelocity = carRigidbody.linearVelocity * fattoreRallentamento;

    // Togli la forza motore
    SetWheelMotorTorque(0);

    // Se siamo quasi fermi, fermati completamente
    if (carRigidbody.linearVelocity.magnitude < 0.25f)
    {
      carRigidbody.linearVelocity = Vector3.zero;
      CancelInvoke(nameof(DecelerateCar));
    }
  }

  /// <summary>Applica forza motore o freni a tutte le ruote</summary>
  void SetWheelMotorTorque(float f) { foreach (var w in wheelColliders) w.motorTorque = f; }
  void SetWheelBrakeTorque(float f) { foreach (var w in wheelColliders) w.brakeTorque = f; }

  /// <summary>Controlla se la macchina sta driftando (scivolando di lato)</summary>
  void UpdateDriftState()
  {
    // Se la velocità laterale è maggiore di 2.5, stiamo driftando
    isDrifting = Mathf.Abs(localVelocityX) > 2.5f;

    // Aggiorna gli effetti visivi del drift
    UpdateDriftEffects();
  }

  // =====================================================================
  // SISTEMA DRIFT (FRENO A MANO)
  // =====================================================================

  /// <summary>Aggiorna gli effetti visivi del drift (fumo e segni gomme)</summary>
  void UpdateDriftEffects()
  {
    // ----- FUMO DALLE RUOTE -----
    if (RLWParticleSystem != null && RRWParticleSystem != null)
    {
      if (isDrifting) { RLWParticleSystem.Play(); RRWParticleSystem.Play(); }
      else { RLWParticleSystem.Stop(); RRWParticleSystem.Stop(); }
    }

    // ----- SEGNI NERI SULL'ASFALTO -----
    // NOTA: le scie di sgommata sono implementate con `TrailRenderer`. Lo script controlla la proprietà `emitting` qui sotto per avviare/fermare la scia.
    // Mostra i segni se: freno a mano tirato O scivolando molto, E velocità > 12 km/h
    bool mostraSegni = (isTractionLocked || Mathf.Abs(localVelocityX) > 5f) && Mathf.Abs(carSpeed) > 12f;

    if (RLWTireSkid != null) RLWTireSkid.emitting = mostraSegni;
    if (RRWTireSkid != null) RRWTireSkid.emitting = mostraSegni;
  }

  /// <summary>Recupera la trazione gradualmente dopo aver rilasciato il freno a mano</summary>
  public void RecoverTraction()
  {
    // Il freno a mano non è più attivo
    isTractionLocked = false;

    // Riduci gradualmente l'effetto drift
    driftingAxis -= Time.deltaTime / 1.5f;
    if (driftingAxis < 0f) driftingAxis = 0f;

    // Se le ruote sono ancora più scivolose del normale, continua a recuperare
    if (wheelFrictions[0].extremumSlip > originalExtremumSlips[0])
    {
      RendiRuoteScivolose(handbrakeDriftMultiplier * driftingAxis);

      // Richiama questa funzione tra poco per continuare il recupero
      Invoke(nameof(RecoverTraction), Time.deltaTime);
    }
    // Se le ruote sono troppo aderenti (bug), resetta ai valori originali
    else if (wheelFrictions[0].extremumSlip < originalExtremumSlips[0]) ResetFrizioneRuote();
  }

  /// <summary>Rende le ruote più scivolose (per il drift)</summary>
  /// <param name="moltiplicatore">Quanto più scivolose (1 = normale, 5 = molto scivoloso)</param>
  void RendiRuoteScivolose(float moltiplicatore)
  {
    for (int i = 0; i < 4; i++)
    {
      // Aumenta l'extremumSlip = la ruota scivola di più
      wheelFrictions[i].extremumSlip = originalExtremumSlips[i] * moltiplicatore;
      wheelColliders[i].sidewaysFriction = wheelFrictions[i];
    }
  }

  /// <summary>Riporta le ruote alla frizione normale</summary>
  void ResetFrizioneRuote()
  {
    // Controlla che gli array esistano (evita errori)
    if (wheelFrictions == null || originalExtremumSlips == null || wheelColliders == null) return;

    for (int i = 0; i < 4; i++)
    {
      // Ripristina i valori originali
      wheelFrictions[i].extremumSlip = originalExtremumSlips[i];
      wheelFrictions[i].stiffness = 1f;
      wheelColliders[i].sidewaysFriction = wheelFrictions[i];
    }

    // Resetta il valore del drift
    driftingAxis = 0f;
  }

  /// <summary>Ferma completamente la macchina e resetta tutti i valori</summary>
  void ResetStatoMacchina()
  {
    carRigidbody.linearVelocity = carRigidbody.angularVelocity = Vector3.zero;
    throttleAxis = steeringAxis = driftingAxis = 0f;

    // Resetta gli stati
    isDrifting = isTractionLocked = deceleratingCar = false;

    // Ferma le funzioni ripetute
    CancelInvoke(nameof(DecelerateCar));
    CancelInvoke(nameof(RecoverTraction));

    // Ripristina la frizione delle ruote
    ResetFrizioneRuote();
  }
}
