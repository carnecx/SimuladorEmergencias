using SimuladorEmergencias.Models;

namespace SimuladorEmergencias.Services
{
    /// <summary>
    /// Servicio encargado de ejecutar la planificación Multinivel por Colas (MLQ).
    /// Divide a los pacientes en colas según su tipo (Rojo, Amarillo, Embarazada,
    /// Verde, Cita, Seguimiento) y ejecuta, dentro de cada cola, el algoritmo de
    /// planificación que el usuario haya configurado (FIFO, SJF o Round Robin).
    /// 
    /// Como resultado devuelve:
    ///   - La lista de franjas de ejecución (Gantt) para graficar el diagrama de Gantt.
    ///   - La lista de reportes de desempeño (tiempo promedio de espera, retorno y
    ///     utilización de CPU) por cada cola/algoritmo.
    /// </summary>
    public class MlqSimuladorService
    {
        /// <summary>
        /// Punto de entrada del simulador MLQ.
        /// Recorre las colas en orden de prioridad y ejecuta el algoritmo
        /// configurado para cada una.
        /// </summary>
        /// <param name="pacientes">Lista completa de pacientes a simular (todas las colas mezcladas).</param>
        /// <param name="configuraciones">Configuración de cada cola: tipo, algoritmo y quantum (si aplica).</param>
        /// <returns>
        /// Una tupla con:
        ///   - gantt: todas las franjas de ejecución generadas por todas las colas.
        ///   - reportes: un reporte de desempeño por cada cola que tuvo pacientes.
        /// </returns>
        public (List<GanttSlotViewModel> gantt, List<ReporteAlgoritmoViewModel> reportes)
            Simular(List<PacienteViewModel> pacientes,
                    List<ConfiguracionColaViewModel> configuraciones)
        {
            var gantt = new List<GanttSlotViewModel>();
            var reportes = new List<ReporteAlgoritmoViewModel>();

            // Las colas se atienden en orden de prioridad (Rojo = prioridad 1 primero,
            // luego Amarillo, Embarazada, Verde, Cita y Seguimiento al final).
            var colasOrdenadas = configuraciones.OrderBy(c => c.Prioridad).ToList();

            // Marca el instante en que terminó de atenderse la última cola procesada.
            int tiempoActual = 0;

            foreach (var cola in colasOrdenadas)
            {
                // Se filtran únicamente los pacientes que pertenecen a esta cola/tipo.
                var pacientesCola = pacientes
                    .Where(p => p.IdTipo == cola.IdTipo)
                    .OrderBy(p => p.TiempoLlegada)
                    .ToList();

                // Si no hay pacientes para este tipo, se salta la cola.
                if (!pacientesCola.Any()) continue;

                // El tiempo de inicio de la cola es el mayor entre:
                //   - el momento en que terminó de atenderse la cola anterior
                //   - el momento en que llega el primer paciente de esta cola
                // Esto evita que una cola de menor prioridad se adelante a una de mayor prioridad.
                int tiempoBase = Math.Max(tiempoActual,
                                          pacientesCola.Min(p => p.TiempoLlegada));

                List<GanttSlotViewModel> slotsCola;

                // Se delega la ejecución en el algoritmo que el usuario eligió para esta cola.
                switch (cola.Algoritmo.ToUpper())
                {
                    case "SJF":
                        slotsCola = EjecutarSJF(pacientesCola, tiempoBase, cola);
                        break;
                    case "RR":
                        slotsCola = EjecutarRR(pacientesCola, tiempoBase, cola);
                        break;
                    default:
                        // Por defecto, si no es SJF ni RR, se usa FIFO.
                        slotsCola = EjecutarFIFO(pacientesCola, tiempoBase, cola);
                        break;
                }

                // Se acumulan las franjas de esta cola al Gantt general.
                gantt.AddRange(slotsCola);

                // Se actualiza el reloj global al momento en que terminó la última franja de esta cola.
                if (slotsCola.Any())
                    tiempoActual = slotsCola.Max(s => s.TiempoFin);

                // Se calculan las métricas de desempeño de esta cola y se agregan al reporte general.
                var reporte = CalcularReporte(pacientesCola, slotsCola, cola, tiempoActual);
                reportes.Add(reporte);
            }

            return (gantt, reportes);
        }

