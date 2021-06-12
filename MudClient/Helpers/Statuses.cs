using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MudClient.Helpers {
    public static class Statuses {
        public static readonly Dictionary<string, double> Hp = new() {
            { "Healthy", 1.0 },
            { "Scratched", 0.9 },
            { "Hurt", 0.75 },
            { "Wounded", 0.5 },
            { "Battered", 0.3 },
            { "Beaten", 0.15 },
            { "Critical", 0 },
            // { "Stunned", -0.001 }, // Not sure if this can become a status
            { "Incapacitated", -0.01 },
            // { "Mortally wounded", -0.02 }, // Not sure if this can become a status
        };

        public static readonly double bleedingAmount = 0.2;

        public static readonly Dictionary<string, double> SpOrDp = new() {
            { "Bursting", 1.0 },
            { "Full", 0.9 },
            { "Strong", 0.5 },
            { "Good", 0.3 },
            { "Fading", 0.15 },
            { "Trickling", 0.1 },
            { "None", 0},
        };

        public static readonly Dictionary<string, double> Mv = new() {
            { "Fresh", 1.0 },
            { "Full", 0.9 },
            { "Strong", 0.75 },
            { "Tiring", 0.5 },
            { "Winded", 0.3 },
            { "Weary", 0.15 },
            { "Haggard", 0},
            { "Collapsing", -0.01},
        };
    }
}
