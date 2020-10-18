using System;


namespace Basic_Script_Interpreter
{
    public class Imports
    {
        public string Status { get; set; }


        public double Hour { get; set; } = 8; // Arbeitszeit

        public double HourExact { get; set; } = 8; // Arbeitszeit
        public double HourWithoutAddition { get; set; } = 8; // Arbeitszeit ohne Zusatz
        public double HourWithAddition { get; set; } = 0; // Arbeitszeit mit Zusatz
        public double HourAddition { get; set; } = 0; // Zusatz
        public int BlockShiftNumber { get; set; } = 2;
        public int ShiftType { get; set; } = 3;
        public int WeekdayNumber { get; set; } = 2;
        public double NightHour { get; set; } = 7;
        public double NighthourAdditionalPercent { get; set; } = 0.1;
        public double HolydayhourAdditionalPercent { get; set; } = 0.1;
        
        public double Holydayhour { get; set; } = 1;
        public double Daybefor_Holydayhour { get; set; } = 7;
        public int BlockNumber { get; set; } = 2;
        public int LastType { get; set; }
        public bool IsHolyday { get; set; } = false;
        public bool IsNextDayHolyday { get; set; } = false;
        public int BlockShiftIndex { get; set; } = 2;
        public double GuaranteedHours { get; set; } = 160;

        public double Hour_after_Night { get; set; } = 1;
        public double Hour_befor_Night { get; set; } = 0;
        public double NightHour_befor_Midnight { get; set; } = 1;
        public double Result { get; set; } = 0;

        public string SubHourValueTag { get; set; } = string.Empty;

        public string TokenEarlyShift { get; set; } = "";
        public string TokenLateShift { get; set; } = "";
        public string TokenNightShift { get; set; } = "";
        public bool IsNotTimeDefined { get; set; } = false;
        public bool IsMinus { get; set; } = false;
        public double LenghtKM { get; set; } = 0;
        public double Duration { get; set; } = 0;
        public double Workingtime { get; set; } = 156.3;
        public int EmploymentType { get; set; } = 3;
        public double NegativeHour { get; set; } = 28;
        public int MonthNumber { get; set; } = DateTime.Now.Month;
        public int CalculateMonthNumber { get; set; } = 3;
        public string ID { get; set; } = "BlaBla";
        public double Info1 { get; set; }
        public int Info1Type { get; set; } = 0;
        public double Info2 { get; set; }
        public int Info2Type { get; set; } = 0;
        public double Info3;
        public int Info3Type = 0;
        public double Info4;
        public int Info4Type = 0;
        public double Info5;
        public int Info5Type = 0;
    }

}
