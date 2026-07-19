# Dashboard Feature - Quick Start Guide

## What's New

A comprehensive patient dashboard endpoint has been added to PharmaLink that displays:
- Account statistics (orders, pending reviews, saved addresses, reward points)
- Current order progress with fulfillment timeline
- Recent order history
- Pagination support for more orders

## Endpoint

```
GET /api/v1/dashboard?recentOrdersCount=5
```

## Quick Integration

### For Frontend Developers

```typescript
// Example: Fetch dashboard data
async function getPatientDashboard(token: string, recentOrdersCount: number = 5) {
  const response = await fetch(
    `/api/v1/dashboard?recentOrdersCount=${recentOrdersCount}`,
    {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    }
  );

  if (!response.ok) {
    throw new Error(`Dashboard fetch failed: ${response.statusText}`);
  }

  return response.json();
}

// Usage
const dashboard = await getPatientDashboard(authToken);

// Display loading state while fetching
console.log('Statistics:', dashboard.statistics);
console.log('Current Order:', dashboard.currentOrder);
console.log('Recent Orders:', dashboard.recentOrders);
console.log('Has More Orders?', dashboard.hasMoreOrders);
```

### For Mobile/Flutter Developers

```dart
// Example: Fetch dashboard data
Future<PatientDashboard> getDashboard(String token, {int recentOrdersCount = 5}) async {
  final response = await http.get(
    Uri.parse('$baseUrl/api/v1/dashboard?recentOrdersCount=$recentOrdersCount'),
    headers: {
      'Authorization': 'Bearer $token',
      'Content-Type': 'application/json',
    },
  );

  if (response.statusCode == 200) {
    return PatientDashboard.fromJson(jsonDecode(response.body));
  } else {
    throw Exception('Failed to load dashboard');
  }
}
```

## Response Format

```json
{
  "statistics": {
    "totalOrders": 5,
    "pendingPrescriptionReviews": 1,
    "savedAddresses": 2,
    "rewardPoints": 0
  },
  "currentOrder": null,  // or OrderInfo
  "recentOrders": [],    // Array of recent orders
  "hasMoreOrders": false
}
```

## Error Handling

```typescript
// Handle different error scenarios
try {
  const dashboard = await getPatientDashboard(token);
} catch (error) {
  if (error.response?.status === 401) {
    // Redirect to login
  } else if (error.response?.status === 403) {
    // User is not a patient
  } else if (error.response?.status === 404) {
    // Patient not found
  } else if (error.response?.status === 500) {
    // Server error
  }
}
```

## Loading States

```typescript
// Show loading state
const [isLoading, setIsLoading] = useState(true);
const [dashboard, setDashboard] = useState(null);

useEffect(() => {
  const fetchDashboard = async () => {
    try {
      setIsLoading(true);
      const data = await getPatientDashboard(token);
      setDashboard(data);
    } finally {
      setIsLoading(false);
    }
  };

  fetchDashboard();
}, [token]);

// In JSX
{isLoading ? <LoadingSpinner /> : <Dashboard data={dashboard} />}
```

## Display Guidelines

### Statistics Display
```typescript
<StatisticsCard 
  title="Total Orders"
  value={dashboard.statistics.totalOrders}
  icon="shopping-bag"
/>
<StatisticsCard 
  title="Pending Reviews"
  value={dashboard.statistics.pendingPrescriptionReviews}
  icon="clipboard"
/>
<StatisticsCard 
  title="Saved Addresses"
  value={dashboard.statistics.savedAddresses}
  icon="map-pin"
/>
<StatisticsCard 
  title="Reward Points"
  value={dashboard.statistics.rewardPoints}
  icon="star"
/>
```

### Current Order Display
```typescript
{dashboard.currentOrder && (
  <CurrentOrderCard 
    orderId={dashboard.currentOrder.orderId}
    status={getOrderStatusLabel(dashboard.currentOrder.status)}
    progress={dashboard.currentOrder.progressTimeline}
  />
)}
```

