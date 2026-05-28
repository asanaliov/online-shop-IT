using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using online_shop_IT.Data;
using online_shop_IT.Models;

namespace OnlineShopTests.Utils;

public static class TestDatabaseHelper
{
    public static void SeedDatabase(ApplicationDbContext context)
    {
        var store1 = new Store { Name = "Технологија ДООЕЛ", Address = "ул. Македонија 1", City = "Скопје", Country = "Македонија", Rating = 8.5 };
        var store2 = new Store { Name = "Мода и Стил", Address = "бул. Партизански 6", City = "Битола", Country = "Македонија", Rating = 9.0 };
        var store3 = new Store { Name = "Спорт Центар", Address = "ул. Руѓер Бошковиќ 16", City = "Скопје", Country = "Македонија", Rating = 7.8 };
        context.Stores.AddRange(store1, store2, store3);
        context.SaveChanges();

        var product1 = new Product { Name = "Лаптоп Делови", Description = "Квалитетни компјутерски делови", Price = 2999.99m, ImageUrl = "https://example.com/products/laptop.jpg", StoreId = store1.Id };
        var product2 = new Product { Name = "Зимска Јакна", Description = "Топла зимска јакна", Price = 1500.00m, ImageUrl = "https://example.com/products/jakna.jpg", StoreId = store2.Id };
        var product3 = new Product { Name = "Фудбалска Топка", Description = "Официјална фудбалска топка", Price = 800.00m, ImageUrl = "https://example.com/products/topka.jpg", StoreId = store1.Id };
        context.Products.AddRange(product1, product2, product3);
        context.SaveChanges();

        var customer1 = new Customer { FirstName = "Александар", LastName = "Петровски", Email = "aleksandar@example.com", PhoneNumber = "070123456", MembershipDate = new DateTime(2023, 1, 15) };
        var customer2 = new Customer { FirstName = "Марија", LastName = "Стојановска", Email = "marija@example.com", PhoneNumber = "071234567", MembershipDate = new DateTime(2023, 3, 20) };
        var customer3 = new Customer { FirstName = "Никола", LastName = "Димитровски", Email = "nikola@example.com", PhoneNumber = "072345678", MembershipDate = new DateTime(2024, 5, 10) };
        context.Customers.AddRange(customer1, customer2, customer3);
        context.SaveChanges();

        // Active order (DeliveryDate == null)
        var order1 = new Order { ProductId = product1.Id, CustomerId = customer1.Id, OrderDate = DateTime.Now.AddDays(-10), DeliveryDate = null };
        // Delivered order
        var order2 = new Order { ProductId = product2.Id, CustomerId = customer1.Id, OrderDate = DateTime.Now.AddDays(-30), DeliveryDate = DateTime.Now.AddDays(-5) };
        // Another active order
        var order3 = new Order { ProductId = product1.Id, CustomerId = customer2.Id, OrderDate = DateTime.Now.AddDays(-3), DeliveryDate = null };
        context.Orders.AddRange(order1, order2, order3);
        context.SaveChanges();
    }

    public static async Task ResetDatabaseAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        SeedDatabase(context);
    }

    public static async Task<int> GetCount<T>(IServiceProvider serviceProvider) where T : class
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Set<T>().CountAsync();
    }

    public static async Task<T> GetFirst<T>(IServiceProvider serviceProvider) where T : class
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Set<T>().FirstAsync();
    }

    public static T? GetById<T>(IServiceProvider serviceProvider, Func<T, bool> predicate) where T : class
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return context.Set<T>().Where(predicate).FirstOrDefault();
    }

    public static async Task<List<T>> GetAllWhere<T>(
        IServiceProvider services,
        Expression<Func<T, bool>> predicate) where T : class
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Set<T>().Where(predicate).ToListAsync();
    }
}