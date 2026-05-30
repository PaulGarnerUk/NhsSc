# Preamble
This file contains information about the sqlite database used by the Nhs Sc project

# Tables
The database should contain the following tables

## Catalog
This table is created by calling the nhssc.exe passing commandline param --rebuild-catalog and passing the name of the spreadsheet containing the catalog data. eg 
`nhssc --rebuild-catalog 'c:\temp\nhssc\Sample Catalogue.xlsx'`

The catalog table contains the following columns

| Name | Type | Description
|------|------|-------------
| id   | int  | Primary key
| NPC  | text | NHS Product Code
| EClass | text |
| Section | text | Type of item
| BaseDescription | text | Primary description
| SecondaryDescription | text | Secondary description
| Supplier | text | Supplier
| Brand | text | Brand
| MPC | text | Manufacturer Product Code
| UOI | text | Unit of Issue
| Unit | numeric | Units
| B1Price | numeric | Decimal price for the unit
| IndividualPrice | numeric | Calculated individual unit price