[![Package Version](https://img.shields.io/nuget/v/Impower.DocumentUnderstanding.Activities.svg?style=flat-square)](https://www.nuget.org/packages/Impower.DocumentUnderstanding.Activities/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](https://opensource.org/licenses/MIT)

> :warning: **Currently only available for net461**
# Usage
Available publicly on [nuget.org](https://www.nuget.org/packages/Impower.DocumentUnderstanding.Activities/)

## Activities
- Filter Taxonomy By Field ID's
  - Takes a 'DocumentTaxonomy' object and filters to a set of fields.
  - Filtered taxonomy can be used in conjuction with [Create Document Validation Action](https://docs.uipath.com/activities/docs/create-document-validation-action) to only get manual validation on the target fields
- Merge Extraction Results
  - Writes one ExtractionResult object onto another at the *ResultDataPoint* level.
  - Used to integrate results from a filtered [Create Document Validation Action](https://docs.uipath.com/activities/docs/create-document-validation-action) back into the original results
- Load Rules From File
- Run Rule On Extraction Result
- Run Rule Set On Extraction Result