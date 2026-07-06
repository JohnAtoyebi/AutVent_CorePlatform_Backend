# Copilot Instructions

## General Guidelines
- Use `IUnitOfWork` and `UnitOfWork` naming for unit-of-work abstractions/implementations instead of `EfUnitOfWork`.
- Use generic request models for pagination, search, and filtering instead of endpoint-specific query parameters.
- Move `HashPassword` logic out of service classes into a dedicated static helper class.
- Use enum types for payment and discount fields (e.g., PaymentMethod and DiscountType) instead of strings in this codebase.
- Use `Id` for ordering (`OrderBy`/`OrderByDescending`) instead of name/date fields in this codebase.
- Prefer SKU/text identifiers to be uppercase alphanumeric (capital letters and numbers).