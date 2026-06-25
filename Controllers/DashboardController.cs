using Microsoft.AspNetCore.Mvc;
using SimuladorEmergencias.Services;

namespace SimuladorEmergencias.Controllers
{
    public class DashboardController : Controller
    {
        private readonly DashboardService _dashboardService;
        private readonly SimulacionDbService _db;

        public DashboardController(DashboardService dashboardService, SimulacionDbService db)
        {
            _dashboardService = dashboardService;
            _db = db;
        }

        public IActionResult Index()
        {
            var idSesion = _db.ObtenerUltimaSesion();

            if (idSesion == 0)
            {
                var modeloVacio = _dashboardService.GenerarDashboard(new(), new());
                return View(modeloVacio);
            }

            var pacientes = _db.ObtenerPacientesPorSesion(idSesion);
            var reportes = _db.ObtenerReportesPorSesion(idSesion);

            var modelo = _dashboardService.GenerarDashboard(pacientes, reportes);

            return View(modelo);
        }
    }
}