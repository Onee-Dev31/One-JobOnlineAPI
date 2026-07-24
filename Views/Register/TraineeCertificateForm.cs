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
                page.MarginHorizontal(28, Unit.Millimetre);
                page.MarginVertical(20, Unit.Millimetre);

                page.DefaultTextStyle(x => x
                    .FontSize(16)
                    .FontFamily("DB Heavent"));

                page.Content().Column(col =>
                {
                    var imagePath = Path.Combine(
                        Directory.GetCurrentDirectory(), "Views", "imagesform", "one_logo.png");

                    col.Item().AlignCenter().Height(70).Image(imagePath).FitHeight();

                    col.Item().PaddingTop(24).AlignCenter().Text("หนังสือรับรองการฝึกงาน")
                        .Bold().FontSize(20).Underline();

                    col.Item()
                        .PaddingTop(32)
                        .Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(14).LineHeight(1.35f));
                        t.Span("          โดยหนังสือฉบับนี้ บริษัทขอรับรองว่า ");
                        t.Span(FullName()).Bold();
                        t.Span(" นักศึกษาจาก");
                        t.Span(G("School")).Bold();
                        t.Span(" คณะ");
                        t.Span(G("Faculty")).Bold();
                        t.Span(" ได้เข้ารับการฝึกงานที่  ");
                        t.Span(G("Office")).Bold();
                        t.Span("  ");
                        t.Span(G("DepartmentName")).Bold();
                        t.Line("");
                        t.Span("ตั้งแต่วันที่  ");
                        t.Span(FormatDate("InternshipStartDate")).Bold();
                        t.Span("  ถึง  ");
                        t.Span(FormatDate("InternshipEndDate")).Bold();
                    });

                    col.Item()
                        .PaddingTop(22)
                        .PaddingLeft(35)
                        .Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(14).LineHeight(1.35f));
                        t.Span("ออกให้ ณ วันที่  ");
                        t.Span(ThaiDateFormatter.FormatFull(DateTime.Now)).Bold();
                    });

                    col.Item().PaddingTop(50).AlignRight().Column(sig =>
                    {
                        sig.Item().PaddingTop(4).Width(220).AlignCenter()
                            .Text("ลงชื่อ..................................................").FontSize(14);
                        sig.Item().PaddingTop(4).Width(220).AlignCenter()
                            .Text("( นางสาวสุนันต์ทา วงษ์จันทร์ทอง )").FontSize(14);
                        sig.Item().Width(220).AlignCenter()
                            .Text("หัวหน้าแผนกทรัพยากรบุคคล").FontSize(14);
                    });

                    col.Item().PaddingTop(50).Column(footer =>
                    {
                        footer.Item().Text("ฝ่ายทรัพยากรบุคคล").FontSize(14);
                        footer.Item().Text("โทร. (662) 669 9508").FontSize(13);
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
