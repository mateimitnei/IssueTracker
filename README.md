# Cerințe Proiect: Issue Tracker Web API

## Tehnologii și Arhitectură
* **Framework**: .NET Core folosind **Minimal API**.
* **Documentație API**: Integrare cu **Swagger** pentru apelarea ușoară a endpoint-urilor.
* **Organizare Cod**: Structurarea endpoint-urilor folosind clase statice.
* **Gestionarea Erorilor**: Implementarea unui sistem centralizat.

---

## Nomenclatoare
* **TicketStatus** (byte):
  * `1` ➔ TO DO
  * `2` ➔ IN PROGRESS
  * `3` ➔ IN REVIEW
  * `4` ➔ DONE
* **TicketPriority** (byte):
  * `1` ➔ LOW
  * `2` ➔ MEDIUM
  * `3` ➔ HIGH

---

## Endpoint-uri API

### 1. Adăugare Tichet (POST)
Endpoint pentru adăugarea unui issue în baza de date.
* **Entitatea `Ticket` (în BD)**: `Id` (int, identity), `TicketKey` (string), `Title` (string), `Description` (string), `CreatedAt` (DateTime), `StatusId` (byte), `PriorityId` (byte).
* **DTO (Data Transfer Object)**: Utilizatorul trimite strict `Title`, `Description` și `PriorityId`.
* **Validări și reguli de salvare**:
  * `Title`: Nu poate fi NULL/EMPTY și nu trebuie să depășească 100 de caractere.
  * `TicketKey`: Șir de caractere **unic** în sistem (generat din cod sau validat ca unic, ex: "TK-101").
  * `CreatedAt`: Se setează automat în momentul procesării (nu este cerut de la utilizator).
  * `StatusId`: Se setează automat la valoarea `1` (TO DO).
  * **Erori**: La o prioritate invalidă, returnează `400 Bad Request` folosind formatul standard `ProblemDetails`.

### 2. Editare Tichet (PATCH)
Endpoint pentru editarea unui tichet, identificat unic prin `TicketKey` primit ca parametru în calea URL-ului (path).
* **Regulă strictă**: Proprietățile `TicketKey` și `CreatedAt` **NU** pot fi editate.

### 3. Preluare Toate Tichetele (GET)
Endpoint pentru a aduce din baza de date lista completă a tichetelor salvate.

### 4. Preluare Tichet Specific (GET)
Endpoint pentru returnarea unui singur tichet, bazat pe `TicketKey` din path.
* **Erori**: Dacă tichetul nu este găsit, aplicația returnează `404 Not Found` (conform contractului REST).

### 5. Ștergere Tichet (DELETE)
Endpoint care șterge un tichet existent, folosind ca identificator `TicketKey` din path.

---

## Cerințe de Audit (Istoric)

* **Monitorizare**: Orice adăugare sau modificare de tichet necesită salvarea versiunii precedente într-o tabelă de log (ex: `TicketAudit`).
* **Date reținute**: Tabela va înregistra datele vechi (ex: `OldStatusId`, `OldTitle`, `ModificationDate`).
* **Endpoint dedicat (GET)**: Permite vizualizarea log-ului (istoricului) pentru un anumit `TicketKey` primit în path.

---

## Baza de Date

* **Sistem**: SQL Server.
* **Tabele necesare**: `TicketStatus`, `TicketPriority`, `Ticket`, `TicketAudit`.
* **Relații**: Tabelele trebuie legate prin *Foreign Keys* (chei externe) conform structurii.
* **Abordare (Database-First)**: Se încurajează scrierea scripturilor SQL manual, aplicând direct la nivelul bazei de date constrângeri precum `UNIQUE`, `NOT NULL` și `DEFAULT`.
