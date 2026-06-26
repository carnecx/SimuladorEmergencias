using Microsoft.AspNetCore.Mvc;
using SimuladorEmergencias.Services;

namespace SimuladorEmergencias.Controllers
{
    
    /// Controlador encargado de gestionar la visualización del Dashboard.
    /// Obtiene la información de la última simulación ejecutada y genera
    /// los indicadores que serán mostrados en la vista.
    
    public class DashboardController : Controller
    {
        // Servicio encargado de generar las métricas e indicadores del Dashboard.
        private readonly DashboardService _dashboardService;

        // Servicio encargado de consultar la información almacenada en la base de datos.
        private readonly SimulacionDbService _db;

       
        /// Inicializa una nueva instancia del controlador Dashboard.
        /// </summary>
        /// <param name="dashboardService">Servicio que genera el modelo del Dashboard.</param>
        /// <param name="db">Servicio de acceso a los datos de la simulación.</param>
        public DashboardController(DashboardService dashboardService, SimulacionDbService db)
        {
            _dashboardService = dashboardService;
            _db = db;
        }

       
        /// Carga la información correspondiente a la última simulación ejecutada
        /// y la envía a la vista del Dashboard.
       
        /// <returns>Vista del Dashboard con la información de la simulación.</returns>
        public IActionResult Index()
        {
            // Obtiene el identificador de la última sesión registrada.
            var idSesion = _db.ObtenerUltimaSesion();

            // Si no existe ninguna simulación registrada,
            // se genera un Dashboard vacío.
            if (idSesion == 0)
            {
                var modeloVacio = _dashboardService.GenerarDashboard(new(), new());
                return View(modeloVacio);
            }

            // Recupera los pacientes y reportes asociados a la última simulación.
            var pacientes = _db.ObtenerPacientesPorSesion(idSesion);
            var reportes = _db.ObtenerReportesPorSesion(idSesion);

            // Genera el modelo del Dashboard con los datos obtenidos.
            var modelo = _dashboardService.GenerarDashboard(pacientes, reportes);

            // Envía el modelo a la vista para su visualización.
            return View(modelo);
        }
    }
}