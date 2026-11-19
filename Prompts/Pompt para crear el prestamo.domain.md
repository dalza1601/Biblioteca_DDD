En src/Prestamos.Domain aplica el mismo estilo que en Catalogo.Domain (sin referenciarlo). Crea:
1.Excepciones.cs (ValidacionDominioException y ReglaNegocioException, namespace Prestamos.Domain) Se duplican a
proposito pero cada contexto es independiente. 2. CodigoLector: Value Object (sealed record, constructor crear, no vacios,trim, mayusculas). 3. EstadoPrestamo (Enum): Activo y Devuelto. 4. Prestamo: estidad raiz con id, LibroId(int), lector(CodigoLector), FechaInicio, FechaLimite(DateOnly), FechaDevolucion(DateOnly?), Estado.
Tiene el metodo Registrar(LibroId, lector, inicio y limite). Estado inicial al registrar es Activo;
LibroId no puede ser nulo o 0. FechaLimite debe ser posterior a la FechaInicio.

Se tiene el metodo Devolver(DateOnly fecha): si ya esta devuelto lanza una excepcion ReglaNegocioException; si fecha < FechaInicio lanza ValidacionDominioExcepcion.
Si todo esta ok guardaFechaDevolucion y pasa a Devuelto.

La entidad Prestamo no debe de conocer la clase Libro: solo debe guardar LibroId. Compila al terminar
