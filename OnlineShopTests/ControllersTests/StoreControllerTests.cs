using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using online_shop_IT.Models;
using OnlineShopTests.Utils;

namespace OnlineShopTests.ControllersTests;

[Collection("Test Suite")]
public class StoreControllerTests : LoggedTestBase, IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public StoreControllerTests(WebApplicationFactory<Program> factory, GlobalTestFixture fixture) : base(fixture)
    {
        _factory = factory.WithTestDatabase();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });
        _client.Timeout = TimeSpan.FromSeconds(10);
    }

    // ── Req 1: CRUD ──────────────────────────────────────────────

    [LoggedFact(Category = "StoreController", Points = 1)]
    public async Task Index_ReturnsAllStores()
    {
        await RunTestAsync(async () =>
        {
            var response = await _client.GetAsync("/Store");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Технологија ДООЕЛ", content);
            Assert.Contains("Мода и Стил", content);
        });
    }

    // ── Req 1b: Store name is a link to Details ────────────────

    [LoggedFact(Category = "StoreController", Points = 5)]
    public async Task Index_StoreNameIsLinkToDetails()
    {
        await RunTestAsync(async () =>
        {
            var store = await TestDatabaseHelper.GetFirst<Store>(_factory.Services);

            var response = await _client.GetAsync("/Store");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            Assert.Contains($"/Store/Details/{store.Id}", content);
            var linkIndex = content.IndexOf($"/Store/Details/{store.Id}");
            var nameIndex = content.IndexOf(store.Name, linkIndex);
            Assert.True(nameIndex > linkIndex,
                $"Store name '{store.Name}' should appear as a link to Details.");
        });
    }

    // ── Req 1c: Product count column ──────────────────────────

    [LoggedFact(Category = "StoreController", Points = 5)]
    public async Task Index_ShowsProductCountPerStore()
    {
        await RunTestAsync(async () =>
        {
            var response = await _client.GetAsync("/Store");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            // store1 (Технологија) has 2 products — page should show "2"
            Assert.Contains("2", content);
        });
    }

    // ── Req 4a: Filter by Name ─────────────────────────────────

    [LoggedFact(Category = "StoreController", Points = 5)]
    public async Task Index_FilterByName_ReturnsMatchingStore()
    {
        await RunTestAsync(async () =>
        {
            var response = await _client.GetAsync("/Store?name=Технологија");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            Assert.Contains("Технологија ДООЕЛ", content);
            Assert.DoesNotContain("Мода и Стил", content);
        });
    }

    // ── Req 4a: Filter by City ─────────────────────────────────

    [LoggedFact(Category = "StoreController", Points = 5)]
    public async Task Index_FilterByCity_ReturnsMatchingStores()
    {
        await RunTestAsync(async () =>
        {
            var response = await _client.GetAsync("/Store?city=Битола");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            Assert.Contains("Мода и Стил", content);
            Assert.DoesNotContain("Технологија ДООЕЛ", content);
        });
    }

    // ── Req 4b: Filter values persist ─────────────────────────

    [LoggedFact(Category = "StoreController", Points = 5)]
    public async Task Index_FilterValues_PersistInForm()
    {
        await RunTestAsync(async () =>
        {
            var response = await _client.GetAsync("/Store?name=Технологија&city=Скопје");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            Assert.Contains("value=\"Технологија\"", content);
        });
    }

    // ── Req 6a: Details has Add Product link ───────────────────

    [LoggedFact(Category = "StoreController", Points = 5)]
    public async Task Details_ShowsAddProductLink()
    {
        await RunTestAsync(async () =>
        {
            var store = await TestDatabaseHelper.GetFirst<Store>(_factory.Services);
            var response = await _client.GetAsync($"/Store/Details/{store.Id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            Assert.Contains("id=\"add-product\"", content);
            Assert.Contains("Додади производ", content);
        });
    }

    // ── Req 6b: Details shows products table ──────────────────

    [LoggedFact(Category = "StoreController", Points = 5)]
    public async Task Details_ShowsProductsTable()
    {
        await RunTestAsync(async () =>
        {
            var store = await TestDatabaseHelper.GetFirst<Store>(_factory.Services);
            var response = await _client.GetAsync($"/Store/Details/{store.Id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            Assert.Contains("id=\"products-table\"", content);
            Assert.Contains("Лаптоп Делови", content);
            Assert.Contains("details-btn", content);
            Assert.Contains("order-btn", content);
        });
    }

    // ── CRUD: Create ───────────────────────────────────────────

    [LoggedFact(Category = "StoreController", Points = 1)]
    public async Task Create_ValidStore_RedirectsToIndex()
    {
        await RunTestAsync(async () =>
        {
            var initialCount = await TestDatabaseHelper.GetCount<Store>(_factory.Services);
            var getResponse = await _client.GetAsync("/Store/Create");
            var token = await getResponse.GetAntiForgeryTokenAsync();

            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Name", "Тест Продавница"),
                new KeyValuePair<string, string>("Address", "ул. Тест 1"),
                new KeyValuePair<string, string>("City", "Тетово"),
                new KeyValuePair<string, string>("Country", "Македонија"),
                new KeyValuePair<string, string>("Rating", "7.5"),
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
            });

            var response = await _client.PostAsync("/Store/Create", form);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Store", response.Headers.Location?.ToString());
            Assert.Equal(initialCount + 1, await TestDatabaseHelper.GetCount<Store>(_factory.Services));
        });
    }

    [LoggedFact(Category = "StoreController", Points = 1)]
    public async Task Create_InvalidStore_ReturnsView()
    {
        await RunTestAsync(async () =>
        {
            var initialCount = await TestDatabaseHelper.GetCount<Store>(_factory.Services);
            var getResponse = await _client.GetAsync("/Store/Create");
            var token = await getResponse.GetAntiForgeryTokenAsync();

            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Name", ""), // Required — should fail
                new KeyValuePair<string, string>("City", "Тетово"),
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
            });

            var response = await _client.PostAsync("/Store/Create", form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(initialCount, await TestDatabaseHelper.GetCount<Store>(_factory.Services));
        });
    }

    // ── CRUD: Details ──────────────────────────────────────────

    [LoggedFact(Category = "StoreController", Points = 1)]
    public async Task Details_ValidId_ReturnsStore()
    {
        await RunTestAsync(async () =>
        {
            var store = await TestDatabaseHelper.GetFirst<Store>(_factory.Services);
            var response = await _client.GetAsync($"/Store/Details/{store.Id}");
            response.EnsureSuccessStatusCode();
            Assert.Contains(store.Name, await response.Content.ReadAsStringAsync());
        });
    }

    [LoggedFact(Category = "StoreController", Points = 1)]
    public async Task Details_InvalidId_ReturnsNotFound()
    {
        await RunTestAsync(async () =>
        {
            var response = await _client.GetAsync("/Store/Details/99999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        });
    }

    // ── CRUD: Edit ─────────────────────────────────────────────

    [LoggedFact(Category = "StoreController", Points = 1)]
    public async Task Edit_ValidStore_RedirectsToIndex()
    {
        await RunTestAsync(async () =>
        {
            var store = await TestDatabaseHelper.GetFirst<Store>(_factory.Services);
            var getResponse = await _client.GetAsync($"/Store/Edit/{store.Id}");
            var token = await getResponse.GetAntiForgeryTokenAsync();

            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Id", store.Id.ToString()),
                new KeyValuePair<string, string>("Name", store.Name + " ИЗМ"),
                new KeyValuePair<string, string>("Address", store.Address),
                new KeyValuePair<string, string>("City", store.City),
                new KeyValuePair<string, string>("Country", store.Country),
                new KeyValuePair<string, string>("Rating", store.Rating.ToString()),
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
            });

            var response = await _client.PostAsync($"/Store/Edit/{store.Id}", form);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Store", response.Headers.Location?.ToString());
        });
    }

    // ── CRUD: Delete ───────────────────────────────────────────

    [LoggedFact(Category = "StoreController", Points = 1)]
    public async Task Delete_ValidStore_RemovesAndRedirects()
    {
        await RunTestAsync(async () =>
        {
            var initialCount = await TestDatabaseHelper.GetCount<Store>(_factory.Services);
            var store = await TestDatabaseHelper.GetFirst<Store>(_factory.Services);
            var getResponse = await _client.GetAsync($"/Store/Delete/{store.Id}");
            var token = await getResponse.GetAntiForgeryTokenAsync();

            var response = await _client.PostAsync($"/Store/Delete/{store.Id}",
                new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("__RequestVerificationToken", token) }));

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Store", response.Headers.Location?.ToString());
            Assert.Equal(initialCount - 1, await TestDatabaseHelper.GetCount<Store>(_factory.Services));
        });
    }

    [LoggedFact(Category = "StoreController", Points = 1)]
    public async Task Delete_InvalidId_ReturnsNotFound()
    {
        await RunTestAsync(async () =>
        {
            var response = await _client.GetAsync("/Store/Delete/99999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        });
    }

    public async Task InitializeAsync() => await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services);
    public async Task DisposeAsync() => await TestDatabaseHelper.ResetDatabaseAsync(_factory.Services);
}