# Issue Tracker Web API — Specificație Proiect

## 1. Tehnologii și Arhitectură

- **Framework:** .NET Core folosind Minimal API.
- **Documentație API:** Integrare cu Swagger pentru apelarea ușoară a endpoint-urilor.
- **Organizare Cod:** Structurarea endpoint-urilor folosind clase statice.
- **Gestionarea Erorilor:** Implementarea unui sistem centralizat.

## 2. Nomenclatoare

### TicketStatus (byte)

| Valoare | Semnificație |
|---------|--------------|
| `1`     | TO DO        |
| `2`     | IN PROGRESS  |
| `3`     | IN REVIEW    |
| `4`     | DONE         |

### TicketPriority (byte)

| Valoare | Semnificație |
|---------|--------------|
| `1`     | LOW          |
| `2`     | MEDIUM       |
| `3`     | HIGH         |

## 3. Endpoint-uri API

### 3.1. Adăugare Tichet — `POST`

Endpoint pentru adăugarea unui issue în baza de date.

- **Entitatea `Ticket` (în BD):** `Id` (int, identity), `TicketKey` (string), `Title` (string), `Description` (string), `CreatedAt` (DateTime), `StatusId` (byte), `PriorityId` (byte).
- **DTO (Data Transfer Object):** Utilizatorul trimite strict `Title`, `Description` și `PriorityId`.
- **Validări și reguli de salvare:**
  - `Title`: nu poate fi `NULL`/`EMPTY` și nu trebuie să depășească 100 de caractere.
  - `TicketKey`: șir de caractere unic în sistem (generat din cod sau validat ca unic, ex: `"TK-101"`).
  - `CreatedAt`: se setează automat în momentul procesării (nu este cerut de la utilizator).
  - `StatusId`: se setează automat la valoarea `1` (TO DO).
- **Erori:** la o prioritate invalidă, se returnează `400 Bad Request` folosind formatul standard `ProblemDetails`.

### 3.2. Editare Tichet — `PATCH`

Endpoint pentru editarea unui tichet, identificat unic prin `TicketKey` primit ca parametru în calea URL-ului (path).

- **Regulă strictă:** proprietățile `TicketKey` și `CreatedAt` NU pot fi editate.

### 3.3. Preluare Toate Tichetele — `GET`

Endpoint pentru a aduce din baza de date lista completă a tichetelor salvate.

### 3.4. Preluare Tichet Specific — `GET`

Endpoint pentru returnarea unui singur tichet, bazat pe `TicketKey` din path.

- **Erori:** dacă tichetul nu este găsit, aplicația returnează `404 Not Found` (conform contractului REST).

### 3.5. Ștergere Tichet — `DELETE`

Endpoint care șterge un tichet existent, folosind ca identificator `TicketKey` din path.

## 4. Cerințe de Audit (Istoric)

- **Monitorizare:** orice adăugare sau modificare de tichet necesită salvarea versiunii precedente într-o tabelă de log (ex: `TicketAudit`).
- **Date reținute:** tabela va înregistra datele vechi (ex: `OldStatusId`, `OldTitle`, `ModificationDate`).
- **Endpoint dedicat (`GET`):** permite vizualizarea log-ului (istoricului) pentru un anumit `TicketKey` primit în path.

## 5. Bază de Date

- **Sistem:** SQL Server.
- **Tabele necesare:** `TicketStatus`, `TicketPriority`, `Ticket`, `TicketAudit`.
- **Relații:** tabelele trebuie legate prin Foreign Keys (chei externe) conform structurii.
- **Abordare (Database-First):** se încurajează scrierea scripturilor SQL manual, aplicând direct la nivelul bazei de date constrângeri precum `UNIQUE`, `NOT NULL` și `DEFAULT`.

---

# Cerință Suplimentară — Ticket Management API (Flux de Lucru pe Echipă)

## Context și Obiectiv

Cerința inițială (Minimal API cu Route Groups, Global Error Handling și Swagger, entitățile `Ticket`, `TicketStatus`, `TicketPriority`, `TicketAudit`) rămâne neschimbată ca bază funcțională. Acest document o împarte în **două fluxuri de lucru paralele**, astfel încât fiecare intern să poată lucra independent.

