using UnityEngine;

public class CameraIntro : MonoBehaviour
{
  [Tooltip("Camera intro (da spegnere quando finisce)")]
  public Camera introCamera;

  [Tooltip("Oggetto Player da attivare")]
  public GameObject player;

  // Metodo invocato dall'Animation Event
  public void OnIntroFinished()
  {
    introCamera?.gameObject.SetActive(false);
    player?.SetActive(true);
  }
}