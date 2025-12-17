using UnityEngine;
using UnityEngine.UI;

/// <summary>Gestisce la scelta dell'auto dopo aver raccolto tutti gli oggetti necessari.</summary>
public class SceltaAuto : MonoBehaviour
{
  public GameObject[] cars;
  public Transform spawnPoint;
  public GameObject garageDoor, player;
  public Camera playerCam;
  public Text inventoryText;

  GameObject previewCar;
  bool autoConfermata, hasExitedGarage;

  bool HaTuttiGliOggetti() =>
    PlayerController.Instance?.Has("Batteria") == true &&
    PlayerController.Instance.Has("Benzina") &&
    PlayerController.Instance.Has("Chiave");

  void Update()
  {
    // Se non ha tutti gli oggetti necessari, non fare nulla
    if (!HaTuttiGliOggetti()) return;

    // Scegli l'auto premendo 1, 2 o 3 e spawnala nel punto prestabilito
    if (!previewCar)
      inventoryText.text = "Premi 1, 2 o 3 per selezionare l'auto. Alcune macchine hanno più velocità, altre più tempo di gioco!";

    for (int i = 0; i < cars.Length; i++)
    {
      if (Input.GetKeyDown(KeyCode.Alpha1 + i))
      {
        if (previewCar) Destroy(previewCar);
        previewCar = Instantiate(cars[i], spawnPoint.position, spawnPoint.rotation);
        SetCarActive(previewCar, false);

        // Mostra le statistiche dell'auto selezionata
        var car = previewCar.GetComponent<CarController>();
        var benzina = previewCar.GetComponent<Benzina>();
        int velocita = car ? car.maxSpeed : 0;
        int timer = benzina ? Mathf.FloorToInt(benzina.tempoLimite / 60) : 0;
        inventoryText.text = $"⚡ {velocita} km/h   ⏱ {timer} min\nPremi E per confermare";
      }
    }

    // Conferma la macchina con E
    if (Input.GetKeyDown(KeyCode.E) && previewCar)
    {
      SetCarActive(previewCar, true);

      // Disattiva player e la camera del player, poi attiva la camera dell'auto
      player.SetActive(false);
      playerCam.gameObject.SetActive(false);
      previewCar.GetComponentInChildren<Camera>(true).gameObject.SetActive(true);

      // Apri la porta del garage e assegna il tag "Car" all'auto selezionata
      if (garageDoor) garageDoor.GetComponent<Interactions>()?.SetDoorOpen(true);
      previewCar.tag = "Car";
      autoConfermata = true;
    }

    // Se l'auto è confermata, aspetta che esca dal garage per abilitare la camera di guida
    if (autoConfermata && previewCar && !hasExitedGarage && Vector3.Distance(previewCar.transform.position, spawnPoint.position) > 5f)
    {
      hasExitedGarage = true;
      previewCar.GetComponent<CarController>()?.EnableGTACamera();
      garageDoor.GetComponent<Interactions>()?.SetDoorOpen(false);
      return;
    }
  }

  void SetCarActive(GameObject car, bool active)
  {
    // Abilita o disabilita i componenti necessari per far funzionare o fermare l'auto
    car.GetComponent<Rigidbody>().isKinematic = !active;
    car.GetComponent<CarController>().enabled = active;
    car.GetComponent<Benzina>().enabled = active;
    foreach (var col in car.GetComponentsInChildren<Collider>()) col.enabled = active;

    // Se la macchina ha una camera di preview, la disattiviamo
    car.GetComponentInChildren<Camera>(true)?.gameObject.SetActive(false);
  }
}
