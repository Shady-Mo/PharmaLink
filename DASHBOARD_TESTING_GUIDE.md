# Dashboard Testing Guide

## Manual Testing Checklist

### 1. Authentication & Authorization
- [ ] Test endpoint without JWT token → 401 Unauthorized
- [ ] Test endpoint with invalid JWT token → 401 Unauthorized
- [ ] Test endpoint as non-Patient role (Pharmacist/Admin) → 403 Forbidden
- [ ] Test endpoint as Patient role → 200 OK

### 2. Query Parameters
- [ ] Test with default (no recentOrdersCount param) → returns 5 recent orders
- [ ] Test with recentOrdersCount=3 → returns 3 recent orders
- [ ] Test with recentOrdersCount=20 → returns max 20 recent orders
- [ ] Test with recentOrdersCount=21 → defaults back to 5
- [ ] Test with recentOrdersCount=0 → defaults back to 5
- [ ] Test with recentOrdersCount=-5 → defaults back to 5

### 3. Dashboard Statistics
- [ ] Verify totalOrders matches count in database
- [ ] Verify pendingPrescriptionReviews count is accurate
- [ ] Verify savedAddresses count matches patient's addresses
- [ ] Verify rewardPoints returns 0 (placeholder implementation)

### 4. Current Order
- [ ] New patient with no orders → currentOrder = null
- [ ] Patient with one order → currentOrder populated correctly
- [ ] Patient with multiple orders → currentOrder shows most recent
- [ ] Order progress timeline includes all fulfillment legs
- [ ] Progress timeline ordered chronologically

### 5. Recent Orders
- [ ] New patient with no orders → recentOrders = []
- [ ] Patient with 3 orders → get 3 recent orders (or less if fewer exist)
- [ ] Patient with 10 orders → get 5 recent orders (default)
- [ ] Recent orders exclude current order
- [ ] Orders ordered by creation date descending
- [ ] Medicine list correctly populated with BrandName
- [ ] OrderNumber formatted correctly (8 char uppercase)

### 6. Pagination
- [ ] Patient with 5 total orders → hasMoreOrders = false
- [ ] Patient with 6 total orders (5 + 1 current) → hasMoreOrders = false
- [ ] Patient with 7 total orders (5 recent + 1 current + 1 more) → hasMoreOrders = true
- [ ] Patient with >25 orders → hasMoreOrders = true with default count

### 7. Error Handling
- [ ] Test with non-existent patient ID → 404 Patient.NotFound
- [ ] Simulate database error → 500 error with descriptive message
- [ ] Test with null/empty data → no null reference exceptions
- [ ] Large dataset performance → response time < 2 seconds

### 8. Data Accuracy
- [ ] Order totals match sum of order items
- [ ] Drug names match database records
- [ ] Fulfillment leg statuses are valid enums
- [ ] Dates are in UTC format
- [ ] GUIDs are valid format

## Unit Test Examples

```csharp
[TestFixture]
public class DashboardServiceTests
{
    private DashboardService _dashboardService;
    private Mock<AppDbContext> _mockContext;

    [SetUp]
    public void Setup()
    {
        _mockContext = new Mock<AppDbContext>();
        _dashboardService = new DashboardService(_mockContext.Object);
    }

    [Test]
    public async Task GetDashboardAsync_WithValidPatient_ReturnsSuccess()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        // ... setup mock data

        // Act
        var result = await _dashboardService.GetDashboardAsync(patientId);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
    }

    [Test]
    public async Task GetDashboardAsync_WithNonExistentPatient_ReturnsFailure()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        // ... setup mock to return no patient

        // Act
        var result = await _dashboardService.GetDashboardAsync(patientId);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("Patient.NotFound", result.Error.Code);
    }

    [Test]
    public async Task GetDashboardAsync_WithMultipleOrders_ExcludesCurrentFromRecent()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        // ... setup mock with 10 orders

        // Act
        var result = await _dashboardService.GetDashboardAsync(patientId, 5);

        // Assert
        Assert.AreEqual(5, result.Value.RecentOrders.Count);
        Assert.AreNotEqual(
            result.Value.CurrentOrder?.OrderId,
            result.Value.RecentOrders.FirstOrDefault()?.OrderId
        );
    }

    [Test]
    public async Task GetDashboardAsync_StatisticsAccurate_CountsMatch()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var expectedOrderCount = 5;
        var expectedPendingReviews = 2;
        var expectedAddresses = 3;
        // ... setup mock data accordingly

        // Act
        var result = await _dashboardService.GetDashboardAsync(patientId);

        // Assert
        Assert.AreEqual(expectedOrderCount, result.Value.Statistics.TotalOrders);
        Assert.AreEqual(expectedPendingReviews, result.Value.Statistics.PendingPrescriptionReviews);
        Assert.AreEqual(expectedAddresses, result.Value.Statistics.SavedAddresses);
    }
}
```

