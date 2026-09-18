# Sistema de inventario y ventas para cafetería

Aplicación web desarrollada con ASP.NET Core MVC para gestionar
los productos, inventario, clientes y ventas de una cafetería.

## Funciones principales

### Cliente

- Registro e inicio de sesión.
- Visualización del menú.
- Búsqueda por nombre.
- Filtro por categoría.
- Carrito de compras.
- Confirmación de pedidos.
- Historial de compras.
- Detalle de cada compra.

### Administrador

- Registro de productos.
- Edición de productos.
- Control de disponibilidad.
- Control de stock mínimo.
- Reporte de productos con stock bajo.
- Panel general de ventas.
- Historial de ventas y clientes.
- Descuento automático del inventario.

## Tecnologías utilizadas

- ASP.NET Core MVC
- .NET 10
- Entity Framework Core
- ASP.NET Core Identity
- PostgreSQL
- Supabase
- Bootstrap
- HTML, CSS y C#

## Base de datos

La aplicación utiliza PostgreSQL mediante Supabase.

La cadena de conexión no se almacena en GitHub. Para ejecutar
el proyecto localmente debe configurarse mediante User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "TU_CONEXION"