using Microsoft.EntityFrameworkCore;
using online_shop_IT.Models;

namespace online_shop_IT.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    
}