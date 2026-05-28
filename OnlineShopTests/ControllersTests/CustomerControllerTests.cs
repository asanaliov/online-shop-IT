using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using online_shop_IT.Data;
using online_shop_IT.Models;
using OnlineShopTests.Utils;

namespace OnlineShopTests.ControllersTests;

[Collection("Test Suite")]
public class 
    CustomerControllerTests : LoggedTestBase, IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CustomerControllerTests(WebApplicationFactory<Program> factory, GlobalTestFixture fixture) : base(fixture)
    {
        _factory = factory.WithTestDatabase();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });
        _client.Timeout = TimeSpan.FromSeconds(10);
    }

    // ── Req 3: Validation ──────────────────────────────────────

    [LoggedFact(Category = "CustomerController", Points = 3)]
    public async Task Create_MissingFirstName_ReturnsView()
    {
        await RunTestAsync(async () =>
        {
            var initialCount = await TestDatabaseHelper.GetCount<Customer>(_factory.Services);
            var getResponse = await _client.GetAsync("/Customer/Create");
            var token = await getResponse.GetAntiForgeryTokenAsync();

            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("FirstName", ""), // Required
                new KeyValuePair<string, string>("LastName", "Петровски"),
                new KeyValuePair<string, string>("Email", "test@example.com"),
                new KeyValuePair<string, string>("PhoneNumber", "070111222"),
                new KeyValuePair<string, string>("MembershipDate", "2024-01-01"),
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
            });

            var response = await _client.PostAsync("/Customer/Create", form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(initialCount, await TestDatabaseHelper.GetCount<Customer>(_factory.Services));
        });
    }

    [LoggedFact(Category = "CustomerController", Points = 3)]
    public async Task Create_MissingLastName_ReturnsView()
    {
        await RunTestAsync(async () =>
        {
            var initialCount = await TestDatabaseHelper.GetCount<Customer>(_factory.Services);
            var getResponse = await _client.GetAsync("/Customer/Create");
            var token = await getResponse.GetAntiForgeryTokenAsync();

            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("FirstName", "Александар"),
                new KeyValuePair<string, string>("LastName", ""), // Required
                new KeyValuePair<string, string>("Email", "test@example.com"),
                new KeyValuePair<string, string>("PhoneNumber", "070111222"),
                new KeyValuePair<string, string>("MembershipDate", "2024-01-01"),
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
            });

            var response = await _client.PostAsync("/Customer/Create", form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(initialCount, await TestDatabaseHelper.GetCount<Customer>(_factory.Services));
        });
    }

    [LoggedFact(Category = "CustomerController", Points = 5)]
    public async Task Create_InvalidPhoneNumber_ReturnsView()
    {
        await RunTestAsync(async () =>
        {
            var initialCount = await TestDatabaseHelper.GetCount<Customer>(_factory.Services);
            var getResponse = await _client.GetAsync("/Customer/Create");
            var token = await getResponse.GetAntiForgeryTokenAsync();

            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("FirstName", "Александар"),
                new KeyValuePair<string, string>("LastName", "Петровски"),
                new KeyValuePair<string, string>("Email", "test@example.com"),
                new KeyValuePair<string, string>("PhoneNumber", "123"), // Not 9 digits
                new KeyValuePair<string, string>("MembershipDate", "2024-01-01"),
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
            });

            var response = await _client.PostAsync("/Customer/Create", form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(initialCount, await TestDatabaseHelper.GetCount<Customer>(_factory.Services));
        });
    }

    // ── CRUD: Create ───────────────────────────────────────────

    [LoggedFact(Category = "CustomerController", Points = 1)]
    public async Task Create_ValidCustomer_RedirectsToIndex()
    {
        await RunTestAsync(async () =>
        {
            var initialCount = await TestDatabaseHelper.GetCount<Customer>(_factory.Services);
            var getResponse = await _client.GetAsync("/Customer/Create");
            var token = await getResponse.GetAntiForgeryTokenAsync();

            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("FirstName", "Тест"),
                new KeyValuePair<string, string>("LastName", "Клиент"),
                new KeyValuePair<string, string>("Email", "tester@example.com"),
                new KeyValuePair<string, string>("PhoneNumber", "070111222"),
                new KeyValuePair<string, string>("MembershipDate", "2024-01-01"),
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
            });

            var response = await _client.PostAsync("/Customer/Create", form);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Customer", response.Headers.Location?.ToString());
            Assert.Equal(initialCount + 1, await TestDatabaseHelper.GetCount<Customer>(_factory.Services));
        });
    }

    // ── CRUD: Details ──────────────────────────────────────────

    [LoggedFact(Category = "CustomerController", Points = 1)]
    public async Task Details_ValidId_ReturnsCustomer()
    {
        await RunTestAsync(async () =>
        {
            var customer = await TestDatabaseHelper.GetFirst<Customer>(_factory.Services);
            var response = await _client.GetAsync($"/Customer/Details/{customer.Id}");
            response.EnsureSuccessStatusCode();
            Assert.Contains(customer.FirstName, await response.Content.ReadAsStringAsync());
        });
    }

    [LoggedFact(Category = "CustomerController", Points = 1)]
    public async Task Details_InvalidId_ReturnsNotFound()
    {
        await RunTestAsync(async () =>
        {
            var response = await _client.GetAsync("/Customer/Details/99999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        });
    }

    // ── Req 7c: Customer/Details shows orders with delivery status

    [LoggedFact(Category = "CustomerController", Points = 5)]
    public async Task Details_ShowsOrdersWithDeliveryStatus()
    {
        await RunTestAsync(async () =>
        {
            var customer = await GetCustomerWithOrdersAsync();
            var response = await _client.GetAsync($"/Customer/Details/{customer.Id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // Should show "Не е доставено" for active order
            Assert.Contains("Не е доставено", content);
            // Should show return button for active order
            Assert.Contains("deliver-btn", content);
        });
    }

    // ── Req 7d: Customer/DeliverOrder sets DeliveryDate ────────

    [LoggedFact(Category = "CustomerController", Points = 5)]
    public async Task DeliverOrder_SetsDeliveryDateAndRedirectsToCustomerDetails()
    {
        await RunTestAsync(async () =>
        {
            var order = await GetActiveOrderAsync();
            Assert.NotNull(order);

            var response = await _client.PostAsync($"/Customer/DeliverOrder/{order.Id}", new StringContent(""));

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains($"/Customer/Details/{order.CustomerId}", response.Headers.Location?.ToString());

            var updated = TestDatabaseHelper.GetById<Order>(_factory.Services, o => o.Id == order.Id);
            Assert.NotNull(updated);
            Assert.NotNull(updated.DeliveryDate);
        });
    }

    // ── CRUD: Edit ─────────────────────────────────────────────

    [LoggedFact(Category = "CustomerController", Points = 1)]
    public async Task Edit_ValidCustomer_RedirectsToIndex()
    {
        await RunTestAsync(async () =>
        {
            var customer = await TestDatabaseHelper.GetFirst<Customer>(_factory.Services);
            var getResponse = await _client.GetAsync($"/Customer/Edit/{customer.Id}");
            var token = await getResponse.GetAntiForgeryTokenAsync();

            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Id", customer.Id.ToString()),
                new KeyValuePair<string, string>("FirstName", customer.FirstName + " ИЗМ"),
                new KeyValuePair<string, string>("LastName", customer.LastName),
                new KeyValuePair<string, string>("Email", customer.Email),
                new KeyValuePair<string, string>("PhoneNumber", customer.PhoneNumber),
                new KeyValuePair<string, string>("MembershipDate", customer.MembershipDate.ToString("yyyy-MM-dd")),
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
            });

            var response = await _client.PostAsync($"/Customer/Edit/{customer.Id}", form);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Customer", response.Headers.Location?.ToString());
        });
    }

    // ── CRUD: Delete ───────────────────────────────────────────

    [LoggedFact(Category = "CustomerController", Points = 1)]
    public async Task Delete_ValidCustomer_RemovesAndRedirects()
    {
        await RunTestAsync(async () =>
        {
            var initialCount = await TestDatabaseHelper.GetCount<Customer>(_factory.Services);
            var customer = await TestDatabaseHelper.GetFirst<Customer>(_factory.Services);
            var getResponse = await _client.GetAsync($"/Customer/Delete/{customer.Id}");
            var token = await getResponse.GetAntiForgeryTokenAsync();

            var response = await _client.PostAsync($"/Customer/Delete/{customer.Id}",
                new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("__RequestVerificationToken", token) }));

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal(initialCount - 1, await TestDatabaseHelper.GetCount<Customer>(_factory.Services));
        });
    }

    // ── Req 5: Tabulator page ──────────────────────────────────

    [LoggedFact(Category = "CustomerController", Points = 3)]
    public async Task Tabulator_ReturnsView()
    {
        await RunTestAsync(async () =>
        {
            var response = await _client.GetAsync("/Customer/Tabulator");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("tabulator", content, StringComparison.OrdinalIgnoreCase);
        });
    }

    // ── Req 5b: API endpoint returns JSON ─────────────────────

    [LoggedFact(Category = "CustomerController", Points = 5)]
    public async Task Api_GetCustomers_ReturnsJsonList()
    {
        await RunTestAsync(async () =>
        {
            var response = await _client.GetAsync("/api/CustomerApi");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var customers = JsonSerializer.Deserialize<JsonElement[]>(content);

            Assert.NotNull(customers);
            Assert.True(customers.Length > 0);

            var first = customers[0];
            Assert.True(first.TryGetProperty("firstName", out _) || first.TryGetProperty("FirstName", out _),
                "JSON must contain firstName field");
            Assert.True(first.TryGetProperty("lastName", out _) || first.TryGetProperty("LastName", out _),
                "JSON must contain lastName field");
            Assert.True(first.TryGetProperty("email", out _) || first.TryGetProperty("Email", out _),
                "JSON must contain email field");
        });
    }

    // ── Helpers ────────────────────────────────────────────────

    private async Task<Customer> GetCustomerWithOrdersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Customers
            .Where(c => c.Orders.Any(o => o.DeliveryDate == null))
            .FirstAsync();
    }

    private async Task<Order?> GetActiveOrderAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Orders.Where(o => o.DeliveryDate == null).FirstOrDefaultAsync();
    }

    public async Task InitializeAsync() => await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services);
    public async Task DisposeAsync() => await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services);
}
