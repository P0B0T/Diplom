using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RepairServiceWeb.DAL;
using RepairServiceWeb.Domain.Entity;
using RepairServiceWeb.Service.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RepairServiceWeb.Controllers
{
    public class AutorizationController : Controller
    {
        private readonly IRolesService _rolesService;
        private readonly ApplicationDbContext _context;

        public AutorizationController(ApplicationDbContext context, IRolesService rolesService)
        {
            _context = context;
            _rolesService = rolesService;
        }

        public IActionResult Index() => View();

        /// <summary>
        /// ����� ��� �����������
        /// </summary>
        /// <param name="login" - ����� ������������></param>
        /// <param name="password" - ������></param>
        /// <returns>������ ��� �����������</returns>
        public async Task<IActionResult> Enter(string login, string password)
        {
            var client = await _context.Clients.FirstOrDefaultAsync(x => EF.Functions.Collate(x.Login, "SQL_Latin1_General_CP1_CS_AS") == login && EF.Functions.Collate(x.Password, "SQL_Latin1_General_CP1_CS_AS") == password); // ����� ������� �� ������ � ������

            var staff = new Staff();

            // ���� ������ �� ������, ���� ����������
            if (client == null)
            {
                staff = await _context.Staff.FirstOrDefaultAsync(x => EF.Functions.Collate(x.Login, "SQL_Latin1_General_CP1_CS_AS") == login && EF.Functions.Collate(x.Password, "SQL_Latin1_General_CP1_CS_AS") == password);

                // ���� ��������� �� ������, �� ���������� BadRequest
                if (staff == null)
                    return BadRequest("�������� ����� ��� ������");
            }

            // ���������� ������ ��� �����������
            return Ok(new
            {
                auth_key = JWTCreate(staff, client), // JWT

                permissions = client != null ? client.RoleId : staff.RoleId, // �����

                userId = client != null ? client.Id : staff.Id, // Id ������������

                login = client != null ? client.Login : staff.Login, // �����

                password = client != null ? client.Password : staff.Password // ������
            });
        }

        /// <summary>
        /// ����� ��� �������� JWT
        /// </summary>
        /// <param name="staff" - ���������></param>
        /// <param name="clients" - ������></param>
        /// <returns>JWT</returns>
        private static string JWTCreate(Staff staff, Client? clients = null)
        {
            List<Claim>? claims;

            // �������� �����������
            if (clients != null)
                claims = [new Claim(ClaimTypes.Name, clients.Name)];
            else
                claims = [new Claim(ClaimTypes.Name, staff.Name)];

            // �������� JWT
            var jwt = new JwtSecurityToken(
                    issuer: AutorizationOptions.ISSUER,
                    audience: AutorizationOptions.AUDIENCE,
                    claims: claims,
                    expires: DateTime.UtcNow.Add(TimeSpan.FromHours(10)),
                    signingCredentials: new SigningCredentials(AutorizationOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        /// <summary>
        /// ����� ��� ��������� �������� ���� �� ����
        /// </summary>
        /// <param name="permissionId" - ��� ����></param>
        /// <returns>Json ����� � ��������� ����</returns>
        public async Task<JsonResult> GetRoleName(int? permissionId)
        {
            var response = await _rolesService.GetRoleName(permissionId);

            if (response.StatusCode == Domain.Enum.StatusCode.OK)
                return Json(new { success = true, data = response.Data });

            return Json(new { success = false });
        }
    }
}
