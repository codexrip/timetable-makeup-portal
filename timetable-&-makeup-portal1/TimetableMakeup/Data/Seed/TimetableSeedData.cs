namespace TimetableMakeup.Data.Seed;

public record SectionSeed(string Name, int Semester);
// Slot 1 = 08:00 ... Slot 9 = 16:00. Hours = consecutive hourly slots.
public record SessionSeed(string Section, DayOfWeek Day, int Slot, int Hours, string Course, string Teacher, string Room);

public static class TimetableSeedData
{
    const DayOfWeek Mo = DayOfWeek.Monday, Tu = DayOfWeek.Tuesday, We = DayOfWeek.Wednesday,
                    Th = DayOfWeek.Thursday, Fr = DayOfWeek.Friday;

    // ---------- Rooms ----------
    const string PhyLab = "Basic Physics Lab C Block (5th Floor)";
    const string L5 = "Lab 5 A Block", L6 = "Lab 6 A Block";
    const string L12 = "Lab 12 C Block (2nd Floor)", L13 = "Lab 13 C Block (3rd Floor)";
    const string LH1 = "Lecture Hall 01";

    // ---------- Courses ----------
    const string AppPhy = "Applied Physics", AppPhyLab = "Applied Physics Lab";
    const string PakStd = "Pakistan Studies", FuncEng = "Functional English";
    const string PF = "Programming Fundamentals", PFLab = "Programming Fundamentals Lab";
    const string ICT = "Application of Information and Communication Technologies";
    const string ICTLab = "Application of Information and Communication Technologies Lab";
    const string Fehm = "Fehm-e-Quran I", FMath = "Foundational Mathematics", Adv = "Advisory Class";
    const string MC = "Mobile Computing", MCLab = "Mobile Computing Lab";
    const string PnS = "Probability and Statistics";
    const string IS = "Information Security", ISLab = "Information Security Lab", ISAdv = "Information Security / Advisory Class";
    const string DS = "Data Structures", DSLab = "Data Structures Lab";
    const string Calc = "Calculus and Analytical Geometry", LA = "Linear Algebra";
    const string OS = "Operating Systems", OSLab = "Operating Systems Lab", OSAdv = "Operating Systems / Advisory Class";
    const string FSWD = "Full Stack Web Development", FSWDLab = "Full Stack Web Development Lab";
    const string DAA = "Design and Analysis of Algorithms";
    const string AI = "Artificial Intelligence", AILab = "Artificial Intelligence Lab";
    const string IM = "Introduction to Management";
    const string COAL = "Computer Organization and Assembly Language", COALLab = "Computer Organization and Assembly Language Lab";
    const string SNA = "Systems and Network Administration", SNALab = "Systems and Network Administration Lab";
    const string VP = "Visual Programming", VPLab = "Visual Programming Lab";

    // ---------- Teachers ----------
    const string Alina = "Ms. Alina Zahid", Saima = "Dr. Saima Rafique", Akbar = "Dr. Muhammad Akbar",
                 SaadA = "Dr. Saad Ahmed", PhyNew = "Physics New Lecturer-II", Rozina = "Ms. Rozina Qureshi",
                 Musharaf = "Dr. Musharaf Zahoor", Azeem = "Mr. Azeem Usmani", Razia = "Ms. Razia Shaheen",
                 Wahid = "Dr. Abdul Wahid", Sadia = "Dr. Sadia Ahsan", Waqas = "Dr. Waqas Sarwar Abbasi",
                 Tanveer = "Dr. Tanveer Kifayat", Lec2 = "Maths-Lecturer-2", Lec3 = "Maths-Lecturer-3",
                 Lec4 = "Maths-Lecturer-4", Zameer = "Dr. Zameer Ul Hassan", Fahad = "Dr. Fahad Tahir",
                 Kanwal = "Ms. Kanwal Ejaz", Eaman = "Ms. Eaman Fatima", Hareem = "Ms. Hareem Kibriya",
                 Fizza = "Ms. Fizza Asmat", Warda = "Ms. Warda Aslam", Ashfaq = "Dr. Ashfaq Hussain",
                 SaraM = "Ms. Sara Mishal", Junaid = "Mr. Muhammad Junaid Nazar", Faryal = "Ms. Faryal Farooq",
                 Waseeqa = "Ms. Waseeqa Ghazanfer", Alia = "Ms. Alia Naseem", Ayaz = "Mr. M Ayaz Khan",
                 Amna = "Ms. Amna Tariq", Sumera = "Dr. Sumera Hayat Khan", Wahab = "Mr. Wahab Ali",
                 Sohaib = "Mr. Sohaib Masood", Obiad = "Mr. Obiad ullah Khan", Mehdi = "Dr. Mehdi Hassan",
                 Hafiz = "Dr. Hafiz Ahmad Jalal",
                 // BSIT-5B only
                 Khurram = "Dr. Khurram Khan", SaraI = "Ms. Sara Ibrahim", Yasir = "Mr. Muhammad Yasir Ali",
                 Romana = "Ms. Romana Maroof", MathsII = "Maths Lecturer-II";

