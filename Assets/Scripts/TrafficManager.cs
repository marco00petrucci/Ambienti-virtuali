using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

// Gestisce spawn e comportamento del traffico, ostacoli e pale eoliche
public class TrafficManager : MonoBehaviour
{
  [Header("Impostazioni")]
  [Tooltip("Cartella contenente i prefab dei veicoli")]
  public Object vehiclesFolder;
  GameObject[] vehiclePrefabs;

  void Start()
  {
    // Carica i modelli delle auto dalla cartella
    var list = new List<GameObject>();
    foreach (string guid in AssetDatabase.FindAssets("t:GameObject", new[] { AssetDatabase.GetAssetPath(vehiclesFolder) }))
      list.Add(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)));
    vehiclePrefabs = list.ToArray();

    // Trova TUTTI i pathNode nella scena
    var allNodes = GameObject.FindGameObjectsWithTag("pathNode");
    if (allNodes.Length == 0) return;

    // Trova i nodi radice (nodi senza genitore 'pathNode'), usati come punto di ritorno quando una strada finisce
    var roots = new List<Transform>();
    foreach (var n in allNodes)
      if (!n.transform.parent || !n.transform.parent.CompareTag("pathNode")) roots.Add(n.transform);

    // Funzione per risalire al root di appartenenza
    Transform FindRoot(Transform node)
    {
      var current = node;
      while (current != null)
      {
        if (roots.Contains(current)) return current;
        current = current.parent;
      }
      // Se non trova un punto di partenza, usa il primo disponibile
      return roots.Count > 0 ? roots[0] : node;
    }

    // Crea un'auto per ogni punto del percorso
    for (int i = 0; i < allNodes.Length; i++)
    {
      if (vehiclePrefabs != null && vehiclePrefabs.Length > 0)
      {
        var node = allNodes[i].transform;
        var prefab = vehiclePrefabs[Random.Range(0, vehiclePrefabs.Length)];
        // Sposta leggermente la posizione per evitare che le auto si sovrappongano
        var pos = node.position + Random.insideUnitSphere * 1f;
        var car = Instantiate(prefab, pos, Quaternion.identity);
        // Aggiunge il comportamento CarAI e lo inizializza con nodo di partenza, il root di riferimento e una velocità casuale nell'intervallo dato
        car.AddComponent<CarAI>().Setup(node, FindRoot(node), Random.Range(8f, 17f));
      }
    }

    // Rende gli ostacoli abbattibili quando colpiti con forza
    foreach (var obj in GameObject.FindGameObjectsWithTag("Ostacoli"))
      if (!obj.GetComponent<PoleRelay>()) obj.AddComponent<PoleRelay>();

    // Aggiunge il rotatore alle pale eoliche se manca
    foreach (var p in GameObject.FindGameObjectsWithTag("Pale eoliche"))
      if (!p.GetComponent<WindTurbineRotator>()) p.AddComponent<WindTurbineRotator>();
  }

  // Gestione del comportamento AI delle auto
  class CarAI : MonoBehaviour
  {
    Transform target, root; // target = nodo corrente, root = nodo di ritorno
    float baseSpeed, currentSpeed; // velocità nominale e attuale (per decelerazioni)

    public void Setup(Transform start, Transform parent, float speed)
    {
      // Inizializza il comportamento: nodo di partenza, root di riferimento e velocità
      target = start; root = parent; baseSpeed = currentSpeed = speed;
    }

    void Update()
    {
      if (!target) return;

      // Calcola la direzione verso il prossimo punto
      var dir = target.position - transform.position;
      dir.y = 0; // Mantiene l'altezza costante
      float dist = dir.magnitude; // Calcola la distanza dal punto di destinazione

      if (dist < 0.01f) return; // Già arrivati
      dir /= dist; // Normalizza il vettore: lo rende di lunghezza 1 mantenendo la direzione

      // Rallenta gradualmente quando si avvicina al punto
      currentSpeed = dist < 5f ? Mathf.Lerp(baseSpeed * .4f, baseSpeed, dist / 5f) : baseSpeed;

      // Muove l'auto in avanti e la gira verso la destinazione
      transform.position += dir * currentSpeed * Time.deltaTime;
      transform.forward = Vector3.Slerp(transform.forward, dir, Time.deltaTime * 3f);

      // Quando arriva abbastanza vicino al punto...
      if (dist < 1.5f)
      {
        // Cerca il prossimo punto del percorso (figlio con tag "pathNode")
        Transform next = null;
        for (int i = 0; i < target.childCount; i++)
          if (target.GetChild(i).CompareTag("pathNode")) { next = target.GetChild(i); break; }

        // Se c'è un prossimo punto vai lì, altrimenti ricomincia dall'inizio
        target = next ? next : root;

        // Quando ricomincia, cambia un po' la velocità per varietà
        if (!next) baseSpeed = Random.Range(8f, 12f);
      }
    }
  }
}

// Relay collisioni per ostacoli
public class PoleRelay : MonoBehaviour
{
  // Gestisce l'impatto sul palo: se la collisione è abbastanza forte, rimuove i vincoli del rigidbody e ferma eventuali turbine figlie
  void OnCollisionEnter(Collision c)
  {
    var rb = GetComponent<Rigidbody>() ?? GetComponentInParent<Rigidbody>();
    if (rb == null || c.relativeVelocity.magnitude <= 8f) return;
    rb.constraints = RigidbodyConstraints.None;
    foreach (var rot in rb.GetComponentsInChildren<WindTurbineRotator>()) rot.StopRotation();
  }
}

// Rotazione pale eoliche
public class WindTurbineRotator : MonoBehaviour
{
  bool rotating = true;
  void Update() { if (rotating) transform.Rotate(Vector3.forward * 50 * Time.deltaTime); }
  public void StopRotation() => rotating = false;
}
