# Dashboard API Quick Reference

## Endpoint
```
GET /api/v1/dashboard?recentOrdersCount=5
```

## Authentication
- Required: YES (JWT Bearer token)
- Required Role: Patient

## Query Parameters
| Parameter | Type | Default | Max | Description |
|-----------|------|---------|-----|-------------|
| recentOrdersCount | int | 5 | 20 | Number of recent orders to retrieve |

## Response Structure

### Top Level
```csharp
{
  "statistics": DashboardStatisticsDTO,
  "currentOrder": CurrentOrderInfoDTO?,
  "recentOrders": List<RecentOrderSummaryDTO>,
  "hasMoreOrders": bool
}
```

### DashboardStatisticsDTO
```csharp
{
  "totalOrders": int,                    // Total orders placed
  "pendingPrescriptionReviews": int,     // Awaiting pharmacist approval
  "savedAddresses": int,                 // Delivery addresses on file
  "rewardPoints": int                    // Loyalty points (currently 0)
}
```

### CurrentOrderInfoDTO (nullable)
```csharp
{
  "orderId": Guid,
  "status": OrderStatus,                 // enum: 1=Pending, 2=Processing, 3=Shipped, 4=Completed, 5=Cancelled
  "progressTimeline": OrderProgressStepDTO[]
}
```

### OrderProgressStepDTO
```csharp
{
  "fulfillmentLegId": Guid,
  "legType": LegType,                    // enum: 1=Preparation, 2=Delivery
  "status": LegStatus,                   // enum: 1=Assigned, 2=Preparing, 3=ReadyForPickup, 4=PickedUpByCourier, 5=Completed, 6=Cancelled
  "pharmacyName": string?,               // Name of handling pharmacy branch
  "estimatedCompletionTime": DateTime?
}
```

### RecentOrderSummaryDTO
```csharp
{
  "orderId": Guid,
  "orderNumber": string,                 // Formatted ID (first 8 chars uppercase)
  "orderDate": DateTime,
  "medicines": OrderedMedicineDTO[],
  "totalAmount": decimal,
  "status": OrderStatus
}
```

### OrderedMedicineDTO
```csharp
{
  "drugId": Guid,
  "drugName": string,                    // Brand name of the medicine
  "quantity": int
}
```

## HTTP Status Codes

| Code | Meaning | Cause |
|------|---------|-------|
| 200 | OK | Successful retrieval |
| 400 | Bad Request | Invalid query parameters (e.g., recentOrdersCount > 20) |
| 401 | Unauthorized | Missing or invalid JWT token |
| 403 | Forbidden | User is not a Patient |
| 404 | Not Found | Patient not found (in response body as error) |
| 500 | Internal Server Error | Database or server error |

## Example Requests

### Get dashboard with default parameters
```bash
curl -H "Authorization: Bearer {token}" \
  https://api.pharmalink.com/api/v1/dashboard
```

### Get dashboard with 10 recent orders
```bash
curl -H "Authorization: Bearer {token}" \
  "https://api.pharmalink.com/api/v1/dashboard?recentOrdersCount=10"
```

## Common Response Scenarios

### Patient with active order and history
```json
{
  "statistics": {
    "totalOrders": 15,
    "pendingPrescriptionReviews": 2,
    "savedAddresses": 3,
    "rewardPoints": 0
  },
  "currentOrder": {
    "orderId": "550e8400-e29b-41d4-a716-446655440000",
    "status": 2,
    "progressTimeline": [
      {
        "fulfillmentLegId": "550e8400-e29b-41d4-a716-446655440001",
        "legType": 1,
        "status": 2,
        "pharmacyName": "Downtown Pharmacy",
        "estimatedCompletionTime": "2025-01-20T15:00:00Z"
      }
    ]
  },
  "recentOrders": [
    {
      "orderId": "550e8400-e29b-41d4-a716-446655440002",
      "orderNumber": "550E8400",
      "orderDate": "2025-01-18T10:30:00Z",
      "medicines": [
        {
          "drugId": "550e8400-e29b-41d4-a716-446655440003",
          "drugName": "Amoxicillin 500mg",
          "quantity": 2
        }
      ],
      "totalAmount": 150.00,
      "status": 4
    }
  ],
  "hasMoreOrders": true
}
```

### New patient with no orders
```json
{
  "statistics": {
    "totalOrders": 0,
    "pendingPrescriptionReviews": 0,
    "savedAddresses": 1,
    "rewardPoints": 0
  },
  "currentOrder": null,
  "recentOrders": [],
  "hasMoreOrders": false
}
```

## Enum Values Reference

### OrderStatus
- 1 = Pending
- 2 = Processing
- 3 = Shipped
- 4 = Completed
- 5 = Cancelled

### LegType
- 1 = Preparation
- 2 = Delivery

### LegStatus
- 1 = Assigned
- 2 = Preparing
- 3 = ReadyForPickup
- 4 = PickedUpByCourier
- 5 = Completed
- 6 = Cancelled

### PrescriptionReviewStatus (for reference)
- 1 = PendingReview
- 2 = Approved
- 3 = Rejected
- 4 = OrderCreated

## Implementation Notes

- PatientUserId is extracted from JWT token (not from request)
- Recent orders exclude the current/most recent order
- Empty states return null for currentOrder and empty array for recentOrders
- hasMoreOrders indicates if more than (recentOrdersCount + 1) orders exist
- Estimated completion times reflect fulfillment leg estimates
- Medicine names use Drug.BrandName field
