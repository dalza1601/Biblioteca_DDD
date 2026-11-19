Implementa los endpoints HTTP de este proyecto:

- En Catalogo.Api: listar libros, registrar un libro y desactivar un libro.
- En Prestamos.Api: listar préstamos, registrar un préstamo y devolver un préstamo.

Usa records para los modelos de request y response. No expongas directamente las entidades de dominio en las respuestas.

Aplica estos códigos HTTP:

- 200 OK al listar, desactivar o devolver, con el detalle correspondiente en la respuesta.
- 201 Created al registrar un libro o préstamo.
- 400 Bad Request cuando se produzca una ValidationDominioException.
- 404 Not Found cuando el recurso solicitado no exista.
- 409 Conflict cuando se produzca una ReglaNegocioException.

Todas las respuestas de error deben tener exactamente esta estructura JSON: {"error": "message"}, donde message contiene el mensaje del error.

Respeta la arquitectura y las convenciones existentes del repositorio. Al terminar, ejecuta la compilación de la solución y corrige cualquier error hasta que compile correctamente.
