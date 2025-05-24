using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MudClient {
    // todo: most of these values aren't actually ever null - they were just nullable in the db

    internal enum ExitKindTypes {
        Normal = 0,
        Door = 1,
        Locked = 2,
    }

    internal enum DirectionType {
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
        Center = 10,
        None = 11, // not 100% sure about this but it seems like labels with this value shouldn't be drawn
        Other = 999,
    }

    internal class ZmudDbOjectTblRow {
        internal int? ObjID { get; set; }
        internal string Name { get; set; }
        internal string IDName { get; set; }
        internal string Hint { get; set; }
        internal string Desc { get; set; }
        internal int? KindID { get; set; }
        internal int? IconID { get; set; }
        internal int? RefNum { get; set; }
        internal int? fKey { get; set; }
        internal int? X { get; set; }
        internal int? Y { get; set; }
        internal int? Z { get; set; }
        internal int? Dx { get; set; }
        internal int? Dy { get; set; }
        internal int? ExitX { get; set; }
        internal int? ExitY { get; set; }
        internal int? ExitZ { get; set; }
        internal int? Cost { get; set; }
        internal int? Color { get; set; }
        internal int? MetaID { get; set; }
        internal int? LabelDir { get; set; }
        internal bool? Enabled { get; set; }
        internal string Script { get; set; }
        internal string Param { get; set; }
        internal string UserStr { get; set; }
        internal int? UserInt { get; set; }
        internal string Content { get; set; }
        internal int? Flags { get; set; }
        internal bool? Deleted { get; set; }
        internal int? UserID { get; set; }
        internal DateTime Modified { get; set; }
        internal int? ZoneID { get; set; }
        internal int? StyleID { get; set; }
        internal DateTime DateAdded { get; set; }
        internal int? ServerId { get; set; }
    }

    internal class ZmudDbExitTblRow {
        internal int? ExitID { get; set; }
        internal int? FromID { get; set; }
        internal int? ToID { get; set; }
        internal int? ExitKindID { get; set; }
        internal string Name { get; set; }
        internal string Param { get; set; } // Door name (I think) or maybe just whether it has a door
        internal string Label { get; set; }
        internal int? X0 { get; set; }
        internal int? Y0 { get; set; }
        internal int? Z0 { get; set; }
        internal int? X1 { get; set; }
        internal int? Y1 { get; set; }
        internal int? Z1 { get; set; }
        internal int? Distance { get; set; }
        internal string Script { get; set; }
        internal int? Color { get; set; }
        internal int? MetaID { get; set; }
        internal bool? DrawRev { get; set; }
        internal int? DirType { get; set; }
        internal int? DirToType { get; set; }
        internal bool? Tested { get; set; }
        internal int? Flags { get; set; }
        internal int? UserID { get; set; }
        internal DateTime Modified { get; set; }
        internal int? ExitIDTo { get; set; }
    }

    internal class ZmudDbZoneTbl {
        internal int? ZoneID { get; set; }
        internal string Name { get; set; }
        internal string ZoneFile { get; set; }
        internal int? UserID { get; set; }
        internal DateTime Modified { get; set; }
        internal string Script { get; set; }
        internal string Desc { get; set; }
        internal int? X { get; set; }
        internal int? Y { get; set; }
        internal int? Z { get; set; }
        internal int? Dx { get; set; }
        internal int? Dy { get; set; }
        internal string Background { get; set; }
        internal int? XScale { get; set; }
        internal int? YScale { get; set; }
        internal int? XOffset { get; set; }
        internal int? YOffset { get; set; }
        internal int? Divisor { get; set; }
        internal int? Multiplier { get; set; }
        internal int? DefSize { get; set; }
        internal int? DefSizeY { get; set; }
        internal int? Res { get; set; }
        internal int? Color { get; set; }
        internal int? Parent { get; set; }
        internal int? MinX { get; set; }
        internal int? MinY { get; set; }
        internal int? MinZ { get; set; }
        internal int? MaxX { get; set; }
        internal int? MaxY { get; set; }
        internal int? MaxZ { get; set; }
        internal int? GridXInc { get; set; }
        internal int? GridYInc { get; set; }
        internal int? GridXOff { get; set; }
        internal int? GridYOff { get; set; }
        internal int? GridCol { get; set; }
        internal int? Flags { get; set; }
    }

}
