
# COPILOT_INSTRUCTIONS.md

Vehicle A/C Inventory Management System
Copilot Build Instructions

---

# 1. Role of Copilot

You are building a **production-grade, offline-first desktop inventory management system** for a vehicle A/C repair shop.

This is NOT a prototype.
This is NOT a demo app.
This is NOT a cloud-first system.

This is a **real business-critical system with zero tolerance for data loss.**

You must prioritize:

1. Data integrity
2. Stability
3. Performance
4. Simplicity of UI
5. Offline functionality

---

# 2. System Summary

The application is a:

* Windows desktop app
* Barcode-driven inventory system
* PostgreSQL database stored on external SSD
* Used by shop staff daily
* Handling 10,000+ items
* Supporting instant stock operations

Owner will access remotely via a read-only dashboard.

---

# 3. Technology Stack

## Desktop Application

* C# .NET
* WPF
* MVVM architecture

## Database

* PostgreSQL
* WAL enabled
* Hosted on external SSD

## Remote Dashboard

* ASP.NET minimal web app
* Read-only endpoints

## Networking

* LAN-based DB connection
* Cloudflare Tunnel for remote owner access

---

# 4. Absolute Constraints (Do NOT violate)

## Data

* Database MUST run from external SSD
* Zero data loss tolerance
* Every stock movement must be logged

## UX

* UI must be extremely simple
* Designed for non-technical users
* No complex dashboards

## Workflow

* Barcode scanning is primary interaction
* Manual stock deduction must NOT exist

## Architecture

* Offline-first
* Local-first
* Cloud optional later

---

# 5. Domain Model

Inventory item uniqueness is defined by:

* Part type
* Vehicle model
* Part brand
* Country of origin

Each unique combination = ONE barcode.

---

# 6. Database Design Rules

## Must be normalized

Tables:

* PartType
* VehicleManufacturer
* VehicleModel
* PartBrand
* Rack
* Item
* Stock
* StockTransaction

## Critical rule

Stock quantity must NEVER be stored in Item table.

Stock must be tracked separately.

---

# 7. Barcode System

## Barcode format

CODE-128

## Encodes

Internal Item ID only.

Example:

ITM-00001234

## Rules

* Every item must have unique barcode
* Barcode used for:

  * add stock
  * remove stock
  * search

---

# 8. Core Functional Modules

## Item Management

* create item
* upload image
* assign rack
* define low stock threshold
* generate barcode

## Stock Add

* scan barcode
* enter quantity
* save

## Stock Remove

* scan barcode
* confirm removal

## Search

* barcode
* model
* type
* brand

## Reporting

* end of day stock
* low stock
* movement history

---

# 9. Transaction Logging (MANDATORY)

Every stock change must create a record in:

StockTransaction table.

Fields:

* item_id
* action_type
* quantity
* timestamp
* machine_name
* checksum_hash

This enables:

* audit
* recovery
* traceability

---

# 10. Performance Requirements

* Barcode lookup must be instant
* Use indexing
* Cache frequently used lookups
* Avoid heavy joins in runtime operations

---

# 11. Data Safety Architecture

## Required protections

* PostgreSQL WAL journaling
* incremental backup every few minutes
* nightly snapshot export
* integrity checks on startup

## Dual logging

Every transaction must also be written to:

local PC mirror log

---

# 12. External SSD Handling

## Must assume:

* disconnect possible
* write interruption possible

System must:

* detect DB availability
* pause operations if unsafe
* never allow write during unstable state

---

# 13. UI Design Instructions

## Main screen

Only show:

* Add stock
* Remove stock
* Search item
* Reports

No complex navigation.

---

## Add stock screen

Flow:

scan → item loads → enter quantity → save

---

## Remove stock

scan → confirm → done

---

## Search screen

Show:

* image
* quantity
* rack
* compatibility

---

# 14. Error Prevention Rules

System must prevent:

* negative stock
* duplicate barcode
* duplicate item definitions
* manual stock edits without barcode

---

# 15. Image Handling

* Store image paths only
* Do NOT store binary images in database
* Use relative paths

---

# 16. Low Stock Logic

Each item has its own threshold.

Alert when:

quantity <= threshold

---

# 17. Remote Dashboard

Owner view only.

Features:

* stock lookup
* low stock
* item search

No editing endpoints allowed.

---

# 18. Security

Single login system.

System must log:

* timestamp
* machine name
* action

---

# 19. Development Order

Follow this sequence strictly.

## Phase 1

Database schema + connectivity

## Phase 2

Item creation + barcode generation

## Phase 3

Stock add/remove

## Phase 4

Transaction logging

## Phase 5

Reporting

## Phase 6

Backup & integrity layer

## Phase 7

Remote dashboard

---

# 20. What Copilot MUST NOT do

* Do NOT introduce cloud dependencies
* Do NOT use SQLite
* Do NOT use MS Access
* Do NOT store stock in Item table
* Do NOT design complex UI
* Do NOT ignore transaction logging
* Do NOT use temporary in-memory stock logic

---

# 21. Future Cloud Migration Notes

Architecture must remain compatible with:

* API layer addition
* Cloud PostgreSQL
* Web UI replacement

Avoid hard-coding local paths.

---

# 22. Definition of Done

System is complete when:

* item creation works
* barcode scanning updates stock
* stock never goes negative
* transaction log exists for every change
* backups run automatically
* remote owner dashboard works
* SSD failure recovery possible

---

# 23. Development Philosophy

This system prioritizes:

Reliability > Features
Stability > Speed
Clarity > Complexity

The user is a repair shop worker, not a software operator.

Every interaction must be:

Fast
Clear
Foolproof

---

# END OF COPILOT INSTRUCTIONS