        /// <summary>
        /// Algoritmo FIFO (First In, First Out / Primero en llegar, primero en ser atendido).
        /// Los pacientes se atienden en estricto orden de llegada, sin interrupciones:
        /// cada uno se atiende completo antes de pasar al siguiente.
        /// </summary>
        /// <param name="pacientes">Pacientes de esta cola.</param>
        /// <param name="tiempoBase">Instante en el que esta cola puede empezar a atender.</param>
        /// <param name="cola">Configuración de la cola (nombre, color, prioridad).</param>
        /// <returns>Lista de franjas de Gantt generadas por esta cola.</returns>
        private List<GanttSlotViewModel> EjecutarFIFO(
            List<PacienteViewModel> pacientes, int tiempoBase,
            ConfiguracionColaViewModel cola)
        {
            var slots = new List<GanttSlotViewModel>();
            int tiempo = tiempoBase;

            // Se asegura el orden de llegada antes de procesar.
            var ordenados = pacientes.OrderBy(p => p.TiempoLlegada).ToList();

            foreach (var p in ordenados)
            {
                // Si el paciente aún no ha llegado cuando el CPU está libre,
                // el reloj avanza hasta su llegada.
                if (tiempo < p.TiempoLlegada) tiempo = p.TiempoLlegada;

                int fin = tiempo + p.TiempoRafaga;

                // Se registra la franja completa de atención de este paciente.
                slots.Add(new GanttSlotViewModel
                {
                    IdPaciente = p.IdPaciente,
                    NombrePaciente = p.Nombre,
                    NombreTipo = cola.NombreTipo,
                    ColorHex = cola.ColorHex,
                    TiempoInicio = tiempo,
                    TiempoFin = fin,
                    Algoritmo = "FIFO",
                    ColaPrioridad = cola.Prioridad
                });

                // Se actualizan los tiempos resultantes del paciente.
                p.TiempoInicio = tiempo;
                p.TiempoFin = fin;
                p.TiempoEspera = tiempo - p.TiempoLlegada;     // Espera = inicio - llegada
                p.TiempoRetorno = fin - p.TiempoLlegada;        // Retorno = fin - llegada
                p.Estado = "finalizado";

                // El reloj avanza hasta que termina este paciente.
                tiempo = fin;
            }

            return slots;
        }

        /// <summary>
        /// Algoritmo SJF (Shortest Job First / El de menor ráfaga primero), no apropiativo.
        /// En cada paso se elige, entre los pacientes que ya han llegado, el que tenga
        /// la ráfaga (tiempo de atención) más corta, y se atiende por completo.
        /// </summary>
        /// <param name="pacientes">Pacientes de esta cola.</param>
        /// <param name="tiempoBase">Instante en el que esta cola puede empezar a atender.</param>
        /// <param name="cola">Configuración de la cola (nombre, color, prioridad).</param>
        /// <returns>Lista de franjas de Gantt generadas por esta cola.</returns>
        private List<GanttSlotViewModel> EjecutarSJF(
            List<PacienteViewModel> pacientes, int tiempoBase,
            ConfiguracionColaViewModel cola)
        {
            var slots = new List<GanttSlotViewModel>();
            var pendientes = pacientes.ToList(); // pacientes que todavía no han sido atendidos
            int tiempo = tiempoBase;

            while (pendientes.Any())
            {
                // Solo se consideran los pacientes que ya llegaron al sistema (TiempoLlegada <= tiempo),
                // y entre ellos se ordenan por ráfaga ascendente (el más corto primero).
                var disponibles = pendientes
                    .Where(p => p.TiempoLlegada <= tiempo)
                    .OrderBy(p => p.TiempoRafaga)
                    .ToList();

                // Si todavía no ha llegado nadie, se adelanta el reloj hasta la próxima llegada
                // para evitar quedar en un ciclo infinito esperando.
                if (!disponibles.Any())
                {
                    tiempo = pendientes.Min(p => p.TiempoLlegada);
                    continue;
                }

                // Se selecciona y remueve de pendientes al paciente con menor ráfaga.
                var p = disponibles.First();
                pendientes.Remove(p);

                int fin = tiempo + p.TiempoRafaga;

                slots.Add(new GanttSlotViewModel
                {
                    IdPaciente = p.IdPaciente,
                    NombrePaciente = p.Nombre,
                    NombreTipo = cola.NombreTipo,
                    ColorHex = cola.ColorHex,
                    TiempoInicio = tiempo,
                    TiempoFin = fin,
                    Algoritmo = "SJF",
                    ColaPrioridad = cola.Prioridad
                });

                p.TiempoInicio = tiempo;
                p.TiempoFin = fin;
                p.TiempoEspera = tiempo - p.TiempoLlegada;
                p.TiempoRetorno = fin - p.TiempoLlegada;
                p.Estado = "finalizado";

                tiempo = fin;
            }

            return slots;
        }

