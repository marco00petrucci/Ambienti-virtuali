using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Controller del player a piedi: movimento WASD, mouse look, inventario</summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
  // Singleton per accesso globale
  public static PlayerController Instance { get; private set; }

  [Header("Movimento")]
  public float moveSpeed = 3.5f;
  public float mouseSensitivity = 150f;
  public Transform cameraTransform;

  [Header("Inventario (UI)")]
  public Text inventoryText;

  readonly HashSet<string> items = new();  // collezione senza duplicati
  CharacterController controller;          // per muovere il player e gestire collisioni semplici
  float xRotation;                         // rotazione verticale accumulata

  void Awake()
  {
    // Singleton semplice
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
  }

  void Start()
  {
    // Cache del componente e setup cursore
    controller = GetComponent<CharacterController>();
    Cursor.lockState = CursorLockMode.Locked; // blocca il cursore al centro della finestra
    Cursor.visible = false;                   // nasconde il cursore
  }

  void Update()
  {
    // --- Mouse look ---
    // Legge il movimento del mouse (scaled per deltaTime) e lo applica
    float mx = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
    float my = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
    xRotation = Mathf.Clamp(xRotation - my, -85f, 85f);
    cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    transform.Rotate(Vector3.up * mx);

    // --- Movimento sul piano XZ ---
    // Combina input orizzontali e li proietta nello spazio locale del player
    Vector3 move = (transform.right * Input.GetAxis("Horizontal") + transform.forward * Input.GetAxis("Vertical")) * moveSpeed;
    move.y = 0f; // indichiamo esplicitamente che non si muove in Y
    controller.Move(move * Time.deltaTime); // Move gestisce collisioni semplici

    // --- Mantieni altezza costante ---
    Vector3 p = transform.position; p.y = 1.06f; transform.position = p;
  }

  // --- Inventario minimale ---
  // Aggiunge un oggetto (no duplicati) e aggiorna l'UI se presente
  public void Add(string item)
  {
    if (items.Add(item)) UpdateInventoryUI();
  }
  public bool Has(string item) => items.Contains(item);

  void UpdateInventoryUI()
  {
    if (!inventoryText) return;
    inventoryText.text = "Raccolti: " + (items.Count == 0 ? "—" : string.Join(", ", items));
  }
}
