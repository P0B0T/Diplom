using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RepairServiceWeb.DAL;
using RepairServiceWeb.Domain.Entity;
using System.Text.Json.Serialization;

namespace RepairServiceWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ��������� ������ ����������� � ���� ������
            var connection = builder.Configuration.GetConnectionString("MSSQLSERVER");

            // ���������� ��������� ���� ������ � �������
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(connection));

            builder.Services.AddControllersWithViews().AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

            // ������������� ������������ � ��������
            builder.Services.InitializeRepositories();
            builder.Services.InitializeServices();

            // ��������� ��������������
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = AutorizationOptions.ISSUER,
                        ValidAudience = AutorizationOptions.AUDIENCE,

                        IssuerSigningKey = AutorizationOptions.GetSymmetricSecurityKey(),
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });

            // ���������� �����������
            builder.Services.AddAuthorization();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseRouting();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Autorization}/{action=Index}/{id?}");

            using (var serviceScope = app.Services.CreateScope())
            {
                // ��������� ��������� ���� ������
                var context = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // ��������, ���� �� ���� ������ ������ ��� �������
                if (context.Database.EnsureCreated())
                {
                    // ���� ���� ������ ������ ��� �������:
                    // ��������� ���� ��������������;
                    context.Roles.Add(new Role { Role1 = "Administrator" });
                    context.SaveChanges();

                    // �������� ID ���� ��������������;
                    var adminRoleId = context.Roles.FirstOrDefault(r => r.Role1.ToLower().Contains("admin") || r.Role1.ToLower().Contains("�����"))?.Id;

                    // ��������� ���������� � ����� ��������������.
                    context.Staff.Add(new Staff
                    {
                        Name = "Admin",
                        Surname = "Admin",
                        Post = "Admin",
                        Salary = 0,
                        DateOfEmployment = default,
                        RoleId = adminRoleId.Value,
                        Login = "Admin",
                        Password = "admin"
                    });
                    context.SaveChanges();
                }
            }

            app.Run();
        }
    }
}
