using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace JobOnlineAPI.Views.Register
{
    public class TraineeCertificateForm : IDocument
    {
        private readonly IDictionary<string, object> _form;

        public TraineeCertificateForm(IDictionary<string, object> form)
        {
            _form = form;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(16, Unit.Millimetre);
                page.MarginVertical(20, Unit.Millimetre);

                page.DefaultTextStyle(x => x
                    .FontSize(15)
                    .FontFamily("DB Heavent"));

                page.Content().Column(col =>
                {
                    var imagePath = Path.Combine(
                        Directory.GetCurrentDirectory(), "Views", "imagesform", "one_logo.png");

                    col.Item().AlignCenter().Height(70).Image(imagePath).FitHeight();

                    col.Item().PaddingTop(22).AlignCenter().Text("หนังสือรับรองการฝึกงาน")
                        .Bold().FontSize(20).Underline();

                    col.Item()
                        .PaddingTop(32)
                        .Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(15).LineHeight(1.35f));
                        t.Span("          โดยหนังสือฉบับนี้ บริษัทขอรับรองว่า ");
                        t.Span(FullName()).Bold().FontColor(Colors.Red.Medium);
                        t.Span(" นักศึกษาจาก ");
                        t.Span(G("School")).Bold().FontColor(Colors.Red.Medium);
                        t.Span(" คณะ ");
                        t.Span(G("Faculty")).Bold().FontColor(Colors.Red.Medium);
                        t.Span(" ได้เข้ารับการฝึกงานที่ ");
                        t.Span(G("Location")).Bold().FontColor(Colors.Red.Medium);
                        t.Span("  ฝ่าย  ");
                        t.Span(G("DepartmentName")).Bold().FontColor(Colors.Red.Medium);
                        t.Line("");
                        t.Span("ตั้งแต่วันที่ ");
                        t.Span(FormatDate("InternshipStartDate")).Bold().FontColor(Colors.Red.Medium);
                        t.Span("  ถึง ");
                        t.Span(FormatDate("InternshipEndDate")).Bold().FontColor(Colors.Red.Medium);
                    });

                    col.Item()
                        .PaddingTop(22)
                        .PaddingLeft(35)
                        .Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(15).LineHeight(1.35f));
                        t.Span("ออกให้ ณ วันที่  ");
                        t.Span(ThaiDateFormatter.FormatFull(DateTime.Now)).Bold().FontColor(Colors.Red.Medium);
                    });

                    col.Item().PaddingTop(180).AlignRight().Column(sig =>
                    {
                        sig.Item().PaddingTop(4).Width(220).AlignCenter()
                            .Text("ลงชื่อ..................................................").FontSize(15);
                        sig.Item().PaddingTop(4).Width(220).AlignCenter()
                            .Text("( นางสาวสุนันต์ทา วงษ์จันทร์ทอง )").FontSize(15);
                        sig.Item().Width(220).AlignCenter()
                            .Text("หัวหน้าแผนกทรัพยากรบุคคล").FontSize(15);
                    });

                    col.Item().PaddingTop(100).Column(footer =>
                    {
                        footer.Item().Text("ฝ่ายทรัพยากรบุคคล").FontSize(15);
                        footer.Item().Text("โทร. (662) 669 9508").FontSize(15);
                    });
                });
            });
        }

        private string G(string key)
        {
            if (!_form.TryGetValue(key, out var v) || v == null || v == DBNull.Value) return "";
            return v.ToString() ?? "";
        }

        private string FullName()
        {
            var title = G("Title");
            var first = G("FirstNameThai");
            var last = G("LastNameThai");
            return $"{title}{first} {last}".Trim();
        }

        private string FormatDate(string key)
        {
            if (!_form.TryGetValue(key, out var v) || v == null || v == DBNull.Value) return "";
            return ThaiDateFormatter.FormatFull(v);
        }
    }
}