Pe lângă cerințele funcționale, fiecare flux conține cerințe explicite de SQL — proceduri stocate, join-uri, view-uri, tranzacții și indexuri.

## Arhitectura Comună (se stabilește împreună înainte de împărțirea task-urilor)

Înainte de a separa task-urile, echipa trebuie să cadă de acord și să livreze împreună, ca fundație:

1. Structura soluției .NET, cu proiect Web API și foldere separate pentru `endpoints`, `dtos`, `data` și `middleware`.
2. Scriptul SQL inițial de creare a bazei de date, care conține tabelele de nomenclator `TicketStatus` și `TicketPriority`, populate cu valorile fixe din cerință.
3. Convenția de conectare la baza de date — fie prin `Microsoft.Data.SqlClient`, fie prin EF Core Database First — aleasă o singură dată pentru tot proiectul.
4. Middleware-ul de Global Error Handling, un singur handler folosit de amândoi, care mapează excepțiile la `ProblemDetails` cu codurile `400`, `404`, `409` și `500`.
5. Configurarea Swagger, comună pentru toate endpoint-urile, indiferent cine le scrie.

## Persoana A — Ticket Lifecycle (Create, Update, Delete)

A este responsabil de tot ce înseamnă **scriere** în baza de date.

### Endpoint-uri A

- **Endpoint 1 — `POST tickets`:** creare tichet dintr-un DTO care conține `Title`, `Description` și `PriorityId`.
  - Se validează `Title` (nu null/gol, max 100 caractere).
  - Se generează un `TicketKey` unic.
  - `StatusId` se setează automat la `1`, iar `CreatedAt` la data și ora curentă.
  - `PriorityId` invalid → `400 Bad Request` (ProblemDetails).
- **Endpoint 2 — `PATCH tickets/{TicketKey}`:** editare parțială a tichetului. `TicketKey` și `CreatedAt` nu sunt editabile — dacă vin în body, fie se ignoră, fie se returnează `400`.
- **Endpoint 5 — `DELETE tickets/{TicketKey}`:** ștergerea unui tichet existent. Dacă nu există → `404`.

### Cerințe SQL — A

1. **`sp_CreateTicket`** — parametri: `Title`, `Description`, `PriorityId`.
   - Generează `TicketKey` unic direct în interiorul procedurii (ex: secvență SQL sau logică bazată pe numărul maxim existent — alegerea se discută și se argumentează).
   - Validează `PriorityId` printr-un `EXISTS` pe tabela `TicketPriority`; dacă prioritatea nu există, aruncă o eroare custom pe care API-ul o traduce în `400`.
   - Inserează tichetul cu `StatusId = 1` și `CreatedAt` automat.
   - Folosește o **tranzacție explicită** (`BEGIN TRAN` / `COMMIT`), deoarece inserarea tichetului și eventuala primă înregistrare de audit trebuie să fie atomice.

2. **`sp_UpdateTicket`** — parametri: `TicketKey` și, opțional, `Title`, `Description`, `PriorityId`, `StatusId` (update parțial).
   - Folosește blocuri `TRY/CATCH` cu `THROW`/`RAISERROR` pentru tichet inexistent sau valori invalide de status/prioritate.
   - Înainte de update, citește rândul vechi și îl inserează în `TicketAudit`.
   - Update-ul și inserarea în audit se fac într-o **tranzacție explicită**.
   - *Nivel avansat:* se poate folosi clauza `OUTPUT` direct în `UPDATE` pentru a popula audit-ul fără un select separat.

3. **`sp_DeleteTicket`** — șterge tichetul pe baza `TicketKey`.
   - Dacă se alege ștergerea fizică, ultima versiune a tichetului trebuie salvată în `TicketAudit` **înainte** de `DELETE`, altfel se pierde istoricul.

### Constrângeri la nivel de bază de date (independente de codul C#)

- `UNIQUE` pe coloana `TicketKey` din tabela `Ticket`.
- `NOT NULL` pe `Title`, `StatusId`, `PriorityId`, `CreatedAt`.
- Valoare implicită (`DEFAULT`) pentru `CreatedAt` = data curentă.
- Valoare implicită `1` pentru `StatusId`.
- Chei străine de la `Ticket` către `TicketStatus` și `TicketPriority`.
- *(opțional)* `CHECK` pentru lungimea maximă a titlului.

