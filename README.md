# README - Fuel Chase

**Esame di Ambienti Virtuali a.a. 2024/2025**

## Progettazione videogioco

Il progetto è stato realizzato per il corso di Ambienti virtuali tenuto dal professor Marcello Antonio Carrozzino, nell'anno accademico 2024-2025, all'interno del CdL di Informatica Umanistica magistrale dell'Università di Pisa. Si tratta di un videogioco in Realtà Virtuale intitolato "**Fuel Chase**", sviluppato con Unity e C#. L'esperienza è pensata per essere eseguita in modalità desktop tradizionale (senza VR).

Per poterlo visionare, occorre installare [Unity Hub](https://unity.com/download) e aprire il progetto tramite l'apposita funzione "Apri progetto esistente", selezionando la cartella radice del progetto.

<video controls width="720">
<source src="Gameplay.mp4" type="video/mp4">
 Il tuo browser non supporta il tag video. Puoi scaricare il file qui: [Scarica gameplay.mp4](gameplay.mp4)
</video>

---

Il progetto è stato sviluppato tenendo in considerazione i principali obiettivi della Realtà Virtuale:

- Presenza: garantire al giocatore la sensazione di essere all'interno dell'ambiente di gioco.
- Immersività: combinare audio, grafica e dinamiche di gioco per favorire l'immersione.
- Interazione: fornire feedback immediati e coerenti alle azioni dell'utente tramite meccaniche e interfacce chiare.

L'interazione implementata è di tipo mediato: l'utente esplora la scena in prima persona e passa alla guida controllando direttamente i veicoli. La visualizzazione e l'aggiornamento della scena sono gestiti in tempo reale dalla pipeline di rendering di Unity, che garantisce coerenza visiva e aggiornamento continuo durante l'esecuzione.

## Introduzione Narrativa

Un improvviso blackout ha prosciugato tutte le scorte di carburante della città. Le strade sono nel caos e ogni automobilista è alla disperata ricerca delle ultime taniche rimaste. Macchine sbandano, semafori non funzionano e il traffico è diventato una vera giungla urbana.

In "Fuel Chase", il giocatore è l'ultima speranza per ristabilire l'ordine. L'avventura inizia nel garage di casa: per poter avviare il veicolo è necessario trovare tre oggetti — una chiave, una tanica di benzina e una batteria — dopodiché si potrà scegliere la macchina e lanciarsi in una corsa contro il tempo per recuperare altre dieci **taniche di benzina** sparse per la città. Solo così sarà possibile riattivare i generatori d'emergenza e salvare la città dalla paralisi totale.

## Analisi Cronologica e Tecnica del Progetto

Il progetto è strutturato alternando una fase esplorativa in prima persona a una fase di guida arcade. Di seguito vengono descritti i componenti software principali nell'ordine in cui intervengono durante il gioco, evidenziando le scelte tecniche adottate. I prefab di scena sono stati importati da Skecthfab e da Unity Asset Store e modificati in Unity per adattarli alle esigenze del gameplay.

### 1. L'Ingresso in Scena e gestione della sessione (`CameraIntro.cs` e `Menu.cs`)

L'esperienza si apre con una sequenza cinematica realizzata tramite il pannello Unity "Animation", strutturata in due momenti distinti: per prima cosa viene mostrato il "**logo**" del gioco; dopo un breve intervallo il logo svanisce e appare un overlay nero semitrasparente sul quale viene presentata la **trama**. Il testo della trama viene animato con fade-in per la comparsa, rimane leggibile per alcuni secondi e poi viene nascosto con un fade-out, restituendo così al giocatore il controllo visivo.

Dopodiché un **Animation Event** posto sull'ultimo frame, invoca il metodo `OnIntroFinished()` dello script `CameraIntro.cs`, che disattiva la camera d'intro e abilita il controller del giocatore, garantendo una transizione precisa e senza scatti.

Il controllo del flusso di gioco è centralizzato in `Menu.cs`: premendo `ESC` il gioco viene messo in pausa e viene mostrata un'interfaccia minimale per riprendere, ricominciare o uscire; i pulsanti (`Riprendi`, `Ricomincia`, `Esci`) sono collegati ai metodi pubblici tramite gli eventi `OnClick` dei `Button` nell'Inspector.

### 2. Esplorazione a Piedi (`PlayerController.cs` e `Interactions.cs`)

Una volta preso il controllo, il gioco passa all'esplorazione a piedi: il personaggio si muove con il `CharacterController`, unito alla classica combinazione WASD per lo spostamento orizzontale e verticale e il mouse per la rotazione della visuale. Inoltre mantiene un inventario minimale accessibile via `PlayerController.Instance`, dove ogni oggetto interattivo — una **chiave**, una **tanica di benzina** e una **batteria** — ha uno `SphereCollider` che permette di mostrare il prompt "Premi E per raccogliere {oggetto}" quando il giocatore si avvicina. Premendo `E` parte il suono collegato, il conteggio dell'inventario viene aggiornato e l'oggetto viene rimosso al termine della riproduzione, assicurando un feedback coerente.
La porta del garage è trattata come un tipo di interazione separato: alla pressione di `E` si avvia un suono di apertura porta e essa si solleva, restando aperta mentre il giocatore è vicino e richiudendosi automaticamente dopo un breve intervallo se il giocatore si allontana al fine di evitare collisioni indesiderate.

### 3. La Scelta del Veicolo (`SceltaAuto.cs`)

Una volta raccolti tutti e tre gli oggetti necessari nel garage, il giocatore può procedere alla selezione del veicolo. Lo script `SceltaAuto.cs` gestisce questa transizione: premendo `1`, `2` o `3` viene istanziata una preview del veicolo allo `spawnPoint` e l'interfaccia mostra un messaggio che indica la velocità massima della macchina e il tempo a disposizione per terminare la missione qualora si opti per quella vettura. Per evitare problemi fisici durante la selezione, queste anteprime sono mantenute in stato cinematico (`isKinematic = true`); selezionando un'altra vettura la preview precedente viene distrutta.

Alla conferma con `E` la preview diventa pienamente attiva: il `Rigidbody` esce dallo stato cinematico, i componenti MonoBehaviour `CarController` e `Benzina` vengono abilitati e i collider riattivati; il GameObject del personaggio a piedi viene disattivato, la camera del player viene spenta e viene attivata la camera integrata nella prefab della macchina. Contestualmente viene aperta la porta del garage tramite `Interactions.SetDoorOpen(true)` e all'oggetto macchina viene assegnato il tag `Car`.
Lo script monitora poi l'uscita dal garage: quando il veicolo si allontana di una certa distanza dal punto di spawn viene abilitata la camera di guida *in stile GTA* ed è invocata la chiusura automatica della porta.

### 4. La Guida e la Fisica (`CarController.cs`)

Comandi principali in auto: `W`/`S` avanti/indietro, `A`/`D` sterzo, `Space` freno a mano (drift), `R` riassesta la macchina, `C` visuale retro, `V` cambia inquadratura, `TAB` mostra i controlli.

La fisica della vettura è gestita da `CarController.cs` tramite `WheelCollider`. I parametri principali sono `maxSpeed`, `accelerationMultiplier` e `brakeForce`, che regolano rispettivamente la velocità massima, l'accelerazione e la forza frenante. La velocità attuale viene calcolata in km/h convertendo la velocità lineare del `Rigidbody` e viene limitata a `maxSpeed` per evitare accelerazioni eccessive.
Premendo `Space` la variabile `driftingAxis` aumenta progressivamente fino a 1; questo valore viene usato insieme a `handbrakeDriftMultiplier` per scalare gli `extremumSlip` originali di ciascuna ruota, diminuendo quindi l'aderenza. Nel codice l'effetto è applicato a tutte e quattro le ruote, con conseguente perdita di grip e maggiore tendenza allo scivolamento laterale. Al rilascio del freno a mano, `RecoverTraction()` avvia un ritorno graduale ai parametri originali (finalizzato da `ResetFrizioneRuote()`), ripristinando progressivamente la stabilità del veicolo.

I suoni (motore, stridio) e gli effetti visivi (fumo, slittamenti) sono sincronizzati con gli stati del veicolo: ad esempio il suono di stridio parte quando lo script rileva scivolamento laterale o quando il freno a mano è attivo ad alta velocità. L'audio (configurato con `AudioSource` nei prefab), le particelle e i trail sono separati dalla logica di gioco e si attivano in risposta a eventi di stato.
Nota tecnica: la scia di sgommata è realizzata tramite il componente `TrailRenderer` assegnato alle ruote posteriori (variabili `RLWTireSkid` e `RRWTireSkid` in `CarController.cs`). Lo script imposta `emitting = true` per far comparire la scia quando la vettura perde aderenza e `emitting = false` per farla cessare.

Premendo il tasto `R`, la vettura viene riassestata: lo script calcola una posizione leggermente sopra il terreno e riallinea l'orientamento della macchina in modo che sia parallela al suolo, evitando così situazioni di ribaltamento o incastro. Se la vettura cade in acqua, viene automaticamente riassestata al punto di spawn precedente, quando tutte e quattro le ruote erano a contatto con il terreno.

### 5. Il Mondo Urbano (`TrafficManager.cs`)

Per rendere la città viva, `TrafficManager.cs` popola le strade con veicoli guidati dalla "AI". Questi veicoli seguono percorsi predefiniti di nodi (`pathNode`): ogni veicolo sceglie casualmente un percorso all'istanziazione e si muove verso il nodo figlio successivo in modo ciclico calcolandone la direzione. La velocità di ogni veicolo è variabile entro un intervallo definito per evitare uniformità eccessiva. La densità del traffico è regolabile tramite il parametro `trafficDensity`, che determina il numero di veicoli istanziati all'inizio della scena.

### 6. La Meccanica di Gioco (`Benzina.cs`)

Il cuore della sfida è gestito da `Benzina.cs`. Lo script si occupa di scegliere casualmente i nodi di spawn (oggetti in scena taggati `FuelTank`, posizionati sulle carreggiate), istanziare le taniche e animarle con una rotazione e una lieve oscillazione verticale per aumentarne la visibilità. La "missione" inizia con un timer (di default 5 minuti) e un obiettivo numerico (di default 10 taniche): quando il veicolo si avvicina entro un raggio stabilito a una tanica, questa viene raccolta e parte un suono automaticamente. Il contatore cresce e viene aggiunto un bonus tempo (es. +10s) che viene mostrato temporaneamente in UI con un breve fade. Il timer inoltre cambia colore in base alla soglia (giallo/rosso) per fornire un feedback chiaro.

Lo script implementa inoltre la logica di termine partita: se si raggiungono tutte le taniche si mostra la schermata di vittoria, altrimenti lo scadere del tempo mostra la schermata di Game Over; in entrambi i casi l'audio di scena viene fermato, i controlli vengono disabilitati e il gioco viene messo in pausa dopo un piccolo ritardo per permettere l'aggiornamento dell'interfaccia.
