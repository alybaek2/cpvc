namespace CPvC
{
    /// <summary>
    /// Enumerates all Amstrad CPC keys. Note that each key is encoded as a two-digit decimal number with the first digit being
    /// the keyboard bit and the second being the keyboard line. See http://cpctech.cpc-live.com/docs/keyboard.html for more
    /// details on the CPC's keyboard.
    /// </summary>
    public class Keys
    {
        public const byte CursorUp = 0;
        public const byte CursorLeft = 1;
        public const byte Clear = 2;
        public const byte Caret = 3;
        public const byte Num0 = 4;
        public const byte Num8 = 5;
        public const byte Num6 = 6;
        public const byte Num4 = 7;
        public const byte Num1 = 8;
        public const byte Joy0Up = 9;

        public const byte CursorRight = 10;
        public const byte Copy = 11;
        public const byte LeftBrace = 12;
        public const byte EqualsSign = 13;
        public const byte Num9 = 14;
        public const byte Num7 = 15;
        public const byte Num5 = 16;
        public const byte Num3 = 17;
        public const byte Num2 = 18;
        public const byte Joy0Down = 19;

        public const byte CursorDown = 20;
        public const byte Function7 = 21;
        public const byte Return = 22;
        public const byte At = 23;
        public const byte O = 24;
        public const byte U = 25;
        public const byte R = 26;
        public const byte E = 27;
        public const byte Escape = 28;
        public const byte Joy0Left = 29;

        public const byte Function9 = 30;
        public const byte Function8 = 31;
        public const byte RightBrace = 32;
        public const byte P = 33;
        public const byte I = 34;
        public const byte Y = 35;
        public const byte T = 36;
        public const byte W = 37;
        public const byte Q = 38;
        public const byte Joy0Right = 39;

        public const byte Function6 = 40;
        public const byte Function5 = 41;
        public const byte Function4 = 42;
        public const byte Plus = 43;
        public const byte L = 44;
        public const byte H = 45;
        public const byte G = 46;
        public const byte S = 47;
        public const byte Tab = 48;
        public const byte Joy0Fire2 = 49;

        public const byte Function3 = 50;
        public const byte Function1 = 51;
        public const byte Shift = 52;
        public const byte Asterix = 53;
        public const byte K = 54;
        public const byte J = 55;
        public const byte F = 56;
        public const byte D = 57;
        public const byte A = 58;
        public const byte Joy0Fire1 = 59;

        public const byte Enter = 60;
        public const byte Function2 = 61;
        public const byte Backlash = 62;
        public const byte Question = 63;
        public const byte M = 64;
        public const byte N = 65;
        public const byte B = 66;
        public const byte C = 67;
        public const byte CapsLock = 68;
        public const byte Unused = 69;

        public const byte FunctionPeriod = 70;
        public const byte Function0 = 71;
        public const byte Control = 72;
        public const byte GreaterThan = 73;
        public const byte LessThan = 74;
        public const byte Space = 75;
        public const byte V = 76;
        public const byte X = 77;
        public const byte Z = 78;
        public const byte Delete = 79;
    }
}