## Persoana B — Read, Audit și Reporting

B este responsabil de tot ce înseamnă **citire, agregare și istoric**.

### Endpoint-uri B

- **Endpoint 3 — `GET tickets`:** aduce toate tichetele, cu statusul și prioritatea afișate ca text (nu doar valori numerice brute).
- **Endpoint 4 — `GET tickets/{TicketKey}`:** aduce un singur tichet. Dacă nu există → `404`.
- **Endpoint 7 — `GET tickets/{TicketKey}/audit`:** aduce istoricul complet de modificări pentru un tichet, ordonat cronologic.

### Cerințe SQL — B

1. **`vw_TicketDetails`** (view) — selectează datele tichetului și le îmbină, printr-un `INNER JOIN`, cu numele statusului din `TicketStatus` și numele priorității din `TicketPriority`. Folosit de endpoint-urile 3 și 4; motivul principal e evitarea expunerii id-urilor brute către client fără traducere în text.

2. **`sp_GetTicketByKey`** — parametru: `TicketKey`; selectează din view-ul de mai sus. Dacă nu găsește nimic, decizia de a returna `404` se ia în codul C#, nu în procedură.

3. **`sp_GetTicketAuditLog`** — parametru: `TicketKey`; face `JOIN` între `TicketAudit` și `Ticket`, plus `LEFT JOIN` către `TicketStatus` și `TicketPriority` pentru a traduce valorile vechi salvate în audit. Exercițiu bun de `LEFT JOIN` cu alias-uri multiple pe același tabel, deoarece atât statusul vechi cât și prioritatea veche trebuie traduse separat.

4. **Indexare** (trebuie justificată, nu doar scrisă):
   - Index `UNIQUE` pe `TicketKey` din `Ticket` — susține atât unicitatea, cât și căutările din endpoint-urile 4 și 7.
   - Index pe coloana care leagă `TicketAudit` de `Ticket`, deoarece `sp_GetTicketAuditLog` filtrează după această coloană.
   - *Întrebare de evaluare orală:* de ce nu se pune index și pe `Title` sau `Description`?

5. *(opțional, bonus)* Interogare de agregare cu `GROUP BY` și `COUNT` — numărul de tichete pe fiecare status și pe fiecare prioritate. Nu este cerută explicit în specificația inițială, dar e un test bun pentru `GROUP BY`, `HAVING` și `JOIN`.

## Tabela de Audit — Design Comun (se stabilește împreună)

`TicketAudit` trebuie să conțină:

- identificator propriu;
- referință către tichetul original;
- câmpurile vechi relevante (titlu, descriere, status vechi, prioritate veche);
- data modificării;
- tipul modificării (`update` sau `delete`).

A scrie în această tabelă, din procedurile de update și delete; B citește din ea, prin procedura de audit log. Acesta este **punctul de integrare** dintre cei doi, iar structura exactă a tabelei trebuie stabilită împreună, înainte de a începe implementarea separată.

## Etape Comune și Integrare Finală

- Fiecare scrie propriile endpoint-uri ca **route groups** separate (ex: unul pentru scriere, altul pentru citire), dar înregistrate în același fișier `Program`.
- Se recomandă **testare încrucișată**: A testează manual endpoint-urile lui B și invers, folosind Swagger și debugger.
- La final, fiecare susține o scurtă prezentare (~15-20 min) în care explică deciziile de design SQL luate, în special modul de generare a `TicketKey` și strategia de audit.

### Pași de urmat

1. Se stabilește fundația comună: structura proiectului, tabelele de nomenclator, middleware-ul de erori și configurarea Swagger.
2. Fiecare scrie scripturile SQL proprii (tabele și proceduri) și le testează direct în SSMS sau Azure Data Studio, independent de API.
3. Se implementează endpoint-urile în C#, care apelează procedurile stocate.
4. Integrare — fluxul de audit trebuie să funcționeze de la un capăt la altul (A scrie, B citește).
5. Code review încrucișat, însoțit de teste manuale din Swagger.
6. Prezentarea finală.