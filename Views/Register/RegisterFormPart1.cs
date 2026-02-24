using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace JobOnlineAPI.Views.Register
{
    public class RegisterFormPart1 : IDocument
    {
        private readonly IDictionary<string, object> _form;

        public RegisterFormPart1(IDictionary<string, object> form)
        {
            _form = form;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);

                page.DefaultTextStyle(x => x
                    .FontSize(11)
                    .FontFamily("DB Heavent"));

                page.Header().Element(ComposeHeader);

                page.Content().Column(col =>
                {
                    col.Item()
                        .PaddingBottom(2)
                        .AlignLeft()
                        .Text("ใบสมัครงาน")
                        .Bold()
                        .FontSize(18);

                    col.Item();
                    RenderPositionSection(col);
                    
                    col.Item().PaddingTop(18);
                    RenderPersonalSection(col);

                    col.Item().PaddingTop(18);
                    RenderSignatureSection(col);
                });


            });
        }

        // ========================= HEADER =========================
        private void ComposeHeader(IContainer container)
        {
            container.PaddingBottom(4).Column(col =>
            {
                var imagePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Views",
                    "imagesform",
                    "one_logo.png");

                col.Item()
                    .AlignCenter()
                    .Height(50)
                    .Image(imagePath, ImageScaling.FitHeight);

                col.Item().PaddingTop(4);

                col.Item()
                    .AlignCenter()
                    .Text("บริษัท เดอะ วัน เอ็นเตอร์ไพรส์ จำกัด (มหาชน)")
                    .Bold()
                    .FontSize(12);

                col.Item()
                    .AlignCenter()
                    .Text("The ONE Enterprise Public Company Limited")
                    .FontSize(11)
                    .FontColor(Colors.Grey.Darken1);

                col.Item()
                    .PaddingTop(10)
                    .LineHorizontal(1.2f)
                    .LineColor(Colors.Grey.Darken2);
            });
        }

        // ========================= POSITION SECTION =========================
        private void RenderPositionSection(ColumnDescriptor col)
        {
            col.Item()
                .Border(1)
                .Background(Colors.Grey.Lighten5)
                .Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Cell()
                        .BorderRight(1)
                        .Padding(10)
                        .Text(t =>
                        {
                            t.Span("ตำแหน่งที่ต้องการสมัคร: ").Bold();
                            t.Span(GetValue("JobTitle"));
                        });

                    table.Cell()
                        .Padding(10)
                        .Text(t =>
                        {
                            t.Span("เงินเดือนที่ต้องการ: ").Bold();
                            t.Span(GetValue("Salary") + " บาท");
                        });
                });
        }

        // ========================= PERSONAL DETAILS =========================
        private void RenderPersonalSection(ColumnDescriptor col)
        {
            col.Item().Border(1).Column(inner =>
            {
                inner.Item()
                    .Background(Colors.Grey.Lighten3)
                    .Padding(6)
                    .Text("ข้อมูลส่วนตัว (Personal Details)")
                    .Bold();

                inner.Item().Padding(12).Column(content =>
                {
                    content.Spacing(8);

                    content.Item().Row(row =>
                    {
                        row.RelativeItem(3).Text(t =>
                        {
                            t.Span("ชื่อ-สกุล[TH]: ").Bold();
                            t.Span($"{GetValue("FirstNameThai")} {GetValue("LastNameThai")}");
                        });

                        row.RelativeItem(2).Text(t =>
                        {
                            t.Span("ชื่อเล่น: ").Bold();
                            t.Span(GetValue("Nickname"));
                        });
                    });

                    content.Item().Row(row =>
                    {
                        row.RelativeItem(3).Text(t =>
                        {
                            t.Span("Name-Surname[EN]: ").Bold();
                            t.Span($"{GetValue("FirstNameEng")} {GetValue("LastNameEng")}");
                        });

                        row.RelativeItem(2).Text(t =>
                        {
                            t.Span("Email: ").Bold();
                            t.Span(GetValue("Email"));
                        });
                    });

                    content.Item().Row(row =>
                    {
                        row.RelativeItem(2).Text(t =>
                        {
                            t.Span("โทรศัพท์: ").Bold();
                            t.Span(GetValue("MobilePhone"));
                        });
                    });
                });
            });
        }

        // ========================= SIGNATURE SECTION =========================
        private void RenderSignatureSection(ColumnDescriptor col)
        {
            col.Item().PaddingTop(10).LineHorizontal(1);

            col.Item().PaddingTop(30).Row(row =>
            {
                row.Spacing(14);

                RenderSignatureBox(row,
                    GetValue("RequesterManagerNameTH"),
                    "ผู้อำนวยการฝ่าย");

                RenderSignatureBox(row,
                    GetValue("HiringManagerNameTH"),
                    "ผู้อำนวยการฝ่ายทรัพยากรบุคคล");

                RenderSignatureBox(row,
                    GetValue("ChiefExecutiveOfficerNameTH"),
                    GetValue("ChiefExecutiveOfficerPosition"));
            });
        }

        private void RenderSignatureBox(RowDescriptor row,
            string name,
            string position)
        {
            row.RelativeItem().PaddingHorizontal(8).Column(col =>
            {
                col.Item()
                    .Text("ลายเซ็น ................................................")
                    .FontSize(10);

                col.Item()
                    .PaddingTop(6)
                    .AlignCenter()
                    .Text($"({name})")
                    .Bold()
                    .FontSize(10);

                col.Item()
                    .AlignCenter()
                    .Text(position)
                    .FontSize(10);

                col.Item()
                    .PaddingTop(6)
                    .AlignCenter()
                    .Text("วันที่ ................................................")
                    .FontSize(10);
            });
        }

        // ========================= HELPER =========================
        private string GetValue(string key)
        {
            return _form.ContainsKey(key) && _form[key] != null
                ? _form[key].ToString() ?? ""
                : "";
        }
    }
}