## Integration Test Examples

```csharp
[TestFixture]
public class DashboardControllerTests
{
    private HttpClient _client;
    private TestWebApplicationFactory<Program> _factory;
    private string _authToken;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _factory = new TestWebApplicationFactory<Program>();
        _client = _factory.CreateClient();
        _authToken = await GetPatientAuthToken();
    }

    [Test]
    public async Task GetDashboard_AuthenticatedPatient_Returns200()
    {
        // Arrange
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/dashboard"
        );
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsAsync<PatientDashboardDTO>();
        Assert.IsNotNull(content);
        Assert.IsNotNull(content.Statistics);
    }

    [Test]
    public async Task GetDashboard_Unauthenticated_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/dashboard");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Test]
    public async Task GetDashboard_WithQueryParameter_UsesParameterValue()
    {
        // Arrange
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/dashboard?recentOrdersCount=10"
        );
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);

        // Act
        var response = await _client.SendAsync(request);
        var content = await response.Content.ReadAsAsync<PatientDashboardDTO>();

        // Assert
        Assert.LessOrEqual(content.RecentOrders.Count, 10);
    }
}
```

## API Testing with Postman/Insomnia

### Collection Template
```json
{
  "info": {
    "name": "Dashboard API Tests",
    "version": "1.0"
  },
  "item": [
    {
      "name": "Get Dashboard",
      "request": {
        "method": "GET",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{jwt_token}}"
          }
        ],
        "url": {
          "raw": "{{base_url}}/api/v1/dashboard?recentOrdersCount=5",
          "host": ["{{base_url}}"],
          "path": ["api", "v1", "dashboard"],
          "query": [
            {
              "key": "recentOrdersCount",
              "value": "5"
            }
          ]
        }
      }
    }
  ],
  "variable": [
    {
      "key": "base_url",
      "value": "http://localhost:5000"
    },
    {
      "key": "jwt_token",
      "value": ""
    }
  ]
}
```

## Performance Testing

### Load Test Scenario
- Simulate 100 concurrent requests to dashboard endpoint
- Measure response time (target: < 2 seconds)
- Monitor database connections
- Check memory usage under load

### Query Performance
- Verify indexes on:
  - Orders.PatientUserId
  - PrescriptionReviews.PatientUserId
  - Addresses.UserId
  - OrderFulfillmentLegs.OrderId

## Database Validation

```sql
-- Verify patient exists and has data
SELECT TOP 1 
    p.Id,
    COUNT(DISTINCT o.OrderId) as OrderCount,
    COUNT(DISTINCT pr.PrescriptionReviewId) as PendingReviews,
    COUNT(DISTINCT a.AddressId) as AddressCount
FROM Patients p
LEFT JOIN Orders o ON p.Id = o.PatientUserId
LEFT JOIN PrescriptionReviews pr ON p.Id = pr.PatientUserId 
    AND pr.ReviewStatus = 1
LEFT JOIN Addresses a ON p.Id = a.UserId
GROUP BY p.Id;

-- Verify order with fulfillment legs
SELECT TOP 1
    o.OrderId,
    o.OrderStatus,
    COUNT(DISTINCT ofl.LegId) as LegCount
FROM Orders o
LEFT JOIN OrderFulfillmentLegs ofl ON o.OrderId = ofl.OrderId
GROUP BY o.OrderId, o.OrderStatus
ORDER BY o.CreatedAt DESC;
```

## Expected Response Times
- With 5 recent orders: < 500ms
- With 20 recent orders: < 1000ms
- Large patient dataset (100+ orders): < 2000ms

## Notes
- All timestamps should be in UTC
- GUIDs should be properly formatted
- Null values should be explicitly handled in client
- Error messages should be user-friendly (sanitized)
