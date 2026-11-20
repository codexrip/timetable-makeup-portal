# Timetable & Makeup Class Portal

A role-based university semester portal built with **.NET 8 Blazor Web App (Interactive Server)** and **MongoDB (`MongoDB.EntityFrameworkCore`)**, allowing **Teachers** to submit conflict-checked makeup class requests for missed sessions and **Program Coordinators** to review schedules, assign suitable rooms, and track makeup completion.

---

## Tech Stack

- **Framework:** C# / .NET 8, Blazor Web App (`InteractiveServer`)
- **Database & ORM:** MongoDB via `MongoDB.EntityFrameworkCore` (LINQ)
- **Authentication:** Cookie authentication behind `IAuthService` (`HardcodedAuthService`) with `[Authorize(Roles = "...")]`
- **UI:** Bootstrap 5, Bootstrap Icons, custom CSS variables (`wwwroot/css/theme.css`, `wwwroot/css/layout.css`)

---

## Prerequisites

1. [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. [MongoDB Community Server](https://www.mongodb.com/try/download/community) running locally on `mongodb://localhost:27017` (or update `appsettings.json`)

---

## Configuration

MongoDB settings are located in `TimetableMakeup/appsettings.json`:

```json
"MongoDbSettings": {
  "ConnectionString": "mongodb://localhost:27017",
  "DatabaseName": "TimetableMakeupDb"
}
```

---

## Running the Application

From the repository root:

```bash
dotnet restore TimetableMakeup/TimetableMakeup.csproj
dotnet run --project TimetableMakeup/TimetableMakeup.csproj
```

Then open the URL printed in the terminal (by default `http://localhost:5000`).

> If upgrading from an older seed schema, drop the existing database first so startup seeding runs cleanly:
> ```bash
> sudo docker exec mongo mongosh TimetableMakeupDb --eval "db.dropDatabase()"
> ```

On first startup, if the MongoDB collections are empty, the app automatically seeds the **BSCS / BSIT (Shift-I)** dataset across **9 one-hour slots (08:00–17:00, with Slot 5 as lunch)**:
- **1 Department:** `CS-IT` (`Department of Computer Science & IT`)
- **1 Program:** `CS-IT` (`BSCS / BSIT (Shift-I)`)
- **9 Sections:** `BSCS-1A`, `BSCS-1B`, `BSCS-1C`, `BSCS-3A`, `BSCS-3B`, `BSCS-3C`, `BSCS-5A`, `BSCS-5B`, `BSIT-5B`
- **42 Teachers, 36 Courses, 27 Rooms, 173 Weekly Timetable Entries**
- **Logins:** 1 Coordinator + individual teacher logins (`firstname.lastname@demo.edu`) + `teacher@demo.edu` alias (mapped to Ms. Faryal Farooq)

---

## Demo Accounts

| Role | Email | Password | Landing Page |
| :--- | :--- | :--- | :--- |
| **Program Coordinator** | `coordinator@demo.edu` | `Coord@123` | `/coordinator/dashboard` |
| **Teacher (Ms. Faryal Farooq)** | `teacher@demo.edu` | `Teach@123` | `/teacher/dashboard` |
| **Individual Faculty Logins** | e.g. `sara.ibrahim@demo.edu` | `Teach@123` | `/teacher/dashboard` |

> **Note:** `Admin` and `HoD` are reserved in the extensible `Role` enum for future phases.

---

## Makeup Class Workflow

1. **Teacher Submits (3-Step Wizard):**
   - **Step A:** Select an assigned Course & Section and a Missed Date (Mon–Fri within the last 14 days). The system detects real scheduled sessions on that weekday and derives block duration (`DurationHours`, e.g., 1h lecture or 3h lab).
   - **Step B:** Pick a reason category and enter an explanation.
   - **Step C:** Pick **2 to 4 preferred start slots** from a two-week (this week & next week, Mon–Fri) grid of conflict-free blocks. Lunch Slot 5 (`12:00–13:00`) is never offered or crossed, and every offered block guarantees teacher, section, and at least one suitable room (`IsLab` match + `Capacity >= StudentCount`) are free for all hours of the block. Teachers never choose rooms.
2. **Coordinator Reviews:**
   - Filters requests across **Pending**, **Review Requested**, **Approved**, **Held/Not Held**, **Rejected**, and **All** (with sidebar badge counts).
   - **Approve & Schedule:** Picks one of the teacher's preferred slots, reviews/adjusts the prefilled suitable room, confirms via dialog, re-validates all block slots, and inserts one `TimetableEntry` per hour (`IsMakeup = true`, `MakeupDate`, `MakeupRequestId`).
   - **Request Review:** Returns the request to the teacher with a required note (`ReviewRequested`); the teacher edits reason/preferred slots (with missed class locked) and resubmits to `Pending`.
   - **Reject:** Rejects the request with a required note.
3. **Completion & Withdrawal:**
   - **Withdraw:** Teachers can withdraw `Pending` requests or `Approved` requests before `ApprovedDate` (removing reserved `TimetableEntry` rows).
   - **Held / Not Held:** On or after `ApprovedDate`, teachers mark the session as **Held** (optional note; timetable rows stay as history) or **Not Held** (required note; `TimetableEntry` rows are deleted so slots become free again).
   - **Overdue:** Any `Approved` request with `ApprovedDate < Today` automatically displays an orange **Overdue** badge without persisting a separate status.

---

## Demo Script

1. **Teacher Submits:**
   - Log in as `teacher@demo.edu` / `Teach@123` (Ms. Faryal Farooq).
   - Open **Makeup Requests** (`/teacher/makeup-requests`) -> **New Request**.
   - Select `Mobile Computing Lab (BSIT-5B)` and pick the most recent **Thursday** -> confirms `Slot 1-3 (08:00-11:00) (3 h)` is detected.
   - Select reason `Official duty`, enter `"Attending departmental workshop"`, pick 2–4 green start slots on the grid, and click **Submit Request**.
2. **Coordinator Reviews:**
   - Log in as `coordinator@demo.edu` / `Coord@123`.
   - Notice the **Pending** badge count on the sidebar and `/coordinator/dashboard`.
   - Open **Makeup Requests** (`/coordinator/makeup-requests`) -> **Review**.
   - Optionally test **Request Review** with a note (then log back in as the teacher to **Edit & Resubmit**).
   - Pick a preferred slot, inspect the prefilled Lab room dropdown, and click **Approve & Schedule** -> confirm in the modal.
   - Open **Teacher Timetables** (Ms. Faryal Farooq), **Section Timetables** (`BSIT-5B`), and **Room Timetables** for the week of `ApprovedDate` -> confirm 3 consecutive cells labeled `Makeup · <Date>`.
3. **Teacher Marks Held (or Not Held):**
   - On or after `ApprovedDate` (or after updating `ApprovedDate` in MongoDB for testing), log in as `teacher@demo.edu`.
   - On `/teacher/makeup-requests`, click **Mark as Held** (or **Not Held** with a note).
   - Log back in as Coordinator -> verify `Held (reported by teacher)` and the teacher's note on `/coordinator/makeup-requests`, plus updated **Teacher Workload** and **Makeups by Course** tables on `/coordinator/dashboard`.

---

## Security & Data Isolation

- Every page requires authentication and explicit role authorization (`[Authorize(Roles = "Coordinator")]` or `[Authorize(Roles = "Teacher")]`).
- `CurrentUserService` reads `Role`, `ProgramId`, and `TeacherId` strictly from signed-in cookie claims—never from URL parameters or form inputs.
- All database queries in `TimetableService`, `MakeupAvailabilityService`, and `MakeupService` filter strictly by the logged-in user's `ProgramId` or `TeacherId`.
