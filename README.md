# Cafetería UPDS — Inventario y ventas

Sistema web académico desarrollado con ASP.NET Core MVC para administrar
productos, inventario, compras, pagos y ventas de una cafetería.

## Demo

https://cafeteri-upds.onrender.com

> El servicio gratuito de Render puede tardar aproximadamente 50 segundos
> en responder después de un periodo de inactividad.

## Funciones

### Cliente

- Registro e inicio de sesión.
- Menú con búsqueda y categorías Bebida, Postre y Snack.
- Carrito de compras con validación de stock.
- Pago mediante efectivo, QR o tarjeta.
- Factura/comprobante académico en PDF con fecha y hora.
- Historial y detalle de compras.
- Aplicación instalable PWA y página sin conexión.
- Asistente de voz mediante Web Speech API.

### Administrador

- CRUD de productos y control de disponibilidad.
- Alertas de stock mínimo.
- Ventas filtradas por periodo.
- Productos más vendidos.
- Detección de carritos abandonados.
- Conciliación de pagos y cálculo de comisiones.
- Reporte administrativo completo en PDF.
- Roles y rutas protegidas con ASP.NET Core Identity.

## Tecnologías

- .NET 10 y ASP.NET Core MVC
- Entity Framework Core
- ASP.NET Core Identity
- PostgreSQL y Supabase
- QuestPDF
- HTML, CSS, Bootstrap y JavaScript
- Web Speech API y Service Worker
- Docker y Render

## Ejecutar localmente

La contraseña de PostgreSQL no se almacena en GitHub. Configura la conexión
mediante User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true"
```

Luego ejecuta:

```powershell
dotnet restore
dotnet build
dotnet ef database update
dotnet run
```

## Observaciones

- Comisiones académicas configuradas: efectivo 0%, QR 1% y tarjeta 3%.
- Un carrito con productos se marca abandonado después de 30 minutos sin cambios.
- Los comprobantes generados son demostrativos y no sustituyen una factura fiscal.
- Al iniciar, el servidor aplica automáticamente las migraciones pendientes.
