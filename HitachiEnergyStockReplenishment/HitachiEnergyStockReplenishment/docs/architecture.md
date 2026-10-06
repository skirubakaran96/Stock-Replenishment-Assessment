# Architecture Notes

## Workflow

```text
Draft
  |
  | POST /submit
  v
Submitted + StockValidation=Pending
  |
  | background worker calls external stock service
  +----> Failed ----> reviewer may Reject
  |
  v
Passed
  |
  +----> Approved ----> Fulfilled
  |
  +----> Rejected
```

## Async validation sequence

1. API loads the draft and changes it to `Submitted/Pending`.
2. API saves the state.
3. API writes the request ID to a bounded-by-design application queue (currently unbounded Channel for the small assignment scope).
4. API returns `202 Accepted` immediately.
5. Hosted worker consumes the ID.
6. Worker calls the simulated slow external service.
7. Worker persists `Passed` or `Failed` plus a human-readable message.
8. Blazor polls the status endpoint every second while pending.

For a production METS-scale system, the same abstraction can be backed by Azure Service Bus/Storage Queue or another durable broker, with retry, dead-lettering, idempotency and distributed tracing.
