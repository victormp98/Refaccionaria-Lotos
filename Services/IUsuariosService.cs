using RefaccionariaWeb.Models.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RefaccionariaWeb.Services
{
    public interface IUsuariosService
    {
        Task<List<UsuarioViewModel>> ObtenerTodos(string tipo, string buscar = null, bool soloBloqueados = false);
        Task<EditarUsuarioViewModel?> ObtenerParaEditar(string id);
        Task<bool> Crear(CrearUsuarioViewModel model);
        Task<bool> Editar(EditarUsuarioViewModel model);
        Task<bool> Bloquear(string id); // El 'Soft Delete' de 100 años
        Task<bool> Desbloquear(string id);
        Task<List<string>> ObtenerRoles();
    }
}
