# Week 7 — Entity Model Expansion

**Topic:** Expanding the EF Core data model with related entities, foreign keys, and navigation properties.

---

## Lecture Connection

This code directly applies the four core concepts from the Week 7 lecture:

| Lecture Topic         | Where you'll see it in the code                                                                               |
| --------------------- | ------------------------------------------------------------------------------------------------------------- |
| **DbContext & DbSet** | `LendingContext` now manages 5 `DbSet<>` properties — one per table                                           |
| **LINQ**              | `LoanApplicationsController.GetAll()` chains `.Where()` and `.ToListAsync()` to filter and query              |
| **Relationships**     | `ApplicantId`/`LoanTypeId` foreign keys + navigation properties wire up one-to-many across entities           |
| **Eager Loading**     | `GetById()` uses `.Include(l => l.Applicant).Include(l => l.LoanType)` to pull related data in a single query |

---

## Overview

In Week 6 we had a single `LoanApplicationDto` entity with everything inline — applicant name, loan type as a string, a flat notes field. This week we normalize the model by extracting **four new entities** that introduce one-to-many relationships, lookup tables, and navigation properties.

### Before (Week 6)

```text
LoanApplicationDto
├── Id
├── ApplicantName (string)
├── LoanType (string)        ← flat string, no referential integrity
├── LoanAmount
├── AnnualIncome
├── Status
├── RiskRating
├── SubmittedDate
└── Notes (string)            ← single field, no history
```

### After (Week 7)

```text
Applicant  ──1:N──►  LoanApplicationDto  ◄──1:N──  LoanType (lookup)
                          │
                          ├──1:N──►  LoanPayment
                          └──1:N──►  LoanNote
```

---

## Links Week 7 Learning Objectives

---

## New Entities

### 1. `Applicant` — `Models/Applicant.cs`

Extracts the person filing the loan into its own entity. One applicant can have many loan applications.

| Property      | Type       | Notes                         |
| ------------- | ---------- | ----------------------------- |
| `Id`          | `int`      | Primary key                   |
| `Name`        | `string`   | Required, max 100 chars       |
| `Email`       | `string`   | Required, max 150 chars       |
| `Phone`       | `string`   | Max 20 chars                  |
| `CreatedDate` | `DateTime` | Defaults to `DateTime.UtcNow` |

**Navigation:** `List<LoanApplicationDto> LoanApplications`

**Key concept:** One-to-many relationship — one applicant files many applications.

---

### 2. `LoanType` — `Models/LoanType.cs`

Replaces the raw `string LoanType` with a proper lookup/reference entity.

| Property        | Type     | Notes                   |
| --------------- | -------- | ----------------------- |
| `Id`            | `int`    | Primary key             |
| `Name`          | `string` | e.g. "Mortgage", "Auto" |
| `Description`   | `string` | Longer description      |
| `MaxTermMonths` | `int`    | Max loan term in months |

**Navigation:** `List<LoanApplicationDto> LoanApplications`

**Key concept:** Lookup table — eliminates free-text inconsistency ("auto" vs "Auto" vs "AUTO").

**Seed data:**

| Id  | Name     | MaxTermMonths |
| --- | -------- | ------------- |
| 1   | Mortgage | 360           |
| 2   | Auto     | 84            |
| 3   | Personal | 60            |
| 4   | Business | 120           |

---

### 3. `LoanPayment` — `Models/LoanPayment.cs`

Tracks individual payments made against an approved loan.

| Property            | Type       | Notes                     |
| ------------------- | ---------- | ------------------------- |
| `Id`                | `int`      | Primary key               |
| `Amount`            | `decimal`  | Payment amount            |
| `PaymentDate`       | `DateTime` | When the payment was made |
| `Method`            | `string`   | "ACH", "Check", "Wire"    |
| `LoanApplicationId` | `int`      | Foreign key               |

**Navigation:** `LoanApplicationDto? LoanApplication`

**Key concept:** Foreign key — each payment belongs to exactly one loan application.

---

### 4. `LoanNote` — `Models/LoanNote.cs`

Replaces the single `Notes` string with a collection of timestamped, authored comments.

