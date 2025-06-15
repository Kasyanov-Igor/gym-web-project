using gym_project_business_logic.Model;
using gym_project_business_logic.Services;
using gym_project_business_logic.Services.Interface;
using Model.Entities;
using gym_project_business_logic.Repositories;
using gym_project_business_logic.Repositories.Interface;
using gym_project;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// –егистраци€ зависимостей
builder.Services.AddScoped<MapperConfig, MapperConfig>();
builder.Services.AddScoped<ICoachService, CoachService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IWorkoutService, WorkoutService>();
builder.Services.AddScoped<ADatabaseConnection, SqliteConnection>();

// –егистраци€ оригинального репозитори€
builder.Services.AddScoped<Repository<Gym>>();
builder.Services.AddScoped<Repository<Admin>>();
builder.Services.AddScoped<Repository<Client>>();
builder.Services.AddScoped<Repository<Coach>>();
builder.Services.AddScoped<Repository<Workout>>();

builder.Services.AddScoped<IRepository<Workout>>(option =>
{
    // –азрешение оригинального репозитори€
    var repo = option.GetRequiredService<Repository<Workout>>();
    var adb = option.GetRequiredService<ADatabaseConnection>(); // –азрешение ADatabaseConnection

    // ¬озврат декоратора, оборачивающего оригинальный репозиторий
    return new TransactionalRepositoryDecorator<Workout>(adb, repo);
});

builder.Services.AddScoped<IRepository<Coach>>(option =>
{
    // –азрешение оригинального репозитори€
    var repo = option.GetRequiredService<Repository<Coach>>();
    var adb = option.GetRequiredService<ADatabaseConnection>(); // –азрешение ADatabaseConnection

    // ¬озврат декоратора, оборачивающего оригинальный репозиторий
    return new TransactionalRepositoryDecorator<Coach>(adb, repo);
});

builder.Services.AddScoped<IRepository<Gym>>(option =>
{
	// –азрешение оригинального репозитори€
	var repo = option.GetRequiredService<Repository<Gym>>();
	var adb = option.GetRequiredService<ADatabaseConnection>(); // –азрешение ADatabaseConnection

	// ¬озврат декоратора, оборачивающего оригинальный репозиторий
	return new TransactionalRepositoryDecorator<Gym>(adb, repo);
});

builder.Services.AddScoped<IRepository<Admin>>(option =>
{
	// –азрешение оригинального репозитори€
	var repo = option.GetRequiredService<Repository<Admin>>();
	var adb = option.GetRequiredService<ADatabaseConnection>(); // –азрешение ADatabaseConnection

	// ¬озврат декоратора, оборачивающего оригинальный репозиторий
	return new TransactionalRepositoryDecorator<Admin>(adb, repo);
});

builder.Services.AddScoped<IRepository<Client>>(option =>
{
	// –азрешение оригинального репозитори€
	var repo = option.GetRequiredService<Repository<Client>>();
	var adb = option.GetRequiredService<ADatabaseConnection>(); // –азрешение ADatabaseConnection

	// ¬озврат декоратора, оборачивающего оригинальный репозиторий
	return new TransactionalRepositoryDecorator<Client>(adb, repo);
});

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

app.UseCors(x => x
	.AllowAnyMethod()
	.AllowAnyHeader()
	.AllowCredentials()
	.SetIsOriginAllowed(origin => true));

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
