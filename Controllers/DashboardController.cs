using Microsoft.AspNetCore.Mvc;
using SimuladorEmergencias.Services;

namespace SimuladorEmergencias.Controllers
{
    public class DashboardController : Controller
    {
        private readonly DashboardService dashboardService = new DashboardService();
        private readonly DatosPruebaService datosPruebaService = new DatosPruebaService();

        public IActionResult Index()
        {
            var pacientes = datosPruebaService.ObtenerPacientes();
            var reportes = datosPruebaService.ObtenerReportes();

            var dashboard = dashboardService.GenerarDashboard(pacientes, reportes);

            return View(dashboard);
        }
    }
}