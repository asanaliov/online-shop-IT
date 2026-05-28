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
public class ProductControllerTests : LoggedTestBase, IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ProductControllerTests(WebApplicationFactory<Program> factory, GlobalTestFixture fixture) : base(fixture)
    {
        _factory = factory.WithTestDatabase();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });
        _client.Timeout = TimeSpan.FromSeconds(10);
    }

    // ── Req 1a: ImageUrl shown as <img> ───────────────────────

    [LoggedFact(Category = "ProductController", Points = 5)]
    public async Task Index_ShowsImageUrlAsImg()
    {
        await RunTestAsync(async () =>
        {
            var response = await _client.GetAsync("/Product");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            Assert.Contains("<img", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("example.com/products", content);
        });
    }

    // ── Req 7e: Product/Details shows active orders ────────────

    [LoggedFact(Category = "ProductController", Points = 5)]
    public async Task Details_ShowsActiveOrdersWhereDeliveryDateIsNull()
    {
        await RunTestAsync(async () =>
        {
            var product = await GetProductWithActiveOrdersAsync();

            var response = await _client.GetAsync($"/Product/Details/{product.Id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // product1 has orders from Александар and Марија (DeliveryDate == null)
            Assert.Contains("Александар", content);
            Assert.Contains("Марија", content);
        });
    }

    [LoggedFact(Category = "ProductController", Points = 1)]
    public async Task Details_InvalidId_ReturnsNotFound()
    {
        await RunTestAsync(async () =>
        {
            var response = await _client.GetAsync("/Product/Details/99999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        });
    }

    // ── Req 7a: Product/Order GET ──────────────────────────────

    [LoggedFact(Category = "ProductController", Points = 5)]
    public async Task Order_GET_ShowsProductName()
    {
        await RunTestAsync(async () =>
        {
            var product = await TestDatabaseHelper.GetFirst<Product>(_factory.Services);
            var response = await _client.GetAsync($"/Product/Order/{product.Id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            Assert.Contains("id=\"product-name\"", content);
            Assert.Contains(product.Name, content);
        });
    }

    [LoggedFact(Category = "ProductController", Points = 5)]
    public async Task Order_GET_ShowsStoreName()
    {
        await RunTestAsync(async () =>
        {
            var product = await TestDatabaseHelper.GetFirst<Product>(_factory.Services);
            var response = await _client.GetAsync($"/Product/Order/{product.Id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            Assert.Contains("id=\"store-name\"", content);
        });
    }

    [LoggedFact(Category = "ProductController", Points = 5)]
    public async Task Order_GET_ShowsCustomerDropdown()
    {
        await RunTestAsync(async () =>
        {
            var product = await TestDatabaseHelper.GetFirst<Product>(_factory.Services);
            var response = await _client.GetAsync($"/Product/Order/{product.Id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            Assert.Contains("<select", content, StringComparison.OrdinalIgnoreCase);
        });
    }

    // ── Req 7b: Product/Order POST ─────────────────────────────

    [LoggedFact(Category = "ProductController", Points = 5)]
    public async Task Order_POST_SavesOrderAndRedirectsToProductDetails()
    {
        await RunTestAsync(async () =>
        {
            var product = await TestDatabaseHelper.GetFirst<Product>(_factory.Services);
            var customer = await TestDatabaseHelper.GetFirst<Customer>(_factory.Services);
            var initialCount = await TestDatabaseHelper.GetCount<Order>(_factory.Services);

            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("ProductId", product.Id.ToString()),
                new KeyValuePair<string, string>("CustomerId", customer.Id.ToString()),
            });

            var response = await _client.PostAsync("/Product/Order", form);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains($"/Product/Details/{product.Id}", response.Headers.Location?.ToString());
            Assert.Equal(initialCount + 1, await TestDatabaseHelper.GetCount<Order>(_factory.Services));

            // DeliveryDate must be null for new order
            var order = TestDatabaseHelper.GetById<Order>(_factory.Services,
                o => o.ProductId == product.Id && o.CustomerId == customer.Id && o.DeliveryDate == null);
            Assert.NotNull(order);
        });
    }

    // ── Req 6a: AddProduct redirects to Store/Details ─────────

    [LoggedFact(Category = "ProductController", Points = 5)]
    public async Task AddProduct_AfterCreate_RedirectsToStoreDetails()
    {
        await RunTestAsync(async () =>
        {
            var store = await TestDatabaseHelper.GetFirst<Store>(_factory.Services);
            var initialCount = await TestDatabaseHelper.GetCount<Product>(_factory.Services);
            var getResponse = await _client.GetAsync("/Product/Create");
            var token = await getResponse.GetAntiForgeryTokenAsync();

            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Name", "Нов Производ"),
                new KeyValuePair<string, string>("Description", "Опис на производот"),
                new KeyValuePair<string, string>("Price", "999.99"),
                new KeyValuePair<string, string>("ImageUrl", "https://example.com/new.jpg"),
                new KeyValuePair<string, string>("StoreId", store.Id.ToString()),
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
            });

            var response = await _client.PostAsync("/Product/Create", form);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains($"/Store/Details/{store.Id}", response.Headers.Location?.ToString());
            Assert.Equal(initialCount + 1, await TestDatabaseHelper.GetCount<Product>(_factory.Services));
        });
    }

    // ── CRUD: Delete ───────────────────────────────────────────

    [LoggedFact(Category = "ProductController", Points = 1)]
    public async Task Delete_ValidProduct_RemovesAndRedirects()
    {
        await RunTestAsync(async () =>
        {
            var initialCount = await TestDatabaseHelper.GetCount<Product>(_factory.Services);
            var product = await TestDatabaseHelper.GetFirst<Product>(_factory.Services);
            var getResponse = await _client.GetAsync($"/Product/Delete/{product.Id}");
            var token = await getResponse.GetAntiForgeryTokenAsync();

            var response = await _client.PostAsync($"/Product/Delete/{product.Id}",
                new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("__RequestVerificationToken", token) }));

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal(initialCount - 1, await TestDatabaseHelper.GetCount<Product>(_factory.Services));
        });
    }

    // ── Helpers ────────────────────────────────────────────────

    private async Task<Product> GetProductWithActiveOrdersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Products
            .Where(p => p.Orders.Any(o => o.DeliveryDate == null))
            .FirstAsync();
    }

    public async Task InitializeAsync() => await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services);
    public async Task DisposeAsync() => await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services);
}