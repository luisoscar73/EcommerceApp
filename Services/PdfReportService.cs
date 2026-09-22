using EcommerceApp.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EcommerceApp.Services
{
    public class PdfReportService
    {
        private const string Black = "#090909";
        private const string Acid = "#D9FF43";
        private const string LightGrey = "#E5E5E5";

        public byte[] GeneratePeriodReport(
            ReportsDashboardViewModel report)
        {
            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(24);
                    page.DefaultTextStyle(style => style.FontSize(8));

                    page.Header().Column(header =>
                    {
                        header.Item()
                            .Background(Black)
                            .Padding(14)
                            .Row(row =>
                            {
                                row.RelativeItem()
                                    .Text("CAFETERIA UPDS")
                                    .FontSize(21)
                                    .Bold()
                                    .FontColor(Colors.White);

                                row.ConstantItem(280)
                                    .AlignRight()
                                    .Text("REPORTE ADMINISTRATIVO")
                                    .FontSize(12)
                                    .Bold()
                                    .FontColor(Acid);
                            });

                        header.Item().PaddingTop(6).Row(row =>
                        {
                            row.RelativeItem().Text(
                                $"Periodo: {report.From:dd/MM/yyyy} al {report.To:dd/MM/yyyy}");
                            row.RelativeItem().AlignRight().Text(
                                $"Generado: {ToBoliviaTime(report.GeneratedAt):dd/MM/yyyy HH:mm}");
                        });
                    });

                    page.Content().PaddingVertical(12).Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Row(row =>
                        {
                            SummaryCard(row.RelativeItem(),
                                "VENTAS", report.Sales.Count.ToString());
                            SummaryCard(row.RelativeItem(),
                                "INGRESO BRUTO", $"Bs {report.GrossIncome:0.00}");
                            SummaryCard(row.RelativeItem(),
                                "COMISIONES", $"Bs {report.TotalCommissions:0.00}");
                            SummaryCard(row.RelativeItem(),
                                "INGRESO NETO", $"Bs {report.NetIncome:0.00}");
                            SummaryCard(row.RelativeItem(),
                                "CARRITOS ABANDONADOS",
                                report.AbandonedCarts.Count.ToString());
                        });

                        SectionTitle(column.Item(), "VENTAS DEL PERIODO");

                        if (report.Sales.Count == 0)
                        {
                            EmptyMessage(column.Item(),
                                "No se registraron ventas en el periodo seleccionado.");
                        }
                        else
                        {
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(100);
                                    columns.ConstantColumn(95);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.ConstantColumn(70);
                                    columns.ConstantColumn(85);
                                    columns.ConstantColumn(85);
                                });

                                table.Header(header =>
                                {
                                    HeaderCell(header.Cell(), "Factura");
                                    HeaderCell(header.Cell(), "Fecha y hora");
                                    HeaderCell(header.Cell(), "Cliente");
                                    HeaderCell(header.Cell(), "NIT/CI");
                                    HeaderCell(header.Cell(), "Unidades");
                                    HeaderCell(header.Cell(), "Pago");
                                    HeaderCell(header.Cell(), "Total");
                                });

                                foreach (Sale sale in report.Sales)
                                {
                                    BodyCell(table.Cell(),
                                        sale.InvoiceNumber ?? $"Venta #{sale.Id}");
                                    BodyCell(table.Cell(),
                                        $"{ToBoliviaTime(sale.SaleDate):dd/MM/yyyy HH:mm}");
                                    BodyCell(table.Cell(),
                                        sale.BillingName
                                        ?? sale.Customer?.FullName
                                        ?? sale.Customer?.Email
                                        ?? "Cliente");
                                    BodyCell(table.Cell(), sale.TaxId ?? "-");
                                    BodyCell(table.Cell(),
                                        sale.Details.Sum(d => d.Quantity).ToString());
                                    BodyCell(table.Cell(),
                                        sale.Payment?.Method ?? "-");
                                    BodyCell(table.Cell(), $"Bs {sale.Total:0.00}");
                                }
                            });
                        }

                        SectionTitle(column.Item(), "PRODUCTOS MAS VENDIDOS");

                        if (report.BestSellers.Count == 0)
                        {
                            EmptyMessage(column.Item(),
                                "No existen productos vendidos en el periodo.");
                        }
                        else
                        {
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(45);
                                    columns.RelativeColumn(3);
                                    columns.ConstantColumn(110);
                                    columns.ConstantColumn(120);
                                });

                                table.Header(header =>
                                {
                                    HeaderCell(header.Cell(), "Pos.");
                                    HeaderCell(header.Cell(), "Producto");
                                    HeaderCell(header.Cell(), "Unidades");
                                    HeaderCell(header.Cell(), "Ingresos");
                                });

                                int position = 1;
                                foreach (BestSellingProductViewModel item
                                    in report.BestSellers)
                                {
                                    BodyCell(table.Cell(), position++.ToString());
                                    BodyCell(table.Cell(), item.ProductName);
                                    BodyCell(table.Cell(), item.Quantity.ToString());
                                    BodyCell(table.Cell(), $"Bs {item.Income:0.00}");
                                }
                            });
                        }

                        SectionTitle(column.Item(), "INVENTARIO CON STOCK MAS BAJO");

                        if (report.LowStockProducts.Count == 0)
                        {
                            EmptyMessage(column.Item(),
                                "No hay productos con stock bajo.");
                        }
                        else
                        {
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(45);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.ConstantColumn(80);
                                    columns.ConstantColumn(80);
                                    columns.ConstantColumn(100);
                                });

                                table.Header(header =>
                                {
                                    HeaderCell(header.Cell(), "ID");
                                    HeaderCell(header.Cell(), "Producto");
                                    HeaderCell(header.Cell(), "Categoria");
                                    HeaderCell(header.Cell(), "Stock");
                                    HeaderCell(header.Cell(), "Minimo");
                                    HeaderCell(header.Cell(), "Estado");
                                });

                                foreach (Product product
                                    in report.LowStockProducts)
                                {
                                    BodyCell(table.Cell(), product.Id.ToString());
                                    BodyCell(table.Cell(), product.Name);
                                    BodyCell(table.Cell(),
                                        product.Category ?? "Sin categoria");
                                    BodyCell(table.Cell(), product.Stock.ToString());
                                    BodyCell(table.Cell(),
                                        product.MinimumStock.ToString());
                                    BodyCell(table.Cell(), InventoryStatus(product));
                                }
                            });
                        }

                        SectionTitle(column.Item(), "CARRITOS ABANDONADOS");

                        if (report.AbandonedCarts.Count == 0)
                        {
                            EmptyMessage(column.Item(),
                                "No hay carritos abandonados registrados.");
                        }
                        else
                        {
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(55);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.ConstantColumn(95);
                                    columns.ConstantColumn(70);
                                    columns.ConstantColumn(90);
                                });

                                table.Header(header =>
                                {
                                    HeaderCell(header.Cell(), "Carrito");
                                    HeaderCell(header.Cell(), "Cliente");
                                    HeaderCell(header.Cell(), "Correo");
                                    HeaderCell(header.Cell(), "Abandonado");
                                    HeaderCell(header.Cell(), "Unidades");
                                    HeaderCell(header.Cell(), "Total");
                                });

                                foreach (SavedCart cart
                                    in report.AbandonedCarts)
                                {
                                    BodyCell(table.Cell(), $"#{cart.Id}");
                                    BodyCell(table.Cell(),
                                        cart.Customer?.FullName ?? "Cliente");
                                    BodyCell(table.Cell(),
                                        cart.Customer?.Email ?? "-");
                                    BodyCell(table.Cell(),
                                        cart.AbandonedAt.HasValue
                                            ? $"{ToBoliviaTime(cart.AbandonedAt.Value):dd/MM/yyyy HH:mm}"
                                            : "-");
                                    BodyCell(table.Cell(),
                                        cart.Items.Sum(i => i.Quantity).ToString());
                                    BodyCell(table.Cell(), $"Bs {cart.Total:0.00}");
                                }
                            });
                        }

                        SectionTitle(column.Item(),
                            "CONCILIACION DE PAGOS Y COMISIONES");

                        if (report.Payments.Count == 0)
                        {
                            EmptyMessage(column.Item(),
                                "No hay pagos en el periodo seleccionado.");
                        }
                        else
                        {
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(65);
                                    columns.ConstantColumn(95);
                                    columns.ConstantColumn(75);
                                    columns.RelativeColumn(2);
                                    columns.ConstantColumn(75);
                                    columns.ConstantColumn(75);
                                    columns.ConstantColumn(75);
                                    columns.ConstantColumn(90);
                                });

                                table.Header(header =>
                                {
                                    HeaderCell(header.Cell(), "Venta");
                                    HeaderCell(header.Cell(), "Fecha y hora");
                                    HeaderCell(header.Cell(), "Metodo");
                                    HeaderCell(header.Cell(), "Referencia");
                                    HeaderCell(header.Cell(), "Bruto");
                                    HeaderCell(header.Cell(), "Comision");
                                    HeaderCell(header.Cell(), "Neto");
                                    HeaderCell(header.Cell(), "Estado");
                                });

                                foreach (Payment payment in report.Payments)
                                {
                                    BodyCell(table.Cell(), $"#{payment.SaleId}");
                                    BodyCell(table.Cell(),
                                        $"{ToBoliviaTime(payment.PaidAt):dd/MM/yyyy HH:mm}");
                                    BodyCell(table.Cell(), payment.Method);
                                    BodyCell(table.Cell(), payment.Reference ?? "-");
                                    BodyCell(table.Cell(),
                                        $"Bs {payment.GrossAmount:0.00}");
                                    BodyCell(table.Cell(),
                                        $"Bs {payment.CommissionAmount:0.00} ({payment.CommissionRate:0.##}%)");
                                    BodyCell(table.Cell(),
                                        $"Bs {payment.NetAmount:0.00}");
                                    BodyCell(table.Cell(),
                                        payment.ReconciliationStatus);
                                }
                            });
                        }
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text(
                            $"Impreso: {BoliviaNow():dd/MM/yyyy HH:mm} | Sistema de inventario y ventas");

                        row.ConstantItem(120).AlignRight().Text(text =>
                        {
                            text.Span("Pagina ");
                            text.CurrentPageNumber();
                            text.Span(" de ");
                            text.TotalPages();
                        });
                    });
                });
            }).GeneratePdf();
        }

        public byte[] GenerateSaleReceipt(Sale sale)
        {
            DateTime issueDate = sale.IssuedAt ?? sale.SaleDate;

            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(35);
                    page.DefaultTextStyle(style => style.FontSize(10));

                    page.Header()
                        .Background(Black)
                        .Padding(18)
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .Text("CAFETERIA UPDS")
                                .FontSize(22)
                                .Bold()
                                .FontColor(Colors.White);

                            row.RelativeItem()
                                .AlignRight()
                                .Column(column =>
                                {
                                    column.Item()
                                        .Text("FACTURA / COMPROBANTE")
                                        .FontSize(13)
                                        .Bold()
                                        .FontColor(Acid);
                                    column.Item()
                                        .Text(sale.InvoiceNumber
                                            ?? $"Venta #{sale.Id}")
                                        .FontColor(Colors.White);
                                });
                        });

                    page.Content().PaddingVertical(20).Column(column =>
                    {
                        column.Spacing(14);

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(left =>
                            {
                                left.Item().Text(
                                    $"Emision: {ToBoliviaTime(issueDate):dd/MM/yyyy HH:mm}");
                                left.Item().Text(
                                    $"Cliente: {sale.BillingName ?? sale.Customer?.FullName ?? "Cliente"}");
                                left.Item().Text(
                                    $"NIT/CI: {sale.TaxId ?? "No registrado"}");
                                left.Item().Text(
                                    $"Correo: {sale.Customer?.Email ?? "No registrado"}");
                            });

                            row.RelativeItem().AlignRight().Column(right =>
                            {
                                right.Item().Text($"Estado: {sale.Status}");
                                right.Item().Text(
                                    $"Pago: {sale.Payment?.Method ?? "No registrado"}");
                                right.Item().Text(
                                    $"Referencia: {sale.Payment?.Reference ?? "-"}");
                            });
                        });

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.ConstantColumn(70);
                                columns.ConstantColumn(100);
                                columns.ConstantColumn(100);
                            });

                            table.Header(header =>
                            {
                                HeaderCell(header.Cell(), "Producto");
                                HeaderCell(header.Cell(), "Cantidad");
                                HeaderCell(header.Cell(), "Precio");
                                HeaderCell(header.Cell(), "Subtotal");
                            });

                            foreach (SaleDetail detail in sale.Details)
                            {
                                BodyCell(table.Cell(),
                                    detail.Product?.Name ?? "Producto");
                                BodyCell(table.Cell(), detail.Quantity.ToString());
                                BodyCell(table.Cell(),
                                    $"Bs {detail.UnitPrice:0.00}");
                                BodyCell(table.Cell(),
                                    $"Bs {detail.Subtotal:0.00}");
                            }
                        });

                        if (sale.Payment != null)
                        {
                            column.Item()
                                .Border(1)
                                .BorderColor(LightGrey)
                                .Padding(10)
                                .Column(payment =>
                                {
                                    payment.Item().Text("DATOS DEL PAGO").Bold();
                                    payment.Item().Text(
                                        $"Pagado: {ToBoliviaTime(sale.Payment.PaidAt):dd/MM/yyyy HH:mm}");
                                    payment.Item().Text(
                                        $"Importe bruto: Bs {sale.Payment.GrossAmount:0.00}");
                                    payment.Item().Text(
                                        $"Comision ({sale.Payment.CommissionRate:0.##}%): Bs {sale.Payment.CommissionAmount:0.00}");
                                    payment.Item().Text(
                                        $"Importe neto: Bs {sale.Payment.NetAmount:0.00}");
                                    payment.Item().Text(
                                        $"Conciliacion: {sale.Payment.ReconciliationStatus}");

                                    if (sale.Payment.ReconciledAt.HasValue)
                                    {
                                        payment.Item().Text(
                                            $"Fecha de conciliacion: {ToBoliviaTime(sale.Payment.ReconciledAt.Value):dd/MM/yyyy HH:mm}");
                                    }
                                });
                        }

                        column.Item()
                            .AlignRight()
                            .Background(Acid)
                            .Padding(14)
                            .Text($"TOTAL: Bs {sale.Total:0.00}")
                            .FontSize(18)
                            .Bold()
                            .FontColor(Black);

                        column.Item()
                            .Text("Documento académico generado por el sistema de la Cafetería UPDS.")
                            .FontSize(8)
                            .FontColor(Colors.Grey.Darken1);
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem()
                            .Text("Gracias por su compra - Cafeteria UPDS");
                        row.RelativeItem().AlignRight()
                            .Text($"Impreso: {BoliviaNow():dd/MM/yyyy HH:mm}");
                    });
                });
            }).GeneratePdf();
        }

        private static void SummaryCard(
            IContainer container,
            string title,
            string value)
        {
            container
                .PaddingHorizontal(3)
                .Background(Black)
                .Padding(9)
                .Column(column =>
                {
                    column.Item().Text(title)
                        .FontSize(7)
                        .Bold()
                        .FontColor(Acid);
                    column.Item().PaddingTop(3).Text(value)
                        .FontSize(14)
                        .Bold()
                        .FontColor(Colors.White);
                });
        }

        private static void SectionTitle(
            IContainer container,
            string title)
        {
            container
                .Background(Acid)
                .PaddingVertical(6)
                .PaddingHorizontal(9)
                .Text(title)
                .Bold()
                .FontColor(Black);
        }

        private static void EmptyMessage(
            IContainer container,
            string text)
        {
            container
                .Border(1)
                .BorderColor(LightGrey)
                .Padding(9)
                .Text(text)
                .FontColor(Colors.Grey.Darken1);
        }

        private static void HeaderCell(
            IContainer container,
            string text)
        {
            container
                .Background(Black)
                .Padding(5)
                .Text(text)
                .Bold()
                .FontColor(Colors.White);
        }

        private static void BodyCell(
            IContainer container,
            string text)
        {
            container
                .BorderBottom(0.5f)
                .BorderColor(LightGrey)
                .Padding(5)
                .Text(text);
        }

        private static string InventoryStatus(Product product)
        {
            if (product.Stock == 0)
                return "Agotado";

            if (!product.IsAvailable)
                return "Inactivo";

            return "Stock bajo";
        }

        private static DateTime BoliviaNow()
            => DateTime.UtcNow.AddHours(-4);

        private static DateTime ToBoliviaTime(DateTime date)
        {
            DateTime utc = date.Kind == DateTimeKind.Utc
                ? date
                : DateTime.SpecifyKind(date, DateTimeKind.Utc);

            return utc.AddHours(-4);
        }
    }
}
