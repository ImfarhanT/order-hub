using iTextSharp.text;
using iTextSharp.text.pdf;
using HubApi.Services;

namespace HubApi.Services;

public class PdfReportService : IPdfReportService
{
    public byte[] GenerateGatewayRevenuePdf(GatewayRevenueSummary gateway, List<OrderRevenueDetail> orders, DateTime startDate, DateTime endDate)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            Document document = new Document(PageSize.A4, 25, 25, 30, 30);
            PdfWriter writer = PdfWriter.GetInstance(document, ms);

            document.Open();

            // Header
            AddHeader(document, $"Gateway Revenue Report - {gateway.GatewayName}", startDate, endDate);

            // Gateway Summary
            AddGatewaySummary(document, gateway);

            // Orders Table
            AddOrdersTable(document, orders, "Orders for " + gateway.GatewayName);

            document.Close();
            return ms.ToArray();
        }
    }

    public byte[] GeneratePartnerRevenuePdf(PartnerRevenueSummary partner, List<OrderRevenueDetail> orders, DateTime startDate, DateTime endDate)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            Document document = new Document(PageSize.A4, 25, 25, 30, 30);
            PdfWriter writer = PdfWriter.GetInstance(document, ms);

            document.Open();

            // Header
            AddHeader(document, $"Partner Revenue Report - {partner.PartnerName}", startDate, endDate);

            // Partner Summary
            AddPartnerSummary(document, partner);

            // Orders Table
            AddOrdersTable(document, orders, "Orders for " + partner.PartnerName);

            document.Close();
            return ms.ToArray();
        }
    }

    public byte[] GenerateFullRevenuePdf(GatewayPartnerRevenueReport report)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            Document document = new Document(PageSize.A4, 25, 25, 30, 30);
            PdfWriter writer = PdfWriter.GetInstance(document, ms);

            document.Open();

            // Header
            AddHeader(document, "Complete Revenue Report", report.StartDate, report.EndDate);

            // Overall Summary
            AddOverallSummary(document, report);

            // Gateway Summaries
            AddGatewaySummariesTable(document, report.GatewaySummaries);

            // Partner Summaries
            AddPartnerSummariesTable(document, report.PartnerSummaries);

            // Orders Table
            AddOrdersTable(document, report.OrderDetails, "All Orders");

            document.Close();
            return ms.ToArray();
        }
    }

    private void AddHeader(Document document, string title, DateTime startDate, DateTime endDate)
    {
        // Title
        Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
        Paragraph titlePara = new Paragraph(title, titleFont);
        titlePara.Alignment = Element.ALIGN_CENTER;
        titlePara.SpacingAfter = 20f;
        document.Add(titlePara);

        // Date Range
        Font dateFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
        Paragraph datePara = new Paragraph($"Period: {startDate:MMM dd, yyyy} - {endDate:MMM dd, yyyy}", dateFont);
        datePara.Alignment = Element.ALIGN_CENTER;
        datePara.SpacingAfter = 20f;
        document.Add(datePara);

        // Separator line
        Paragraph separator = new Paragraph("_".PadRight(100, '_'));
        separator.Alignment = Element.ALIGN_CENTER;
        separator.SpacingAfter = 20f;
        document.Add(separator);
    }

    private void AddGatewaySummary(Document document, GatewayRevenueSummary gateway)
    {
        Font sectionFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
        Paragraph sectionTitle = new Paragraph("Gateway Summary", sectionFont);
        sectionTitle.SpacingAfter = 10f;
        document.Add(sectionTitle);

        PdfPTable table = new PdfPTable(2);
        table.WidthPercentage = 100;
        table.SpacingBefore = 10f;
        table.SpacingAfter = 20f;

        AddTableRow(table, "Gateway Name", gateway.GatewayName);
        AddTableRow(table, "Gateway Code", gateway.GatewayCode);
        AddTableRow(table, "Total Revenue", $"${gateway.TotalRevenue:F2}");
        AddTableRow(table, "Gateway Fees", $"${gateway.TotalFees:F2}");
        AddTableRow(table, "Net Revenue", $"${gateway.NetRevenue:F2}");
        AddTableRow(table, "Order Count", gateway.OrderCount.ToString());

        document.Add(table);
    }

    private void AddPartnerSummary(Document document, PartnerRevenueSummary partner)
    {
        Font sectionFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
        Paragraph sectionTitle = new Paragraph("Partner Summary", sectionFont);
        sectionTitle.SpacingAfter = 10f;
        document.Add(sectionTitle);

        PdfPTable table = new PdfPTable(2);
        table.WidthPercentage = 100;
        table.SpacingBefore = 10f;
        table.SpacingAfter = 20f;

        AddTableRow(table, "Partner Name", partner.PartnerName);
        AddTableRow(table, "Partner Code", partner.PartnerCode);
        AddTableRow(table, "Total Revenue Share", $"${partner.TotalRevenueShare:F2}");
        AddTableRow(table, "Order Count", partner.OrderCount.ToString());

        document.Add(table);

        // Gateway Breakdown
        if (partner.GatewayRevenues.Any())
        {
            Paragraph breakdownTitle = new Paragraph("Revenue by Gateway", sectionFont);
            breakdownTitle.SpacingAfter = 10f;
            document.Add(breakdownTitle);

            PdfPTable breakdownTable = new PdfPTable(4);
            breakdownTable.WidthPercentage = 100;
            breakdownTable.SpacingBefore = 10f;
            breakdownTable.SpacingAfter = 20f;

            // Headers
            AddTableHeader(breakdownTable, "Gateway", "Revenue Share", "Percentage", "Orders");

            foreach (var gatewayRevenue in partner.GatewayRevenues)
            {
                breakdownTable.AddCell(new PdfPCell(new Phrase(gatewayRevenue.GatewayCode)) { Padding = 5 });
                breakdownTable.AddCell(new PdfPCell(new Phrase($"${gatewayRevenue.RevenueShare:F2}")) { Padding = 5 });
                breakdownTable.AddCell(new PdfPCell(new Phrase($"{gatewayRevenue.AssignmentPercentage:F1}%")) { Padding = 5 });
                breakdownTable.AddCell(new PdfPCell(new Phrase(gatewayRevenue.OrderCount.ToString())) { Padding = 5 });
            }

            document.Add(breakdownTable);
        }
    }

    private void AddOverallSummary(Document document, GatewayPartnerRevenueReport report)
    {
        Font sectionFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
        Paragraph sectionTitle = new Paragraph("Overall Summary", sectionFont);
        sectionTitle.SpacingAfter = 10f;
        document.Add(sectionTitle);

        PdfPTable table = new PdfPTable(2);
        table.WidthPercentage = 100;
        table.SpacingBefore = 10f;
        table.SpacingAfter = 20f;

        AddTableRow(table, "Total Revenue", $"${report.TotalRevenue:F2}");
        AddTableRow(table, "Total Orders", report.OrderDetails.Count.ToString());
        AddTableRow(table, "Total Gateways", report.GatewaySummaries.Count.ToString());
        AddTableRow(table, "Total Partners", report.PartnerSummaries.Count.ToString());

        document.Add(table);
    }

    private void AddGatewaySummariesTable(Document document, List<GatewayRevenueSummary> gateways)
    {
        Font sectionFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
        Paragraph sectionTitle = new Paragraph("Gateway Revenue Summary", sectionFont);
        sectionTitle.SpacingAfter = 10f;
        document.Add(sectionTitle);

        PdfPTable table = new PdfPTable(5);
        table.WidthPercentage = 100;
        table.SpacingBefore = 10f;
        table.SpacingAfter = 20f;

        // Headers
        AddTableHeader(table, "Gateway", "Revenue", "Fees", "Net Revenue", "Orders");

        foreach (var gateway in gateways)
        {
            table.AddCell(new PdfPCell(new Phrase(gateway.GatewayName)) { Padding = 5 });
            table.AddCell(new PdfPCell(new Phrase($"${gateway.TotalRevenue:F2}")) { Padding = 5 });
            table.AddCell(new PdfPCell(new Phrase($"${gateway.TotalFees:F2}")) { Padding = 5 });
            table.AddCell(new PdfPCell(new Phrase($"${gateway.NetRevenue:F2}")) { Padding = 5 });
            table.AddCell(new PdfPCell(new Phrase(gateway.OrderCount.ToString())) { Padding = 5 });
        }

        document.Add(table);
    }

    private void AddPartnerSummariesTable(Document document, List<PartnerRevenueSummary> partners)
    {
        Font sectionFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
        Paragraph sectionTitle = new Paragraph("Partner Revenue Summary", sectionFont);
        sectionTitle.SpacingAfter = 10f;
        document.Add(sectionTitle);

        PdfPTable table = new PdfPTable(3);
        table.WidthPercentage = 100;
        table.SpacingBefore = 10f;
        table.SpacingAfter = 20f;

        // Headers
        AddTableHeader(table, "Partner", "Revenue Share", "Orders");

        foreach (var partner in partners)
        {
            table.AddCell(new PdfPCell(new Phrase(partner.PartnerName)) { Padding = 5 });
            table.AddCell(new PdfPCell(new Phrase($"${partner.TotalRevenueShare:F2}")) { Padding = 5 });
            table.AddCell(new PdfPCell(new Phrase(partner.OrderCount.ToString())) { Padding = 5 });
        }

        document.Add(table);
    }

    private void AddOrdersTable(Document document, List<OrderRevenueDetail> orders, string title)
    {
        Font sectionFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
        Paragraph sectionTitle = new Paragraph(title, sectionFont);
        sectionTitle.SpacingAfter = 10f;
        document.Add(sectionTitle);

        PdfPTable table = new PdfPTable(8);
        table.WidthPercentage = 100;
        table.SpacingBefore = 10f;
        table.SpacingAfter = 20f;

        // Headers
        AddTableHeader(table, "Order ID", "Site", "Customer", "Total", "Gateway", "Fees", "Net Revenue", "Date");

        foreach (var order in orders)
        {
            table.AddCell(new PdfPCell(new Phrase(order.WcOrderId)) { Padding = 3 });
            table.AddCell(new PdfPCell(new Phrase(order.SiteName)) { Padding = 3 });
            table.AddCell(new PdfPCell(new Phrase(order.CustomerName)) { Padding = 3 });
            table.AddCell(new PdfPCell(new Phrase($"${order.OrderTotal:F2}")) { Padding = 3 });
            table.AddCell(new PdfPCell(new Phrase(order.PaymentGatewayCode)) { Padding = 3 });
            table.AddCell(new PdfPCell(new Phrase($"${order.GatewayFees:F2}")) { Padding = 3 });
            table.AddCell(new PdfPCell(new Phrase($"${order.NetRevenue:F2}")) { Padding = 3 });
            table.AddCell(new PdfPCell(new Phrase(order.PlacedAt.ToString("MMM dd, yyyy"))) { Padding = 3 });
        }

        document.Add(table);
    }

    private void AddTableRow(PdfPTable table, string label, string value)
    {
        Font labelFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
        Font valueFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

        table.AddCell(new PdfPCell(new Phrase(label, labelFont)) { Padding = 5 });
        table.AddCell(new PdfPCell(new Phrase(value, valueFont)) { Padding = 5 });
    }

    private void AddTableHeader(PdfPTable table, params string[] headers)
    {
        Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
        
        foreach (string header in headers)
        {
            PdfPCell cell = new PdfPCell(new Phrase(header, headerFont));
            cell.Padding = 5;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            table.AddCell(cell);
        }
    }
}
