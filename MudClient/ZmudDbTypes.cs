using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MudClient {
    // todo: most of these values aren't actually ever null - they were just nullable in the db

    public enum ExitKindTypes {
        Normal = 0,
        Door = 1,
        Locked = 2,
    }

    public enum DirectionType {
        North = 0,
        NE = 1,
        East = 2,
        SE = 3,
        South = 4,
        SW = 5,
        West = 6,
        NW = 7,
        Up = 8,
        Down = 9,
        Other = 999,
    }

    public class ZmudDbOjectTblRow {
        public int? ObjID { get; set; }
        public string Name { get; set; }
        public string IDName { get; set; }
        public string Hint { get; set; }
        public string Desc { get; set; }
        public int? KindID { get; set; }
        public int? IconID { get; set; }
        public int? RefNum { get; set; }
        public int? fKey { get; set; }
        public int? X { get; set; }
        public int? Y { get; set; }
        public int? Z { get; set; }
        public int? Dx { get; set; }
        public int? Dy { get; set; }
        public int? ExitX { get; set; }
        public int? ExitY { get; set; }
        public int? ExitZ { get; set; }
        public int? Cost { get; set; }
        public int? Color { get; set; }
        public int? MetaID { get; set; }
        public int? LabelDir { get; set; }
        public bool? Enabled { get; set; }
        public string Script { get; set; }
        public string Param { get; set; }
        public string UserStr { get; set; }
        public int? UserInt { get; set; }
        public string Content { get; set; }
        public int? Flags { get; set; }
        public bool? Deleted { get; set; }
        public int? UserID { get; set; }
        public DateTime Modified { get; set; }
        public int? ZoneID { get; set; }
        public int? StyleID { get; set; }
        public DateTime DateAdded { get; set; }
        public int? ServerId { get; set; }
    }

    public class ZmudDbExitTblRow {
        public int? ExitID { get; set; }
        public int? FromID { get; set; }
        public int? ToID { get; set; }
        public int? ExitKindID { get; set; }
        public string Name { get; set; }
        public string Param { get; set; }
        public string Label { get; set; }
        public int? X0 { get; set; }
        public int? Y0 { get; set; }
        public int? Z0 { get; set; }
        public int? X1 { get; set; }
        public int? Y1 { get; set; }
        public int? Z1 { get; set; }
        public int? Distance { get; set; }
        public string Script { get; set; }
        public int? Color { get; set; }
        public int? MetaID { get; set; }
        public bool? DrawRev { get; set; }
        public int? DirType { get; set; }
        public int? DirToType { get; set; }
        public bool? Tested { get; set; }
        public int? Flags { get; set; }
        public int? UserID { get; set; }
        public DateTime Modified { get; set; }
        public int? ExitIDTo { get; set; }
    }

    public class ZmudDbZoneTbl {
        public int? ZoneID { get; set; }
        public string Name { get; set; }
        public string ZoneFile { get; set; }
        public int? UserID { get; set; }
        public DateTime Modified { get; set; }
        public string Script { get; set; }
        public string Desc { get; set; }
        public int? X { get; set; }
        public int? Y { get; set; }
        public int? Z { get; set; }
        public int? Dx { get; set; }
        public int? Dy { get; set; }
        public string Background { get; set; }
        public int? XScale { get; set; }
        public int? YScale { get; set; }
        public int? XOffset { get; set; }
        public int? YOffset { get; set; }
        public int? Divisor { get; set; }
        public int? Multiplier { get; set; }
        public int? DefSize { get; set; }
        public int? DefSizeY { get; set; }
        public int? Res { get; set; }
        public int? Color { get; set; }
        public int? Parent { get; set; }
        public int? MinX { get; set; }
        public int? MinY { get; set; }
        public int? MinZ { get; set; }
        public int? MaxX { get; set; }
        public int? MaxY { get; set; }
        public int? MaxZ { get; set; }
        public int? GridXInc { get; set; }
        public int? GridYInc { get; set; }
        public int? GridXOff { get; set; }
        public int? GridYOff { get; set; }
        public int? GridCol { get; set; }
        public int? Flags { get; set; }
    }

}
