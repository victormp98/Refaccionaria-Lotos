namespace RefaccionariaWeb.Models.ViewModels
{
    public class UsuarioViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string Rol { get; set; }
        public bool EstaBloqueado { get; set; } // Se basará en si tiene Lockout activo
    }
}