        /// <summary>
        /// Algoritmo Round Robin (RR), apropiativo, con quantum configurable.
        /// Cada paciente se atiende solo durante un "quantum" de tiempo; si su ráfaga
        /// no se completa en ese lapso, vuelve al final de la cola para esperar su
        /// siguiente turno. Así se reparte el CPU equitativamente entre todos.
        /// </summary>
        /// <param name="pacientes">Pacientes de esta cola.</param>
        /// <param name="tiempoBase">Instante en el que esta cola puede empezar a atender.</param>
        /// <param name="cola">Configuración de la cola, incluye el Quantum elegido por el usuario.</param>
        /// <returns>Lista de franjas de Gantt generadas por esta cola (puede haber varias franjas por paciente).</returns>
        private List<GanttSlotViewModel> EjecutarRR(
            List<PacienteViewModel> pacientes, int tiempoBase,
            ConfiguracionColaViewModel cola)
        {
            var slots = new List<GanttSlotViewModel>();

            // Si no se configuró un quantum válido, se usa 2 como valor por defecto.
            int quantum = cola.Quantum > 0 ? cola.Quantum : 2;

            // Se envuelve cada paciente en un objeto RRTrabajo para llevar control
            // de su tiempo restante y del primer instante en que fue atendido
            // (necesario para calcular correctamente el tiempo de espera real).
            var trabajos = pacientes
                .OrderBy(p => p.TiempoLlegada)
                .Select(p => new RRTrabajo
                {
                    Paciente = p,
                    Restante = p.TiempoRafaga,
                    PrimerInicio = null
                })
                .ToList();

            // Cola FIFO interna de trabajos listos para ejecutarse.
            var queue = new Queue<RRTrabajo>();
            int tiempo = tiempoBase;
            int idx = 0;

            // Se ingresan a la cola los trabajos que ya llegaron en el tiempo inicial.
            while (idx < trabajos.Count && trabajos[idx].Paciente.TiempoLlegada <= tiempo)
                queue.Enqueue(trabajos[idx++]);

            // El ciclo continúa mientras existan trabajos en la cola o pendientes por llegar.
            while (queue.Any() || idx < trabajos.Count)
            {
                // Si no hay nadie listo en este instante, se adelanta el reloj
                // hasta la siguiente llegada y se reincorporan los que correspondan.
                if (!queue.Any())
                {
                    tiempo = trabajos[idx].Paciente.TiempoLlegada;
                    while (idx < trabajos.Count && trabajos[idx].Paciente.TiempoLlegada <= tiempo)
                        queue.Enqueue(trabajos[idx++]);
                }

                // Se toma el siguiente trabajo de la cola (el que lleva más tiempo esperando).
                var trabajo = queue.Dequeue();
                var p = trabajo.Paciente;

                // Se registra la primera vez que este paciente es atendido (para el tiempo de espera real).
                if (trabajo.PrimerInicio == null)
                    trabajo.PrimerInicio = tiempo;

                // Se ejecuta como máximo "quantum" unidades de tiempo,
                // o menos si lo que le queda es menor al quantum.
                int ejecutar = Math.Min(quantum, trabajo.Restante);
                int fin = tiempo + ejecutar;

                // Se registra esta franja parcial de atención en el Gantt.
                slots.Add(new GanttSlotViewModel
                {
                    IdPaciente = p.IdPaciente,
                    NombrePaciente = p.Nombre,
                    NombreTipo = cola.NombreTipo,
                    ColorHex = cola.ColorHex,
                    TiempoInicio = tiempo,
                    TiempoFin = fin,
                    Algoritmo = "RR",
                    ColaPrioridad = cola.Prioridad
                });

                // Se descuenta el tiempo ejecutado y se avanza el reloj.
                trabajo.Restante -= ejecutar;
                tiempo = fin;

                // Se incorporan a la cola los pacientes que hayan llegado durante este quantum.
                while (idx < trabajos.Count && trabajos[idx].Paciente.TiempoLlegada <= tiempo)
                    queue.Enqueue(trabajos[idx++]);

                if (trabajo.Restante > 0)
                {
                    // Si todavía le falta ráfaga por ejecutar, vuelve al final de la cola.
                    queue.Enqueue(trabajo);
                }
                else
                {
                    // Si ya terminó, se calculan sus tiempos finales.
                    p.TiempoInicio = trabajo.PrimerInicio;
                    p.TiempoFin = fin;
                    p.TiempoEspera = (trabajo.PrimerInicio ?? 0) - p.TiempoLlegada;
                    p.TiempoRetorno = fin - p.TiempoLlegada;
                    p.Estado = "finalizado";
                }
            }

            return slots;
        }

