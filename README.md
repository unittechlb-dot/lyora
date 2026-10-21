# LYORA – Smart Luxury Perfume MIS

Senior project – University of Tripoli, Lebanon (2025–2026).
A perfume store and management information system where every sale automatically deducts the raw materials of each perfume according to its formula (BOM).

## Contents

| Folder | What it is |
|---|---|
| `02_Database/` | SQL Server scripts – run them in SSMS in order 01 → 04 |
| `03_Website_Code/Lyora/` | ASP.NET Core MVC (.NET 8, C#) solution with 3 layers: `Lyora.Web` (Presentation), `Lyora.BLL` (Business Logic), `Lyora.DAL` (Data Access) |

## Run it (Windows)

1. Install **SQL Server Express**, **SSMS** and **Visual Studio 2022** (workload *ASP.NET and web development*).
2. In SSMS run `01_Create_Database.sql`, `02_Sample_Data.sql`, `03_Procedures_and_Views.sql` (and `04_Test_BOM_Scenario.sql` for the BOM demo).
3. Open `03_Website_Code/Lyora/Lyora.sln` and set your server name in `Lyora.Web/appsettings.json` → `ConnectionStrings:LyoraDB`.
4. Press **F5**.

Sample logins: `admin@lyora.com` / `Admin@123` · `sara@example.com` / `Customer@123`
