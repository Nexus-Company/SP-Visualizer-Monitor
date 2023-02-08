using CsvHelper;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using OfficeOpenXml;
using SP.Visualizer.Monitor.Comuns.Models;
using SP.Visualizer.Monitor.Comuns.Models.Enums;
using System.Text;

namespace SP.Visualizer.Monitor.Comuns.Helpers;

public class ExportHelper
{
    public GraphHelper Graph { get; set; }
    private static string DirectoryResults => Path.GetFullPath("Results");

    public ExportHelper(GraphHelper graph)
    {
        Graph = graph;
    }

    /// <summary>
    /// Exportar atividades como Planilha Excel.
    /// </summary>
    /// <param name="search">String de busca</param>
    /// <param name="start">Data de inicio</param>
    /// <param name="end">Data de fim.</param>
    /// <param name="ascending">Ordem Crescente (true) ou Descrescente (false).</param>
    /// <returns>Local do arquivo</returns>
    public async Task<string> ExportActivitiesAsync(string? search, DateTime? start, DateTime? end, bool ascending, ExportTypes format = ExportTypes.Xlsx)
    {
        if (!Directory.Exists(DirectoryResults))
            Directory.CreateDirectory(DirectoryResults);

        string path = GetExportFilePath(start, end, format);
        string? userName, userEmail, fileName;
        (userName, userEmail, fileName) = search.GetSearch();
        int page = 1;

        ExportActivity[] exports = ExportActivity.Convert(Graph.GetActivities(ref page, out _, ascending: ascending, fileName: fileName, userName: userName, userEmail: userEmail, start: start, end: end));

        switch (format)
        {
            #region Export Xlsx
            case ExportTypes.Xlsx:
                // Cria um novo arquivo XLSX usando o EPPlus
                using (var package = new ExcelPackage())
                {
                    // Adiciona uma nova planilha ao arquivo
                    var sheet = package.Workbook.Worksheets.Add("Atividades");

                    sheet.Cells[1, 1].Value = "Tipo";
                    sheet.Cells[1, 2].Value = "Data";
                    sheet.Cells[1, 3].Value = "Hora";
                    sheet.Cells[1, 4].Value = "Usuário";
                    sheet.Cells[1, 5].Value = "Diretório";
                    sheet.Cells[1, 6].Value = "Nome do Arquivo";
                    sheet.Cells[1, 7].Value = "Url de acesso";

                    for (int i = 2; i < exports.Length + 2; i++)
                    {
                        ExportActivity actv = exports[i - 2];

                        sheet.Cells[i, 1].Value = actv.Type;
                        sheet.Cells[i, 2].Value = actv.Date;
                        sheet.Cells[i, 3].Value = actv.Hour;
                        sheet.Cells[i, 4].Value = actv.Email;
                        sheet.Cells[i, 5].Value = actv.Directory;
                        sheet.Cells[i, 6].Value = actv.FileName;
                        sheet.Cells[i, 7].Value = actv.WebUrl;
                    }

                    // Adiciona a tabela
                    sheet.Tables.Add(new ExcelAddressBase(1, 1, exports.Length + 1, 7), "Atividades");

                    // Salve o arquivo Excel
                    await package.SaveAsAsync(new FileInfo(path));
                }
                break;
            #endregion

            #region Export Csv
            case ExportTypes.Csv:
                using (var writer = new StreamWriter(path))
                {
                    using var csv = new CsvWriter(writer, Thread.CurrentThread.CurrentCulture);

                    await csv.WriteRecordsAsync(exports);
                }
                break;
            #endregion

            #region Export PDF
            case ExportTypes.Pdf:
                // Create a new MigraDoc document
                Document doc = new();

                // Define the section and table
                Section sec = doc.AddSection();
                var tab = sec.AddTable();
                // Add the headers to the table
                tab.AddColumn();
                tab.AddColumn();
                tab.AddColumn();
                tab.AddColumn();
                tab.AddColumn();
                tab.AddColumn();
                tab.AddColumn();

                // Add the headers row
                var row = tab.AddRow();
                row.Shading.Color = Colors.DarkOliveGreen;
                row.Cells[0].AddParagraph("Tipo");
                row.Cells[1].AddParagraph("Data");
                row.Cells[3].AddParagraph("Hora");
                row.Cells[4].AddParagraph("Usuário");
                row.Cells[5].AddParagraph("Diretório");
                row.Cells[6].AddParagraph("Nome do Arquivo");

                // Add the data rows
                foreach (var obj in exports)
                {
                    row = tab.AddRow();
                    row.Cells[0].AddParagraph()
                        .AddFormattedText(obj.Type);
                    row.Cells[1].AddParagraph().AddFormattedText(obj.Date);
                    row.Cells[3].AddParagraph().AddFormattedText(obj.Hour);
                    row.Cells[4].AddParagraph()
                        .AddHyperlink(obj.Email, HyperlinkType.Url)
                        .AddFormattedText(obj.User);
                    row.Cells[5].AddParagraph()
                        .AddFormattedText(obj.Directory);
                    row.Cells[6].AddParagraph(obj.FileName)
                        .AddHyperlink(obj.WebUrl, HyperlinkType.Web)
                        .AddFormattedText(obj.FileName);
                }

                PdfDocumentRenderer pdfRenderer = new(true)
                {
                    Document = doc
                };
                pdfRenderer.RenderDocument();
                pdfRenderer.Save(path);
                break;
                #endregion
        }

        return path;
    }

    private string BuildHtml(IEnumerable<ExportActivity> exports)
    {
        // Create a new StringBuilder to hold the HTML
        StringBuilder sb = new();

        // Add the HTML table header
        sb.Append("<table>");
        sb.Append("<tr>");
        sb.Append("<th>Coluna 1</th>");
        sb.Append("<th>Coluna 2</th>");
        sb.Append("<th>Coluna 3</th>");
        sb.Append("</tr>");

        // Add the data rows
        foreach (var obj in exports)
        {
            sb.Append("<tr>");
            sb.Append("<td>" + obj.Type + "</td>");
            sb.Append("<td>" + obj.Date + "</td>");
            sb.Append("<td>" + obj.Hour + "</td>");
            sb.Append("</tr>");
        }

        // Add the HTML table footer
        sb.Append("</table>");

        // Return the HTML
        return sb.ToString();
    }
    private static string GetExportFilePath(DateTime? start, DateTime? end, ExportTypes format = ExportTypes.Xlsx)
    {
        string fileName = "Exportacao ";
        string extension = "." + Enum.GetName(format)?.ToLowerInvariant();

        if (start != null && end == null)
            fileName += $"{start:dd-MM-yyyy}";
        else if (start != null && end != null)
            fileName += $"{start:dd-MM-yyyy} ate {end:dd-MM-yyyy}";
        else if (start == null && end != null)
            fileName += $"ate {end:dd-MM-yyyy}";

        string originalPath = Path.Combine(DirectoryResults, fileName);
        string path = originalPath + extension;
        int counter = 1;

        while (File.Exists(path))
        {
            path = $"{originalPath} ({counter}){extension}";
            counter++;
        }

        return path;
    }
}