| Property            | Type       | Notes                   |
| ------------------- | ---------- | ----------------------- |
| `Id`                | `int`      | Primary key             |
| `Author`            | `string`   | Who wrote the note      |
| `Text`              | `string`   | Note content (max 1000) |
| `CreatedDate`       | `DateTime` | Timestamp               |
| `LoanApplicationId` | `int`      | Foreign key             |

**Navigation:** `LoanApplicationDto? LoanApplication`

**Key concept:** Audit trail — preserves history instead of overwriting a single field.

---

## Changes to Existing Files

### `Models/LoanApplicationDto.cs`

- **Removed:** `string LoanType` property
- **Added:** `ApplicantId` (int FK) + `Applicant?` navigation
- **Added:** `LoanTypeId` (int FK) + `LoanType?` navigation
- **Added:** `List<LoanPayment> Payments` collection
- **Added:** `List<LoanNote> LoanNotes` collection

### `Data/LendingContext.cs`

- **Added DbSets:** `Applicants`, `LoanTypes`, `LoanPayments`, `LoanNotes`
- **Updated seed data:** All 8 loan applications now reference `ApplicantId` and `LoanTypeId`
- **New seed data:** 8 applicants, 4 loan types, 3 sample payments, 4 sample notes

### `Controllers/LoanApplicationsController.cs`

- `GetAll` filter changed from `string? loanType` → `int? loanTypeId`
- Added `Include()` calls for eager loading of `Applicant`, `LoanType`, `Payments`, and `LoanNotes`
- `Update` method now copies `ApplicantId` and `LoanTypeId`

### `Program.cs`

- Added `ReferenceHandler.IgnoreCycles` JSON option to prevent circular reference errors from bidirectional navigation properties

---

## New Controllers

| Controller             | Route                                | Methods   |
| ---------------------- | ------------------------------------ | --------- |
| `ApplicantsController` | `api/applicants`                     | GET, POST |
| `LoanTypesController`  | `api/loantypes`                      | GET       |
| `PaymentsController`   | `api/loanapplications/{id}/payments` | GET, POST |
| `NotesController`      | `api/loanapplications/{id}/notes`    | GET, POST |

**Key concept:** Nested routes — payments and notes are scoped under their parent loan application.

---

## EF Core Concepts Demonstrated

| Concept                   | Where it appears                                                  |
| ------------------------- | ----------------------------------------------------------------- |
| Foreign keys              | `ApplicantId`, `LoanTypeId`, `LoanApplicationId`                  |
| Navigation properties     | `Applicant.LoanApplications`, `LoanApplicationDto.Payments`, etc. |
| One-to-many relationship  | Applicant → Applications, Application → Payments                  |
| Lookup / reference table  | `LoanType` entity                                                 |
| Eager loading (`Include`) | `LoanApplicationsController.GetById()`                            |
| Seed data (`HasData`)     | `LendingContext.OnModelCreating()`                                |
| Cycle handling            | `ReferenceHandler.IgnoreCycles` in `Program.cs`                   |

---

## Try It

```bash
cd buckeye-lending/backend/Buckeye.Lending.Api
dotnet run
```

Then explore the API using **Swagger UI** and the **`.http` test files**:

### Option 1: Swagger UI

Open [http://localhost:5000/swagger](http://localhost:5000/swagger) in your browser. Expand each endpoint group and use **"Try it out"** to send requests interactively.

### Option 2: `.http` Test Files (VS Code REST Client)

Open any of the `.http` files in VS Code and click **"Send Request"** above each request:

| File                          | What it tests                             |
| ----------------------------- | ----------------------------------------- |
| `LoanApplications_Tests.http` | CRUD for loan applications with FK fields |
| `Applicants_Tests.http`       | GET all/by-ID, POST new applicant         |
| `LoanTypes_Tests.http`        | GET all loan types, GET by ID             |
| `Payments_Tests.http`         | GET/POST payments nested under a loan app |
| `Notes_Tests.http`            | GET/POST notes nested under a loan app    |

> **Tip:** Try the error cases too (ID 99, empty name, negative amount) — observe the Problem Details (RFC 7807) responses.
