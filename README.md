Platforma za upravljanje stručnim događajima i prijavama učesnika

  Opis projekta

Ova aplikacija predstavlja web platformu razvijenu korišćenjem ASP.NET 8.0 tehnologije, namenjenu upravljanju stručnim događajima u organizaciji fakulteta. Sistem omogućava organizatorima jednostavno kreiranje, izmenu i pregled događaja, kao i upravljanje predavačima, lokacijama i prijavama učesnika.
Podržani tipovi događaja uključuju:
*konferencije
*seminare
*radionice
*predavanja
Cilj sistema je centralizacija svih informacija i pojednostavljenje procesa organizacije događaja.

  Korišćene tehnologije
Backend
*ASP.NET Core 8.0 (Web API)
*C#
*Entity Framework Core 8

Baza podataka
*PostgreSQL / SQL Server
*ORM: Entity Framework Core
*Code First pristup (migracije)

Alati i okruženje
Visual Studio
Git (verzionisanje)

  Arhitektura
Aplikacija je organizovana po slojevima:
  Controllers – obrada HTTP zahteva
  Services – poslovna logika
  Repositories – pristup bazi
  Models/Entities – domen modeli
  DTOs – objekti za prenos podataka

  Funkcionalnosti sistema
Organizator može:
  kreirati, izmeniti i obrisati događaje
  definisati lokacije i njihov kapacitet
  unositi i uređivati podatke o predavačima
  povezivati predavače sa događajima
  pregledati sve dostupne događaje
Sistem omogućava:
  evidenciju prijava učesnika
  validaciju kapaciteta lokacije
  pregled svih relevantnih informacija
