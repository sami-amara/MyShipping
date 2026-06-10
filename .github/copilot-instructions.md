# Copilot Instructions

## General Guidelines
- Present implementation plans before proposing or making code changes; include rationale, steps, potential impacts, and prefer implementing changes step-by-step with incremental validation rather than large batch changes; when refactoring, inline CSS first, then inline scripts to better understand file organization. Use evidence-based debugging: reproduce issues using existing working code paths, rely on logs, tests, and observed behavior to guide fixes rather than speculative guesses.
- Do not make any code changes without explicit approval from the requester.
- Comment out code instead of removing it during debugging/testing changes so it can be reviewed and removed later.

## Project Guidelines
- When reverting a file, verify the restored file is not empty and confirm content integrity before reporting completion.
- When editing `ShipmentService.js`, apply minimal targeted changes only and avoid broad refactors; do not remove or alter function structure. When fixing JavaScript payload issues, prefer modifying existing functions (e.g., getShipmentFormData) instead of introducing new helper functions; validate changes incrementally.
- For this project (learning/testing), keep temporary API keys in `appsettings` for now.
- Use strict localization via resource keys/properties; avoid hardcoded UI/alert text in views and page event scripts. Use the existing ManagePageControls retrieval pattern to load/select all dropdown options (for example Countries, Cities, Carrier). Do not hardcode any dropdown values in scripts/views — load lists (including all payment methods) from the database via ManagePageControls.
- Load all payment methods from the database using the existing ManagePageControls pattern and present a single PaymentMethod dropdown in the Create view; do not duplicate payment inputs or hardcode payment options. Reuse existing payment interfaces and services in Business.Contracts and Business.Services; avoid introducing duplicate interfaces, services, or abstractions during refactoring.
- Remove unused payment-method preselection code/properties from account settings when there is no payment dropdown.
- Refactor repetitive dropdown option builders into a cleaner pattern while keeping architecture-consistent placement decisions.
- Use existing project validation patterns for new validation changes; avoid implementing custom ad-hoc controller checks. Extend or reuse shared validators, model-level annotations, middleware, or the established validation framework and update relevant tests as needed.
- Handle shared messages like payment alert texts through one global static file instead of page-specific duplicate handlers. Prefer shared alert/message texts to live in common shared partials/global static infrastructure rather than page-specific hardcoded view markup.
- When applying global font changes, update the main CSS files for admin and user themes instead of site-custom.css or admin-custom.css.