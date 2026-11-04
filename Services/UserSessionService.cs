using Microsoft.AspNetCore.Http;
using Proyecto.Data;
using Proyecto.Models;

namespace Proyecto.Services
{
    public class UserSessionService : IUserSessionService
    {
        private const string SessionKeyUserId = "CurrentUserId";
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AppDbContext _context;

        public UserSessionService(IHttpContextAccessor httpContextAccessor, AppDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        private ISession Session => _httpContextAccessor.HttpContext?.Session
            ?? throw new InvalidOperationException("Session is not available.");

        public int? GetCurrentUserId()
        {
            return Session.GetInt32(SessionKeyUserId);
        }

        public void SetCurrentUserId(int userId)
        {
            Session.SetInt32(SessionKeyUserId, userId);
        }

        public void Clear()
        {
            Session.Remove(SessionKeyUserId);
        }

        public bool IsLoggedIn => GetCurrentUserId().HasValue;

        public bool IsAdmin
        {
            get
            {
                var currentUser = GetCurrentUser();
                return currentUser?.Activo == true && currentUser.EsAdministrador;
            }
        }

        public bool IsEntrepreneur
        {
            get
            {
                var currentUser = GetCurrentUser();
                return currentUser?.Activo == true && currentUser.EsEmprendedor && !currentUser.EsAdministrador;
            }
        }

        public bool CanShop
        {
            get
            {
                var currentUser = GetCurrentUser();
                return currentUser?.Activo == true && currentUser.EsCliente;
            }
        }

        public ApplicationUser? GetCurrentUser()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return null;
            }

            // Users es el DbSet de Identity (AspNetUsers)
            return _context.Users.Find(userId.Value);
        }
    }
}
