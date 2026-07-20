using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace JobOnlineAPI.Views.Register
{
    public class TraineeRegisterForm : IDocument
    {
        private readonly IDictionary<string, object> _form;

        public TraineeRegisterForm(IDictionary<string, object> form)
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
                page.MarginVertical(14, Unit.Millimetre);

                page.DefaultTextStyle(x => x
                    .FontSize(11)
                    .FontFamily("DB Heavent")
                    .LineHeight(1.15f)
                    .FontColor(Colors.Grey.Darken4));

                page.Header().Element(ComposeHeader);

                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    col.Item()
                        .AlignLeft()
                        .Text("ใบสมัครฝึกงาน")
                        .Bold()
                        .FontSize(18);

                    RenderInternshipSection(col);
                    RenderEducationSection(col);
                    RenderPersonalSection(col);
                    RenderReasonSection(col);
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
                    .Image(imagePath).FitHeight();

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

        // ========================= INTERNSHIP SECTION =========================
        private void RenderInternshipSection(ColumnDescriptor col)
        {
            var desiredFields = new[]
            {
                GetValue("DesiredField1"),
                GetValue("DesiredField2"),
                GetValue("DesiredField3")
            }.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            col.Item().ShowEntire().Border(1).BorderColor(Colors.Grey.Medium).Column(inner =>
            {
                inner.Item()
                    .Background(Colors.Grey.Lighten3)
                    .Padding(6)
                    .Text("ข้อมูลการฝึกงาน (Internship Information)")
                    .Bold();

                inner.Item().Padding(10).Column(content =>
                {
                    content.Spacing(8);

                        content.Item().Text(t =>
                        {
                            t.Span("ตำแหน่งที่ต้องการ: ").Bold();

                            if (desiredFields.Length == 0)
                            {
                                t.Span("................................................");
                            }
                            else
                            {
                                for (var index = 0; index < desiredFields.Length; index++)
                                {
                                    t.Span($"{index + 1}) ").Bold();
                                    t.Span(desiredFields[index]);
                                    if (index < desiredFields.Length - 1)
                                        t.Span("     ");
                                }
                            }
                        });

                        content.Item().Text(t =>
                        {
                            t.Span("ประเภทการฝึกงาน: ").Bold();
                            t.Span(GetValue("InternshipType"));
                        });

                        content.Item().Text(t =>
                        {
                            t.Span("ระยะเวลาฝึกงาน: ").Bold();
                            var months = GetTrainingMonths();
                            t.Span(months > 0 ? $"{months} เดือน" : "................................");
                        });

                        content.Item().Row(row =>
                        {
                            row.RelativeItem().Text(t =>
                            {
                                t.Span("วันที่เริ่มฝึกงาน: ").Bold();
                                t.Span(GetValue("StartDate"));
                            });

                            row.RelativeItem().Text(t =>
                            {
                                t.Span("วันที่สิ้นสุด: ").Bold();
                                t.Span(GetValue("EndDate"));
                            });
                        });
                });
            });
        }

        // ========================= EDUCATION SECTION =========================
        private void RenderEducationSection(ColumnDescriptor col)
        {
            col.Item().ShowEntire().Border(1).Column(inner =>
            {
                inner.Item()
                    .Background(Colors.Grey.Lighten3)
                    .Padding(6)
                    .Text("ข้อมูลการศึกษา (Education)")
                    .Bold();

                inner.Item().Padding(12).Column(content =>
                {
                    content.Spacing(8);

                    content.Item().Row(row =>
                    {
                        row.RelativeItem(3).Text(t =>
                        {
                            t.Span("มหาวิทยาลัย/สถาบัน: ").Bold();
                            t.Span(GetValue("School"));
                        });

                        row.RelativeItem(2).Text(t =>
                        {
                            t.Span("ชั้นปีที่: ").Bold();
                            t.Span(GetValue("YearOfStudy"));
                        });
                    });

                    content.Item().Row(row =>
                    {
                        row.RelativeItem(3).Text(t =>
                        {
                            t.Span("คณะ: ").Bold();
                            t.Span(GetValue("Faculty"));
                        });

                        row.RelativeItem(2).Text(t =>
                        {
                            t.Span("สาขา: ").Bold();
                            t.Span(GetValue("Major"));
                        });
                    });

                    content.Item().Row(row =>
                    {
                        row.RelativeItem(3).Text(t =>
                        {
                            t.Span("วิชาโท: ").Bold();
                            t.Span(GetValue("Minor"));
                        });

                        row.RelativeItem(2).Text(t =>
                        {
                            t.Span("เกรดเฉลี่ย (GPA): ").Bold();
                            t.Span(GetValue("GPA"));
                        });
                    });

                    content.Item().Row(row =>
                    {
                        row.RelativeItem(3).Text(t =>
                        {
                            t.Span("อาจารย์ที่ปรึกษา: ").Bold();
                            t.Span(GetValue("AdvisorName"));
                        });

                        row.RelativeItem(2).Text(t =>
                        {
                            t.Span("เบอร์โทรอาจารย์: ").Bold();
                            t.Span(GetValue("AdvisorPhone"));
                        });
                    });
                });
            });
        }

        // ========================= PERSONAL SECTION =========================
        private void RenderPersonalSection(ColumnDescriptor col)
        {
            col.Item().ShowEntire().Border(1).Column(inner =>
            {
                inner.Item()
                    .Background(Colors.Grey.Lighten3)
                    .Padding(6)
                    .Text("ข้อมูลผู้สมัคร (Personal Details)")
                    .Bold();

                inner.Item().Padding(12).Column(content =>
                {
                    content.Spacing(8);

                    content.Item().Row(row =>
                    {
                        row.RelativeItem(3).Text(t =>
                        {
                            t.Span("ชื่อ-สกุล: ").Bold();
                            t.Span(GetValue("FullNameThai"));
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
                            t.Span("Email: ").Bold();
                            t.Span(GetValue("Email"));
                        });

                        row.RelativeItem(2).Text(t =>
                        {
                            t.Span("โทรศัพท์: ").Bold();
                            t.Span(GetValue("MobilePhone"));
                        });
                    });

                    content.Item().Row(row =>
                    {
                        row.RelativeItem(3).Text(t =>
                        {
                            t.Span("วันเกิด: ").Bold();
                            t.Span(GetValue("DateOfBirth"));
                        });

                        row.RelativeItem(2).Text(t =>
                        {
                            t.Span("อายุ: ").Bold();
                            t.Span(GetValue("Age"));
                            if (!string.IsNullOrWhiteSpace(GetValue("Age")))
                                t.Span(" ปี");
                        });
                    });

                    content.Item().Text(t =>
                    {
                        t.Span("ที่อยู่: ").Bold();
                        t.Span(GetValue("FullAddress"));
                    });
                });
            });
        }

        // ========================= REASON SECTION =========================
        private void RenderReasonSection(ColumnDescriptor col)
        {
            col.Item().ShowEntire().Border(1).Column(inner =>
            {
                inner.Item()
                    .Background(Colors.Grey.Lighten3)
                    .Padding(6)
                    .Text("หมายเหตุ / Remark")
                    .Bold();

                inner.Item().Padding(12).Column(content =>
                {
                    content.Spacing(10);

                    var reason = GetValue("Reason");
                    var reasonOther = GetValue("ReasonOther");

                    if (!string.IsNullOrWhiteSpace(reason) || !string.IsNullOrWhiteSpace(reasonOther))
                    {
                        content.Item().Text(t =>
                        {
                            t.Span("เหตุผลในการฝึกงาน: ").Bold();
                            t.Span(reason);
                            if (!string.IsNullOrWhiteSpace(reasonOther))
                                t.Span($" ({reasonOther})");
                        });
                    }
                    else
                    {
                        content.Item().PaddingTop(10).PaddingBottom(6).LineHorizontal(1);
                        content.Item().PaddingBottom(6).LineHorizontal(1);
                        content.Item().PaddingBottom(6).LineHorizontal(1);
                        content.Item().LineHorizontal(1);
                    }
                });
            });
        }

        // ========================= SIGNATURE SECTION =========================
        private void RenderSignatureSection(ColumnDescriptor col)
        {
            col.Item().ShowEntire().PaddingTop(14).Column(section =>
            {
                section.Item().LineHorizontal(1);
                section.Item().PaddingTop(24).Row(row =>
                {
                    row.Spacing(14);

                    RenderSignatureBox(row, GetValue("FullNameThai"), "ผู้สมัครฝึกงาน");
                    RenderSignatureBox(row, GetValue("AdvisorName"), "อาจารย์ที่ปรึกษา");
                    RenderSignatureBox(row, "", "ผู้รับผิดชอบฝึกงาน");
                });
            });
        }

        private void RenderSignatureBox(RowDescriptor row, string name, string position)
        {
            row.RelativeItem().PaddingHorizontal(8).Column(col =>
            {
                col.Item()
                    .Text("ลายเซ็น ................................................")
                    .FontSize(10);

                col.Item()
                    .PaddingTop(6)
                    .AlignCenter()
                    .Text(string.IsNullOrWhiteSpace(name) ? "(...................................)" : $"({name})")
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
            if (key == "FullNameThai")
            {
                var fullName = JoinValues("PrefixT", "NameFirstT", "NameLastT");
                if (!string.IsNullOrWhiteSpace(fullName))
                    return fullName;
            }

            if (!_form.TryGetValue(key, out var value) || value == null)
                return "";

            if ((key is "StartDate" or "EndDate" or "DateOfBirth") &&
                DateTime.TryParse(value.ToString(), out var date))
                return date.ToString("dd/MM/yyyy");

            return value.ToString() ?? "";
        }

        private string JoinValues(params string[] keys)
        {
            return string.Join(" ", keys
                .Select(key => _form.TryGetValue(key, out var value) ? value?.ToString() : null)
                .Where(value => !string.IsNullOrWhiteSpace(value)));
        }

        private int GetTrainingMonths()
        {
            if (int.TryParse(GetValue("DurationMonths"), out var durationMonths) && durationMonths > 0)
                return durationMonths;

            if (!DateTime.TryParse(GetValue("StartDateRaw"), out var startDate) ||
                !DateTime.TryParse(GetValue("EndDateRaw"), out var endDate) ||
                endDate.Date < startDate.Date)
                return 0;

            return Math.Max(1, (int)Math.Ceiling((endDate.Date - startDate.Date).TotalDays / 30d));
        }
    }
}
