# Sample Documents

This folder contains local sample content for learning and manual API testing.

## `docs`

Small synthetic `.md` files used for Phase 1 text/markdown ingestion.

These files are safe to commit because they are project-owned sample data and keep the repository easy to run after cloning.

Use the API ingestion endpoint with:

```json
{
  "sourcePath": "samples/docs"
}
```

## `scanned`

Local scanned PDF/image files used for Phase 2 Azure AI Document Intelligence testing.

Recommended learning datasets:

- SROIE / ICDAR 2019 scanned receipts
- RVL-CDIP scanned document images

Do not commit downloaded dataset files or real customer documents. Keep only documentation files in git and place local scanned files here when testing.

Supported file types:

- `.pdf`
- `.png`
- `.jpg`
- `.jpeg`
- `.bmp`
- `.tif`
- `.tiff`

Example local layout:

```text
ProjectRag.Api/samples/scanned/
  README.md
  receipt-001.jpg
  receipt-002.jpg
  invoice-001.pdf
```

Use the API ingestion endpoint with:

```json
{
  "sourcePath": "samples/scanned"
}
```