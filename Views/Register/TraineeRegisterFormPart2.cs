using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace JobOnlineAPI.Views.Register
{
    public class TraineeRegisterFormPart2 : IDocument
    {
        private readonly IDictionary<string, object> _form;

        public TraineeRegisterFormPart2(IDictionary<string, object> form) => _form = form;

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        private string G(string key) =>
            _form.TryGetValue(key, out var v) && v != null ? v.ToString()! : "";

        private string Radio(bool on) => on ? "●" : "○";
        private string Check(bool on) => on ? "■" : "□";

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(16, Unit.Millimetre);
                page.MarginVertical(14, Unit.Millimetre);
                page.DefaultTextStyle(x => x
                    .FontFamily("DB Heavent")
                    .FontSize(10)
                    .LineHeight(1.15f)
                    .FontColor(Colors.Grey.Darken4));
                page.Content().Column(col =>
                {
                    col.Spacing(8);

                    // Keep the introduction together, so the photo and its related
                    // application details are not separated by a page break.
                    col.Item().ShowEntire().Column(intro =>
                    {
                        intro.Spacing(8);
                        intro.Item().Element(Header);
                        intro.Item().Element(DurationRow);
                    });

                    // Each section is small enough to fit on one page. ShowEntire
                    // moves it to the next page when the remaining space is too short.
                    col.Item().ShowEntire().Element(PersonalSection);
                    col.Item().ShowEntire().Element(EducationSection);
                    col.Item().ShowEntire().Element(ActivitiesAndSourcesSection);
                    col.Item().ShowEntire().Element(CertificationSection);
                    col.Item().ShowEntire().Element(ResultSection);
                    col.Item().ShowEntire().PaddingTop(2).Element(Footer);
                });
            });
        }

        // ==================== HEADER ====================
        private void Header(IContainer c)
        {
            var imgPath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "imagesform", "one_logo.png");
            c.Row(row =>
            {
                row.ConstantItem(80).AlignMiddle().Height(45).Image(imgPath).FitHeight();

                row.RelativeItem().AlignMiddle().Column(col =>
                {
                    col.Item().AlignCenter().Text("ใบสมัครนักศึกษาฝึกงาน").Bold().FontSize(15);
                    col.Item().PaddingTop(2).AlignCenter().Text("TRAINEE APPLICATION FORM")
                        .FontSize(10).FontColor(Colors.Grey.Darken1).LetterSpacing(0.5f);
                });

                row.ConstantItem(130).Border(1).BorderColor(Colors.Grey.Medium).Padding(5).Column(col =>
                {
                    col.Item().AlignCenter().Text("\"บริษัทฯ หรือ หน่วยงาน ที่ต้องการฝึกงาน\"").FontSize(8).Italic();
                    col.Item().PaddingTop(3).Text("☆ ......................................... ☆").FontSize(8).AlignCenter();
                    col.Item().PaddingTop(2).AlignRight().Text("ไม่ระบุ □").FontSize(8);
                });
            });
        }

        // ==================== DURATION ROW ====================
        private void DurationRow(IContainer c)
        {
            // Calculate the training duration from actual days and round up.
            int months = 0;
            if (DateTime.TryParse(G("StartDateRaw"), out var sd) &&
                DateTime.TryParse(G("EndDateRaw"), out var ed) && ed.Date >= sd.Date)
                months = Math.Max(1, (int)Math.Ceiling((ed.Date - sd.Date).TotalDays / 30d));

            c.Row(row =>
            {
                // Bottom alignment keeps this block directly above Personal Background
                // instead of leaving unused space beneath the application details.
                row.RelativeItem().AlignBottom().Column(col =>
                {
                    col.Item().Text(t =>
                    {
                        t.Span("ระยะเวลาฝึกงาน : ").Bold();
                        t.Span(months > 0 ? $"{months} เดือน" : "................................");
                    });
                    col.Item().PaddingTop(5).Row(r =>
                    {
                        r.AutoItem().Text(t =>
                        {
                            t.Span("เริ่มต้น ").Bold();
                            t.Span(string.IsNullOrEmpty(G("StartDate")) ? ".................................................." : G("StartDate"));
                        });
                        r.AutoItem().PaddingLeft(20).Text(t =>
                        {
                            t.Span("สิ้นสุด ").Bold();
                            t.Span(string.IsNullOrEmpty(G("EndDate")) ? ".................................................." : G("EndDate"));
                        });
                    });

                    // These fields share the space beside the photo, preventing
                    // the photo height from creating a large blank vertical gap.
                    col.Item().PaddingTop(8).Element(DesiredFieldsRow);
                    col.Item().PaddingTop(7).Element(ReasonRow);
                });

                // Standard 2-inch application photo area (approximately 4 x 5 cm).
                row.ConstantItem(46, Unit.Millimetre)
                    .PaddingLeft(6, Unit.Millimetre)
                    .AlignTop()
                    .Border(1.2f)
                    .BorderColor(Colors.Grey.Darken2)
                    .Height(50, Unit.Millimetre)
                    .Column(col =>
                {
                    col.Item().AlignCenter().PaddingTop(14).Text("ภาพถ่าย 2 นิ้ว").FontSize(10).Bold();
                    col.Item().AlignCenter().PaddingTop(2).Text("2-INCH PHOTO")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                    col.Item().AlignCenter().PaddingTop(10)
                        .Text("[ ชาย  /  หญิง ]").FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        }

        // ==================== DESIRED FIELDS ====================
        private void DesiredFieldsRow(IContainer c)
        {
            var desiredFields = new[]
            {
                G("DesiredField1"),
                G("DesiredField2"),
                G("DesiredField3")
            }.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            c.Column(col =>
            {
                col.Item().Text("มีความประสงค์ขอฝึกงานเกี่ยวกับสาขาวิชา :").Bold();
                col.Item().PaddingTop(4).Text(t =>
                {
                    if (desiredFields.Length == 0)
                    {
                        t.Span("............................................................................");
                        return;
                    }

                    for (var index = 0; index < desiredFields.Length; index++)
                    {
                        t.Span($"{index + 1})  ").Bold();
                        t.Span(desiredFields[index]);

                        if (index < desiredFields.Length - 1)
                            t.Span("     ");
                    }
                });
            });
        }

        // ==================== REASON ====================
        private void ReasonRow(IContainer c)
        {
            var reason = G("Reason").ToLower();
            bool isCourse = reason.Contains("course") || reason.Contains("วิชา");
            bool isCooperative = reason.Contains("cooperative") || reason.Contains("experience") || reason.Contains("ประสบการณ์");
            bool isOther = !isCourse && !isCooperative && !string.IsNullOrEmpty(reason);

            c.Column(col =>
            {
                col.Item().Row(r =>
                {
                    r.AutoItem().Text("สาเหตุของการฝึกงาน : ").Bold();
                    r.AutoItem().PaddingLeft(8).Text($"{Radio(isCourse)}  เป็นส่วนหนึ่งของวิชาเรียน");
                    r.AutoItem().PaddingLeft(8).Text($"{Radio(isCooperative)}  เพื่อเสริมสร้างประสบการณ์");
                });
                col.Item().PaddingTop(3).Text(t =>
                {
                    t.Span($"{Radio(isOther)}  อื่นๆ ระบุ ");
                    t.Span(isOther ? G("ReasonOther") : "............................................................................");
                });
            });
        }

        // ==================== PERSONAL SECTION ====================
        private void PersonalSection(IContainer c)
        {
            var fatherAlive = G("FatherStatus").ToLower().Contains("alive");
            var motherAlive = G("MotherStatus").ToLower().Contains("alive");

            c.Column(col =>
            {
                col.Item().Background(Colors.Grey.Lighten3).Padding(3)
                    .Text("ประวัติส่วนตัว Personal Background :").Bold();

                // ชื่อ / นามสกุล / ชื่อเล่น
                col.Item().PaddingTop(3).Table(t =>
                {
                    t.ColumnsDefinition(d => { d.RelativeColumn(3); d.RelativeColumn(3); d.RelativeColumn(2); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("ชื่อ (นาย,นางสาว)  ").Bold(); tt.Span(G("Title") + " " + G("FirstNameThai")); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("นามสกุล  ").Bold(); tt.Span(G("LastNameThai")); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("ชื่อเล่น  ").Bold(); tt.Span(G("Nickname")); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(1)
                        .Text(tt => { tt.Span("Name  ").FontColor(Colors.Grey.Darken1); tt.Span(G("FirstNameEng")); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(1)
                        .Text(tt => { tt.Span("Surname  ").FontColor(Colors.Grey.Darken1); tt.Span(G("LastNameEng")); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(1)
                        .Text(tt => { tt.Span("Nickname  ").FontColor(Colors.Grey.Darken1); tt.Span(G("NicknameE")); });
                });

                // วันเกิด / อายุ / สถานที่เกิด
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(d => { d.RelativeColumn(3); d.RelativeColumn(2); d.RelativeColumn(3); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("วัน-เดือน-ปีเกิด  ").Bold(); tt.Span(G("DateOfBirth")); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("อายุ  ").Bold(); tt.Span(G("Age")); tt.Span("  ปี"); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("สถานที่เกิด  ").Bold(); tt.Span(G("PlaceOfBirth")); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(1)
                        .Text("Date of Birth").FontColor(Colors.Grey.Darken1);
                    t.Cell().BorderBottom(0.5f).PaddingVertical(1)
                        .Text("Age").FontColor(Colors.Grey.Darken1);
                    t.Cell().BorderBottom(0.5f).PaddingVertical(1)
                        .Text("Place of Birth").FontColor(Colors.Grey.Darken1);
                });

                // สัญชาติ / เชื้อชาติ / ศาสนา / ส่วนสูง / น้ำหนัก
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(d =>
                    {
                        d.RelativeColumn(2); d.RelativeColumn(2);
                        d.RelativeColumn(2); d.RelativeColumn(2); d.RelativeColumn(2);
                    });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("สัญชาติ  ").Bold(); tt.Span(G("Nationality")); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("เชื้อชาติ  ").Bold(); tt.Span(G("Race")); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("ศาสนา  ").Bold(); tt.Span(G("Religion")); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("ส่วนสูง  ").Bold(); tt.Span(G("Height")); tt.Span("  ซม."); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("น้ำหนัก  ").Bold(); tt.Span(G("Weight")); tt.Span("  กก."); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(1).Text("Nationality").FontColor(Colors.Grey.Darken1);
                    t.Cell().BorderBottom(0.5f).PaddingVertical(1).Text("Race").FontColor(Colors.Grey.Darken1);
                    t.Cell().BorderBottom(0.5f).PaddingVertical(1).Text("Religion").FontColor(Colors.Grey.Darken1);
                    t.Cell().BorderBottom(0.5f).PaddingVertical(1).Text("Cm.").FontColor(Colors.Grey.Darken1);
                    t.Cell().BorderBottom(0.5f).PaddingVertical(1).Text("Kg.").FontColor(Colors.Grey.Darken1);
                });

                // บัตรประชาชน
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(d => { d.RelativeColumn(3); d.RelativeColumn(3); d.RelativeColumn(2); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("บัตรประชาชนเลขที่  ").Bold(); tt.Span(G("IDCardNo")); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("ออกโดย  ").Bold(); tt.Span(G("IDIssuedBy")); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("หมดอายุ  ").Bold(); tt.Span(G("IDExpiredDate")); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(1).Text("ID Card No.").FontColor(Colors.Grey.Darken1);
                    t.Cell().BorderBottom(0.5f).PaddingVertical(1).Text("Issued by").FontColor(Colors.Grey.Darken1);
                    t.Cell().BorderBottom(0.5f).PaddingVertical(1).Text("Expired Date.").FontColor(Colors.Grey.Darken1);
                });

                // ที่อยู่
                col.Item().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                { tt.Span("ที่อยู่ปัจจุบัน  ").Bold(); tt.Span(G("FullAddress")); });
                col.Item().BorderBottom(0.5f).PaddingVertical(1)
                    .Text("Place of Address").FontColor(Colors.Grey.Darken1);

                // โทรศัพท์ / มือถือ / อีเมล์
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(d => { d.RelativeColumn(); d.RelativeColumn(); d.RelativeColumn(); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("โทรศัพท์  ").Bold(); tt.Span(G("HomePhone")); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("มือถือ  ").Bold(); tt.Span(G("MobilePhone")); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("อีเมล์  ").Bold(); tt.Span(G("Email")); });
                });

                // บิดา
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(d => { d.RelativeColumn(3); d.RelativeColumn(3); d.RelativeColumn(2); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("ชื่อบิดา  ").Bold(); tt.Span(G("FatherName")); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("อาชีพ  ").Bold(); tt.Span(G("FatherOccupation")); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Row(r =>
                    {
                        r.AutoItem().Text($"{Check(fatherAlive)} ยังคงมีชีวิตอยู่  ").FontSize(9);
                        r.AutoItem().PaddingLeft(12).Text($"{Check(!fatherAlive)} ถึงแก่กรรม").FontSize(9);
                    });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(1).Text("Father 's name").FontColor(Colors.Grey.Darken1);
                    t.Cell().BorderBottom(0.5f).PaddingVertical(1).Text("Occupation").FontColor(Colors.Grey.Darken1);
                    t.Cell().BorderBottom(0.5f).PaddingVertical(1).Row(r =>
                    {
                        r.AutoItem().Text("Alive").FontSize(9).FontColor(Colors.Grey.Darken1);
                        r.AutoItem().PaddingLeft(28).Text("Demised").FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                });

                // มารดา
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(d => { d.RelativeColumn(3); d.RelativeColumn(3); d.RelativeColumn(2); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("ชื่อมารดา  ").Bold(); tt.Span(G("MotherName")); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("อาชีพ  ").Bold(); tt.Span(G("MotherOccupation")); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Row(r =>
                    {
                        r.AutoItem().Text($"{Check(motherAlive)} ยังคงมีชีวิตอยู่  ").FontSize(9);
                        r.AutoItem().PaddingLeft(12).Text($"{Check(!motherAlive)} ถึงแก่กรรม").FontSize(9);
                    });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(1).Text("Mother 's name").FontColor(Colors.Grey.Darken1);
                    t.Cell().BorderBottom(0.5f).PaddingVertical(1).Text("Occupation").FontColor(Colors.Grey.Darken1);
                    t.Cell().BorderBottom(0.5f).PaddingVertical(1).Row(r =>
                    {
                        r.AutoItem().Text("Alive").FontSize(9).FontColor(Colors.Grey.Darken1);
                        r.AutoItem().PaddingLeft(28).Text("Demised").FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                });

                // พี่น้อง
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(d => { d.RelativeColumn(); d.RelativeColumn(); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("มีพี่น้องจำนวน  ").Bold(); tt.Span(G("SiblingsAll")); tt.Span("  คน"); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("ท่านเป็นคนที่  ").Bold(); tt.Span(G("SiblingOrder")); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(1).Text("Brother/Sister (S)").FontColor(Colors.Grey.Darken1);
                    t.Cell().BorderBottom(0.5f).PaddingVertical(1).Text("You are No.").FontColor(Colors.Grey.Darken1);
                });

                // Emergency Contact
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(d => { d.RelativeColumn(2); d.RelativeColumn(3); d.RelativeColumn(2); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text("บุคคลที่ติดต่อได้ในกรณีฉุกเฉิน").Bold();
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("ชื่อ  ").Bold(); tt.Span(G("EmergencyName")); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("ความสัมพันธ์  ").Bold(); tt.Span(G("EmergencyRelation")); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(1).Text("Emergency Contact").FontColor(Colors.Grey.Darken1);
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("ที่อยู่  ").Bold(); tt.Span(G("EmergencyAddress")); });
                    t.Cell().BorderBottom(0.5f).PaddingVertical(2).Text(tt =>
                    { tt.Span("โทรศัพท์  ").Bold(); tt.Span(G("EmergencyPhone")); });
                });
            });
        }

        // ==================== EDUCATION SECTION ====================
        private void EducationSection(IContainer c)
        {
            c.Column(col =>
            {
                col.Item().Background(Colors.Grey.Lighten3).Padding(3)
                    .Text("ประวัติการศึกษา / Education Background").Bold();

                col.Item().PaddingTop(4).BorderBottom(0.5f).PaddingVertical(2).Text(t =>
                {
                    t.Span("ชื่อสถานศึกษา  ").Bold(); t.Span(G("School") + "  ");
                    t.Span("คณะ  ").Bold(); t.Span(G("Faculty") + "  ");
                    t.Span("วิชาเอก  ").Bold(); t.Span(G("Major") + "  ");
                    t.Span("วิชาโท  ").Bold(); t.Span(G("Minor"));
                });

                col.Item().PaddingTop(3).Text(t =>
                {
                    t.Span("ศึกษาชั้นปีที่  ").Bold(); t.Span(G("YearOfStudy") + "  ");
                    t.Span("อาจารย์ที่ปรึกษา  ").Bold(); t.Span(G("AdvisorName") + "  ");
                    t.Span("โทร  ").Bold(); t.Span(G("AdvisorPhone"));
                });
            });
        }

        // ==================== ACTIVITIES + INFO SOURCES ====================
        private void ActivitiesAndSourcesSection(IContainer c)
        {
            var sources = G("InfoSources").ToLower();
            bool srcWebsite = sources.Contains("website");
            bool srcInstitute = sources.Contains("institute") || sources.Contains("สถาบัน");
            bool srcBoard = sources.Contains("board") || sources.Contains("ประกาศ");
            bool srcStaff = sources.Contains("staff") || sources.Contains("บุคลากร") || !string.IsNullOrEmpty(G("InfoSourceStaffName"));
            bool srcOther = sources.Contains("other") || sources.Contains("อื่น");

            c.Row(row =>
            {
                // กิจกรรม
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("กิจกรรมพิเศษชมรมศึกษา (ระบุ) /Association Activity :").Bold().FontSize(9);
                    var act = G("Activities");
                    if (!string.IsNullOrEmpty(act))
                    {
                        col.Item().PaddingTop(4).Text(act).FontSize(9);
                    }
                    else
                    {
                        col.Item().PaddingTop(10).LineHorizontal(0.5f);
                        col.Item().PaddingTop(10).LineHorizontal(0.5f);
                        col.Item().PaddingTop(10).LineHorizontal(0.5f);
                    }
                });

                row.ConstantItem(12);

                // แหล่งข้อมูล
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("ทราบข้อมูลการรับสมัครนักศึกษาฝึกงานจาก :").Bold().FontSize(9);
                    col.Item().PaddingTop(3).Text($"{Check(srcWebsite)} เว็บไซต์ GMM Grammy").FontSize(9);
                    col.Item().PaddingTop(2).Text($"{Check(srcInstitute)} เว็บไซต์ของสถาบัน").FontSize(9);
                    col.Item().PaddingTop(2).Text($"{Check(srcBoard)} แผ่นประกาศในมหาวิทยาลัย").FontSize(9);
                    col.Item().PaddingTop(2).Text(t =>
                    {
                        t.Span($"{Check(srcStaff)} บุคลากรภายในบริษัทฯ ชื่อ ").FontSize(9);
                        t.Span(G("InfoSourceStaffName")).FontSize(9);
                        t.Span("  หน่วยงาน ").FontSize(9);
                        t.Span(G("InfoSourceDepartment")).FontSize(9);
                    });
                    col.Item().PaddingTop(2).Text(t =>
                    {
                        t.Span($"{Check(srcOther)} อื่นๆ ").FontSize(9);
                        t.Span(srcOther ? G("InfoSourceOther") : "........................................").FontSize(9);
                    });
                });
            });
        }

        // ==================== CERTIFICATION ====================
        private void CertificationSection(IContainer c)
        {
            c.Column(col =>
            {
                col.Item().AlignCenter().Text("ข้าพเจ้ารับรองว่า ข้อมูลข้างต้นเป็นความจริงทุกประการ").Bold();
                col.Item().PaddingTop(20).Row(row =>
                {
                    row.RelativeItem();
                    row.AutoItem().Column(right =>
                    {
                        right.Item().Text("ลงชื่อ..............................................ผู้สมัคร");
                        right.Item().PaddingTop(6).AlignCenter().Text("วันที่......./........./.........");
                    });
                });
            });
        }

        // ==================== RESULT ====================
        private void ResultSection(IContainer c)
        {
            c.Border(1).Padding(8).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("ผลการพิจารณานักศึกษาฝึกงาน").Bold();
                    col.Item().PaddingTop(5).Text("□  รับ");
                    col.Item().PaddingTop(4).Text("□  ไม่รับ");
                    col.Item().PaddingTop(4).Text("□  อื่นๆ .................................");
                });
                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text("ลงชื่อ..............................................ผู้อนุมัติ");
                    col.Item().PaddingTop(6).Text("วันที่......./........./.........");
                });
            });
        }

        // ==================== FOOTER ====================
        private void Footer(IContainer c)
        {
            var imgPath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "imagesform", "one_logo.png");
            c.Column(col =>
            {
                col.Item().LineHorizontal(0.5f);
                col.Item().Row(row =>
                {
                    row.ConstantItem(35).PaddingTop(4).Height(18).Image(imgPath).FitHeight();
                    row.RelativeItem().AlignMiddle().PaddingLeft(4)
                        .Text("*** แนบเอกสารประกอบการฝึกงาน เช่น สำเนาบัตรนักศึกษา / เอกสารจากทางมหาวิทยาลัย / Profile ฯลฯ เพื่อประกอบการพิจารณา***")
                        .FontSize(9.5f).SemiBold().LineHeight(1.2f);
                });
            });
        }
    }
}
