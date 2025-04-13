using MacroSocietyAPI.EmailServies;
using MacroSocietyAPI.Models;
using MacroSocietyAPI.Randoms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ���������� DbContext � ������������� �� appsettings.json
builder.Services.AddDbContext<MacroSocietyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

/*builder.Services.AddDbContext<MacroSocietyDbContext>();*/

// ������������ EmailService � ������ �����������
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<CreateVerificationCode>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); 
}

app.UseRouting();
app.UseHttpsRedirection();// �������� �������� �� HTTPS
app.UseAuthorization();
app.MapControllers();

app.Run();