        /// <summary>
        /// Clase auxiliar privada utilizada únicamente dentro de <see cref="EjecutarRR"/>
        /// para llevar el control del tiempo restante de cada paciente y del primer
        /// instante en que fue atendido mientras dura la planificación Round Robin.
        /// </summary>
        private class RRTrabajo
        {
            /// <summary>Paciente original al que pertenece este trabajo.</summary>
            public PacienteViewModel Paciente { get; set; } = null!;

            /// <summary>Tiempo de ráfaga que todavía le falta ejecutar al paciente.</summary>
            public int Restante { get; set; }

            /// <summary>
            /// Primer instante en que el paciente fue atendido por el CPU.
            /// Se usa para calcular su tiempo de espera real (no solo el de la última franja).
            /// </summary>
            public int? PrimerInicio { get; set; }
        }

        /// <summary>
        /// Calcula las métricas de desempeño de una cola una vez finalizada su ejecución:
        /// tiempo promedio de espera, tiempo promedio de retorno y porcentaje de
        /// utilización del CPU dentro del rango de tiempo que ocupó esa cola.
        /// </summary>
        /// <param name="pacientes">Pacientes que pertenecen a esta cola.</param>
        /// <param name="slots">Franjas de Gantt generadas por esta cola.</param>
        /// <param name="cola">Configuración de la cola (algoritmo, nombre, color, prioridad).</param>
        /// <param name="tiempoTotal">Tiempo total acumulado hasta el momento (no usado directamente en el cálculo, se conserva por compatibilidad de firma).</param>
        /// <returns>Un <see cref="ReporteAlgoritmoViewModel"/> con las métricas calculadas.</returns>
        private ReporteAlgoritmoViewModel CalcularReporte(
            List<PacienteViewModel> pacientes,
            List<GanttSlotViewModel> slots,
            ConfiguracionColaViewModel cola,
            int tiempoTotal)
        {
            // Solo se toman en cuenta los pacientes que efectivamente terminaron de ser atendidos.
            var finalizados = pacientes.Where(p => p.Estado == "finalizado").ToList();

            // Si no hay pacientes finalizados, se devuelve un reporte vacío para esta cola.
            if (!finalizados.Any())
                return new ReporteAlgoritmoViewModel { Algoritmo = cola.Algoritmo };

            // Tiempo promedio de espera de los pacientes finalizados.
            decimal avgEspera = finalizados.Any(p => p.TiempoEspera.HasValue)
                ? (decimal)finalizados.Where(p => p.TiempoEspera.HasValue)
                                      .Average(p => p.TiempoEspera!.Value)
                : 0;

            // Tiempo promedio de retorno de los pacientes finalizados.
            decimal avgRetorno = finalizados.Any(p => p.TiempoRetorno.HasValue)
                ? (decimal)finalizados.Where(p => p.TiempoRetorno.HasValue)
                                      .Average(p => p.TiempoRetorno!.Value)
                : 0;

            // Tiempo total que el CPU estuvo activo atendiendo pacientes de esta cola
            // (suma de la duración de todas sus franjas de Gantt).
            int tiempoActivo = slots.Sum(s => s.TiempoFin - s.TiempoInicio);

            // Duración total que abarcó esta cola, desde que empezó su primera franja
            // hasta que terminó la última.
            int duracionCola = slots.Any()
                ? slots.Max(s => s.TiempoFin) - slots.Min(s => s.TiempoInicio)
                : 1;

            // Porcentaje de utilización del CPU = tiempo activo / duración total * 100.
            decimal utilizacion = duracionCola > 0
                ? Math.Round((decimal)tiempoActivo / duracionCola * 100, 2)
                : 100;

            return new ReporteAlgoritmoViewModel
            {
                Algoritmo = cola.Algoritmo,
                ColaPrioridad = cola.Prioridad,
                NombreCola = cola.NombreTipo,
                ColorHex = cola.ColorHex,
                AvgEspera = Math.Round(avgEspera, 2),
                AvgRetorno = Math.Round(avgRetorno, 2),
                UtilizacionCpu = utilizacion,
                TotalProcesos = finalizados.Count
            };
        }
    }
}