### Recent Orders Display
```typescript
<RecentOrdersList 
  orders={dashboard.recentOrders}
  onViewAll={() => navigateTo('/orders')}
  hasMore={dashboard.hasMoreOrders}
/>
```

## Enum Values Reference

### Order Status
- 1 = Pending
- 2 = Processing
- 3 = Shipped
- 4 = Completed
- 5 = Cancelled

### Leg Type
- 1 = Preparation
- 2 = Delivery

### Leg Status
- 1 = Assigned
- 2 = Preparing
- 3 = Ready for Pickup
- 4 = Picked up by Courier
- 5 = Completed
- 6 = Cancelled

## Query Parameters

| Parameter | Type | Default | Range | Purpose |
|-----------|------|---------|-------|---------|
| recentOrdersCount | int | 5 | 1-20 | Number of recent orders to retrieve |

```typescript
// Fetch with 10 recent orders instead of default 5
const dashboard = await getPatientDashboard(token, 10);

// Request will be: GET /api/v1/dashboard?recentOrdersCount=10
```

## Common Use Cases

### 1. Display Dashboard on App Load
```typescript
useEffect(() => {
  const loadDashboard = async () => {
    const data = await getPatientDashboard(authToken);
    updateDashboardUI(data);
  };

  loadDashboard();
}, [authToken]);
```

### 2. Show Current Order Progress
```typescript
if (dashboard.currentOrder) {
  const progress = dashboard.currentOrder.progressTimeline;
  progress.forEach(step => {
    console.log(`${step.pharmacyName}: ${getLegStatusLabel(step.status)}`);
  });
}
```

### 3. Navigate to Full Orders List
```typescript
if (dashboard.hasMoreOrders) {
  <Button onClick={() => navigateTo('/orders')}>
    View All Orders
  </Button>
}
```

### 4. Auto-refresh Dashboard
```typescript
const refreshDashboard = async () => {
  const updated = await getPatientDashboard(authToken);
  setDashboard(updated);
};

// Refresh every 30 seconds
setInterval(refreshDashboard, 30000);
```

## Testing the Endpoint

### Using cURL
```bash
curl -X GET "http://localhost:5000/api/v1/dashboard?recentOrdersCount=5" \
  -H "Authorization: Bearer your_jwt_token_here" \
  -H "Content-Type: application/json"
```

### Using Postman
1. Method: GET
2. URL: `{{base_url}}/api/v1/dashboard`
3. Query Params: `recentOrdersCount=5`
4. Headers: `Authorization: Bearer {{jwt_token}}`
5. Click Send

## Troubleshooting

### 401 Unauthorized
- Check JWT token is valid
- Ensure token hasn't expired
- Verify Authorization header format: `Bearer {token}`

### 403 Forbidden
- User account must be a Patient role
- Cannot access dashboard as Pharmacist or Admin

### 404 Not Found
- Patient account may not exist
- Check patient ID in JWT token

### 500 Internal Server Error
- Check server logs
- Database connectivity issue
- Contact support with error details

## Performance Tips

1. **Pagination**: Use `recentOrdersCount=5` by default, increase only if needed
2. **Caching**: Consider client-side caching to reduce requests
3. **Refresh**: Auto-refresh every 30-60 seconds for real-time updates
4. **Lazy Loading**: Load current order details separately if needed

## Documentation

For detailed information, see:
- `DASHBOARD_API_REFERENCE.md` - Complete API specification
- `DASHBOARD_TESTING_GUIDE.md` - Testing procedures
- `DASHBOARD_IMPLEMENTATION.md` - Technical details

## Support

For issues or questions:
1. Check troubleshooting section above
2. Review documentation files
3. Check server logs
4. Contact backend team

---

**Note**: This endpoint requires patient authentication. Ensure valid JWT token is provided in all requests.
