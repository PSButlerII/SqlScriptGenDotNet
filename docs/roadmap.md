# Roadmap

Current support is intentionally limited to PostgreSQL/MySQL create-table/create-database generation.

Realistic future work: SQLite and SQL Server renderers; indexes; alter-table operations; views; multiple-table project documents; dependency ordering; schema import; migration diff generation; and optional desktop/web interfaces. Each dialect addition requires a catalog, renderer, validation rules, fixtures, and exact-output tests.

MongoDB will not be listed as a SQL dialect. A document-schema generator could be evaluated as a separate product with different domain concepts and output contracts.
