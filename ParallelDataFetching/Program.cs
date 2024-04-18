using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection.Emit;


namespace ParallelDataFetching
{
    internal class Program
    {
        public class Employee
        {
            public int Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string PhoneNumber { get; set; }
        }
        public class MyDbContext : DbContext
        {
            public DbSet<Employee> Employee { get; set; }

            public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<Employee>().ToTable("Employee");
            }
        }
        public interface IRepository<T> where T : class
        {
            Task<List<T>> GetAllAsync();
        }
        public class Repository<T> : IRepository<T> where T : class
        {
            private readonly MyDbContext _dbContext;

            public Repository(MyDbContext dbContext)
            {
                _dbContext = dbContext;
            }

            public async Task<List<T>> GetAllAsync()
            {
                return await _dbContext.Set<T>().ToListAsync();
            }

        }

        public class DataFetcherService
        {
            private readonly IRepository<Employee> _repository;

            public DataFetcherService(IRepository<Employee> repository)
            {
                _repository = repository;
            }

            public async Task<List<Employee>> FetchDataFromDatabaseAsync()
            {
                return await _repository.GetAllAsync();
            }
            public async Task<List<Employee>> GetRandomEntitiesAsync(int count)
            {
                var randomEntities = await _repository.GetAllAsync();
                var randomSelection = randomEntities.OrderBy(x => Guid.NewGuid()).Take(count).ToList();
                return randomSelection;
            }
            public async Task<List<Employee>> GetEmployeesWithPhoneNumberEndingAsync(int number)
            {
                var employees = await _repository.GetAllAsync();
                var filteredEmployees = employees.Where(e => e.PhoneNumber.EndsWith(number.ToString())).ToList();
                return filteredEmployees;
            }
            public async Task<List<Employee>> GetRandomEmployeesWithPhoneNumberEndingAsync(int number, int count)
            {
                var employees = await _repository.GetAllAsync();
                var filteredEmployees = employees.Where(e => e.PhoneNumber.EndsWith(number.ToString())).ToList();
                var randomSelection = filteredEmployees.OrderBy(x => Guid.NewGuid()).Take(count).ToList();
                return randomSelection;
            }
        }
        static string connectionString = "Data Source=SEFADEDE\\SQLEXPRESS;Initial Catalog=ekonobi; User ID=sa;Password=sd147852369; TrustServerCertificate=true";
        static async Task Main(string[] args)
        {
            var options = new DbContextOptionsBuilder<MyDbContext>()
            .UseSqlServer(connectionString)
            .Options;

            using (var dbContext = new MyDbContext(options))
            {
                await dbContext.Database.EnsureCreatedAsync();
            }

            var serviceProvider = new ServiceCollection()
                .AddScoped<IRepository<Employee>, Repository<Employee>>()
                .AddSingleton<DataFetcherService>()
                .AddDbContext<MyDbContext>(options => options.UseSqlServer(connectionString))
                .BuildServiceProvider();

            using (var scope = serviceProvider.CreateScope())
            {
                var dataFetcher = scope.ServiceProvider.GetRequiredService<DataFetcherService>();

                while (true)
                {
                    //var data = await dataFetcher.FetchDataFromDatabaseAsync();
                    var randomEntities = await dataFetcher.GetRandomEmployeesWithPhoneNumberEndingAsync(1, 10);
                    foreach (var entity in randomEntities)
                    {
                        Console.WriteLine($"Id: {entity.Id}, Name: {entity.FirstName}, Surname: {entity.LastName}, Phone: {entity.PhoneNumber}");
                    }
                    Console.WriteLine("--------------------------------------");
                    await Task.Delay(2000);
                }
            }
        }
    }
}