using Microsoft.EntityFrameworkCore;

namespace MRC.Agendia.Infrastructure;

public class AgendiaDbContext : DbContext
{
    public AgendiaDbContext(DbContextOptions options) : base(options)
    {

    }
}
