using Microsoft.AspNetCore.Mvc;
using SimuladorEmergencias.Services;

namespace SimuladorEmergencias.Controllers
{
    public class DashboardController : Controller
    {
        private readonly DashboardService _dashboardService;
        private readonly SesionSimulacionService _sesion;


        public DashboardController(DashboardService dashboardService, SesionSimulacionService sesion)
        {
            _dashboardService = dashboardService;
            _sesion = sesion;
        }

        public IActionResult Index()
        {
            var modelo = _dashboardService.GenerarDashboard(
                _sesion.Pacientes,
                _sesion.Reportes
            );

            return View(modelo);
        }
    }
}