    static SessionSeed S(string sec, DayOfWeek d, int slot, int hrs, string course, string teacher, string room)
        => new(sec, d, slot, hrs, course, teacher, room);

    public static readonly SectionSeed[] Sections =
    {
        new("BSCS-1A", 1), new("BSCS-1B", 1), new("BSCS-1C", 1),
        new("BSCS-3A", 3), new("BSCS-3B", 3), new("BSCS-3C", 3),
        new("BSCS-5A", 5), new("BSCS-5B", 5), new("BSIT-5B", 5),
    };

    public static readonly SessionSeed[] Sessions =
    {
        // ===================== BSIT-5B (original 1-hour grid) =====================
        S("BSIT-5B", Mo, 1, 1, IM,     Khurram, "C-206"),
        S("BSIT-5B", Mo, 2, 1, MC,     Faryal,  "C-301"),
        S("BSIT-5B", Mo, 3, 1, LA,     MathsII, "C-302"),
        S("BSIT-5B", Mo, 4, 1, VP,     SaraI,   "C-202"),
        S("BSIT-5B", Mo, 7, 3, SNALab, Yasir,   L13),
        S("BSIT-5B", Tu, 1, 1, IM,     Khurram, "C-203"),
        S("BSIT-5B", Tu, 2, 1, COAL,   Waseeqa, LH1),
        S("BSIT-5B", Tu, 3, 1, SNA,    Yasir,   "C-205"),
        S("BSIT-5B", Tu, 5, 1, LA,     MathsII, "C-306"),
        S("BSIT-5B", Tu, 6, 1, VP,     SaraI,   "C-201"),
        S("BSIT-5B", We, 1, 3, COALLab,Romana,  L5),
        S("BSIT-5B", We, 6, 1, SNA,    Yasir,   "C-201"),
        S("BSIT-5B", Th, 1, 3, MCLab,  Faryal,  L12),
        S("BSIT-5B", Th, 5, 1, Adv,    Sumera,  "C-305"),
        S("BSIT-5B", Th, 6, 1, MC,     Faryal,  "C-205"),
        S("BSIT-5B", Fr, 1, 3, VPLab,  SaraI,   L6),
        S("BSIT-5B", Fr, 4, 1, LA,     MathsII, "C-203"),
        S("BSIT-5B", Fr, 5, 1, COAL,   Waseeqa, LH1),

        // ===================== BSCS-1A =====================
        S("BSCS-1A", Mo, 1, 2, AppPhyLab, Alina,    PhyLab),
        S("BSCS-1A", Mo, 4, 1, PakStd,    Musharaf, "CYS-203"),
        S("BSCS-1A", Mo, 6, 1, AppPhy,    Saima,    "CYS-203"),
        S("BSCS-1A", Tu, 1, 1, FuncEng,   Rozina,   "CS-301"),
        S("BSCS-1A", Tu, 3, 2, ICTLab,    Kanwal,   L13),
        S("BSCS-1A", Tu, 6, 1, PF,        Fahad,    "CYS-204"),
        S("BSCS-1A", We, 1, 2, Fehm,      Azeem,    "CYS-203"),
        S("BSCS-1A", We, 3, 1, PF,        Fahad,    "CYS-203"),
        S("BSCS-1A", We, 4, 1, PakStd,    Musharaf, "CYS-204"),
        S("BSCS-1A", We, 7, 1, FMath,     Lec2,     "CS-301"),
        S("BSCS-1A", Th, 2, 1, ICT,       Kanwal,   "CYS-206"),
        S("BSCS-1A", Th, 3, 2, PFLab,     Eaman,    L13),
        S("BSCS-1A", Th, 6, 1, AppPhy,    Saima,    "CYS-203"),
        S("BSCS-1A", Fr, 2, 1, FuncEng,   Rozina,   "CYS-205"),
        S("BSCS-1A", Fr, 3, 1, Adv,       Hareem,   "CYS-203"),
        S("BSCS-1A", Fr, 4, 1, ICT,       Kanwal,   "CS-306"),
        S("BSCS-1A", Fr, 7, 1, FMath,     Lec2,     "CS-301"),

        // ===================== BSCS-1B =====================
        S("BSCS-1B", Mo, 1, 1, FuncEng,   Rozina,   "CS-304"),
        S("BSCS-1B", Mo, 2, 1, PakStd,    Musharaf, "CYS-201"),
        S("BSCS-1B", Mo, 3, 2, ICTLab,    Kanwal,   L13),
        S("BSCS-1B", Mo, 6, 1, PF,        Fahad,    "CS-302"),
        S("BSCS-1B", Tu, 2, 1, AppPhy,    Akbar,    "CYS-205"),
        S("BSCS-1B", Tu, 3, 2, Fehm,      Azeem,    "CYS-204"),
        S("BSCS-1B", Tu, 6, 2, AppPhyLab, Alina,    PhyLab),
        S("BSCS-1B", We, 2, 1, ICT,       Kanwal,   "CYS-206"),
        S("BSCS-1B", We, 3, 1, FuncEng,   Rozina,   "CYS-204"),
        S("BSCS-1B", We, 4, 1, PF,        Fahad,    "CYS-202"),
        S("BSCS-1B", We, 7, 1, FMath,     Lec3,     "CS-302"),
        S("BSCS-1B", Th, 1, 2, PFLab,     Fizza,    L13),
        S("BSCS-1B", Th, 4, 1, AppPhy,    Akbar,    "CS-305"),
        S("BSCS-1B", Th, 6, 1, ICT,       Kanwal,   "CYS-206"),
        S("BSCS-1B", Fr, 1, 1, Adv,       Warda,    "CYS-201"),
        S("BSCS-1B", Fr, 2, 1, PakStd,    Musharaf, "CYS-206"),
        S("BSCS-1B", Fr, 7, 1, FMath,     Lec3,     "CS-302"),

        // ===================== BSCS-1C =====================
        S("BSCS-1C", Mo, 2, 1, FuncEng,   Rozina,   "CYS-203"),
        S("BSCS-1C", Mo, 3, 1, AppPhy,    SaadA,    "CYS-205"),
        S("BSCS-1C", Mo, 4, 1, PF,        Ashfaq,   "CYS-204"),
        S("BSCS-1C", Mo, 6, 2, AppPhyLab, PhyNew,   PhyLab),
        S("BSCS-1C", Tu, 1, 2, ICTLab,    SaraM,    L13),
        S("BSCS-1C", Tu, 4, 1, Adv,       Junaid,   "CYS-205"),
        S("BSCS-1C", We, 2, 1, FuncEng,   Rozina,   "CYS-205"),
        S("BSCS-1C", We, 3, 2, Fehm,      Razia,    "CYS-206"),
        S("BSCS-1C", We, 6, 1, PakStd,    Musharaf, "CYS-203"),
        S("BSCS-1C", We, 7, 1, FMath,     Lec4,     "CS-303"),
        S("BSCS-1C", Th, 1, 1, ICT,       SaraM,    "CYS-205"),
        S("BSCS-1C", Th, 2, 1, AppPhy,    SaadA,    "CYS-203"),
        S("BSCS-1C", Th, 3, 1, PF,        Ashfaq,   "CYS-204"),
        S("BSCS-1C", Th, 4, 1, ICT,       SaraM,    "CYS-203"),
        S("BSCS-1C", Fr, 1, 2, PFLab,     Fizza,    L13),
        S("BSCS-1C", Fr, 3, 1, PakStd,    Musharaf, "CYS-205"),
        S("BSCS-1C", Fr, 7, 1, FMath,     Lec4,     "CS-303"),

        // ===================== BSCS-3A =====================
        S("BSCS-3A", Mo, 3, 1, PnS,    Wahid,   "CS-304"),
        S("BSCS-3A", Mo, 5, 1, MC,     Faryal,  "CYS-205"),   // moved from slot 2 (clash with BSIT-5B)
        S("BSCS-3A", Mo, 6, 2, ISLab,  Waseeqa, L12),
        S("BSCS-3A", Tu, 1, 2, MCLab,  Faryal,  L5),
        S("BSCS-3A", Tu, 4, 1, DS,     Ashfaq,  "CYS-206"),
        S("BSCS-3A", Tu, 6, 1, IS,     Waseeqa, "CYS-205"),
        S("BSCS-3A", We, 1, 1, MC,     Faryal,  "CS-303"),
        S("BSCS-3A", We, 3, 2, DSLab,  Alia,    L6),
        S("BSCS-3A", Th, 2, 1, Calc,   Sadia,   "CS-302"),
        S("BSCS-3A", Th, 3, 1, PnS,    Wahid,   "CS-304"),
        S("BSCS-3A", Fr, 2, 1, Calc,   Sadia,   "CS-302"),
        S("BSCS-3A", Fr, 3, 1, DS,     Ashfaq,  "CS-301"),
        S("BSCS-3A", Fr, 4, 1, ISAdv,  Waseeqa, "CS-303"),

        // ===================== BSCS-3B =====================
        S("BSCS-3B", Mo, 2, 1, PnS,    Wahid,   "CYS-204"),
        S("BSCS-3B", Mo, 3, 2, ISLab,  Ayaz,    L6),
        S("BSCS-3B", Tu, 3, 2, MCLab,  Amna,    L5),
        S("BSCS-3B", We, 1, 1, MC,     SaraM,   "CS-302"),
        S("BSCS-3B", We, 4, 2, DSLab,  Sumera,  L5),          // moved from slot 3 (Lab 5 busy)
        S("BSCS-3B", Th, 2, 1, IS,     Ayaz,    "CS-305"),
        S("BSCS-3B", Th, 3, 1, Calc,   Sadia,   "CS-302"),
        S("BSCS-3B", Th, 4, 1, DS,     Sumera,  "CS-304"),
        S("BSCS-3B", Th, 6, 1, PnS,    Wahid,   "CS-303"),
        S("BSCS-3B", Fr, 1, 1, ISAdv,  Ayaz,    "CYS-204"),
        S("BSCS-3B", Fr, 2, 1, DS,     Sumera,  "CS-303"),
        S("BSCS-3B", Fr, 3, 1, Calc,   Sadia,   "CS-302"),
        S("BSCS-3B", Fr, 4, 1, MC,     SaraM,   "CYS-205"),

        // ===================== BSCS-3C =====================
        S("BSCS-3C", Mo, 1, 2, MCLab,  SaraM,   L5),
        S("BSCS-3C", Mo, 6, 1, ISAdv,  Ayaz,    "CYS-201"),
        S("BSCS-3C", Tu, 2, 1, IS,     Ayaz,    "CS-302"),
        S("BSCS-3C", Tu, 3, 1, MC,     SaraM,   "CS-302"),
        S("BSCS-3C", Tu, 6, 2, DSLab,  Wahab,   L5),
        S("BSCS-3C", We, 2, 1, Calc,   Lec2,    "CS-302"),
        S("BSCS-3C", We, 3, 1, DS,     Wahab,   "CYS-205"),
        S("BSCS-3C", We, 6, 2, ISLab,  Waseeqa, L5),
        S("BSCS-3C", Th, 4, 1, PnS,    Tanveer, "CS-306"),
        S("BSCS-3C", Th, 6, 1, Calc,   Lec2,    "CS-302"),
        S("BSCS-3C", Fr, 2, 1, PnS,    Tanveer, "CS-301"),
        S("BSCS-3C", Fr, 3, 1, MC,     SaraM,   "CS-306"),
        S("BSCS-3C", Fr, 4, 1, DS,     Wahab,   "CS-302"),

        // ===================== BSCS-5A =====================
        S("BSCS-5A", Mo, 2, 1, LA,     Waqas,   "CS-304"),
        S("BSCS-5A", Mo, 3, 2, OSLab,  Sohaib,  L12),
        S("BSCS-5A", Mo, 6, 1, FSWD,   Obiad,   "CS-301"),
        S("BSCS-5A", Tu, 1, 2, FSWDLab,Obiad,   L6),
        S("BSCS-5A", Tu, 4, 1, DAA,    Mehdi,   "CS-304"),
        S("BSCS-5A", Tu, 6, 1, AI,     Hafiz,   "CS-305"),
        S("BSCS-5A", We, 1, 2, AILab,  Amna,    L6),
        S("BSCS-5A", We, 4, 1, OS,     Sohaib,  "CS-303"),
        S("BSCS-5A", We, 6, 1, FSWD,   Obiad,   "CS-306"),
        S("BSCS-5A", Th, 2, 1, LA,     Waqas,   "CYS-204"),
        S("BSCS-5A", Th, 3, 1, AI,     Hafiz,   "CS-305"),
        S("BSCS-5A", Th, 4, 1, DAA,    Mehdi,   "CS-301"),
        S("BSCS-5A", Th, 6, 1, IM,     Zameer,  "CYS-205"),
        S("BSCS-5A", Fr, 1, 1, IM,     Zameer,  "CS-306"),
        S("BSCS-5A", Fr, 2, 1, OSAdv,  Sohaib,  "CYS-203"),

        // ===================== BSCS-5B =====================
        S("BSCS-5B", Mo, 1, 1, OSAdv,  Obiad,   "CS-305"),
        S("BSCS-5B", Mo, 2, 1, DAA,    Junaid,  "CS-305"),
        S("BSCS-5B", Mo, 3, 1, LA,     Waqas,   "CS-301"),
        S("BSCS-5B", Mo, 6, 1, FSWD,   Fizza,   "CS-305"),
        S("BSCS-5B", Tu, 1, 1, FSWD,   Fizza,   "CS-305"),
        S("BSCS-5B", Tu, 3, 2, FSWDLab,Fizza,   L6),
        S("BSCS-5B", We, 3, 1, OS,     Obiad,   "CS-301"),
        S("BSCS-5B", We, 6, 2, AILab,  Hareem,  L12),
        S("BSCS-5B", Th, 1, 1, AI,     Hareem,  "CS-305"),
        S("BSCS-5B", Th, 2, 1, DAA,    Junaid,  "CS-304"),
        S("BSCS-5B", Th, 3, 1, LA,     Waqas,   "CYS-206"),
        S("BSCS-5B", Th, 4, 1, IM,     Zameer,  "CYS-206"),
        S("BSCS-5B", Fr, 1, 2, OSLab,  Obiad,   L12),
        S("BSCS-5B", Fr, 3, 1, IM,     Zameer,  "CYS-206"),
        S("BSCS-5B", Fr, 4, 1, AI,     Hareem,  "CS-301"),
    };
}
